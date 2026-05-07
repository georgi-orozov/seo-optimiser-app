# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A full-stack AI-powered SEO optimisation chat application. Authenticated users interact
with a conversational UI to engage an AI agent that analyses web pages, audits their HTML
tags, and returns structured optimisation suggestions. The assistant asks clarifying
questions (e.g. target keywords) and iterates on suggestions based on user feedback.

The backend is complete. The frontend is not yet started.

## Repository Structure

```
seo-optimiser-app/
├── docker-compose.yml                     # Root-level compose — runs all three services
├── .env.example                           # Copy to .env and fill in secrets
├── backend/
│   ├── SEOOptimiser.slnx                  # .NET 10 uses .slnx (not .sln)
│   ├── docker-compose.yml                 # Local dotnet run workflow (postgres only)
│   ├── .env                               # gitignored — copy from backend/.env.example
│   ├── .env.example
│   ├── SEOOptimiser.API/                  # HTTP layer: controllers, Program.cs, Dockerfile
│   ├── SEOOptimiser.Core/                 # Domain: entities, interfaces, CQRS use cases, DTOs
│   └── SEOOptimiser.Infrastructure/       # EF Core, migrations, SeoAgentService
└── frontend/                              # React + Vite + TypeScript + Mantine + Clerk
    ├── Dockerfile                         # Multi-stage: Node 22 build → Nginx serve
    └── nginx.conf                         # SPA routing config
```

## Backend

### Stack

- **Runtime**: .NET 10
- **Architecture**: Clean Architecture (3-project solution — Core ← Infrastructure ← API)
- **Database**: PostgreSQL 17 via Entity Framework Core 10 (Npgsql provider)
- **Mediator**: MediatR 13 (CQRS pattern — Commands + Queries)
- **Auth**: Clerk JWT validation (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **AI Agent**: Microsoft Agent Framework (`Microsoft.Agents.AI.Anthropic` 1.3.0-preview) with `claude-sonnet-4-5`
- **Page fetching**: Named `HttpClient` ("PageFetcher") + `HtmlAgilityPack` for HTML parsing
- **API docs**: Swashbuckle (`/swagger`)
- **Migrations**: EF Core code-first (`SEOOptimiser.Infrastructure`)

### Setup — Full Stack (Docker Compose)

```bash
# From the repo root

# 1. Copy and fill in secrets
cp .env.example .env
# Edit .env: POSTGRES_PASSWORD, ANTHROPIC_API_KEY, CLERK_AUTHORITY, VITE_CLERK_PUBLISHABLE_KEY

# 2. Start all three services
docker compose up --build
```

- Frontend: `http://localhost:3000`
- API / Swagger: `http://localhost:8080/swagger`
- Postgres: `localhost:5432`

### Setup — Backend (local dotnet run)

```bash
cd backend

# 1. Copy and fill in secrets
cp .env.example .env
# edit .env with ANTHROPIC_API_KEY, CLERK_AUTHORITY, POSTGRES_PASSWORD

# 2. Copy and fill in local dev appsettings (gitignored)
# edit SEOOptimiser.API/appsettings.Development.json with the same values

# 3. Start database only (for dotnet run)
docker compose up postgres -d

# 4. Run the API locally
dotnet run --project SEOOptimiser.API
```

- Swagger UI: `http://localhost:5273/swagger` (dotnet run, HTTP) or `https://localhost:7271/swagger` (dotnet run, HTTPS)
- Auto-migration runs on every startup — no manual `dotnet ef database update` needed in normal use

### Commands

```bash
# Build
dotnet build SEOOptimiser.slnx

# Add a migration after entity changes
dotnet ef migrations add <MigrationName> \
  -p SEOOptimiser.Infrastructure \
  -s SEOOptimiser.API

# Apply migrations manually (if not using auto-migrate)
dotnet ef database update -p SEOOptimiser.Infrastructure -s SEOOptimiser.API
```

### API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/sessions` | Create a new chat session |
| `GET` | `/api/sessions` | List sessions for the authenticated user |
| `GET` | `/api/sessions/{id}` | Get a single session with its full message history |
| `POST` | `/api/sessions/{id}/messages` | Send a user message, receive agent reply |

All endpoints require a Clerk JWT Bearer token (unless auth bypass is enabled — see below).

### Observability

The backend exports OpenTelemetry traces, structured logs, and metrics via OTLP to the .NET Aspire Dashboard, which starts automatically with `docker compose up`.

- **Browser UI**: http://localhost:18888 (no login required in local dev)
- **OTLP receiver** (internal Docker network): `http://aspire-dashboard:18889`
- **OTLP receiver** (host, for `dotnet run`): `http://localhost:18889`

The dashboard shows traces, correlated log records, and metrics in a single UI.

#### Manual spans in `SeoAgentService`

`Microsoft.Agents.AI.Anthropic` 1.3.0-preview has no built-in OTel support, so spans are added manually via `System.Diagnostics.ActivitySource`. The source is defined in `SEOOptimiser.Infrastructure/Telemetry/SeoTelemetry.cs`:

| Span name | Where | Key tags |
|-----------|-------|----------|
| `mediator.<Name>` | Every MediatR command/query (`TelemetryBehavior`) | `mediator.request` |
| `seo-agent.run` | `SeoAgentService.RunAsync` | `prior.message.count`, `suggestions.captured` |
| `seo-agent.retry` | Missed-tool-call retry block | `retry.reason` |
| `seo-agent.fetch-page` | `FetchPageAsync` | `url`, `fetch.source`, `fetch.response.bytes`, `fetch.blocked` |

#### Using observability with local `dotnet run`

```bash
# 1. Start only the dashboard container
docker compose up aspire-dashboard -d

# 2. Add to backend/SEOOptimiser.API/appsettings.Development.json (gitignored):
#    "OpenTelemetry": { "OtlpEndpoint": "http://localhost:18889" }

# 3. Run the API
dotnet run --project SEOOptimiser.API
```

### Auth Bypass (Development Only)

Set `Auth:Bypass = true` in `appsettings.Development.json` (already the default there) to skip JWT
validation. All requests will be authenticated as `dev-user-bypass`. The Swagger UI will not show
a padlock when bypass is active.

For Docker: add `Auth__Bypass: "true"` to the `api` service environment in `docker-compose.yml`.
Never enable this in production.

### Agent Architecture

The AI agent is implemented in `SEOOptimiser.Infrastructure/Services/SeoAgentService.cs` as a
singleton. `ChatClientAgent` is built once at startup from `AnthropicClient`.

**Conversation history**: There is no serialized agent state. On every `SendMessage` request, all
prior `ChatMessage` rows for the session are loaded from the DB and replayed into a fresh
`AgentSession` via `agentSession.SetInMemoryChatHistory(...)`. This keeps the persistence model
simple and avoids framework-internal serialization.

**`fetch_page` tool**: Registered via `AIFunctionFactory.Create`. Fetches the URL with the named
`HttpClient`, parses HTML with HtmlAgilityPack, and returns a JSON string with `title`,
`metaDescription`, `h1`, `ogTitle`, `ogDescription`, `canonical`.

**System prompt**: Lives in `SEOOptimiser.Core/Constants/AgentSystemPrompt.cs`. Any SEO rule
changes must be made there.

### Key Coding Conventions

- DTOs and result records live in `Core`; EF entity configs live in `Infrastructure`
- Controllers only dispatch MediatR — no business logic
- Handlers throw `KeyNotFoundException` for not-found cases; controllers catch it and return 404
- Use `record` types for Commands, Queries, and DTOs
- Map entities → DTOs in handlers, not in repositories
- Use `CancellationToken` in all async methods
- Environment secrets never in source code — `appsettings.Development.json` (gitignored) or env vars

## Frontend

### Stack

- **Framework**: React 18 + Vite + TypeScript
- **UI Library**: Mantine v7
- **Auth**: Clerk (prebuilt components: `<SignIn />`, `<SignUp />`, `<UserButton />`)
- **HTTP**: Axios with Clerk JWT injected via interceptor
- **State**: Zustand (`useChatStore` in `src/store/chatStore.ts`)
- **Routing**: React Router v6

### Frontend Conventions

- All API calls go through `src/api/` — never inline `fetch`/`axios` in components
- Clerk JWT is attached via a token getter registry in `src/api/client.ts` — `setTokenGetter` is called once from `App.tsx` after Clerk loads; the Axios interceptor calls it on every request
- Mantine `notifications` for all error/success feedback
- `SuggestionCard` renders the structured JSON suggestions in a readable card format — do not dump raw JSON
- TypeScript strict mode enabled — no `any`
- Components stay under 200 lines; split if larger

## What Claude Should Always Do

- Follow Clean Architecture — Infrastructure depends on Core, API depends on Core and Infrastructure, Core depends on nothing
- Use MediatR for ALL controller actions — no business logic in controllers
- Write EF Core migrations for every schema change; never modify the DB manually
- Keep the AI agent's system prompt in `AgentSystemPrompt.cs`, not hardcoded inline
- When adding a new SEO rule, update `AgentSystemPrompt.cs` AND document it in this file
- Ask before adding new NuGet or npm packages not already in the project
- Run `dotnet build SEOOptimiser.slnx` after any backend change to catch errors early
- Run `npm run build` after any frontend change to catch type errors early
- When adding new infrastructure services that call external APIs or do significant async work, add a manual `SeoTelemetry.Source.StartActivity(...)` span following the pattern in `SeoAgentService`
- Never add sensitive data (API keys, message content) as span tags — lengths and counts are fine, raw content is not

## What Claude Should Never Do

- Never put `ANTHROPIC_API_KEY` or any other secret in source code
- Never enable `Auth:Bypass` in production — it authenticates all requests without a token
- Never use `dynamic` or `object` in C# where a typed record/DTO can be used
- Never use `any` in TypeScript
- Never store conversation history only in frontend state — it must be persisted in Postgres
- Never call the Anthropic API directly from the frontend
- Never use `.sln` commands — the solution file is `.slnx` format (use `dotnet build SEOOptimiser.slnx`)

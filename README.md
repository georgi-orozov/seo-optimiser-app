# SEO Optimiser

An AI-powered SEO analysis chat application. Authenticated users paste a URL into a conversational interface; the agent fetches the page, audits its HTML tags (title, meta description, H1, Open Graph), and returns up to three structured improvement suggestions for each tag. Users can then ask follow-up questions, specify target keywords, or request refinements — the agent iterates on its suggestions across the conversation.

**Stack at a glance**

| Layer | Technology |
|---|---|
| Frontend | React 19 + Vite + TypeScript, Mantine UI, Clerk auth |
| Backend | .NET 10, Clean Architecture, MediatR (CQRS), EF Core |
| Database | PostgreSQL 17 |
| AI | Anthropic Claude via Microsoft Agents SDK |
| Observability | OpenTelemetry → .NET Aspire Dashboard |

---

## Prerequisites

- **Docker Desktop 4.x+** — the only required tool for the recommended quickstart
- Ports **3000, 8080, 5432, 18888, 18889** must be free on your machine

---

## Quick Start — Docker (Recommended)

All services (frontend, API, database, observability dashboard) start with a single command.

```bash
# 1. Clone the repository
git clone <repo-url>
cd seo-optimiser-app

# 2. Place the supplied .env file at the repo root
#    (sent via email — see "Credentials" section below)

# 3. Build and start everything
docker compose up --build
```

Once the stack is up:

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API / Swagger | http://localhost:8080/swagger |
| Observability | http://localhost:18888 |

The database schema is applied automatically on first startup — no manual migration step is needed.

To stop: `docker compose down` (add `-v` to also remove the database volume).

---

## Credentials and Environment Variables

A `.env` file will be provided via email. Place it at the **repo root** (next to `docker-compose.yml`). It must contain the four variables below.

| Variable | Required | Purpose |
|---|---|---|
| `POSTGRES_PASSWORD` | Yes | Password for the PostgreSQL `seouser` account |
| `ANTHROPIC_API_KEY` | Yes | Anthropic API key used by the backend to call Claude |
| `CLERK_AUTHORITY` | Yes | Clerk JWT issuer URL — backend uses this to validate auth tokens |
| `VITE_CLERK_PUBLISHABLE_KEY` | Yes | Clerk publishable key — injected into the frontend at build time |

**That is the only file you need beyond the repository.** No separate Clerk account is required; the reviewers authenticate through the candidate's existing Clerk application. The `VITE_CLERK_PUBLISHABLE_KEY` is a publishable (non-secret) key and is safe to distribute.

### Optional overrides

These are not needed for normal evaluation but are available:

| Variable | Default | Effect |
|---|---|---|
| `Auth__Bypass` | `false` | Set to `true` to skip JWT validation — all requests authenticate as `dev-user-bypass`. Useful if Clerk connectivity is an issue. |
| `PAGEFETCHER__USEFAKE` | `false` | Set to `true` to return static fixture HTML instead of fetching real URLs (offline/testing). |

Add either override directly to the `.env` file.

---

## Configuring the AI Model

The model is set in code rather than via an environment variable to keep agent behaviour deterministic:

**[`backend/SEOOptimiser.Infrastructure/Services/SeoAgentService.cs`](backend/SEOOptimiser.Infrastructure/Services/SeoAgentService.cs)** — look for:

```csharp
anthropicClient.AsAIAgent(model: "claude-sonnet-4-5", ...)
```

To use a different model, change the string to any Anthropic model ID (e.g. `claude-opus-4-7`, `claude-haiku-4-5-20251001`) and rebuild the Docker image (`docker compose up --build`).

---

## Models Used and Why

### Main agent — `claude-sonnet-4-5`

Claude Sonnet 4.5 was chosen for the interactive SEO agent because it offers the best balance of reasoning quality, reliable structured tool-call output, and response latency for a real-time chat loop. The agent must:

- hold a multi-turn conversation history,
- process full page HTML in a single context window, and
- call tools (`fetch_page`, `record_seo_suggestions`, `update_seo_suggestion`) with well-formed JSON arguments on every turn.

Sonnet handles all three reliably without the cost or latency of Opus.

### Eval harness — `claude-haiku-4-5-20251001`

Used only in the standalone `SEOOptimiser.Eval` console project (not part of the running application). Haiku generates synthetic test cases and scores agent output on a 0–100 scale cheaply and quickly, making it practical to run a large batch of evaluations offline.

---

## Third-Party Services

### Anthropic

- **Purpose**: Powers the AI agent (Claude Sonnet 4.5)
- **SDK**: `Microsoft.Agents.AI.Anthropic` 1.3.0-preview, which wraps the Anthropic Messages API and provides a `ChatClientAgent` abstraction with tool-use support
- **Credential**: `ANTHROPIC_API_KEY` in `.env`

### Clerk

- **Purpose**: User authentication — sign-up, sign-in, and JWT issuance
- **Frontend**: `@clerk/react` v6 — provides `<SignIn />`, `<SignUp />`, and `useAuth()` hook for token retrieval
- **Backend**: `Microsoft.AspNetCore.Authentication.JwtBearer` validates tokens against the Clerk JWKS endpoint derived from `CLERK_AUTHORITY`
- **Credentials**: `CLERK_AUTHORITY` and `VITE_CLERK_PUBLISHABLE_KEY` in `.env`
- **No Clerk secret key is required** — the backend only needs the public JWKS endpoint to verify signatures

---

## Local Development (Without Docker)

If you prefer to run services outside of containers:

### Backend

```bash
cd backend

# Start only the database
docker compose up postgres -d

# Install .NET 10 SDK if not present: https://dotnet.microsoft.com/download/dotnet/10.0

# Copy and fill in backend-specific env
cp .env.example .env
# Edit .env: set POSTGRES_PASSWORD and ANTHROPIC_API_KEY (CLERK_AUTHORITY optional if Auth__BYPASS=true)

# Run the API
dotnet run --project SEOOptimiser.API
```

- Swagger: http://localhost:5273/swagger (HTTP) or https://localhost:7271/swagger (HTTPS)

### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Copy and configure env
cp .env.example .env
# Set VITE_CLERK_PUBLISHABLE_KEY and VITE_API_BASE_URL=http://localhost:5273

# Start dev server
npm run dev
```

- Frontend: http://localhost:5173

### Observability (optional, local)

```bash
# Start only the dashboard container
docker compose up aspire-dashboard -d
```

Then add to `backend/SEOOptimiser.API/appsettings.Development.json`:

```json
"OpenTelemetry": { "OtlpEndpoint": "http://localhost:18889" }
```

Dashboard: http://localhost:18888

---

## Observability

The .NET Aspire Dashboard starts automatically with `docker compose up` and requires no login in local development.

**http://localhost:18888** shows:

- **Traces** — every request traced end-to-end: API controller → MediatR handler → AI agent run → page fetch → database queries
- **Structured logs** — correlated with traces; click a span to see its log lines
- **Metrics** — ASP.NET Core request counts/durations and HTTP client metrics

`Microsoft.Agents.AI.Anthropic` 1.3.0-preview has no built-in OpenTelemetry support, so spans for agent operations are added manually via `System.Diagnostics.ActivitySource` in [`SeoAgentService`](backend/SEOOptimiser.Infrastructure/Services/SeoAgentService.cs).

---

## Architecture Overview

```
seo-optimiser-app/
├── docker-compose.yml              # Orchestrates all four services
├── .env                            # Secrets (supplied via email, gitignored)
├── backend/
│   ├── SEOOptimiser.API/           # HTTP layer: controllers, middleware, Program.cs
│   ├── SEOOptimiser.Core/          # Domain: entities, interfaces, CQRS commands/queries, DTOs
│   ├── SEOOptimiser.Infrastructure/# EF Core, migrations, SeoAgentService, Telemetry
│   └── SEOOptimiser.Eval/          # Offline eval harness (not part of running stack)
└── frontend/
    ├── src/
    │   ├── api/                    # Axios client with Clerk JWT interceptor
    │   ├── components/             # MessageList, SuggestionCard, ChatInput, etc.
    │   ├── pages/                  # SignInPage, ChatPage
    │   └── store/                  # Zustand chatStore
    └── nginx.conf                  # SPA routing for production build
```

**Request flow:**

1. User sends a message → Clerk JWT attached by Axios interceptor
2. `POST /api/sessions/{id}/messages` → `SendMessageCommand` dispatched via MediatR
3. `SeoAgentService.RunAsync`: all prior `ChatMessage` rows loaded from DB, replayed into a fresh `AgentSession`
4. Agent calls tools as needed (`fetch_page` → HTTP fetch + HtmlAgilityPack parse; `record_seo_suggestions` / `update_seo_suggestion` → DB persist)
5. Final assistant reply saved and returned to the client

---

## Assumptions and Shortcuts

**No agent state serialisation.** Conversation history is stored as plain `ChatMessage` rows in Postgres and replayed into a new `AgentSession` on every request. This avoids any dependency on the SDK's internal state format and keeps the persistence model straightforward. The trade-off is an extra DB read per turn, which is negligible at this scale.

**Auth bypass in development.** `Auth:Bypass=true` injects a synthetic `dev-user-bypass` identity instead of validating a Clerk JWT. This makes local backend development faster but means the JWT validation path is only exercised in the full Docker stack.

**Microsoft Agents SDK (preview).** `Microsoft.Agents.AI.Anthropic` 1.3.0-preview was chosen to stay within the .NET ecosystem. Because it is a preview release, it lacks built-in OpenTelemetry instrumentation — spans are therefore added manually using `System.Diagnostics.ActivitySource`.

**Fake page fetcher.** `PageFetcher:UseFake=true` returns a static HTML fixture instead of fetching real URLs. Useful for offline testing or running the eval harness without network access.

**Eval project excluded from compose.** `SEOOptimiser.Eval` is a standalone console app for offline agent quality scoring. It does not start as part of `docker compose up` and does not need to be run to evaluate the application.

# SEOOptimiser.Eval

A command-line evaluation harness for measuring the quality of the SEO agent's output. Run it before and after any prompt or model change to get a comparable score.

## How it works

1. **Generate** — Haiku 4.5 creates diverse test cases (varied HTML pages with intentional SEO flaws) and saves them as JSON files.
2. **Evaluate** — Each test case is run through the real `SeoAgentService` (using the injected HTML instead of fetching a real URL), then Haiku 4.5 scores the agent's 9 suggestions against a rubric.

Each result contains:
- `score` — 0–100
- `reasoning` — 2–4 sentence explanation
- `strengths` — 1–3 key things the agent did well
- `weaknesses` — 1–3 key areas for improvement

## Setup

Create a `.env` file in this directory (it is gitignored):

```
ANTHROPIC_API_KEY=sk-ant-...
```

The tool loads it automatically on startup. Alternatively, export the variable in your shell before running.

## Commands

All commands are run from this directory:

```bash
cd backend/SEOOptimiser.Eval
```

### Generate test cases

```bash
dotnet run -- generate
dotnet run -- generate --count 10   # generate 10 cases (default: 5, max: 20)
```

Test cases are written to `test-cases/tc-NNN.json`. Running generate multiple times appends new cases without overwriting existing ones — IDs continue from the highest existing number.

### Evaluate

```bash
dotnet run -- evaluate                          # evaluate all test cases
dotnet run -- evaluate --test-case-id tc-001   # evaluate a single case
```

Results are written to `results/tc-NNN-result.json`. At the end of a full run the tool prints an aggregate summary:

```
─────────────────────────────────
Results: 5/5 evaluated
Average: 72.4/100
Min:     58/100
Max:     88/100
─────────────────────────────────
```

## Workflow for iterating on the agent

```bash
# 1. Generate a fixed set of test cases once (reuse across experiments)
dotnet run -- generate --count 10

# 2. Evaluate the current agent — this is your baseline
dotnet run -- evaluate

# 3. Make changes (edit AgentSystemPrompt.cs, tweak the model, etc.)

# 4. Re-evaluate with the same test cases and compare scores
dotnet run -- evaluate
```

## Scoring rubric

Haiku scores each result out of 100 across five dimensions:

| Dimension | Points | What it checks |
|-----------|--------|----------------|
| Completeness | 20 | Exactly 9 suggestions produced (3× title, meta description, h1) |
| Keyword Integration | 25 | Target keywords appear naturally in all suggestions |
| SEO Compliance | 25 | Title 50–60 chars, meta description 150–160 chars, H1 readable |
| Quality & Naturalness | 20 | No keyword stuffing, compelling and human-readable copy |
| Differentiation | 10 | The 3 options per tag are meaningfully distinct |

## File layout

```
SEOOptimiser.Eval/
├── .env                  ← your API key (gitignored)
├── test-cases/           ← generated test case JSON files
│   └── tc-001.json
├── results/              ← evaluation result JSON files
│   └── tc-001-result.json
├── Infrastructure/
│   ├── AnthropicHaikuClient.cs   ← raw HTTP wrapper for Haiku
│   └── NullHttpClientFactory.cs  ← stub so SeoAgentService can be instantiated without DI
├── Models/
│   ├── TestCase.cs
│   └── EvalResult.cs
└── Services/
    ├── TestCaseGeneratorService.cs
    ├── AgentRunnerService.cs
    └── EvaluatorService.cs
```

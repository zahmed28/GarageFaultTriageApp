# Garage Fault Assistant

Service-adviser tool that turns free-text customer fault descriptions into structured workshop triage: vehicle system, urgency, symptoms, workshop checks, clarifying questions, and a safety warning when domain rules require it.

Output is a **triage aid**, not a diagnosis and not a decision to repair.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20+ (or current LTS)

## Build and test

From the repository root:

```bash
dotnet build GarageFaultAssistant.sln
dotnet test GarageFaultAssistant.sln
```

## Run backend

```bash
dotnet run --project src/GarageFaultAssistant.Api
```

Default URL (from `launchSettings.json`): **http://localhost:5031**

Health check: `GET http://localhost:5031/health`

Analyse endpoint: `POST http://localhost:5031/api/fault-assessments/analyse`

## Run frontend

With the API running on port 5031:

```bash
cd frontend
npm install
npm run dev
```

Vite URL: **http://localhost:5173**

The Vite dev server proxies `/api` to `http://localhost:5031`, so leave `VITE_API_BASE_URL` empty in development (see `frontend/.env.development`).

Sample flow: paste a description containing `brake` (Fake provider) → submit → triage result with safety warning for Brakes + Critical.

## Configuration

AI settings bind from the `Ai` configuration section (see [Appendix A in Tasks.md](Docs/04-plans/Tasks.md)).

| Key | Default | Purpose |
|-----|---------|---------|
| `Ai:Provider` | `Fake` | `Fake` or `OpenAI` |
| `Ai:TimeoutSeconds` | `30` | Analysis timeout |
| `Ai:OpenAI:Endpoint` | — | OpenAI-compatible chat completions URL |
| `Ai:OpenAI:ApiKey` | — | **Do not commit.** Use env or user-secrets |
| `Ai:OpenAI:Model` | — | Model id |

### Fake (default)

No API key required. `appsettings.json` / `appsettings.Development.json` set `Ai:Provider` to `Fake`. Keyword fixtures are listed in [Tasks.md Appendix B](Docs/04-plans/Tasks.md).

### Switching to OpenAI

1. Set `Ai:Provider` to `OpenAI` (for example in `appsettings.Development.json`).
2. Ensure `Endpoint` and `Model` are set (defaults exist in `appsettings.json`).
3. Supply the API key **without committing it**:

```bash
# Environment variable (.NET nested-key convention)
# Windows PowerShell:
$env:Ai__OpenAI__ApiKey = "your-key-here"

# bash / zsh:
export Ai__OpenAI__ApiKey=your-key-here
```

Or use .NET user secrets while developing:

```bash
dotnet user-secrets set "Ai:OpenAI:ApiKey" "your-key-here" --project src/GarageFaultAssistant.Api
```

Startup fails with a clear error if `Provider` is `OpenAI` and Endpoint, ApiKey, or Model is missing.

## Architecture

| Doc | Purpose |
|-----|---------|
| [Docs/02-specs/spec.md](Docs/02-specs/spec.md) | Product rules, HTTP contract, domain constraints |
| [Docs/03-design/design.md](Docs/03-design/design.md) | Clean architecture layers, pipeline, AI adapters |
| [Docs/04-plans/Tasks.md](Docs/04-plans/Tasks.md) | Implementation tasks, Ai config, Fake fixtures |

Backend layout (single project, folder layers): Domain → Application → Infrastructure → Api, composed in `Program.cs`. Frontend is a Vite + React + TypeScript SPA under `frontend/`.

## Trade-offs / with more time

v1 deliberately excludes auth, persistence, job cards, VIN lookup, and production CORS (Development Vite origin only). See [spec §2.2–2.3](Docs/02-specs/spec.md).

With more time: save assessments, job-card draft from triage, adviser accounts, richer safety matrix, retries/circuit breaker beyond a single timeout, and observability dashboards.

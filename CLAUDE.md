# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

JobVault is a job-application tracking pipeline: a Claude Agent (separate private repo) evaluates job postings and POSTs structured payloads to this API, which persists the application and hands off document generation (CV/cover letter DOCX+PDF, compatibility report, tailoring notes) to an async worker. Everything lands in a GitHub "vault" repo, the Vue dashboard updates in real time via SSE, and Telegram sends a push notification. Currently single-user/single-tenant.

Full narrative architecture, the end-to-end event flow, and the reasoning behind each infra choice (RabbitMQ vs HTTP, MongoDB vs Postgres, GitHub-as-vault, modular monolith vs microservices) are in [README.md](README.md) — read it before making cross-service changes.

## Commands

### Backend (.NET 9, solution at `backend/src/JobVault.API/JobVault.sln`)

```bash
# Run API / Worker locally
cd backend/src/JobVault.API && dotnet run
cd backend/src/JobVault.Worker && dotnet run

# All backend tests (unit + architecture)
cd backend/src/JobVault.API && dotnet test JobVault.sln

# A single test
dotnet test JobVault.sln --filter "FullyQualifiedName~ApplicationIngestionServiceTests.Should_Publish_Event_On_Success"

# Just architecture tests (layer-boundary enforcement)
dotnet test JobVault.sln --filter "FullyQualifiedName~ArchitectureTests"
```

### Frontend (`frontend/jobvault-ui`, Vue 3 + TS + Vite)

```bash
cd frontend/jobvault-ui
npm run dev          # dev server
npm run build         # vue-tsc -b && vite build (type-checks then builds)
npm test              # vitest run
npm run test:watch    # vitest watch mode
npx vitest run src/composables/__tests__/useCompanies.spec.ts   # single file
```

### Full stack via Docker

```bash
docker network create jobvault-internal   # first time only
docker compose up -d                       # starts jobvault-api + jobvault-worker
```

The generation service (Node/TypeScript DOCX rendering) and the Claude Agent are separate repos, not part of this checkout — see [README.md](README.md#project-structure) for their URLs. `docs/local-development.md` and `docs/env.md` have the full prerequisite/env-var setup.

## Architecture

### Layers and the dependency rule

Backend is Clean Architecture, enforced at build time by `backend/tests/JobVault.ArchitectureTests` (NetArchTest), not just convention:

- **`JobVault.Domain`** — entities/value objects only. Zero dependencies on any other layer or on MongoDB/RabbitMQ/Telegram/ASP.NET Core.
- **`JobVault.Contracts`** — DTOs and events shared across process boundaries (API ↔ Worker via RabbitMQ). Same zero-dependency rule as Domain.
- **`JobVault.Application`** — interfaces (`Interfaces/`) and use-case services (`Services/`). Cannot depend on Infrastructure, API, or Worker, or reference MongoDB/RabbitMQ/Telegram/ASP.NET Core packages directly.
- **`JobVault.Infrastructure`** — concrete implementations (MongoDB persistence, RabbitMQ pub/sub, GitHub vault commits, Telegram, generation-service client). May depend on Application/Domain/Contracts, never on API or Worker.
- **`JobVault.API`** — controllers + `Program.cs`. Controllers must inherit `ControllerBase`, be named `*Controller`, and are barred from touching Infrastructure, `IJobApplicationRepository` directly, the filesystem, `Process` APIs, Markdig, or JWT handlers directly — that logic belongs in Application services. All DI wiring (including which Infrastructure implementation backs which interface) lives in `Program.cs`, not in controllers.
- **`JobVault.Worker`** — hosted background services (`Program.cs` wires everything up, same as API). No direct Infrastructure package references either; goes through Application interfaces.

Every `Application.Interfaces` type must have exactly one implementation, and Infrastructure classes may only implement Application interfaces (not ad-hoc custom ones) — both are asserted by the architecture tests. If you add a new interface/service pair, follow the existing naming convention (`I{Name}` in `Interfaces/`, `{Name}` in `Services/` or the relevant Infrastructure subfolder) or the tests will fail.

Controllers stay thin: request validation and delegation only, all business logic goes through an Application-layer service.

### Event-driven flow (API → RabbitMQ → Worker)

1. Claude Agent POSTs to `POST /api/ingest/applications` → API validates, persists to MongoDB (`Processing`), publishes `job.application.received`, returns `202` immediately.
2. `ApplicationIngestionConsumer` (Worker) consumes it, calls the generation service for CV/cover letter DOCX, converts both to PDF via LibreOffice, commits all 6 files atomically to the GitHub vault (Git Trees API).
3. Worker updates MongoDB to `Ready to Apply`, publishes `job.application.created` and `notification.new`.
4. `RabbitMqConsumer` (Worker) fans out Telegram notifications; `SseNotificationConsumer` persists + broadcasts the notification over SSE to the dashboard.

Transient failures (generation service down, GitHub network errors) retry 3x with exponential backoff; permanent failures (invalid payload, 4xx) skip straight to the dead-letter queue. When touching consumers in `JobVault.Infrastructure/Messaging/RabbitMQ`, preserve this retry/fast-fail distinction.

Env vars are `SCREAMING_SNAKE_CASE` and get mapped to .NET config paths in each `Program.cs` (see the `E(...)` helper) — add new config here, not by reading `Environment.GetEnvironmentVariable` ad hoc elsewhere.

### Frontend

Vue 3 (Composition API, `<script setup>`) + Pinia + Vue Router + Tailwind, in `frontend/jobvault-ui/src`. Data access is composable-based (`composables/useCompanies.ts`, `useJobQueue.ts`, `useNotifications.ts`, ...) wrapping `api.ts`; views (`views/*View.vue`) compose these rather than calling the API directly. Routing is **ID-based**, not name-based — company/application routes key off a stable id, not company name (name changes/renames must not break routes or search; see recent history around `frontend/jobvault-ui/src/router`).

### Identity note

The codebase recently moved from ad hoc identifiers to id-based identity end-to-end (backend routing and frontend routing both), and ingestion has TOCTOU-race protection for concurrent vault writes — keep both invariants in mind when touching ingestion or routing code.

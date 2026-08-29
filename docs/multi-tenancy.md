# Multi-Tenancy Plan

JobVault is currently single-user/single-tenant end to end. This document is the design reference for turning it into a shared deployment that multiple people (friends) can use with fully isolated data, before any implementation starts. It captures the current-state audit, the decisions already made, and the phased plan to execute later.

Status: **design only — nothing in this document is implemented yet.**

---

## ⚠️ Security finding — rotate before this goes further

While auditing `jobvault-claude-agent` (see below), a **live ingestion API key is committed in plaintext** in two files in that repo: `CLAUDE.md` (Step 5b curl example) and `send-to-jobvault.ps1` (likely also `send-to-jobvault-stg.ps1` for staging). This key currently authenticates as the single shared `Ingestion:ApiKey` (today's only ingestion credential — see the audit below). Rotate it and stop committing the live value in plaintext regardless of the multi-tenancy timeline; Phase 3 replaces this scheme with per-user hashed keys anyway, but the exposed value is live *today*.

---

## Decisions made

| Question | Decision |
|---|---|
| Backend deployment model | **Shared instance, isolated accounts.** One API/Worker/MongoDB/RabbitMQ deployment (hosted once, by the owner). Each friend gets their own account; all data is scoped by user id. Not separate per-friend stacks. |
| Claude Agent pipeline model | **Each friend runs their own Agent instance.** Own CV/profile, own schedule/machine, own JobVault API key pointed at the shared backend. Not a central process looping over everyone's profiles. |
| Generation service model | **Each friend runs their own generation-service instance too** (see audit below — the DOCX templates aren't just missing per-tenant config, they're structurally built for one person's specific employment history). The shared Worker calls out to whichever generation-service URL is configured for that tenant. |

These decisions are what make the first one tractable without also having to build tenant-isolated context handling inside the agent or the document renderer — the only shared surface between friends is the JobVault API, which gets real tenant isolation (below), while each friend's CV, career data, and rendered documents never leave their own machine except as the already-scoped API payload they submit.

---

## Current-state audit (why this is genuinely single-tenant today)

Confirmed by reading the code, not assumed:

- **Auth** — [`AuthenticationService.cs`](../backend/src/JobVault.Infrastructure/Auth/AuthenticationService.cs) hardcodes exactly two accounts (`Auth:Email`/`Auth:PasswordHash` "owner" + a demo login) from config. No `Users` collection exists.
- **Data model** — no Mongo document has a tenant/user field. [`JobApplicationDocument.cs`](../backend/src/JobVault.Infrastructure/Persistence/MongoDB/JobApplicationDocument.cs), `PendingJobDocument`, the notifications document, and `AppSettingsDocument` are all global.
- **Settings** — [`SettingsRepository.cs`](../backend/src/JobVault.Infrastructure/Persistence/MongoDB/SettingsRepository.cs) queries with `FilterDefinition<AppSettingsDocument>.Empty` — there is exactly one settings row for the whole deployment (one GitHub owner/repo/branch, one Telegram chat id).
- **Ingestion auth** — [`ApiKeyAttribute.cs`](../backend/src/JobVault.API/Filters/ApiKeyAttribute.cs) checks one shared `X-Api-Key` header against a single `Ingestion:ApiKey` config value. Anyone with that key posts as "the" user.
- **GitHub vault token** — [`GitHubFileService.cs:29`](../backend/src/JobVault.Infrastructure/GitHub/GitHubFileService.cs) reads a single `GitHub:Token` env var. One PAT, one repo, for everyone.
- **SSE fan-out** — `NotificationHub.BroadcastAsync` writes every notification to every connected client, unfiltered. A second account would see a first account's applications land in real time.
- **RabbitMQ events** — `JobApplicationEvent` and friends (`job.application.received/created`, `notification.new`) carry no tenant identifier across the API→Worker boundary at all.

Net effect: multi-tenancy touches every layer — identity, every repository query, the ingestion API key scheme, per-tenant GitHub/Telegram credentials, the event contracts crossing RabbitMQ, and SSE fan-out. There is no shortcut that only touches the frontend or only touches auth.

---

## Backend implementation phases

Ordered by dependency — each phase assumes the previous ones are done. **Phases 0–2 must land before a second real account exists**; everything else is inert (or unsafe) until queries are actually tenant-scoped.

### Phase 0 — Data model foundation
- New `JobVault.Domain.Entities.User` (Id, Email, PasswordHash, Role, IngestionApiKeyHash, CreatedAt) + `IUserRepository` (Application) / `UserRepository` (Infrastructure, Mongo), following the existing `I{Name}` / `{Name}` convention.
- Add `TenantId` (= the user's id) to every document: `JobApplicationDocument`, `PendingJobDocument`, `AppSettingsDocument`, the notifications document.
- One-time migration: create the owner's own `User` row, backfill `TenantId` on all existing data to that id.

### Phase 1 — Real identity
- Replace `AuthenticationService`'s hardcoded email compare with an `IUserRepository` lookup + BCrypt verify.
- `TokenService.GenerateToken` gains a `userId` claim (`sub`/`uid`) — this becomes the tenant id carried on every authenticated request.
- New `ICurrentUserService` (Application interface, backed by `IHttpContextAccessor` in Infrastructure) so Application-layer services read the tenant id without touching `HttpContext` directly — preserves the layering CLAUDE.md enforces.
- Account creation is invite-only, not public signup: an owner-only admin endpoint to create each friend's account is the simplest v1 (no need for email verification / self-serve registration flow at this scale).

### Phase 2 — Scope every query by tenant
- `MongoDbService`, `SettingsRepository`, `PendingJobRepository`, the notification repo: every filter gains `tenantId`. No more `FilterDefinition.Empty` reads.
- **Gotcha to get right**: the TOCTOU-protected unique index on `jobUrlNormalized` (see the Identity note in [CLAUDE.md](../CLAUDE.md)) must become a compound unique index on `(tenantId, jobUrlNormalized)` — otherwise two friends applying to the same public job posting collide with each other's uniqueness constraint.
- `ApplicationQueryService` threads `tenantId` through to the repository calls.
- Add compound indexes (`{tenantId:1, createdAt:-1}` etc.) for query performance once filters are in place.

### Phase 3 — Ingestion identity (Claude Agent → API)
- Replace the single global `Ingestion:ApiKey` with a per-user key: `IngestionApiKeyHash` (+ a displayed prefix) on `User`, generated as `jv_live_<random>`, shown once in the Settings UI, stored only hashed.
- `ApiKeyAttribute` resolves the header to a user via the repository, then populates `ICurrentUserService` for the rest of the pipeline — uniform whether the request authenticated via JWT or API key.
- Each friend's own Agent instance is configured with their own key, pointed at the shared API URL.

### Phase 4 — Carry tenant identity across RabbitMQ
- Add `TenantId` to `JobApplicationEvent` and the other Contracts events crossing the API↔Worker boundary. The Worker has no HTTP context — this field is the only way it knows whose GitHub vault / Telegram chat / generation-service settings to use.
- `ApplicationIngestionConsumer` / `ApplicationProcessorService` load `AppSettings` by `event.TenantId` instead of the single global fetch.
- `RabbitMqConsumer` (Telegram fan-out) sends to that tenant's `TelegramChatId`, not a global one.
- `DocumentGenerationClient` (Infrastructure) currently calls one shared `DOCUMENT_GENERATION_BASE_URL`. Since each friend runs their own generation-service instance (decision above), this becomes per-tenant too — add `GenerationServiceBaseUrl` to `AppSettings` and resolve it from `event.TenantId`, same as the GitHub/Telegram fields.

### Phase 5 — Per-tenant secrets & endpoints
- Move the GitHub PAT off the single `GitHub:Token` env var onto each user's `AppSettings` row (alongside the existing `GitHubOwner`/`Repository`/`Branch`).
- Add `GenerationServiceBaseUrl` (see Phase 4) to the same row — not a secret, but per-tenant config that needs the same "settings row per user" treatment.
- Encrypt secrets at rest via ASP.NET Core's Data Protection API (already in the framework — no new infra); decrypt only inside `GitHubFileService` at call time. The generation-service URL doesn't need encryption, just tenant scoping.

### Phase 6 — Scope SSE
- `NotificationHub.Subscribe()` takes a `tenantId`; `BroadcastAsync` only writes to channels whose tenant matches the notification's `TenantId`.
- Confirm how the SSE client authenticates: `EventSource` can't send custom headers, so the token likely needs to travel as a query param or via a short-lived SSE ticket endpoint. Check `useNotifications.ts` on the frontend when implementing.

### Phase 7 — Frontend
- Mostly unaffected — routing is already id-based and data access is composable-based (`composables/useCompanies.ts`, `useJobQueue.ts`, `useNotifications.ts`, …), so once the backend returns only the caller's data the UI is naturally scoped.
- New surface needed: an admin view for the owner to create/manage friend accounts and issue ingestion API keys (Settings-adjacent).

### Phase 8 — Guardrail
- NetArchTest can't easily assert "every Mongo filter includes `tenantId`" mechanically, so this stays a discipline note rather than an automated rule for now. Once Phases 0–6 land, add an explicit callout to `CLAUDE.md` under the Identity note so future changes don't regress isolation. Revisit a custom Roslyn analyzer only if this becomes a recurring source of bugs.

---

## Generation service (`jobvault-generation-service`)

Read directly (`D:\Personal\Job-Applications\Automation\jobvault-generation-service`). This is the deepest single-tenant coupling in the whole system — not a config gap, an architecture one.

**What's actually in the templates.** `scripts/build-cv-template.js` and `scripts/build-cl-template.js` *generate* `templates/cv_template.docx` / `cl_template.docx` from code. Reading that generator shows the template is built from two kinds of content:
- **Static, hardcoded text**: full name ("KHAWAJA BILAL UDDIN"), phone, email, LinkedIn/GitHub URLs, both university degrees, and — critically — the section headers, company names, locations, and dates for all five work-experience roles, including one entire role ("Independent Software Engineer / Product Development" / JobVault) whose bullets are fully static, not templated at all. The cover letter's closing salutation and name are hardcoded in its template too (confirmed by a comment in the Agent's `CLAUDE.md`: *"the salutation is hardcoded in the DOCX template"*).
- **Dynamic, docxtemplater-tagged content**: `{headline}`, `{summary}`, the skills table, and per-role bullet loops — but the loop tag names themselves (`{#calvergy_bullets}`, `{#senior_baris_bullets}`, `{#developer_baris_bullets}`, `{#junior_baris_bullets}`) are one specific person's actual employer names, hardcoded both in the template XML and as an enum (`VALID_ROLE_IDS` in `src/core/generateCv.ts`, re-validated in `src/rest/router.ts`).

A different person's CV isn't "the same template with different data" — it has a different number of jobs, different employers, different degrees, different fixed/dynamic split. A generic multi-tenant template can't be built by parameterizing this one; each person's work history has to be its own template.

**No identity/tenant concept in the service at all.** `src/rest/server.ts` and `router.ts` have zero auth — no API key, no tenant header. `generateCv.ts` / `generateCoverLetter.ts` read the template from one hardcoded path (`path.resolve(__dirname, '../../templates/cv_template.docx')`) per process. One running instance = one person's documents, full stop.

**Conclusion — matches the Agent decision.** Since each friend already runs their own Agent instance with their own career data, the generation service follows the same model: **each friend builds/runs their own generation-service instance** with their own `build-cv-template.js` (edited for their own roles/education/identity) and their own `templates/*.docx`. The shared JobVault Worker just needs a per-tenant URL to call — see Phase 4/5 above (`GenerationServiceBaseUrl` on `AppSettings`). No changes needed to the generation service's *code* to make it "multi-tenant" — it stays single-tenant by design, one instance per person, same as the Agent. What a friend *does* need is their own copy of this repo as a starting template, with the build script rewritten for their own CV structure.

---

## Claude Agent pipeline (`jobvault-claude-agent`)

Read directly (`D:\Personal\Job-Applications\Automation\jobvault-claude-agent`). Confirms the checklist from the earlier design discussion, now grounded — and surfaced the credential exposure flagged at the top of this document.

| File | Contains | Action for a shareable template |
|---|---|---|
| `CLAUDE.md` | Operator instructions addressed to "Bilal" by name; scoring rubric tuned to Bilal's stack (C#/.NET, Node, Vue); **hardcoded production/staging URLs and a live plaintext ingestion API key** in the Step 5b curl example | Rewrite as generic instructions parameterized by a profile; move URL + key to env vars, never commit the live value |
| `send-to-jobvault.ps1`, `send-to-jobvault-stg.ps1` | Same hardcoded API URL + **the same live plaintext API key** | Read `$env:JOBVAULT_API_URL` / `$env:JOBVAULT_API_KEY` instead of literals |
| `profile-summary.md` | Full identity block: name, location, languages, education, employment timeline, stack confidence levels, salary expectations, differentiators | This *is* the profile — becomes the per-friend file that gets swapped, e.g. `profile/profile-summary.md` |
| `bullet-library.md` (971 lines) | Personal achievement/bullet database keyed to the same 4 role IDs used in the generation service | Per-friend; tightly coupled to that friend's own generation-service role IDs — must be edited together |
| `story-bank.md` (468 lines) | Personal narrative bank for cover letters | Per-friend |
| `skills-rules.md` | Fixed 12-row skills table specific to Bilal's stack | Per-friend |
| `criteria.md` | Job-matching/scoring criteria | Mostly reusable structure, but thresholds (stack match, language gate) are Bilal-specific — review per friend |
| `payload-template.json` | Structural payload shape matching `IngestApplicationRequest` | Reusable as-is — no personal data, ships in the template |
| `jd-input.md` | Scratch space for pasting a JD to process | Reusable as-is |
| `.claude/settings.local.json` | Tool permission allowlist (Bash, PowerShell, Chrome MCP, etc.) | Reusable as-is — no personal data |

Practical takeaway: this repo is small enough (~1,600 lines across the personal files) that "extract into `profile/`" is really "split this repo into a generic template half (`CLAUDE.md` rewritten to reference a profile, `payload-template.json`, `jd-input.md`, `.claude/settings.local.json`) and a personal half (`profile-summary.md`, `bullet-library.md`, `story-bank.md`, `skills-rules.md`, `criteria.md`) that never ships in the shared template's git history." The role-ID coupling between `bullet-library.md` and the generation service's `VALID_ROLE_IDS` means a friend's fork of the Agent and fork of the generation service have to agree on role IDs — worth documenting that contract explicitly in the template README so a friend doesn't drift the two out of sync.

---

## Rollout sequencing

```
Phase 0 (data model) ─▶ Phase 1 (identity) ─▶ Phase 2 (query scoping)
                                                     │
                        ┌────────────────────────────┼───────────────────────────┐
                        ▼                            ▼                           ▼
             Phase 3 (ingestion keys)      Phase 5 (per-tenant secrets      Phase 6 (SSE scoping)
                        │                    & GenerationServiceBaseUrl)
                        ▼
             Phase 4 (tenant on RabbitMQ events,
                       routes to per-tenant generation-service URL)
                        │
                        ▼
             Friend forks the Agent repo + the generation-service repo,
             fills in their own profile/CV/templates, points both at
             their own Phase 3 API key (needs Phase 3 done to authenticate)
                        │
                        ▼
             Phase 7 (frontend admin surface) ─▶ Phase 8 (guardrail note in CLAUDE.md)
```

Do not onboard a second real account until Phases 0–2 are done — an unscoped query is the one bug class that leaks a friend's data to another friend.

Independent of the phase order above: **rotate the exposed ingestion API key now** (see the Security finding at the top) — that isn't gated on any phase.

---

## Open questions to resolve during implementation

- Exact shape of the owner-only "create account" admin endpoint (manual DB insert vs. a real admin UI) — fine to start manual for the first 1–2 friends.
- Whether GitHub vault repos should be one-per-friend (their own GitHub account/PAT, as assumed above) or the owner's single vault repo with per-tenant subfolders. The plan above assumes the former (matches "GitHub-as-vault" being personal to each user); confirm before Phase 5.
- SSE auth transport (query-param token vs. ticket endpoint) — decide when implementing Phase 6, after inspecting `useNotifications.ts`.
- Where does each friend host their generation-service instance — same machine/schedule as their Agent, a small always-on container, or run-on-demand triggered somehow by the Worker? The Worker calls it synchronously during document generation, so it needs to be reachable (not just runnable) whenever that friend's applications are being processed.
- Whether to build actual tooling to keep a friend's `bullet-library.md`/`story-bank.md` role IDs in sync with their generation-service `VALID_ROLE_IDS`, or just document the contract and trust each friend to keep both forks consistent (reasonable at friends-scale).

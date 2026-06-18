# JobVault Unit Test Plan

## Overview

This plan introduces unit testing across the JobVault codebase — a .NET 9 backend (Clean Architecture) and Vue 3 + TypeScript frontend. The repo already has architecture tests (`JobVault.ArchitectureTests`) and empty placeholder projects for unit and integration tests.

---

## Current State

| Area | Status |
|------|--------|
| Architecture tests (NetArchTest) | 8 tests, running in CI |
| Backend unit tests (`JobVault.UnitTests`) | Empty — `.gitkeep` only |
| Backend integration tests (`JobVault.IntegrationTests`) | Empty — `.gitkeep` only |
| Frontend tests | No test framework configured |

---

## Phase 1 — Backend Unit Tests (Priority: High)

### 1.1 Project Setup

**Project:** `backend/tests/JobVault.UnitTests/JobVault.UnitTests.csproj`

Create the `.csproj` with:

| Package | Purpose |
|---------|---------|
| `xunit 2.9.2` | Test framework (matches ArchitectureTests) |
| `xunit.runner.visualstudio 2.8.2` | Test discovery |
| `Microsoft.NET.Test.Sdk 17.12.0` | Test host |
| `coverlet.collector 6.0.2` | Code coverage |
| `NSubstitute 5.3.0` | Mocking framework |
| `FluentAssertions 8.0.1` | Readable assertions |

Add project references to: `JobVault.Application`, `JobVault.Domain`, `JobVault.Contracts`, `JobVault.Infrastructure`, `JobVault.API`.

Register the project in `JobVault.sln` so `dotnet test` picks it up in CI.

### 1.2 Test Directory Structure

```
backend/tests/JobVault.UnitTests/
├── Application/
│   ├── Services/
│   │   ├── ApplicationIngestionServiceTests.cs
│   │   ├── MarkdownParserServiceTests.cs
│   │   └── WebhookHandlerTests.cs
│   └── Common/
│       ├── ApplicationIngestionResultTests.cs
│       ├── UpsertResultTests.cs
│       ├── FileIngestResultTests.cs
│       └── WebhookResultTests.cs
├── Infrastructure/
│   ├── Notifications/
│   │   ├── TelegramNotificationServiceTests.cs
│   │   └── NotificationHubTests.cs
│   └── Messaging/
│       └── SseNotificationConsumerTests.cs
├── API/
│   ├── Controllers/
│   │   ├── VaultControllerTests.cs
│   │   ├── NotificationsControllerTests.cs
│   │   └── WebhookControllerTests.cs
└── Domain/
    └── Entities/
        ├── JobApplicationTests.cs
        └── AppNotificationTests.cs
```

---

### 1.3 Application Layer Tests

#### `ApplicationIngestionServiceTests` (~20 tests)

The core business service. Depends on `IJobApplicationRepository`, `IRabbitMqPublisher`, `ILogger` — all mockable.

| Test Case | Asserts |
|-----------|---------|
| `IngestAsync_ValidRequest_ReturnsSuccessWithId` | `IsSuccess == true`, `ApplicationId` matches repo return |
| `IngestAsync_MissingCompanyName_ReturnsFailure` | Error: "companyName is required" |
| `IngestAsync_MissingJobTitle_ReturnsFailure` | Error: "jobTitle is required" |
| `IngestAsync_MissingRecommendation_ReturnsFailure` | Error: "recommendation is required" |
| `IngestAsync_MatchScoreBelowZero_ReturnsFailure` | Error: "matchScore must be between 0 and 100" |
| `IngestAsync_MatchScoreAbove100_ReturnsFailure` | Error: "matchScore must be between 0 and 100" |
| `IngestAsync_MatchScoreAtBoundary0_Succeeds` | Boundary: 0 is valid |
| `IngestAsync_MatchScoreAtBoundary100_Succeeds` | Boundary: 100 is valid |
| `IngestAsync_MissingCvBase64_ReturnsFailure` | Error: "cvDocxBase64 is required" |
| `IngestAsync_InvalidCvBase64_ReturnsFailure` | Error: "cvDocxBase64 is not valid base64" |
| `IngestAsync_MissingCoverLetterBase64_ReturnsFailure` | Error: "coverLetterDocxBase64 is required" |
| `IngestAsync_InvalidCoverLetterBase64_ReturnsFailure` | Error: "coverLetterDocxBase64 is not valid base64" |
| `IngestAsync_MissingCompatibilityReport_ReturnsFailure` | Error: "compatibilityReportMarkdown is required" |
| `IngestAsync_MissingTailoringNotes_ReturnsFailure` | Error: "tailoringNotesMarkdown is required" |
| `IngestAsync_RepositoryUpsertFails_ReturnsFailure` | Error: "Failed to persist application" |
| `IngestAsync_RepositoryReturnsNullId_ReturnsFailure` | `UpsertResult.Id == null` case |
| `IngestAsync_RabbitMqPublishFails_StillReturnsSuccess` | Publisher throws, result is still success (202 pattern) |
| `IngestAsync_SetsStatusToProcessing` | `JobApplication.Status == "Processing"` on repository call |
| `IngestAsync_DefaultsCurrencyToEUR` | When `Currency` is null |
| `IngestAsync_DefaultsSalaryPeriodToAnnual` | When `SalaryPeriod` is null |

#### `MarkdownParserServiceTests` (~8 tests)

Pure parsing logic. Only dependency is `ILogger`.

| Test Case | Asserts |
|-----------|---------|
| `ExtractJobApplication_ValidMarkdownWithJson_ReturnsParsedApplication` | Parses `CompanyName`, `JobTitle`, etc. |
| `ExtractJobApplication_EmptyString_ReturnsNull` | Null on empty input |
| `ExtractJobApplication_NullString_ReturnsNull` | Null on null input |
| `ExtractJobApplication_NoJsonCodeBlock_ReturnsNull` | Markdown without ` ```json ``` ` |
| `ExtractJobApplication_InvalidJson_ReturnsNull` | Malformed JSON inside code block |
| `ExtractJobApplication_EmptyJsonBlock_ReturnsNull` | ` ```json ``` ` with nothing inside |
| `ExtractJobApplication_MultipleJsonBlocks_ParsesFirst` | Only first match used |
| `ExtractJobApplication_CaseInsensitiveProperties_Parses` | `companyName` vs `CompanyName` |

#### `WebhookHandlerTests` (~10 tests)

Depends on `IGitHubFileService`, `IMarkdownParserService`, `IJobApplicationRepository`, `IRabbitMqPublisher`, `ILogger`.

| Test Case | Asserts |
|-----------|---------|
| `HandleAsync_NoCommits_ReturnsSuccessNoCommits` | Message: "No commits to process" |
| `HandleAsync_CommitWithNewFile_ProcessesApplication` | Calls file service, parser, repo upsert |
| `HandleAsync_CommitWithModifiedFile_ProcessesApplication` | Modified files also processed |
| `HandleAsync_NoValidCompanyNames_ReturnsFailure` | No markdown files in paths |
| `HandleAsync_GitHubFetchReturnsNull_LogsAndContinues` | Skips that file gracefully |
| `HandleAsync_ParserReturnsNull_LogsAndContinues` | Markdown parse failure skipped |
| `HandleAsync_UpsertFails_LogsAndContinues` | Partial failure doesn't stop batch |
| `HandleAsync_NewDocument_PublishesCreatedEvent` | `EventType == "created"` |
| `HandleAsync_UpdatedDocument_PublishesUpdatedEvent` | `EventType == "updated"` |
| `HandleAsync_RabbitMqFails_LogsAndContinues` | Publishing failure is non-fatal |

---

### 1.4 Result Object Tests

Simple factory-method validation — fast to write, prevents regressions.

#### `ApplicationIngestionResultTests` (~4 tests)

| Test Case | Asserts |
|-----------|---------|
| `Success_SetsIsSuccessTrue_AndApplicationId` | `IsSuccess == true`, `ApplicationId == "abc"` |
| `Failure_SetsIsSuccessFalse_AndErrorMessage` | `IsSuccess == false`, `ErrorMessage` set |
| `Success_HasNullErrorMessage` | `ErrorMessage == null` |
| `Failure_HasNullApplicationId` | `ApplicationId == null` |

#### `UpsertResultTests` (~4 tests)

| Test Case | Asserts |
|-----------|---------|
| `Success_NewDocument_SetsIsNewDocumentTrue` | `IsNewDocument == true` |
| `Success_ExistingDocument_SetsIsNewDocumentFalse` | `IsNewDocument == false` |
| `Failure_SetsIsSuccessFalse` | `IsSuccess == false` |
| `Success_ContainsId` | `Id` is populated |

#### `FileIngestResultTests` (~3 tests)

#### `WebhookResultTests` (~3 tests)

---

### 1.5 API Controller Tests

Controllers are thin — test request/response mapping and HTTP status codes.

#### `VaultControllerTests` (~8 tests)

| Test Case | Asserts |
|-----------|---------|
| `IngestApplication_Success_Returns202Accepted` | `AcceptedResult` with `ApplicationId` |
| `IngestApplication_ValidationFails_Returns400` | `BadRequestObjectResult` |
| `IngestApplication_ServiceThrows_Returns500` | `ObjectResult` with 500 status |
| `Ingest_MissingCompanyName_Returns400` | Legacy endpoint validation |
| `Ingest_NoFiles_Returns400` | Empty form submission |
| `Ingest_ValidForm_Returns200WithResponse` | `OkObjectResult` with `IngestResponse` |
| `Ingest_ServiceFails_Returns500` | Service returns failure |
| `Ingest_ValidRequest_CallsServiceWithCorrectArgs` | Verify mapped `IngestedFile` objects |

#### `NotificationsControllerTests` (~5 tests)

| Test Case | Asserts |
|-----------|---------|
| `GetNotifications_ReturnsOkWithList` | 200 with notification array |
| `GetNotifications_RepositoryThrows_Returns500` | Error handling |
| `MarkAllRead_ReturnsOk` | 200 OK |
| `MarkRead_ValidGuid_ReturnsOk` | 200 OK |
| `StreamNotifications_SetsSseHeaders` | Content-Type, Cache-Control, Connection headers |

#### `WebhookControllerTests` (~4 tests)

| Test Case | Asserts |
|-----------|---------|
| `HandleWebhook_Success_Returns200` | `OkObjectResult` with `WebhookResponse` |
| `HandleWebhook_Failure_Returns400` | `BadRequestObjectResult` |
| `HandleWebhook_Exception_Returns500` | Error handling |
| `HandleWebhook_CallsHandlerWithPayload` | Verify correct payload passed |

---

### 1.6 Infrastructure Tests (Isolated Logic Only)

These test infrastructure classes in isolation with mocked external dependencies (no real MongoDB/RabbitMQ).

#### `NotificationHubTests` (~6 tests)

| Test Case | Asserts |
|-----------|---------|
| `Subscribe_ReturnsDisposableAndReader` | Non-null tuple |
| `BroadcastAsync_SingleSubscriber_ReceivesNotification` | Message appears on channel |
| `BroadcastAsync_MultipleSubscribers_AllReceive` | All channels get the message |
| `Dispose_Subscription_NoLongerReceives` | Disposed subscriber is removed |
| `Dispose_Subscription_CompletesChannel` | `ChannelReader.Completion` is done |
| `ConcurrentSubscribeAndBroadcast_ThreadSafe` | No exceptions under concurrency |

#### `SseNotificationConsumerTests` (~8 tests)

Static methods `BuildNotification` and `SlugifyCompanyName` — pure functions, no mocking needed.

| Test Case | Asserts |
|-----------|---------|
| `BuildNotification_CreatedEvent_SetsTypeNewApplication` | `Type == "new_application"` |
| `BuildNotification_UpdatedEvent_SetsTypeStageChanged` | `Type == "stage_changed"` |
| `BuildNotification_UnknownEvent_SetsTypeSyncCompleted` | Default fallback |
| `BuildNotification_IncludesCompanyInTitle` | Title contains `CompanyName` |
| `BuildNotification_IncludesScoreInBody` | Body contains `MatchScore` |
| `SlugifyCompanyName_SpacesToHyphens` | `"Acme Corp"` → `"acme-corp"` |
| `SlugifyCompanyName_RemovesSpecialChars` | `"O'Reilly, Inc."` → `"oreilly-inc"` |
| `SlugifyCompanyName_TrimsTrailingHyphen` | No trailing `-` |

#### `TelegramNotificationServiceTests` (~6 tests)

| Test Case | Asserts |
|-----------|---------|
| `SendNotification_NotInitialized_LogsWarning` | No Telegram call, warning logged |
| `SendNotification_CreatedEvent_FormatsCorrectMessage` | Message contains company, score |
| `SendNotification_UpdatedEvent_FormatsUpdateMessage` | Different format for updates |
| `SendNotification_UnknownEventType_UsesGenericFormat` | Fallback message |
| `SendNotification_TelegramApiThrows_LogsError` | Exception caught and logged |
| `Constructor_MissingConfig_LogsWarning` | Graceful degradation |

---

### 1.7 Domain Entity Tests

#### `JobApplicationTests` (~4 tests)

| Test Case | Asserts |
|-----------|---------|
| `NewJobApplication_HasDefaultTimestamps` | `CreatedAt`/`UpdatedAt` defaults |
| `NewJobApplication_StatusDefaultsToNull` | No implicit status |
| `Properties_CanBeSetAndRead` | Roundtrip all 17 properties |
| `MatchScore_AcceptsFullRange` | 0, 50, 100 all valid |

#### `AppNotificationTests` (~3 tests)

| Test Case | Asserts |
|-----------|---------|
| `NewNotification_ReadDefaultsToFalse` | `Read == false` |
| `NewNotification_IdIsGuid` | Valid `Guid` |
| `Properties_CanBeSetAndRead` | Roundtrip all properties |

---

## Phase 2 — Frontend Unit Tests (Priority: Medium)

### 2.1 Framework Setup

Add to `frontend/jobvault-ui/package.json`:

| Package | Purpose |
|---------|---------|
| `vitest` | Test runner (Vite-native, fast) |
| `@vue/test-utils` | Vue component mounting |
| `@testing-library/vue` | DOM queries (optional) |
| `jsdom` | Browser environment simulation |

Add scripts:
```json
{
  "test": "vitest run",
  "test:watch": "vitest",
  "test:coverage": "vitest run --coverage"
}
```

Create `vitest.config.ts` with `jsdom` environment and `@/` path alias resolution.

### 2.2 Test Directory Structure

```
frontend/jobvault-ui/src/
├── utils/
│   └── __tests__/
│       └── score.test.ts
├── composables/
│   └── __tests__/
│       ├── useCompanies.test.ts
│       ├── useNotifications.test.ts
│       └── useTheme.test.ts
```

### 2.3 Utility Tests

#### `score.test.ts` (~18 tests)

Pure functions — no mocking needed.

| Test Case | Asserts |
|-----------|---------|
| `recommendColor — Strong Apply` | Returns emerald palette |
| `recommendColor — Moderate Apply` | Returns amber palette |
| `recommendColor — Risky Apply` | Returns red palette |
| `recommendColor — Skip` | Returns slate palette |
| `recommendColor — unknown string` | Returns default slate |
| `matchPctColor — null` | Returns `text-text-muted` |
| `matchPctColor — 80+` | Returns `text-emerald-400` |
| `matchPctColor — 65-79` | Returns `text-blue-400` |
| `matchPctColor — 50-64` | Returns `text-amber-400` |
| `matchPctColor — below 50` | Returns `text-red-400` |
| `matchPctBar — null` | Returns `bg-slate-600` |
| `matchPctBar — thresholds` | Same breakpoints as color |
| `STAGE_COLORS — every stage has dot/text/bg` | No undefined values |
| `OUTCOME_COLORS — Pass/Fail/Pending` | All three defined |

#### Boundary value tests

| Test Case | Asserts |
|-----------|---------|
| `matchPctColor — exactly 80` | Emerald (>= check) |
| `matchPctColor — exactly 65` | Blue |
| `matchPctColor — exactly 50` | Amber |
| `matchPctColor — 49` | Red |

### 2.4 Composable Tests

#### `useCompanies.test.ts` (~12 tests)

Mock `fetch` globally. Test reactive computed properties.

| Test Case | Asserts |
|-----------|---------|
| `loadCompanies — success` | `companies` populated, `loaded == true` |
| `loadCompanies — fetch error` | `error` set, `companies` empty |
| `loadCompanies — uses mock data` | When `VITE_USE_MOCK` is set |
| `filtered — stage filter` | Only matching stage returned |
| `filtered — search filter` | Case-insensitive name matching |
| `filtered — combined filters` | Both applied |
| `stats.total` | Correct count |
| `stats.avgMatchPct` | Handles null match scores |
| `stats.interviewConversionRate` | Applied → Interview percentage |
| `pipelineCounts` | Count per stage with percentage |
| `scoreDistribution` | Count per recommendation tier |
| `getByName` | Returns correct company |

#### `useNotifications.test.ts` (~10 tests)

Mock `fetch` and `EventSource`.

| Test Case | Asserts |
|-----------|---------|
| `loadNotifications — success` | Notifications array populated |
| `loadNotifications — error` | Error state set |
| `connect — creates EventSource` | SSE URL correct |
| `onmessage — prepends notification` | New items at front |
| `onmessage — malformed data` | Silently ignored |
| `onerror — triggers reconnect` | 5-second timer set |
| `unreadCount — counts unread` | Filters `read == false` |
| `markAllRead — updates all` | All items `read = true` |
| `markRead — updates single` | Target item `read = true` |
| `disconnect — clears EventSource` | `connected == false` |

#### `useTheme.test.ts` (~6 tests)

Mock `localStorage` and `document.documentElement`.

| Test Case | Asserts |
|-----------|---------|
| `default — dark mode` | `isDark == true` |
| `toggle — switches to light` | `isDark == false`, class applied |
| `toggle twice — back to dark` | Roundtrip |
| `setTheme — explicit light` | `theme == 'light'` |
| `persists to localStorage` | `setItem` called |
| `restores from localStorage` | Reads saved preference |

---

## Phase 3 — CI Integration

### 3.1 Update `dotnet test` in CI

The existing GitHub Actions workflow already runs `dotnet test JobVault.sln`. Once `JobVault.UnitTests` is registered in the `.sln`, it will be picked up automatically.

### 3.2 Add Frontend Test Job

Add a step to `.github/workflows/ci-cd-with-webhook.yml`:

```yaml
- name: Run Frontend Tests
  working-directory: frontend/jobvault-ui
  run: |
    npm ci
    npm test
```

### 3.3 Coverage Reporting

- Backend: `coverlet` is already included — run with `--collect:"XPlat Code Coverage"` flag
- Frontend: Add `@vitest/coverage-v8` for coverage output
- Consider adding a coverage gate (e.g., 60% minimum for new code)

---

## Execution Order

| Step | Scope | Estimated Tests | Effort |
|------|-------|-----------------|--------|
| 1 | Create `JobVault.UnitTests.csproj`, register in `.sln` | — | Small |
| 2 | Result object tests (Application/Common) | ~14 | Small |
| 3 | `MarkdownParserServiceTests` | ~8 | Small |
| 4 | `ApplicationIngestionServiceTests` | ~20 | Medium |
| 5 | `WebhookHandlerTests` | ~10 | Medium |
| 6 | `SseNotificationConsumerTests` (static methods) | ~8 | Small |
| 7 | `NotificationHubTests` | ~6 | Small |
| 8 | Controller tests (Vault, Notifications, Webhook) | ~17 | Medium |
| 9 | `TelegramNotificationServiceTests` | ~6 | Medium |
| 10 | Domain entity tests | ~7 | Small |
| 11 | Frontend setup (Vitest + config) | — | Small |
| 12 | `score.test.ts` | ~18 | Small |
| 13 | `useCompanies.test.ts` | ~12 | Medium |
| 14 | `useNotifications.test.ts` | ~10 | Medium |
| 15 | `useTheme.test.ts` | ~6 | Small |
| 16 | CI integration | — | Small |

**Total: ~142 unit tests**

---

## Conventions

- **Naming:** `MethodName_Scenario_ExpectedResult` for test methods
- **Arrange-Act-Assert** pattern in all tests
- **One assertion concept per test** (multiple `Assert` calls okay if testing one logical thing)
- **No test interdependencies** — each test creates its own state
- **Mocking:** NSubstitute for backend, `vi.fn()` / `vi.mock()` for frontend
- **No real I/O** — all external calls (MongoDB, RabbitMQ, HTTP, filesystem) are mocked

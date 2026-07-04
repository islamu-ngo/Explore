<!-- ABOUTME: Working context log for the improved overall testing implementation stream. -->
<!-- ABOUTME: Records completed slices, verification evidence, decisions, risks, and handoff notes. -->

# Improved Overall Testing - Context

Last Updated: 2026-07-04 Europe/Brussels

## Session Progress (2026-07-04 Europe/Brussels)

### Completed
- Planning workstream created at `dev/active/improved-overall-testing/`.
- Current-state report completed with evidence from testing docs, workflows, AppHost/Compose Mailpit wiring, email dispatch docs, and current test files.
- Context7 documentation was consulted for TUnit, Testcontainers for .NET, and Microsoft Playwright .NET.
- User directed implementation of the plan on 2026-07-04.
- Phase 0 baseline build captured: `dotnet build --configuration Release --verbosity quiet` passed with 25 projects, 0 errors, and existing package advisory warnings.
- Phase 0 category taxonomy foundation added for Email, RabbitMQ, Runtime, Slow, Manual, and E2E-focused lanes in code and `docs/TESTING.md`.
- Corrected category filter syntax from the rejected `////[Category=...]` form to the current TUnit/Microsoft Testing Platform form `/*/*/*/*[Category=...]`.
- Phase 0 obsolete-test audit recorded in `obsolete-test-audit.md`; removed the `LocationService.GetLocations()` compatibility alias and its test because it had no production callers.
- Phase 1.2/2.1 added a local `Explore.Infrastructure.Tests` Mailpit Testcontainers fixture and `SmtpEmailServiceMailpitTests` proving real MailKit SMTP send and connection checks against Mailpit.
- Phase 2.2 added no-container `SmtpEmailServiceConfigurationTests` for missing SMTP configuration failure behavior before provider handoff.
- Phase 2.3 extended `SmtpConfigResolverTests` and tagged it as Email coverage. The suite now proves tenant `SettingContext` propagation, tenant-specific cache separation, missing host/from-address behavior, defaults, and specific-tenant cache invalidation.
- Phase 2.4 added a real Mailpit send assertion that `EmailResult.Message` and `EmailResult.ErrorMessage` do not echo sentinel body text or a sentinel SMTP password value.
- Phase 3.1 added `EmailDispatchDrainMailpitTests`, which drives `EmailDispatchDrainService.ProcessBatchAsync` through real `SmtpEmailService` and Mailpit with a stateful in-memory outbox repository.
- Phase 3.2 extended `EmailDispatchDrainMailpitTests` with a duplicate-consumer race around `ProcessSingleAsync`; the test asserts exactly one Sent outcome, one already-claimed/settled outcome, one attempt, one completed receipt, and one Mailpit message.
- Phase 3.3 extended `EmailDispatchDrainServiceTests` for exhausted retry budget dead-letter outcomes and timeout-like unknown outcomes.
- Phase 3.4 was satisfied by existing tenant pause/resume/replay/HAL coverage and verified with focused infrastructure, persistence, and API/HAL test runs.
- Phase 3.5 updated `docs/EMAIL_NOTIFICATIONS.md` with the Basic Dispatch test evidence matrix and focused commands, complementing the broader matrix in `docs/TESTING.md`.
- Phase 4.1 added the missing null-pointer TickerQ wrapper test and verified the API Email lane.
- Phase 4.2 extracted a deterministic hosted drain cycle runner and added API Email coverage proving hosted-service fallback runs recovery before batch drain through `IEmailDispatchDrainService`.
- Phase 4.3 closed readiness gaps for Basic Dispatch `Mode=Disabled` and RabbitMQ healthy-enabled transport state.
- Phase 1.3/1.4 added a RabbitMQ Testcontainers fixture, runtime smoke test, package lock update, and bounded polling diagnostics for RabbitMQ management readiness.
- Phase 5.1 added a live RabbitMQ topology and health test for enabled transport mode.
- Phase 5.2 added live RabbitMQ confirmed publish and mandatory-return outcome coverage.
- Phase 5.3 added live broker payload privacy coverage by reading a published RabbitMQ message from the dispatch queue.
- Phase 5.4 added a live RabbitMQ consumer-to-Mailpit test proving ACK after durable `Sent` state.
- Phase 5.5 added live RabbitMQ malformed-pointer and missing-outbox DLQ coverage with no Mailpit send.
- Phase 5.6 added live RabbitMQ DLQ replay and parking coverage.
- Phase 6.1 was already satisfied in current source by `RegistrationFlowTests`; focused registration E2E passed against the existing build.
- Phase 6.2 standardized E2E browser flow categories: every `Explore.Blazor.Client.E2ETests/Flows/*Tests.cs` file now carries `E2E`, and the sidebar visual suite also carries `Manual`.
- Phase 6.3 required no new Blazor admin SMTP/UI tests because the implementation did not change the admin SMTP component surface; the existing Blazor client suite passed.
- Phase 6.4 verified the BFF token boundary with the Blazor integration suite after loading the project BFF guidance.
- CI lane split updated: fast CI runs `Explore.Infrastructure.Tests` with `Category!=Runtime`, while integration-enabled callers run focused infrastructure `Email` and `RabbitMQ` categories and publish lane-specific TRX evidence.
- Phase 7 finalized testing, email, operations, troubleshooting, contributor, getting-started, and workflow documentation for Mailpit, Basic Dispatch, RabbitMQ Dispatch, E2E categories, runtime logs, TRX, and container diagnostics.
- Verification uncovered an outdated `AdminContextTests` first-party principal helper; it now includes the current `internal_user_id` claim expected by `AdminContext`.
- The E2E test project build now passes after the source moved past the earlier `SupportAccessFlowTests.cs` `PlatformDefaults` compile blocker.
- Final verification uncovered and fixed two non-email regressions needed to leave the touched suite green: `EventRegistrationRepository.GetRegistrationsByEventWithDetailsPaged` now includes `User` for event-scoped registration details, and `SupportAccessClientService.StartAsync` now uses the approved `IBffClient.SendAsync` boundary instead of raw HTTP JSON helpers.
- Final implementation verification is recorded below. The implementation workstream is complete; the remaining full-suite failure is isolated to unrelated `Event.API.IntegrationTests` auth/storage/projection behavior.

### In Progress
- None. Implementation is complete.

### Next
1. Resolve the unrelated `Event.API.IntegrationTests` full-suite failures in the owning backend/API workstream.
2. Keep full Aspire-backed E2E in manual/nightly lanes until reliability evidence supports PR-gate promotion.

### Blockers
- Full `Event.API.IntegrationTests` does not pass in the shared dirty worktree. The focused `Email` lane is green, but the full suite has 19 unrelated auth/storage/projection failures such as `401 Unauthorized` where tests expected OK and one projection empty-body JSON parse failure.

## Quick Resume
1. Read `improved-overall-testing-plan.md`.
2. Read `improved-overall-testing-tasks.md`.
3. Start with Phase 0 unless the user gives a different priority.
4. Keep all three dev docs updated after each meaningful implementation slice.
5. Do not claim implementation is complete unless plan/context/tasks reflect the actual state.

## Key Files And Responsibilities
| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `docs/TESTING.md` | Existing | Docs | Canonical test strategy, projects, commands, taxonomy. | Needs email scenario matrix and updated category commands. |
| `.claude/rules/tests.md` | Existing | Rules | Test governance, TUnit commands, skip rules. | Keep aligned with any taxonomy changes. |
| `.github/workflows/_build-test.yml` | Existing | DevOps | Fast and integration CI lanes. | May need category additions or email focused evidence. |
| `.github/workflows/e2e.yml` | Existing | DevOps | Nightly/manual Aspire-backed Playwright lane. | Candidate for email E2E assertions and artifacts. |
| `Explore.Blazor.Client.E2ETests/Fixtures/MailpitContainerFixture.cs` | Existing | E2E | Starts Mailpit and exposes HTTP inspection helpers. | Candidate to reuse/extract after Phase 1. |
| `Explore.Infrastructure.Tests/Fixtures/RabbitMqContainerFixture.cs` | New | Test support | Starts Testcontainers RabbitMQ with management diagnostics. | Runtime-tagged; exposes AMQP connection string without logging credentials. |
| `Explore.Blazor.Client.E2ETests/Fixtures/AppHostFixture.cs` | Existing | E2E | Starts Aspire testing app and preconfigures Mailpit SMTP. | Already exposes Mailpit helpers. |
| `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/RegistrationFlowTests.cs` | Existing | E2E | Browser registration journey. | Already proves Mailpit delivery and carries `E2E` plus `Email` categories. |
| `Explore.Infrastructure/Mail/SmtpEmailService.cs` | Existing | Infrastructure | MailKit SMTP sender. | Direct Mailpit integration tests target this behavior. |
| `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` | Existing | Infrastructure | Resolves tenant-aware SMTP config. | Config precedence tests should stay here/in infrastructure tests. |
| `Explore.Infrastructure/EmailDispatchDrainService.cs` | Existing | Infrastructure | Shared durable outbox drain. | Basic and RabbitMQ tests must converge here. |
| `Explore.Infrastructure/Messaging/RabbitMqEmailDispatchTransport.cs` | Existing | Infrastructure | RabbitMQ topology/publish transport. | Needs live broker tests. |
| `Explore.Infrastructure/Messaging/EmailDispatchRabbitMqConsumerService.cs` | Existing | Infrastructure | Manual-ack RabbitMQ consumer over drain service. | Needs live consume-to-Mailpit proof. |
| `Explore.API/BackgroundServices/EmailDispatchHostedDrainRunner.cs` | New | API | Runs one hosted-service fallback drain cycle. | Deterministic test seam over `IEmailDispatchDrainService`; no SMTP/RabbitMQ ownership. |
| `Event.API.IntegrationTests/Features/EmailDispatchProcessorTests.cs` | New | API tests | Hosted-service fallback trigger coverage. | Proves recovery-before-drain ordering and configured processor settings propagation. |
| `Event.API.IntegrationTests/Features/EmailDispatchHealthCheckTests.cs` | Existing | API tests | Basic Dispatch readiness coverage. | Covers enabled, disabled, `Mode=Disabled`, TickerQ scheduler disabled, and HostedService. |
| `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchRabbitMqHealthCheckTests.cs` | Existing | Infrastructure tests | Optional RabbitMQ readiness coverage. | Covers disabled, healthy-enabled, and unhealthy transport states. |
| `Explore.Infrastructure.Tests/Infrastructure/RabbitMqContainerFixtureTests.cs` | New | Infrastructure tests | RabbitMQ fixture smoke coverage. | Starts real broker and verifies AMQP plus management overview readiness. |
| `Explore.Infrastructure.Tests/Infrastructure/RabbitMqEmailDispatchTransportLiveTests.cs` | New | Infrastructure tests | Live RabbitMQ transport topology coverage. | Proves enabled topology declaration and healthy readiness against a real broker. |
| `Explore.Infrastructure.Tests/Infrastructure/RabbitMqEmailDispatchConsumerMailpitTests.cs` | New | Infrastructure tests | Live RabbitMQ consumer to Mailpit coverage. | Proves valid pointer drains via real SMTP and broker ACK follows durable sent state. |
| `Explore.Infrastructure.Tests/Infrastructure/RabbitMqEmailDispatchDeadLetterReplayLiveTests.cs` | New | Infrastructure tests | Live RabbitMQ DLQ replay and parking coverage. | Proves replay resets durable row before republish and unsafe payloads park. |
| `Event.Persistence.IntegrationTests/Repositories/EmailDispatchOutboxTransitionRepositoryTests.cs` | Existing | Persistence tests | PostgreSQL outbox transition and concurrency proof. | Extend only if state-machine gaps are found. |
| `Event.API.IntegrationTests/Features/EmailDispatchAdminControllerTests.cs` | Existing | API tests | Admin API operator actions. | Add Email category if relevant. |
| `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` | Existing | Architecture tests | Prevents direct side-effect calls in wrong layers. | Must remain green. |
| `Explore.Testing/*` | New candidate | Test support | Shared Mailpit/RabbitMQ/polling helpers if reused across projects. | Create only if reuse justifies it. |

## Key Decisions
| Decision | Reason |
|---|---|
| Keep TUnit as the only test framework. | Repo is standardized on TUnit and architecture docs enforce TUnit filter conventions. |
| Use Mailpit for runtime email assertions. | It validates real SMTP/MailKit behavior without sending external email. |
| Treat RabbitMQ as optional pointer transport. | Current architecture keeps PostgreSQL `EmailDispatchOutbox` as source of truth and RabbitMQ payloads pointer-only. |
| Add shared fixtures only after real reuse. | Avoid hiding scenario intent behind premature abstractions. |
| Start runtime email/RabbitMQ lanes as manual/nightly. | Docker/Aspire/browser/broker tests need reliability evidence before becoming required gates. |
| Delete/rewrite obsolete compatibility tests. | User explicitly stated development mode and no backward compatibility concern. |

## Constraints And Rules To Remember
- Repositories return entities, never DTOs.
- Application handlers must not send SMTP, publish RabbitMQ, or call TickerQ directly.
- Integration/E2E tests use real infrastructure; mocks are for unit tests.
- TUnit uses `--treenode-filter`.
- Disabled tests need `Category:` and `Removal:` in `[Skip]`.
- New C# files need two `ABOUTME:` lines.
- BFF tests must keep tokens server-side.
- HAL links gate UI actions.
- No backward-compatibility-only tests for obsolete behavior.

## Validation Baseline
Use project-level commands, not solution-level `dotnet test`:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

Full final verification should include every test project listed in `docs/TESTING.md` unless the implementation context records a concrete blocker.

## Current Known Risks / Unknowns
- TickerQ runtime timing may be flaky; prefer deterministic job/drain calls for PR and runtime polling for manual/nightly.
- RabbitMQ live tests may require adding `Testcontainers.RabbitMq` and package lock updates.
- E2E flow should not over-assert localized copy; assert semantic email content and links.
- Existing paused SMTP plan is old; use it as history only.
- Runtime artifacts must avoid leaking full email bodies or secrets.

## Handoff Notes

### Handoff - 2026-07-04 Europe/Brussels
- **Current state:** Planning docs created. No implementation files or tests changed.
- **Next action:** Review plan, then begin Phase 0 baseline and taxonomy audit.
- **Blockers:** None.
- **Modified files:** `dev/active/improved-overall-testing/improved-overall-testing-plan.md`, `dev/active/improved-overall-testing/improved-overall-testing-context.md`, `dev/active/improved-overall-testing/improved-overall-testing-tasks.md`.
- **Validation:** Pending lightweight doc verification after files are written.
- **Documentation impact:** This workstream will later update `docs/TESTING.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/OPERATIONS.md`, `docs/CONTRIBUTING.md`, and possibly workflows.
- **Risks:** Runtime test determinism and CI budget are the main risks.
- **Notes for next contributor/agent:** Do not implement all email paths in E2E first. Start with deterministic infrastructure/API tests, then add Playwright proof.

### Implementation Update - 2026-07-04 Europe/Brussels
- **Current state:** Implementation started. Baseline Release build is green in the shared worktree. Category constants now exist for API, infrastructure, BFF, and E2E test suites. Existing email/RabbitMQ decision tests and Aspire-backed browser flows are tagged so focused TUnit filters can find them.
- **Context7 refresh:** Consulted current docs for `/thomhurst/tunit`, `/testcontainers/testcontainers-dotnet`, and `/microsoft/playwright-dotnet`. The implementation uses TUnit `[Category]`, class-level metadata, `--treenode-filter`, async fixture patterns, and bounded polling instead of sleeps.
- **Known caveat:** The broad Codegraph exploration query timed out; source inspection continued through targeted local reads.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `Event.Architecture.Tests` passed 240 total / 239 succeeded / 1 intentional skip. `Explore.Infrastructure.Tests` Email filter passed 28/28, RabbitMQ filter passed 34/34, and `Event.API.IntegrationTests` Email filter passed 23/23 using `/*/*/*/*[Category=...]`.
- **Next action:** Run focused Email/RabbitMQ/E2E category checks and architecture tests, then continue Phase 0.3 obsolete-test review.

### Implementation Update - 2026-07-04 Phase 0.3
- **Current state:** Removed one compatibility-only Blazor client service alias: `ILocationService.GetLocations()` and its test. The canonical `GetAllLocationsAsync()` path remains covered.
- **Audit record:** Added `obsolete-test-audit.md` with retained dispositions for migration endpoint retirement, retired outbox event fail-closed behavior, deployment configuration mapping, authorization bridge tests, HAL member affordance tests, and active tag service usage.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with 25 projects and 0 errors. `Explore.Blazor.Client.Tests` passed 1456 total / 1455 succeeded / 1 intentional skip. `Event.Architecture.Tests` passed 240 total / 239 succeeded / 1 intentional skip. `git diff --check` passed.
- **Next action:** Start Phase 1.1 shared helper decision.

### Implementation Update - 2026-07-04 Phase 1.1
- **Decision:** Do not create `Explore.Testing` yet. Keep Mailpit/RabbitMQ helpers local until at least two test projects need the same stable helper API. This follows the plan's "shared helpers only after real reuse" rule and avoids solution/package churn before the SMTP and RabbitMQ live tests prove their exact needs.
- **Rationale:** `MailpitContainerFixture` currently has one concrete consumer in `Explore.Blazor.Client.E2ETests`. `Explore.Infrastructure.Tests` can add its first SMTP-to-Mailpit proof with a local fixture or a deliberately small copied helper; extract to shared test support only if Phase 2 duplicates clear/poll/message-detail behavior across infrastructure and E2E. RabbitMQ live tests belong in `Explore.Infrastructure.Tests` first and do not yet need cross-project reuse.
- **Constraint:** No production project may reference test helpers. If `Explore.Testing` is later created, only test projects should reference it, and architecture tests should prove that boundary.
- **Next action:** Phase 1.2/2.1: harden or add the local Mailpit helper needed by `Explore.Infrastructure.Tests`, then prove `SmtpEmailService` sends to Mailpit.

### Implementation Update - 2026-07-04 Phase 1.2 / 2.1 / 2.2
- **Current state:** `Explore.Infrastructure.Tests` now has a local `MailpitContainerFixture` using Testcontainers image `axllent/mailpit:v1.30.0`, random host ports, bounded startup polling, message clear, message summary polling, and text/html detail retrieval. The fixture keeps diagnostic failure output bounded to message counts and does not print raw email bodies or SMTP secrets.
- **SMTP proof:** `SmtpEmailServiceMailpitTests` constructs the real `SmtpEmailService` with a test `ISmtpConfigResolver`, `SmtpSecurityMode.None`, and Mailpit's mapped SMTP endpoint. It verifies `SendAsync` returns success and Mailpit receives the expected recipient, sender, unique subject, text body, and HTML body. It also verifies `TestConnectionAsync` succeeds against Mailpit.
- **Configuration failure proof:** `SmtpEmailServiceConfigurationTests` covers missing SMTP config for `SendAsync` and `TestConnectionAsync` without starting Docker. These tests prove failure occurs before provider handoff and returns the existing safe "SMTP is not configured" message.
- **CI/docs:** `_build-test.yml` fast infrastructure step now excludes `Runtime`; integration-enabled callers run the infrastructure `Email` category and publish a dedicated TRX. `docs/TESTING.md`, `docs/CI_CD_GOVERNANCE.md`, `docs/OPERATIONS.md`, and `docs/RELEASE_CHECKLIST.md` describe the split.
- **Verification:** `dotnet restore --locked-mode` passed with existing advisory warnings. `dotnet build --configuration Release --verbosity quiet` passed. `Explore.Infrastructure.Tests` passed 642/642. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 640/640. `Explore.Infrastructure.Tests` `Category=Email` passed 32/32 and started Mailpit. `Event.Architecture.Tests` passed 240 total / 239 succeeded / 1 intentional skip. `git diff --check`, `validate-action-pins`, and `validate-workflow-cache-policy` passed. Earlier in the slice, the same Email filter passed 31/31 before the missing-connection-config test was added.
- **Additional fix:** `AdminContextTests.CreatePrincipal` now emits `internal_user_id`, matching `AdminContext`'s first-party user resolution contract. This fixed two no-runtime infrastructure failures discovered while validating the filter.
- **Next action:** Phase 2.3: extend `SmtpConfigResolverTests` for system default, tenant override, missing required settings, and cache invalidation behavior.

### Implementation Update - 2026-07-04 Phase 2.3
- **Current state:** `SmtpConfigResolverTests` is now tagged with `InfrastructureTestCategories.Email` so SMTP settings resolution is discoverable through the focused Email lane.
- **Config precedence proof:** Added `ResolveAsync_UsesTenantContextForSettingsCascade` to verify the resolver passes the active tenant id into `IHierarchicalSettingsResolver` for governance and secret settings. Added `ResolveAsync_WhenTenantChanges_UsesSeparateCacheEntry` to simulate a system default for one tenant and a tenant override for another tenant, then prove the resolver caches those configurations separately and does not bleed host values across tenants.
- **Existing retained coverage:** Missing host, missing from-address, invalid/zero defaulting, security-mode parsing, and tenant-specific cache invalidation remain covered in the same class.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `SmtpConfigResolverTests` class filter passed 13/13. `Explore.Infrastructure.Tests` passed 644/644. After tagging the resolver class, the `Explore.Infrastructure.Tests` Email filter passed 45/45.
- **Next action:** Phase 2.4: review SMTP tests and fixture diagnostics for secret/body leakage, then add focused assertions if needed.

### Implementation Update - 2026-07-04 Phase 2.4
- **Current state:** `SmtpEmailServiceMailpitTests.SendAsync_WithMailpitSmtpConfig_DeliversMessageToMailpit` now uses unique sentinel body text and an unused sentinel SMTP password value. It still proves the body reaches Mailpit, but also asserts the `EmailResult` success/error fields do not echo either sentinel.
- **Fixture review:** `MailpitContainerFixture` diagnostic timeout output remains bounded to message count. It does not include connection strings, raw message bodies, recipients, or secrets in its timeout exception.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `Explore.Infrastructure.Tests` Email filter passed 45/45 with Mailpit/Testcontainers.
- **Next action:** Phase 3.1: add deterministic Basic EmailDispatch drain-to-Mailpit coverage through `EmailDispatchDrainService`, not through scheduler timing.

### Implementation Update - 2026-07-04 Phase 3.1
- **Current state:** Added `EmailDispatchDrainMailpitTests` with `MailpitContainerFixture`. The test builds a real `EmailDispatchDrainService`, a real `SmtpEmailService`, and a stateful in-memory `IEmailDispatchOutboxRepository` that mutates an `EmailDispatchOutbox`, `EmailDispatchAttempt`, and `EmailDispatchReceipt` like the success path requires.
- **Drain proof:** `ProcessBatchAsync_WithPendingOutbox_SendsToMailpitAndPersistsSentState` starts from a pending outbox row, runs `ProcessBatchAsync`, asserts `Pending=1`, `Processed=1`, `Sent=1`, verifies the row becomes `Sent` with `SentAt` and provider message id, verifies one succeeded attempt and one completed receipt, verifies tenant context set/clear, then polls Mailpit for the expected recipient/subject/text/html body.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `Explore.Infrastructure.Tests` Email filter passed 46/46 with Mailpit/Testcontainers.
- **Next action:** Phase 3.2: prove duplicate claims do not duplicate email by racing two drain calls for the same outbox row.

### Implementation Update - 2026-07-04 Phase 3.2
- **Current state:** `EmailDispatchDrainMailpitTests` now includes `ProcessSingleAsync_WhenDuplicateConsumersRace_SendsOneMailpitMessageAndKeepsSingleSentReceipt`. The stateful repository double supports tenant/publish-event lookup and lock-protected claim, receipt, attempt, and sent transitions.
- **Duplicate proof:** The test runs two `ProcessSingleAsync` calls concurrently for the same outbox row. It asserts one call sends, the other observes already-claimed or already-settled state, the row ends `Sent`, one attempt exists, one receipt is completed, and Mailpit contains exactly one message with the unique subject.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `Explore.Infrastructure.Tests` Email filter passed 47/47 with Mailpit/Testcontainers.
- **Next action:** Phase 3.3: cover retry/dead-letter/unknown failure paths and sanitized persistence.

### Implementation Update - 2026-07-04 Phase 3.3
- **Current state:** `EmailDispatchDrainServiceTests` now covers retry-scheduled, dead-lettered, and unknown outcomes at the scheduler-neutral drain boundary.
- **Failure proof:** Existing `ProcessSingleAsyncPersistsExpectedSmtpFailureWithoutThrowing` still proves a normal SMTP failure records a failed attempt and schedules retry. Added `ProcessSingleAsyncDeadLettersWhenRetryBudgetIsExhausted` to prove exhausted attempts return `DeadLettered` and persist `smtp_send_failed` classification through `MarkAsFailed`. Added `ProcessSingleAsyncMarksTimeoutLikeFailureUnknown` to prove timeout-like provider failures return `Unknown`, record an unknown attempt, mark the outbox unknown, and mark the receipt failed with `smtp_outcome_unknown`.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `EmailDispatchDrainServiceTests` class filter passed 12/12. `Explore.Infrastructure.Tests` Email filter passed 49/49.
- **Next action:** Phase 3.4: tenant pause and replay behavior.

### Implementation Update - 2026-07-04 Phase 3.4
- **Current state:** Existing tests already cover the Phase 3.4 acceptance criteria. No duplicate tests were added.
- **Tenant pause proof:** `EmailDispatchDrainServiceTests.ProcessSingleAsyncReturnsTenantPausedBeforePreferenceLookupOrSend` proves a paused tenant returns `TenantPaused` before processing claim, preference lookup, or SMTP send.
- **Persistence proof:** `EmailDispatchTenantControlRepositoryTests.SetTenantPauseStateCreatesAndUpdatesSingleTenantControlRow` proves PostgreSQL pause/resume stores one durable tenant control row and `IsTenantPaused` follows the current state.
- **API/HAL proof:** `EmailDispatchAdminControllerTests` cover protected routes, pause/resume validation problem mapping, replay conflict/misconfiguration mapping, stable route names, write policies, and advertised problem details. `EmailDispatchAdminHateoasTests` cover replay/park affordances by row state.
- **Validation:** Focused infrastructure pause test passed 1/1. Focused PostgreSQL tenant-control test passed 1/1. Focused API/HAL EmailDispatch admin tests passed 10/10.
- **Next action:** Phase 3.5: document the Basic Dispatch matrix and commands in `docs/EMAIL_NOTIFICATIONS.md`.

### Implementation Update - 2026-07-04 Phase 3.5
- **Current state:** `docs/EMAIL_NOTIFICATIONS.md` now has a Basic Dispatch Test Evidence section with exact source files and commands. `docs/TESTING.md` already has the broader Email and Messaging Scenario Matrix and now names Basic Dispatch drain, duplicate consumer, retry/dead-letter/unknown, tenant pause, API, and HAL evidence.
- **Validation:** Documentation quality will be covered by the next `Event.Architecture.Tests` run.
- **Next action:** Phase 4.1: verify TickerQ wrapper tests.

### Implementation Update - 2026-07-04 Phase 4.1
- **Current state:** `EmailDispatchTickerQJobsTests` now has six tests. Existing tests already proved `EmailDispatchTickerQJobs` delegates drain/recovery to `IEmailDispatchDrainService`, bubbles unexpected drain failures to TickerQ, and `EventLifecycleTickerQJobs` delegates pointer-only event reminder dispatch or skips unsupported use cases.
- **Added coverage:** `DispatchEventReminderAsyncSkipsWhenPointerContextIsMissing` proves a null TickerQ pointer context is ignored without calling `ProcessSingleAsync`.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `EmailDispatchTickerQJobsTests` class filter passed 6/6. `Event.API.IntegrationTests` Email filter passed 24/24.
- **Next action:** Phase 4.2: hosted-service fallback deterministic test.

### Implementation Update - 2026-07-04 Phase 4.2
- **Current state:** `EmailDispatchProcessor` now delegates each hosted-service fallback cycle to `EmailDispatchHostedDrainRunner`. The runner creates a DI scope, resolves `IEmailDispatchDrainService`, runs `RecoverStaleProcessingAsync`, then runs `ProcessBatchAsync`.
- **Added coverage:** `EmailDispatchProcessorTests.RunOnceAsync_WhenHostedFallbackRuns_RecoversBeforeDrainingThroughSharedService` builds a minimal provider with only the drain boundary and processor options, then verifies recovery-before-drain ordering, scoped resolution, `BatchSize`, `PollingIntervalSeconds`, `ConsumerId`, and cancellation-token propagation.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now name `EmailDispatchProcessorTests` alongside TickerQ trigger coverage and include the focused scheduler-trigger command.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. Focused `EmailDispatchProcessorTests` filter passed 1/1. `Event.API.IntegrationTests` Email category passed 25/25.
- **Next action:** Phase 4.3: verify EmailDispatch readiness states for mode combinations.

### Implementation Update - 2026-07-04 Phase 4.3
- **Current state:** Basic Dispatch readiness now covers all configured scheduler states required by the plan, and optional RabbitMQ readiness now covers disabled, healthy-enabled, and unhealthy transport states.
- **Added coverage:** `EmailDispatchHealthCheckTests.CheckHealthAsyncWhenSchedulerModeDisabledReturnsDegraded` proves `EmailDispatchProcessor:Mode=Disabled` is degraded even when the processor is otherwise enabled. `EmailDispatchRabbitMqHealthCheckTests.CheckHealthAsyncWhenTransportHealthyReturnsHealthy` proves enabled RabbitMQ health data is surfaced as healthy without requiring a live broker.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now name the readiness tests and focused commands.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. `EmailDispatchHealthCheckTests` passed 5/5. `EmailDispatchRabbitMqHealthCheckTests` passed 5/5. `Event.API.IntegrationTests` Email category passed 26/26. `Explore.Infrastructure.Tests` RabbitMQ category passed 35/35.
- **Next action:** Phase 1.3: add a RabbitMQ Testcontainers fixture for live broker tests.

### Implementation Update - 2026-07-04 Phase 1.3 / 1.4
- **Current state:** `Explore.Infrastructure.Tests` now references `Testcontainers.RabbitMq` 4.10.0 and has a local `RabbitMqContainerFixture` using `rabbitmq:4-management`. The fixture exposes an AMQP URI for `EmailDispatchRabbitMq:ConnectionString`, host/port accessors, and bounded management API overview diagnostics.
- **Bounded polling:** `RabbitMqContainerFixture` waits for `/api/overview` with a two-minute startup timeout and 250 ms polling. Together with the existing Mailpit fixture, runtime tests now use bounded readiness/message polling instead of blind sleeps.
- **Added coverage:** `RabbitMqContainerFixtureTests.InitializeAsync_WithRabbitMqContainer_ExposesAmqpAndManagementDiagnostics` starts the real broker and verifies AMQP plus management diagnostics before Phase 5 live transport tests depend on it.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now name the RabbitMQ fixture smoke test.
- **Validation:** `dotnet restore Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --use-lock-file` passed and updated the lock file. `dotnet build --configuration Release --verbosity quiet` passed. Focused fixture test passed 1/1. `Explore.Infrastructure.Tests` RabbitMQ category passed 36/36. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 645/645, confirming the Docker-backed fixture remains out of the fast lane.
- **Next action:** Phase 5.1: prove live topology declaration and enabled health against the RabbitMQ fixture.

### Implementation Update - 2026-07-04 Phase 5.1
- **Current state:** `RabbitMqEmailDispatchTransportLiveTests` now drives the real `RabbitMqEmailDispatchTransport` against `RabbitMqContainerFixture` with unique per-test topology names.
- **Added coverage:** `DeclareTopologyAsync_WithEnabledRabbitMq_DeclaresDispatchDeadLetterAndParkingTopology` calls `DeclareTopologyAsync`, then `CheckHealthAsync`, and verifies through the RabbitMQ management API that dispatch/DLX exchanges are durable direct exchanges, dispatch/dead-letter/parking queues are durable, dispatch queue dead-letter arguments point at the DLX/routing key, and all required bindings exist.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now name the live topology test and focused command.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. Focused live transport test passed 1/1. `Explore.Infrastructure.Tests` RabbitMQ category passed 37/37. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 645/645.
- **Next action:** Phase 5.2: add live publish-confirm and mandatory-return coverage.

### Implementation Update - 2026-07-04 Phase 5.2
- **Current state:** `RabbitMqEmailDispatchTransportLiveTests` now covers live publish outcomes in addition to topology and health.
- **Added coverage:** `PublishDispatchPointerAsync_WithBoundRoutingKey_ReturnsConfirmed` proves a pointer routed to the dispatch queue returns `Confirmed`, success, and a publish sequence number. `PublishDispatchPointerAsync_WithUnroutableMandatoryMessage_ReturnsReturned` uses a sequence-aware options monitor so topology is declared for one routing key and the mandatory publish uses an unbound key; the real broker returns it and the transport reports `Returned` with `mandatory_return`.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now describe confirmed and mandatory-return broker evidence.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. Live transport class filter passed 3/3. `Explore.Infrastructure.Tests` RabbitMQ category passed 39/39. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 645/645.
- **Next action:** Phase 5.3: assert live broker pointer payload privacy.

### Implementation Update - 2026-07-04 Phase 5.3
- **Current state:** `RabbitMqEmailDispatchTransportLiveTests` now reads the actual RabbitMQ queue payload after a confirmed publish.
- **Added coverage:** `PublishDispatchPointerAsync_WithSensitiveOutboxSnapshot_PublishesPointerOnlyPayload` creates an `EmailDispatchOutbox` with sentinel recipient, subject, plain-text body, HTML body, reply-to, provider message id, and raw error/password text. It publishes `EmailDispatchPointer.FromOutbox`, reads one message from the live queue through the management API, and asserts pointer ids/source fields are present while sensitive values and field names are absent.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now describe live pointer payload privacy evidence.
- **Validation:** `dotnet build --configuration Release --verbosity quiet` passed. Live transport class filter passed 4/4. `Explore.Infrastructure.Tests` RabbitMQ category passed 40/40. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 645/645.
- **Next action:** Phase 5.4: live consumer drains to Mailpit and ACKs after durable outcome.

### Implementation Update - 2026-07-04 Phase 5.4
- **Current state:** `RabbitMqEmailDispatchConsumerMailpitTests` drives the real manual-ack consumer against Testcontainers RabbitMQ and Mailpit.
- **Added coverage:** `Consumer_WithValidPointer_DrainsToMailpitAndAcksAfterDurableOutcome` starts `EmailDispatchRabbitMqConsumerService`, publishes a valid pointer, resolves the actual `EmailDispatchDrainService` from a scoped provider, sends through real `SmtpEmailService` to Mailpit, persists `Sent` outbox state plus succeeded attempt/completed receipt state, verifies tenant context set/clear, and waits for RabbitMQ ready/unacknowledged counters to reach zero after delivery.
- **Shared helper:** The previous nested in-memory outbox repository from `EmailDispatchDrainMailpitTests` is now `Fixtures/InMemoryEmailDispatchOutboxRepository.cs` so both Mailpit drain and RabbitMQ consumer runtime tests exercise the same success-path durable-state double.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now name the live consumer/Mailpit test and focused command.
- **Validation:** `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed. Focused consumer test passed 1/1. `Explore.Infrastructure.Tests` RabbitMQ category passed 41/41. `Explore.Infrastructure.Tests` Email category passed 50/50. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 645/645. Full `dotnet build --configuration Release --verbosity quiet` is currently blocked by unrelated `SupportAccessFlowTests.cs` missing `PlatformDefaults`.
- **Next action:** Phase 5.5: live malformed/missing pointer DLQ path.

### Implementation Update - 2026-07-04 Phase 5.5
- **Current state:** `RabbitMqEmailDispatchConsumerMailpitTests` now covers valid, malformed, and missing-outbox consumer paths.
- **Added coverage:** `Consumer_WithMalformedPointer_RejectsToDeadLetterQueueWithoutSendingMail` publishes raw invalid JSON through the RabbitMQ management API, waits for the DLQ payload, and proves no Mailpit message or drain attempt occurs. `Consumer_WithMissingOutbox_RejectsToDeadLetterQueueWithoutSendingMail` publishes a valid pointer with no matching durable outbox row, waits for the DLQ payload containing the pointer id, and proves no Mailpit message or unrelated repository mutation occurs.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now describe live malformed/missing pointer DLQ evidence.
- **Validation:** `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed. Consumer class filter passed 3/3. `Explore.Infrastructure.Tests` RabbitMQ category passed 43/43. `Explore.Infrastructure.Tests` Email category passed 52/52. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 645/645.
- **Next action:** Phase 5.6: live DLQ replay and parking.

### Implementation Update - 2026-07-04 Phase 5.6
- **Current state:** The RabbitMQ live runtime suite now covers topology, health, confirmed publish, mandatory return, pointer payload privacy, valid consumer drain, malformed/missing DLQ, and DLQ replay/parking.
- **Added coverage:** `RabbitMqEmailDispatchDeadLetterReplayLiveTests.ReplayWorker_WithDeadLetteredOutbox_ResetsDurableRowAndRepublishesPointer` publishes a DLQ pointer for a dead-lettered durable row, starts `EmailDispatchRabbitMqDeadLetterReplayService`, verifies the row resets to `Pending`, verifies one replay reset, verifies the pointer is republished to the dispatch queue, and verifies the original DLQ message settles. `ReplayWorker_WithMissingOutbox_ParksPayloadAndAcksDeadLetter` verifies an unsafe DLQ payload is published to the parking queue and the original DLQ delivery settles without durable replay.
- **Docs:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now describe live DLQ replay/parking evidence and commands.
- **Validation:** `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed. Replay class filter passed 2/2. `Explore.Infrastructure.Tests` RabbitMQ category passed 45/45. `Explore.Infrastructure.Tests` `Category!=Runtime` passed 645/645.
- **Next action:** Phase 6.1: add Mailpit assertion to the Aspire-backed registration E2E.

### Implementation Update - 2026-07-04 Phase 6.1
- **Current state:** No code change was needed. `RegistrationFlowTests` already clears Mailpit, registers through the Aspire-backed API/BFF/browser fixture, waits for `EmailDispatchOutbox` to reach `Sent`, verifies succeeded attempt and completed receipt rows, locates the Mailpit message by registrant and event title, and asserts semantic body text plus event title.
- **Docs:** `docs/EMAIL_NOTIFICATIONS.md` now names the browser registration email proof and focused command.
- **Validation:** Focused no-build E2E registration filter passed 1/1 in 1m 44s. The E2E project build later passed after the source moved past the earlier `SupportAccessFlowTests.cs` compile blocker.
- **Next action:** Phase 6.2: E2E category consistency audit.

### Implementation Update - 2026-07-04 Phase 6.2
- **Current state:** Added missing E2E categories to `AuthorizationEnforcementFlowTests`, `BffTokenForwardingChainFlowTests`, and `TenantIsolationFlowTests`. `SidebarLayoutVisualTests` now carries both `E2E` and `Manual`, so nightly/manual browser filtering can select it intentionally.
- **Docs:** `docs/TESTING.md` now states that all flow test classes carry `E2E`, registration email also carries `Email`, and visual/manual browser tests carry `Manual`.
- **Validation:** `rg --files-without-match "\\[Category\\(E2ETestCategories\\.E2E\\)\\]" Explore.Blazor.Client.E2ETests/Flows -g '*Tests.cs'` returned no files. `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet /clp:ErrorsOnly` passed with 13 projects, 0 errors.
- **Next action:** Phase 6.3: assess whether Blazor admin SMTP/UI tests are needed for this implementation.

### Implementation Update - 2026-07-04 Phase 6.3
- **Current state:** No new bUnit tests were added. This implementation did not modify the Blazor admin SMTP components, so SMTP/admin behavior is covered at the lower infrastructure/API layers and by the existing admin settings tests.
- **Docs:** No user-facing docs needed for this no-op UI slice beyond task/context status.
- **Validation:** `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed 1469 total / 1468 succeeded / 1 intentional skip.
- **Next action:** Phase 6.4: verify BFF token-boundary coverage.

### Implementation Update - 2026-07-04 Phase 6.4
- **Current state:** Loaded the project Blazor BFF guidance and verified that email/admin work still keeps bearer tokens server-side. Existing coverage exercises `AccessTokenForwardingHandler` for cookie/circuit token forwarding and `BffProxyHeaderSanitizer` for stripping browser-controlled credential and tenant headers.
- **Docs:** No new docs required in this slice; Phase 7 will consolidate BFF/E2E lane documentation.
- **Validation:** `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed 187/187.
- **Next action:** Phase 7.1: update final testing documentation.

### Implementation Update - 2026-07-04 Phase 7
- **Current state:** Updated the final testing matrix and contributor-facing commands. `docs/TESTING.md` now documents the Email/RabbitMQ scenario matrix, `Category!=Runtime` fast lane, focused runtime categories, current E2E/Manual category policy, and expected TRX/runtime artifacts. `docs/EMAIL_NOTIFICATIONS.md`, `docs/OPERATIONS.md`, and `docs/TROUBLESHOOTING.md` now explain Mailpit, Basic Dispatch, RabbitMQ Dispatch, HAL-gated replay/park operations, and focused verification commands. `docs/CONTRIBUTING.md` and `docs/GETTING_STARTED.md` now include the fast infrastructure lane, focused Email/RabbitMQ lanes, and Mailpit/Aspire expectations.
- **CI:** `_build-test.yml` now runs both `Email` and `RabbitMQ` infrastructure runtime categories when integration tests are enabled, writes separate TRX files, captures command logs, captures Docker/container diagnostics, and includes both runtime outcomes in the evidence summary. The E2E workflow remains manual/nightly and already captures TRX, Playwright artifacts, test logs, and Docker logs.
- **Validation:** Pending final architecture/docs/build verification.
- **Next action:** Final verification checklist and implementation summary.

### Final Verification - 2026-07-04
- **Build/static checks:** `dotnet build --configuration Release --verbosity quiet` passed with 26 projects, 0 errors, and existing package advisory warnings. `git diff --check` passed. Workflow YAML parse passed for `.github/workflows/_build-test.yml` and `.github/workflows/e2e.yml`.
- **Unit and architecture suites:** `Event.Domain.UnitTests` passed 313/313. `Event.Application.UnitTests` passed 1955/1955. `Explore.Secrets.UnitTests` passed 202/202. `Event.Architecture.Tests` passed 243 total / 242 succeeded / 1 intentional skip.
- **Infrastructure/runtime lanes:** `Explore.Infrastructure.Tests` fast lane with `Category!=Runtime` passed 645/645. Runtime Email category passed 52/52. Runtime RabbitMQ category passed 45/45, including live transport, consumer-to-Mailpit, and DLQ replay/parking coverage.
- **Integration suites:** `Event.Persistence.IntegrationTests` passed 226/226 after the repository include fix. `Explore.Blazor.IntegrationTests` passed 187/187. `Event.API.IntegrationTests` focused `Email` category passed 26/26.
- **Client and E2E suites:** `Explore.Blazor.Client.Tests` passed 1480 total / 1479 succeeded / 1 intentional skip. `Explore.Blazor.Client.E2ETests` project build passed, all flow classes carry the `E2E` category, and focused registration E2E passed 1/1 against Aspire/Mailpit earlier in the implementation.
- **Known unrelated blocker:** Full `Event.API.IntegrationTests` failed with 19 auth/storage/projection failures outside this email/testing workstream. Examples include storage HATEOAS tests returning `401 Unauthorized` where OK was expected, governance/admin endpoint tests returning `401 Unauthorized`, and `CustomPropertyProjectionAdminControllerTests.GetEventProjections_WithPublicCeiling_ExcludesInternalRowsAndMetadata` receiving an empty body where JSON was expected.

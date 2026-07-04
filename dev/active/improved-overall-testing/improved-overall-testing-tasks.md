<!-- ABOUTME: Task checklist for the improved overall testing implementation stream. -->
<!-- ABOUTME: Tracks phase completion, validation evidence, and remaining deferred work. -->

# Improved Overall Testing - Task Checklist

Last Updated: 2026-07-04 Europe/Brussels

## Status Summary
- **Overall status:** Implementation complete; final verification recorded with an unrelated full API-suite blocker.
- **Completed:** 36/36
- **Current priority:** Complete.
- **Next recommended slice:** Resolve unrelated full `Event.API.IntegrationTests` auth/storage/projection failures in the owning backend/API workstream.

## Implementation Maintenance Rules
- [x] Before starting work, read plan/context/tasks.
- [x] After each completed task, update this checklist immediately.
- [x] If implementation changes scope or architecture, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline
- [x] **0.1 User reviews and approves or corrects scope**
  - **Files:** `dev/active/improved-overall-testing/*`
  - **Acceptance:** Plan status changes from Draft to User-reviewed or Approved.
  - **Validation:** User instructed implementation on 2026-07-04; plan status changed to Implementation in progress.
  - **Effort:** S
  - **Dependencies:** none
- [x] **0.2 Capture current build/test baseline**
  - **Files:** `improved-overall-testing-context.md`
  - **Acceptance:** Context records commands run, failures, and whether failures are pre-existing.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with 25 projects, 0 errors, and existing package advisory warnings.
  - **Effort:** M
  - **Dependencies:** 0.1
- [x] **0.3 Audit obsolete/backward-compatibility-only tests**
  - **Files:** all test projects; start with tests that mention retired routes, old DTOs, aliases, or compatibility.
  - **Acceptance:** Obsolete tests are deleted or rewritten around desired current behavior; no commented `[Test]` markers.
  - **Validation:** Removed `LocationService.GetLocations()` compatibility alias/test and recorded retained candidates in `obsolete-test-audit.md`. `Explore.Blazor.Client.Tests` passed 1456 total / 1455 succeeded / 1 intentional skip. `Event.Architecture.Tests` passed 240 total / 239 succeeded / 1 intentional skip.
  - **Effort:** M
  - **Dependencies:** 0.2
- [x] **0.4 Standardize test categories**
  - **Files:** `Event.API.IntegrationTests/Fixtures/TestCategories.cs`, possible E2E/BFF category helpers, `docs/TESTING.md`
  - **Acceptance:** Email/RabbitMQ/runtime categories are named and documented; filters use `--treenode-filter`.
  - **Validation:** Passed `Explore.Infrastructure.Tests` Email filter 28/28, `Explore.Infrastructure.Tests` RabbitMQ filter 34/34, and `Event.API.IntegrationTests` Email filter 23/23 with `/*/*/*/*[Category=...]`. `Explore.Blazor.Client.E2ETests` remains manual/nightly because it starts Aspire-backed browser infrastructure.
  - **Effort:** M
  - **Dependencies:** 0.2

## Phase 1: Shared Fixture And Harness Foundation
- [x] **1.1 Decide shared helper location**
  - **Files:** possible new `Explore.Testing/Explore.Testing.csproj`, solution file, or local fixtures only.
  - **Acceptance:** Decision recorded in plan/context; no production project references test helpers.
  - **Validation:** Decision recorded in context: no `Explore.Testing` project yet; helpers stay local until stable reuse exists across at least two test projects.
  - **Effort:** S
  - **Dependencies:** 0.4
- [x] **1.2 Harden Mailpit helper**
  - **Files:** `Explore.Blazor.Client.E2ETests/Fixtures/MailpitContainerFixture.cs` or shared helper.
  - **Acceptance:** Supports clear, poll-until, message summary, text/html detail, bounded timeout, and diagnostic failure output.
  - **Validation:** Added local `Explore.Infrastructure.Tests/Fixtures/MailpitContainerFixture.cs`; `Explore.Infrastructure.Tests` Email filter passed 32/32 and started Mailpit through Testcontainers.
  - **Effort:** M
  - **Dependencies:** 1.1
- [x] **1.3 Add RabbitMQ Testcontainers fixture**
  - **Files:** `Directory.Packages.props`, relevant `packages.lock.json`, `Explore.Infrastructure.Tests/*`, optional shared helper.
  - **Acceptance:** Real broker starts with readiness checks; exposes AMQP URI and management diagnostics if available.
  - **Validation:** Added `Testcontainers.RabbitMq`, `RabbitMqContainerFixture`, and `RabbitMqContainerFixtureTests`. The fixture starts `rabbitmq:4-management`, exposes an AMQP connection string plus host/port, polls the management API with bounded timeout, and returns bounded overview diagnostics. Build passed; focused fixture test passed 1/1; infrastructure RabbitMQ category passed 36/36; fast infrastructure `Category!=Runtime` passed 645/645.
  - **Effort:** M
  - **Dependencies:** 1.1
- [x] **1.4 Add eventually/assertion helpers**
  - **Files:** test helper location from 1.1.
  - **Acceptance:** Runtime tests use bounded polling instead of blind sleeps; failures include useful diagnostics without secrets.
  - **Validation:** Local runtime fixtures now use bounded polling: `MailpitContainerFixture.WaitForMessageAsync` and `RabbitMqContainerFixture.WaitForManagementApiAsync`. Fixture diagnostics report counts/status only and avoid raw email bodies, connection strings, and credentials. Runtime fixture tests passed through the Email/RabbitMQ category runs.
  - **Effort:** S
  - **Dependencies:** 1.1

## Phase 2: SMTP And Config Integration Tests
- [x] **2.1 Test `SmtpEmailService` sends to Mailpit**
  - **Files:** `Explore.Infrastructure.Tests/Infrastructure/SmtpEmailServiceMailpitTests.cs` (new)
  - **Acceptance:** Send returns success; Mailpit receives one message with expected recipient/sender/subject/body.
  - **Validation:** `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1` passed 32/32.
  - **Effort:** M
  - **Dependencies:** 1.2
- [x] **2.2 Test SMTP connection success and failure**
  - **Files:** same as 2.1 or focused test file.
  - **Acceptance:** `TestConnectionAsync` succeeds for Mailpit and fails safely for unreachable/missing config.
  - **Validation:** `SmtpEmailServiceMailpitTests.TestConnectionAsync_WithMailpitSmtpConfig_ReturnsSuccess` and `SmtpEmailServiceConfigurationTests.TestConnectionAsync_WhenSmtpConfigMissing_ReturnsFailure` passed in the 32/32 Email category run.
  - **Effort:** S
  - **Dependencies:** 2.1
- [x] **2.3 Test SMTP config precedence and tenant override**
  - **Files:** `Explore.Infrastructure.Tests/*` and/or API integration tests depending on resolver dependencies.
  - **Acceptance:** System defaults, tenant override, and missing required settings resolve as documented.
  - **Validation:** `SmtpConfigResolverTests` now covers active `SettingContext` tenant propagation, tenant-specific cache separation, missing host/from-address behavior, and cache invalidation. The resolver class is tagged `Email`; `Explore.Infrastructure.Tests` Email filter passed 45/45.
  - **Effort:** M
  - **Dependencies:** 2.1
- [x] **2.4 Assert no secret leakage in SMTP test output/log paths**
  - **Files:** infrastructure tests; possible logging capture helper.
  - **Acceptance:** Test fixtures do not print passwords, full connection strings, raw provider errors, or full message bodies by default.
  - **Validation:** `SmtpEmailServiceMailpitTests` now sends sentinel body text and a sentinel SMTP password value, then asserts `EmailResult.Message` and `EmailResult.ErrorMessage` do not echo either value. `MailpitContainerFixture` timeout diagnostics remain bounded to message count. The `Explore.Infrastructure.Tests` Email filter passed 45/45.
  - **Effort:** S
  - **Dependencies:** 2.1

## Phase 3: Basic Email Dispatch With PostgreSQL And Mailpit
- [x] **3.1 Add deterministic outbox drain-to-Mailpit test**
  - **Files:** `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchDrainMailpitTests.cs` or API runtime test.
  - **Acceptance:** Pending outbox row is claimed, sent through Mailpit, and persisted as Sent with attempt/receipt rows.
  - **Validation:** Added `EmailDispatchDrainMailpitTests.ProcessBatchAsync_WithPendingOutbox_SendsToMailpitAndPersistsSentState`, which drives `EmailDispatchDrainService.ProcessBatchAsync`, sends through real `SmtpEmailService` to Mailpit, and asserts Sent outbox state plus succeeded attempt/completed receipt state. `Explore.Infrastructure.Tests` Email filter passed 46/46.
  - **Effort:** L
  - **Dependencies:** 2.1
- [x] **3.2 Prove duplicate claims do not duplicate email**
  - **Files:** persistence/infrastructure tests.
  - **Acceptance:** Concurrent claims result in one Mailpit message and one final durable owner.
  - **Validation:** Added `EmailDispatchDrainMailpitTests.ProcessSingleAsync_WhenDuplicateConsumersRace_SendsOneMailpitMessageAndKeepsSingleSentReceipt`, which races two `ProcessSingleAsync` calls for one outbox row and asserts one `Sent` result, one already-claimed/settled result, one attempt, one completed receipt, and one matching Mailpit message. `Explore.Infrastructure.Tests` Email filter passed 47/47.
  - **Effort:** M
  - **Dependencies:** 3.1
- [x] **3.3 Prove retry/dead-letter/unknown paths**
  - **Files:** `EmailDispatchDrainServiceTests.cs`, new integration tests if needed.
  - **Acceptance:** Expected SMTP failures persist retry/dead-letter states; timeout-like unknowns stay inspectable; no raw error leaks.
  - **Validation:** Added `ProcessSingleAsyncDeadLettersWhenRetryBudgetIsExhausted` and `ProcessSingleAsyncMarksTimeoutLikeFailureUnknown` to `EmailDispatchDrainServiceTests`, alongside the existing retry-scheduled failure test and stale-processing recovery test. Drain-service class filter passed 12/12; `Explore.Infrastructure.Tests` Email filter passed 49/49.
  - **Effort:** M
  - **Dependencies:** 3.1
- [x] **3.4 Prove tenant pause and replay behavior**
  - **Files:** `Event.Persistence.IntegrationTests/Repositories/EmailDispatchTenantControlRepositoryTests.cs`, API EmailDispatch admin tests.
  - **Acceptance:** Paused tenants do not send; resume/replay follows HAL/API state rules.
  - **Validation:** Existing coverage verified: `EmailDispatchDrainServiceTests.ProcessSingleAsyncReturnsTenantPausedBeforePreferenceLookupOrSend` passed 1/1, `EmailDispatchTenantControlRepositoryTests` passed 1/1 against PostgreSQL, and focused API/HAL `EmailDispatchAdminControllerTests` + `EmailDispatchAdminHateoasTests` passed 10/10.
  - **Effort:** M
  - **Dependencies:** 3.1
- [x] **3.5 Document Basic Dispatch test matrix**
  - **Files:** `docs/TESTING.md`, `docs/EMAIL_NOTIFICATIONS.md`
  - **Acceptance:** Docs name exact test files and commands for Basic Dispatch.
  - **Validation:** `docs/TESTING.md` and `docs/EMAIL_NOTIFICATIONS.md` now name `SmtpEmailServiceMailpitTests`, `SmtpConfigResolverTests`, `EmailDispatchDrainMailpitTests`, `EmailDispatchDrainServiceTests`, `EmailDispatchTenantControlRepositoryTests`, `EmailDispatchAdminControllerTests`, and `EmailDispatchAdminHateoasTests` with focused commands.
  - **Effort:** S
  - **Dependencies:** 3.1-3.4

## Phase 4: TickerQ And HostedService Trigger Coverage
- [x] **4.1 Extend TickerQ wrapper tests where needed**
  - **Files:** `Event.API.IntegrationTests/Features/EmailDispatchTickerQJobsTests.cs`
  - **Acceptance:** Jobs remain payload-free and delegate to shared drain/recovery methods.
  - **Validation:** Existing tests already covered drain/recovery delegation, pointer dispatch, unsupported use-case skip, and exception bubbling. Added `DispatchEventReminderAsyncSkipsWhenPointerContextIsMissing` for the null pointer branch. TickerQ class filter passed 6/6; API Email filter passed 24/24.
  - **Effort:** S
  - **Dependencies:** 3.1
- [x] **4.2 Add hosted-service mode deterministic test**
  - **Files:** API or infrastructure tests around `EmailDispatchProcessor`.
  - **Acceptance:** HostedService mode uses same drain and respects polling/batch settings without direct SMTP in handlers.
  - **Validation:** Added `EmailDispatchHostedDrainRunner` and `EmailDispatchProcessorTests.RunOnceAsync_WhenHostedFallbackRuns_RecoversBeforeDrainingThroughSharedService`, which proves the hosted-service fallback cycle resolves `IEmailDispatchDrainService` from a scope, runs recovery before batch drain, and uses configured processor settings without SMTP/RabbitMQ dependencies. Build passed; focused processor filter passed 1/1; API Email category passed 25/25.
  - **Effort:** M
  - **Dependencies:** 3.1
- [x] **4.3 Verify readiness for mode combinations**
  - **Files:** `Event.API.IntegrationTests/Features/EmailDispatchHealthCheckTests.cs`
  - **Acceptance:** TickerQ enabled/disabled, HostedService, Disabled, and RabbitMQ disabled/enabled readiness states match docs.
  - **Validation:** Added Basic Dispatch `Mode=Disabled` readiness coverage to `EmailDispatchHealthCheckTests` and enabled-healthy RabbitMQ transport coverage to `EmailDispatchRabbitMqHealthCheckTests`. Build passed. Health-check class filters passed 5/5 each; API Email category passed 26/26; infrastructure RabbitMQ category passed 35/35.
  - **Effort:** M
  - **Dependencies:** 4.1

## Phase 5: RabbitMQ Live Transport
- [x] **5.1 Test live topology declaration and health**
  - **Files:** `Explore.Infrastructure.Tests/Infrastructure/RabbitMqEmailDispatchTransportLiveTests.cs` (new)
  - **Acceptance:** Enabled mode declares exchange/queues/DLX/parking; health succeeds; disabled mode does not open broker.
  - **Validation:** Added `RabbitMqEmailDispatchTransportLiveTests.DeclareTopologyAsync_WithEnabledRabbitMq_DeclaresDispatchDeadLetterAndParkingTopology`, which enables the real transport against `RabbitMqContainerFixture`, checks healthy enabled readiness, verifies durable direct dispatch and DLX exchanges, verifies durable dispatch/dead-letter/parking queues, verifies dispatch queue DLX arguments, and verifies dispatch/DLQ/parking bindings through RabbitMQ management API. Build passed; focused live test passed 1/1; infrastructure RabbitMQ category passed 37/37; fast `Category!=Runtime` passed 645/645.
  - **Effort:** M
  - **Dependencies:** 1.3
- [x] **5.2 Test publisher confirms and mandatory returns**
  - **Files:** same live transport tests.
  - **Acceptance:** Confirmed publish records success; unroutable publish records failure category without changing SMTP delivery state.
  - **Validation:** Extended `RabbitMqEmailDispatchTransportLiveTests` with `PublishDispatchPointerAsync_WithBoundRoutingKey_ReturnsConfirmed` and `PublishDispatchPointerAsync_WithUnroutableMandatoryMessage_ReturnsReturned`. The confirmed test publishes a pointer through a bound routing key and asserts `Confirmed`, success, and a sequence number. The return test declares topology with one key, publishes mandatory through an unbound key, and asserts `Returned` with `mandatory_return`; the transport remains pointer-only and does not touch SMTP/outbox delivery state. Build passed; live transport class filter passed 3/3; infrastructure RabbitMQ category passed 39/39; fast `Category!=Runtime` passed 645/645.
  - **Effort:** M
  - **Dependencies:** 5.1
- [x] **5.3 Test pointer-only payload against real broker**
  - **Files:** live transport tests and existing `EmailDispatchPointerTests.cs`.
  - **Acceptance:** Broker message contains only pointer fields; no recipient, subject, body, SMTP credentials, provider IDs, or raw errors.
  - **Validation:** Added `PublishDispatchPointerAsync_WithSensitiveOutboxSnapshot_PublishesPointerOnlyPayload`, which creates an `EmailDispatchOutbox` containing sentinel recipient, subject, plain text body, HTML body, reply-to, provider message id, and raw error/password text; publishes `EmailDispatchPointer.FromOutbox`; reads the real RabbitMQ queue message through the management API; asserts pointer ids/source fields are present and sensitive values/field names are absent. Build passed; live transport class filter passed 4/4; infrastructure RabbitMQ category passed 40/40; fast `Category!=Runtime` passed 645/645.
  - **Effort:** M
  - **Dependencies:** 5.2
- [x] **5.4 Test consumer drains to Mailpit and ACKs after durable outcome**
  - **Files:** new runtime/integration tests around `EmailDispatchRabbitMqConsumerService`.
  - **Acceptance:** Valid pointer is consumed, outbox row becomes Sent, Mailpit receives one message, ACK happens after durable drain outcome.
  - **Validation:** Added `RabbitMqEmailDispatchConsumerMailpitTests.Consumer_WithValidPointer_DrainsToMailpitAndAcksAfterDurableOutcome`, which starts the real manual-ack consumer, publishes a valid pointer, runs the actual `EmailDispatchDrainService` through `SmtpEmailService` to Mailpit, asserts `Sent` outbox state plus succeeded attempt/completed receipt state, verifies tenant context set/clear, verifies Mailpit text/html delivery, and waits for RabbitMQ ready/unacknowledged counters to reach zero. Scoped infrastructure build passed. Focused consumer test passed 1/1; infrastructure RabbitMQ category passed 41/41; infrastructure Email category passed 50/50; fast `Category!=Runtime` passed 645/645. Full solution build is currently blocked by an unrelated `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/SupportAccessFlowTests.cs` missing `PlatformDefaults` compile error.
  - **Effort:** L
  - **Dependencies:** 3.1, 5.2
- [x] **5.5 Test malformed/missing pointer DLQ path**
  - **Files:** live RabbitMQ tests.
  - **Acceptance:** Malformed/missing pointer rejects to DLQ; no Mailpit send.
  - **Validation:** Extended `RabbitMqEmailDispatchConsumerMailpitTests` with malformed JSON and missing durable outbox cases. The malformed test publishes raw invalid JSON through the RabbitMQ management API, waits for the DLQ payload, asserts no Mailpit message, and verifies no drain attempt/tenant context occurred. The missing-outbox test publishes a valid pointer with no matching durable row, waits for the DLQ payload, asserts no Mailpit message, and verifies the unrelated repository row remains pending with no attempts. Infrastructure project build passed; consumer class filter passed 3/3; infrastructure RabbitMQ category passed 43/43; infrastructure Email category passed 52/52; fast `Category!=Runtime` passed 645/645.
  - **Effort:** M
  - **Dependencies:** 5.4
- [x] **5.6 Test DLQ replay and parking**
  - **Files:** live RabbitMQ tests around `EmailDispatchRabbitMqDeadLetterReplayService`.
  - **Acceptance:** Replayable rows republish safely; unsafe rows park; original DLQ message is ACKed only after durable replay/parking action.
  - **Validation:** Added `RabbitMqEmailDispatchDeadLetterReplayLiveTests` with live replay and parking coverage. `ReplayWorker_WithDeadLetteredOutbox_ResetsDurableRowAndRepublishesPointer` publishes a pointer to the DLQ, starts the replay worker, verifies the durable row resets from `DeadLettered` to `Pending`, verifies one replay reset, verifies the pointer appears on the dispatch queue, and verifies the original DLQ settles. `ReplayWorker_WithMissingOutbox_ParksPayloadAndAcksDeadLetter` verifies an unsafe payload is parked and the DLQ settles without durable replay. Infrastructure project build passed; replay class filter passed 2/2; infrastructure RabbitMQ category passed 45/45; fast `Category!=Runtime` passed 645/645.
  - **Effort:** L
  - **Dependencies:** 5.5

## Phase 6: Blazor/BFF/Playwright Email Journeys
- [x] **6.1 Add Mailpit assertion to registration E2E**
  - **Files:** `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/RegistrationFlowTests.cs`, fixtures as needed.
  - **Acceptance:** Flow clears Mailpit, registers, polls for confirmation email, asserts recipient/subject/body/link, and avoids duplicates.
  - **Validation:** Existing current source already satisfies this slice: `RegistrationFlowTests` clears Mailpit, drives the registration API path through the Aspire-backed fixture, waits for the durable outbox row to become `Sent`, verifies succeeded attempt/completed receipt rows, finds the Mailpit message by registrant and event title, and asserts semantic body text. Focused no-build E2E registration filter passed 1/1 in 1m 44s; the E2E project build later passed after the source moved past the earlier `SupportAccessFlowTests.cs` compile blocker.
  - **Effort:** L
  - **Dependencies:** 1.2, 3.1
- [x] **6.2 Ensure E2E critical flows are categorized consistently**
  - **Files:** E2E flow test files.
  - **Acceptance:** Critical browser flows that belong to nightly/manual E2E are tagged or documented.
  - **Validation:** Added `[Category(E2ETestCategories.E2E)]` to `AuthorizationEnforcementFlowTests`, `BffTokenForwardingChainFlowTests`, and `TenantIsolationFlowTests`; added `[Category(E2ETestCategories.E2E)]` plus `[Category(E2ETestCategories.Manual)]` to `SidebarLayoutVisualTests`. Source audit with `rg --files-without-match "\\[Category\\(E2ETestCategories\\.E2E\\)\\]" Explore.Blazor.Client.E2ETests/Flows -g '*Tests.cs'` returned no files, and `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet /clp:ErrorsOnly` passed with 13 projects, 0 errors.
  - **Effort:** S
  - **Dependencies:** 0.4
- [x] **6.3 Add Blazor admin SMTP/UI tests if UI changes are needed**
  - **Files:** `Explore.Blazor.Client.Tests/*`, admin SMTP components.
  - **Acceptance:** UI validation and action affordances are HAL/authorization-aware; no local role-only gating.
  - **Validation:** No new Blazor admin SMTP/UI tests were required because this implementation did not change the admin SMTP component surface; SMTP behavior was covered at infrastructure/API/E2E layers instead. Existing client coverage includes admin settings SMTP layout behavior and HAL-gated affordance patterns. `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed 1469 total / 1468 succeeded / 1 intentional skip.
  - **Effort:** M
  - **Dependencies:** 2.3
- [x] **6.4 Preserve BFF token boundary in email/admin flows**
  - **Files:** `Explore.Blazor.IntegrationTests/*`
  - **Acceptance:** New/changed admin flows do not expose bearer tokens to browser; YARP forwarding remains server-side.
  - **Validation:** Loaded the project BFF guidance and verified the existing server-side token boundary coverage. `AccessTokenForwardingHandlerTests` keeps bearer forwarding inside the BFF handler/circuit token store, while `BffProxyHeaderSanitizerTests` strips browser-controlled credential and tenant headers before proxying. `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed 187/187.
  - **Effort:** M
  - **Dependencies:** 6.3

## Phase 7: CI, Docs, And Operational Evidence
- [x] **7.1 Update `docs/TESTING.md`**
  - **Files:** `docs/TESTING.md`
  - **Acceptance:** Contains final email/RabbitMQ scenario matrix, commands, categories, lane policy, and artifact expectations.
  - **Validation:** `docs/TESTING.md` now documents the Email/RabbitMQ scenario matrix, `Category!=Runtime` fast lane, focused `Email` and `RabbitMQ` runtime commands, E2E/Manual category policy, and TRX/runtime artifact expectations.
  - **Effort:** M
  - **Dependencies:** Phases 2-6 as implemented
- [x] **7.2 Update email and operations docs**
  - **Files:** `docs/EMAIL_NOTIFICATIONS.md`, `docs/OPERATIONS.md`, possibly `docs/TROUBLESHOOTING.md`
  - **Acceptance:** Docs explain Mailpit, Basic Dispatch, RabbitMQ Dispatch, and troubleshooting based on test-proven behavior.
  - **Validation:** `docs/EMAIL_NOTIFICATIONS.md` names Mailpit, Basic Dispatch, RabbitMQ topology/publish/consumer/DLQ evidence, focused commands, and broker troubleshooting. `docs/OPERATIONS.md` now includes operational verification commands for Email, RabbitMQ, and API Email contracts. `docs/TROUBLESHOOTING.md` separates EmailDispatch issues from the general outbox processor path.
  - **Effort:** M
  - **Dependencies:** Phases 2-6 as implemented
- [x] **7.3 Update contributor docs**
  - **Files:** `docs/CONTRIBUTING.md`, `docs/GETTING_STARTED.md`
  - **Acceptance:** Contributors know fast commands, runtime commands, Aspire/Compose expectations, and Mailpit verification steps.
  - **Validation:** Contributor and getting-started docs now include `Explore.Infrastructure.Tests` with `Category!=Runtime`, focused Email/RabbitMQ runtime commands, current standard-project count, and Mailpit/Aspire local expectations.
  - **Effort:** S
  - **Dependencies:** 7.1
- [x] **7.4 Add or update runtime email workflow**
  - **Files:** `.github/workflows/e2e.yml`, optional `.github/workflows/email-runtime.yml`
  - **Acceptance:** Manual/nightly runtime lane collects TRX, Docker logs, Mailpit summaries, RabbitMQ diagnostics, Playwright artifacts, and does not leak secrets.
  - **Validation:** Existing E2E workflow already collects TRX, Playwright artifacts, test logs, and Docker logs. `_build-test.yml` now adds `Infrastructure RabbitMQ Runtime Tests`, writes `Explore.Infrastructure.Tests.RabbitMQ.trx`, captures runtime command logs, captures Docker/container diagnostics, and includes RabbitMQ in the integration evidence summary.
  - **Effort:** M
  - **Dependencies:** 5.4, 6.1
- [x] **7.5 Promote reliable tests to PR gate**
  - **Files:** `.github/workflows/_build-test.yml`
  - **Acceptance:** Deterministic fast/integration email tests are merge-blocking; flaky runtime tests stay advisory until stable.
  - **Validation:** Fast CI runs `Explore.Infrastructure.Tests` with `Category!=Runtime`; integration-enabled callers now run focused `Email` and `RabbitMQ` runtime categories with separate TRX/log/diagnostic artifacts. E2E remains manual/nightly in `.github/workflows/e2e.yml` until reliability evidence supports promotion.
  - **Effort:** M
  - **Dependencies:** 7.4

## Verification Checklist
- [ ] LSP diagnostics clean for modified source files where available. Not run; final validation used build, architecture tests, focused project tests, `git diff --check`, and workflow YAML parsing.
- [x] `dotnet build --configuration Release --verbosity quiet` passed with 26 projects, 0 errors, and existing package advisory warnings.
- [x] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed 313/313.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed 1955/1955.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1` passed 645/645.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1` passed 52/52.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=RabbitMQ]" --minimum-expected-tests 1` passed 45/45.
- [x] `dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet` passed 202/202.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed 243 total / 242 succeeded / 1 intentional skip.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed 226/226.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` failed with 19 unrelated auth/storage/projection failures; focused `Email` category passed 26/26.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1` passed 26/26.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed 187/187.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1` passed 1480 total / 1479 succeeded / 1 intentional skip.
- [x] `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet /clp:ErrorsOnly` passed with 13 projects, 0 errors; focused registration E2E passed 1/1 earlier, and the full E2E suite remains documented as manual/nightly.
- [x] `git diff --check` passed.
- [x] Workflow YAML parse passed for `.github/workflows/_build-test.yml` and `.github/workflows/e2e.yml`.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work
- Full `Event.API.IntegrationTests` has 19 unrelated auth/storage/projection failures in the shared dirty worktree. The email/API lane added by this workstream is green.
- Full Aspire-backed E2E remains manual/nightly until reliability evidence supports promotion to a required PR gate.

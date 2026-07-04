<!-- ABOUTME: Implementation plan for improving the Event platform's overall testing system. -->
<!-- ABOUTME: Captures scope, decisions, phases, and verification policy for email/runtime coverage. -->

# Improved Overall Testing - Implementation Plan

Last Updated: 2026-07-04 Europe/Brussels

## 0. Planning Metadata
- **Request:** Create an implementation plan for improved overall testing, including email tests in every relevant scenario, updated tests, and no backward-compatibility preservation because the platform is still in development.
- **Task directory:** `dev/active/improved-overall-testing/`
- **Planning status:** Implementation complete; final verification recorded with an unrelated full API-suite blocker.
- **Matched intents:** No exact testing-strategy intent exists in `.claude/contract/intents.yaml`. This plan uses the fallback contract from `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `.claude/rules/tests.md`, and relevant skills. Adjacent intents: `ci-cd-change` if implementation edits workflows, and `external-infrastructure-bootstrap` if implementation changes Aspire/Docker test infrastructure.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `outbox-pattern`, `aspire`, `auth-patterns`, `blazor-bff-patterns`, `source-command-check`.
- **Relevant rules:** `.claude/rules/tests.md`, `.claude/rules/api-controllers.md`, `.claude/rules/application-layer.md`, `.claude/rules/blazor-server.md`, `.claude/rules/blazor-client.md`.
- **External documentation consulted:** Context7 docs for TUnit, Testcontainers for .NET, and Microsoft Playwright .NET were consulted during planning and refreshed on 2026-07-04 before implementation.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor, Docs, DevOps.
- **Estimated complexity:** XL. The work spans test taxonomy, fixtures, PostgreSQL state, Mailpit, RabbitMQ, Aspire-backed E2E, BFF boundaries, CI workflows, documentation, and removal or rewrite of obsolete tests.

## 1. Executive Summary
The target is an enterprise-grade testing system that proves the platform from fast domain rules through full runtime email delivery. Email testing must cover direct SMTP to Mailpit, Basic Email Dispatch with PostgreSQL outbox and TickerQ/hosted-service triggers, RabbitMQ pointer dispatch over the same durable outbox state machine, failure/retry/dead-letter paths, tenant SMTP overrides, and browser-visible registration flows.

The implementation should improve the existing TUnit suite without replacing the repo's chosen stack. It should add shared test fixtures where reuse is real, standardize categories and CI lanes, make Mailpit the local assertion surface for email, and keep RabbitMQ tests as transport-mode proof instead of a parallel email implementation.

Explicitly out of scope: preserving obsolete routes, DTO shapes, UI behavior, or compatibility tests. When a test only guards retired behavior, delete or rewrite it around the intended behavior.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log
| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| The repo uses TUnit across unit, integration, architecture, BFF, component, and E2E projects. | Verified: `docs/TESTING.md`; search matched `TUnit` package references in all test csproj files. | High | TUnit filtering must use `--treenode-filter`, not VSTest `--filter`. |
| Current test docs define 10 test projects and CI lanes. | Verified: `docs/TESTING.md`; verified workflows in `.github/workflows/_build-test.yml` and `.github/workflows/e2e.yml`. | High | Existing lanes are a good base, but email-specific runtime proof is incomplete. |
| Mailpit is local development infrastructure in Aspire and Compose. | Verified: `Explore.AppHost/AppHost.cs`, `docker-compose.yml`, `.env.example`, `docs/GETTING_STARTED.md`, `docs/OPERATIONS.md`, `docs/EMAIL_NOTIFICATIONS.md`. | High | Aspire exposes `localhost:1025` SMTP and `http://localhost:8025` UI; Compose uses `mailpit:1025` in containers. |
| E2E tests have a Mailpit Testcontainers fixture and AppHost SMTP preconfiguration. | Verified: `Explore.Blazor.Client.E2ETests/Fixtures/MailpitContainerFixture.cs`; `Explore.Blazor.Client.E2ETests/Fixtures/AppHostFixture.cs`; `RegistrationFlowTests`. | High | `RegistrationFlowTests` now proves Mailpit delivery through the Aspire-backed browser/API/BFF stack. |
| Basic Email Dispatch uses PostgreSQL `EmailDispatchOutbox` plus SMTP, not RabbitMQ. | Verified: `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `Explore.Application/Contracts/Services/IEmailDispatchDrainService.cs`, `Explore.Infrastructure/EmailDispatchDrainService.cs`. | High | RabbitMQ is optional transport over the same outbox state machine. |
| RabbitMQ dispatch payloads are pointer-only and intentionally exclude PII and secrets. | Verified: `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `Explore.Application/Contracts/Infrastructure/EmailDispatchPointer.cs`, `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchPointerTests.cs`. | High | Tests must keep asserting this security boundary. |
| Existing email tests are broad at unit/persistence/API levels. | Verified by search: `Event.Application.UnitTests/Services/EventLifecycleEmailOutboxFactoryTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchDrainServiceTests.cs`, `Event.Persistence.IntegrationTests/Repositories/EmailDispatchOutboxTransitionRepositoryTests.cs`, `Event.API.IntegrationTests/Features/EmailDispatchAdminControllerTests.cs`. | High | They mostly do not prove live Mailpit or live RabbitMQ delivery. |
| Existing RabbitMQ tests are mostly unit/decision tests, not live broker tests. | Verified: `Explore.Infrastructure.Tests/Infrastructure/EmailDispatchRabbitMqPointerPublisherTests.cs`, `EmailDispatchRabbitMqConsumerDecisionTests.cs`, `EmailDispatchRabbitMqDeadLetterReplayDecisionTests.cs`, `EmailDispatchRabbitMqHealthCheckTests.cs`. | High | Add Testcontainers RabbitMQ coverage for topology/publish/consume/DLQ. |
| Architecture tests enforce durable side-effect boundaries. | Verified: `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs`. | High | New tests must not normalize direct SMTP/broker calls from Application handlers or controllers. |
| Existing E2E critical flows are consistently tagged. | Verified by source audit under `Explore.Blazor.Client.E2ETests/Flows`: every `*Tests.cs` file carries `[Category(E2ETestCategories.E2E)]`; visual/operator-reviewed checks also carry `Manual`. | High | E2E filters can now select all browser flow classes, and manual checks can be included or excluded explicitly. |
| Related paused work exists for SMTP abstraction. | Verified: `dev/pause/email-smtp-abstraction/*`. | Medium | It is historical and narrower than this plan. Do not merge workstreams unless implementation discovers unfinished SMTP tasks still apply. |

### 2.2 Existing Implementation
By layer:

- **Domain:** `Explore.Domain/EmailDispatchOutbox.cs`, receipt/attempt entities, and governance setting keys model email dispatch state and SMTP settings without knowing MailKit, RabbitMQ, TickerQ, or HTTP.
- **Application:** lifecycle services create `EmailDispatchOutbox` rows and scheduler pointers. Email admin command/query handlers operate through `IEmailDispatchOutboxRepository` and map entities to DTOs in handlers.
- **Persistence:** `Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs` owns transition queries, duplicate claim prevention, RabbitMQ publish batch selection, tenant pause state, replay, parking, and stale-processing recovery.
- **Infrastructure:** `SmtpEmailService` sends via MailKit; `SmtpConfigResolver` resolves tenant-aware SMTP settings; `EmailDispatchDrainService` drains one durable row; RabbitMQ services publish/consume pointer-only messages and reuse the same drain.
- **API:** `Explore.API/Scheduling/EmailDispatchTickerQJobs.cs`, `Explore.API/HealthChecks/EmailDispatchHealthCheck.cs`, `SmtpHealthCheck`, and EmailDispatch admin controllers expose scheduler, health, and operator actions.
- **Blazor/E2E:** `AppHostFixture` starts API/Blazor through Aspire testing, uses external test-managed PostgreSQL/Keycloak/Mailpit, and preconfigures Mailpit SMTP settings before runtime.
- **DevOps:** `_build-test.yml` runs fast and integration groups separately; `e2e.yml` runs Aspire-backed Playwright manually/nightly with TRX, Playwright, and Docker artifacts.

### 2.3 Existing Tests And Verification Coverage
Verified coverage:

- `Event.Domain.UnitTests`: settings registry and domain invariants.
- `Event.Application.UnitTests`: lifecycle email outbox factory, scheduler pointer creation, EmailDispatch command/query handlers, and email dispatch metrics.
- `Explore.Infrastructure.Tests`: SMTP-adjacent infrastructure behavior, `EmailDispatchDrainService`, RabbitMQ pointer contract, RabbitMQ publish decisions, consumer settlement decisions, DLQ replay decisions, settings validators, and health check behavior.
- `Event.Persistence.IntegrationTests`: PostgreSQL transition behavior for outbox state, duplicate claim prevention, tenant pause state, RabbitMQ publish metadata, and stale lease recovery.
- `Event.API.IntegrationTests`: EmailDispatch admin API, HAL links, health checks, TickerQ job wrappers, security/policy contracts, real-runtime API fixtures.
- `Explore.Blazor.IntegrationTests`: BFF token/header/cookie boundary tests, including Keycloak-backed security fixtures.
- `Explore.Blazor.Client.Tests`: bUnit/component/service tests.
- `Explore.Blazor.Client.E2ETests`: Playwright flows, AppHost fixture, test-managed PostgreSQL/Keycloak/Mailpit, registration flow, tenant isolation flow, BFF forwarding flow, smoke tests.
- `Event.Architecture.Tests`: Clean Architecture and durable side-effect boundary tests.

Main missing coverage:

- No verified live `SmtpEmailService` to Mailpit integration test in an infrastructure or runtime test project.
- No verified Basic Email Dispatch test that creates an outbox row, drains it, and asserts the message in Mailpit.
- No verified hosted-service vs TickerQ trigger matrix for Basic Dispatch using Mailpit.
- No verified live RabbitMQ broker test that publishes a pointer, consumes it, drains through SMTP, and confirms Mailpit delivery.
- No verified browser test that completes registration and asserts the confirmation email appears in Mailpit.
- No single risk traceability matrix that maps email scenarios to exact test files and CI lanes.

### 2.4 Existing Documentation And Contracts
Relevant docs and contracts:

- `docs/TESTING.md`: current test projects, TUnit conventions, categories, disabled-test governance, and CI lanes.
- `docs/OPERATIONS.md`: Aspire profiles, health checks, Basic Email Dispatch operations, RabbitMQ dispatch operations.
- `docs/CONFIGURATION.md`: `EmailDispatchProcessor`, `Scheduler:TickerQ`, `EmailDispatchRabbitMq`, and SMTP setting precedence.
- `docs/EMAIL_NOTIFICATIONS.md`: SMTP implementation boundary, Mailpit local development behavior, and unsupported email fanout claims.
- `docs/GETTING_STARTED.md`: contributor profile matrix and Mailpit local endpoints.
- `docs/CONTRIBUTING.md`: PR/test expectations.
- `.github/workflows/_build-test.yml`, `.github/workflows/e2e.yml`, `.github/workflows/security-tests.yml`, `.github/workflows/openapi-contract.yml`: current automation lanes.
- `Event.API.IntegrationTests/Fixtures/TestCategories.cs`: current API category constants.
- `Explore.Blazor.IntegrationTests/Fixtures/BffKeycloakFixture.cs`: current BFF category constants.

### 2.5 Current Pain Points / Improvement Areas
1. **Runtime email proof is not complete.** Existing tests prove many units and state transitions, but not every full delivery path through Mailpit.
2. **RabbitMQ lacks live broker confidence.** Current tests protect pointer and decision logic, but not topology declaration, publish confirms, manual ACK after durable drain, DLQ routing, or replay/parking against a real broker.
3. **E2E registration does not appear to verify email.** The fixture exposes Mailpit helpers, but the registration flow should poll Mailpit and assert subject, recipient, body/link, and outbox state.
4. **Test taxonomy is not consistently applied.** Some E2E classes are not tagged; email and RabbitMQ have no dedicated categories or project-level lane naming.
5. **Fixtures are powerful but localized.** Mailpit exists only in E2E. Reusable Mailpit/RabbitMQ helpers should be shared only where multiple projects need them.
6. **CI evidence is generic.** Current runtime lanes collect useful artifacts, but email-specific artifacts and broker diagnostics are not first-class.
7. **Docs lag implementation risk.** `docs/TESTING.md` explains lanes, but it does not yet include a complete email scenario matrix with exact tests and commands.
8. **Paused SMTP plan has stale tasks.** `dev/pause/email-smtp-abstraction` predates current outbox/RabbitMQ work; implementation should mine it for useful history but not treat it as current truth.

### 2.6 Unknowns After Investigation
| Unknown | What Was Searched | Resolution Task |
|---|---|---|
| Whether live `SmtpEmailService` already has a Mailpit integration test under another name. | Searched tests for `Mailpit`, `SmtpEmailService`, `EmailDispatch`, and `IEmailService`. | Phase 2 task creates or confirms it. |
| Whether TickerQ can be made deterministic enough for an automated Mailpit test. | Read `EmailDispatchTickerQJobsTests`, `AppHostFixture`, and operations docs. | Phase 4 proves TickerQ wrapper directly and uses runtime polling only if stable. |
| Whether RabbitMQ live tests belong in `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, or a new runtime lane. | Inspected current project roles and package references. | Phase 5 starts in `Explore.Infrastructure.Tests` for transport topology and uses API/E2E only for full app runtime. |
| Whether shared test helpers should be a new project or local fixtures. | Searched for existing test-support projects; none found. | Phase 1 decides based on reuse across at least two test projects. |
| CI runtime budget for RabbitMQ + Mailpit + Aspire E2E. | Inspected workflows only; no duration history available locally. | Phase 7 adds manual/nightly first, then promotes when reliable. |

## 3. Proposed Future State
The testing system should operate as layered proof:

```text
Domain/Application unit tests
  -> no infrastructure, fast behavior and safety invariants

Infrastructure tests
  -> MailKit/Mailpit SMTP, RabbitMQ transport, config validators, provider boundaries

Persistence integration tests
  -> real PostgreSQL outbox state machine, tenant isolation, idempotent claims

API integration tests
  -> HTTP contracts, HAL operator actions, health/readiness, Basic Dispatch controls

BFF/component tests
  -> cookie/token boundary, client action gating, admin settings surfaces

Playwright E2E
  -> registration/browser journeys with Aspire + Mailpit assertions

Nightly/manual runtime lane
  -> full local infra, Mailpit evidence, RabbitMQ evidence, artifacts
```

Email scenario matrix target:

| Scenario | Primary Test Layer | Required Proof |
|---|---|---|
| Direct SMTP works with Mailpit | `Explore.Infrastructure.Tests` | `SmtpEmailService.SendAsync` reaches Mailpit; `TestConnectionAsync` succeeds; no real SMTP. |
| Missing SMTP config fails safely | `Explore.Infrastructure.Tests` | Clear error, no send attempt, no secret leakage. |
| Basic Dispatch manual drain | `Explore.Infrastructure.Tests` or `Event.API.IntegrationTests` | Pending outbox row becomes Sent and Mailpit receives one message. |
| Basic Dispatch hosted-service trigger | `Event.API.IntegrationTests` | HostedService mode drains due rows against Mailpit or deterministic processor harness. |
| Basic Dispatch TickerQ trigger | `Event.API.IntegrationTests` | TickerQ job calls shared drain; runtime polling test if deterministic. |
| Retry/dead-letter/unknown outcomes | `Explore.Infrastructure.Tests`, `Event.Persistence.IntegrationTests` | Failure categories and final states are persisted without leaking PII. |
| Tenant paused | `Event.Persistence.IntegrationTests`, `Event.API.IntegrationTests` | Paused tenant does not send, operator can resume/replay. |
| Tenant SMTP override | `Event.API.IntegrationTests` or E2E fixture | Correct tenant settings reach Mailpit; no cross-tenant config bleed. |
| RabbitMQ pointer publish | `Explore.Infrastructure.Tests` | Real broker topology, mandatory route, publisher confirm, pointer-only payload. |
| RabbitMQ consume to SMTP | Runtime/integration | Pointer consumed, drain sends through Mailpit, ACK after durable outcome. |
| RabbitMQ malformed/missing pointer | Runtime/integration | Rejects to DLQ; no SMTP send. |
| RabbitMQ DLQ replay/parking | Runtime/integration | Safe rows replay, unsafe rows park, original DLQ ACK only after durable action. |
| Browser registration email | `Explore.Blazor.Client.E2ETests` | User completes flow; Mailpit contains expected confirmation email. |

## 4. Non-Negotiable Constraints
- Repositories return entities, never DTOs; mapping stays in handlers.
- Validators are manually instantiated; do not move them into DI.
- Application handlers must create durable intent rows and must not send SMTP, publish RabbitMQ, or call TickerQ directly.
- Domain must not reference persistence, MailKit, RabbitMQ, TickerQ, ASP.NET Core, or test infrastructure.
- Integration/E2E tests use real infrastructure; mocks are allowed for unit tests only.
- TUnit filters use `--treenode-filter`; do not add VSTest `--filter` examples.
- Disabled tests require `[Skip("Category: ... Removal: ...")]`; do not comment out `[Test]`.
- Backward-compatibility-only tests for obsolete behavior must be deleted or rewritten.
- HAL links remain the UI source of truth for actions; component tests should assert link-gated affordances.
- BFF tests must preserve cookie-only browser auth and server-side token forwarding; no browser-accessible tokens.
- Every new C# file needs the two-line `ABOUTME:` header.

## 5. Architecture And Design Decisions

### Decision 1: Keep TUnit And Project-Local Test Ownership
- **Why:** The repo is already standardized on TUnit and has architecture tests enforcing TUnit conventions.
- **Alternatives considered:** Introducing xUnit/NUnit for integration helpers. Rejected because it fragments the suite.
- **Consequences:** New tests use TUnit lifecycle, categories, and `--treenode-filter`.
- **Files/layers affected:** All test projects, `docs/TESTING.md`, CI workflows.

### Decision 2: Use Mailpit As The Email Assertion Boundary
- **Why:** Mailpit gives a real SMTP endpoint plus HTTP inspection without sending external email.
- **Alternatives considered:** Mocking `IEmailService` in integration/E2E tests. Rejected for runtime proof because it would not validate MailKit/config/network behavior.
- **Consequences:** Fast unit tests can still use fakes; integration/E2E tests must assert actual Mailpit messages.
- **Files/layers affected:** `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests`, `Explore.Blazor.Client.E2ETests`, `Explore.AppHost`, Compose docs.

### Decision 3: Treat RabbitMQ As Optional Transport, Not A Separate Email System
- **Why:** Current architecture states RabbitMQ carries pointer-only messages over the same PostgreSQL `EmailDispatchOutbox` and shared drain service.
- **Alternatives considered:** Creating separate broker-backed email tests that send email payloads through RabbitMQ. Rejected for privacy and architectural reasons.
- **Consequences:** RabbitMQ tests assert pointer payload safety, topology, ACK/NACK/DLQ semantics, and final SMTP delivery through the same drain.
- **Files/layers affected:** `Explore.Infrastructure/Messaging/*`, `Explore.Infrastructure.Tests`, `Event.Persistence.IntegrationTests`, runtime CI.

### Decision 4: Add Shared Test Helpers Only When They Serve Multiple Projects
- **Why:** A test-support project reduces duplication, but premature shared abstraction can hide scenario intent.
- **Alternatives considered:** Keep all fixtures local forever. Rejected if Mailpit/RabbitMQ helpers duplicate across infrastructure, API, and E2E tests.
- **Consequences:** Prefer a small test-only helper project if two or more projects need identical Mailpit/RabbitMQ polling and API models.
- **Files/layers affected:** possible new `Explore.Testing` project, solution file, test csproj references.

### Decision 5: Runtime Email Tests Start Manual/Nightly, Then Promote
- **Why:** Docker/Aspire/RabbitMQ/Playwright tests are expensive and need reliability evidence before becoming merge-blocking.
- **Alternatives considered:** Add every runtime email test to PR immediately. Rejected because it risks noisy gates before the suite has stability data.
- **Consequences:** Fast deterministic tests run in PR without hidden runtime prerequisites. Docker-backed provider tests stay behind explicit categories and run in integration/manual/nightly lanes until reliability justifies promotion. As of 2026-07-04, fast CI runs `Explore.Infrastructure.Tests` with `Category!=Runtime`, while integration-enabled callers run the focused infrastructure `Email` category for Mailpit SMTP evidence.
- **Files/layers affected:** `.github/workflows/_build-test.yml`, `.github/workflows/e2e.yml`, optional new `.github/workflows/email-runtime.yml`.

## 6. Implementation Phases

### Phase 0: Baseline, Delete Obsolete Coverage, And Lock The Taxonomy
- **Goal:** Establish current pass/fail state, identify obsolete tests, and define categories before adding runtime tests.
- **Depends on:** User approval of this plan. Satisfied by the 2026-07-04 user instruction to fully implement the plan.
- **Relevant files:** `docs/TESTING.md` (existing), `.claude/rules/tests.md` (existing), `Event.API.IntegrationTests/Fixtures/TestCategories.cs` (existing), `Explore.Blazor.IntegrationTests/Fixtures/BffKeycloakFixture.cs` (existing), E2E flow files (existing).
- **Related skills/rules:** `source-command-check`, tests rule, Clean Architecture.
- **Acceptance criteria:** Current test inventory is documented; obsolete/backward-compat-only tests are listed for deletion; category constants cover Email, RabbitMQ, E2E, Runtime, Slow/Manual where needed.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`; architecture tests for test governance.
- **Rollback / failure handling:** If baseline fails for unrelated existing issues, record exact failures in context before editing tests.

### Phase 1: Shared Fixture And Test Harness Foundation
- **Goal:** Build reusable, boring test infrastructure for Mailpit, RabbitMQ, polling, and email assertions.
- **Depends on:** Phase 0 taxonomy.
- **Relevant files:** `Explore.Blazor.Client.E2ETests/Fixtures/MailpitContainerFixture.cs` (existing), possible new `Explore.Testing/*`, `Directory.Packages.props`, `Explore.Infrastructure.Tests.csproj`, `Event.API.IntegrationTests.csproj`.
- **Acceptance criteria:** Mailpit fixture supports send/clear/poll/message detail; RabbitMQ fixture starts a real broker with readiness; helper APIs avoid leaking credentials and raw message bodies into logs.
- **Verification:** Targeted fixture tests in `Explore.Infrastructure.Tests`; no production project references test helpers.
- **Rollback / failure handling:** Keep helpers local to first test project if a shared project creates dependency churn.

### Phase 2: SMTP And Config Integration Tests
- **Goal:** Prove direct SMTP behavior against Mailpit and config precedence without a browser.
- **Depends on:** Phase 1.
- **Relevant files:** `Explore.Infrastructure/Mail/SmtpEmailService.cs` (existing), `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` (existing), `Explore.Infrastructure.Tests/Infrastructure/*` (existing/new).
- **Acceptance criteria:** Send succeeds to Mailpit; test connection succeeds; missing host/from-address fails safely; tenant/system config paths resolve expected values; logs/assertions avoid secrets.
- **Verification:** `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1`.
- **Rollback / failure handling:** If Mailpit container flakiness appears, add explicit wait strategy and diagnostic capture before broadening tests.

### Phase 3: Basic Email Dispatch Tests With PostgreSQL And Mailpit
- **Goal:** Prove the durable outbox drain sends real email through Mailpit and persists final state.
- **Depends on:** Phase 2.
- **Relevant files:** `Explore.Infrastructure/EmailDispatchDrainService.cs` (existing), `Event.Persistence.IntegrationTests/Repositories/EmailDispatchOutboxTransitionRepositoryTests.cs` (existing), `Event.API.IntegrationTests/Features/*` (existing/new).
- **Acceptance criteria:** Pending row drains once; duplicate claims still prevent duplicate sends; retry/dead-letter/unknown states are persisted; tenant pause prevents send; operator replay produces expected state.
- **Verification:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`; targeted API integration tests.
- **Rollback / failure handling:** If full API host is unstable, keep deterministic drain tests in infrastructure/persistence and isolate host lifecycle separately.

### Phase 4: Scheduler Trigger Coverage For TickerQ And HostedService
- **Goal:** Prove both Basic Dispatch trigger modes call the same drain service and are testable without duplicating email logic.
- **Depends on:** Phase 3.
- **Relevant files:** `Explore.API/Scheduling/EmailDispatchTickerQJobs.cs` (existing), `Explore.API/BackgroundServices/EmailDispatchProcessor.cs` (existing), `Event.API.IntegrationTests/Features/EmailDispatchTickerQJobsTests.cs` (existing), new targeted tests if needed.
- **Acceptance criteria:** TickerQ job wrappers stay pointer/payload-free; hosted-service mode drains due rows through the shared service; selected mode affects readiness correctly.
- **Verification:** API integration tests plus architecture durable-boundary tests.
- **Rollback / failure handling:** Prefer deterministic service/job invocation over timing-dependent waits unless runtime lane reliability is proven.

### Phase 5: RabbitMQ Live Transport Tests
- **Goal:** Add real RabbitMQ coverage for optional dispatch mode while preserving pointer-only privacy.
- **Depends on:** Phases 1 and 3.
- **Relevant files:** `Explore.Infrastructure/Messaging/RabbitMqEmailDispatchTransport.cs` (existing), `EmailDispatchRabbitMqPointerPublisher.cs` (existing), `EmailDispatchRabbitMqConsumerService.cs` (existing), `EmailDispatchRabbitMqDeadLetterReplayService.cs` (existing), `Explore.Infrastructure.Tests/Infrastructure/*` (existing/new), `Directory.Packages.props`.
- **Acceptance criteria:** Topology declares; mandatory publish confirms; missing route returns failure; valid pointer consume drains to Mailpit and ACKs; malformed/missing pointers reject to DLQ; DLQ replay/parking follows durable database checks.
- **Verification:** `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=RabbitMQ]"`.
- **Rollback / failure handling:** Keep live broker tests category-gated and manual/nightly until stable.

### Phase 6: Blazor/BFF/Playwright Email Journeys
- **Goal:** Prove user-facing flows that require a browser, BFF, API, Keycloak, PostgreSQL, and Mailpit.
- **Depends on:** Phases 2 and 3.
- **Relevant files:** `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/RegistrationFlowTests.cs` (existing), `AppHostFixture.cs` (existing), `MailpitContainerFixture.cs` (existing), Blazor admin SMTP components if touched.
- **Acceptance criteria:** Registration flow clears Mailpit before run, completes browser registration, polls Mailpit, asserts recipient/subject/body/link, and verifies no duplicate sends; admin SMTP UI tests assert HAL/authorization boundaries where applicable.
- **Verification:** `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=E2E]"`.
- **Rollback / failure handling:** If browser flow is flaky, keep a deterministic API/runtime email test as the required gate and leave browser email proof in nightly/manual with artifacts.

### Phase 7: CI, Artifacts, And Documentation
- **Goal:** Make the new coverage usable by contributors and maintainers.
- **Depends on:** Phases 0-6.
- **Relevant files:** `.github/workflows/_build-test.yml`, `.github/workflows/e2e.yml`, optional `.github/workflows/email-runtime.yml`, `docs/TESTING.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/OPERATIONS.md`, `docs/CONTRIBUTING.md`, `docs/GETTING_STARTED.md`.
- **Acceptance criteria:** Fast tests stay PR-friendly; runtime email/RabbitMQ lane is manual/nightly with TRX, Mailpit message summaries, broker diagnostics, Playwright artifacts, and Docker logs; docs list exact commands and scenario matrix.
- **Verification:** YAML lint/CI dry-run where available; architecture docs tests; manual workflow dispatch after implementation.
- **Rollback / failure handling:** If runtime lane is expensive, keep it advisory and documented while project-level deterministic tests remain merge-blocking.

## 7. Testing Strategy
Required end-state commands:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

Focused commands to add or document:

```bash
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=RabbitMQ]" --minimum-expected-tests 1
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=Email]" --minimum-expected-tests 1
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=E2E]" --minimum-expected-tests 1
```

Test design principles:

- Unit tests prove pure behavior with fakes/substitutes.
- Integration tests use real PostgreSQL/Mailpit/RabbitMQ where the behavior depends on provider semantics.
- Playwright tests are small and only cover browser/BFF/JS behavior that lower layers cannot prove.
- Runtime tests poll eventually with bounded timeouts and diagnostic output; they do not sleep blindly.
- Email tests assert both positive delivery and negative privacy constraints.
- RabbitMQ tests assert final durable state, not only broker state.

## 8. Documentation, Configuration, And Operations Impact
Docs to update during implementation:

- `docs/TESTING.md`: add email scenario matrix, categories, focused commands, CI lane meanings, and runtime artifact expectations.
- `docs/EMAIL_NOTIFICATIONS.md`: document which email flows are test-proven and how Mailpit assertions work locally.
- `docs/OPERATIONS.md`: add operator/developer troubleshooting for Mailpit/RabbitMQ test lanes if runtime tests expose common failures.
- `docs/CONTRIBUTING.md`: include contributor-friendly test commands and no-solution-level `dotnet test` rule.
- `docs/GETTING_STARTED.md`: mention how contributors can verify Mailpit after `aspire run` and Compose.
- `.env.example` only if implementation discovers missing test env keys; avoid adding secrets.

Configuration impact:

- Possible new test-only `Testcontainers.RabbitMq` package and lock-file updates.
- Possible new category constants for `Email`, `RabbitMQ`, `Runtime`, `Slow`, and `Manual`.
- Optional new GitHub Actions runtime workflow if existing `e2e.yml` should not absorb RabbitMQ/Mailpit email proof.

## 9. Security, Authorization, Privacy, And Abuse Considerations
- Email bodies, recipients, subjects, SMTP credentials, provider IDs, raw SMTP errors, and AMQP connection strings must not appear in broker payloads, metrics, or logs.
- RabbitMQ payload tests must assert `EmailDispatchPointer` remains pointer-only.
- BFF/E2E tests must preserve token secrecy: browser cookies only, server-side bearer forwarding only.
- Admin SMTP and EmailDispatch operator actions must remain authorized and HAL-gated.
- Tenant SMTP override tests must prove no cross-tenant setting bleed.
- Test artifacts must avoid storing full email bodies or secrets unless deliberately redacted and scoped to local test evidence.
- Negative tests must prove disabled RabbitMQ mode does not attempt broker connections.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
- **Multi-tenancy:** Applicable. Email dispatch rows, SMTP settings, tenant pause controls, and Mailpit assertions must preserve tenant boundaries.
- **Federation:** Needs investigation. Current email scope is local application dispatch; do not add federation email claims unless a federated flow is tested.
- **Localization:** Applicable for user-facing email bodies and browser flows. Tests should not lock copy too tightly unless copy is part of contract; prefer semantic tokens/links.
- **Accessibility:** Applicable for Blazor admin SMTP/EmailDispatch surfaces. Component tests should cover labels, validation feedback, and HAL-gated affordances if UI changes occur.
- **Product:** Applicable. The suite should support contributors using `local-full` and maintainers using `local-core`/`local-lite`, while CI keeps deterministic evidence.

## 11. Observability And Operations
Testing should verify:

- `smtp`, `email-dispatch`, and `email-dispatch-rabbitmq` health checks report safe, bounded data.
- Metrics use low-cardinality tags and omit sensitive fields.
- Runtime lanes collect TRX, Docker logs, Mailpit message summaries, RabbitMQ container logs/topology diagnostics, Playwright traces/screenshots/videos, and API/Blazor logs.
- Failure output tells developers whether the issue is SMTP config, Mailpit readiness, broker readiness, database state, scheduler timing, or browser flow.

## 12. Migration And Compatibility Plan
No production data migration is expected for the planning target unless implementation discovers missing test-only schema behavior. If tests expose incorrect schema/index behavior, create normal EF Core migrations following repo conventions.

Compatibility policy: because the repo is pre-v1 and the user explicitly said development mode, do not add compatibility shims or backward-compatibility tests for obsolete routes/contracts. Delete or rewrite obsolete tests around the desired behavior.

## 13. Risk Register
| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Runtime email tests become flaky due to scheduler timing. | Medium | High | Prefer deterministic drain/job calls for PR; put timing-dependent proof in nightly/manual first. | Intermittent E2E/RabbitMQ lane failures. | Phase 4/6 |
| RabbitMQ live tests leak payload details or normalize unsafe broker payloads. | Low | High | Assert pointer-only contract and log/metric redaction. | Architecture tests or payload assertions fail. | Phase 5 |
| Shared test helper project creates dependency sprawl. | Medium | Medium | Keep helpers test-only; extract only after two projects need them. | Production project references test helper. | Phase 1 |
| Full E2E lane exceeds CI budget. | Medium | Medium | Keep category filters and manual/nightly runtime lane; promote after evidence. | Workflow duration/failure summaries. | Phase 7 |
| Mailpit image drift breaks tests. | Medium | Medium | Pin test image and align docs; use wait strategy and diagnostics. | Mailpit readiness/API failures. | Phase 1/2 |
| Obsolete compatibility tests hide desired behavior. | Medium | Medium | Audit and delete/rewrite during Phase 0. | Tests fail only because old behavior changed. | Phase 0 |

## 14. Success Metrics And Definition Of Done
Functional success:

- Every email scenario in the matrix has a named test file and command.
- Basic Dispatch and RabbitMQ Dispatch both end in Mailpit delivery proof.
- Negative cases prove no duplicate sends, no PII in broker payloads, and safe failure states.
- Contributor docs explain exactly which tests to run and when.

Quality gates:

- Build passes.
- All required test projects pass individually.
- New test categories work with TUnit `--treenode-filter`.
- Architecture tests pass, including durable side-effect boundaries and disabled-test governance.
- Runtime email lane has retained artifacts and documented status.

Definition of done:

- `docs/TESTING.md` contains the final email test matrix.
- CI/workflows match the documented lane model.
- Dev docs are updated after each implementation slice.
- No skipped test lacks `Category:` and `Removal:`.
- No backward-compatibility-only tests remain for retired behavior.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT
Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `improved-overall-testing-context.md`, and `improved-overall-testing-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `improved-overall-testing-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `improved-overall-testing-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report "done" unless docs reflect the actual current state.
5. Every implementation summary to the user must include:
   - what was implemented, explained as a developer teaching summary;
   - which architecture/design patterns, libraries, infrastructure components, protocols, and project abstractions were used;
   - which important files/classes/interfaces/handlers/components changed and what each is responsible for;
   - the relevant data/control flow;
   - what was verified;
   - what remains;
   - what should be worked on next.
6. If validation fails, update context/tasks with the failure, root cause if known, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract
When an implementation agent finishes a slice, its final response should use:

- **Implemented:** medium-sized developer teaching summary.
- **Verified:** exact commands and outcomes.
- **Remaining:** incomplete tasks and known risks.
- **Next:** next recommended slice.
- **Docs updated:** yes/no with reason.

For email slices, the implemented summary must name whether the slice used SMTP/MailKit, Mailpit, PostgreSQL `EmailDispatchOutbox`, TickerQ/HostedService, RabbitMQ, Testcontainers, Playwright, or Aspire, and where retries/idempotency/error handling live.

## 17. Potential Risks & Unknowns
The hardest part is not writing more tests; it is making runtime email proof deterministic enough to be valuable. Basic Dispatch has scheduler timing, PostgreSQL lease state, tenant SMTP resolution, and Mailpit readiness in one path. RabbitMQ adds broker topology, publisher confirms, manual ACK/NACK, DLQ routing, and consumer lifecycle. The plan therefore keeps PR gates focused on deterministic unit/integration proof and promotes full runtime paths only after they produce stable manual/nightly evidence.

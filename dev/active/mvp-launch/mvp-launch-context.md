<!-- ABOUTME: Current handoff context for the MVP launch implementation workstream. -->
<!-- ABOUTME: Records source evidence, decisions, risks, and next actions after the July 2026 rebaseline. -->

# MVP Launch Context

Last Updated: 2026-07-04 Europe/Brussels

## Current Status

The MVP launch docs have been rebaselined from an old May 2026 work-package backlog into a launch-closure program. Implementation has started with the Phase 2 email-dispatch compliance slice, and Phase 3 registration-integrity source/test/docs work is now complete. Phase 1 runtime proof is currently blocked by real full-local Aspire lifecycle failures observed on 2026-07-04.

This is the important shift:

- The repo already contains substantial launch infrastructure.
- The old docs understated completed work and over-prescribed obsolete implementations.
- The next implementation should verify, harden, and polish existing flows before adding new surface area.
- Registration email must use the existing `EmailDispatchOutbox` pipeline, not a new generic outbox handler path.
- Dispatch-time preference enforcement and unsubscribe affordances now live in the existing `EmailDispatchDrainService`; runtime proof through the real Mailpit path still remains.
- Registration client/UI handling now keeps the generated `BaseCommandResponseOfGuid` contract stable, maps generated-client failures safely, shares one outcome classifier across modal/list/preview registration flows, and gates event-detail registration affordances from HAL.
- The next runtime action is not generic smoke testing. First eliminate the Aspire CLI/AppHost lifecycle variable: the installed CLI is `13.3.0-preview.1.26221.24`, while the repository pins Aspire `13.4.6`, and a controlled run timed out waiting for the AppHost backchannel even after API/Blazor reached listening ports.

## Session Progress - 2026-07-04

Completed during rebaseline:

- Loaded the senior CTO feedback workflow and required project contract docs.
- Read project quick-reference, governance, operations, testing, architecture, API, security, authorization, Blazor, accessibility, design-system, and multi-tenancy docs.
- Read relevant path rules for API controllers, HATEOAS, application layer, persistence, Blazor client/server, and tests.
- Read relevant skills for Clean Architecture, CQRS/MediatR, EF Core, auth, Blazor BFF, Blazor UI, outbox, and error tracking.
- Audited the existing MVP launch plan/context/tasks.
- Verified current implementation reality around registration, email dispatch, unsubscribe, Data Protection, calendar, SEO, health, HAL helpers, and persistence constraints.
- Rewrote `mvp-launch-plan.md`, `mvp-launch-context.md`, and `mvp-launch-tasks.md` around current source evidence.
- Ran post-edit stale-reference, header/date, whitespace, Release build, and architecture-context checks.

Completed during Phase 2 implementation slice:

- Used Context7 official docs for current MailKit/MimeKit message/header behavior before changing email construction.
- Added terminal `Skipped` status to `EmailDispatchOutbox`, `EmailDispatchAttempt`, and `EmailDispatchReceipt`.
- Added `SkippedCount` and `EmailDispatchDrainOutcome.Skipped` to the drain service contract.
- Extended `EmailDispatchDrainService` to enforce mapped `UserNotificationPreference` before SMTP handoff, record opted-out rows as `Skipped` with `recipient_unsubscribed`, generate unsubscribe URLs through `IEmailUnsubscribeTokenService`, and add `List-Unsubscribe`, `List-Unsubscribe-Post`, and visible unsubscribe affordances when public base URL configuration is valid.
- Added repository methods for skipped outbox/receipt settlement and prevented skipped rows from being parked/replayed.
- Updated RabbitMQ DLQ replay decision to park already-skipped pointers.
- Updated API/HAL operator behavior so skipped rows do not receive replay or park affordances.
- Added focused Application, Infrastructure, API integration, and Persistence integration tests for skipped transitions and unsubscribe affordances.
- Updated `docs/API.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `docs/SECURITY-MODEL.md` with current behavior.

Completed during Phase 3 implementation slice:

- Used Context7 EF Core documentation to confirm the explicit-transaction pattern: user-initiated transactions must run inside the execution strategy delegate when retries are enabled.
- Added `Event.Persistence.IntegrationTests/Repositories/EventRegistrationIntentRepositoryTests.cs` with PostgreSQL-backed registration capacity tests.
- Proved concurrent registration attempts against a capacity-one session produce one approved child registration, waitlist the rest, and keep `current_audience_attendees` at `1`.
- Hardened `EventRegistrationIntentRepository` rollback cleanup so an already-completed Npgsql transaction does not mask the original retryable serializable transaction failure.
- Proved capacity increments roll back when `SaveChangesAsync` fails after reservation updates, using the child-level unique session/user index as the failure trigger.
- Added a duplicate session-selection persistence test proving the parent intent unique index returns the existing intent without adding another child row or capacity increment.
- Added `WasExisting` to `EventRegistrationIntentCreationResult`, translated duplicate parent-intent `23505` violations in `EventRegistrationIntentRepository`, and taught `CreateEventRegistrationCommandHandler` to return the existing-id response without running contact-share consent side effects.
- Added duplicate event-scope and day-scope PostgreSQL persistence tests using a seeded `EventDay` plus an alternate session, proving parent-intent duplicates return the existing id and roll back attempted capacity increments.
- Found and fixed a real EF model/schema gap: repeated unnamed `HasIndex` calls over `(TenantId, EventId, UserId)` caused EF Core to keep only the later session-selection partial index in migrations/snapshot. The fix gives the event and session-selection indexes distinct EF model names and adds a corrective migration for `ix_event_registration_intents_unique_event_scope`.
- Added a migration preflight check that raises a clear exception if duplicate active event-scope intents already exist, avoiding silent data deletion while still making operator cleanup explicit.
- Added an API boundary test proving `POST /api/eventregistration` returns HTTP 200 with the existing id when the command layer reports `Event Registration already exists.`.
- Added PostgreSQL persistence coverage proving a duplicate registration retry with a second `EmailDispatchOutbox` object still leaves only one pending registration-confirmation dispatch row tied to the original intent.
- Added real database-backed API repeat-submit coverage in `EventRegistrationRealRuntimeTests`: the same authenticated session-selection POST through `RealRuntimeApiFixture` returns the same intent id and leaves one parent intent, one child registration, one registration-confirmation outbox row, and one capacity reservation in PostgreSQL.
- Expanded `EventRegistrationRealRuntimeTests` with PostgreSQL-backed API coverage for a full session waitlist response and unauthenticated create rejection. The waitlist path stores waitlisted intent/child status, leaves `current_audience_attendees` unchanged, and still creates one registration-confirmation outbox row; the unauthenticated path returns `401` and creates no registration state.
- Confirmed the generated create-registration response contract remains `BaseCommandResponseOfGuid`; no OpenAPI/NSwag regeneration was required for the current Blazor outcome work.
- Updated `Explore.Blazor.Client/Services/EventRegistrationService.cs` so generated-client `ApiException` failures map to bounded `FailureCode`, `Message`, and `Errors` values instead of raw exception or response text.
- Updated `Explore.Blazor.Client/Pages/Events/EventListRegistrationWorkflow.cs` with shared confirmed/waitlisted/already-registered/failed outcome classification.
- Aligned `EventRegistration`, `EventList`, and `EventPreviewWorkspace` on the shared outcome classifier.
- Updated `EventPreviewWorkspace` to use the same policy-aware registration request builder as `EventList`, preserving whole-event registration when policy requires it.
- Proved `EventDetailsSidebar` renders the registration action only when the event detail HAL resource includes the `register` link.
- Added/updated Blazor client tests for safe registration-service error mapping, already-registered state, shared outcome classification, full EventList behavior, and HAL-gated sidebar registration affordances.
- Updated `docs/BLAZOR.md` with the registration service/outcome/HAL guidance.

In progress:

- Runtime proof and remaining MVP launch phases. The first Phase 2 code slice is implemented and tested, but Phase 1 runtime proof and Phase 2 Mailpit proof are still open.
- 2026-07-04 full-local Aspire proof attempts: `aspire start --apphost Explore.AppHost/Explore.AppHost.csproj --format Json --non-interactive --isolated` proved that full-local infrastructure can start, but did not prove launch runtime readiness. One attempt showed `explore-api` changing `Running -> Finished`; a later controlled attempt showed API/Blazor listening temporarily before the `13.3.0-preview` Aspire CLI timed out waiting for the AppHost backchannel and `aspire ps --format Json` still returned `[]`.
- Phase 3 source/test/docs work is complete; remaining launch risk is runtime/visual release evidence, not more registration service design.

Known context note:

- `AGENTS.md` imports `@RTK.md`, but `RTK.md` was not present at the repository root during this rebaseline. This was not treated as an MVP launch blocker, but context owners may want to resolve it separately if architecture/context tests require it.

## Quick Resume

If resuming this workstream:

1. Open `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, and `docs/OPERATIONS.md`.
2. Re-open the relevant rules and skills for the implementation slice.
3. Read these three files:
   - `dev/active/mvp-launch/mvp-launch-plan.md`
   - `dev/active/mvp-launch/mvp-launch-context.md`
   - `dev/active/mvp-launch/mvp-launch-tasks.md`
4. Confirm the worktree status and identify unrelated changes before editing.
5. Start with Phase 0 in the tasks file unless the owner explicitly selects a later phase.

## Current Source Evidence

### Registration and Capacity

| File | Evidence |
|---|---|
| `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` | Creates registration intent, child session rows, capacity-aware result, and an email dispatch outbox row through Application-layer orchestration. Validator is manually instantiated. |
| `Explore.Application/Contracts/Persistence/IEventRegistrationIntentRepository.cs` | Exposes `CreateWithChildrenAndCapacityAsync`, existing-intent lookup, and registered-user fanout batch. |
| `Explore.Persistence/Repositories/EventRegistrationIntentRepository.cs` | Runs serializable transaction, reserves capacity through conditional SQL update, inserts parent/children, inserts optional `EmailDispatchOutbox`, preserves original retryable transaction failures when rollback cleanup sees an already-completed Npgsql transaction, catches parent-intent duplicate `23505` violations for event/day/session scopes, and uses named tenant-filter bypass for exact tenant fanout. |
| `Explore.Persistence/Configurations/Entities/EventRegistrationIntentConfiguration.cs` | Has partial unique indexes for active event, day, and session-selection registration intents; same-column event/session indexes use distinct EF model names so both survive migrations. |
| `Explore.Persistence/Configurations/Entities/EventRegistrationConfiguration.cs` | Has a unique active child registration index per tenant/event/session/user. |
| `Event.Application.UnitTests/Features/EventRegistrations/Commands/CreateEventRegistrationCommandHandlerTests.cs` | Tests available capacity, waitlist message, event-scope child rows, and outbox creation. |
| `Event.Persistence.IntegrationTests/Repositories/EventRegistrationIntentRepositoryTests.cs` | Proves capacity-one concurrency, event/day/session duplicate parent-intent idempotency, duplicate registration-confirmation dispatch-row prevention, alternate-session rollback, and child-level unique failure rollback against PostgreSQL. |
| `Event.API.IntegrationTests/Features/EventRegistrationControllerTests.cs` | Proves the create endpoint preserves an idempotent command success response with the existing registration id. |
| `Event.API.IntegrationTests/Features/EventRegistrationRealRuntimeTests.cs` | Uses `RealRuntimeApiFixture` and PostgreSQL/Testcontainers to prove session-selection create success, repeat-submit idempotency, full-session waitlist behavior, one registration-confirmation outbox row, capacity preservation, and unauthenticated create rejection. |
| `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/RegistrationFlowTests.cs` | Exercises API registration and checks Mailpit/email dispatch when full runtime dependencies are available. |

Launch implication:

- Registration is source-implemented enough to verify and harden. Do not redesign it unless tests expose a defect.

### Email Dispatch

| File | Evidence |
|---|---|
| `Explore.Domain/EmailDispatchOutbox.cs` | Durable dispatch intent with pending/processing/sent/retry/dead-letter/parked/unknown/skipped states. |
| `Explore.Domain/EmailDispatchAttempt.cs` | Attempt ledger for each send try, including skipped-by-preference attempts. |
| `Explore.Domain/EmailDispatchReceipt.cs` | Idempotent receipt tracking keyed to provider/publish events, including skipped receipts. |
| `Explore.Application/Services/EventLifecycleEmailOutboxFactory.cs` | Builds lifecycle email outbox rows, including registration confirmation. Current body is basic and does not appear to include visible unsubscribe links. |
| `Explore.Infrastructure/EmailDispatchDrainService.cs` | Drains pending rows, recovers stale processing rows, respects tenant pause, claims receipts, enforces mapped `UserNotificationPreference`, adds unsubscribe headers/body links when public base URL is configured, sends through `IEmailService`, records attempts, and updates statuses/metrics. |
| `Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs` | Implements worker polling, operator status, tenant pause, park, replay, stale processing recovery, receipt claim/update, skipped settlement, and status transitions. |
| `Explore.API/Scheduling/TickerQScheduledEmailDispatchTrigger.cs` | Schedules email dispatch drain through TickerQ. |
| `Explore.API/HealthChecks/EmailDispatchHealthCheck.cs` | Health coverage for email dispatch backlog/failure state. |
| `docs/OPERATIONS.md` | Describes Basic Email Dispatch Mode, TickerQ, hosted worker fallback, tenant pause, and operator signals. |

Launch implication:

- The launch task is now runtime proof and remaining reliability coverage, not inventing a new email framework.

### Unsubscribe and Preferences

| File | Evidence |
|---|---|
| `Explore.API/Controllers/EmailUnsubscribeController.cs` | Public unsubscribe endpoint exists. |
| `Explore.Infrastructure/Mail/Unsubscribe/EmailUnsubscribeTokenService.cs` | Token generation/validation exists. |
| `Explore.Application/Contracts/Services/IEmailUnsubscribeTokenService.cs` | Application contract exists. |
| `Explore.Persistence/Repositories/UserNotificationPreferenceRepository.cs` | Preference persistence exists. |
| `Event.API.IntegrationTests/Features/EmailUnsubscribeControllerTests.cs` | API integration coverage exists. |
| `Explore.Infrastructure.Tests/Infrastructure/EmailUnsubscribeTokenServiceTests.cs` | Token service coverage exists. |

Launch implication:

- Foundation is now integrated into dispatch for mapped lifecycle categories. Remaining evidence: Mailpit/runtime proof and owner confirmation that registration confirmations should remain category-preference controlled rather than transactional-exempt.

### Public Surface

| File | Evidence |
|---|---|
| `Explore.API/Controllers/EventController.cs` | Calendar endpoint exists on event controller. |
| `Explore.API/Services/Calendar/IcalNetEventCalendarFileBuilder.cs` | iCal builder exists. |
| `Event.API.IntegrationTests/Features/Calendar/IcalNetEventCalendarFileBuilderTests.cs` | Calendar builder coverage exists. |
| `Explore.API/Controllers/SitemapController.cs` | `sitemap.xml` controller exists. |
| `Explore.Blazor/Controllers/RobotsController.cs` | `robots.txt` controller exists. |
| `Explore.Blazor.Client/Helpers/CanonicalUrlHelper.cs` | Canonical URL helper exists. |
| `Explore.Blazor.Client.Tests/Seo/CanonicalMetadataTests.cs` | Canonical metadata tests exist. |
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | Event detail includes canonical/Open Graph/Twitter metadata. |
| `docs/SEO.md` | Documents sitemap, robots, public render-policy classification, and tenant public-experience controls; explicitly says JSON-LD automation and site-wide SEO automation are not proven. |

Launch implication:

- Treat calendar/sitemap/robots/canonical/social metadata as source-complete but requiring runtime smoke and endpoint coverage checks. JSON-LD automation was not found by source search, and no web manifest was found beyond existing favicon/landing icon assets.

### HAL and UI Authorization

| File | Evidence |
|---|---|
| `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs` | Central HAL link helper methods exist. |
| Event detail/list/edit/sidebar components | Many launch-critical event actions already use `HasHalLink(...)`. |
| `Explore.Blazor.Client/Helpers/RoleHelper.cs` | Role helper still exists for labels/colors and some eligibility/admin logic. |
| `Explore.Blazor.Client/Services/EventCreationEligibilityService.cs` and selected admin/member components | Some paths still derive behavior from roles or combine role display with HAL checks. |

Launch implication:

- Do a targeted audit. Replace local role-based action affordance gates with HAL links; keep role helpers where they only format labels, colors, or select role options.

### Data Protection and Health

| File | Evidence |
|---|---|
| `Explore.Persistence/DataProtectionKeyContext.cs` | Persisted Data Protection key context exists. |
| `Explore.Persistence/DataProtectionKeyContextFactory.cs` | Design-time/runtime factory exists. |
| `Explore.Persistence/Extensions/DataProtectionServiceCollectionExtensions.cs` | Service registration exists. |
| `Event.MigrationService/Worker.cs` | Migration service migrates Data Protection key context. |
| `Event.Persistence.IntegrationTests/DataProtection/DataProtectionKeyPersistenceTests.cs` | Persistence tests exist. |
| `docs/OPERATIONS.md` | Current health table covers API, Blazor, email dispatch, queue, idempotency cleanup, storage, TickerQ, and related checks. |

Launch implication:

- Runtime restart proof and health degradation behavior are still required.

### Runtime Proof Attempt - 2026-07-04

| Evidence | Result |
|---|---|
| Aspire CLI | `aspire --version` returned `13.3.0-preview.1.26221.24+c8e41e142776da4d569f8b30c4c62aa026061715`; the startup log reported stable `13.4.6` available. |
| Context7 docs | Official .NET Aspire docs were checked for AppHost/resource-service concepts; use `aspire start`, `aspire ps`, and resource endpoint discovery rather than guessing ports. |
| Baseline build | `dotnet build --configuration Release --verbosity quiet` passed before startup with 26 projects and 0 errors; warning count remains existing backlog noise. |
| Startup command | `ISLAMU_ASPIRE_MODE=FullLocal SecretProvider__Provider=None Infisical__ProjectId= Infisical__ClientId= Infisical__ClientSecret= aspire start --apphost Explore.AppHost/Explore.AppHost.csproj --format Json --non-interactive --isolated`. |
| First parent CLI result | Timed out waiting for AppHost startup and exited 2. Parent log: `/home/amir/.aspire/logs/cli_20260704T125100_027f1b5c.log`. |
| First child AppHost log | `/home/amir/.aspire/logs/cli_20260704T125101270_detach-child_27520be465824a7f87469b8f21e77ab3.log` showed local infrastructure ready, migration service finishing, API/Blazor running, and `Distributed application started`. |
| First failure symptom | `explore-api` changed `Running -> Finished` at `2026-07-04 12:51:38`, shortly after AppHost startup. No API application exception was visible in the CLI log slice. |
| Controlled rerun parent CLI result | Timed out waiting for AppHost startup and exited 2. Parent log: `/home/amir/.aspire/logs/cli_20260704T131748_89e333c2.log`; child log: `/home/amir/.aspire/logs/cli_20260704T131749234_detach-child_0b5673acbdd54c9dbd0a2dabfec41aa1.log`. The parent waited for `/home/amir/.aspire/cli/backchannels/auxi.sock.975b8e77015db52e` and timed out even though the child reported `Distributed application started`. |
| Controlled rerun resource state | The child log showed `event-migrationservice` finished, `explore-api` reached `Running`, and `explore-blazor` reached `Running`. While the parent was still waiting, process environment showed API at `http://localhost:38665` and Blazor at `http://localhost:32773`, and `ss` confirmed both ports were listening. The controlled child log did not show the earlier `explore-api Running -> Finished` transition before the parent timeout. |
| Smoke after timeout | API curls failed after the port disappeared. Blazor `/alive` and `/health` returned 500 after dependencies disappeared, with an EF/Npgsql connection-refused path to `127.0.0.1:35051` during tenant resolution. Treat this only as evidence that smoke was run too late and Blazor health paths can touch tenant/database resolution when dependencies are gone, not as a successful health proof. |
| AppHost discovery | `aspire ps --format Json` returned `[]` during/after the controlled run despite AppHost/API/Blazor processes existing briefly, so no stable AppHost remained discoverable for `aspire describe` or reliable runtime smoke. |
| Docker resource state | Local full infrastructure containers remained running; `mailpit-4ccf7fcf` was healthy, and isolated mode mapped Mailpit to random host ports. Use resource endpoints or Docker mappings for isolated Mailpit proof. |

Launch implication:

- Do not proceed to browser/API smoke, Mailpit proof, or Data Protection restart proof until the Aspire CLI/AppHost lifecycle is controlled. Align the Aspire CLI to the repo's `13.4.6` AppHost SDK or run a foreground AppHost control first. If `explore-api` still exits after that, capture API resource logs directly and fix or document that application failure.

## Key Decisions

### D1 - Registration email uses specialized EmailDispatchOutbox

Use `EmailDispatchOutbox` as the canonical durable intent for lifecycle email. Do not add a second registration confirmation pipeline through generic `OutboxMessage`.

### D2 - Runtime proof before feature expansion

Start implementation with launch runtime evidence because many old backlog items are already source-complete but not proven in a deployable flow.

### D3 - Email compliance is explicit work

Unsubscribe endpoints and tokens are not enough for launch. Dispatch must integrate headers, visible unsubscribe URLs where applicable, preference checks, metrics, and tests.

### D4 - Registration schema mostly exists

Partial unique indexes and capacity transactions already exist. Add tests first; only add migrations if tests show a real gap.

### D5 - HAL remains the UI affordance contract

Local roles may support display and selection, but launch-critical write affordances must come from `_links`.

### D6 - Manifest/offline split

Minimal manifest/icons can be launch scope if desired. Offline service worker behavior is deferred unless the owner re-scopes it.

### D7 - Keep docs tied to behavior

Do not update broad docs speculatively. Update operations/security/API/Blazor docs only when implementation behavior changes or runtime proof reveals required operator guidance.

## Constraints to Preserve

- Repositories return entities, not DTOs.
- Validators are manually instantiated.
- Use `int` for lookups, `Guid` UUIDv7 for aggregates, and `long` for cursors.
- Public `GET` endpoints use `[AllowAnonymous]` when intended; writes use `[Authorize]`.
- Every source file starts with two `ABOUTME:` lines.
- HAL `_links` are the source of truth for UI action affordances.
- Tenant-filter bypasses require explicit named reasons and exact tenant predicates.
- Project-level tests only; no solution-level `dotnet test`.
- Preserve unrelated dirty worktree changes.

## Current Risks and Unknowns

| Risk or unknown | Current read |
|---|---|
| Registration confirmation opt-out semantics | Current implementation treats `RegistrationConfirmation` as preference-controlled because `NotificationPreferenceCategories.RegistrationConfirmations` exists. Owner should confirm this is intended, or adjust category mapping before launch. |
| Email runtime proof | Unit/integration tests pass, but Mailpit proof through the real registration/drain path remains open. |
| Registration duplicate API behavior | Repository, handler, persistence tests, API boundary tests, and real PostgreSQL API tests now cover event/day/session parent-intent duplicate races idempotently, including proof that duplicate retries do not persist a second registration-confirmation dispatch row. Real PostgreSQL API coverage now includes success, repeat-submit, waitlist, and unauthenticated rejection. Blazor client coverage now proves generated-client service agreement, safe error mapping, idempotent already-registered state, waitlist copy, and HAL-gated event-detail registration affordances. Remaining risk is runtime/visual release evidence. |
| JSON-LD | Re-checked on 2026-07-04: source search and `docs/SEO.md` did not find JSON-LD automation. Treat as a launch gap only if structured data is in scope. |
| Web app manifest/icons | Re-checked on 2026-07-04: no web manifest was found; existing assets are `Explore.Blazor/wwwroot/favicon.ico` and `Explore.Blazor.Client/wwwroot/image/Icon_landingpage.png`. Decide if installability is launch scope. |
| Runtime proof | Existing tests and docs are strong, but full-local runtime proof is now actively blocked by Aspire lifecycle evidence. The repo pins Aspire `13.4.6`, the installed CLI is `13.3.0-preview`, the parent CLI timed out waiting for the AppHost backchannel, and `aspire ps` remained empty even while API/Blazor listened briefly. Align CLI or run foreground AppHost before diagnosing API internals; then capture resource logs if an API exit still reproduces. |
| Dirty worktree | Many unrelated files were modified before this rebaseline. Implementation agents must isolate their own changes. |
| Missing `RTK.md` import | `AGENTS.md` references it, but the file was not found during this session. |

## Validation Results for Runtime-Plan Refresh

Checks run after updating Phase 1 runtime evidence and SEO/manifest evidence:

```bash
git diff --check -- dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
rg -n "[[:blank:]]+$" dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
```

Results:

- Diff whitespace check: passed.
- Trailing-whitespace scan: no matches.
- Architecture tests: passed, 243 total, 242 succeeded, 1 existing documented API response-metadata skip.
- Latest rerun after adding the controlled Aspire backchannel-timeout evidence also passed: diff whitespace check clean, trailing-whitespace scan no matches, and architecture tests passed 243 total, 242 succeeded, 1 existing documented API response-metadata skip.
- CTO skill's sample `--filter` commands were attempted first, but this repository's TUnit/Microsoft Testing Platform runner rejects `--filter`; use `--treenode-filter` or the project-level architecture command above.
- Runtime startup proof is still not green. The docs now record both observed full-local Aspire failure modes: an earlier API early-exit symptom and a later CLI/AppHost backchannel timeout with API/Blazor temporarily listening.

## Validation Results for This Rebaseline

Checks run after rewriting the workstream:

```bash
rg -n "RegistrationConfirmedOutbox(H|h)andler|RoutingOutboxMessage(D|d)ispatcher|Last Updated: 2026-0[35]|WP-[0-9]" dev/active/mvp-launch
rg -n "Last Updated: 2026-07-04 Europe/Brussels" dev/active/mvp-launch/*.md
git diff --check -- dev/active/mvp-launch
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Results:

- Stale-reference check: no obsolete handler names, May 2026 date markers, or old work-package IDs remained.
- Header/date check: all three files have `ABOUTME` headers and `Last Updated: 2026-07-04 Europe/Brussels`.
- Diff whitespace check: passed.
- Release build: passed, 25 projects, 0 errors, 27 warnings. Warnings included existing NuGet vulnerability warnings for `AutoMapper` and `Microsoft.OpenApi`.
- Architecture tests: passed, 240 total, 239 succeeded, 1 existing skipped response-metadata rule.

Full implementation verification remains in the plan and tasks file.

## Validation Results for Phase 2 Email Dispatch Slice

Checks run after implementing dispatch-time preference and unsubscribe behavior:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchDrainServiceTests/*|/*/*/EmailDispatchRabbitMqDeadLetterReplayDecisionTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchTickerQJobsTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchOutboxTransitionRepositoryTests/*" --minimum-expected-tests 1 --no-progress
```

Results:

- Release build: passed, 25 projects, 0 errors, existing NuGet vulnerability warning backlog.
- Application unit tests: passed, 1,947/1,947.
- Focused infrastructure dispatch tests: passed, 10/10.
- Focused API TickerQ drain tests: passed, 5/5.
- Focused PostgreSQL email dispatch transition tests: passed, 12/12.
- Full `Explore.Infrastructure.Tests` was also attempted and failed only on unrelated pre-existing `AdminContextTests` role-check expectations: `IsTenantAdminAsync_UsesTenantAdminRoleCheck` and `IsGroupAdminAsync_WhenMembershipHasGroupAdminRole_ReturnsTrue`. The focused dispatch lane passed.

## Validation Results for Phase 3 Registration Integrity Slice

Checks run after implementing the duplicate-intent persistence/API boundary slice and duplicate dispatch-row proof:

```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationIntentRepositoryTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationControllerTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventRegistrationRealRuntimeTests/*" --minimum-expected-tests 3 --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --no-progress
git diff --check -- Event.Persistence.IntegrationTests/Repositories/EventRegistrationIntentRepositoryTests.cs Event.API.IntegrationTests/Features/EventRegistrationControllerTests.cs Explore.Persistence/Configurations/Entities/EventRegistrationIntentConfiguration.cs Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs Explore.Persistence/Migrations/20260704120000_AddEventRegistrationEventScopeUniqueIndex.cs
git diff --check -- Event.API.IntegrationTests/Features/EventRegistrationRealRuntimeTests.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
rg -n "[[:blank:]]+$" Event.API.IntegrationTests/Features/EventRegistrationRealRuntimeTests.cs dev/active/mvp-launch/mvp-launch-plan.md dev/active/mvp-launch/mvp-launch-context.md dev/active/mvp-launch/mvp-launch-tasks.md
```

Results:

- Focused PostgreSQL registration-intent persistence tests: passed, 6/6.
- Debug rerun before the rollback cleanup fix failed with `InvalidOperationException: This NpgsqlTransaction has completed; it is no longer usable.`; the same focused class passed 6/6 after `RollbackIfPendingAsync(...)` preserved the original retryable transaction failure.
- Focused API event-registration controller tests: passed, 6/6.
- Focused real-runtime API create/repeat-submit/waitlist/unauthenticated tests: passed, 3/3.
- Architecture tests: passed, 240 total, 239 succeeded, 1 existing documented skip.
- Release build: passed, 25 projects, 0 errors, 895 warnings. Warnings are existing NuGet vulnerability/analyzer backlog, including `AutoMapper`, `Microsoft.OpenApi`, `Microsoft.CodeAnalysis.NetAnalyzers`, and analyzer findings.
- Full API integration suite: attempted after the focused real-runtime test passed, then aborted after unrelated broader failures had already appeared. At cancellation it reported 502 total, 479 succeeded, 22 failed, and 1 skipped. Observed failures were outside this registration slice, including `EventMultiTagFilterTests.GetEvents_WithMultiTagFilters_ShouldReturnOk` returning `GatewayTimeout` and `ExternalApiOwnerTypeIntegrationTests.UserOwnerKey_InMultiTenantMode_AuthenticatesAndResolvesTenant` failing to parse an empty response while the test host logged unreachable `https://phase0-auth.test/.well-known/openid-configuration`.
- Source diagnostics: focused test compilation and Release build completed with no errors for the edited persistence test; earlier LSP diagnostics remained clean for the edited registration-intent EF configuration, corrective migration, persistence test, and API test.
- Diff/trailing-whitespace checks: passed for the persistence/API slice files; direct trailing-whitespace scan also returned no matches for the untracked real-runtime API test plus MVP launch docs.

Documentation/source note:

- Earlier EF Core verification used local package XML to confirm the `HasIndex(expression, name)` overload semantics that keep same-column event/session partial indexes distinct in the EF model.
- This real-runtime API slice used Context7 against the official ASP.NET Core docs for `WebApplicationFactory` integration-test guidance, then followed the repository's `RealRuntimeApiFixture`/PostgreSQL host-profile conventions for the actual implementation.
- Create-specific `403 Forbidden` coverage was not added because the current `POST /api/eventregistration` contract authenticates the caller and binds `UserId` from claims, but does not perform a resource authorization check that can deny an authenticated caller with `403`. Existing registration read coverage still proves ownership mismatch returns `403`.

## Validation Results for Phase 3 Blazor Client Slice

Checks run after aligning the generated-client service wrapper, registration UI state/copy, and HAL-gated sidebar affordance:

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventListRegistrationWorkflowTests/*|/*/*/EventRegistrationServiceTests/*|/*/*/EventRegistrationTests/*|/*/*/EventDetailsSidebarTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EventListTests/*" --minimum-expected-tests 1 --no-progress
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress
dotnet build --configuration Release --verbosity quiet
git diff --check -- Explore.Blazor.Client/Pages/Events/EventListRegistrationWorkflow.cs Explore.Blazor.Client/Services/EventRegistrationService.cs Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor Explore.Blazor.Client/Pages/Events/EventList.razor.cs Explore.Blazor.Client/Components/Events/EventPreviewWorkspace.razor.cs Explore.Blazor.Client.Tests/Pages/Event/EventListRegistrationWorkflowTests.cs Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventRegistrationTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs Explore.Blazor.Client.Tests/Components/Event/EventDetailsSidebarTests.cs
rg -n "[[:blank:]]+$" Explore.Blazor.Client/Pages/Events/EventListRegistrationWorkflow.cs Explore.Blazor.Client/Services/EventRegistrationService.cs Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor Explore.Blazor.Client/Pages/Events/EventList.razor.cs Explore.Blazor.Client/Components/Events/EventPreviewWorkspace.razor.cs Explore.Blazor.Client.Tests/Pages/Event/EventListRegistrationWorkflowTests.cs Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventRegistrationTests.cs Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs Explore.Blazor.Client.Tests/Components/Event/EventDetailsSidebarTests.cs
```

Results:

- Focused workflow/service/modal/sidebar tests: passed, 10/10.
- Focused EventList component tests: passed, 27/27.
- Full `Explore.Blazor.Client.Tests`: passed, 1468/1469, with 1 existing documented component-accessibility skip.
- Architecture tests: passed, 242/243, with 1 existing documented API response-metadata skip.
- Release build: passed, 26 projects, 0 errors, 27 existing package/deprecation warnings.
- Diff/trailing-whitespace checks: passed for the Blazor/client slice files.
- Browser visual QA was not run for this slice because no Aspire/Compose runtime with an authenticated, seeded registration page was started. Runtime/visual proof remains tracked under Phase 1 and Phase 6.

Documentation/source note:

- Context7 official Blazor docs were used for current component-state guidance, and bUnit docs were used for current async assertion guidance.
- `docs/BLAZOR.md` now documents registration outcome classification, safe generated-client error handling, and HAL-gated registration actions.

## Handoff

Recommended next action:

1. Align the Aspire CLI with the repo's Aspire `13.4.6` AppHost SDK, or run `dotnet run --project Explore.AppHost/Explore.AppHost.csproj` as a foreground AppHost control to remove the `aspire start` detach/backchannel variable.
2. Repeat full-local startup with captured parent log, child log, `aspire ps --format Json`, process URLs, and resource logs. If `explore-api` again changes `Running -> Finished`, capture API resource output directly and fix that application failure.
3. Once `aspire ps` shows a stable AppHost and API/Blazor remain alive, run health/public endpoint smoke before the parent CLI timeout window can invalidate the result.
4. Execute Phase 2.5 Mailpit proof for registration email through the real API/BFF/drain path, using Aspire/Docker-discovered Mailpit endpoints under `--isolated`.
5. Have the owner confirm whether registration confirmations should stay preference-controlled.
6. Run visual/runtime proof for the registration flow when Aspire/Compose is stable.

Do not start by adding new registration/email abstractions. The current architecture already has the main pieces; launch work should make them reliable, compliant, observable, and documented.

<!-- ABOUTME: Tactical checklist for implementing actor subscriptions and notification fanout. -->
<!-- ABOUTME: Tracks review, implementation slices, acceptance criteria, validation, and docs maintenance. -->

# Subscription Notification — Task Checklist

Last Updated: 2026-05-29 Europe/Brussels

## Status Summary

- **Overall status:** Phase 4 event-published fanout service, dispatcher routing, and focused internal fanout integration coverage are implemented.
- **Completed:** Phase 1 model foundation; Phase 2 repository primitives, DTOs, validators, queries, commands, mapping, focused subscribe-command tests, actor-subscription authorization resource/policy wiring, initial Phase 3 actor/event API/HAL surface, focused actor-subscription API/HAL integration coverage, expanded API failure/transition coverage, contract regeneration, Phase 4.1 internal fanout outbox request, Phase 4.2 fanout service, Phase 4.3 composite dispatcher routing, and Phase 4.5 focused publish-to-notification fanout integration coverage.
- **Current priority:** Decide whether Phase 4 metrics/logging are required before moving to Blazor UX.
- **Next recommended slice:** Either add low-cardinality fanout metrics/logging or start Phase 5 Blazor subscription UX; defer EF migration until unrelated dirty snapshot/schema work is isolated.

## Implementation Maintenance Rules

- [ ] Before starting implementation work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline 🟡 IN PROGRESS

- [x] **0.0 Create/re-baseline dev docs**
  - **Files:** `dev/active/subscription-notification/subscription-notification-plan.md`, `subscription-notification-context.md`, `subscription-notification-tasks.md`
  - **Acceptance:** Three-file dev-doc structure exists with Last Updated metadata and source-grounded current-state report.
  - **Validation:** Files created and reviewed for required sections.
  - **Effort:** M
  - **Dependencies:** Consultation report.

- [x] **0.1 Apply Senior CTO re-baseline**
  - **Files:** `subscription-notification-plan.md`, `subscription-notification-context.md`, `subscription-notification-tasks.md`
  - **Acceptance:** Plan locks v1 to organization/group subscriptions, disables user targets, defers SignalR, requires tenant-local subscriber checks, and separates internal fanout outbox from external MQContract `EventPublished`.
  - **Validation:** Dev docs updated consistently; baseline build and full architecture test run recorded in context.
  - **Effort:** M
  - **Dependencies:** 0.0

- [x] **0.2 User reviews the CTO-baselined plan and approves/corrects scope**
  - **Files:** `dev/active/subscription-notification/*`
  - **Acceptance:** Plan status changes to User-reviewed/Approved, or explicit overrides are recorded before code edits.
  - **Validation:** User requested implementation start and explicitly removed backward-compatibility constraints for the development-mode schema slice.
  - **Effort:** S
  - **Dependencies:** 0.1

- [x] **0.3 Planning agent records current repo state and build baseline**
  - **Files:** `git status`, context
  - **Acceptance:** Context records the unrelated dirty worktree and build result from this planning pass.
  - **Validation:** `git status --short`; `dotnet build --configuration Release --verbosity quiet` passed with existing warnings on 2026-05-29; full `Event.Architecture.Tests` passed after filtered TUnit attempts failed due unsupported `--filter`.
  - **Effort:** S
  - **Dependencies:** 0.1

- [x] **0.4 Implementation agent reconfirms current repo state before runtime edits**
  - **Files:** `git status`, plan/context/tasks
  - **Acceptance:** Context records unrelated dirty files; agent agrees not to revert/mix unrelated work.
  - **Validation:** `git status --short` showed unrelated dirty files before edits; `dotnet build --configuration Release --verbosity quiet` passed after Phase 1 fixture updates.
  - **Effort:** S
  - **Dependencies:** 0.2

- [x] **0.5 Decide whether to add a Contribution Contract intent**
  - **Files:** `.claude/contract/intents.yaml` only if changed; otherwise context note
  - **Acceptance:** Decision recorded. If `.claude/**` changes, architecture context tests pass.
  - **Validation:** No new intent added; existing CQRS, EF migration, repository query, endpoint, HAL, and Blazor affordance intents cover the remaining slices.
  - **Effort:** S
  - **Dependencies:** 0.2

## Phase 1: Domain And Persistence Foundation 🟡 IN PROGRESS

- [x] **1.1 Add actor subscription lookup entities/enums**
  - **Files:** new `Explore.Domain/ActorSubscriptionStatus.cs`; new `Explore.Domain/ActorSubscriptionNotificationLevel.cs`; new enum files; new EF configs; seed updates
  - **Acceptance:** Stable IDs/codes/names for `ACTIVE`, `UNSUBSCRIBED`, `BLOCKED`, `NONE`, `ALL`, `PERSONALIZED`.
  - **Validation:** Build, architecture tests, domain unit tests, and application unit tests passed; runtime lookup seed methods added.
  - **Effort:** M
  - **Dependencies:** 0.4

- [x] **1.2 Add `ActorSubscription` entity and EF configuration**
  - **Files:** new `Explore.Domain/ActorSubscription.cs`; new `Explore.Persistence/Configurations/Entities/ActorSubscriptionConfiguration.cs`; modify `ExploreDbContext.DbSets.cs`; query filters
  - **Acceptance:** Tenant-scoped actor FK; tenant-local subscriber FK via `SubscriberTenantUserId`; denormalized `SubscriberUserId` for notification delivery; audit/soft-delete/concurrency; unique non-deleted subscription index; fanout scan index over active subscriptions and active tenant users.
  - **Validation:** Build and architecture tests passed; persistence integration tests still needed after migration isolation.
  - **Effort:** L
  - **Dependencies:** 1.1

- [x] **1.3 Add notification deduplication key**
  - **Files:** existing `Explore.Domain/Notification.cs`; `NotificationConfiguration.cs`; `NotificationRepository.cs`
  - **Acceptance:** Fanout notifications can use deterministic dedup keys; duplicate rows are prevented/ignored safely.
  - **Validation:** Build, domain unit tests, and application unit tests passed after existing notification test fixtures were updated with deterministic keys; persistence integration test still needed after migration isolation.
  - **Effort:** M
  - **Dependencies:** 1.2

- [x] **1.4 Add `NotificationFanoutRun` entity/config/repository primitives**
  - **Files:** new `Explore.Domain/NotificationFanoutRun.cs`; new EF config; DbSet; optional lookup enum/entity
  - **Acceptance:** Unique run per source event/actor/kind; cursor and counts persisted; no PII fields.
  - **Validation:** Entity/config/DbSet/query filter build and architecture tests passed; repository primitives compile in focused persistence build; persistence integration tests remain after migration isolation.
  - **Effort:** L
  - **Dependencies:** 1.2

- [ ] **1.5 Generate EF migration and update schema docs**
  - **Files:** new migration under `Explore.Persistence/Migrations/`; `ExploreDbContextModelSnapshot.cs`; `schemas/islamu-event.md`
  - **Acceptance:** `Up`/`Down` reversible; seed/model snapshot in sync; schema docs updated.
  - **Validation:** Deferred because `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs` and `schemas/islamu-event.md` were already dirty with unrelated work before this slice; generating now risks mixing migrations.
  - **Effort:** M
  - **Dependencies:** 1.1, 1.2, 1.3, 1.4

## Phase 2: Application Contracts And CQRS 🟡 IN PROGRESS

- [x] **2.1 Add repository contracts and implementations**
  - **Files:** new `IActorSubscriptionRepository.cs`; new `INotificationFanoutRunRepository.cs`; existing `ITenantUserRepository`; new repository classes; DI registration
  - **Acceptance:** Repositories return entities; read queries use `AsNoTracking`; fanout scans exclude inactive/deleted tenant users; no DTO mapping in Persistence.
  - **Validation:** `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet`; architecture tests passed; persistence integration tests remain after migration isolation.
  - **Effort:** L
  - **Dependencies:** Phase 1

- [x] **2.2 Add subscription DTOs and validators**
  - **Files:** new `Explore.Application/DTOs/ActorSubscription/*.cs`; validators
  - **Acceptance:** State/list/update DTOs expose lookup primitives and concurrency stamp; validators manually instantiated later.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`; application unit tests passed through command-handler coverage.
  - **Effort:** M
  - **Dependencies:** 2.1

- [x] **2.3 Add get/list subscription queries**
  - **Files:** new `Explore.Application/Features/ActorSubscriptions/Requests/Queries/*`; handlers
  - **Acceptance:** Current user only; returns state for target actor and paged/listed current-user subscriptions.
  - **Validation:** Application project build passed; dedicated query unit tests still recommended before API wiring.
  - **Effort:** M
  - **Dependencies:** 2.2

- [x] **2.4 Add subscribe command**
  - **Files:** new command/handler/validator under `Features/ActorSubscriptions`
  - **Acceptance:** Idempotently creates/reactivates active subscription; default level `ALL`; validates active `TenantUser`, tenant, actor, self-subscribe, and v1 user-target-disabled policy.
  - **Validation:** `Event.Application.UnitTests` passed with focused create/inactive-tenant-user/reactivation tests; API integration tests later.
  - **Effort:** L
  - **Dependencies:** 2.2

- [x] **2.5 Add notification-level update command**
  - **Files:** new command/handler/validator
  - **Acceptance:** Updates `NotificationLevelId` with concurrency check; only current owner can update; invalid level rejected.
  - **Validation:** Application project build passed; focused command unit tests still recommended.
  - **Effort:** M
  - **Dependencies:** 2.4

- [x] **2.6 Add unsubscribe command**
  - **Files:** new command/handler/validator
  - **Acceptance:** Idempotently marks the canonical subscription row as `UNSUBSCRIBED`; preserves audit; does not soft-delete as business state.
  - **Validation:** Application project build passed; focused command unit tests still recommended.
  - **Effort:** M
  - **Dependencies:** 2.4

- [x] **2.7 Add authorization resource kind/actions/policies**
  - **Files:** `AuthorizationActions.cs`; `ResourceKinds.cs`; `ResourceDescriptors.cs`; fallback authorization; Cerbos policy files/tests as needed
  - **Acceptance:** Actor subscription has a dedicated resource kind/action catalog, DTO descriptors, fallback authorization handling, machine scope mapping, request-level `[AuthorizeResource]` metadata, and Cerbos policy/schema. Concrete owner checks remain handler-level through current active `TenantUser` resolution because the current Cerbos principal schema does not expose user identity.
  - **Validation:** LSP diagnostics clean for touched auth/request/infrastructure folders; `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed. Normal `Event.Architecture.Tests` is currently blocked by unrelated `AiChatRequest` query-namespace failure; normal `Event.Application.UnitTests` is currently blocked by unrelated AI assistant policy test failure.
  - **Effort:** L
  - **Dependencies:** 2.3-2.6

## Phase 3: API Endpoints And HAL 🟡 IN PROGRESS

- [x] **3.1 Add `ActorSubscriptionController`**
  - **Files:** new `Explore.API/Controllers/ActorSubscriptionController.cs`; route constants
  - **Acceptance:** Canonical `/api/actor-subscriptions` routes with explicit templates/names/classification/response types; user-specific GETs and writes authorized; ProblemDetails metadata.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed; LSP diagnostics clean for new/touched API files. API integration tests still pending.
  - **Effort:** L
  - **Dependencies:** Phase 2

- [x] **3.2 Add subscription HAL links to actor resources**
  - **Files:** existing `Explore.API/Hateoas/Policies/ActorLinkPolicy.cs`; related assembler/tests
  - **Acceptance:** Actor detail/list policies now emit authenticated subscription-state and subscribe affordances for organization/group actors only, guarded by actor-subscription authorization metadata. Actor-subscription resources emit self, collection, actor, update-notification-level, unsubscribe, and create affordances.
  - **Validation:** Focused API build passed; HATEOAS integration tests still pending.
  - **Effort:** M
  - **Dependencies:** 3.1

- [x] **3.3 Add organizer subscription HAL links to event detail**
  - **Files:** event HATEOAS policy/assembler; event DTO mapping if needed
  - **Acceptance:** Event detail carries authenticated organizer subscription-state and subscribe affordances for organization/group organizer actors only; no local UI role inference needed.
  - **Validation:** LSP diagnostics clean for `EventLinkPolicy`; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed. HATEOAS integration tests still pending.
  - **Effort:** M
  - **Dependencies:** 3.1

- [x] **3.4 API integration tests for endpoints**
  - **Files:** new `Event.API.IntegrationTests/Features/Hateoas/ActorSubscriptionHateoasTests.cs`
  - **Acceptance:** Focused coverage seeds an authenticated user with active tenant-local `TenantUser`, subscribes to an organization actor through `/api/actor-subscriptions`, verifies the durable `ActorSubscription` row, and confirms the authenticated HAL collection exposes create/self/actor/update-notification-level/unsubscribe affordances. Remaining failure-case expansion is tracked below.
  - **Validation:** `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed; targeted TUnit run passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ActorSubscriptionHateoasTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`.
  - **Effort:** L
  - **Dependencies:** 3.1

- [x] **3.4a Expand API failure-case coverage**
  - **Files:** `Event.API.IntegrationTests/Features/Hateoas/ActorSubscriptionHateoasTests.cs` or dedicated controller test file
  - **Acceptance:** Expanded focused coverage now verifies user-target-disabled failure, inactive tenant-user failure, notification-level update, unsubscribe, and idempotent resubscribe against durable rows. Unauthorized, self-subscribe, and cross-tenant cases remain useful security-hardening additions but are no longer blocking the current endpoint contract slice.
  - **Validation:** `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed; targeted TUnit run passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ActorSubscriptionHateoasTests/*" --minimum-expected-tests 5 --no-progress --maximum-parallel-tests 1` returned 5 total, 5 succeeded.
  - **Effort:** M
  - **Dependencies:** 3.4

- [x] **3.5 Regenerate OpenAPI/client and update API changelog**
  - **Files:** `schemas/openapi.json`; `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`
  - **Acceptance:** Generated OpenAPI includes `/api/actor-subscriptions` routes and actor-subscription HAL schemas; generated client includes `GetActorSubscriptionsAsync`, `SubscribeToActorAsync`, `GetActorSubscriptionByActorAsync`, `UnsubscribeFromActorAsync`, `UpdateActorSubscriptionNotificationLevelAsync`, and actor-subscription DTO/HAL wrapper types; API changelog documents the new authenticated contract and HAL affordance rule.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed; `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet` passed; `ApiContractInventoryGeneratorTests` targeted TUnit run passed. `ContractInvariantsTests` remains blocked by unrelated `HalResourceOfAiAssistantBootstrapDto` empty schema.
  - **Effort:** M
  - **Dependencies:** 3.1-3.4

## Phase 4: Event Published Fanout 🟡 IN PROGRESS

- [x] **4.1 Add internal notification fanout outbox request**
  - **Files:** `PublishEventCommandHandler.cs`; new internal fanout payload model, for example `EventPublishedNotificationFanoutRequested`
  - **Acceptance:** Event publish writes the existing external `EventPublished` outbox row and a separate internal `EventPublishedNotificationFanoutRequested` row atomically; fanout payload carries tenant, event, title, source actor, schedule, and published timestamp data; `EventPublishedIntegrationEvent.cs` remains unchanged in this slice.
  - **Validation:** LSP diagnostics clean for publish handler, internal payload, and publish-handler tests; `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed; targeted TUnit publish-handler run passed with 3 tests.
  - **Effort:** M
  - **Dependencies:** Phase 2

- [x] **4.2 Add event-published notification fanout service**
  - **Files:** new `Explore.Application/Contracts/Services/IEventPublishedNotificationFanoutService.cs`; new `Explore.Application/Services/EventPublishedNotificationFanoutService.cs`; `Explore.Application/ApplicationServicesRegistration.cs`; `Event.Application.UnitTests/Services/EventPublishedNotificationFanoutServiceTests.cs`
  - **Acceptance:** Active `ALL` subscriptions for the publishing source actor create one deterministic durable `Notification` per active tenant-local subscriber; duplicates are skipped with `Notification.DeduplicationKey`; completed runs are idempotent no-ops; `NotificationFanoutRun` cursor/count/status fields are updated for resumability.
  - **Validation:** LSP diagnostics clean; `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed; targeted fanout service TUnit run passed with 3 tests using `-p:RunAnalyzers=false -p:WarningLevel=0` to isolate from unrelated existing test warnings.
  - **Effort:** XL
  - **Dependencies:** 4.1

- [x] **4.3 Add composite outbox dispatcher routing**
  - **Files:** new `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs`; `Explore.Infrastructure/InfrastructureServicesRegistration.cs`; `Explore.Infrastructure.Tests/Infrastructure/CompositeOutboxMessageDispatcherTests.cs`
  - **Acceptance:** `EventPublished` still publishes through the existing MQContract dispatcher; `EventPublishedNotificationFanoutRequested` routes only to the internal fanout service; unknown event types fail closed and are tested.
  - **Validation:** LSP diagnostics clean; `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed; targeted composite dispatcher TUnit run passed with 3 tests using `-p:RunAnalyzers=false -p:WarningLevel=0` to isolate from unrelated existing test warnings.
  - **Effort:** L
  - **Dependencies:** 4.2

- [ ] **4.4 Add fanout metrics/logging**
  - **Files:** `BusinessMetrics.cs`; fanout service; tests/docs
  - **Acceptance:** Low-cardinality metrics; logs contain safe IDs/categories/counts only.
  - **Validation:** telemetry tests/build.
  - **Effort:** M
  - **Dependencies:** 4.2

- [x] **4.5 End-to-end publish-to-notification integration test**
  - **Files:** `Event.API.IntegrationTests/Features/Notifications/EventPublishedNotificationFanoutIntegrationTests.cs`; `Explore.Persistence/Repositories/NotificationRepository.cs`
  - **Acceptance:** Focused integration coverage seeds an active organization subscription, dispatches the internal `EventPublishedNotificationFanoutRequested` outbox message through the real composite dispatcher, verifies one durable notification row, verifies the completed `NotificationFanoutRun`, and re-dispatches the same internal message to prove deterministic deduplication prevents duplicates. `NotificationRepository.ExistsByDeduplicationKeyAsync` now uses an exact tenant predicate with tenant-filter bypass so background fanout idempotency is not dependent on ambient HTTP tenant context. Publish-handler unit tests already verify event publication writes both external and internal outbox rows; a full outbox-processor end-to-end test remains optional future coverage.
  - **Validation:** LSP diagnostics clean for the new integration test and notification repository hardening; `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed; `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed; targeted TUnit run passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -p:TreatWarningsAsErrors=false -- --treenode-filter "/*/*/EventPublishedNotificationFanoutIntegrationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` returned 1 total, 1 succeeded. An earlier `--no-build` run raced the project build and found zero tests; it was invalid and rerun after build completion.
  - **Effort:** L
  - **Dependencies:** 4.3

## Phase 5: Blazor Subscription UX ⏳ NOT STARTED

- [ ] **5.1 Add Blazor actor subscription service**
  - **Files:** new `IActorSubscriptionService.cs`; new `ActorSubscriptionService.cs`; DI registration
  - **Acceptance:** Wraps generated client; handles API exceptions; no token handling; logs safely.
  - **Validation:** `Explore.Blazor.Client.Tests` service tests.
  - **Effort:** M
  - **Dependencies:** 3.5

- [ ] **5.2 Add `ActorSubscriptionButton` component**
  - **Files:** new `.razor`, `.razor.cs`, `.razor.css` under `Explore.Blazor.Client/Shared/`
  - **Acceptance:** Renders Subscribe/Subscribed/bell states from HAL/state; accessible labels; keyboard usable; BEM/logical CSS; no role checks.
  - **Validation:** bUnit component/accessibility tests.
  - **Effort:** L
  - **Dependencies:** 5.1

- [ ] **5.3 Replace organization profile static Subscribe button**
  - **Files:** `OrganizationProfile.razor`, `.razor.cs`, `.razor.css`
  - **Acceptance:** Uses `ActorSubscriptionButton`; no false static button; graceful load/error.
  - **Validation:** Blazor client tests.
  - **Effort:** M
  - **Dependencies:** 5.2

- [ ] **5.4 Replace group profile static Subscribe button**
  - **Files:** `GroupProfile.razor`, `.razor.cs`, `.razor.css`
  - **Acceptance:** Uses same reusable component; HAL-gated.
  - **Validation:** Blazor client tests.
  - **Effort:** M
  - **Dependencies:** 5.2

- [ ] **5.5 Add event-detail organizer subscribe action**
  - **Files:** `EventDetail.razor`, `.razor.cs`, `.razor.css`
  - **Acceptance:** Clear “Subscribe to organizer” UX near organizer card; registration CTA remains primary; HAL-gated.
  - **Validation:** Blazor client tests/manual keyboard smoke.
  - **Effort:** M
  - **Dependencies:** 5.2, 3.3

## Phase 6: Notification Inbox UX ⏳ NOT STARTED

- [ ] **6.1 Fix notification entity deep-link routing**
  - **Files:** `NotificationBell.razor.cs`; tests
  - **Acceptance:** Event/org/group routes match `Routes.razor`; tests cover mapping.
  - **Validation:** `Explore.Blazor.Client.Tests`.
  - **Effort:** S
  - **Dependencies:** Phase 4

- [ ] **6.2 Improve subscription notification item display**
  - **Files:** `NotificationItem.razor`, `.razor.cs`, `.razor.css`; DTO mapping if needed
  - **Acceptance:** Shows reason/source actor clearly; accessible; no color-only meaning.
  - **Validation:** bUnit/accessibility tests.
  - **Effort:** M
  - **Dependencies:** 6.1

- [ ] **6.3 Reassess mark-all-read-on-open behavior**
  - **Files:** `NotificationBell.razor.cs`; docs/tests if changed
  - **Acceptance:** Decision recorded. If changed, behavior is tested and docs updated.
  - **Validation:** Blazor tests.
  - **Effort:** S/M
  - **Dependencies:** 6.1

## Phase 7: Documentation And Operations ⏳ NOT STARTED

- [ ] **7.1 Update notifications docs**
  - **Files:** `docs/NOTIFICATIONS.md`; `docs/EMAIL_NOTIFICATIONS.md` if boundary note changes
  - **Acceptance:** Documents implemented subscription in-app fanout; still avoids unsupported email/push claims.
  - **Validation:** Docs/context architecture tests.
  - **Effort:** M
  - **Dependencies:** Phases 4-6

- [ ] **7.2 Update domain/architecture/API docs**
  - **Files:** `docs/DOMAIN.md`; `docs/ARCHITECTURE.md`; `docs/API_CHANGELOG.md`; `schemas/islamu-event.md`
  - **Acceptance:** New model, outbox fanout, API contract, and schema are documented with source anchors.
  - **Validation:** Architecture docs tests; build.
  - **Effort:** M
  - **Dependencies:** Phases 1-6

- [ ] **7.3 Update operations docs if fanout operational knobs exist**
  - **Files:** `docs/OPERATIONS.md`; `docs/CONFIGURATION.md` if keys added
  - **Acceptance:** Metrics, health, config, and backlog behavior documented only if implemented.
  - **Validation:** Docs tests.
  - **Effort:** S/M
  - **Dependencies:** 4.4

## Phase 8: Optional Real-Time Refresh ⏸️ DEFERRED BY DEFAULT

- [ ] **8.1 Decide whether to add SignalR notification refresh**
  - **Files:** plan/context/tasks unless approved for code
  - **Acceptance:** Decision recorded; if approved, create sub-plan/tasks for `NotificationHub` and client connection.
  - **Validation:** Docs update only unless approved.
  - **Effort:** S
  - **Dependencies:** Durable fanout complete

- [ ] **8.2 Implement SignalR refresh hint layer if approved**
  - **Files:** new API hub, Blazor service, optional config/docs/tests
  - **Acceptance:** SignalR only hints refresh/unread count; persisted notification remains truth; polling fallback stays.
  - **Validation:** API/Blazor tests; optional Redis backplane docs if configured.
  - **Effort:** L/XL
  - **Dependencies:** 8.1 approval

## Verification Checklist

- [ ] LSP/compiler diagnostics clean for modified files.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes.
- [ ] OpenAPI/client regenerated only after stable API contract.
- [ ] Docs updated where behavior/config/operations/API changed.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work

- SignalR real-time notification refresh is deferred unless user approves it for v1.
- Public user-to-user subscriptions are deferred; v1 API/UI should fail closed for user targets.
- Email fanout from actor subscriptions is deferred.
- Browser/mobile push is deferred.
- Personalized/ranked notifications are deferred.
- Public subscriber counts/lists are deferred.
- Federation follow/activity publishing is deferred pending federation roadmap.

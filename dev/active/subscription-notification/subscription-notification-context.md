<!-- ABOUTME: Operational memory for the actor subscription and notification fanout workstream. -->
<!-- ABOUTME: Tracks current status, decisions, key files, validation, risks, and handoff notes. -->

# Subscription Notification — Context

Last Updated: 2026-05-29 Europe/Brussels

## SESSION PROGRESS (2026-05-29 Europe/Brussels)

### ✅ COMPLETED

- CTO consultation created at `dev/active/subscription-notification/subscription-notification-consultation.md`.
- Implementation plan created at `dev/active/subscription-notification/subscription-notification-plan.md`.
- Senior CTO re-baseline applied to the implementation plan: v1 organizations/groups only, user-target subscriptions disabled, SignalR deferred, tenant-local subscriber identity required, and internal fanout outbox separated from external MQContract publishing.
- Current-state report completed with source evidence across Domain, Application, Persistence, API, Blazor, notification docs, and outbox docs.
- Task checklist created at `dev/active/subscription-notification/subscription-notification-tasks.md`.
- Relevant skills/rules loaded: Clean Architecture, CQRS/MediatR, EF Core, auth, Blazor UI/CSS/design-system/accessibility, outbox, and path-scoped repo rules.
- Baseline build on 2026-05-29 passed: `dotnet build --configuration Release --verbosity quiet` with existing warnings.
- User requested implementation start and explicitly allowed development-mode schema strictness without backward-compatibility scaffolding.
- Phase 1 model foundation added: actor subscription lookup entities/enums/configuration/seeding, `ActorSubscription`, required notification deduplication key, `NotificationFanoutRun`, DbSets, query filters, and EF configurations.
- Existing notification unit test fixtures were updated with deterministic `DeduplicationKey` values so the stricter required property compiles.
- Phase 1 verification passed after fixture updates: build, architecture tests, domain unit tests, and application unit tests completed successfully.
- Phase 2 repository/application slice added entity-returning actor subscription and fanout run repositories, deduplication lookup support on notifications, subscription DTOs, validators, AutoMapper profile, current-user get/list queries, subscribe/reactivate, notification-level update, and unsubscribe commands.
- Subscribe-command application tests cover create, inactive tenant-user failure, and unsubscribed-row reactivation.
- Phase 2.7 authorization slice added actor-subscription resource kind/actions, resource descriptors/registry mapping, machine scope mapping, fallback authorization handling, request-level `[AuthorizeResource]`/`ISecureRequest` metadata, and Cerbos policy/schema.
- Phase 3 API/HAL slice added `ActorSubscriptionController`, route names, HAL policies/assembler registration, HAL OpenAPI schema catalog entries, actor resource subscription affordance links for organization/group actors, and event-detail organizer subscription affordances.
- Focused Phase 3 API/HAL integration coverage added `ActorSubscriptionHateoasTests`, which seeds an authenticated active tenant-local user, subscribes to an organization actor through the API, verifies the durable `ActorSubscription` row, and verifies subscription HAL affordances on the authenticated collection response.
- Expanded actor-subscription API integration coverage now verifies user-target-disabled failure, inactive tenant-user failure, notification-level update, unsubscribe, and idempotent resubscribe durable-row behavior.
- Phase 3.5 contract regeneration completed: `schemas/openapi.json`, generated Blazor client, API contract inventory, and API changelog now include the actor-subscription endpoints and DTO/HAL schemas.
- Phase 4.1 internal fanout outbox request added: event publishing now writes the existing external `EventPublished` outbox message and a separate internal `EventPublishedNotificationFanoutRequested` outbox message in the same transaction.
- Phase 4.2/4.3 fanout service and dispatcher routing added: the internal fanout outbox message now routes to an idempotent notification fanout service, while external `EventPublished` messages continue through MQContract dispatch.
- Phase 4.5 focused publish-to-notification integration coverage added: the real composite dispatcher routes an internal fanout outbox message to the fanout service, creates one durable notification for an active organization subscriber, completes the fanout run, and remains idempotent on duplicate dispatch.
- `NotificationRepository.ExistsByDeduplicationKeyAsync` now uses the tenant-filter bypass helper with an exact tenant predicate so background fanout dedupe checks do not depend on ambient HTTP tenant context.

### 🟡 IN PROGRESS

- Phase 4 event-published notification fanout is in progress. Fanout service, dispatcher routing, and focused internal fanout integration coverage are implemented; next runtime decision is whether low-cardinality metrics/logging are required before Phase 5 Blazor UX. Phase 2 still has deferred focused update/unsubscribe/query unit tests, persistence integration tests, and EF migration/schema documentation once unrelated dirty migration state is isolated.

### ⏭️ NEXT

1. Decide whether to add Phase 4.4 low-cardinality metrics/logging now or move directly to Phase 5 Blazor subscription UX.
2. Unless overridden, v1 scope remains organizations/groups only, public user subscriptions disabled, SignalR deferred, and fanout driven by a separate internal outbox event.
3. Add additional security-hardening API tests for unauthorized, self-subscribe, and cross-tenant cases when touching endpoint authorization again.
4. Add focused query/update/unsubscribe command tests when touching that surface again.
5. Add persistence tests for subscription uniqueness, tenant filters, deduplication, and fanout run idempotency once migration state is clean.
6. Keep this context and `subscription-notification-tasks.md` updated after every meaningful implementation slice.

### ⚠️ BLOCKERS

- User may still override the CTO-baselined v1 scope before coding; absent override, implementation should proceed with the locked decisions above.
- Working tree contains many unrelated modified/untracked files from other workstreams. Implementation agents must not revert or mix unrelated changes.
- EF migration generation is deferred because `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs` and `schemas/islamu-event.md` were already dirty before this implementation slice.

## Quick Resume

1. Read `dev/active/subscription-notification/subscription-notification-plan.md`.
2. Read `dev/active/subscription-notification/subscription-notification-tasks.md`.
3. Confirm there is no user override to the CTO-baselined v1 scope.
4. Run `git status --short` and record unrelated dirty files before editing.
5. Start from the first unchecked high-priority task in the checklist.
6. Update plan/context/tasks after each meaningful slice.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Domain/Actor.cs` | Existing | Domain | Common actor identity for users, organizations, groups, bots, system. | Subscription target should be `Actor`. |
| `Explore.Domain/Enums/ActorTypeEnum.cs` | Existing | Domain | Actor type IDs: User=1, Organization=2, Bot=3, Group=4, System=5. | Used for target type validation. |
| `Explore.Domain/Event.cs` | Existing | Domain | Event aggregate with `ActorId` publisher. | Event publish fanout uses this actor. |
| `Explore.Domain/Notification.cs` | Existing | Domain | User-owned in-app inbox row. | Now requires `DeduplicationKey` for fanout idempotency. |
| `Explore.Domain/TenantUser.cs` | Existing | Domain | Tenant-local user participation/status. | Actor subscriptions must require active tenant-local subscriber state. |
| `Explore.Domain/UserNotificationPreference.cs` | Existing | Domain | Category-level notification preferences. | Not sufficient for per-actor bell state. |
| `Explore.Domain/ActorSubscription.cs` | New | Domain | Durable user-to-actor subscription. | Phase 1. |
| `Explore.Domain/NotificationFanoutRun.cs` | New | Domain | Resumable fanout progress/run state. | Recommended for enterprise-grade fanout. |
| `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs` | Existing | Application | Publishes events and writes `EventPublished` outbox message. | Do not fan out directly inside transaction. |
| `Explore.Application/Models/IntegrationEvents/EventPublishedIntegrationEvent.cs` | Existing | Application | MQContract integration event for published event. | Keep stable in v1; do not change it for local fanout. |
| `Explore.Application/Models/InternalEvents/EventPublishedNotificationFanoutRequested.cs` | New | Application | Internal outbox payload for subscription fanout. | Carries tenant/event/source actor context without changing MQContract. |
| `Explore.Application/Contracts/Services/IEventPublishedNotificationFanoutService.cs` | New | Application | Fanout service contract. | Dispatch target for the internal outbox event. |
| `Explore.Application/Services/EventPublishedNotificationFanoutService.cs` | New | Application | Creates idempotent durable notification rows for eligible subscribers. | Uses subscription, notification, and fanout-run repositories. |
| `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` | New | Infrastructure | Routes external MQ events and internal fanout events. | Preserves MQContract dispatch for `EventPublished`; routes fanout internally. |
| `Event.API.IntegrationTests/Features/Notifications/EventPublishedNotificationFanoutIntegrationTests.cs` | New | API tests | Focused internal fanout integration coverage. | Dispatches internal outbox event through real composite dispatcher and verifies notification/run idempotency. |
| `Explore.Application/Contracts/Persistence/IActorSubscriptionRepository.cs` | New | Application | Entity-returning subscription persistence contract. | Repositories must not return DTOs. |
| `Explore.Application/Contracts/Persistence/INotificationFanoutRunRepository.cs` | New | Application | Entity-returning fanout run persistence contract. | Supports idempotent fanout run lookup and worker polling. |
| `Explore.Application/Features/ActorSubscriptions/**` | New | Application | Commands/queries/handlers for state and mutations. | Manual validators, CT propagation. |
| `Explore.Application/Authorization/ResourceKinds.cs` and related auth catalog files | Existing | Application/Auth | Actor subscription authorization resource/action descriptors. | Phase 2.7 added `islamuevent_actor_subscription`. |
| `cerbos/policies/islamuevent_actor_subscription.yaml` | New | Auth policy | Cerbos policy for actor subscription actions. | Handlers enforce concrete current-user ownership. |
| `Explore.API/BackgroundServices/OutboxProcessor.cs` | Existing | API | Polls and dispatches outbox messages. | At-least-once; fanout must be idempotent. |
| `Explore.Infrastructure/Messaging/MqContractOutboxMessageDispatcher.cs` | Existing | Infrastructure | Dispatches `EventPublished` to MQContract messaging. | Preserve behavior; composite dispatcher routes internal fanout separately. |
| `Explore.API/Controllers/ActorSubscriptionController.cs` | New | API | Subscription endpoints. | Explicit routes/names/classification/response types. |
| `Explore.API/Hateoas/Policies/ActorLinkPolicy.cs` | Existing | API/HAL | Actor affordances. | Add subscription links. |
| `Explore.API/Hateoas/Policies/ActorSubscriptionLinkPolicy.cs` | New | API/HAL | Actor subscription resource affordances. | Emits self, actor, update, unsubscribe, and create links. |
| `Explore.API/Hateoas/Assemblers/ActorSubscriptionResourceAssembler.cs` | New | API/HAL | HAL assembler for actor subscription DTOs. | Registered in API DI. |
| Event HATEOAS policy files | Existing | API/HAL | Event affordances. | Add organizer subscription links. |
| `Event.API.IntegrationTests/Features/Hateoas/ActorSubscriptionHateoasTests.cs` | New | API tests | Focused runtime coverage for actor subscription API/HAL. | Seeds active `TenantUser`, subscribes to org actor, verifies persistence and HAL affordances. |
| `schemas/openapi.json` | Existing/generated | API contract | Canonical OpenAPI document. | Regenerated with actor-subscription paths and HAL schemas. |
| `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Existing/generated | Blazor client | NSwag-generated API client. | Regenerated with actor-subscription methods and DTO/HAL types. |
| `docs/API_CONTRACT_INVENTORY.md` | Existing/generated | Docs | Live OpenAPI inventory. | Regenerated; actor-subscription operations listed. |
| `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor` | Existing | Blazor | Organization profile with static Subscribe button. | Replace with reusable component. |
| `Explore.Blazor.Client/Pages/Groups/GroupProfile.razor` | Existing | Blazor | Group profile with static Subscribe button. | Replace with reusable component. |
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | Existing | Blazor | Event detail and organizer card. | Add organizer subscribe action. |
| `Explore.Blazor.Client/Layout/NotificationBell.razor.cs` | Existing | Blazor | Polling notification bell and deep-link mapping. | Fix org/group route mapping. |
| `Explore.Blazor.Client/Shared/ActorSubscriptionButton.razor` | New | Blazor | HAL-aware Subscribe/Subscribed/bell UI. | Must be accessible and BEM-styled. |
| `docs/NOTIFICATIONS.md` | Existing | Docs | Current in-app notification lifecycle. | Update only after fanout implemented. |
| `docs/API_CHANGELOG.md` | Existing | Docs | API contract changes. | Required for new endpoints. |
| `schemas/islamu-event.md` | Existing | Schema docs | Database schema documentation. | Required after migration. |

## Key Decisions

1. **Canonical target is `Actor`.** Avoid per-type subscription tables.
2. **Subscription and bell setting are separate.** Store bell level on `ActorSubscription`.
3. **V1 delivery is in-app durable notification rows.** Email, push, and SignalR are not delivery truth.
4. **Fanout is outbox-driven on a separate internal event.** Event publication writes the external `EventPublished` outbox row and an internal `NotificationFanoutRequested` row in the same transaction.
5. **Deduplication is mandatory.** Add deterministic `Notification.DeduplicationKey` and unique index.
6. **Preserve existing MQ dispatch.** Do not replace `MqContractOutboxMessageDispatcher` with notification-only logic.
7. **HAL links gate UI actions.** Blazor must not infer subscribe/edit/delete permissions from local roles/claims.
8. **Tenant-local subscriber state is mandatory.** Active `TenantUser` state gates subscription commands and fanout eligibility.
9. **Public user subscriptions are privacy-sensitive.** V1 is organizations/groups only; user targets fail closed unless a future plan adds privacy controls.
10. **SignalR is deferred.** Polling remains the v1 refresh path; persisted notifications are the source of truth.

## Constraints And Rules To Remember

- Every new file starts with two `ABOUTME:` lines.
- Repositories return entities, never DTOs.
- Validators are manually instantiated in handlers/services.
- `Guid` for aggregates/durable rows, `int` for lookup IDs, `long` only for cursors/size.
- Writes are authorized; user-specific subscription state is authenticated.
- Tenant isolation must be enforced by FKs, filters, and handler checks.
- Active tenant-local user state must be checked before subscription writes and during fanout.
- No direct external side effects inside transaction lambdas.
- UI actions are HAL-link-gated only.
- CSS uses BEM and logical properties; no physical left/right CSS.
- Do not use solution-level `dotnet test`; run project tests individually.

## Validation Baseline

Required before claiming full implementation complete:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Docs-only planning validation performed in this session:

- Baseline build: `dotnet build --configuration Release --verbosity quiet` passed on 2026-05-29 with existing warnings.
- Architecture verification: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed on 2026-05-29 (181 total, 180 succeeded, 1 documented skip). Earlier filtered attempts with `--filter` failed because this TUnit project does not support that option.
- Planning docs created/updated; no runtime code changed in this session.

Implementation validation performed in this session:

- `dotnet build --configuration Release --verbosity quiet` passed after Phase 1 model/schema changes and notification fixture updates.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 181 total, 180 succeeded, 1 documented skip.
- `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed: 285 total, 285 succeeded.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 1094 total, 1094 succeeded.
- Tavily and Context7 were invoked as requested, but both were quota-blocked by provider limits; implementation proceeded from repository source-of-truth docs and loaded skills/rules.
- EF migration generation intentionally not run because the migration snapshot and schema docs were already dirty with unrelated work.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed after Phase 2 DTO/CQRS additions.
- `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet` passed after repository additions.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed after subscribe-command tests: 1100 total, 1100 succeeded.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed after Phase 2 additions: 181 total, 180 succeeded, 1 documented skip.
- A full solution build attempt during Phase 2 was blocked by unrelated dirty-worktree failures outside actor subscriptions; focused Application, Persistence, Architecture, and Application Unit test validation passed for this slice.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after Phase 2.7 authorization wiring: 2 projects, 0 errors, 133 existing warnings.
- Phase 2.7 LSP diagnostics were clean for `Explore.Application/Authorization`, `Explore.Application/Features/ActorSubscriptions/Requests`, and `Explore.Infrastructure/Services`.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` is currently blocked by unrelated `Explore.Application.Contracts.Infrastructure.Ai.AiChatRequest` failing `Queries_ShouldResideIn_QueriesNamespace`.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` is currently blocked by unrelated `Handle_WithAiAssistantPolicy_IncludesAvailabilityAndAnonymousAccess` failure.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed after Phase 3 actor subscription API/HAL additions: 7 projects, 0 errors, 405 existing warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed after event organizer subscription HAL additions: 7 projects, 0 errors, 649 existing warnings.
- Phase 3 LSP diagnostics were clean for `ActorSubscriptionController`, actor-subscription HAL policy/assembler, `ActorLinkPolicy`, HATEOAS registration, and HAL OpenAPI catalog changes.
- Phase 3 LSP diagnostics were clean for `EventLinkPolicy` after organizer subscription HAL additions.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after `ActorSubscriptionHateoasTests` was added: 8 projects, 0 errors, 14 existing warnings.
- Targeted TUnit actor-subscription API/HAL test passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ActorSubscriptionHateoasTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` returned 1 total, 1 succeeded. Earlier VSTest `--filter` and category-filter attempts were invalid for this TUnit project; docs confirm class-name `--treenode-filter` syntax is required.
- LSP diagnostics were clean for expanded `ActorSubscriptionHateoasTests` coverage.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after expanded actor-subscription API tests.
- Targeted TUnit actor-subscription API test class passed after expanded failure/transition coverage: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/ActorSubscriptionHateoasTests/*" --minimum-expected-tests 5 --no-progress --maximum-parallel-tests 1` returned 5 total, 5 succeeded.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed during Phase 3.5 OpenAPI regeneration: 7 projects, 0 errors.
- `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet` passed during NSwag client regeneration: 2 projects, 0 errors, 1 existing warning.
- Targeted `HateoasLinkDeserializationTests` passed: 4 total, 4 succeeded.
- Targeted `ApiContractInventoryGeneratorTests` passed and refreshed `docs/API_CONTRACT_INVENTORY.md`: 1 total, 1 succeeded.
- Targeted `ContractInvariantsTests` is blocked by an unrelated AI HAL schema issue: `HalResourceOfAiAssistantBootstrapDto` is an empty public HAL detail schema.
- Phase 4.1 LSP diagnostics were clean for `PublishEventCommandHandler`, `EventPublishedNotificationFanoutRequested`, and publish-handler unit tests.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after Phase 4.1 internal fanout outbox changes.
- Targeted publish-handler tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false -- --treenode-filter "/*/*/PublishEventCommandHandlerTests/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1` returned 3 total, 3 succeeded.
- Phase 4.2/4.3 LSP diagnostics were clean for fanout service, composite dispatcher, DI registration changes, and new focused tests.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after fanout service changes.
- `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after composite dispatcher registration changes.
- Targeted fanout service tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/EventPublishedNotificationFanoutServiceTests/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1` returned 3 total, 3 succeeded.
- Targeted composite dispatcher tests passed: `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/CompositeOutboxMessageDispatcherTests/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1` returned 3 total, 3 succeeded.
- Initial targeted Phase 4.2/4.3 test runs without `RunAnalyzers=false`/`WarningLevel=0` were blocked by unrelated existing analyzer/nullable warnings in other test classes; the implementation projects and targeted test classes were then verified with analyzer suppression for test-project compilation.
- Phase 4.5 LSP diagnostics were clean for `Event.API.IntegrationTests/Features/Notifications/EventPublishedNotificationFanoutIntegrationTests.cs` and `Explore.Persistence/Repositories/NotificationRepository.cs`.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after the publish-to-notification integration test was added.
- `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after notification dedupe tenant-filter hardening.
- Targeted Phase 4.5 integration test passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -p:TreatWarningsAsErrors=false -- --treenode-filter "/*/*/EventPublishedNotificationFanoutIntegrationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` returned 1 total, 1 succeeded.
- An earlier Phase 4.5 `--no-build` run raced the API integration project build and found zero tests; it was invalid and rerun successfully after build completion.

## Current Known Risks / Unknowns

- **R1:** Fanout composition with existing MQ dispatcher is the highest-risk code path; use a separate internal outbox event and composite dispatcher routing. Owner tasks: 4.1, 4.2, 4.3.
- **R2:** Notification duplicates under outbox retry if dedup is skipped. Owner tasks: 1.3, 4.2.
- **R3:** User-to-user subscriptions can create privacy/abuse risk. V1 disables them. Owner tasks: 2.3, 3.1, 3.4/security review.
- **R4:** Static profile Subscribe buttons currently mislead users. Owner tasks: 5.2, 5.3.
- **R5:** Notification deep-link route mismatch can break navigation. Owner task: 6.1.
- **R6:** Existing working tree has unrelated changes; scope hygiene is mandatory. Owner task: 0.4.
- **R7:** Suspended/banned/removed tenant users could receive notifications if fanout checks only `UserId`. Owner tasks: 1.2, 2.1, 4.2.

## Handoff Notes

### Handoff — 2026-05-29 Europe/Brussels

- **Current state:** Consultation and dev-docs implementation plan/context/tasks are created and CTO re-baselined. No subscription runtime implementation started.
- **Next action:** User reviews the CTO-baselined scope or implementation starts with Phase 1 domain/persistence if accepted.
- **Blockers:** No implementation blocker if CTO baseline is accepted; unrelated dirty working tree exists.
- **Modified files:** `dev/active/subscription-notification/subscription-notification-plan.md`, `subscription-notification-context.md`, `subscription-notification-tasks.md`; prior consultation file exists.
- **Validation:** Docs-only work. Baseline build passed on 2026-05-29 with existing warnings. Full `Event.Architecture.Tests` passed; filtered `--filter` attempts failed due unsupported TUnit option and were replaced by the full project run.
- **Documentation impact:** Dev docs updated. Canonical docs should wait until implementation changes behavior.
- **Risks:** Main implementation risk is idempotent fanout composition without coupling local fanout failure to external MQContract publishing.
- **Notes for next contributor/agent:** Do not implement SignalR first. Build durable actor subscription + internal notification fanout first. Preserve HAL gating, tenant-local subscriber checks, and repository entity-return rules.

### Implementation Handoff — 2026-05-29 Europe/Brussels

- **Current state:** Phase 1 model/schema foundation compiles. New durable subscription and fanout run entities exist with EF configuration, lookup seeding, DbSets, and query filters.
- **Modified runtime/test files:** `Explore.Domain/ActorSubscription*.cs`, `Explore.Domain/NotificationFanoutRun.cs`, `Explore.Domain/Notification.cs`, new EF configs under `Explore.Persistence/Configurations/Entities/`, `Explore.Persistence/ExploreDbContext.DbSets.cs`, `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `Explore.Persistence/Seed/LookupTableSeeder.cs`, and notification test fixtures requiring `DeduplicationKey`.
- **Validation:** Build, architecture tests, domain unit tests, and application unit tests passed with existing warnings/skips noted above.
- **Blocked/deferred:** EF migration and schema docs are deferred due unrelated pre-existing dirty `ExploreDbContextModelSnapshot.cs` and `schemas/islamu-event.md` state.
- **Next action:** Add repository contracts/implementations and CQRS handlers, or first isolate/generate a clean EF migration if schema work must be completed before application code.

### Implementation Handoff — Phase 2 Application Slice — 2026-05-29 Europe/Brussels

- **Current state:** Repository primitives and application CQRS surface for actor subscriptions are implemented. Current authenticated users resolve through active tenant-local `TenantUser`; v1 targets are organization/group actors only; subscribe is idempotent create/reactivate; notification-level updates and unsubscribe use concurrency stamps; repositories return entities.
- **Modified runtime/test files:** New actor subscription DTOs/validators/profile, requests/handlers under `Explore.Application/Features/ActorSubscriptions/**`, repository contracts, persistence repositories, DI registration, notification repository dedup lookup, and subscribe-command unit tests.
- **Validation:** Focused Application build passed, focused Persistence build passed, Architecture tests passed, and Application Unit Tests passed with 1100 successes.
- **Blocked/deferred:** EF migration/schema docs remain deferred due unrelated dirty migration snapshot/schema state. Full solution build is not a reliable slice gate right now because unrelated analyzer/test failures exist outside the actor-subscription work.
- **Next action:** Add Phase 2.7 authorization resource/action/policy coverage, then Phase 3 API controller and HAL links.

### Implementation Handoff — Phase 2.7 Authorization Slice — 2026-05-29 Europe/Brussels

- **Current state:** Actor subscriptions now have an authorization resource kind (`islamuevent_actor_subscription`), action catalog entries, resource descriptors/registry mapping, machine scope mapping, fallback authorization handling, request-level `[AuthorizeResource]` metadata, and Cerbos policy/schema.
- **Ownership model:** Cerbos/fallback authorizes the actor-subscription resource for authenticated callers similarly to notifications; command/query handlers still enforce concrete ownership by resolving the current user to an active tenant-local `TenantUser` and reading only that subscriber's rows.
- **Modified auth files:** `Explore.Application/Authorization/ResourceKinds.cs`, `AuthorizationActions.cs`, `ResourceDescriptors.cs`, `ResourceDescriptorRegistry.cs`, `MachineScopeMapping.cs`, actor subscription request classes, fallback authorization service files, and new Cerbos policy/schema files.
- **Validation:** Warning-relaxed Application build passed and LSP diagnostics were clean for touched auth/request/infrastructure folders.
- **Blocked/deferred:** Normal Architecture and Application Unit test runs are currently blocked by unrelated AI workstream failures noted in the validation section above. API authorization tests remain pending until Phase 3 endpoints exist.
- **Next action:** Implement `ActorSubscriptionController` and HAL affordances, then add API integration tests for auth, subscribe, unsubscribe, notification-level update, self-subscribe/cross-tenant/inactive-user/user-target-disabled cases.

### Implementation Handoff — Phase 3 API/HAL Slice — 2026-05-29 Europe/Brussels

- **Current state:** Initial actor-subscription API/HAL surface is implemented. `ActorSubscriptionController` exposes authenticated current-user list/state/subscribe/update-notification-level/unsubscribe routes under `/api/actor-subscriptions`, all backed by MediatR handlers and named routes.
- **HAL model:** Actor detail/list resources now expose authenticated `subscription` and `subscribe` affordances only for organization/group actors. Event details expose authenticated organizer subscription-state and subscribe affordances for organization/group organizers. Actor-subscription HAL resources expose self, collection, target actor, update-notification-level, unsubscribe, and collection create links with actor-subscription permission metadata.
- **Modified API files:** `Explore.API/Controllers/ActorSubscriptionController.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.API/Hateoas/Policies/ActorLinkPolicy.cs`, `ActorSubscriptionLinkPolicy.cs`, `EventLinkPolicy.cs`, `ActorSubscriptionResourceAssembler.cs`, `Explore.API/Extensions/HateoasAssemblerRegistration.cs`, and `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`.
- **Validation:** LSP diagnostics clean for touched API files; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed with existing warnings after both actor and event HAL changes.
- **Blocked/deferred:** API integration tests, generated OpenAPI/client refresh, and API changelog remain pending. Architecture/Application Unit test blockers from unrelated AI workstream remain unchanged.
- **Next action:** Add API integration tests for auth, list/state, subscribe, unsubscribe, notification-level update, self-subscribe/cross-tenant/inactive-user/user-target-disabled cases, then regenerate OpenAPI/client once endpoint contract is ready.

### Implementation Handoff — Phase 3 API Failure/Transition Tests — 2026-05-29 Europe/Brussels

- **Current state:** Actor-subscription API integration coverage now includes happy-path subscribe/HAL, user-target-disabled failure, inactive tenant-user failure, notification-level update, unsubscribe, and idempotent resubscribe over the same durable subscription row.
- **Modified test file:** `Event.API.IntegrationTests/Features/Hateoas/ActorSubscriptionHateoasTests.cs`.
- **Validation:** LSP diagnostics clean; API integration test project build passed with warning-as-error disabled; targeted TUnit class run passed with 5 executed tests.
- **Blocked/deferred:** Full API integration project remains too slow/unstable in the current dirty workspace; unauthorized, self-subscribe, and cross-tenant security-hardening cases are still useful follow-up tests but are not blocking OpenAPI/client regeneration.
- **Next action:** Regenerate OpenAPI/client and update `docs/API_CHANGELOG.md`, then move into Phase 4 internal notification fanout.

### Implementation Handoff — Phase 3 API/HAL Integration Slice — 2026-05-29 Europe/Brussels

- **Current state:** Focused API/HAL runtime coverage exists for the primary subscribe/list affordance path. The test seeds the default tenant, a user actor, an active `TenantUser`, and an organization target actor; posts `SubscribeToActorDto` to `/api/actor-subscriptions`; verifies the persisted `ActorSubscription`; then reads `/api/actor-subscriptions` and verifies collection/item HAL affordances.
- **Modified test files:** `Event.API.IntegrationTests/Features/Hateoas/ActorSubscriptionHateoasTests.cs`.
- **Validation:** LSP diagnostics clean for the new test file. API integration test project build passed with warning-relaxed analyzer settings. Targeted TUnit class run passed with 1 test.
- **Blocked/deferred:** Full API integration project run timed out in the current dirty workspace before producing a useful result; use targeted TUnit filters for this lane until broader API suite runtime is isolated. Remaining failure cases are not yet covered.
- **Next action:** Add focused failure-case tests for unauthorized, inactive tenant user, self-subscribe, user-target-disabled, cross-tenant, unsubscribe, update notification level, and idempotent resubscribe; then regenerate OpenAPI/client and update `docs/API_CHANGELOG.md`.

### Implementation Handoff — Phase 3.5 Contract Slice — 2026-05-29 Europe/Brussels

- **Current state:** The actor-subscription API contract is regenerated and documented. `schemas/openapi.json` contains `/api/actor-subscriptions` list/subscribe routes, actor-target state/unsubscribe routes, notification-level update route, actor-subscription DTO schemas, and actor-subscription HAL wrapper schemas. The generated Blazor client contains the matching methods and DTO/HAL types.
- **Modified generated/docs files:** `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CONTRACT_INVENTORY.md`, and `docs/API_CHANGELOG.md`.
- **Validation:** API build passed, Blazor client build passed, HAL link deserialization tests passed, and API contract inventory generation passed. Contract invariants are blocked by an unrelated AI HAL schema issue (`HalResourceOfAiAssistantBootstrapDto`).
- **Blocked/deferred:** Full API integration and contract invariant suites are not clean in the current dirty workspace because of unrelated AI workstream failures. EF migration/schema docs remain deferred due pre-existing dirty migration snapshot/schema files.
- **Next action:** Start Phase 4 by adding a separate internal notification fanout outbox request from `PublishEventCommandHandler`, while leaving the external `EventPublishedIntegrationEvent` unchanged.

### Implementation Handoff — Phase 4.1 Internal Fanout Outbox Request — 2026-05-29 Europe/Brussels

- **Current state:** Publishing a ready draft event now creates two general outbox rows in the same transaction: the existing external `EventPublished` message for MQContract dispatch and a new internal `EventPublishedNotificationFanoutRequested` message for subscription notification fanout. The external `EventPublishedIntegrationEvent` record remains unchanged.
- **Internal payload:** `EventPublishedNotificationFanoutRequested` carries `TenantId`, `EventId`, `EventTitle`, `SourceActorId`, `StartDate`, optional `EndDate`, and `PublishedAt`. This gives the future fanout service enough tenant/source actor/event context without requiring the MQContract payload to change.
- **Modified runtime/test files:** `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`, `Explore.Application/Models/InternalEvents/EventPublishedNotificationFanoutRequested.cs`, and `Event.Application.UnitTests/Features/Events/Commands/PublishEventCommandHandlerTests.cs`.
- **Validation:** LSP diagnostics clean; warning-relaxed Application build passed; targeted publish-handler unit tests passed with 3 tests. Existing unrelated AI workstream blockers and deferred EF migration/schema state remain unchanged.
- **Next action:** Implement the fanout service and composite dispatcher route so `EventPublishedNotificationFanoutRequested` creates idempotent durable `Notification` rows for eligible active actor subscribers while `EventPublished` continues to publish externally through MQContract.

### Implementation Handoff — Phase 4.2/4.3 Fanout Service And Dispatcher — 2026-05-29 Europe/Brussels

- **Current state:** `EventPublishedNotificationFanoutRequested` now has an internal dispatcher route and application fanout service. The service creates or resumes a `NotificationFanoutRun`, scans eligible active actor subscriptions for the publishing source actor, skips duplicate dedupe keys, writes durable `Notification` inbox rows, advances cursor/count fields, and marks the run completed or failed for outbox retry behavior.
- **Dispatcher model:** `CompositeOutboxMessageDispatcher` is registered as `IOutboxMessageDispatcher`. It routes `EventPublished` to the existing `MqContractOutboxMessageDispatcher`, routes `EventPublishedNotificationFanoutRequested` to `IEventPublishedNotificationFanoutService`, and throws for unknown event types.
- **Modified runtime/test files:** `Explore.Application/Contracts/Services/IEventPublishedNotificationFanoutService.cs`, `Explore.Application/Services/EventPublishedNotificationFanoutService.cs`, `Explore.Application/ApplicationServicesRegistration.cs`, `Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Event.Application.UnitTests/Services/EventPublishedNotificationFanoutServiceTests.cs`, and `Explore.Infrastructure.Tests/Infrastructure/CompositeOutboxMessageDispatcherTests.cs`.
- **Validation:** LSP diagnostics clean; warning-relaxed Application and Infrastructure builds passed; targeted fanout service and composite dispatcher test classes passed with 3 tests each using analyzer suppression for test-project compilation because unrelated existing test warnings currently fail builds.
- **Blocked/deferred:** End-to-end publish-to-notification integration coverage, optional metrics/logging, EF migration/schema docs, and broad test-suite cleanup remain pending. Existing unrelated AI and test-analyzer blockers are unchanged.
- **Next action:** Add integration coverage proving publish creates both outbox rows and internal fanout dispatch creates exactly one notification per eligible subscriber with retry-safe deduplication.

### Implementation Handoff — Phase 4.5 Publish-To-Notification Integration Coverage — 2026-05-29 Europe/Brussels

- **Current state:** Focused integration coverage now exercises the internal fanout dispatcher path with real DI. The test seeds an active tenant-local subscriber, an organization source actor, an active `ActorSubscription`, and a published event, then dispatches `EventPublishedNotificationFanoutRequested` twice through `IOutboxMessageDispatcher`.
- **Runtime behavior verified:** The first internal dispatch creates exactly one durable `Notification` row for the subscriber and one completed `NotificationFanoutRun`; the second dispatch is idempotent and creates no duplicate notification because the deterministic `Notification.DeduplicationKey` already exists.
- **Tenant-filter hardening:** `NotificationRepository.ExistsByDeduplicationKeyAsync` now bypasses the tenant query filter only with an exact tenant predicate, matching the fanout repository pattern and protecting background worker dedupe checks from missing ambient HTTP tenant context.
- **Modified runtime/test files:** `Explore.Persistence/Repositories/NotificationRepository.cs` and `Event.API.IntegrationTests/Features/Notifications/EventPublishedNotificationFanoutIntegrationTests.cs`.
- **Validation:** LSP diagnostics clean; warning-relaxed API integration test project build passed; warning-relaxed Persistence build passed; targeted TUnit fanout integration test passed with 1 executed test. The earlier zero-test result was a `--no-build` race before the new test assembly was built and is not a final failure.
- **Blocked/deferred:** A full outbox-processor end-to-end test remains optional future coverage because publish-handler unit tests already verify both external and internal outbox rows are written. EF migration/schema docs remain deferred due pre-existing dirty migration snapshot/schema files. Broad suites remain affected by unrelated AI/test-analyzer blockers.
- **Next action:** Decide whether to add Phase 4.4 low-cardinality metrics/logging now or move to Phase 5 Blazor subscription UX.

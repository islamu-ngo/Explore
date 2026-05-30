<!-- ABOUTME: Operational memory for the actor subscription and notification fanout workstream. -->
<!-- ABOUTME: Tracks current status, decisions, key files, validation, risks, and handoff notes. -->

# Subscription Notification — Context

Last Updated: 2026-05-29 Europe/Brussels

## SESSION PROGRESS (2026-05-29 Europe/Brussels)

### ✅ COMPLETED

- CTO consultation created at `dev/active/subscription-notification/subscription-notification-consultation.md`.
- Implementation plan created at `dev/active/subscription-notification/subscription-notification-plan.md`.
- Senior CTO re-baseline applied to the implementation plan: v1 organizations/groups only, user-target subscriptions disabled, real-time refresh deferred to optional SSE, tenant-local subscriber identity required, and internal fanout outbox separated from external MQContract publishing.
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
- Phase 4.4 fanout observability added: the fanout service now emits low-cardinality `Explore.Business` metrics for runs and subscriber decisions, plus structured safe logs for processing, completed, skipped-completed, and failed runs.
- Phase 5 Blazor subscription UX service/component/profile/event slice added: generated actor-subscription client calls are wrapped by `ActorSubscriptionService`, reusable `ActorSubscriptionButton` renders HAL-gated Subscribe/Subscribed states, organization and group profile static buttons are replaced, group HAL link relations are preserved, and event details expose an organizer subscription action without demoting the registration CTA.
- Phase 6 notification inbox UX slice added: notification entity deep links are centralized and aligned with `Routes.razor`, subscription notification items show reason/source/context with keyboard-accessible semantics, and notification bell open no longer implicitly marks every item read.
- Phase 7 canonical documentation/operations slice updated notification, email-boundary, domain, architecture, API, outbox pattern, operations, troubleshooting, and API changelog docs to match the implemented actor subscription and durable in-app fanout behavior.
- `schemas/islamu-event.md` DBML schema reference now documents the implemented actor-subscription lookup tables, `actor_subscriptions`, notification deduplication key/index, `notification_fanout_runs`, and related references. EF migration generation/model-snapshot sync remains deferred.
- Phase 8 real-time refresh research completed: ASP.NET Core 10 SSE support was verified through Microsoft/Context7 documentation, and the active plan now uses SSE as the only planned one-way refresh-hint transport if lower-latency notification refresh is approved later.
- Phase 8 SSE refresh runtime implemented: authenticated `GET /api/notification/stream` emits minimal one-way notification refresh hints, and the Blazor notification bell consumes them through browser `EventSource` while preserving the existing 60-second polling fallback.

### 🟡 IN PROGRESS

- Phase 7 documentation updates, DBML schema reference update, Phase 8 SSE-only planning, and the Phase 8 SSE runtime refresh-hint layer are implemented and focused verified. Phase 2 still has deferred focused update/unsubscribe/query unit tests, persistence integration tests, and EF migration/model-snapshot generation once unrelated dirty migration state is isolated.

### ⏭️ NEXT

1. Isolate a clean EF migration/model-snapshot pass if database migration artifacts must be completed next; `schemas/islamu-event.md` is now updated, but `ExploreDbContextModelSnapshot.cs` and migration files remain intentionally untouched.
2. Unless overridden, v1 scope remains organizations/groups only, public user subscriptions disabled, SSE refresh hints are one-way/non-PII, polling remains fallback, and durable fanout remains driven by a separate internal outbox event.
3. Resolve or isolate the unrelated `AiConversationDto` compile blocker when the authored Phase 5 Blazor service/component tests need to execute.
4. Add additional security-hardening API tests for unauthorized, self-subscribe, and cross-tenant cases when touching endpoint authorization again.
5. Add focused query/update/unsubscribe command tests when touching that surface again.
6. Add persistence tests for subscription uniqueness, tenant filters, deduplication, and fanout run idempotency once migration state is clean.
7. Keep this context and `subscription-notification-tasks.md` updated after every meaningful implementation slice.

### ⚠️ BLOCKERS

- User may still override the CTO-baselined v1 scope before coding; absent override, implementation should proceed with the locked decisions above.
- Working tree contains many unrelated modified/untracked files from other workstreams. Implementation agents must not revert or mix unrelated changes.
- EF migration generation is deferred because `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs` was already dirty before this implementation slice. The DBML schema reference has now been updated separately by explicit user request.

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
| `Explore.Application/Telemetry/BusinessMetrics.cs` | Existing | Application/Telemetry | Business OpenTelemetry metrics. | Now includes notification fanout run and subscriber decision counters. |
| `Event.Application.UnitTests/Telemetry/BusinessMetricsNotificationFanoutTests.cs` | New | Application tests | Fanout metrics contract tests. | Verifies bounded tags and absence of high-cardinality/sensitive fanout dimensions. |
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
| `Explore.Blazor.Client/Contracts/Services/Notifications/IActorSubscriptionService.cs` | New | Blazor | Subscription service contract. | Wraps generated client behind BFF-safe service boundary. |
| `Explore.Blazor.Client/Services/ActorSubscriptionService.cs` | New | Blazor | Actor subscription API service. | Handles generated client calls and safe API failures. |
| `Explore.Blazor.Client/Shared/ActorSubscriptionButton.razor` | New | Blazor | Reusable HAL-aware subscription button. | Renders Subscribe/Subscribed states without local role checks. |
| `Explore.Blazor.Client.Tests/Services/ActorSubscriptionServiceTests.cs` | New | Blazor tests | Focused service tests. | Authored but blocked by unrelated AI DTO compile error. |
| `Explore.Blazor.Client.Tests/Components/ActorSubscriptionButtonTests.cs` | New | Blazor tests | Focused bUnit component tests. | Authored but blocked by unrelated AI DTO compile error. |
| `Explore.Blazor.Client/Layout/NotificationBell.razor.cs` | Existing | Blazor | Polling notification bell and deep-link mapping. | Fix org/group route mapping. |
| `Explore.Blazor.Client/Helpers/NotificationNavigationHelper.cs` | New | Blazor | Central notification entity deep-link mapping. | Aligns bell and full inbox with `Routes.razor`. |
| `Explore.Blazor.Client/Layout/NotificationItem.razor*` | Existing | Blazor | Notification row/card display. | Shows subscription reason/source/context and keyboard-accessible button semantics. |
| `Explore.Application/DTOs/Notification/NotificationRefreshHintDto.cs` | New | Application | Minimal SSE refresh-hint payload. | Contains unread count, reason, and generated timestamp only; no notification body/PII. |
| `Explore.Application/Contracts/Services/INotificationRefreshStreamService.cs` | New | Application | Notification SSE stream contract. | Produces one-way refresh hints for authenticated users. |
| `Explore.Application/Services/NotificationRefreshStreamService.cs` | New | Application | Server-side refresh hint stream. | Emits initial/changed unread-count hints and honors cancellation. |
| `Explore.Blazor.Client/Contracts/Services/Notifications/INotificationRefreshStreamClient.cs` | New | Blazor | Browser SSE client contract. | Exposes refresh hint events to UI components. |
| `Explore.Blazor.Client/Services/NotificationRefreshStreamClient.cs` | New | Blazor | JS interop wrapper for browser `EventSource`. | Uses same-origin cookies and safe reconnect/error behavior. |
| `Explore.Blazor.Client/wwwroot/js/notification-refresh.js` | New | Blazor JS | EventSource module for notification refresh hints. | Subscribes to `notification-refresh` events and calls .NET handlers. |
| `Event.Application.UnitTests/Services/NotificationRefreshStreamServiceTests.cs` | New | Application tests | SSE stream service tests. | Verifies initial hint and unauthenticated fail-closed behavior. |
| `Explore.Blazor.Client.Tests/Services/NotificationRefreshStreamClientTests.cs` | New | Blazor tests | SSE client callback tests. | Verifies JS-invokable hint mapping and safe default reason. |
| `Explore.Blazor.Client.Tests/Helpers/NotificationNavigationHelperTests.cs` | New | Blazor tests | Focused route mapping tests. | Verifies event/org/group routes and unsupported entity behavior. |
| `Explore.Blazor.Client.Tests/Components/NotificationItemTests.cs` | New | Blazor tests | Focused notification item bUnit tests. | Verifies subscription metadata display and Enter/Space activation. |
| `docs/NOTIFICATIONS.md` | Existing | Docs | Current in-app notification lifecycle. | Now documents actor-subscription in-app fanout, deduplication, fanout runs, corrected routes, and explicit read-state behavior. |
| `docs/EMAIL_NOTIFICATIONS.md` | Existing | Docs | SMTP/email notification boundary. | Clarifies actor-subscription fanout is in-app only, not SMTP fanout. |
| `docs/DOMAIN.md` | Existing | Docs | Domain model reference. | Now documents `ActorSubscription`, `NotificationFanoutRun`, subscription lookups, and notification deduplication. |
| `docs/ARCHITECTURE.md` | Existing | Docs | System architecture reference. | Now documents external/internal event-published outbox split and composite dispatcher routing. |
| `docs/API.md` | Existing | Docs | API reference. | Now documents actor-subscription endpoints and notification fanout metrics. |
| `docs/OPERATIONS.md` | Existing | Docs | Operator guide. | Now documents fanout operational flow, metrics, dead-letter/backlog signals, and at-least-once dedupe expectations. |
| `docs/OUTBOX_PATTERN.md` | Existing | Docs | Outbox pattern reference. | Now documents composite dispatcher and internal notification fanout routing. |
| `docs/TROUBLESHOOTING.md` | Existing | Docs | Troubleshooting guide. | Updated stale outbox dispatcher guidance. |
| `docs/API_CHANGELOG.md` | Existing | Docs | API contract changes. | Required for new endpoints. |
| `schemas/islamu-event.md` | Existing | Schema docs | DBML database schema reference. | Now documents actor subscription lookup tables, `actor_subscriptions`, notification dedupe key/index, `notification_fanout_runs`, and related references. |

## Key Decisions

1. **Canonical target is `Actor`.** Avoid per-type subscription tables.
2. **Subscription and bell setting are separate.** Store bell level on `ActorSubscription`.
3. **V1 delivery is in-app durable notification rows.** Email, push, and SSE are not delivery truth.
4. **Fanout is outbox-driven on a separate internal event.** Event publication writes the external `EventPublished` outbox row and an internal `NotificationFanoutRequested` row in the same transaction.
5. **Deduplication is mandatory.** Add deterministic `Notification.DeduplicationKey` and unique index.
6. **Preserve existing MQ dispatch.** Do not replace `MqContractOutboxMessageDispatcher` with notification-only logic.
7. **HAL links gate UI actions.** Blazor must not infer subscribe/edit/delete permissions from local roles/claims.
8. **Tenant-local subscriber state is mandatory.** Active `TenantUser` state gates subscription commands and fanout eligibility.
9. **Public user subscriptions are privacy-sensitive.** V1 is organizations/groups only; user targets fail closed unless a future plan adds privacy controls.
10. **Real-time refresh is deferred; SSE is the Phase 8 transport.** Polling remains the v1 refresh path; persisted notifications are the source of truth. If approved later, SSE should stream minimal refresh hints only.

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
- Phase 5 LSP diagnostics were clean for `IActorSubscriptionService`, `ActorSubscriptionService`, `ActorSubscriptionButton.razor.cs`, `HalResourceExtensions`, `GroupService`, and the new focused Blazor test files.
- `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet -p:TreatWarningsAsErrors=false` passed after Phase 5 service/component/profile/event wiring: 2 projects, 0 errors, existing warnings.
- Focused Blazor service/component tests were authored but could not execute because `Explore.Blazor.Client.Tests` currently fails project-reference compilation on unrelated `Explore.Application/DTOs/Ai/AiConversationDtos.cs(28,41): error CS0509: 'AiConversationDto': cannot derive from sealed type 'AiConversationSummaryDto'`. Analyzer suppression does not bypass this compile error.
- Phase 6 LSP diagnostics were clean for `NotificationNavigationHelper`, `NotificationBell.razor.cs`, `Notifications.razor.cs`, `NotificationItem.razor.cs`, `NotificationNavigationHelperTests`, and `NotificationItemTests`.
- `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet -p:TreatWarningsAsErrors=false` passed after Phase 6 inbox UX changes: 2 projects, 0 errors, existing warnings.
- Targeted notification route-helper tests passed: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/NotificationNavigationHelperTests/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1` returned 4 total, 4 succeeded.
- Targeted notification item tests passed: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/NotificationItemTests/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1` returned 3 total, 3 succeeded.
- An earlier combined TUnit `--treenode-filter` attempt for both Phase 6 classes matched only one class and exited nonzero because `--minimum-expected-tests 7` was too high for the matched set; it was a filter/minimum mismatch, not a test failure. The classes were rerun separately and passed.
- Phase 4.4 LSP diagnostics were clean for `BusinessMetrics`, `EventPublishedNotificationFanoutService`, `EventPublishedNotificationFanoutServiceTests`, and `BusinessMetricsNotificationFanoutTests`.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after fanout observability changes.
- Targeted fanout metrics tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/BusinessMetricsNotificationFanoutTests/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1` returned 3 total, 3 succeeded.
- Targeted fanout service tests passed after the metrics dependency update: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/EventPublishedNotificationFanoutServiceTests/*" --minimum-expected-tests 3 --no-progress --maximum-parallel-tests 1` returned 3 total, 3 succeeded. Output included a transient file-copy retry warning from parallel build/test access to `Explore.Application.dll`, then tests passed.
- Phase 7 documentation verification passed: targeted `DocumentationQualityTests` returned 4 total, 4 succeeded.
- Phase 7 documentation link verification passed: targeted `AgentContextLinkTests` returned 8 total, 8 succeeded.
- DBML schema reference update verification passed: `schemas/islamu-event.md` was read back and grep-verified for actor subscription, notification fanout, and deduplication schema elements; targeted `DocumentationQualityTests` returned 4 total, 4 succeeded after the schema patch.
- Phase 8 SSE planning verification passed with no-build targeted `DocumentationQualityTests`: 4 total, 4 succeeded. A normal rebuild attempt was blocked before test execution by unrelated `KeycloakBootstrapService.ClientSecretUpdateResult.Success` duplicate-member compile error.
- Phase 8 SSE runtime LSP diagnostics were clean for the SSE API endpoint, Application stream service, Blazor EventSource client, notification bell integration, and focused SSE tests.
- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after SSE stream service changes.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:TreatWarningsAsErrors=false` passed after the authenticated SSE endpoint was added.
- `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet -p:TreatWarningsAsErrors=false` passed after Blazor EventSource client and notification bell integration.
- Targeted SSE stream service tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/NotificationRefreshStreamServiceTests/*" --minimum-expected-tests 2 --no-progress --maximum-parallel-tests 1` returned 2 total, 2 succeeded.
- Targeted Blazor SSE client tests passed: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet -p:RunAnalyzers=false -p:WarningLevel=0 -p:TreatWarningsAsErrors=false -p:WarningsAsErrors= -- --treenode-filter "/*/*/NotificationRefreshStreamClientTests/*" --minimum-expected-tests 2 --no-progress --maximum-parallel-tests 1` returned 2 total, 2 succeeded.

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
- **Notes for next contributor/agent:** Do not implement real-time refresh before the durable actor subscription + internal notification fanout path. Preserve HAL gating, tenant-local subscriber checks, and repository entity-return rules.

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
- **Next action:** Proceed with Phase 5 Blazor subscription UX; Phase 4.4 metrics/logging was completed later in this workstream.

### Implementation Handoff — Phase 5 Blazor Subscription UX — 2026-05-29 Europe/Brussels

- **Current state:** The initial Blazor subscription UX is implemented. `ActorSubscriptionService` wraps generated actor-subscription client methods behind a scoped service with safe API exception handling. `ActorSubscriptionButton` renders HAL-gated Subscribe/Subscribed states, loads current subscription state when the `subscription` link exists, and uses `AppButton`, accessibility labels, live announcements, scoped BEM CSS, and no local role/claim checks.
- **UI surfaces updated:** Organization profile and group profile no longer show static Subscribe buttons; both use `ActorSubscriptionButton` from HAL affordances. `GroupService` now preserves `_links` relation names from the raw group HAL response so the group profile can gate affordances correctly. Event detail now renders an organizer subscription button near the organizer card using `organizer-subscription`/`subscribe-organizer` HAL links while leaving registration as the primary CTA.
- **Modified runtime/test files:** `Explore.Blazor.Client/Contracts/Services/Notifications/IActorSubscriptionService.cs`, `Explore.Blazor.Client/Services/ActorSubscriptionService.cs`, `Explore.Blazor.Client/Shared/ActorSubscriptionButton.razor*`, `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`, `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`, `Explore.Blazor.Client/Services/GroupService.cs`, `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor`, `Explore.Blazor.Client/Pages/Groups/GroupProfile.razor`, `Explore.Blazor.Client/Pages/Events/EventDetail.razor`, `Explore.Blazor.Client/Pages/Events/EventDetail.razor.css`, `Explore.Blazor.Client.Tests/Services/ActorSubscriptionServiceTests.cs`, and `Explore.Blazor.Client.Tests/Components/ActorSubscriptionButtonTests.cs`.
- **Validation:** Blazor client build passed with warning-as-error disabled; LSP diagnostics were clean for touched C# files and new tests. Razor files were validated through the Blazor client build because Razor LSP is unavailable. The new focused Blazor tests are blocked from execution by an unrelated AI DTO compile error in `Explore.Application`.
- **Blocked/deferred:** EF migration/schema docs remain deferred due pre-existing dirty migration snapshot/schema files. Broad Blazor/Application/Architecture test runs remain affected by unrelated AI workstream issues. Phase 4.4 metrics/logging was completed later in this workstream.
- **Next action:** Either resolve/isolate the unrelated AI DTO compile blocker to run the authored Blazor tests, or proceed to Phase 6 notification inbox UX with the current Blazor client build and LSP evidence.

### Implementation Handoff — Phase 6 Notification Inbox UX — 2026-05-29 Europe/Brussels

- **Current state:** Notification inbox UX fixes are implemented. Deep-link route mapping is centralized in `NotificationNavigationHelper` and used by both the bell and full notifications page. Event, organization, and group links now match `Routes.razor`; unsupported notification entity types do not navigate to guessed URLs.
- **Inbox item UX:** `NotificationItem` now exposes subscription context in text: reason, source actor (`From ...`), and recipient context (`via ...`) when available. The button-like notification row supports Enter/Space activation, includes a richer accessible label, and has a visible focus ring.
- **Read-state decision:** Automatic mark-all-read-on-open was removed from the bell. Opening the notification panel no longer zeroes unread count or fire-and-forgets a mark-all-read call; explicit read actions remain the source of read-state changes.
- **Modified runtime/test files:** `Explore.Blazor.Client/Helpers/NotificationNavigationHelper.cs`, `Explore.Blazor.Client/Layout/NotificationBell.razor.cs`, `Explore.Blazor.Client/Pages/Notifications/Notifications.razor.cs`, `Explore.Blazor.Client/Layout/NotificationItem.razor`, `NotificationItem.razor.cs`, `NotificationItem.razor.css`, `Explore.Blazor.Client.Tests/Helpers/NotificationNavigationHelperTests.cs`, and `Explore.Blazor.Client.Tests/Components/NotificationItemTests.cs`.
- **Validation:** LSP diagnostics clean; Blazor client build passed; targeted route-helper tests passed with 4 tests; targeted notification item tests passed with 3 tests. A previous combined TUnit filter mismatch was rerun as separate passing class filters.
- **Blocked/deferred:** EF migration/schema docs remain deferred due pre-existing dirty migration snapshot/schema files. Broad Blazor/Application/Architecture test runs remain affected by unrelated AI workstream issues, but the Phase 6 focused tests passed.
- **Next action:** Move to Phase 4.4 metrics/logging, then Phase 7 documentation/operations updates.

### Implementation Handoff — Phase 4.4 Fanout Metrics/Logging — 2026-05-29 Europe/Brussels

- **Current state:** Fanout observability is implemented. `BusinessMetrics` now exposes low-cardinality counters for notification fanout runs and subscriber decisions, and `EventPublishedNotificationFanoutService` records processing/completed/skipped/failed outcomes while preserving outbox retry semantics.
- **Metrics contract:** `explore.notifications.fanout_runs` records bounded run outcomes by `tenant_id`, normalized `fanout_kind`, and normalized `outcome`. `explore.notifications.fanout_subscribers` records aggregate subscriber decisions by the same bounded tags. Metrics intentionally do not include event IDs, actor IDs, subscriber IDs, notification IDs, deduplication keys, or event titles.
- **Logging contract:** Fanout logs use structured message templates with safe run/event/tenant IDs and aggregate counts only. Non-cancellation exceptions are logged with safe context, the fanout run is marked failed, and the exception is rethrown so the outbox processor can retry or dead-letter.
- **Modified runtime/test files:** `Explore.Application/Telemetry/BusinessMetrics.cs`, `Explore.Application/Services/EventPublishedNotificationFanoutService.cs`, `Event.Application.UnitTests/Telemetry/BusinessMetricsNotificationFanoutTests.cs`, and `Event.Application.UnitTests/Services/EventPublishedNotificationFanoutServiceTests.cs`.
- **Validation:** LSP diagnostics clean; warning-relaxed Application build passed; targeted fanout metrics tests passed with 3 tests; targeted fanout service tests passed with 3 tests after the metrics dependency update.
- **Blocked/deferred:** EF migration/schema docs remain deferred due pre-existing dirty migration snapshot/schema files. Broad Blazor/Application/Architecture test runs remain affected by unrelated AI workstream issues, but the Phase 4.4 focused validation passed.
- **Next action:** Move to Phase 7 documentation/operations updates for the implemented subscription model, API/HAL contract, fanout pipeline, Blazor UX, notification inbox UX, and new fanout observability.

### Implementation Handoff — Phase 7 Documentation/Operations — 2026-05-29 Europe/Brussels

- **Current state:** Canonical docs now describe the implemented actor-subscription and notification fanout behavior. Notification docs cover internal outbox-driven in-app fanout, deduplication, fanout runs, corrected notification routes, and explicit read-state behavior. Domain/architecture/API/outbox docs describe `ActorSubscription`, `NotificationFanoutRun`, actor-subscription endpoints, composite dispatcher routing, and internal `EventPublishedNotificationFanoutRequested` fanout. Operations docs cover fanout metrics, backlog/dead-letter signals, and safe structured logging.
- **Docs updated:** `docs/NOTIFICATIONS.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/DOMAIN.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md`, and `docs/TROUBLESHOOTING.md`.
- **Schema/config decision:** `schemas/islamu-event.md` has now been updated by explicit user request. EF migration generation and `ExploreDbContextModelSnapshot.cs` remain intentionally deferred with a clean migration slice. `docs/CONFIGURATION.md` was not changed because no new configuration keys were added.
- **Validation:** Targeted `DocumentationQualityTests` passed with 4 tests; targeted `AgentContextLinkTests` passed with 8 tests.
- **Blocked/deferred:** EF migration generation/model-snapshot sync, additional command/persistence tests, and Phase 5 Blazor service/component test execution blocked by unrelated AI DTO compile failure remain deferred. Optional real-time refresh is now documented as SSE-only if approved later.
- **Next action:** Either isolate a clean EF migration/model-snapshot pass or resolve unrelated AI/test blockers before broad-suite verification.

### Implementation Handoff — DBML Schema Reference Update — 2026-05-29 Europe/Brussels

- **Current state:** `schemas/islamu-event.md` now reflects the implemented subscription notification schema in DBML form. The update documents actor subscription lookup tables, the durable `actor_subscriptions` relationship, notification deduplication, resumable `notification_fanout_runs`, corrected notification references, and the corrected actor type seed note.
- **Schema elements updated:** Added `actor_subscription_statuses`, `actor_subscription_notification_levels`, `actor_subscriptions`, `notification_fanout_runs`, `notifications.deduplication_key`, the unique notification dedupe index, actor subscription references, notification fanout run references, notification tenant/source/recipient actor references, and the correct notification entity type reference.
- **Validation:** DBML sections were read back after patching, grep-verified for the new subscription/fanout/dedupe schema elements, and targeted `DocumentationQualityTests` passed with 4 tests.
- **Not touched:** No EF migration was generated and `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs` was not modified. Migration/model-snapshot synchronization remains a separate clean-slice task because the snapshot was already dirty before this workstream.
- **Next action:** If database artifacts are needed, isolate a clean EF migration/model-snapshot pass for the implemented model; otherwise proceed with final review/cleanup or unrelated blocker resolution.

### Implementation Handoff — Phase 8 SSE Refresh Research — 2026-05-29 Europe/Brussels

- **Current state:** Phase 8 planning now treats Server-Sent Events as the optional transport for one-way notification refresh hints. Research verified ASP.NET Core 10 first-class SSE results through `TypedResults.ServerSentEvents` and `IAsyncEnumerable<SseItem<T>>`.
- **Recommendation:** The user is correct for this feature: SSE fits notification refresh hints because the server only needs to tell the browser that unread count or inbox state changed. The caveat is that SSE still opens one long-lived HTTP request and should not replace durable `Notification` rows or authenticated notification APIs.
- **Plan updates:** `subscription-notification-plan.md` and `subscription-notification-tasks.md` now define Phase 8 as SSE-only if approved, with polling fallback, minimal non-PII hint payloads, same-origin cookie/BFF authentication, reconnect/`Last-Event-ID`, cancellation, and reverse-proxy buffering requirements.
- **Validation:** No-build targeted `DocumentationQualityTests` passed after the plan/task/context updates. A normal rebuild attempt was blocked by unrelated Keycloak duplicate-member compile error before test execution.
- **Next action:** Phase 8 runtime implementation has now been added after explicit approval; see the following handoff.

### Implementation Handoff — Phase 8 SSE Runtime Refresh Hints — 2026-05-29 Europe/Brussels

- **Current state:** SSE-only notification refresh hints are implemented. Authenticated users can connect to `GET /api/notification/stream`, which streams `notification-refresh` events containing unread-count state hints. Blazor's notification bell starts a browser `EventSource` subscription and keeps the existing 60-second polling timer as fallback.
- **Server flow:** `NotificationController` returns ASP.NET Core 10 `TypedResults.ServerSentEvents` from `INotificationRefreshStreamService`. The stream yields an initial unread-count hint and later changed-count hints, honors cancellation, uses SSE IDs/reconnect interval, disables request timeout, and sends no-store/no-cache plus `X-Accel-Buffering: no` headers.
- **Client flow:** `NotificationRefreshStreamClient` loads `/js/notification-refresh.js`, starts `EventSource` with same-origin cookies, maps `notification-refresh` events back into .NET, and raises typed refresh events. `NotificationBell` updates unread count and reloads open panel state from the hint while preserving polling fallback and durable notification APIs as source of truth.
- **Payload contract:** `NotificationRefreshHintDto` contains only `UnreadCount`, `HasUnread`, `Reason`, and `GeneratedAt`; it intentionally carries no notification title/body, entity IDs, user IDs, dedupe keys, or other unnecessary PII/high-cardinality data.
- **Modified runtime/test files:** `Explore.API/Controllers/NotificationController.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.Application/DTOs/Notification/NotificationRefreshHintDto.cs`, `Explore.Application/Contracts/Services/INotificationRefreshStreamService.cs`, `Explore.Application/Services/NotificationRefreshStreamService.cs`, `Explore.Application/ApplicationServicesRegistration.cs`, `Explore.Blazor.Client/Contracts/Services/Notifications/INotificationRefreshStreamClient.cs`, `Explore.Blazor.Client/Services/NotificationRefreshStreamClient.cs`, `Explore.Blazor.Client/wwwroot/js/notification-refresh.js`, `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`, `Explore.Blazor.Client/Layout/NotificationBell.razor*`, `Event.Application.UnitTests/Services/NotificationRefreshStreamServiceTests.cs`, and `Explore.Blazor.Client.Tests/Services/NotificationRefreshStreamClientTests.cs`.
- **Validation:** LSP diagnostics clean; warning-relaxed Application, API, and Blazor client builds passed; targeted `NotificationRefreshStreamServiceTests` passed with 2 tests; targeted `NotificationRefreshStreamClientTests` passed with 2 tests.
- **Blocked/deferred:** EF migration/model-snapshot sync remains deferred. Broad test suites remain affected by unrelated Keycloak/AI/test-analyzer issues, but the Phase 8 SSE runtime slice has focused passing verification.
- **Next action:** Update canonical notification/API/operations docs for the runtime SSE endpoint if this behavior should be part of public/operator docs, then proceed with final cleanup or the clean EF migration/model-snapshot slice.

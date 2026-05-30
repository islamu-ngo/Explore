<!-- ABOUTME: Implementation plan for actor subscriptions and subscription-driven in-app notification fanout. -->
<!-- ABOUTME: Grounds future work in Clean Architecture, CQRS, EF Core, HAL, Blazor, and outbox conventions. -->

# Subscription Notification — Implementation Plan

Last Updated: 2026-05-29 Europe/Brussels

## 0. Planning Metadata

- **Request:** Create an implementation plan for a YouTube-inspired subscription model and notification system where users can subscribe to organizations, groups, and users, manage a bell setting, and receive navigation-dropdown notifications when subscribed actors publish events.
- **Task directory:** `dev/active/subscription-notification/`
- **Planning status:** CTO re-baselined; ready for user approval or implementation with the v1 scope decisions below.
- **Consultation source:** `dev/active/subscription-notification/subscription-notification-consultation.md`.
- **Matched intents:** Multi-intent feature implementation. No single existing intent covers a cross-layer subscription/fanout feature, so this plan uses a fallback contract plus the stricter union of closest intents: `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `add-get-endpoint`, `add-write-endpoint`, `add-hal-link`, `openapi-contract-change`, and `blazor-component-affordance`.
- **Relevant skills loaded:** `senior-cto-feedback`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `accessibility`, `outbox-pattern`, `cto-consultation`.
- **Relevant rules loaded:** `.claude/rules/domain.md`, `application-layer.md`, `efcore-persistence.md`, `efcore-migrations.md`, `api-controllers.md`, `api-hateoas.md`, `blazor-client.md`, `tests.md`.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor Client, Tests, Docs. Optional later: Blazor BFF / SSE refresh hints / DevOps.
- **Estimated complexity:** XL. The feature cuts across a new aggregate, lookup seed data, EF migrations, idempotent background fanout, API/HAL contract, authorization policy, generated client, profile/event-detail UX, notification inbox UX, integration tests, and docs.

### 0.1 Contribution Contract Mapping

| Intent | Why it applies | Must-read docs / skills / rules | Paths in scope | Minimum tests | Docs to update | Unique acceptance / forbidden |
|---|---|---|---|---|---|---|
| `add-cqrs-handler` — Add Command/Query handler | Subscription commands/queries and fanout orchestration live in Application. | `docs/ARCHITECTURE.md`, `docs/QUICK_REFERENCE.md`; `cqrs-mediatr-guidelines`; `.claude/rules/application-layer.md` | `Explore.Application/Features/**/*.cs` | `Event.Application.UnitTests`, `Event.Architecture.Tests` | none required by intent | Pipeline behavior respected; no cross-feature internals coupling. |
| `add-ef-migration` — Add/modify EF migration | New subscription/fanout tables, lookups, and notification dedup field require schema changes. | `docs/QUICK_REFERENCE.md`, `docs/DOMAIN.md`; `dotnet-efcore-guidelines`; `.claude/rules/efcore-migrations.md` | `Explore.Persistence/Migrations/**/*.cs`, `Explore.Domain/**/*.cs` | `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests` | `schemas/islamu-event.md` | Migration reversible; seed/enum sync; no destructive `Down()` without approval. |
| `update-repository-query` — Modify repository query/specification | Subscription/fanout repository queries and notification insertion need entity-returning repositories. | `docs/QUICK_REFERENCE.md`; `dotnet-efcore-guidelines`; `.claude/rules/efcore-persistence.md` | `Explore.Persistence/Repositories/**/*.cs` | `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests` | none required by intent | Repositories return entities; no unsafe tenant-filter bypass. |
| `add-get-endpoint` — Add read endpoint | Subscription state/list/summary endpoints are API reads. | `docs/API.md`, `docs/QUICK_REFERENCE.md`; `cqrs-mediatr-guidelines`; `.claude/rules/api-controllers.md` | `Explore.API/Controllers/**/*.cs`, Application queries | `Event.API.IntegrationTests`, `Event.Architecture.Tests` | `docs/API_CHANGELOG.md` | Explicit routes/names/classification/response types. |
| `add-write-endpoint` — Add mutation endpoint | Subscribe, unsubscribe, and bell-level update are authenticated writes. | `docs/API.md`, `docs/QUICK_REFERENCE.md`, `docs/SECURITY-MODEL.md`; `cqrs-mediatr-guidelines`, `auth-patterns`; `.claude/rules/api-controllers.md` | `Explore.API/Controllers/**/*.cs`, Application commands | `Event.API.IntegrationTests`, `Event.Architecture.Tests` | `docs/API_CHANGELOG.md` | Authorized writes, rate limiting, idempotency considered, HAL policy updated. |
| `add-hal-link` — Add/modify HAL affordance | Profile/event Subscribe and bell actions must be `_links`-gated. | `docs/API.md`, `docs/QUICK_REFERENCE.md`; `.claude/rules/api-hateoas.md` | `Explore.API/Hateoas/**/*.cs`, `Explore.Blazor.Client/**/*.razor` | `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests` | none required by intent | Separate policies; auth encoded server-side; no role/claim UI gating. |
| `openapi-contract-change` — Public API contract change | New endpoints/DTOs affect OpenAPI and NSwag client. | `docs/API.md`, `docs/QUICK_REFERENCE.md`; `.claude/rules/api-controllers.md` | API controllers, `docs/API_CHANGELOG.md`, generated client when regenerated | `Event.API.IntegrationTests`, `Event.Architecture.Tests` | `docs/API_CHANGELOG.md` | Operation IDs stable; breaking changes are acceptable in development mode when documented. |
| `blazor-component-affordance` — Toggle Blazor mutation affordance | Subscribe/bell UI must use HAL links, not local auth/role checks. | `docs/BLAZOR.md`, `docs/QUICK_REFERENCE.md`; `blazor-ui-conventions`; `.claude/rules/blazor-client.md` | `Explore.Blazor.Client/**/*.razor` | `Explore.Blazor.Client.Tests` | none required by intent | Button visibility gated by HAL link presence. |

### 0.2 Fallback Contract

Because no intent directly covers “new actor-subscription + notification fanout platform capability,” implementation agents must obey `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, all loaded skills/rules above, and the strictest union of tests/docs in section 14. Task 0.5 asks whether a reusable `subscription-notification-feature` or `background-fanout-feature` intent should be added if this pattern recurs.

### 0.3 Senior CTO Re-Baseline Verdict

**Decision:** Approve with required changes incorporated into this plan. The direction is right, but implementation must be split and must not couple subscription fanout failure to the external `EventPublished` MQContract publish path.

Required v1 decisions now locked unless the user explicitly overrides them before coding:

- V1 supports organization and group subscriptions only. User-target subscriptions remain a reserved model/policy path and must fail closed until public-user privacy controls are deliberately designed.
- Real-time refresh is deferred for v1. If low-latency browser refresh is approved later, Server-Sent Events (SSE) are the preferred Phase 8 candidate for one-way notification refresh hints; durable PostgreSQL notification rows and polling remain the delivery truth/fallback.
- Event publication writes a separate internal notification-fanout outbox message in addition to the existing external `EventPublished` integration event. Do not mutate the MQContract `EventPublishedIntegrationEvent` just to support local fanout.
- `ActorSubscription` uses tenant-local subscriber identity. Store or resolve `TenantUser` explicitly so suspended, banned, removed, or wrong-tenant users cannot subscribe or receive fanout.
- Use one durable non-deleted subscription row per tenant/subscriber/target actor. Unsubscribe is a status transition; resubscribe reactivates the same row and resets the notification level to the v1 default.

## 1. Executive Summary

Implement first-class actor subscriptions for ISLAMU Event. A signed-in tenant-local user can subscribe to supported `Actor` targets: organizations and groups in v1, with public user actors explicitly deferred behind future privacy controls. After subscribing, the UI shows a bell control with `Notify me` and `Off`. When an actor publishes an event, a dedicated internal outbox event drives durable in-app `Notification` fanout for eligible subscribers. The existing notification bell/dropdown becomes the delivery surface; optional SSE can later provide low-latency refresh hints, but persisted notifications remain the source of truth.

The implementation must be PostgreSQL-first, free/self-hostable, tenant-isolated, idempotent, auditable, HAL-gated, accessible, and compatible with Clean Architecture/CQRS/MediatR. Do not create separate organization/group/user subscription tables. The canonical target is `Actor`.

Out of scope for the first implementation slice:

- email fanout from subscriptions;
- browser/mobile push notifications;
- YouTube-like ranking/personalization algorithms;
- public subscriber lists or subscriber counts;
- public user-to-user subscriptions in UI/API;
- RabbitMQ-required fanout;
- broad compatibility shims for obsolete contracts, because this repo is in development mode.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| `Actor` is the common identity wrapper for users, organizations, groups, bots, and system actors. | Verified: `Explore.Domain/Actor.cs`; `Explore.Domain/Enums/ActorTypeEnum.cs`. | High | `Actor` has optional `UserId`, `OrganizationId`, `GroupId`. |
| `Event` is published by an actor. | Verified: `Explore.Domain/Event.cs`; `Explore.Persistence/Configurations/Entities/EventConfiguration.cs`. | High | `Event.ActorId` has tenant-scoped FK to `Actor`. |
| Event publishing already writes an `EventPublished` outbox message. | Verified: `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`. | High | Handler writes `OutboxMessage` in `IUnitOfWork.ExecuteInTransactionAsync`. |
| General outbox is at-least-once and calls `IOutboxMessageDispatcher`. | Verified: `Explore.API/BackgroundServices/OutboxProcessor.cs`; `Explore.Domain/OutboxMessage.cs`; `Explore.Persistence/Repositories/OutboxRepository.cs`. | High | Consumers must be idempotent. |
| Current dispatcher routes `EventPublished` to MQContract messaging. | Verified: `Explore.Infrastructure/Messaging/MqContractOutboxMessageDispatcher.cs`; `EventPublishedIntegrationEvent.cs`. | High | Subscription fanout must be routed separately through a composite dispatcher, not silently replace external integration publishing. |
| `EventPublishedIntegrationEvent` lacks `ActorId`. | Verified: `Explore.Application/Models/IntegrationEvents/EventPublishedIntegrationEvent.cs`. | High | Fanout should use an internal fanout outbox payload with actor context or load event by ID; do not change the external payload in v1. |
| `EventPublishedIntegrationEvent` is an MQContract message with `typeVersion: "1.0.0"`. | Verified: `Explore.Application/Models/IntegrationEvents/EventPublishedIntegrationEvent.cs`. | High | Do not change this broker contract for local fanout in v1; use a separate internal outbox payload or a deliberate versioned contract PR. |
| Tenant-local user state exists and should gate subscriber identity. | Verified: `Explore.Domain/TenantUser.cs`; `Explore.Persistence/Configurations/Entities/TenantUserConfiguration.cs`; `ITenantUserRepository`. | High | Subscription writes and fanout should use active `TenantUser` state, not only the global `User` row. |
| Notifications already support user-owned, tenant-scoped inbox records with type/reason/scope/source actor/entity link. | Verified: `Explore.Domain/Notification.cs`; `Explore.Persistence/Configurations/Entities/NotificationConfiguration.cs`; `docs/NOTIFICATIONS.md`. | High | This is sufficient as the inbox storage target after adding dedup. |
| `NotificationReason.Subscription` already exists. | Verified: `Explore.Domain/Enums/NotificationReasonEnum.cs`; `Explore.Persistence/Seed/LookupTableSeeder.cs`. | High | Use for subscription fanout. |
| Existing notification UI is polling-based, not real-time transport based. | Verified: `Explore.Blazor.Client/Layout/NotificationBell.razor.cs`; `docs/NOTIFICATIONS.md`. | High | Polls every 60 seconds; opening the bell no longer marks all read. |
| Existing profile pages contain static Subscribe buttons with no service/API behavior. | Verified: `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor`; `Explore.Blazor.Client/Pages/Groups/GroupProfile.razor`. | High | Replace with reusable HAL-aware subscription component. |
| Event detail has organizer profile navigation but no subscribe affordance. | Verified: `Explore.Blazor.Client/Pages/Events/EventDetail.razor`; `EventDetail.razor.cs`. | High | Add subscribe UI near organizer card. |
| Notification deep links likely have route mismatches for org/group. | Verified: `NotificationBell.razor.cs` maps `/organizations/{id}` and `/groups/{id}`; `Routes.razor` uses `/organization/profile/:id` and `/group/profile/:id`. | High | Fix as part of notification UX slice. |
| No current `ActorSubscription` or fanout run model exists. | Verified by search: `rg "ActorSubscription|NotificationFanout|DeduplicationKey"` found no implementation outside consultation/plans. | High | New model required. |
| No app-level SSE notification refresh transport exists. | Verified by search: `rg "ServerSentEvents|SseItem"`. External docs verified ASP.NET Core 10 `TypedResults.ServerSentEvents` support for `IAsyncEnumerable<SseItem<T>>`. | Medium | SSE is the Phase 8 direction for one-way notification refresh hints if real-time refresh is approved. |
| Current docs explicitly warn not to claim notification-to-email/push/fanout support. | Verified: `docs/NOTIFICATIONS.md`; `docs/EMAIL_NOTIFICATIONS.md`. | High | Update only after behavior is implemented. |

### 2.2 Existing Implementation

**Domain**
- `Explore.Domain/Actor.cs` models actors with `ActorTypeId` and optional user/organization/group references.
- `Explore.Domain/Event.cs` uses `ActorId` as the event publisher/organizer.
- `Explore.Domain/Notification.cs` supports in-app inbox state: type, title/body, entity reference, source actor, recipient context actor, reason, read/archive/snooze, tenant, audit, soft delete.
- `UserNotificationPreference` exists, but it is category-scoped (`registration-confirmations`, `event-updates`, etc.) and not suitable for per-actor bell preferences.
- `TenantUser` stores tenant-local user participation/status. Subscription ownership must be tied to an active tenant-local user, even though notification inbox rows continue to target `Notification.UserId`.

**Application**
- CQRS handlers exist under `Explore.Application/Features/*` and use manually instantiated validators.
- `PublishEventCommandHandler` already writes an `EventPublished` general outbox row in the event publication transaction.
- `EventPublishedIntegrationEvent` exists for external integration dispatch, but currently carries event ID/title/dates/deleted state and not publisher actor ID. Keep this external broker contract stable in v1 and add a separate internal fanout request if source actor context is needed.

**Persistence**
- `ExploreDbContext` applies tenant and soft-delete query filters to domain entities.
- `NotificationRepository` supports user-scoped paged list, unread count, mark-read, mark-all-read, archive, snooze, and ownership-checked detail retrieval.
- `NotificationConfiguration` has indexes for unread counts and filters but no deterministic deduplication key.
- Lookup seed data already includes `NotificationReason.Subscription` and `NotificationType.EventCreated` / `EventUpdated` / `EventCancelled`.

**Infrastructure/API background processing**
- `OutboxProcessor` polls `OutboxMessage`, uses optimistic locking, calls `IOutboxMessageDispatcher`, and marks completed/failed/dead-lettered.
- `MqContractOutboxMessageDispatcher` currently deserializes `EventPublished` and publishes it to `events.published`. Subscription fanout should be routed through a composite dispatcher on a separate internal event type so local fanout failures do not cause duplicate external MQ publishes or block the broker contract from evolving deliberately.

**API/HAL**
- `NotificationController` exposes authenticated notification inbox endpoints.
- `ActorController`, `EventController`, and HATEOAS policies exist; new subscription affordances should be added there or through a dedicated `ActorSubscriptionController`.
- HAL links are the mandatory UI affordance source.

**Blazor Client**
- `OrganizationProfile` and `GroupProfile` already render static Subscribe buttons.
- `EventDetail` renders organizer information and profile navigation; no subscribe UI exists there.
- `NotificationBell` polls unread count, loads panel items, marks all read on open, and maps entity links by string type.

### 2.3 Existing Tests And Verification Coverage

Verified relevant test projects/patterns:

- `Event.Domain.UnitTests` exists for domain behavior.
- `Event.Application.UnitTests` covers handlers/services and should cover subscription commands/fanout service.
- `Event.Persistence.IntegrationTests` covers EF/repository behavior with PostgreSQL and should cover constraints, tenant filters, dedup, and fanout queries.
- `Event.API.IntegrationTests` covers API contracts, HAL, auth, tenant isolation, and should cover new endpoints and links.
- `Explore.Blazor.Client.Tests` uses bUnit and service/component tests; should cover subscription button and notification link behavior.
- `Event.Architecture.Tests` enforces architecture/context/accessibility/HAL-ish conventions and must run after cross-layer work.

Missing today:

- no tests for actor subscription because no feature exists;
- no notification dedup tests;
- no fanout service tests;
- no component tests for Subscribe buttons because they are static;
- no SSE notification refresh tests because no SSE endpoint exists.

### 2.4 Existing Documentation And Contracts

Relevant docs:

- `docs/ARCHITECTURE.md` — Clean Architecture, outbox, background services, caching, BFF.
- `docs/DOMAIN.md` — Actor/Event/Notification domain facts and lookup conventions.
- `docs/API.md` — controller, HAL, OpenAPI, output-cache conventions.
- `docs/BLAZOR.md` — Blazor client/BFF architecture.
- `docs/NOTIFICATIONS.md` — current notification lifecycle and unsupported claims.
- `docs/EMAIL_NOTIFICATIONS.md` — boundary between in-app and SMTP email.
- `docs/OUTBOX_PATTERN.md` — general and specialized outbox conventions.
- `docs/SECURITY-MODEL.md` / `docs/AUTHORIZATION_PATTERNS.md` — authentication/authorization boundaries.
- `docs/MULTI_TENANCY.md` — tenant isolation.
- `docs/TESTING.md` — test project commands and TUnit conventions.
- `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs` — generated API contract artifacts, updated only when API contract is stable.

### 2.5 Current Pain Points / Improvement Areas

1. **Static Subscribe buttons create false affordances.** Profile pages show Subscribe with no backing API, state, or authorization. Replace with HAL-driven component.
2. **No per-actor preference model.** `UserNotificationPreference` is category-level and cannot represent “notify me for this organization but off for that group.”
3. **No idempotent notification fanout.** Existing outbox is at-least-once; without notification dedup, retries could duplicate inbox rows.
4. **Current `EventPublished` dispatcher is single-purpose MQ publishing.** Subscription fanout must avoid breaking external integration dispatch or coupling external publish retries to local notification fanout failures.
5. **Event-published payload lacks actor context and is already a versioned MQContract message.** V1 fanout should use a separate internal outbox payload with actor context rather than changing the external payload.
6. **Notification panel deep links are fragile.** Organization/group route strings do not match current route configuration.
7. **Notification bell marks all read on open.** Keep for v1 unless changed intentionally, but subscription event notifications are actionable and may need future `SeenAt` semantics.
8. **No operational fanout status.** Large fanout needs at least logs/metrics and preferably a resumable fanout run table.
9. **User-to-user subscriptions are privacy-sensitive.** Model can support them, but v1 should gate public user targets behind explicit policy/governance.
10. **Subscriber identity must respect tenant-local status.** A global `UserId` alone does not prove the user is active in the resolved tenant; subscription commands and fanout scans must check `TenantUser.StatusId` and `IsDeleted`.

### 2.6 Unknowns After Investigation

| Unknown | Searched / evidence | Resolution task |
|---|---|---|
| Exact current generated HAL DTO helper shape for new subscription DTOs. | Existing generated client contains HAL resources and DTO `_links`, but no subscription DTOs. | API/client slice must inspect generated client conventions before Blazor service implementation. |
| Whether public user profile routes beyond `/my/profile` exist. | `Routes.razor` shows `/my/profile`; no public `/user/profile/:id` confirmed. | User-target subscription task must either defer UI or create public profile route intentionally. |
| Whether Cerbos static policies need a new resource kind immediately. | Existing `ResourceKinds`/Cerbos parity tests likely require mapping for new resource kind. | Authorization slice must update local fallback and Cerbos policies/tests. |
| Whether fanout should share `EventPublished` or use a dedicated internal event. | Current `EventPublished` is an external MQContract message; CTO re-baseline selected a separate internal fanout event. | Implement `NotificationFanoutRequested` in Phase 4. |
| Whether real-time refresh is worth v1 inclusion. | No existing app notification hub or SSE endpoint; current UI polling works. ASP.NET Core 10 has first-class SSE support for future one-way refresh hints. | Deferred to optional Phase 8 unless user explicitly approves SSE refresh. |

## 3. Proposed Future State

### 3.1 User experience

- Organization and group profile heroes show a real subscription component instead of a static button.
- Event detail organizer card shows “Subscribe to organizer” for the event’s `ActorId`.
- Subscribed state shows `Subscribed` plus a bell dropdown.
- Bell dropdown has `Notify me`, `Notifications off`, and `Unsubscribe`.
- Existing notification bell shows event-published notifications with source actor and deep link to the event.
- “My notification settings” can later list all actor subscriptions and their bell levels.

### 3.2 Data/control flow

```text
Subscribe click
  -> Blazor component sees HAL `subscribe` link
  -> PUT /api/actor-subscriptions/targets/{actorId}
  -> SubscribeToActorCommand validates tenant, active TenantUser, target actor, self-subscribe, policy
  -> ActorSubscription canonical row active with NotificationLevel=All
  -> API returns state + HAL links
  -> UI announces and renders Subscribed + bell

Publish event
  -> PublishEventCommandHandler sets EventStatus=Published
  -> Same transaction writes OutboxMessage(EventPublished) for external MQContract
  -> Same transaction writes OutboxMessage(NotificationFanoutRequested) for internal in-app fanout
  -> OutboxProcessor dispatches each message independently
  -> Composite dispatcher preserves EventPublished MQ publish and routes internal fanout separately
  -> Fanout service creates/resumes NotificationFanoutRun
  -> Service pages active ActorSubscription rows with active TenantUser and NotifyMe/All
  -> Service inserts Notification rows with deterministic DeduplicationKey
  -> Existing NotificationBell polling or optional SSE refresh hint shows unread item
```

### 3.3 Target design principles

- `Actor` is the only subscription target abstraction.
- `ActorSubscription` is the relationship; `Notification` is the delivered inbox item.
- Fanout is asynchronous, idempotent, and on a separate retry path from external MQ publishing.
- Real-time is optional refresh, not delivery truth.
- UI mutation affordances come from HAL links only.
- Tenant isolation is enforced by tenant-scoped FKs, EF filters, and handler checks.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs; handlers map to DTOs.
- Validators are manually instantiated; do not inject `IValidator<T>`.
- Use `Guid` for aggregates/durable rows, `int` for lookup IDs, `long` only for cursors/size where justified.
- New write endpoints are `[Authorize]`; GET endpoints follow repo auth conventions, but user-specific subscription-state GETs must be authenticated.
- HAL `_links` are the single source of truth for Blazor action affordances.
- Preserve Clean Architecture dependency direction: Domain -> none, Application -> Domain/contracts, Persistence/Infrastructure -> Application/Domain, API/Blazor as composition/presentation.
- Every new source/doc file starts with two `ABOUTME:` lines.
- Do not bypass tenant query filters casually; no `IgnoreQueryFilters()` without explicit tenant-safety proof.
- Subscription ownership must resolve an active `TenantUser`; suspended, banned, removed, deleted, or wrong-tenant users cannot subscribe or receive fanout.
- Do not send SMTP, broker messages, real-time refresh messages, or any external side effect inside command transaction lambdas.
- Because the repo is in development mode, prefer clean breaking changes over compatibility shims, but document API/schema changes.

## 5. Architecture And Design Decisions

### Decision 1: Target subscriptions at `Actor`

- **Decision:** Create `ActorSubscription` with `TargetActorId`, not per-type tables.
- **Why:** `Actor` already unifies organization/group/user publishers, and `Event.ActorId` is the source actor for event publication.
- **Alternatives considered:** `OrganizationSubscription`, `GroupSubscription`, `UserSubscription`; JSON polymorphic target fields.
- **Consequences:** Simpler fanout and API; requires strong validation for actor type and public-user policy.
- **Files/layers affected:** Domain entity, EF config, repository, API DTOs, Blazor components.

### Decision 2: Store notification setting on the subscription

- **Decision:** `ActorSubscription.NotificationLevelId` stores `NONE`, `ALL`, and reserved `PERSONALIZED`; unsubscribe is a status transition on the same durable row and resubscribe resets the level to `ALL` in v1.
- **Why:** YouTube separates subscribe state from bell state. Per-actor setting is not covered by current category preferences.
- **Alternatives considered:** global `UserNotificationPreference`; separate topic-preference table in v1.
- **Consequences:** V1 UI stays simple; future topic matrices can be added without replacing core relationship.

### Decision 3: Use a separate internal outbox message for fanout

- **Decision:** Event publish writes the existing external `EventPublished` outbox message and a separate internal `NotificationFanoutRequested` outbox message in the same transaction.
- **Why:** Publish must stay fast and reliable, but local fanout failure should not cause duplicate external MQ publishes or force changes to the versioned MQContract payload.
- **Alternatives considered:** Direct fanout inside publish handler; transport-only refresh without durable notifications; using the external `EventPublished` dispatcher for both MQ and notification fanout.
- **Consequences:** Requires a composite dispatcher route for internal fanout, idempotent consumer behavior, and dedup keys; preserves independent retry/dead-letter behavior.

### Decision 4: Add deterministic notification deduplication

- **Decision:** Add `Notification.DeduplicationKey` with unique `(TenantId, UserId, DeduplicationKey)`.
- **Why:** Outbox is at-least-once; duplicate processing must not duplicate user inbox rows.
- **Alternatives considered:** Check existing notification by event/user before insert; fanout-run-only dedup.
- **Consequences:** Stronger correctness; requires migration and repository insert behavior that handles conflicts.

### Decision 5: Preserve external integration dispatch

- **Decision:** `EventPublishedIntegrationEvent` remains the external MQContract publish path for v1; internal subscription fanout uses its own event type/payload and must not be broker-published.
- **Why:** `EventPublished` currently publishes to MQContract with `typeVersion: "1.0.0"`; replacing or mutating it would create integration risk unrelated to local notification delivery.
- **Alternatives considered:** Replace dispatcher with notification-only dispatcher; add `ActorId` to `EventPublishedIntegrationEvent` during this feature.
- **Consequences:** Implement a composite dispatcher that routes by `OutboxMessage.EventType`, delegates `EventPublished` to `MqContractOutboxMessageDispatcher`, and routes `NotificationFanoutRequested` to the fanout service.

### Decision 6: Use tenant-local subscriber identity

- **Decision:** Store or resolve `SubscriberTenantUserId` alongside the notification recipient `SubscriberUserId`; subscription commands and fanout scans require an active, non-deleted `TenantUser`.
- **Why:** `User` is global, while participation, moderation, and tenant membership live in `TenantUser`.
- **Alternatives considered:** Store only `SubscriberUserId` and trust handlers to add tenant predicates.
- **Consequences:** Stronger tenant isolation and moderation behavior; repository queries must join or include tenant-user state for fanout eligibility.

### Decision 7: SSE real-time refresh deferred

- **Decision:** V1 uses PostgreSQL durable notifications and existing polling. If product needs lower-latency browser refresh later, Phase 8 uses ASP.NET Core 10 Server-Sent Events via `TypedResults.ServerSentEvents` and `IAsyncEnumerable<SseItem<T>>` for one-way notification refresh hints.
- **Why:** Free/self-hostable correctness matters more than low-latency refresh, and the current notification need is one-way server-to-browser state invalidation: “new notification/unread count changed; refresh the inbox.” SSE is simpler than a hub protocol, works over regular HTTP, and matches the persisted-notification source-of-truth model. It still uses one long-lived HTTP request; it does not remove the need for authenticated notification APIs or durable rows.
- **Alternatives considered:** WebSocket-only push; RabbitMQ-required real-time fanout; polling only.
- **Consequences:** Phase 8 needs an authenticated SSE endpoint, reconnect/`Last-Event-ID` behavior, cancellation-safe streaming, polling fallback, and proxy buffering guidance for `text/event-stream`. SSE must carry minimal non-PII refresh hints only; no client-to-server commands should be modeled over SSE.

## 6. Implementation Phases

### Phase 0: Plan Review And Baseline

- **Goal:** Confirm scope, current dirty worktree, and implementation order.
- **Depends on:** This plan.
- **Relevant files:** `dev/active/subscription-notification/*`, `git status`.
- **Acceptance criteria:** User approves/corrects plan; implementation agent records baseline and does not touch unrelated dirty files.
- **Verification:** `git status --short`; read plan/context/tasks.
- **Rollback / failure handling:** If scope changes, update all dev docs before code edits.

#### Task 0.1: Apply Senior CTO re-baseline
- **Type:** docs
- **Layer:** Docs
- **Files:** existing `dev/active/subscription-notification/subscription-notification-plan.md`; existing context/tasks files
- **Description:** Apply Senior CTO review decisions to make the workstream implementation-ready and internally consistent.
- **Acceptance Criteria:** Plan locks v1 scope, internal fanout outbox, tenant-local subscriber identity, real-time refresh deferral with SSE-only Phase 8 planning, and PR split.
- **Dependencies:** None
- **Effort:** M
- **Required Skills/Rules:** `senior-cto-feedback`
- **Validation:** Dev docs updated consistently.

#### Task 0.2: User review of CTO-baselined scope
- **Type:** investigate / docs
- **Layer:** Docs
- **Files:** existing `dev/active/subscription-notification/subscription-notification-plan.md`; existing context/tasks files
- **Description:** User reviews the CTO-baselined v1 scope: organizations/groups only, public user subscriptions deferred, real-time refresh deferred unless SSE refresh is later approved, separate internal fanout outbox message.
- **Acceptance Criteria:** User accepts the baseline or provides explicit overrides; any overrides are recorded in plan/context/tasks before code edits.
- **Dependencies:** 0.1
- **Effort:** S
- **Required Skills/Rules:** `senior-cto-feedback`
- **Validation:** Docs updated.

#### Task 0.3: Planning baseline
- **Type:** investigate
- **Layer:** All
- **Files:** `git status`; active docs; relevant source files
- **Description:** Confirm current working tree and run baseline build for this planning pass.
- **Acceptance Criteria:** Context file records unrelated dirty files and baseline build result.
- **Dependencies:** 0.1
- **Effort:** S
- **Validation:** `dotnet build --configuration Release --verbosity quiet`.

#### Task 0.4: Implementation baseline before runtime edits
- **Type:** investigate
- **Layer:** All
- **Files:** `git status`; active docs; relevant source files
- **Description:** Implementation agent reconfirms the working tree and reruns baseline build if practical before first runtime code change.
- **Acceptance Criteria:** Context file records unrelated dirty files and baseline result.
- **Dependencies:** 0.2
- **Effort:** S
- **Validation:** `dotnet build --configuration Release --verbosity quiet`.

#### Task 0.5: Consider a new intent
- **Type:** docs / investigate
- **Layer:** Agent Context
- **Files:** `.claude/contract/intents.yaml` (only if changed)
- **Description:** Decide whether recurring cross-layer fanout/subscription work needs a new Contribution Contract intent.
- **Acceptance Criteria:** Decision recorded; if changed, context tests run.
- **Dependencies:** 0.2
- **Effort:** S
- **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` if `.claude/**` changes.

### Phase 1: Domain And Persistence Foundation

- **Goal:** Add actor subscription and fanout persistence primitives.
- **Depends on:** Phase 0.
- **Relevant files:** new `Explore.Domain/ActorSubscription.cs`; new lookup entities/enums; existing `Explore.Domain/Notification.cs`; existing `Explore.Persistence/ExploreDbContext.DbSets.cs`; new EF configs; migrations.
- **Related skills/rules:** `clean-architecture-rules`, `dotnet-efcore-guidelines`, domain/persistence/migration rules.
- **Acceptance criteria:** Domain/persistence compile; lookup seeds stable; tenant-scoped constraints/indexes exist; notification dedup unique index exists.
- **Verification:** Domain unit tests if added, persistence integration tests, architecture tests, build.
- **Rollback / failure handling:** Revert migration/model together; do not leave snapshot inconsistent.

#### Task 1.1: Add subscription lookup model
- **Type:** create
- **Layer:** Domain / Persistence
- **Files:** new `Explore.Domain/ActorSubscriptionStatus.cs`; new `Explore.Domain/ActorSubscriptionNotificationLevel.cs`; new `Explore.Domain/Enums/ActorSubscriptionStatusEnum.cs`; new `Explore.Domain/Enums/ActorSubscriptionNotificationLevelEnum.cs`; new EF configs; `LookupTableSeeder.cs` if needed
- **Description:** Add normalized lookup entities for `ACTIVE`, `UNSUBSCRIBED`, `BLOCKED` and notification levels `NONE`, `ALL`, `PERSONALIZED`.
- **Acceptance Criteria:** Lookup IDs/codes/names are stable; enum IDs match seed; API DTOs can expose `*Id/*Code/*Name` later.
- **Dependencies:** 0.4
- **Effort:** M
- **Validation:** `dotnet build --configuration Release --verbosity quiet`; architecture tests.

#### Task 1.2: Add `ActorSubscription` entity and EF configuration
- **Type:** create
- **Layer:** Domain / Persistence
- **Files:** new `Explore.Domain/ActorSubscription.cs`; new `Explore.Persistence/Configurations/Entities/ActorSubscriptionConfiguration.cs`; modify `Explore.Persistence/ExploreDbContext.DbSets.cs`; modify query filters file
- **Description:** Model tenant-scoped subscriber to target-actor relationship with `SubscriberTenantUserId`, denormalized `SubscriberUserId` for notification delivery, audit, soft delete, status, notification level, timestamps, and concurrency stamp.
- **Acceptance Criteria:** Tenant and soft-delete filters applied; FK to `(TenantId, TargetActorId)` enforces same-tenant target; FK to `(TenantId, SubscriberTenantUserId)` enforces tenant-local subscriber; unique non-deleted index on `(TenantId, SubscriberTenantUserId, TargetActorId)` prevents duplicate historical rows; fanout index supports target actor scans over active subscription and active tenant-user state.
- **Dependencies:** 1.1
- **Effort:** L
- **Validation:** Persistence integration tests for uniqueness and tenant filter.

#### Task 1.3: Add notification dedup field
- **Type:** modify
- **Layer:** Domain / Persistence
- **Files:** existing `Explore.Domain/Notification.cs`; existing `Explore.Persistence/Configurations/Entities/NotificationConfiguration.cs`; existing `NotificationRepository.cs`
- **Description:** Add nullable/required-after-backfill `DeduplicationKey` and unique index `(TenantId, UserId, DeduplicationKey)` with appropriate null behavior.
- **Acceptance Criteria:** Fanout-created notifications can be inserted idempotently; existing notification workflows still work before they provide keys.
- **Dependencies:** 1.2
- **Effort:** M
- **Validation:** Persistence integration test: duplicate key for same user/tenant rejected/ignored by repository method.

#### Task 1.4: Add `NotificationFanoutRun` entity
- **Type:** create
- **Layer:** Domain / Persistence
- **Files:** new `Explore.Domain/NotificationFanoutRun.cs`; new status lookup/enum if needed; new EF config; DbSet
- **Description:** Persist fanout progress for `event.published`, including internal source outbox ID, source actor, entity ID, cursor, status, counts, redacted error.
- **Acceptance Criteria:** Unique run per `(TenantId, FanoutKind, EntityTypeId, EntityId, SourceActorId)`; cursor enables resume; no PII in fields.
- **Dependencies:** 1.2
- **Effort:** L
- **Validation:** Persistence integration tests for uniqueness, status transitions, cursor update.

#### Task 1.5: Create EF migration and schema docs update
- **Type:** create / docs
- **Layer:** Persistence / Docs
- **Files:** new migration under `Explore.Persistence/Migrations/`; update `ExploreDbContextModelSnapshot.cs`; update `schemas/islamu-event.md`
- **Description:** Generate migration for new tables/lookups/indexes/dedup field.
- **Acceptance Criteria:** Migration `Up` and `Down` are reversible; seed data is synchronized; schema docs reflect new tables.
- **Dependencies:** 1.1-1.4
- **Effort:** M
- **Validation:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`; architecture tests.

### Phase 2: Repository And Application Contracts

- **Goal:** Provide entity-returning repositories and application DTOs/commands/queries.
- **Relevant files:** new `IActorSubscriptionRepository`, `INotificationFanoutRunRepository`, existing `ITenantUserRepository`, repository implementations, DTOs, validators, handlers.
- **Acceptance criteria:** Handlers implement subscription state transitions, verify active tenant-local subscriber state, map entities to DTOs, manually instantiate validators, pass cancellation tokens.

#### Task 2.1: Add repository contracts and implementations
- **Type:** create
- **Layer:** Application / Persistence
- **Files:** new `Explore.Application/Contracts/Persistence/IActorSubscriptionRepository.cs`; new `INotificationFanoutRunRepository.cs`; new `Explore.Persistence/Repositories/ActorSubscriptionRepository.cs`; new `NotificationFanoutRunRepository.cs`; modify DI registration
- **Description:** Implement entity-returning methods for get state, active scan by actor/cursor, create/update, unsubscribe, notification-level update, fanout run claim/update, and fanout scans that exclude inactive/deleted tenant users.
- **Acceptance Criteria:** No repository returns DTOs; read paths use `AsNoTracking`; writes use tracked entities or atomic `ExecuteUpdateAsync` where safe.
- **Dependencies:** Phase 1
- **Effort:** L
- **Validation:** Persistence integration tests; architecture tests.

#### Task 2.2: Add subscription DTOs and validators
- **Type:** create
- **Layer:** Application
- **Files:** new `Explore.Application/DTOs/ActorSubscription/*.cs`; new validators
- **Description:** Define `ActorSubscriptionStateDto`, `ActorSubscriptionListDto`, `UpdateActorSubscriptionNotificationDto`, and any request DTOs with lookup primitives.
- **Acceptance Criteria:** DTOs expose `NotificationLevelId/Code/Name`, `TargetActorTypeId/Code/Name`, `IsSubscribed`, `ConcurrencyStamp`, and actor display metadata needed by UI.
- **Dependencies:** 2.1
- **Effort:** M
- **Validation:** Build; application unit tests.

#### Task 2.3: Add subscription commands and queries
- **Type:** create
- **Layer:** Application
- **Files:** new `Explore.Application/Features/ActorSubscriptions/Requests/**`; handlers under `Handlers/**`
- **Description:** Implement get state, list my subscriptions, subscribe, update notification level, and unsubscribe.
- **Acceptance Criteria:** Auth current user required; active `TenantUser` required; self-subscribe blocked; cross-tenant actor blocked; organizations/groups allowed; user actors fail closed in v1; subscribe/reactivate is idempotent and resets notification level to `ALL`; unsubscribe is idempotent status transition; concurrency checked on bell update.
- **Dependencies:** 2.2
- **Effort:** XL
- **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.

#### Task 2.4: Add authorization catalog entries
- **Type:** modify
- **Layer:** Application / Security
- **Files:** existing `Explore.Application/Authorization/AuthorizationActions.cs`; `ResourceKinds.cs`; `ResourceDescriptors.cs`; local fallback authorization; Cerbos policy files/tests as needed
- **Description:** Add actor-subscription resource kind/actions and policies for owner-only management.
- **Acceptance Criteria:** Authorization parity tests pass; policies fail closed; no admin endpoint mutates personal subscriptions through user endpoints.
- **Dependencies:** 2.3
- **Effort:** L
- **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` plus API auth tests.

### Phase 3: API Endpoints And HAL Affordances

- **Goal:** Expose subscription state and mutations with explicit contract and HAL links.
- **Relevant files:** new `ActorSubscriptionController`; `RouteNames`; actor/event HATEOAS policies; OpenAPI/client artifacts.
- **Acceptance criteria:** Endpoints are explicit, authenticated where user-specific, documented in API changelog, and covered by integration tests.

#### Task 3.1: Add `ActorSubscriptionController`
- **Type:** create
- **Layer:** API
- **Files:** new `Explore.API/Controllers/ActorSubscriptionController.cs`; modify route names
- **Description:** Add canonical routes under `/api/actor-subscriptions`: `GET /targets/{actorId:guid}`, `PUT /targets/{actorId:guid}`, `PATCH /targets/{actorId:guid}/notification-level`, `DELETE /targets/{actorId:guid}`, and `GET /api/actor-subscriptions` for the current user's list.
- **Acceptance Criteria:** Explicit route templates/names/classification/response types; writes `[Authorize]`; user-specific GETs `[Authorize]`; ProblemDetails response metadata present.
- **Dependencies:** Phase 2
- **Effort:** L
- **Validation:** API integration tests; OpenAPI generation/build.

#### Task 3.2: Add HAL links to actor resources
- **Type:** modify
- **Layer:** API HATEOAS
- **Files:** existing `Explore.API/Hateoas/Policies/ActorLinkPolicy.cs`; assembler if needed
- **Description:** Emit `subscription-state`, `subscribe`, `unsubscribe`, and `subscription-notifications` links according to current user and resource state.
- **Acceptance Criteria:** Links use `yield return`; fail closed; no mutable list policy; detail/collection separation preserved.
- **Dependencies:** 3.1
- **Effort:** M
- **Validation:** HATEOAS API integration tests.

#### Task 3.3: Add organizer subscription links to event detail
- **Type:** modify
- **Layer:** API HATEOAS / Application DTO if needed
- **Files:** event DTO/policy/assembler files; `EventMappingProfile` if needed
- **Description:** Include links allowing event detail UI to query/update subscription for `Event.ActorId`.
- **Acceptance Criteria:** Event detail exposes organizer subscription affordances only when authorized; UI can render button without local role checks.
- **Dependencies:** 3.1
- **Effort:** M
- **Validation:** API integration tests for anonymous/authenticated states.

#### Task 3.4: Add API integration tests for endpoints
- **Type:** test
- **Layer:** API Tests
- **Files:** new `Event.API.IntegrationTests/Features/ActorSubscriptionControllerTests.cs`
- **Description:** Cover subscription state, subscribe, unsubscribe, bell update, auth failure, self-subscribe block, cross-tenant block, inactive tenant-user block, user-target disabled, and idempotency.
- **Acceptance Criteria:** Endpoint behavior, ProblemDetails, rate-limit/idempotency posture, and tenant/auth boundaries are covered before client regeneration.
- **Dependencies:** 3.1-3.3
- **Effort:** L
- **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`.

#### Task 3.5: Regenerate client and update API docs
- **Type:** docs / generated
- **Layer:** API / Blazor Client / Docs
- **Files:** `schemas/openapi.json`; `Explore.Blazor.Client/Clients/EventApiClient.g.cs`; `docs/API_CHANGELOG.md`; maybe `docs/API.md`
- **Description:** Regenerate after API contract stabilizes.
- **Acceptance Criteria:** Generated client includes new endpoints/DTOs; changelog explains breaking/pre-v1 changes.
- **Dependencies:** 3.1-3.4
- **Effort:** M
- **Validation:** API integration tests; Blazor client build/tests.

### Phase 4: Event Published Notification Fanout

- **Goal:** Convert event publication into idempotent in-app notification rows for subscribed users.
- **Relevant files:** internal fanout outbox payload; new fanout service; composite dispatcher route; notification repository insert method; tests.
- **Acceptance criteria:** Active notify-on subscribers receive one notification; off/unsubscribed/inactive tenant users/self recipients are skipped; duplicate outbox retries do not duplicate rows; external `EventPublished` MQ publish remains independently retriable.

#### Task 4.1: Add internal notification fanout outbox request
- **Type:** modify
- **Layer:** Application
- **Files:** existing `PublishEventCommandHandler.cs`; new internal fanout payload model, for example `EventPublishedNotificationFanoutRequested`; do not change `EventPublishedIntegrationEvent.cs` in this slice.
- **Description:** In the publish transaction, write the existing external `EventPublished` outbox message and a second internal `NotificationFanoutRequested` message carrying tenant ID, event ID, source actor ID, actor type, and minimal fanout metadata.
- **Acceptance Criteria:** Fanout has source actor and tenant context without changing the versioned MQContract event; both outbox rows are written atomically with event publish state.
- **Dependencies:** Phase 2
- **Effort:** M
- **Validation:** Application unit tests; build.

#### Task 4.2: Add fanout service
- **Type:** create
- **Layer:** Application / Infrastructure boundary
- **Files:** new `Explore.Application/Contracts/Services/IEventPublishedNotificationFanoutService.cs`; implementation in appropriate layer (Application if repository-only; Infrastructure if dispatcher-owned)
- **Description:** Create/resume fanout run from the internal outbox message, page active subscriptions for the source actor, verify active tenant-user state, insert notifications with dedup keys, update counts/cursor/status, log sanitized outcomes.
- **Acceptance Criteria:** Batch size configurable or constant with task to configure later; inactive/suspended/banned/removed tenant users are skipped; no PII in logs; cancellation respected; repository APIs return entities.
- **Dependencies:** 4.1
- **Effort:** XL
- **Validation:** Application/infrastructure unit tests for success, off, unsubscribed, duplicate, resume, failure.

#### Task 4.3: Add composite outbox dispatcher routing
- **Type:** modify
- **Layer:** Infrastructure
- **Files:** existing `Explore.Infrastructure/Messaging/MqContractOutboxMessageDispatcher.cs`; new composite dispatcher if preferred; DI registration
- **Description:** Register a composite `IOutboxMessageDispatcher` that delegates `EventPublished` to `MqContractOutboxMessageDispatcher`, routes `NotificationFanoutRequested` to the fanout service, and fails closed for unknown event types.
- **Acceptance Criteria:** Existing integration publishing still happens; internal fanout is not broker-published; failures on either outbox message retry only that message's side effect; tests cover routing and unknown-type behavior.
- **Dependencies:** 4.2
- **Effort:** L
- **Validation:** `Explore.Infrastructure.Tests`; outbox integration tests.

#### Task 4.4: Add fanout metrics and operational logs
- **Type:** modify
- **Layer:** Application / Infrastructure / Ops
- **Files:** existing `Explore.Application/Telemetry/BusinessMetrics.cs`; fanout service; docs
- **Description:** Add low-cardinality counters for runs and recipients.
- **Acceptance Criteria:** Tags include tenant ID/fanout kind/outcome only; no user ID/actor ID/title/body in metric tags.
- **Dependencies:** 4.2
- **Effort:** M
- **Validation:** telemetry unit tests if existing pattern; build.

### Phase 5: Blazor Subscription UX

- **Goal:** Replace static buttons with reusable HAL-aware component and add event-detail organizer subscribe affordance.
- **Relevant files:** new component/service; organization/group/event detail pages; tests.
- **Acceptance criteria:** UI state matches server; HAL links gate mutations; accessible; no direct role checks.

#### Task 5.1: Add client subscription service
- **Type:** create
- **Layer:** Blazor Client
- **Files:** new `Explore.Blazor.Client/Contracts/Services/Subscriptions/IActorSubscriptionService.cs`; new `Explore.Blazor.Client/Services/ActorSubscriptionService.cs`; DI registration
- **Description:** Wrap generated API client calls and return friendly state/results with logging consistent with existing services.
- **Acceptance Criteria:** Handles 401/403/404/API exceptions safely; no token handling in client; cancellation supported where pattern allows.
- **Dependencies:** 3.5
- **Effort:** M
- **Validation:** `Explore.Blazor.Client.Tests` service tests.

#### Task 5.2: Add `ActorSubscriptionButton` component
- **Type:** create
- **Layer:** Blazor Client
- **Files:** new `Explore.Blazor.Client/Shared/ActorSubscriptionButton.razor`; `.razor.cs`; `.razor.css`
- **Description:** Render Subscribe/Subscribed/bell dropdown from HAL/state and call service actions.
- **Acceptance Criteria:** Icon buttons have `aria-label`; keyboard usable; announcements on state change; CSS BEM/logical properties; no local role/claim gating.
- **Dependencies:** 5.1
- **Effort:** L
- **Validation:** bUnit tests for states/actions/accessibility; architecture accessibility tests.

#### Task 5.3: Integrate on organization/group profiles
- **Type:** modify
- **Layer:** Blazor Client
- **Files:** existing `OrganizationProfile.razor(.cs/.css)`; `GroupProfile.razor(.cs/.css)`
- **Description:** Replace static Subscribe buttons with component using actor ID/display name/HAL affordances.
- **Acceptance Criteria:** Button appears only when HAL state permits; no false affordance; loading/error states graceful.
- **Dependencies:** 5.2
- **Effort:** M
- **Validation:** component/page tests.

#### Task 5.4: Integrate on event detail organizer card
- **Type:** modify
- **Layer:** Blazor Client
- **Files:** existing `Explore.Blazor.Client/Pages/Events/EventDetail.razor(.cs/.css)`
- **Description:** Add “Subscribe to organizer” near organizer card for `Event.ActorId`.
- **Acceptance Criteria:** Does not compete with registration CTA; uses event HAL/actor state; labels target actor clearly.
- **Dependencies:** 5.2, 3.3
- **Effort:** M
- **Validation:** bUnit tests/manual keyboard smoke.

### Phase 6: Notification Inbox UX Improvements

- **Goal:** Make subscription notifications understandable and correctly linked.

#### Task 6.1: Fix notification deep links
- **Type:** modify / test
- **Layer:** Blazor Client
- **Files:** existing `NotificationBell.razor.cs`; `NotificationItem.razor(.cs)`; route tests
- **Description:** Align entity URL mapping with `Routes.razor`, especially organization/group profile routes.
- **Acceptance Criteria:** Event notifications navigate to event detail; org/group notifications navigate to profile routes; tests cover mappings.
- **Dependencies:** Phase 4
- **Effort:** S
- **Validation:** `Explore.Blazor.Client.Tests`.

#### Task 6.2: Render subscription context in notification items
- **Type:** modify
- **Layer:** Blazor Client
- **Files:** `NotificationItem.razor(.cs/.css)`; maybe DTO mapping if source actor missing
- **Description:** Show reason/source actor in a clear way for subscription notifications.
- **Acceptance Criteria:** User can tell why they got the notification; no color-only meaning; no PII overexposure.
- **Dependencies:** 6.1
- **Effort:** M
- **Validation:** bUnit/accessibility tests.

### Phase 7: Documentation And Operations

- **Goal:** Update source-of-truth docs after behavior exists.

#### Task 7.1: Update notification/domain/API docs
- **Type:** docs
- **Layer:** Docs
- **Files:** `docs/NOTIFICATIONS.md`; `docs/DOMAIN.md`; `docs/ARCHITECTURE.md`; `docs/API_CHANGELOG.md`; `docs/index.md` if adding new doc; maybe `docs/OPERATIONS.md`
- **Description:** Document implemented actor-subscription fanout, boundaries from email/push, outbox idempotency, API contract, and operational signals.
- **Acceptance Criteria:** Implemented vs planned behavior separated; no claims of email/push unless implemented; source anchors valid.
- **Dependencies:** Phases 1-6
- **Effort:** M
- **Validation:** architecture docs quality tests; link checks via `Event.Architecture.Tests`.

### Phase 8: Optional SSE Real-Time Refresh

- **Goal:** Add SSE notification refresh hints only if product requires lower-latency nav badge or inbox refresh than polling.
- **Default status:** Implemented after explicit approval; SSE-only.

#### Task 8.1: Decide SSE refresh scope
- **Type:** investigate
- **Layer:** API / Blazor / Ops
- **Files:** docs/context only unless approved
- **Description:** Decide whether to add an authenticated SSE endpoint and Blazor client stream for notification refresh hints. The decision must cover reconnect/`Last-Event-ID`, cancellation, polling fallback, same-origin cookie/BFF authentication, proxy buffering for `text/event-stream`, and horizontal scale-out expectations.
- **Acceptance Criteria:** Decision recorded. SSE is the planned real-time refresh path for notification refresh hints; polling remains fallback and persisted notifications remain source of truth.
- **Dependencies:** Durable fanout complete
- **Effort:** S
- **Validation:** No code until approved.

#### Task 8.2: Implement SSE refresh hint layer if approved
- **Type:** add
- **Layer:** API / Blazor / Ops
- **Files:** authenticated API SSE endpoint or minimal API; notification refresh stream service; Blazor client stream service/component; docs/tests
- **Description:** Stream minimal notification refresh hints with ASP.NET Core 10 `TypedResults.ServerSentEvents` and `IAsyncEnumerable<SseItem<NotificationRefreshHintDto>>`. Hints should tell the browser to refresh unread count or inbox state; persisted `Notification` rows and existing APIs remain the delivery truth.
- **Acceptance Criteria:** SSE only hints refresh/unread-count changes; payload contains no notification body, email, or unnecessary PII; polling fallback stays; endpoint honors cancellation and reconnect semantics; deployment docs cover reverse-proxy buffering and same-origin cookie authentication.
- **Dependencies:** 8.1 approval
- **Effort:** L
- **Validation:** API/Blazor tests plus docs for proxy/auth/reconnect behavior.

### Recommended PR Split

1. **PR 1 — Domain/persistence foundation:** subscription lookups, `ActorSubscription`, notification dedup field, fanout run table, repositories, migration, schema docs, domain/persistence tests.
2. **PR 2 — Application/API/HAL contract:** commands/queries/validators, authorization catalog/policies, controller routes, HAL links, API integration tests, OpenAPI/client regeneration and API changelog.
3. **PR 3 — Fanout worker path:** internal `NotificationFanoutRequested` outbox payload, composite dispatcher routing, fanout service, metrics/logs, publish-to-notification integration tests.
4. **PR 4 — Blazor and notification UX:** subscription service/component, profile/event-detail integration, notification deep-link fixes, bUnit/accessibility tests.
5. **PR 5 — Operations/docs hardening:** notification/domain/architecture/operations docs, troubleshooting notes, final verification, cleanup of stale static affordance references.

## 7. Testing Strategy

| Requirement | Test layer/project | Likely test files |
|---|---|---|
| Lookup IDs and entity invariants | `Event.Domain.UnitTests` | new `ActorSubscriptionTests.cs` |
| Repository uniqueness, tenant-local subscriber FKs, tenant filters, dedup, fanout cursor | `Event.Persistence.IntegrationTests` | new `ActorSubscriptionRepositoryTests.cs`, `NotificationFanoutRunRepositoryTests.cs`, `NotificationRepositoryDedupTests.cs` |
| Commands: subscribe/unsubscribe/update/get state | `Event.Application.UnitTests` | new `Features/ActorSubscriptions/*Tests.cs`, including inactive `TenantUser` rejection |
| Fanout service idempotency and skip logic | `Event.Application.UnitTests` / `Explore.Infrastructure.Tests` depending placement | new fanout service tests, including inactive tenant users and duplicate internal outbox dispatch |
| API endpoints auth/ProblemDetails/idempotency | `Event.API.IntegrationTests` | new `ActorSubscriptionControllerTests.cs` for auth, user-target disabled, cross-tenant, tenant-user inactive, idempotency |
| HAL links on actor/event resources | `Event.API.IntegrationTests/Features/Hateoas` | new/updated HATEOAS tests |
| Authorization parity | `Event.Architecture.Tests` | existing auth parity tests after policy updates |
| Blazor subscription component | `Explore.Blazor.Client.Tests` | new `ActorSubscriptionButtonTests.cs`, service tests |
| Notification deep links | `Explore.Blazor.Client.Tests` | update/add `NotificationBellTests`/`NotificationItemTests` |
| Accessibility/CSS conventions | `Event.Architecture.Tests`, `Explore.Blazor.Client.Tests` | existing accessibility convention tests |
| Full build | Build | `dotnet build --configuration Release --verbosity quiet` |

Do not use solution-level `dotnet test`. Run test projects individually with `--project`.

## 8. Documentation, Configuration, And Operations Impact

Docs likely updated after implementation:

- `docs/NOTIFICATIONS.md` — implemented subscription-to-in-app fanout lifecycle.
- `docs/EMAIL_NOTIFICATIONS.md` — continue to state email fanout unsupported unless separately implemented.
- `docs/DOMAIN.md` — new `ActorSubscription`, notification dedup, fanout run.
- `docs/ARCHITECTURE.md` — outbox-driven notification fanout and optional SSE refresh boundary.
- `docs/API_CHANGELOG.md` — new endpoints/DTOs/HAL links.
- `schemas/islamu-event.md` — schema changes.
- `schemas/openapi.json` and generated client — if API contract changes.
- `docs/OPERATIONS.md` — metrics/health/backlog only if fanout operational surface added.

Configuration v1 should avoid required new dependencies. Add governance/config keys only when used by code. Candidate future keys: `notifications.subscriptions.enabled`, `notifications.subscriptions.user_targets_enabled`, `notifications.fanout.batch_size`.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Browser continues to use BFF and generated API client; no browser token storage.
- Subscribe/update/unsubscribe endpoints are authenticated writes and should use write rate limiting and idempotency keys.
- User-specific subscription-state GET is authenticated because it reveals private relationship state.
- HAL links gate all mutation controls; Blazor does not inspect local roles/claims.
- Self-subscription is blocked for user actors.
- An active tenant-local `TenantUser` is required for subscription writes and fanout eligibility.
- Cross-tenant subscriptions are blocked by tenant-scoped FKs and handler validation.
- Public user subscriptions are privacy-sensitive and disabled in v1 UI/API; attempts should fail closed with a stable error code until user/public-creator mode is approved.
- Subscriber lists and subscriber counts are not public in v1.
- Logs/metrics must not include email, notification body, event body, user IDs in metric tags, or raw exception payloads.
- Abuse controls: rate limits, max subscription quotas, future block relationships, future actor-level disable switch.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Applicability | Plan |
|---|---|---|
| Multi-tenancy | Applicable | All rows include `TenantId`; tenant filters and scoped FKs; no cross-tenant follows in v1. |
| Federation | Needs investigation | Actor model has ATProto/federation fields, but subscription fanout is local in-app only. Do not publish follow activities until federation roadmap approves it. |
| Localization | Applicable | UI labels (`Subscribe`, `Notify me`, `Off`) should be localizable when localization infrastructure is active; avoid hardcoding in long-lived services. |
| Accessibility | Applicable | Button/dropdown keyboard, ARIA labels, live announcements, target size, logical CSS. |
| Product | Applicable | Organizations/groups first; user targets privacy-gated; event detail subscription is secondary to registration CTA. |
| Self-hosting | Applicable | PostgreSQL-only v1; no RabbitMQ/Redis required. |
| Enterprise | Applicable | Audit fields, idempotency, fanout run status, metrics, policy checks, tenant controls. |

## 11. Observability And Operations

Add low-cardinality metrics:

- `explore.actor_subscriptions.changed` tags: `tenant_id`, `target_actor_type`, `operation`, `outcome`.
- `explore.notification_fanout.runs` tags: `tenant_id`, `fanout_kind`, `outcome`.
- `explore.notification_fanout.recipients` tags: `tenant_id`, `fanout_kind`, `outcome`.
- `explore.notifications.created` tags: `tenant_id`, `notification_type`, `reason`.

Logs should include fanout run ID, outbox ID, event ID, source actor ID, batch size, created/skipped counts, and normalized failure category. Do not log notification bodies or user emails.

Operational failure modes:

- Outbox retries on fanout failure.
- Fanout run records failure cursor/counts.
- Duplicate insertion is no-op/handled by dedup key.
- If SSE refresh later fails, polling remains fallback.

## 12. Migration And Compatibility Plan

Development mode means no backward-compatibility shims are required. Prefer clean schema/API contracts.

Migration sequence:

1. Add lookup tables/seeds.
2. Add `actor_subscriptions`.
3. Add `notification_fanout_runs`.
4. Add `notifications.deduplication_key` nullable or with safe partial unique index.
5. Add indexes and constraints.
6. Update schema docs.

Contract notes:

- Do not change `EventPublishedIntegrationEvent` for this v1 feature. If future external consumers need actor context, handle it as a separate MQContract versioned-contract change with docs and tests.
- Add a new internal fanout outbox payload/event type for subscription notification fanout; it is not part of the public API or broker contract.
- Generated NSwag client should be regenerated only after API endpoints stabilize.
- Existing notifications without dedup keys must remain valid unless a deliberate data reset is approved.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Duplicate notifications from at-least-once outbox | High | High | `DeduplicationKey` unique index and idempotent insert | Duplicate inbox rows in tests/logs | 1.3, 4.2 |
| Fanout breaks or duplicates MQ external publishing | Medium | High | Separate internal fanout outbox event and composite dispatcher tests preserving `MqContractOutboxMessageDispatcher` behavior | MQ publish tests fail / duplicate broker publish evidence | 4.1, 4.3 |
| Cross-tenant subscription leak | Medium | Critical | Tenant-scoped FKs, handler checks, integration tests | Cross-tenant API test failure | 1.2, 2.3, 3.1 |
| Inactive/suspended tenant user receives notifications | Medium | High | `TenantUser` FK/state checks in commands and fanout scans | Tenant-user status tests fail / operator reports | 1.2, 2.1, 4.2 |
| Static UI affordance remains misleading | Medium | Medium | Replace static buttons with HAL-aware component | Component/page tests | 5.2, 5.3 |
| User subscriptions create privacy issue | Medium | High | Disable user targets in v1 UI/API and return stable fail-closed error | API auth/privacy tests | 2.3 |
| Fanout run over-modeled and slows first delivery | Medium | Medium | Implement minimal run table fields; batch processing | Implementation complexity | 1.4, 4.2 |
| Notification deep links route incorrectly | High | Medium | Add route mapping tests | Blazor tests fail | 6.1 |
| Generated client drift | Medium | Medium | Regenerate intentionally after stable contract | API client naming/OpenAPI tests | 3.5 |
| Docs claim email/push support incorrectly | Low | High | Update docs precisely; keep boundaries | Docs review/tests | 7.1 |

## 14. Success Metrics And Definition Of Done

Functional success:

- Users can subscribe/unsubscribe to organization and group actors.
- User-target subscriptions are disabled/fail-closed in v1.
- Bell setting can be set to notify/off and persists.
- Event detail exposes organizer subscription action.
- Event publication creates one in-app notification per eligible subscribed user.
- Off/unsubscribed users receive no subscription notification.
- Suspended, banned, removed, deleted, or wrong-tenant tenant users cannot subscribe and are skipped by fanout.
- Duplicate outbox dispatch does not duplicate notifications.
- External `EventPublished` MQ dispatch remains independent of internal notification fanout retries.
- UI gates actions by HAL links.

Quality gates:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Docs gates:

- `docs/NOTIFICATIONS.md`, `docs/DOMAIN.md`, `docs/API_CHANGELOG.md`, and schema docs updated.
- Dev docs plan/context/tasks reflect final implementation state.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `subscription-notification-context.md`, and `subscription-notification-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `subscription-notification-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `subscription-notification-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report “done” unless docs reflect the actual current state.
5. Every implementation summary to the user must include a developer teaching summary naming patterns, libraries/infrastructure, important files/classes/interfaces/handlers/components, control/data flow, conventions followed, verification, remaining work, and next step.
6. If validation fails, update context/tasks with the failure, likely cause, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract

When an implementation agent finishes a slice, its final response should use:

- **Implemented:** medium-sized developer teaching summary naming Clean Architecture/CQRS/EF/HAL/outbox/UI details.
- **Verified:** exact commands/tests and results.
- **Remaining:** unfinished tasks and risks.
- **Next:** recommended next slice.
- **Docs updated:** whether plan/context/tasks were updated.

## 17. Potential Risks & Unknowns

The hardest part is not the Subscribe button; it is reliable fanout composition. The existing `EventPublished` outbox path currently publishes to MQContract through `MqContractOutboxMessageDispatcher`; local notification fanout must not mutate that broker contract or make external publishing replay whenever local fanout fails. The safer v1 design writes a second internal fanout outbox message in the publish transaction, then routes it through a composite dispatcher and idempotent fanout service. If the implementation skips dedup, relies only on global `UserId` without active `TenantUser` checks, or fans out synchronously inside `PublishEventCommandHandler`, the feature will look simple but fail under retries, crashes, moderation changes, or popular actors.

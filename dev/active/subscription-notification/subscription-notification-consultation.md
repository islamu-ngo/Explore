<!-- ABOUTME: CTO consultation report for YouTube-inspired actor subscriptions and notification fanout. -->
<!-- ABOUTME: Recommends UX, data model, architecture, infrastructure, and phased implementation for ISLAMU Event. -->

# Subscription And Notification Consultation

> **Audience:** Product/Admin | API | Frontend | Platform/Ops | AI agents
> **Status:** Draft consultation
> **Owner:** Product/Admin
> **Last Verified:** 2026-05-29
> **Source Anchors:** `AGENTS.md`, `docs/PROJECT.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/NOTIFICATIONS.md`, `docs/OUTBOX_PATTERN.md`, `docs/EMAIL_NOTIFICATIONS.md`, `Explore.Domain/Notification.cs`, `Explore.Domain/Actor.cs`, `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`, `Explore.Blazor.Client/Layout/NotificationBell.razor.cs`

## 1. Executive Decision

Build this as a first-class **Actor Subscription** feature, not as a property on organizations, groups, users, or notifications.

The canonical subscription target should be `Actor`, because the current domain already models `Actor` as the common identity wrapper for `User`, `Organization`, and `Group`. A user subscribes to an actor. Notification preferences live on that actor subscription. When an event is published by an actor, an outbox-driven fanout process creates durable `Notification` inbox rows for subscribed users whose subscription-level notification setting allows the event-published topic.

The best default implementation is:

1. `ActorSubscription` stores the durable relationship: `SubscriberUserId -> TargetActorId` inside a tenant.
2. `ActorSubscription.NotificationLevel` stores the bell state: initially `NotifyMe` / `Off`, but modeled as an enum/lookup that can evolve to YouTube-style `All`, `Personalized`, `None` without a migration rewrite.
3. `PublishEventCommandHandler` continues to write an `EventPublished` outbox message in the same transaction as event publication.
4. An idempotent `EventPublishedNotificationFanoutService` consumes that outbox event and creates `Notification` rows in pages.
5. The existing notification bell remains the durable inbox UI; add optional SignalR only as a low-latency transport, never as the source of truth.
6. UI buttons must be exposed through HAL links. The Blazor client must show Subscribe, Unsubscribe, and bell-setting affordances only when the relevant `_links` exist.

This is enterprise-grade without paid dependencies: PostgreSQL + EF Core + current outbox infrastructure are enough for correctness. SignalR, Redis backplane, RabbitMQ, Web Push, and email fanout can be optional deployment-tier upgrades after the durable inbox path works.

## 2. Research Basis

This report is based on repository source review plus current external references checked on 2026-05-29.

### Repository facts used

| Area | Current fact |
|---|---|
| Architecture | The platform uses Clean Architecture, CQRS/MediatR, BFF, PostgreSQL/EF Core, HAL/HATEOAS, output cache, HybridCache, ETag middleware, tenant query filters, and outbox-driven side effects. |
| Actor model | `Actor` already has `ActorTypeId`, optional `UserId`, `OrganizationId`, and `GroupId`. This is the right polymorphic subscription target. |
| Event publisher | `Event.ActorId` points to the actor that owns/publishes the event. |
| Event publish flow | `PublishEventCommandHandler` validates readiness, switches status to `Published`, updates the event, writes an `OutboxMessage` of type `EventPublished`, and invalidates caches inside a transaction. |
| Notifications | `Notification` is tenant-scoped, user-owned, supports type/reason/scope, source actor, recipient context actor, entity reference, read/archive/snooze, soft delete, and indexes for unread counts. |
| Notification UI | `NotificationBell` polls unread count every 60 seconds, opens a dropdown panel, marks all read on open, supports scope tabs, and links to `/notifications`. |
| Notification limitations | Existing docs explicitly say in-app notifications do not imply email, push, queue fanout, delivery tracking, or unsubscribe behavior. |
| User preferences | `UserNotificationPreference` exists, but it is category-based and not target-actor-specific. It is not enough for YouTube-style per-channel bell settings. |
| HAL rule | HAL links are the UI source of truth for action affordances; clients must not locally infer from roles/claims. |
| Tenant isolation | EF global query filters enforce tenant isolation; do not bypass tenant filters casually. |

### External product and technology facts used

| Source | Relevant observations |
|---|---|
| YouTube Help: “Subscribe to YouTube channels” (`support.google.com/youtube/answer/4489286`) | YouTube places Subscribe under videos and on channel pages. Subscribing means you see more from the channel; after subscribing, users can change notification settings. YouTube notes that unsubscribe/re-subscribe resets notification settings. |
| YouTube Help: “Manage YouTube notifications” (`support.google.com/youtube/answer/3382248`) | YouTube separates subscription from notification intensity. When users subscribe, they automatically get personalized notifications; the bell lets them switch between all notifications, personalized notifications, or none. YouTube surfaces notifications through mobile, web, and inbox channels. |
| YouTube Help: notification troubleshooting (`support.google.com/youtube/answer/7391308`) | YouTube treats “subscribed but not receiving notifications” as a user-support scenario; settings can exist at channel level, browser level, app level, and email level. |
| ASP.NET Core SignalR docs via Context7 (`/dotnet/aspnetcore.docs`) | SignalR manages real-time connections and can send to all clients, specific users, or groups. Scale-out options include Redis backplane and Azure SignalR Service. Microsoft guidance says Redis should be near the app to reduce latency; Azure SignalR is preferred on Azure. |

Do not copy YouTube’s full personalization algorithm. ISLAMU Event should copy the product separation: **subscribe relationship** is distinct from **notification preference** and **delivery channel**.

## 3. Product Interpretation For ISLAMU Event

The user story is not simply “notify followers when an organization publishes an event.” It is a platform capability:

- A user can subscribe to an organization.
- A user can subscribe to a group.
- A user can subscribe to another user.
- Subscribe affordances appear on actor profile pages.
- Subscribe affordances also appear on event detail pages, where the target is the event publisher actor.
- After subscribing, a bell dropdown appears for that actor subscription.
- When that actor publishes an event, subscribed users see an in-app notification in the nav dropdown.
- The model must be scalable, enterprise-grade, free/self-hostable, and compatible with the current Clean Architecture/CQRS/BFF/HAL stack.

The important distinction:

| Concept | Meaning | ISLAMU Event model |
|---|---|---|
| Subscription | “I follow this actor.” | Durable `ActorSubscription` row. |
| Bell setting | “How noisy should this actor be?” | Per-subscription notification setting. |
| Notification | “A concrete inbox item for this user.” | Existing `Notification` row. |
| Fanout | “Create recipient rows for eligible subscribers.” | Outbox-driven service over subscriptions. |
| Real-time | “Update open browsers quickly.” | Optional SignalR push after durable row exists. |
| Email/push | “Deliver outside app.” | Future optional channels, not required for phase 1. |

## 4. YouTube-Inspired UX Principles To Adopt

### 4.1 Keep subscribe and bell separate

YouTube’s strongest product pattern is not the red button. It is the separation between:

1. subscribing to a channel; and
2. selecting notification intensity for that subscription.

ISLAMU Event should do the same. A user may want to follow an organization’s events without being alerted for every publication. The data model must not collapse “subscribed” into “notify me always.”

Recommended state machine:

| User-visible state | Durable meaning | UI label |
|---|---|---|
| Not subscribed | No active subscription row, or row status `Unsubscribed` | `Subscribe` |
| Subscribed + notify | Active subscription, in-app level `All` or `NotifyMe` | `Subscribed` + active bell |
| Subscribed + notifications off | Active subscription, in-app level `None` | `Subscribed` + muted bell |

Recommended internal enum even if UI starts with two options:

| Internal value | Meaning | Show in v1? |
|---|---|---|
| `None` | Do not create actor-subscription event-published notifications. | Yes: “Off” |
| `All` | Notify for every event publication by this actor. | Yes: “Notify me” |
| `Personalized` / `Highlights` | Future ranking/summary mode. | Not necessary for v1; reserve the value. |

Because ISLAMU Event is an event platform, not a high-volume video feed, `All` can be the v1 default after subscribing. Later, if popular actors publish too frequently, `Personalized` can become the default for high-volume deployments.

### 4.2 Put Subscribe in every place the user has intent

Do not restrict Subscribe to profile pages. YouTube exposes Subscribe from both channel and watch pages because the strongest follow intent happens while consuming content. ISLAMU Event should expose the target actor subscription in these surfaces:

| Surface | Subscribe target | UX rule |
|---|---|---|
| Organization profile | Organization’s actor | Primary CTA near title/banner. |
| Group profile | Group’s actor | Primary CTA near title/banner. |
| Public user profile | User’s actor | Primary CTA if public-user subscriptions are enabled and not viewing self. |
| Event detail hero/byline | `Event.ActorId` | Secondary CTA near organizer card: “Subscribe to organizer.” |
| Event cards/list | Optional later | Avoid clutter in v1; consider compact follow icon only after design validation. |
| Notification item | Source actor | Optional “Manage subscription” menu after v1. |

### 4.3 Treat actor identity clearly

The user should understand what they are subscribing to. On event detail, do not show a vague “Subscribe” button detached from context. Use labels such as:

- `Subscribe to ISLAMU NGO`
- `Subscribe to Brussels Youth Group`
- `Subscribe to Ahmed’s events`

When already subscribed:

- `Subscribed` with a check icon.
- Adjacent bell icon / dropdown.
- Dropdown items: `Notify me`, `Notifications off`, divider, `Unsubscribe`.

### 4.4 Make notification outcomes explainable

A common YouTube problem is “I subscribed but did not get notified.” ISLAMU Event can avoid confusion by making the state explicit:

- If subscribed with notifications off: show muted bell and helper text, “You follow this organizer, but event alerts are off.”
- If subscribed with notify on: show “You will get in-app notifications when this organizer publishes events.”
- If not signed in: show Subscribe only if the HAL/API design supports anonymous visual hints; click opens sign-in, but actual subscribe action remains authenticated.
- In Settings > Notifications, add a “Subscriptions” section listing actors and bell state.

### 4.5 Do not mark all read just because the panel opens forever

The current `NotificationBell` marks all notifications as read when opened. That is YouTube-like, but for an event platform it can be surprising because events are actionable and may require registration. Consider changing the future behavior to:

1. Opening panel marks notifications as “seen” but not “read”; or
2. Opening panel marks as read only after a short delay; or
3. Keep current behavior for now but add explicit visual emphasis and test user expectations.

This requires a new `SeenAt` field if implemented properly. Do not block subscription v1 on it, but include it in the notification-inbox maturity roadmap.

## 5. Recommended Data Model

### 5.1 Core aggregate: `ActorSubscription`

Create a new domain entity under `Explore.Domain`:

```csharp
public sealed class ActorSubscription : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SubscriberUserId { get; set; }
    public Guid TargetActorId { get; set; }
    public int TargetActorTypeId { get; set; }
    public int StatusId { get; set; }
    public int NotificationLevelId { get; set; }
    public DateTime SubscribedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

Design notes:

- `Id` is `Guid` because subscriptions are durable business rows, not lookup rows.
- `TargetActorId` is the canonical target. Do not create `OrganizationSubscription`, `GroupSubscription`, and `UserSubscription` tables.
- `TargetActorTypeId` is denormalized from `Actor.ActorTypeId` for fast filtering and audit readability. It must be validated against the target actor.
- `StatusId` should be a lookup (`Active`, `Unsubscribed`, possibly `Blocked`) or a carefully mapped enum. Lookups use `int` IDs per repo convention.
- `NotificationLevelId` should be a lookup (`None`, `All`, `Personalized`) or enum-backed lookup. Prefer lookup if the public API exposes stable codes/names.
- Keep a soft-delete field but do not rely on soft delete for unsubscribe semantics. Unsubscribe is business state; soft delete is lifecycle cleanup.
- `ConcurrencyStamp` supports safe update of bell settings from multiple tabs.

### 5.2 Required constraints and indexes

Minimum EF/PostgreSQL constraints:

| Constraint/index | Purpose |
|---|---|
| Unique active subscription on `(TenantId, SubscriberUserId, TargetActorId)` where active/not deleted | Prevent duplicate subscribe rows. |
| FK `(TenantId, TargetActorId)` -> `Actor(TenantId, Id)` | Prevent cross-tenant target references. |
| FK `(TenantId, SubscriberUserId)` or `UserId` + tenant filter discipline | Prevent subscribing from one tenant identity into another tenant. |
| Check `SubscriberUserId` cannot equal target actor’s `UserId` for self user subscriptions | Avoid self-follow. May require app validation because check constraints cannot easily join. |
| Index `(TenantId, TargetActorId, StatusId, NotificationLevelId, Id)` | Fanout recipient paging. |
| Index `(TenantId, SubscriberUserId, StatusId, UpdatedAt)` | “My subscriptions” page. |
| Index `(TenantId, TargetActorTypeId, StatusId)` | Admin analytics and filters. |

Use `Id` as the fanout cursor. UUIDv7 ordering gives useful locality; if a strict cursor is needed later, add a `long SequenceId` only if proven necessary.

### 5.3 Lookups

Add normalized lookup tables or stable lookup metadata for:

#### `ActorSubscriptionStatus`

| ID | Code | Meaning |
|---|---|---|
| 1 | `ACTIVE` | User currently subscribes. |
| 2 | `UNSUBSCRIBED` | User intentionally unsubscribed; preserve audit and settings-reset behavior. |
| 3 | `BLOCKED` | Future safety/moderation state where actor/user relationship is disallowed. |

#### `ActorSubscriptionNotificationLevel`

| ID | Code | Meaning |
|---|---|---|
| 1 | `NONE` | Do not generate subscription notifications. |
| 2 | `ALL` | Generate all supported notifications for this actor. |
| 3 | `PERSONALIZED` | Future ranked/highlight notifications. |

For v1 UI, expose `ALL` as `Notify me` and `NONE` as `Off`. Keep `PERSONALIZED` internal or disabled until ranking exists.

### 5.4 Topic model: start fixed, leave room for expansion

Avoid a runtime custom-property-like schema for notification topics. Topics are platform behavior and need typed policy semantics. Start with fixed topic codes:

| Topic | v1? | Meaning |
|---|---:|---|
| `event.published` | Yes | Actor published a new event. |
| `event.updated.major` | Later | Significant event update after publication. |
| `event.cancelled` | Later | Published event was cancelled. |
| `organizer.announcement` | Later | Organizer explicitly broadcasts to subscribers. |
| `live.started` | Not applicable now | Future live-stream/session feature. |

Do not implement per-topic matrices in v1 unless the UI needs them. A single `NotificationLevelId` is enough to satisfy the requested bell dropdown. Add `ActorSubscriptionTopicPreference` only when product truly needs separate toggles.

### 5.5 Fanout run table: strongly recommended

The existing general `OutboxMessage` is enough to trigger work, but large recipient sets need resumability and idempotency. Add `NotificationFanoutRun` as a durable progress table.

Suggested fields:

| Field | Type | Purpose |
|---|---|---|
| `Id` | `Guid` | Fanout run ID. |
| `TenantId` | `Guid` | Tenant isolation. |
| `SourceOutboxMessageId` | `Guid` | Links to `OutboxMessage`. |
| `FanoutKind` | string/lookup | `event.published`. |
| `SourceActorId` | `Guid` | Actor whose subscribers are targeted. |
| `EntityTypeId` | `int` | Notification entity type, e.g. Event. |
| `EntityId` | `Guid` or string | Event ID. Prefer `Guid` internally; existing Notification stores string. |
| `StatusId` | `int` | Pending, Processing, Completed, Failed, DeadLettered. |
| `LastSubscriptionId` | `Guid?` | Resume cursor for paged subscription scanning. |
| `AttemptCount` | `int` | Operational retry count. |
| `CreatedCount` | `int` | Number of notification rows inserted. |
| `SkippedCount` | `int` | Off, muted, self, invalid, deleted, etc. |
| `LastError` | `string?` | Redacted failure category/message. |
| `CreatedAt`, `UpdatedAt`, `CompletedAt` | dates | Audit and operations. |

Unique constraint:

- `(TenantId, FanoutKind, EntityTypeId, EntityId, SourceActorId)` unique.

This ensures an at-least-once outbox retry does not create a second fanout job.

### 5.6 Notification deduplication key

Add a `DeduplicationKey` or `SourceEventKey` to `Notification`.

Recommended key format:

```text
subscription:event.published:{eventId}:user:{recipientUserId}
```

Add a unique index:

```text
(TenantId, UserId, DeduplicationKey)
```

Without this, the at-least-once outbox guarantee can produce duplicate notifications if the worker crashes after inserting rows but before marking the outbox/fanout run complete.

### 5.7 Reuse existing `Notification` shape

Subscription fanout should create normal `Notification` rows:

| Notification field | Value for event-published subscription notification |
|---|---|
| `TenantId` | Event tenant. |
| `UserId` | Subscriber user. |
| `NotificationTypeId` | `EventCreated` or a new `EventPublished` lookup. Prefer adding `EventPublished` if product wording differs from creation. |
| `Title` | `New event from {actorName}` or event title. |
| `Body` | Short event summary/date; do not copy full event content. |
| `NotificationEntityTypeId` | `Event`. |
| `EntityId` | Event ID string. |
| `NotificationScopeId` | Target actor type (`Organization`, `Group`, or `User`). |
| `SourceActorId` | Target actor / event publisher. |
| `RecipientContextActorId` | Target actor. |
| `NotificationReasonId` | `Subscription`. |
| `IsRead` | `false`. |
| `DeduplicationKey` | Deterministic key. |

Important: existing `NotificationEntityTypeId` + `EntityId` shape requires GUID-looking strings due to a check constraint. Event IDs are GUIDs, so this works.

## 6. System Design

### 6.1 End-to-end control flow

```text
User clicks Subscribe
  -> Blazor checks HAL `subscribe` link
  -> API command SubscribeToActorCommand
  -> Handler validates actor, tenant, self-subscribe, policy, idempotency
  -> ActorSubscription row active with NotificationLevel=All
  -> Actor profile/event detail reloads subscription state
  -> UI shows Subscribed + bell dropdown

Organizer publishes event
  -> PublishEventCommandHandler validates readiness and sets EventStatus=Published
  -> Same transaction writes OutboxMessage(EventPublished)
  -> OutboxProcessor claims EventPublished
  -> EventPublishedNotificationFanoutService creates/resumes NotificationFanoutRun
  -> Service pages ActorSubscription rows for Event.ActorId
  -> Service inserts Notification rows idempotently
  -> Optional SignalR notifier sends unread-count delta/new-item signal to online users
  -> Existing NotificationBell polling or SignalR refresh updates nav dropdown
```

### 6.2 Why this is the right architecture

| Requirement | Architecture answer |
|---|---|
| Free/self-hostable | PostgreSQL + existing API background service are sufficient. |
| Enterprise-grade | Durable outbox, idempotent fanout, audit fields, tenant filters, health/metrics. |
| Scalable | Recipient fanout is paged, resumable, and index-backed. Optional Redis/RabbitMQ/SignalR scale-out later. |
| YouTube-inspired | Subscription is separate from bell preference and inbox delivery. |
| Codebase fit | Uses existing `Actor`, `Event.ActorId`, `Notification`, `OutboxMessage`, MediatR, HAL, Blazor services. |
| Multi-tenant safe | Every row carries `TenantId`; cross-tenant subscriptions are blocked. |
| UI-safe | HAL links control whether subscribe/bell actions appear. |
| Testable | Each layer has clear unit/integration tests. |

### 6.3 Do not send notification fanout inside publish transaction

Never create thousands of user notification rows inside `PublishEventCommandHandler`. The publish transaction should stay small:

1. update event status;
2. write one outbox message;
3. invalidate caches;
4. commit.

Fanout happens after commit. This avoids slow publish actions, database lock contention, and partial failures in the user-facing write path.

### 6.4 Fanout batching strategy

Recommended first implementation:

- Batch size: 250 to 1,000 subscriptions per worker iteration.
- Query by `(TenantId, TargetActorId, Status=Active, NotificationLevel != None, Id > cursor)`.
- Insert notifications with deterministic `DeduplicationKey`.
- Use `AddRange` + catch unique conflict, or use PostgreSQL `ON CONFLICT DO NOTHING` if a repository method supports it.
- Update `NotificationFanoutRun.LastSubscriptionId` after each successful batch.
- Mark run complete when no rows remain.

Do not load all subscribers into memory. Do not call `SaveChanges` per recipient.

### 6.5 Idempotency model

There are three independent idempotency layers:

| Layer | Mechanism |
|---|---|
| API writes | Existing `Idempotency-Key` middleware should apply to subscribe/unsubscribe/preference updates. |
| Fanout job | Unique `NotificationFanoutRun` per event-published source. |
| Recipient notification | Unique `Notification(TenantId, UserId, DeduplicationKey)`. |

This handles browser retries, outbox retries, worker crashes, and multi-node duplicate execution.

### 6.6 Personalized notifications: defer the ranking engine

YouTube’s `Personalized` notifications use user-specific signals. ISLAMU Event should not implement a ranking system until it has enough data and privacy review. If the `PERSONALIZED` value exists in v1, treat it as one of these:

1. hidden/reserved; or
2. equivalent to `ALL` for now but labeled internally as `Highlights`; or
3. only generated for high-value topics such as `event.published`, not minor updates.

Do not pretend to have YouTube-like personalization until there are source-grounded ranking signals, explicit privacy behavior, and explainable user controls.

## 7. API And CQRS Design

### 7.1 Resource naming

Prefer actor-centered endpoints because the subscription target is an actor:

| Operation | Endpoint | Auth | Notes |
|---|---|---|---|
| Get my subscription state for actor | `GET /api/actors/{actorId:guid}/subscription` | `[Authorize]` | Returns `ActorSubscriptionStateDto`; 404 if actor inaccessible. |
| Subscribe | `PUT /api/actors/{actorId:guid}/subscription` | `[Authorize]` + write rate limit | Idempotent. Creates or reactivates subscription. |
| Update bell setting | `PATCH /api/actors/{actorId:guid}/subscription/notifications` | `[Authorize]` | Body includes expected concurrency stamp and level. |
| Unsubscribe | `DELETE /api/actors/{actorId:guid}/subscription` | `[Authorize]` | Idempotent. Marks unsubscribed. |
| List my subscriptions | `GET /api/me/subscriptions` or `GET /api/actor-subscriptions` | `[Authorize]` | For settings page. |
| Public subscriber count | `GET /api/actors/{actorId:guid}/subscription-summary` | `[AllowAnonymous]` only if product wants public counts | Cache carefully; respect actor visibility/privacy. |

Controller names can be `ActorSubscriptionController` or nested actions in `ActorController`. A dedicated controller is cleaner once there are multiple operations.

### 7.2 DTOs

Recommended DTOs:

```csharp
public sealed class ActorSubscriptionStateDto
{
    public Guid? Id { get; set; }
    public Guid TargetActorId { get; set; }
    public int TargetActorTypeId { get; set; }
    public string? TargetActorTypeCode { get; set; }
    public string? TargetActorName { get; set; }
    public bool IsSubscribed { get; set; }
    public int NotificationLevelId { get; set; }
    public string NotificationLevelCode { get; set; } = "NONE";
    public string NotificationLevelName { get; set; } = "None";
    public Guid? ConcurrencyStamp { get; set; }
}
```

```csharp
public sealed class UpdateActorSubscriptionNotificationDto
{
    public int NotificationLevelId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}
```

Expose lookup primitives (`*Id`, `*Code`, `*Name`) instead of raw enum wrappers, following repo lookup rules.

### 7.3 Commands and queries

Application layer:

| Request | Purpose |
|---|---|
| `GetActorSubscriptionStateRequest` | Resolve current user’s state for one actor. |
| `GetMyActorSubscriptionsRequest` | List current user’s subscriptions. |
| `SubscribeToActorCommand` | Create/reactivate active subscription. |
| `UpdateActorSubscriptionNotificationLevelCommand` | Change bell setting. |
| `UnsubscribeFromActorCommand` | Mark unsubscribed. |
| `FanoutEventPublishedNotificationsCommand` or service method | Background worker command/service called from outbox dispatcher. |

Validators should be manually instantiated inside handlers, per project rule.

### 7.4 Authorization model

Add `ResourceKinds.ActorSubscription` and actions such as:

- `actor_subscription:create`
- `actor_subscription:update`
- `actor_subscription:delete`
- `actor_subscription:read`

Rules:

- A normal authenticated user can manage only their own subscription rows.
- Admins should not use the same endpoints to mutate another user’s personal subscriptions.
- Future admin moderation can block subscriptions to an actor through separate admin policy, not by impersonating user actions.
- Subscribing to private or disabled actors should be denied by policy.

Controller endpoint attributes:

- GET state/list: `[Authorize]` because it exposes user-specific state.
- Subscribe/update/delete: `[Authorize]`, write rate limiter, idempotency recommended.
- Public count: `[AllowAnonymous]` only if product explicitly wants counts and actor visibility permits it.

### 7.5 HAL links

Actor detail/profile resources should conditionally expose links:

| Link rel | When present | Method |
|---|---|---|
| `subscription-state` | Authenticated user can view state | GET |
| `subscribe` | Authenticated user can subscribe and is not subscribed | PUT |
| `unsubscribe` | Authenticated user has active subscription | DELETE |
| `subscription-notifications` | Authenticated user has active subscription and can update bell | PATCH |

Event detail resources should expose organizer subscription links, either as top-level links or embedded actor links:

| Link rel | Target |
|---|---|
| `organizer-subscription-state` | `GET /api/actors/{event.ActorId}/subscription` |
| `subscribe-organizer` | `PUT /api/actors/{event.ActorId}/subscription` |
| `organizer-subscription-notifications` | `PATCH /api/actors/{event.ActorId}/subscription/notifications` |

The Blazor UI must check `_links` presence. It must not infer “I can subscribe” from local authentication state or role checks.

## 8. Blazor UX And Component Design

### 8.1 Build one reusable component

Create a reusable actor subscription component, e.g.:

```text
Explore.Blazor.Client/Shared/ActorSubscriptionButton.razor
Explore.Blazor.Client/Shared/ActorSubscriptionButton.razor.cs
Explore.Blazor.Client/Shared/ActorSubscriptionButton.razor.css
```

Inputs:

| Parameter | Purpose |
|---|---|
| `ActorId` | Target actor. |
| `ActorDisplayName` | Label text. |
| `Links` or `SubscriptionLinks` | HAL affordances from parent DTO. |
| `InitialState` | Optional loaded subscription state. |
| `Variant` | `ProfilePrimary`, `EventOrganizer`, `Compact`. |
| `OnStateChanged` | Parent can refresh counts/labels. |

Behavior:

- If no `subscribe`/`subscription-state` link: show nothing or sign-in hint depending on product decision.
- If not subscribed and `subscribe` link exists: show `Subscribe` button.
- If subscribed: show `Subscribed` button with check icon and adjacent bell dropdown.
- Bell dropdown includes `Notify me`, `Off`, and `Unsubscribe`.
- Use `IAccessibilityAnnouncerService` to announce state changes.
- Use focus save/restore for dropdown/dialog interactions.
- Use aria labels for icon-only bell button.

### 8.2 Profile page placement

On organization and group profile pages:

- Place Subscribe near the profile hero title, not hidden in tabs.
- Use sticky/visible placement on mobile if profile hero is tall.
- Show subscriber count only if public and useful; do not block v1 on counts.
- For actors the user manages, consider showing `Manage profile` instead of `Subscribe`, but this must come from HAL links.

On user profile pages:

- Current route `/my/profile` is the authenticated user’s own profile. Do not show subscribe there.
- If public user profiles are introduced, subscriptions must respect privacy settings and possibly default disabled for users.

### 8.3 Event detail placement

On event detail pages:

- Near organizer name/avatar: `Subscribe to organizer`.
- If already subscribed: `Subscribed` + bell.
- Do not confuse this with registering for the event. Registration CTA remains the primary event action.
- If the actor is a group under an organization, label the exact target: “Subscribe to {Group}”, and optionally show parent organization separately.

### 8.4 Notification dropdown improvements

The existing nav `NotificationBell` is a good v1 target. Improve it later for subscription notifications:

- Add notification reason chip: `Subscription`.
- Show source actor avatar/name.
- Better deep links: event notifications should navigate to `/events/{eventId}`; organization/group/profile notification links should use existing route names. Current code has mismatches (`/organizations/{id}` vs profile route `/organization/profile/:id`; `/groups/{id}` vs `/group/profile/:id`). Fix when touching notification routing.
- Add quick action: `Manage subscription` for subscription notifications.
- Consider replacing automatic mark-all-read-on-open with explicit read/seen semantics.

### 8.5 Accessibility requirements

Follow `docs/ACCESSIBILITY.md`:

- Icon-only bell needs `aria-label` such as `Notification settings for ISLAMU NGO`.
- Dropdown must be keyboard reachable and closable with Escape.
- State changes should announce politely: `Subscribed to ISLAMU NGO`; `Notifications off for ISLAMU NGO`.
- Do not rely on color alone to distinguish active/muted bell.
- Touch targets must meet minimum size.
- CSS must use logical properties, not `left/right` physical layout, to preserve RTL support.

## 9. Infrastructure Recommendation

### 9.1 Tier 1: PostgreSQL-only durable inbox

This should be the default and required path.

Components:

- Existing API host background service (`OutboxProcessor`).
- PostgreSQL tables: `actor_subscriptions`, `notification_fanout_runs`, `notifications`.
- Existing Blazor polling every 60 seconds.

Pros:

- Free.
- Works in Docker Compose/self-hosting.
- No broker required.
- Reliable because notifications are persisted.
- Easy to back up and restore with application data.

Cons:

- Users may wait up to 60 seconds for nav badge update unless panel reloads or page refreshes.

This is acceptable for v1.

### 9.2 Tier 2: SignalR for low-latency UI

Add SignalR after durable inbox works.

Use SignalR only to tell connected clients:

- “Your unread count changed.”
- “A new notification exists; refresh/list or render this small DTO.”

Do not use SignalR as the queue. If SignalR fails, the notification still exists in PostgreSQL and polling catches it.

Recommended hub:

```text
Explore.API/Hubs/NotificationHub.cs
```

Patterns:

- Authenticate hub connections through the BFF/token forwarding model.
- Use `Clients.User(userId)` for personal notifications.
- For admin/actor dashboards later, use groups such as `tenant:{tenantId}:actor:{actorId}` only when needed.
- Keep payload small and non-sensitive: notification ID, created time, unread count, maybe title. Do not send PII-heavy content over transient channels if not needed.

Scale-out:

- Single node: no extra dependency.
- Multi-node self-host: Redis backplane if Redis is already part of deployment.
- Azure: Azure SignalR Service if deployed on Azure and operator accepts managed dependency.

### 9.3 Tier 3: RabbitMQ fanout transport only when needed

The repo already treats RabbitMQ as optional transport for email dispatch. Follow that precedent.

Do not require RabbitMQ for subscription notifications. Add RabbitMQ only if:

- fanout volumes exceed what the API background worker can process comfortably;
- operators need decoupled workers;
- multi-service topology emerges; or
- there is a concrete SLO for high-throughput fanout.

Even then, PostgreSQL remains source of truth:

- outbox/fanout run rows are durable state;
- broker messages are pointer-only;
- workers are idempotent;
- RabbitMQ payloads do not contain notification body PII if avoidable.

### 9.4 Web Push and email are future channels

In-app notifications should ship first. Browser push and email require deeper preference, consent, unsubscribe, and deliverability work.

| Channel | Recommendation |
|---|---|
| In-app | Required v1. |
| SignalR | Optional v1.5 for immediacy. |
| Email | Future, built over existing `EmailDispatchOutbox`; require per-subscription email level and unsubscribe links. |
| Web Push | Future, requires push subscription storage, VAPID keys, browser permissions, per-device lifecycle, and privacy documentation. |
| Mobile push | Not applicable until mobile app exists. |

## 10. Integration With Existing Notifications

### 10.1 Existing docs boundary must change only after implementation

`docs/NOTIFICATIONS.md` currently states no push/email/fanout claims. After implementation, update it to say:

- subscription-to-in-app fanout is implemented for `event.published`;
- email and push remain unsupported unless separately implemented;
- notifications are created asynchronously from outbox fanout;
- delivery is at-least-once internally with idempotent deduplication.

### 10.2 Existing `NotificationReasonEnum.Subscription` is already useful

The lookup has `Subscription = 4`. Use it for this feature. That is a strong sign the current model anticipated subscription-originated notifications.

### 10.3 Existing `NotificationScopeType` should be reviewed

`LookupTableSeeder` seeds notification scope IDs using `ActorTypeEnum` values. This works for User=1, Organization=2, Group=4, System=5, but it couples scope IDs to actor type IDs. The consultation recommendation is:

- Keep it for v1 if architecture tests and current API rely on it.
- Document the coupling explicitly if subscription feature uses it.
- If refactoring, do it as a separate lookup normalization task with migration/tests.

Do not silently change lookup IDs in the subscription PR.

### 10.4 Existing deep-link routes need cleanup

`NotificationBell.GetEntityUrl` currently maps:

- `organization` -> `/organizations/{id}`
- `group` -> `/groups/{id}`

But routes show:

- organization profile route: `/organization/profile/:id`
- group profile route: `/group/profile/:id`

Event notification links should use the actual event detail route. Before relying on notifications for subscription UX, verify and fix route mapping tests.

## 11. Privacy, Safety, And Governance

### 11.1 Tenant isolation

Default rule: users can subscribe only to actors in their current tenant. This matches EF tenant filters and avoids cross-tenant data leakage.

If cross-tenant public subscriptions are desired later, design it as a federation/discovery feature with explicit mirrored public records. Do not bypass tenant filters.

### 11.2 User-to-user subscription privacy

Subscribing to another user is more sensitive than subscribing to an organization.

Recommended policy:

- Organizations and groups: subscriptions enabled by default if actor is approved/public.
- Users: subscriptions disabled by default until public profiles and privacy controls are mature, or enabled only for users who opt in to public creator mode.
- Do not expose subscriber lists publicly in v1.
- Subscriber counts for user actors should be hidden unless the user opts in.

### 11.3 Blocking and abuse

Subscription systems create abuse vectors: unwanted following, scraping, harassment, and notification spam.

Future-safe model additions:

- `BlockedActorRelationship` or `UserActorBlock` table.
- `ActorSubscriptionStatus.Blocked` reserved value.
- Rate limiting on subscribe/unsubscribe/update.
- Audit events for unusual subscription spikes.
- Admin moderation tools to disable subscriptions for an actor.

### 11.4 Notification volume controls

Enterprise operators need tenant-level control. Add governance keys later:

| Key | Default | Meaning |
|---|---|---|
| `notifications.subscriptions.enabled` | `true` | Master feature switch. |
| `notifications.subscriptions.user_targets_enabled` | `false` initially | Allow subscribing to user actors. |
| `notifications.subscriptions.default_level` | `all` or `personalized` | Default bell setting on subscribe. |
| `notifications.subscriptions.max_subscriptions_per_user` | e.g. `1000` | Abuse/spam guard. |
| `notifications.fanout.batch_size` | e.g. `500` | Worker tuning. |
| `notifications.fanout.enabled` | `true` | Operational kill switch. |

Follow the hierarchical settings cascade if/when these become governance settings.

## 12. Implementation Path

### Phase 0 — Contract and design finalization

Deliverables:

- Decide whether user-to-user subscriptions are enabled in v1 or model-only.
- Decide default notification level after subscribing: recommended `ALL` for v1 in-app event notifications.
- Decide whether public subscriber counts are visible: recommended no for v1.
- Add an implementation plan/tasks file after this consultation if proceeding.

### Phase 1 — Domain and persistence

Add:

- `ActorSubscription` domain entity.
- Lookup enums/entities for subscription status and notification level.
- EF configuration with tenant-scoped FK, unique active index, fanout query index.
- Repository interface and implementation returning entities, never DTOs.
- `Notification.DeduplicationKey` field and unique index.
- `NotificationFanoutRun` entity/config/repository if adopting durable fanout runs immediately.
- Migration and seed updates.

Tests:

- Persistence integration tests for uniqueness and tenant isolation.
- Fanout query index behavior with active/off/unsubscribed subscriptions.
- Notification dedup unique constraint.

### Phase 2 — API/CQRS/HAL subscription controls

Add:

- DTOs and validators.
- Commands/queries for subscribe, unsubscribe, update level, get state, list mine.
- `ActorSubscriptionController`.
- Authorization resource kind/actions and local/Cerbos policy updates if needed.
- HAL policies for actor and event resources exposing subscription links.
- API changelog entries.

Tests:

- API integration tests for authenticated write endpoints.
- Anonymous users cannot mutate subscription state.
- Self-subscribe blocked.
- Cross-tenant actor subscribe blocked.
- HAL links present/absent according to state and auth.
- Idempotency-Key replay does not create duplicates.

### Phase 3 — Fanout from event publication

Add:

- `EventPublishedNotificationFanoutService` in Application or Infrastructure boundary depending on dispatcher ownership.
- Dispatcher branch for `OutboxMessage.EventType == "EventPublished"`.
- Payload compatibility: handle old payloads without `ActorId` by loading the event; add `ActorId` to future `EventPublishedIntegrationEvent` if safe.
- Batch recipient selection from `ActorSubscriptionRepository`.
- Idempotent notification insertion using `DeduplicationKey`.
- Business metrics: attempted fanout, created rows, skipped rows, failed rows.
- Health/ops docs if adding a distinct background worker/health check.

Tests:

- Unit tests: active subscription + notify creates notification.
- Unit tests: notification off creates no notification.
- Unit tests: unsubscribed creates no notification.
- Unit tests: duplicate outbox dispatch does not duplicate notifications.
- Integration tests: publish event creates outbox and fanout creates notification after processor service path.

### Phase 4 — Blazor UI

Add:

- `ActorSubscriptionButton` reusable component.
- Actor subscription service wrapping NSwag client.
- Placement on organization profile, group profile, public user profile if enabled, and event detail organizer area.
- Notification panel item improvements for subscription reason/source actor.
- Accessibility announcements and keyboard behavior.
- CSS using logical properties.

Tests:

- bUnit component tests for not subscribed/subscribed/off states.
- HAL gating tests: component hides mutation controls if links absent.
- Service tests for API errors and idempotent state updates.
- Route/deep-link tests for notification click behavior.

### Phase 5 — Real-time optional layer

Add only after durable fanout works:

- `NotificationHub`.
- Client connection service in Blazor WASM through BFF-compatible auth model.
- Server notifier called after notification row commit.
- Redis backplane optional config if multi-node deployments need it.

Tests:

- Hub authorization tests.
- SignalR notification update test if practical.
- Fallback test: polling still works if SignalR disabled.

### Phase 6 — Settings and enterprise controls

Add:

- User settings page section: list subscriptions and bell state.
- Tenant/instance governance switches.
- Admin dashboard metrics for fanout backlog/failures.
- Optional cleanup/archive policy for old notification rows.

## 13. Best-Practice Details

### 13.1 Transaction boundaries

Use `IUnitOfWork.ExecuteInTransactionAsync` for multi-write subscription commands, especially subscribe/reactivate where you may:

1. load target actor;
2. create/update subscription;
3. maybe update actor subscription count projection;
4. write audit event or outbox event.

Keep external side effects outside the transaction. Do not send SignalR, SMTP, or broker messages inside handler transactions.

### 13.2 Repository boundaries

Repositories must return entities. DTO mapping belongs in handlers. This is a hard project rule.

Correct:

```text
ActorSubscriptionRepository.GetBySubscriberAndActorAsync(...) -> ActorSubscription?
Handler maps ActorSubscription -> ActorSubscriptionStateDto
```

Wrong:

```text
ActorSubscriptionRepository.GetStateDtoAsync(...) -> ActorSubscriptionStateDto
```

### 13.3 Cache invalidation

Subscription state is user-specific. Be careful with output caching:

- Actor profile public data can be cached normally.
- Subscription state endpoint must vary by Authorization or not be output-cached.
- HAL links on actor/event detail that depend on authenticated user must vary by Authorization, consistent with existing `DetailData` cache variance.
- Subscriber count projections can use short cache durations if public.

### 13.4 Metrics

Add low-cardinality metrics only:

| Metric | Tags |
|---|---|
| `explore.actor_subscriptions.changed` | `tenant_id`, `target_actor_type`, `operation`, `outcome` |
| `explore.notification_fanout.runs` | `tenant_id`, `fanout_kind`, `outcome` |
| `explore.notification_fanout.recipients` | `tenant_id`, `fanout_kind`, `outcome` |
| `explore.notifications.created` | `tenant_id`, `notification_type`, `reason` |

Do not tag metrics with user ID, actor ID, event title, email, or raw error text.

### 13.5 Logging

Structured logs should include IDs and normalized categories:

- tenant ID;
- fanout run ID;
- source outbox message ID;
- event ID;
- source actor ID;
- batch size;
- created/skipped counts;
- failure category.

Do not log notification body, event content, user email, or browser push tokens.

### 13.6 Retention

Existing notifications are user-facing operational state. Do not auto-delete them until lifecycle rules are explicit.

Recommended future policy:

- keep unread notifications indefinitely or until user deletes/archives;
- archive/read notifications can be retained for a configurable period;
- hard-delete only after privacy/export requirements are satisfied;
- keep fanout run rows longer than outbox rows for operational audit, or summarize before cleanup.

## 14. Option Scoring

| Option | User value | Complexity | Ops burden | Self-host friendliness | Enterprise readiness | Recommendation |
|---|---|---|---|---|---|---|
| A. Direct notification rows in publish handler | Medium | Low | Low | Medium | Low | Reject: slows publish and risks transaction bloat. |
| B. Existing outbox + direct fanout without fanout run | High | Medium | Low | High | Medium | Acceptable MVP if subscriber volume is small, but add dedup key. |
| C. Existing outbox + durable fanout run + batched inserts | High | Medium/High | Medium | High | High | Recommended default. |
| D. RabbitMQ-required notification fanout | High | High | High | Low/Medium | High | Defer; too much required infrastructure for v1. |
| E. SignalR-only notifications | Medium | Medium | Medium | Medium | Low | Reject: not durable and misses offline users. |
| F. PostgreSQL durable inbox + optional SignalR | High | Medium | Low/Medium | High | High | Recommended delivery model. |

## 15. “Perfect” V1 Definition

A “perfect” first release is not the one with every channel and ranking algorithm. It is the one with the fewest correctness compromises.

V1 acceptance criteria:

1. A user can subscribe/unsubscribe to organization and group actors; user actor support is either enabled with privacy rules or explicitly disabled by policy.
2. Event detail page exposes Subscribe to the event publisher actor.
3. Profile pages expose Subscribe for the profile actor.
4. Subscribed state and bell setting survive reload and are tenant-scoped.
5. Bell `Notify me` creates in-app notifications on future event publication.
6. Bell `Off` does not create subscription notifications, while preserving the subscription relationship.
7. Event publish remains fast because fanout is asynchronous.
8. Duplicate outbox processing cannot duplicate notifications.
9. Existing notification bell shows subscription notifications and links to the event.
10. HAL links, not local role checks, gate all mutation affordances.
11. Build, architecture tests, API integration tests, persistence tests, and Blazor component tests pass.
12. Docs clearly separate implemented in-app fanout from unsupported email/push fanout.

## 16. Key Risks And Mitigations

| Risk | Mitigation |
|---|---|
| Duplicate notifications from outbox retries | Add `Notification.DeduplicationKey` unique per tenant/user. |
| Publish endpoint becomes slow | Do not fan out inside publish transaction. |
| Cross-tenant data leak | Tenant-scoped FKs and no `IgnoreQueryFilters()` shortcuts. |
| UI shows actions user cannot perform | HAL link gating only. |
| User-to-user privacy issues | Disable user targets by default or require public creator opt-in. |
| Notification spam | Per-subscription bell off, future default/highlights, max subscriptions per user. |
| Operational backlog invisible | Fanout run status, metrics, health/readiness if worker is separable. |
| SignalR messages lost | Treat SignalR as refresh hint only; persisted notifications remain source of truth. |
| Route mismatch in notification deep links | Add tests and route-name-based URL mapping. |
| Over-modeling topics too early | Start with `event.published`; reserve topic model for later. |

## 17. Concrete File Areas To Change When Implementing

Likely code areas:

| Layer | Files/folders |
|---|---|
| Domain | `Explore.Domain/ActorSubscription.cs`, lookup entities/enums, `Notification.cs` dedup field. |
| Persistence | `Explore.Persistence/Configurations/Entities/*`, `ExploreDbContext.DbSets.cs`, repository implementations, migrations, lookup seeding. |
| Application contracts | `Explore.Application/Contracts/Persistence/IActorSubscriptionRepository.cs`, fanout service contracts. |
| Application CQRS | `Explore.Application/Features/ActorSubscriptions/**`. |
| Application events | `Explore.Application/Models/IntegrationEvents/EventPublishedIntegrationEvent.cs` to include actor context if safe. |
| API | `Explore.API/Controllers/ActorSubscriptionController.cs`, HATEOAS policies for actor/event resources, route names. |
| Blazor client | reusable subscription component, services, profile pages, event detail page, notification deep-link logic. |
| Tests | Persistence integration, API integration, application unit, Blazor component/service, architecture/HAL tests. |
| Docs | `docs/NOTIFICATIONS.md`, `docs/API_CHANGELOG.md`, `docs/DOMAIN.md`, `docs/ARCHITECTURE.md` if fanout worker/state added. |

## 18. Final Recommendation

Implement actor subscriptions as a durable, tenant-scoped, actor-targeted relationship with per-subscription notification level. Use the current outbox pattern to fan out `EventPublished` into existing in-app `Notification` rows. Keep real-time delivery optional and secondary. Start with in-app notifications only. Expose Subscribe and bell controls from profile pages and event detail pages through HAL links. Preserve future YouTube-style sophistication by modeling notification intensity as an extensible lookup, but do not build ranking/personalization until the platform has real usage signals and privacy governance.

The model should be simple for community self-hosters, robust for enterprise tenants, and extensible for future email, push, federation, and personalization without rewriting the core relationship.

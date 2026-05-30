ABOUTME: Documents implemented in-app notification lifecycle, inbox UI, and API boundaries.
ABOUTME: Separates in-app notifications from SMTP email delivery and unsupported push/fanout claims.

# Notifications

> **Audience:** Admins | Integrators | Contributors
> **Status:** Implemented
> **Owner:** Product/Admin
> **Last Verified:** 2026-05-29
> **Source Anchors:** `Explore.Application/Features/Notifications/`, `Explore.Application/Services/EventPublishedNotificationFanoutService.cs`, `Explore.Application/Services/NotificationRefreshStreamService.cs`, `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`, `Explore.API/Controllers/NotificationController.cs`, `Explore.API/Controllers/ActorSubscriptionController.cs`, `Explore.Blazor.Client/Services/NotificationRefreshStreamClient.cs`, `Explore.Blazor.Client/Layout/NotificationBell.razor.cs`, `Explore.Blazor.Client/Helpers/NotificationNavigationHelper.cs`, `Explore.Blazor.Client/Pages/Notifications/Notifications.razor`, `docs/EMAIL_NOTIFICATIONS.md`

Notifications are authenticated, user-owned, tenant-scoped in-app records. They power the notification bell, notification panel, and full inbox page. Event-published actor-subscription fanout is implemented through the transactional outbox and creates durable in-app `Notification` rows. The SSE stream is a one-way refresh hint for unread count/inbox state; it does not replace durable notification rows, list/detail APIs, SMTP email delivery, push delivery, or external delivery receipts.

## Lifecycle

| State Or Action | Implemented Behavior |
|---|---|
| Create / store | Notification records store tenant, user, scope, type, reason, entity, actor context, read/archive/snooze state, and soft-delete state. |
| Actor subscription fanout | Event publication writes an internal `EventPublishedNotificationFanoutRequested` outbox message. The fanout service scans active organization/group actor subscriptions and creates durable in-app notification rows for active tenant-local subscribers. |
| Deduplication | Fanout notifications use deterministic `Notification.DeduplicationKey` values so outbox retries and duplicate dispatches do not create duplicate inbox rows. |
| Fanout progress | `NotificationFanoutRun` records source event/actor/kind, cursor, status, and aggregate counts so fanout can be retried and inspected without storing PII. |
| List | User-scoped list retrieval supports paging and filters from the API and full inbox UI. |
| Detail | Detail retrieval is ownership-checked for the current user. |
| Unread count | API and bell use unread count for the badge. |
| Real-time refresh hint | Authenticated SSE at `GET /api/notification/stream` emits minimal unread-count refresh hints; it does not carry notification bodies or replace list/detail APIs. |
| Mark read | Single notification read and bulk mark-all-read are implemented. |
| Archive | Archive and unarchive are implemented. |
| Snooze | Snooze and unsnooze are implemented with a `snoozedUntil` value. |
| Delete | Delete is implemented as soft delete, not permanent deletion. |

There is no implemented mark-unread endpoint. Do not document read state as reversible unless source adds that operation.

## API Surface

All notification endpoints require an authenticated user. Handler and repository logic enforce current-user ownership; there is no separate admin role requirement for a user's own inbox.

| Action | Endpoint |
|---|---|
| List notifications | `GET /api/notification` |
| Get notification detail | `GET /api/notification/{id}` |
| Get unread count | `GET /api/notification/unread-count` |
| Stream refresh hints | `GET /api/notification/stream` (`text/event-stream`) |
| Mark one read | `PATCH /api/notification/{id}/read` |
| Mark all read | `POST /api/notification/read-all` |
| Archive or unarchive | `PATCH /api/notification/{id}/archive?archive=true|false` |
| Snooze or unsnooze | `PATCH /api/notification/{id}/snooze?snoozedUntil=...` |
| Soft delete | `DELETE /api/notification/{id}` |

When documenting filters, use the source API names. For notification type filtering, use `notificationTypeId`; do not copy stale shorthand such as `type` unless the controller changes.

Actor-subscription state and mutations are separate authenticated endpoints under `/api/actor-subscriptions`. The API exposes current-user subscription state, subscribe, unsubscribe, and notification-level update flows. UI affordances for subscription actions must be gated from HAL `_links` on actor, event organizer, and actor-subscription resources; clients must not infer those actions from local roles or claims.

## UI Behavior

| Surface | Behavior |
|---|---|
| Notification bell | Shows unread badge, listens for SSE refresh hints, keeps polling as fallback, and opens the panel without marking notifications read just because the panel opened. |
| Notification panel | Shows scope tabs, loads more items, supports item actions, and links to the full inbox. |
| Full inbox page | Supports filters for reason, unread-only, archived-only, and snoozed-only; includes mark-all-read. |
| Notification item | Displays scope/type/reason presentation and navigates to known entity URLs when mappable. |

Clicking a notification item navigates only when the item can be mapped to a supported route: events use `/events/{id}`, organizations use `/organization/profile/{id}`, and groups use `/group/profile/{id}`. Unsupported entity types, including event sessions without a dedicated route, should not be routed to guessed URLs. The item click itself is not the read-state operation; read state is handled by explicit user actions and read endpoints.

## SSE Refresh Hints

`GET /api/notification/stream` is an authenticated `text/event-stream` endpoint. It emits `notification-refresh` events with a minimal payload: `UnreadCount`, `HasUnread`, bounded `Reason`, and `GeneratedAt`.

The browser client uses same-origin cookies through `EventSource`. The API response disables request timeout, sends `Cache-Control: no-store`, and sets `X-Accel-Buffering: no` so reverse proxies do not buffer the stream. The existing 60-second bell polling remains the fallback. Treat SSE as a refresh hint only; notification content and read/archive/snooze state still come from the authenticated notification APIs.

## Email Boundary

In-app notifications are separate from SMTP email delivery:

- The notification feature does not call `IEmailService` or the SMTP implementation.
- Actor-subscription fanout creates in-app `Notification` rows only; it does not send email digests or SMTP messages.
- `docs/EMAIL_NOTIFICATIONS.md` documents direct SMTP delivery and explicitly states notification-to-email fanout is not implemented.
- Do not claim push notifications, email digests, SMTP delivery, external delivery tracking, or email unsubscribe behavior for in-app notifications.

## Unsupported Claims To Avoid

- Permanent deletion.
- Mark-as-unread.
- Push notification delivery.
- Email fanout from notification records.
- Public user-to-user actor subscriptions.
- SSE as delivery truth or as a replacement for notification list/detail APIs.
- External delivery receipts or delivery tracking.
- Event-session deep links unless a matching route is implemented.

## Related Documentation

- [EMAIL_NOTIFICATIONS.md](EMAIL_NOTIFICATIONS.md) - SMTP email delivery boundary.
- [API.md](API.md) - API conventions and error shape.
- [API_COOKBOOK.md](API_COOKBOOK.md) - task-first integration patterns.
- [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md) - authorization pipeline patterns.

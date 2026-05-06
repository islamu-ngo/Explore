ABOUTME: Documents implemented in-app notification lifecycle, inbox UI, and API boundaries.
ABOUTME: Separates in-app notifications from SMTP email delivery and unsupported push/fanout claims.

# Notifications

> **Audience:** Admins | Integrators | Contributors
> **Status:** Implemented
> **Owner:** Product/Admin
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.Application/Features/Notifications/`, `Explore.API/Controllers/NotificationController.cs`, `Explore.Blazor.Client/Layout/NotificationBell.razor.cs`, `Explore.Blazor.Client/Pages/Notifications/Notifications.razor`, `docs/EMAIL_NOTIFICATIONS.md`

Notifications are authenticated, user-owned, tenant-scoped in-app records. They power the notification bell, notification panel, and full inbox page; they do not imply email delivery, push delivery, or a background dispatcher.

## Lifecycle

| State Or Action | Implemented Behavior |
|---|---|
| Create / store | Notification records store tenant, user, scope, type, reason, entity, actor context, read/archive/snooze state, and soft-delete state. |
| List | User-scoped list retrieval supports paging and filters from the API and full inbox UI. |
| Detail | Detail retrieval is ownership-checked for the current user. |
| Unread count | API and bell use unread count for the badge. |
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
| Mark one read | `PATCH /api/notification/{id}/read` |
| Mark all read | `POST /api/notification/read-all` |
| Archive or unarchive | `PATCH /api/notification/{id}/archive?archive=true|false` |
| Snooze or unsnooze | `PATCH /api/notification/{id}/snooze?snoozedUntil=...` |
| Soft delete | `DELETE /api/notification/{id}` |

When documenting filters, use the source API names. For notification type filtering, use `notificationTypeId`; do not copy stale shorthand such as `type` unless the controller changes.

## UI Behavior

| Surface | Behavior |
|---|---|
| Notification bell | Shows unread badge, polls unread count, opens the panel, and marks all notifications read when opened. |
| Notification panel | Shows scope tabs, loads more items, supports item actions, and links to the full inbox. |
| Full inbox page | Supports filters for reason, unread-only, archived-only, and snoozed-only; includes mark-all-read. |
| Notification item | Displays scope/type/reason presentation and navigates to known entity URLs when mappable. |

Clicking a notification item navigates when the item can be mapped to an event, organization, group, or event session. The item click itself is not the read-state operation; read state is handled by the explicit read endpoints and the bell's open behavior.

## Email Boundary

In-app notifications are separate from SMTP email delivery:

- The notification feature does not call `IEmailService` or the SMTP implementation.
- `docs/EMAIL_NOTIFICATIONS.md` documents direct SMTP delivery and explicitly states notification-to-email fanout is not implemented.
- Do not claim push notifications, email digests, queue processing, delivery tracking, or unsubscribe behavior for in-app notifications.

## Unsupported Claims To Avoid

- Permanent deletion.
- Mark-as-unread.
- Push notification delivery.
- Email fanout from notification records.
- External delivery receipts or delivery tracking.
- Deep HAL links to related entities unless source changes; current related-entity behavior should be verified before exposing it as an integration contract.

## Related Documentation

- [EMAIL_NOTIFICATIONS.md](EMAIL_NOTIFICATIONS.md) - SMTP email delivery boundary.
- [API.md](API.md) - API conventions and error shape.
- [API_COOKBOOK.md](API_COOKBOOK.md) - task-first integration patterns.
- [AUTHORIZATION_PATTERNS.md](AUTHORIZATION_PATTERNS.md) - authorization pipeline patterns.

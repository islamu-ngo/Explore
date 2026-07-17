ABOUTME: Documents implemented in-app notification lifecycle, inbox UI, and API boundaries.
ABOUTME: Separates durable in-app notifications, browser Web Push refresh delivery, and SMTP email boundaries.

# Notifications

> **Audience:** Admins | Integrators | Contributors
> **Status:** Implemented
> **Owner:** Product/Admin
> **Last Verified:** 2026-07-02
> **Source Anchors:** `Explore.Application/Features/Notifications/`, `Explore.Application/Features/EventReporting/`, `Explore.Application/Services/EventPublishedNotificationFanoutService.cs`, `Explore.Application/Services/EventModerationNotificationFanoutService.cs`, `Explore.Application/Services/EventModerationOutboxMessageFactory.cs`, `Explore.Application/Services/NotificationRefreshStreamService.cs`, `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`, `Explore.Application/Features/Events/Handlers/Commands/ModerateEventCommandHandler.cs`, `Explore.Application/Features/Events/Handlers/Commands/HeavyRedactEventCommandHandler.cs`, `Explore.Application/Features/EventReporting/Handlers/Commands/ExecuteReportDecisionCommandHandler.cs`, `Explore.API/Controllers/NotificationController.cs`, `Explore.API/Controllers/ActorSubscriptionController.cs`, `Explore.Blazor.Client/Services/Interop/NotificationRefreshStreamClient.cs`, `Explore.Blazor.Client/Layout/NotificationBell.razor.cs`, `Explore.Blazor.Client/Helpers/NotificationNavigationHelper.cs`, `Explore.Blazor.Client/Pages/Notifications/Notifications.razor`, `docs/EMAIL_NOTIFICATIONS.md`

Notifications are authenticated, user-owned, tenant-scoped in-app records. They power the notification bell, notification panel, and full inbox page. Event-published actor-subscription fanout and event-moderation attendee fanout are implemented through the transactional outbox and create durable in-app `Notification` rows. The SSE stream is a one-way refresh hint for unread count/inbox state; it does not replace durable notification rows, list/detail APIs, SMTP email delivery, push delivery, or external delivery receipts.

## Approved Channel Model (Planned, Not Implemented)

The lifecycle-email workstream defines one `NotificationIntent` per business occurrence/recipient and one `NotificationDelivery` per selected channel. In-app notification and email are sibling deliveries, not fallback intents. A failed or unavailable optional email therefore leaves the required in-app delivery on the same logical intent.

The planned policy snapshot records channel requiredness, policy/template version, consent purpose, preference result, disclosure/link authority, and recipient-address source. Dispatch can narrow that ceiling using current state but cannot add a channel, purpose, private location, or link. Reporter case-update and follow-up consent are independent and default false; heavy moderation is generic/linkless; Osprey remains signal-only; Coop may produce an outcome only after its separate durable effect repair and successful local enforcement.

## Lifecycle

| State Or Action | Implemented Behavior |
|---|---|
| Create / store | Notification records store tenant, user, scope, type, reason, entity, actor context, read/archive/snooze state, and soft-delete state. |
| Actor subscription fanout | Event publication writes an internal `EventPublishedNotificationFanoutRequested` outbox message. The fanout service scans active organization/group actor subscriptions and creates durable in-app notification rows for active tenant-local subscribers. |
| Moderation attendee fanout | Light moderation writes `EventLightModeratedNotificationFanoutRequested`; heavy redaction writes `EventHeavyRedactedNotificationFanoutRequested`. Both scan event registration intents and create durable in-app rows for registered users. |
| Deduplication | Fanout notifications use deterministic `Notification.DeduplicationKey` values so outbox retries and duplicate dispatches do not create duplicate inbox rows. Moderation fanout keys include tenant, moderation record, and recipient. |
| Fanout progress | `NotificationFanoutRun` records source event or moderation-record metadata, cursor, status, and aggregate counts so fanout can be retried and inspected without storing recipient PII. |
| List | User-scoped list retrieval supports paging and filters from the API and full inbox UI. |
| Detail | Detail retrieval is ownership-checked for the current user. |
| Unread count | API and bell use unread count for the badge. |
| Real-time refresh hint | Authenticated SSE at `GET /api/notification/stream` emits minimal unread-count refresh hints; it does not carry notification bodies or replace list/detail APIs. |
| Browser Web Push | Authenticated users explicitly enroll one browser device. A durable dispatch outbox sends VAPID-authenticated, encrypted, generic refresh payloads and removes stale subscriptions on push-service `404`/`410` responses. |
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
| Current-user preference matrix | `GET /api/notification/preferences/me` |
| Save current-user preferences | `PUT /api/notification/preferences/me` |
| Set current-user mute | `PUT /api/notification/preferences/me/mute` |
| Get public Web Push configuration | `GET /api/notification/web-push/config` |
| Get current browser subscription status | `GET /api/notification/web-push/subscription?deviceIdentifier=...` |
| Subscribe current browser | `POST /api/notification/web-push/subscriptions` |
| Unsubscribe current browser | `DELETE /api/notification/web-push/subscriptions/{subscriptionId}` |
| Organization preference matrix | `GET|PUT /api/organization/{id}/notification-preferences` and `PUT /api/organization/{id}/notification-preferences/mute` |
| Group preference matrix | `GET|PUT /api/group/{id}/notification-preferences` and `PUT /api/group/{id}/notification-preferences/mute` |

When documenting filters, use the source API names. For notification type filtering, use `notificationTypeId`; do not copy stale shorthand such as `type` unless the controller changes.

Actor-subscription state and mutations are separate authenticated endpoints under `/api/actor-subscriptions`. The API exposes current-user subscription state, subscribe, unsubscribe, and notification-level update flows. UI affordances for subscription actions must be gated from HAL `_links` on actor, event organizer, and actor-subscription resources; clients must not infer those actions from local roles or claims.

Notification preference responses are HAL resources. The Blazor matrix renders `save` and `set-mute` actions only when the server emits those links.

## UI Behavior

| Surface | Behavior |
|---|---|
| Notification bell | Shows unread badge, listens for SSE refresh hints, keeps polling as fallback, and opens the panel without marking notifications read just because the panel opened. |
| Notification panel | Shows scope tabs, loads more items, supports item actions, and links to the full inbox. |
| Full inbox page | Supports filters for reason, unread-only, archived-only, and snoozed-only; includes mark-all-read. |
| Notification item | Displays scope/type/reason presentation and navigates to known entity URLs when mappable. |
| Preference matrix | Lets authorized users choose Email and In-App channels by category for current-user, organization, and group scopes; the current-user matrix also exposes Push enrollment when the browser and server support it. Required categories remain locked by server metadata; global mute suppresses non-essential delivery without deleting saved choices. |
| Browser Web Push | Current-user settings show an explicit Enable action only when the server emits the HAL affordance. Denied permission is terminal until the user changes browser settings; Disable unsubscribes the browser and deactivates the owned server row. |

Clicking a notification item navigates only when the item can be mapped to a supported route: events use `/events/{id}`, organizations use `/organization/profile/{id}`, and groups use `/group/profile/{id}`. Unsupported entity types, including event sessions without a dedicated route, should not be routed to guessed URLs. The item click itself is not the read-state operation; read state is handled by explicit user actions and read endpoints.

## Event Moderation Fanout

Light moderation and heavy redaction deliberately use different notification contracts.

| Moderation Kind | Outbox Payload | Created Notification |
|---|---|---|
| Light moderation | `EventLightModeratedNotificationFanoutRequested` contains tenant id, event id, moderation record id, event title, source actor id, and moderation time. | Uses `EventUpdated`, links to the event entity, and may include the event title because the event content is preserved. |
| Heavy redaction | `EventHeavyRedactedNotificationFanoutRequested` contains tenant id, moderation record id, source actor id, and redaction time. It does not contain event id, title, slug, URL, image URI, object key, or original content. | Uses `General`, has no `NotificationEntityTypeId`, no `EntityId`, no `SourceActorId`, no recipient context actor, and generic copy only. |

Heavy fanout resolves the event id internally from the safe `EventModerationRecord` using an explicit tenant/id repository lookup, then scans registration intents for recipients. The event id is used only for recipient lookup and fanout-run progress. It is not written into attendee-facing notification title/body/entity fields, and the notification item has no route because `NotificationNavigationHelper` only navigates when entity metadata is present.

Event-report intake, triage, assignment, decision capture, provider sync, and provider callbacks do not create user notifications by themselves. Notifications are created only when an `EventReportDecision` is executed through the existing light moderation or heavy redaction command path. Report-driven enforcement links `EventModerationRecord.SourceReportId` and `SourceReportDecisionId` for auditability, but notification payloads still follow the same light/heavy contracts above and must not include reporter text, report IDs, provider payloads, provider case IDs, or reviewer notes.

Moderation fanout is in-app only. It does not call SMTP, push providers, or external brokers for attendee delivery. It resolves notification preferences through the required trust-safety category before creating attendee inbox rows, so category requiredness stays centralized in the preference resolver.

## SSE Refresh Hints

`GET /api/notification/stream` is an authenticated `text/event-stream` endpoint. It emits `notification-refresh` events with a minimal payload: `UnreadCount`, `HasUnread`, bounded `Reason`, and `GeneratedAt`.

The browser client uses same-origin cookies through `EventSource`. The API response disables request timeout, sends `Cache-Control: no-store`, and sets `X-Accel-Buffering: no` so reverse proxies do not buffer the stream. The existing 60-second bell polling remains the fallback. Treat SSE as a refresh hint only; notification content and read/archive/snooze state still come from the authenticated notification APIs.

## Browser Web Push Safety

Web Push carries generic refresh/navigation hints only. Durable authenticated `Notification` rows remain the content and state source of truth; payloads do not contain notification titles, bodies, email addresses, tenant data, or access tokens.

Flood prevention is layered:

- The server sets a bounded `TTL` for every send and stops retries when the dispatch expires.
- The Web Push `Topic` header coalesces pending refresh messages for the same subscription/category at the push service.
- The service worker uses one stable notification `tag` with `renotify: false` so already displayed app notifications replace instead of repeatedly alerting.
- A visible same-origin window receives a refresh message and no OS popup.
- If three app notifications are already displayed, the service worker closes them and shows one generic summary.
- Subscription writes accept only bounded HTTPS endpoints and correctly sized Push API key material. The delivery adapter re-resolves endpoint hosts, blocks private/loopback/link-local/metadata destinations, and refuses redirects before sending, so browser-supplied endpoints cannot become an SSRF path.

`Topic` and `tag` are deliberately separate: `Topic` controls queued transport delivery; `tag` controls displayed OS notification replacement. The service worker validates navigation as a same-origin relative path and focuses an existing app window before opening a new one.

## Email Boundary

In-app notifications are separate from SMTP email delivery:

- The notification feature does not call `IEmailService` or the SMTP implementation.
- Actor-subscription fanout creates in-app `Notification` rows only; it does not send email digests or SMTP messages.
- Light and heavy moderation attendee fanout create in-app `Notification` rows only; they do not send SMTP messages.
- Actor-subscription and registration fallback fanout consult the in-app preference matrix before creating non-required `Notification` rows. Disabled non-required categories skip row creation after dedupe checks and before persistence.
- Event-report intake, report workflow actions, provider sync, and provider callbacks do not send SMTP messages and do not create in-app notifications unless they execute an existing moderation command.
- `docs/EMAIL_NOTIFICATIONS.md` documents direct SMTP delivery, including preference-based skip behavior for direct `EmailDispatchOutbox` rows, and explicitly states notification-to-email fanout is not implemented.
- Do not claim email digests, SMTP delivery, external delivery tracking, or email unsubscribe behavior for in-app notifications.

## Unsupported Claims To Avoid

- Permanent deletion.
- Mark-as-unread.
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

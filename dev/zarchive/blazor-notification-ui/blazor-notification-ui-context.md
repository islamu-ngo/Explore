# Blazor Notification UI — Context

> Last Updated: 2026-04-08

## SESSION PROGRESS

### ✅ COMPLETED (Phases 0–9, 11)
- Phase 0: Swagger + NSwag regeneration
- Phase 1: Service layer (INotificationService + NotificationService + DI)
- Phase 2: Components (NotificationBell + NotificationPanel + NotificationItem)
- Phase 3: NavMenu integration (bell in Authorized section)
- Phase 4: Deep linking, mark-all-read-on-open, loading/empty states
- Phase 5: Scope filtering tabs (All / Personal / Organization / Group) in popover
- Phase 6+7: `/notifications` inbox page with unread filter + "View all" from popover
- Phase 8: NotificationSettingDefinitions (4 settings, registered in SettingRegistry)
- Phase 9: Backend entity extensions (NotificationReason lookup, IsArchived, ArchivedAt, SnoozedUntil, archive/snooze endpoints, new filters)
- Phase 11: NotificationServiceTests (25 tests, all passing)

### PENDING (Phase 10)
- Phase 10: Wire filters end-to-end (blocked on: migration + NSwag regen)

### FUTURE ENHANCEMENTS
- Wire setting definitions to UI (display density, default scope, poll interval, badge count)
- Split-pane inbox layout with NotificationDetail component
- Separate NotificationToolbar/NotificationFilterBar components (if complexity warrants)
- Component render tests for panel and page
- Archive/snooze UI action buttons on NotificationItem

### BLOCKERS
- Phase 10 blocked on: run migration, regenerate swagger.json + NSwag client

---

## Key Design Decisions

### 1. YouTube-Style Mark-All-Read-on-Open ✅ (Implemented)
Opening the notification popover automatically marks all unread as read. Badge resets immediately, API call fires in background.

### 2. Custom Popover (Not MudMenu/MudPopover) ✅ (Implemented)
Uses custom CSS dropdown pattern matching NavMenu's user menu for consistency.

### 3. Scope Tabs (MudToggleGroup) ✅ (Implemented)
MudToggleGroup in both popover and inbox page: All (null) / Personal (1) / Organization (2) / Group (4). Maps to `notificationScopeId` API parameter. Wired through NotificationBell state and Notifications page state independently.

### 4. Polling (60s Timer, Not SignalR) ✅ (Implemented)
Timer-based polling for unread count. Runs for authenticated users, disposed on teardown.

### 5. Component Composition ✅ (Implemented)
```
NavMenu.razor
  └─ NotificationBell.razor          (bell icon + badge + popover trigger)
       └─ NotificationPanel.razor    (popover: scope tabs + list + footer)
            └─ NotificationItem.razor (individual notification row)

/notifications (full page)
  └─ Notifications.razor             (standalone page, scope tabs + unread filter + list)
       └─ NotificationItem.razor     (reused)
```

### 6. Notification Settings (Defined, Not Wired) ✅ (Implemented)
4 settings defined in `NotificationSettingDefinitions.cs`, registered in `SettingRegistry`:
- `notifications.display_density`: comfortable/compact (User scope)
- `notifications.default_scope`: all/personal/organization/group (User scope)
- `notifications.poll_interval_seconds`: 60 default (Tenant scope)
- `notifications.max_badge_count`: 99 default (Tenant scope)
Settings exist in the registry but are NOT yet consumed by components.

### 7. Inbox Page (Single Column) ✅ (Implemented)
- Route: `/notifications`, InteractiveServer, `[Authorize]`
- Single-column MudContainer MaxWidth.Medium layout
- Header + scope tabs + unread toggle + notification list + load more
- **Not** split-pane email-client layout — kept simple for initial implementation

### 8. "View All" Link ✅ (Implemented)
NotificationPanel footer with "View all notifications" MudButton. Closes popover and navigates to `/notifications`.

---

## Backend Capabilities (Current)

**Available for filtering (API ready, pending migration + NSwag regen):**
- `isRead` (bool?) — ✅ used by "Show unread" toggle on inbox page
- `notificationTypeId` (int?) — ✅ available, not exposed in UI
- `notificationScopeId` (int?) — ✅ used by scope tabs in popover + inbox page
- `notificationReasonId` (int?) — ✅ API ready (Direct=1, Mention=2, Assignment=3, Subscription=4, Membership=5, System=6)
- `isArchived` (bool?) — ✅ API ready
- `isSnoozed` (bool?) — ✅ API ready

**New endpoints (pending migration):**
- `PATCH /api/notification/{id}/archive?archive=true/false`
- `PATCH /api/notification/{id}/snooze?snoozedUntil=ISO8601`

**NOT available (no backend support):**
- Assigned-to-me / Created-by-me / Subscribed-by-me (would need additional entity fields)

---

## Files (All Existing)

| File | Status |
|---|---|
| `Explore.Blazor.Client/Layout/NotificationBell.razor(.cs/.css)` | ✅ Complete — scope state, view-all, polling |
| `Explore.Blazor.Client/Layout/NotificationPanel.razor(.cs/.css)` | ✅ Complete — scope tabs, footer, all params |
| `Explore.Blazor.Client/Layout/NotificationItem.razor(.cs/.css)` | ✅ Complete — no changes needed |
| `Explore.Blazor.Client/Pages/Notifications/Notifications.razor(.cs/.css)` | ✅ Complete — inbox page |
| `Explore.Blazor.Client/Contracts/Services/Notifications/INotificationService.cs` | ✅ Complete — scope params |
| `Explore.Blazor.Client/Services/NotificationService.cs` | ✅ Complete — full implementation |
| `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` | ✅ DI registered |
| `Explore.Domain/Settings/Definitions/NotificationSettingDefinitions.cs` | ✅ Complete — 4 settings |
| `Explore.Domain/Settings/SettingRegistry.cs` | ✅ Notification settings registered |
| `Explore.Domain/NotificationReason.cs` | ✅ Lookup entity (Id, MasterCode, FullName, Description) |
| `Explore.Domain/Enums/NotificationReasonEnum.cs` | ✅ 6 values (Direct through System) |
| `Explore.Persistence/Configurations/Entities/NotificationReasonConfiguration.cs` | ✅ EF config |
| `Explore.Application/Features/Notifications/Requests/Commands/ArchiveNotificationCommand.cs` | ✅ Command |
| `Explore.Application/Features/Notifications/Requests/Commands/SnoozeNotificationCommand.cs` | ✅ Command |
| `Explore.Application/Features/Notifications/Handlers/Commands/ArchiveNotificationCommandHandler.cs` | ✅ Handler |
| `Explore.Application/Features/Notifications/Handlers/Commands/SnoozeNotificationCommandHandler.cs` | ✅ Handler |
| `Explore.Blazor.Client.Tests/Services/NotificationServiceTests.cs` | ✅ Complete — 25 tests |

## API Endpoints

```
GET    /api/notification?pageNumber&pageSize&isRead&notificationTypeId&notificationScopeId&notificationReasonId&isArchived&isSnoozed
GET    /api/notification/unread-count?notificationScopeId
GET    /api/notification/{id}
PATCH  /api/notification/{id}/read
POST   /api/notification/read-all
PATCH  /api/notification/{id}/archive?archive=true
PATCH  /api/notification/{id}/snooze?snoozedUntil=2026-04-09T12:00:00Z
DELETE /api/notification/{id}
```

## Quick Resume

To continue work:
1. Read this file + tasks.md
2. Run migration: `dotnet ef migrations add AddNotificationReasonAndArchiveSnooze`
3. Regenerate swagger.json + NSwag client
4. Phase 10: Wire filters end-to-end (Blazor service + UI)
5. Future: Wire settings to UI, split-pane layout, component tests, archive/snooze action buttons

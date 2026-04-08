# Blazor Notification UI — Implementation Plan

> Last Updated: 2026-04-08

## Executive Summary

In-app notification center for the Blazor frontend. The backend (8 API endpoints, full CQRS stack with archive/snooze support) and all Blazor components are complete. The popover bell, scope-filtered inbox page, notification service with tests, setting definitions, and backend entity extensions (NotificationReason, archive, snooze) are all implemented and verified. Pending: migration + NSwag regen + Phase 10 filter wiring.

## Current State — All Implemented

### ✅ Phase 0: Prerequisites
- `swagger.json` has 29 notification matches
- `EventApiClient.g.cs` has all notification methods generated
- NSwag client fully operational

### ✅ Phase 1: Service Layer
- `INotificationService` — 6 methods including scope/read filter params
- `NotificationService` — wraps IEventApiClient with try-catch + logging
- DI registered in `ServiceCollectionExtensions.cs`

### ✅ Phase 2: Components
- `NotificationBell.razor` — bell icon + MudBadge + panel toggle + 60s polling
- `NotificationPanel.razor` — popover with scope tabs, loading/empty states, load-more, "view all" footer
- `NotificationItem.razor` — type icon mapping, scope color, relative time, delete

### ✅ Phase 3: NavMenu Integration
- `<NotificationBell />` in NavMenu inside `<Authorized>` section
- Polling + unread count + panel toggle all handled by NotificationBell

### ✅ Phase 4: Deep Linking + Polish
- Deep linking via `GetEntityUrl()` for event/org/group/eventsession
- Mark-all-read-on-open (YouTube style)
- Loading + empty states

### ✅ Phase 5: Scope Filtering Tabs
- MudToggleGroup added to NotificationPanel header: All / Personal / Organization / Group
- Maps to `notificationScopeId`: null / 1 / 2 / 4
- `SelectedScope` + `OnScopeChanged` parameters on NotificationPanel
- NotificationBell manages `_selectedScope` state, passes to service calls
- `HandleScopeChanged` clears list, resets page, reloads with new scope

### ✅ Phase 6+7: Notification Inbox Page + View All
- `Pages/Notifications/Notifications.razor` — route `/notifications`, InteractiveServer, Authorize
- Single-column layout (MudContainer MaxWidth.Medium)
- Header with title + "Mark all read" button
- Scope tabs (same MudToggleGroup as popover) + unread filter toggle (FilterList icon)
- Loading/empty/list states using NotificationItem + load more
- "View all notifications" footer in NotificationPanel popover
- NotificationBell `HandleViewAll` closes panel and navigates to `/notifications`

### ✅ Phase 8: Notification Setting Definitions
- `NotificationSettingDefinitions.cs` in `Explore.Domain/Settings/Definitions/`
- 4 settings: DisplayDensity (User), DefaultScope (User), PollIntervalSeconds (Tenant), MaxBadgeCount (Tenant)
- Registered in `SettingRegistry.cs`
- **Settings defined but not yet consumed by UI components**

### ✅ Phase 11: Testing
- `NotificationServiceTests.cs` — 25 tests covering all 6 service methods
- Success paths, error paths (ApiException + general Exception), edge cases (null values)
- Follows EventRegistrationServiceTests pattern (TUnit + NSubstitute)
- All 606 Blazor client tests passing

## Architecture

### Data Flow
```
NavMenu                    NotificationBell           NotificationService      NSwag Client         API
  |                              |                          |                      |                 |
  |-- renders ------------------>| OnInit                   |                      |                 |
  |                              |-- GetUnreadCount ------->| ------------------->| --------------->|
  |                              |<-- badge count ----------|<-------------------|<----------------|
  |                              |                          |                      |                 |
  |                              |-- Bell Click ----------->| MarkAllAsRead ----->| --------------->|
  |                              |  (opens popover/nav)     | GetNotifications -->| --------------->|
  |                              |<-- notification list ----|<-------------------|<----------------|
  |                              |                          |                      |                 |
  |                              |-- Scope Changed -------->| GetNotifications -->| --------------->|
  |                              |  (clears + reloads)      |  (with scopeId)    |                 |
  |                              |<-- filtered list --------|<-------------------|<----------------|
```

### Component Hierarchy
```
NavMenu.razor
  +-- NotificationBell.razor               (bell icon + badge + popover trigger)
       +-- NotificationPanel.razor         (popover: scope tabs + list + footer)
            +-- NotificationItem.razor     (individual notification row)

/notifications (full page)
  +-- Notifications.razor                  (standalone: scope tabs + unread filter + list)
       +-- NotificationItem.razor          (reused)
```

### Scope Tab Mapping
| Tab Label     | notificationScopeId | ActorType |
|---------------|---------------------|-----------|
| All           | null                | —         |
| Personal      | 1                   | User      |
| Organization  | 2                   | Organization |
| Group         | 4                   | Group     |

---

### ✅ Phase 9: Backend Entity Extensions
- `NotificationReason` lookup entity (Direct=1, Mention=2, Assignment=3, Subscription=4, Membership=5, System=6)
- `Notification` entity: added `NotificationReasonId`, `IsArchived`, `ArchivedAt`, `SnoozedUntil`
- EF config: `NotificationReasonConfiguration`, updated `NotificationConfiguration` (FK + archive index)
- `DbSet<NotificationReason>` in ExploreDbContext, seeder for 6 values
- DTOs updated with new fields + reason name mapping
- Repository: new filters (reason, archive, snooze) + `ArchiveNotification()` + `SnoozeNotification()`
- `ArchiveNotificationCommand/Handler` + `SnoozeNotificationCommand/Handler`
- Controller: 3 new query params on GetAll + archive/snooze PATCH endpoints
- **Pending**: migration + swagger/NSwag regen

---

## Remaining Work

### Phase 10: Wire All Filters End-to-End (After Migration + NSwag Regen)

- Run migration + regenerate swagger.json + NSwag client
- Update `INotificationService` + `NotificationService` — add reason/archive/snooze params
- "Mentions" filter → `notificationReasonId=2` (Mention)
- Archive/snooze action buttons on NotificationItem
- "Show archived" / "Show snoozed" toggles
- Update `NotificationServiceTests` for new params

### Future Enhancements

| Enhancement | Description |
|---|---|
| Wire settings to UI | Consume NotificationSettingDefinitions in NotificationBell (display density, default scope, poll interval, badge count) |
| Split-pane inbox | Email-client layout with NotificationDetail component for `/notifications` |
| Toolbar component | Extract NotificationToolbar from panel header (Inbox label + action icons) |
| FilterBar component | Extract NotificationFilterBar with reason/archive/snooze filters |
| Component tests | Render tests for NotificationPanel and Notifications page |
| Notification preferences page | Wire SettingsNotifications.razor (currently placeholder) |
| Archive/snooze UX | Swipe-to-archive, snooze picker, visual indicators for archived/snoozed items |

---

## Risk Assessment

### Medium Risk
- **Setting consumption timing**: `IHierarchicalSettingsResolver` needs server-side access. In InteractiveAuto, first render may be WASM where resolver isn't available. Mitigation: Default to standard values, resolve on server prerender.
- **Split layout responsive**: If split-pane is added later, email-client layout needs careful mobile handling. Mitigation: Use MudGrid responsive breakpoints.

### Low Risk
- **Filter state complexity**: Multiple filter dimensions (unread, scope, future reason) compound into complex query state. Mitigation: Centralize filter state in a record if needed.

---

## API Endpoints (Backend — 8 Total)

```
GET    /api/notification?pageNumber&pageSize&isRead&notificationTypeId&notificationScopeId&notificationReasonId&isArchived&isSnoozed
GET    /api/notification/unread-count?notificationScopeId
GET    /api/notification/{id}
PATCH  /api/notification/{id}/read
POST   /api/notification/read-all
PATCH  /api/notification/{id}/archive?archive=true
PATCH  /api/notification/{id}/snooze?snoozedUntil=ISO8601
DELETE /api/notification/{id}
```

## Key Files

| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Layout/NotificationBell.razor(.cs/.css)` | Bell trigger + popover orchestration |
| `Explore.Blazor.Client/Layout/NotificationPanel.razor(.cs/.css)` | Popover panel with scope tabs + footer |
| `Explore.Blazor.Client/Layout/NotificationItem.razor(.cs/.css)` | Notification row component |
| `Explore.Blazor.Client/Pages/Notifications/Notifications.razor(.cs/.css)` | Inbox page |
| `Explore.Blazor.Client/Contracts/Services/Notifications/INotificationService.cs` | Service contract |
| `Explore.Blazor.Client/Services/NotificationService.cs` | Service implementation |
| `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` | DI registration |
| `Explore.Domain/Settings/Definitions/NotificationSettingDefinitions.cs` | 4 notification settings |
| `Explore.Domain/Settings/SettingRegistry.cs` | Setting definitions registry |
| `Explore.Domain/NotificationReason.cs` | Lookup entity |
| `Explore.Domain/Enums/NotificationReasonEnum.cs` | 6 enum values |
| `Explore.Persistence/Configurations/Entities/NotificationReasonConfiguration.cs` | EF config |
| `Explore.Application/Features/Notifications/Requests/Commands/ArchiveNotificationCommand.cs` | Archive command |
| `Explore.Application/Features/Notifications/Requests/Commands/SnoozeNotificationCommand.cs` | Snooze command |
| `Explore.Application/Features/Notifications/Handlers/Commands/ArchiveNotificationCommandHandler.cs` | Archive handler |
| `Explore.Application/Features/Notifications/Handlers/Commands/SnoozeNotificationCommandHandler.cs` | Snooze handler |
| `Explore.Blazor.Client.Tests/Services/NotificationServiceTests.cs` | 25 service tests |
| `Explore.Blazor/Pages/Settings/Components/SettingsNotifications.razor` | Settings page (placeholder) |

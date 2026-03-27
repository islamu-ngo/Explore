# Blazor Notification UI — Context

> Last Updated: 2026-03-04

## SESSION PROGRESS (2026-03-04)

### ✅ COMPLETED
- Research: YouTube notification UI pattern, Blazor/MudBlazor component patterns
- Codebase analysis: NavMenu structure, service layer pattern, NSwag client generation, HAL extensions
- Plan creation with 5 phases

### 🟡 IN PROGRESS
- Nothing yet — awaiting plan approval to start implementation

### ⚠️ BLOCKERS
- `swagger.json` does NOT contain notification endpoints — must regenerate before any Blazor work

---

## Key Design Decisions

### 1. YouTube-Style Mark-All-Read-on-Open
Opening the notification popover automatically marks all unread notifications as read (POST /api/notification/read-all). Badge resets immediately in UI, API call fires in background.

### 2. Custom Popover (Not MudMenu/MudPopover)
NavMenu uses a custom CSS dropdown pattern (not MudBlazor popover). Notification panel follows the same pattern for visual consistency and positioning control.

### 3. Scope Tabs (All / Personal / Organization / Group)
Maps to `NotificationScopeId` filter parameter on API. Default tab is "All" (no filter). Tab selection sends `notificationScopeId` to both GetNotifications and GetUnreadCount.

### 4. Polling (60s Timer, Not SignalR)
No SignalR hub exists yet. Polling unread count every 60s is the pragmatic first step. Timer runs only for authenticated users and is disposed on component teardown.

### 5. Component Composition (Not Monolithic)
Three separate components: `NotificationBell` (trigger), `NotificationPanel` (list container), `NotificationItem` (row). Each has its own CSS isolation file. NotificationBell lives in NavMenu, panel is its child.

---

## Key Files (Existing — Must Modify)

| File | Purpose | Change |
|---|---|---|
| `Explore.Blazor.Client/Layout/NavMenu.razor` | Navbar markup | Add NotificationBell between theme toggle and user dropdown |
| `Explore.Blazor.Client/Layout/NavMenu.razor.cs` | Navbar code-behind | Inject INotificationService, add unread count state, timer |
| `Explore.Blazor.Client/Layout/NavMenu.razor.css` | Navbar styles | Add notification bell BEM styles |
| `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` | DI registration | Add INotificationService |
| `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs` | HAL type converters | Add notification HAL extensions (if HAL-wrapped) |
| `Explore.API/swagger.json` | OpenAPI spec | Must regenerate to include notification endpoints |

## Key Files (To Create)

| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Contracts/Services/INotificationService.cs` | Service contract |
| `Explore.Blazor.Client/Services/NotificationService.cs` | Service implementation |
| `Explore.Blazor.Client/Layout/NotificationBell.razor` | Bell icon + badge component |
| `Explore.Blazor.Client/Layout/NotificationBell.razor.cs` | Bell code-behind |
| `Explore.Blazor.Client/Layout/NotificationBell.razor.css` | Bell styles |
| `Explore.Blazor.Client/Layout/NotificationPanel.razor` | Notification popover panel |
| `Explore.Blazor.Client/Layout/NotificationPanel.razor.cs` | Panel code-behind |
| `Explore.Blazor.Client/Layout/NotificationPanel.razor.css` | Panel styles |
| `Explore.Blazor.Client/Layout/NotificationItem.razor` | Individual notification row |
| `Explore.Blazor.Client/Layout/NotificationItem.razor.cs` | Item code-behind |
| `Explore.Blazor.Client/Layout/NotificationItem.razor.css` | Item styles |

## Key Patterns (From Codebase)

### Service Pattern
```csharp
// Interface in Contracts/Services/
public interface INotificationService
{
    Task<int> GetUnreadCountAsync(int? notificationScopeId = null);
    Task<PaginatedResult<NotificationListDto>> GetNotificationsAsync(int page, int pageSize, int? scopeId = null);
    Task<bool> MarkAllAsReadAsync();
    Task<bool> MarkAsReadAsync(Guid notificationId);
    Task<bool> DeleteAsync(Guid notificationId);
}

// Implementation wraps IEventApiClient with try-catch + logging
// Follows EventRegistrationService pattern exactly
```

### DI Registration Pattern
```csharp
// In ServiceCollectionExtensions.AddSharedApplicationServices()
services.AddScoped<INotificationService, NotificationService>();
```

### NavMenu Injection Pattern
```csharp
[Inject] protected INotificationService NotificationService { get; set; } = null!;
```

### BEM CSS Pattern (NavMenu)
```css
.navbar__notification-bell { /* bell container */ }
.navbar__notification-bell__badge { /* unread count badge */ }
.navbar__notification-panel { /* popover panel */ }
.navbar__notification-panel--open { /* visible state */ }
.navbar__notification-item { /* single notification row */ }
.navbar__notification-item--unread { /* unread modifier */ }
```

## API Endpoints (Backend)

```
GET    /api/notification?pageNumber&pageSize&isRead&notificationTypeId&notificationScopeId
GET    /api/notification/unread-count?notificationScopeId
GET    /api/notification/{id}
PATCH  /api/notification/{id}/read
POST   /api/notification/read-all
DELETE /api/notification/{id}
```

## NSwag Client Generation

The API client is auto-generated:
1. API exports `swagger.json` on startup via `OpenApiExportService`
2. NSwag reads `../Explore.API/swagger.json` and generates `Clients/EventApiClient.g.cs`
3. Build target `GenerateApiClient` runs before `CoreCompile`
4. Generated types follow pattern: `NotificationGETAsync`, `NotificationGET2Async`, etc.

**Critical**: Must run API first to export swagger.json, then rebuild Blazor client.

## Quick Resume

To continue work:
1. Read this file
2. Phase 0: Regenerate swagger.json (run API, copy swagger, rebuild NSwag)
3. Phase 1: Create service layer
4. Phase 2: Create components
5. Phase 3: Wire into NavMenu
6. Build + test after each phase

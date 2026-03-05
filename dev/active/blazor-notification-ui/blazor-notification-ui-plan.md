# Blazor Notification UI — Implementation Plan

> Last Updated: 2026-03-04

## Executive Summary

Implement a YouTube-style notification center in the Blazor frontend for the ISLAMU Event platform. The API backend (6 endpoints) is already complete. This plan covers the Blazor client-side only: a bell icon with unread badge in the navbar, a popover panel showing notifications with scope grouping (personal/organization/group/system), and mark-all-read-on-open behavior.

## Current State

### ✅ Backend API (Complete)
- `NotificationController` with 6 endpoints (all `[Authorize]`)
- DTOs: `NotificationDto`, `NotificationListDto`, `UnreadCountDto`
- Scope metadata: `NotificationScopeId/Name`, `SourceActorId/Name`, `RecipientContextActorId/Name`
- See `dev/active/notification-system/` for full backend context

### ❌ NSwag Client (NOT Generated)
- `swagger.json` does NOT contain notification endpoints yet
- `EventApiClient.g.cs` has zero notification methods
- **Must regenerate before any Blazor work can compile**

### ❌ Blazor Client (Nothing Exists)
- No `INotificationService` / `NotificationService`
- No notification components
- No bell icon in NavMenu
- No notification state management

## Architecture

### Data Flow
```
NavMenu                      NotificationService        NSwag Client         API
  │                                │                        │                 │
  ├─ OnInit ──────────────────────►│ GetUnreadCountAsync ──►│ ──────────────►│
  │◄── badge count ────────────────┤◄──────────────────────┤◄───────────────┤
  │                                │                        │                 │
  ├─ Bell Click ──────────────────►│ MarkAllAsReadAsync ──►│ ──────────────►│
  │  (opens popover)               │ GetNotificationsAsync►│ ──────────────►│
  │◄── notification list ──────────┤◄──────────────────────┤◄───────────────┤
  │                                │                        │                 │
  ├─ Scroll/Load More ────────────►│ GetNotificationsAsync►│ ──────────────►│
  │◄── next page ──────────────────┤◄──────────────────────┤◄───────────────┤
```

### Component Hierarchy
```
NavMenu.razor
  └─ NotificationBell.razor          (bell icon + badge + popover trigger)
       └─ NotificationPanel.razor    (popover: header + scope tabs + list + actions)
            └─ NotificationItem.razor (individual notification row)
```

### YouTube-Style Behavior
1. Bell icon shows **unread count badge** (red dot with number, max "99+")
2. Clicking bell opens popover panel
3. **Opening popover auto-marks all as read** (like YouTube)
4. Badge count resets to 0 after opening
5. Panel shows notifications grouped/filtered by scope tabs (All / Personal / Organization / Group)
6. Each notification row: icon + title + body preview + relative timestamp + scope indicator
7. Click notification → navigate to entity (deep link via `EntityType` + `EntityId`)
8. Polling: refresh unread count every 60 seconds

## Implementation Phases

### Phase 0: Prerequisites (Swagger + NSwag Regeneration)
Generate the swagger.json from the running API, then rebuild the NSwag client to get notification methods.

### Phase 1: Service Layer
- `INotificationService` contract interface
- `NotificationService` implementation wrapping `IEventApiClient`
- `NotificationState` scoped service for cross-component state (unread count, panel open state)
- DI registration in `ServiceCollectionExtensions.cs`

### Phase 2: Notification Components
- `NotificationBell.razor` — bell icon with MudBadge, click handler
- `NotificationPanel.razor` — popover with scope tabs, notification list, infinite scroll
- `NotificationItem.razor` — individual notification row with icon, text, timestamp
- CSS isolation files for all three components

### Phase 3: NavMenu Integration
- Inject `INotificationService` + `NotificationState` in NavMenu
- Place `NotificationBell` in the `navbar__actions` div (before user dropdown)
- Load unread count on init
- Timer-based polling for unread count refresh

### Phase 4: HAL Extensions + Deep Linking
- Add notification HAL resource extensions to `HalResourceExtensions.cs`
- Notification item click → navigate to entity URL based on EntityType + EntityId
- Handle scope-specific navigation (org dashboard vs personal vs group)

### Phase 5: Testing
- Blazor component tests for NotificationBell, NotificationPanel, NotificationItem
- Service unit tests for NotificationService

## Risk Assessment

### High Risk
- **NSwag regeneration may fail** if API doesn't start properly or swagger.json format changes. Mitigation: Manual swagger.json export if needed.
- **HAL wrapper complexity**: Notification endpoints may generate HAL-wrapped types. Mitigation: Follow existing `HalResourceExtensions` pattern.

### Medium Risk
- **Popover positioning on mobile**: Custom navbar dropdown + MudBlazor popover may clash. Mitigation: Use the same custom dropdown pattern as user menu.
- **Polling performance**: 60s timer on every authenticated page. Mitigation: Only poll when user is authenticated; cancel timer on dispose.

### Low Risk
- **Scope tabs UX**: Users may not understand scope filtering initially. Mitigation: Default to "All" tab, clear labels.

## Potential Risks & Unknowns

The biggest risk is the NSwag client regeneration step. The notification API endpoints haven't been included in `swagger.json` yet, so the entire Blazor implementation depends on successfully exporting a new OpenAPI spec and regenerating the NSwag client. If the API has build issues or the OpenAPI export service doesn't pick up the new controller, this will block all subsequent work. The second risk is that the notification endpoints may generate HAL-wrapped response types (like `HalCollectionResourceOfNotificationListDto`) rather than plain `PaginatedResult`, which would require new entries in `HalResourceExtensions.cs` — the exact generated types won't be known until NSwag runs.

# Blazor Notification UI — Task Checklist

> Last Updated: 2026-03-04

## Phase 0: Prerequisites

- [ ] **0.1** Run API to export swagger.json with notification endpoints
- [ ] **0.2** Rebuild Blazor client to regenerate NSwag client
- [ ] **0.3** Verify notification methods exist in EventApiClient.g.cs

## Phase 1: Service Layer

- [ ] **1.1** Create `INotificationService` interface in `Contracts/Services/`
- [ ] **1.2** Create `NotificationService` implementation wrapping IEventApiClient
- [ ] **1.3** Register `INotificationService` in `ServiceCollectionExtensions.cs`
- [ ] **1.4** Add HAL resource extensions for notification types (if HAL-wrapped)
- [ ] **1.5** Build + verify no errors

## Phase 2: Notification Components

- [ ] **2.1** Create `NotificationBell.razor` + `.razor.cs` + `.razor.css`
- [ ] **2.2** Create `NotificationPanel.razor` + `.razor.cs` + `.razor.css`
- [ ] **2.3** Create `NotificationItem.razor` + `.razor.cs` + `.razor.css`
- [ ] **2.4** Build + verify no errors

## Phase 3: NavMenu Integration

- [ ] **3.1** Inject `INotificationService` in NavMenu.razor.cs
- [ ] **3.2** Add `NotificationBell` component in NavMenu.razor `navbar__actions` div
- [ ] **3.3** Add CSS for notification bell positioning in NavMenu.razor.css
- [ ] **3.4** Wire up unread count loading in NavMenu OnInitializedAsync
- [ ] **3.5** Add 60s polling timer for unread count refresh
- [ ] **3.6** Build + verify no errors

## Phase 4: Deep Linking + Polish

- [ ] **4.1** Implement notification click → navigate to entity URL
- [ ] **4.2** Add scope filtering tabs in NotificationPanel
- [ ] **4.3** Add "Mark all as read" button behavior (on panel open)
- [ ] **4.4** Handle loading states, error states, empty states
- [ ] **4.5** Build + verify no errors

## Phase 5: Testing

- [ ] **5.1** Create Blazor client tests for NotificationService
- [ ] **5.2** Visual QA with Playwriter (if requested)
- [ ] **5.3** Update dev-docs

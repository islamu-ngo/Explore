# Blazor Notification UI — Task Checklist

> Last Updated: 2026-04-08

## Phase 0: Prerequisites ✅

- [x] **0.1** Run API to export swagger.json with notification endpoints
- [x] **0.2** Rebuild Blazor client to regenerate NSwag client
- [x] **0.3** Verify notification methods exist in EventApiClient.g.cs

## Phase 1: Service Layer ✅

- [x] **1.1** Create `INotificationService` interface in `Contracts/Services/Notifications/`
- [x] **1.2** Create `NotificationService` implementation wrapping IEventApiClient
- [x] **1.3** Register `INotificationService` in `ServiceCollectionExtensions.cs`
- [x] **1.4** HAL extensions — not needed (NSwag handles HAL directly)

## Phase 2: Notification Components ✅

- [x] **2.1** Create `NotificationBell.razor` + `.razor.cs` + `.razor.css`
- [x] **2.2** Create `NotificationPanel.razor` + `.razor.cs` + `.razor.css`
- [x] **2.3** Create `NotificationItem.razor` + `.razor.cs` + `.razor.css`
- [x] **2.4** Build verified — 0 errors

## Phase 3: NavMenu Integration ✅

- [x] **3.1** Place `<NotificationBell />` in NavMenu `<Authorized>` section
- [x] **3.2** Bell handles own state (polling, unread count, panel toggle)
- [x] **3.3** NavMenu has no notification-specific code

## Phase 4: Deep Linking + Polish ✅

- [x] **4.1** Deep linking via `GetEntityUrl()` for event/org/group/eventsession
- [x] **4.3** Mark-all-read-on-open (YouTube style)
- [x] **4.4** Loading states + empty states

## Phase 5: Scope Filtering Tabs ✅

- [x] **5.1** Add MudToggleGroup to NotificationPanel header (All / Personal / Organization / Group)
- [x] **5.2** Add `SelectedScope` + `EventCallback<int?> OnScopeChanged` parameters to NotificationPanel
- [x] **5.3** Add scope state to NotificationBell, pass `_selectedScope` to service calls
- [x] **5.4** Add `HandleScopeChanged` method — clears list, resets page, reloads with new scope
- [x] **5.5** Add BEM styles for toolbar section in NotificationPanel.razor.css
- [x] **5.6** Build verified — 0 errors

## Phase 6+7: Notification Inbox Page + View All ✅

- [x] **6.1** Create `Pages/Notifications/Notifications.razor` + `.razor.cs` + `.razor.css`
  - Route: `/notifications`, render mode: InteractiveServer, `[Authorize]`
  - MudContainer MaxWidth.Medium, single-column layout
  - Header with overline + h4 + "Mark all read" button
  - Scope tabs toolbar (same MudToggleGroup as popover)
  - Unread filter toggle (FilterList icon button)
  - Loading/empty/list states using NotificationItem
  - Load more footer
- [x] **6.2** Add "View all notifications" footer to NotificationPanel
- [x] **6.3** Add `OnViewAll` EventCallback parameter to NotificationPanel
- [x] **6.4** Wire `HandleViewAll` in NotificationBell (close panel + navigate to /notifications)
- [x] **6.5** Add footer BEM styles in NotificationPanel.razor.css
- [x] **6.6** Build verified — 0 errors

## Phase 8: Notification Setting Definitions ✅

- [x] **8.1** Create `NotificationSettingDefinitions.cs` in `Explore.Domain/Settings/Definitions/`
  - `DisplayDensity`: String, default "comfortable", User scope, allowed ["comfortable", "compact"]
  - `DefaultScope`: String, default "all", User scope, allowed ["all", "personal", "organization", "group"]
  - `PollIntervalSeconds`: Integer, default "60", Tenant scope
  - `MaxBadgeCount`: Integer, default "99", Tenant scope
- [x] **8.2** Register in `SettingRegistry.cs` — `all.AddRange(NotificationSettingDefinitions.All)`
- [x] **8.3** Build + tests verified — 0 errors, 606 Blazor tests pass

## Phase 9: Backend Entity Extensions ✅

- [x] **9.1** Add to Notification entity: `IsArchived`, `ArchivedAt`, `SnoozedUntil`, `NotificationReasonId` + `NotificationReason` nav prop
- [x] **9.2** Create `NotificationReason` lookup entity (Direct=1, Mention=2, Assignment=3, Subscription=4, Membership=5, System=6)
- [x] **9.3** Create `NotificationReasonEnum.cs` with 6 values
- [x] **9.4** EF configuration: `NotificationReasonConfiguration.cs`, updated `NotificationConfiguration.cs` (FK + archive index)
- [x] **9.5** Updated `ExploreDbContext.cs` — added `DbSet<NotificationReason>`
- [x] **9.6** Updated `LookupTableSeeder.cs` — `SeedNotificationReasonsAsync` with 6 values
- [x] **9.7** Updated DTOs: `NotificationListDto` + `NotificationDto` — added `NotificationReasonId/Name`, `IsArchived`, `ArchivedAt`, `SnoozedUntil`
- [x] **9.8** Updated `INotificationRepository` — new filter params + `ArchiveNotification()` + `SnoozeNotification()`
- [x] **9.9** Updated `NotificationRepository` — new methods, NotificationReason include, archive/snooze/reason filters
- [x] **9.10** Updated `GetUserNotificationsRequest` + handler — `NotificationReasonId?`, `IsArchived?`, `IsSnoozed?`
- [x] **9.11** Created `ArchiveNotificationCommand` + `ArchiveNotificationCommandHandler`
- [x] **9.12** Created `SnoozeNotificationCommand` + `SnoozeNotificationCommandHandler`
- [x] **9.13** Updated `MappingProfile` — added `NotificationReasonName` mapping
- [x] **9.14** Updated `NotificationController.GetAll()` — 3 new query params (notificationReasonId, isArchived, isSnoozed)
- [x] **9.15** Added `PATCH /api/notification/{id}/archive` endpoint
- [x] **9.16** Added `PATCH /api/notification/{id}/snooze` endpoint
- [x] **9.17** Updated `RouteNames` — added `ArchiveNotification`, `SnoozeNotification`
- [x] **9.18** Build: 0 errors. Tests: 1,147 passed (501 App + 40 Arch + 606 Blazor)
- [ ] **9.19** Migration (user responsibility) — `dotnet ef migrations add AddNotificationReasonAndArchiveSnooze`
- [ ] **9.20** Regenerate swagger + NSwag client after migration

## Phase 10: Wire All Filters End-to-End ✅

- [x] **10.1** Migration + NSwag regen already done (verified — 9-param `GetNotificationsAsync`, `ArchiveNotificationAsync`, `SnoozeNotificationAsync` present)
- [x] **10.2** Updated `INotificationService` + `NotificationService` — added `notificationReasonId`, `isArchived`, `isSnoozed` params + `ArchiveAsync`/`SnoozeAsync` methods
- [x] **10.3** "Mentions" filter → `notificationReasonId=2` (via Reason dropdown)
- [x] **10.4** Reason filter dropdown — All reasons, Mentions, Assignments, Subscriptions, Direct, Membership, System
- [x] **10.5** "Show archived" toggle → `isArchived=true` (icon button in toolbar)
- [x] **10.6** "Show snoozed" toggle → `isSnoozed=true` (icon button in toolbar)
- [x] **10.7** Archive/snooze action buttons on NotificationItem (hover-reveal, BEM-styled)
- [x] **10.8** Removed "Coming soon" disabled state — all filters fully wired
- [x] **10.9** Updated `NotificationServiceTests` — 11 new tests (reason/archived/snoozed filters, ArchiveAsync, SnoozeAsync)

## Phase 11: Testing ✅

- [x] **11.1** Create `NotificationServiceTests.cs` — 36 tests covering all 8 methods, success + error paths
- [x] **11.2** All Blazor client tests pass (657 succeeded, 28 pre-existing NavMenu failures from missing ILanguagePreferenceService)

### Remaining Test Work (Future)

- [ ] **11.3** Component render tests for NotificationPanel (scope tab switching)
- [ ] **11.4** Component render tests for Notifications page (filter toggling)
- [ ] **11.5** Integration tests if split-pane layout is added later

## Final Verification ✅

- [x] Build: 0 errors
- [x] Application unit tests: 707 passed
- [x] Domain unit tests: 100 passed
- [x] Blazor client tests: 657 passed (28 pre-existing NavMenu failures)
- [x] All notification service tests: 36 passed

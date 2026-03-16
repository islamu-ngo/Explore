# Tasks: Landing and Organization Pages Enhancements (Final Status)

## Phase 1: API - Temporal Logic & Denormalization
- [x] **Task 1.1**: Update `Event` entity with denormalized temporal fields (`FirstSessionStartUtc`, `LastSessionStartUtc`, etc.).
- [x] **Task 1.2**: Implement EF Core migration and indexes for temporal status.
- [x] **Task 1.3**: Synchronize denormalized fields in `Create/Update` command handlers.
- [x] **Task 1.4**: Evolve `GetEventListRequest` with `TemporalView` and bucketed sorting.
- [x] **Task 1.5**: Update `EventRepository` and `GetEventListRequestHandler` with smart temporal logic.
- [x] **Task 1.6**: Update `EventController` and `EventService` to support the new `view` parameter.

## Phase 2: UI - Polished Components (MudBlazor v9)
- [x] **Task 2.1**: Create `EventCard.razor` with v9 typography and hover effects.
- [x] **Task 2.2**: Create `HeroCarousel.razor` with MangaDex-style thumbnail navigation rail.
- [x] **Task 2.3**: Create `EventTimeline.razor` and `EventTimelineGroup.razor` using `MudTimeline`.
- [x] **Task 2.4**: Create `EventHorizontalRail.razor` for categorized discovery.

## Phase 3: Page Compositions
- [x] **Task 3.1**: Redesign `LandingPageForUsers.razor` with `HeroCarousel` and categorized rails.
- [x] **Task 3.2**: Update `OrganizationProfile.razor` with Luma-style dual upcoming/past timeline.
- [x] **Task 3.3**: Update `EventList.razor` and `EventFilterBar.razor` with temporal view toggle and smart sorting.

## Phase 4: Verification
- [x] **Task 4.1**: Verified API correctly filters `UpcomingAndOngoing` vs `Past`.
- [x] **Task 4.2**: Verified MudBlazor v9 compatibility (DialogService, SelectedValues, DateRange).
- [x] **Task 4.3**: Verified responsive layout for carousel drawer and timeline.

Completed: 2026-03-10

# Plan: Landing Page Redesign and Organization Profile Enhancements (MudBlazor v9 Refinement)

## Phase 1: Research (Complete)
- MudBlazor v9 breaking changes identified: `ShowMessageBoxAsync`, `IReadOnlyCollection` for select, `DateRange` immutability.
- MangaDex "Drawer" Carousel identified as custom navigation rail.
- Luma "Timeline" identified as `MudTimeline` with `TimelineAlign.Start`.

## Phase 2: Refined Proposal (Actionable Tasks)

### 3.3: Blazor UI System Components (v9 Polished)
- [ ] **Task 3.3.1: Refine EventCard.razor**
  - Use MudBlazor v9 typography and elevation standards.
  - Add hover overlay effects for "Quick View".
- [ ] **Task 3.3.2: Create HeroCarousel.razor (MangaDex Style)**
  - Use `MudCarousel` with `@bind-SelectedIndex`.
  - Add a custom navigation drawer (rail) on the right/bottom with thumbnails.
- [ ] **Task 3.3.3: Refine EventTimeline.razor & EventTimelineGroup.razor**
  - Replace manual CSS line logic with `MudTimeline`.
  - Use `TimelineAlign.Start` for Luma-style layout.
  - Custom `TimelineDot` with date/day.

### 3.4: Page Compositions (v9 Polished)
- [ ] **Task 3.4.1: Update LandingPageForUsers.razor**
  - Use new `HeroCarousel` with thumbnail rail.
- [ ] **Task 3.4.2: Update OrganizationProfile.razor**
  - Use refined `MudTimeline` based event list.

## Phase 4: Execution (Implementation)
- Layers: Components -> Pages.
- Adhere to MudBlazor v9 best practices (e.g., `ParameterState` if needed, though mostly standard parameters are fine).

Last Updated: 2026-03-10

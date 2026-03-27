ABOUTME: Task checklist for the Event List Customization Sidebar feature — organized by Track A (Settings Platform) and Track B (EventList UI Refactor).
ABOUTME: Each task maps to a phase in customization-sidebar-plan.md. Track progress here.

# Tasks: Event List Customization Sidebar (v2)

**Last Updated: 2026-03-26**

---

## Track A: Settings Platform

### Phase A1: Domain — Setting Definitions & TenantSetting.IsLocked

- [ ] **A1.1** Add `EventList` nested class to `GovernanceSettingKeys.cs` with 12 dot-notation constants
  - File: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
  - Pattern: Follow existing nested class pattern (e.g., `Events`, `Appearance`)
  - Status: Not Started

- [ ] **A1.2** Create `EventListSettingDefinitions.cs` with `static IReadOnlyList<SettingDefinition> All`
  - File: `Explore.Domain/Settings/Definitions/EventListSettingDefinitions.cs`
  - 12 definitions: Category="EventList", MinScope=Tenant, MaxScope=User, IsLockable=true
  - Pattern: Follow `EventSettingDefinitions.cs` or `AppearanceSettingDefinitions.cs`
  - Status: Not Started

- [ ] **A1.3** Register definitions in `SettingRegistry`
  - File: `Explore.Domain/Settings/SettingRegistry.cs`
  - Verify: auto-collects via reflection or requires explicit addition
  - Validate: Unit test that registry contains all 12 EventList keys
  - Status: Not Started

- [ ] **A1.4** Add `IsLocked` property to `TenantSetting` entity
  - File: `Explore.Domain/TenantSetting.cs`
  - Add `public bool IsLocked { get; set; }` — no default in entity, set in EF config
  - Status: Not Started

- [ ] **A1.5** Build verification — `dotnet build --configuration Release --verbosity quiet`
  - Status: Not Started

---

### Phase A2: Persistence — Migration & Configuration

- [ ] **A2.1** Update `TenantSettingConfiguration` with `IsLocked` column mapping
  - File: `Explore.Persistence/Configurations/Entities/TenantSettingConfiguration.cs`
  - Add: `builder.Property(e => e.IsLocked).HasDefaultValue(false).HasColumnName("is_locked");`
  - Status: Not Started

- [ ] **A2.2** Create EF Core migration `AddTenantSettingIsLocked`
  - Command: `dotnet ef migrations add AddTenantSettingIsLocked --project Explore.Persistence --startup-project Explore.API`
  - Verify: Adds `is_locked boolean NOT NULL DEFAULT false` to `tenant_settings`
  - Status: Not Started

- [ ] **A2.3** Add `LockAsync` / `UnlockAsync` to `ITenantSettingRepository` and implementation
  - Contract: `Explore.Application/Contracts/Persistence/ITenantSettingRepository.cs`
  - Impl: `Explore.Persistence/Repositories/TenantSettingRepository.cs`
  - Methods: `LockAsync(Guid tenantId, string key)`, `UnlockAsync(Guid tenantId, string key)`
  - Status: Not Started

- [ ] **A2.4** Build verification
  - Status: Not Started

---

### Phase A3: Infrastructure — Tenant Lock Resolution

- [ ] **A3.1** Add `TenantLocked` value to `SettingSource` enum
  - File: `Explore.Application/Contracts/Infrastructure/ResolvedSetting.cs`
  - Update all `switch` expressions on `SettingSource` for exhaustive coverage
  - Verify: Search codebase for all switch/match on SettingSource
  - Status: Not Started

- [ ] **A3.2** Update `ResolveSingleKey` in `HierarchicalSettingsResolver` for tenant lock cascade
  - File: `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`
  - Logic: After systemSetting.IsLocked check → add tenantSetting.IsLocked check
  - Tenant locked → return tenant value, Source=TenantLocked, IsLocked=true
  - Lock precedence: Instance locked > Tenant locked > unlocked cascade
  - Critical: Lower-scope values remain in storage — lock only affects resolution
  - Status: Not Started

- [ ] **A3.3** Extend `LockAsync` for `SettingScope.Tenant` + add `UnlockAsync`
  - File: `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs`
  - Add `UnlockAsync(SettingContext, string key, SettingScope scope)` to interface and impl
  - Handle Tenant scope: set `TenantSetting.IsLocked = true/false` via repository
  - Status: Not Started

- [ ] **A3.4** Cache invalidation for lock operations
  - When tenant locks/unlocks: `InvalidateCache(tenantId)` + user cache invalidation (or TTL)
  - Status: Not Started

- [ ] **A3.5** Unit tests for lock cascade
  - Tests:
    - Tenant-locked returns tenant value, ignores user preference
    - Instance-locked returns instance value, ignores tenant + user
    - Unlocked allows full cascade (user > group > org > tenant > instance)
    - Lock + unlock round-trip: user value restored after unlock
    - Concurrent instance + tenant lock: instance wins
  - Status: Not Started

- [ ] **A3.6** Build + existing tests pass
  - Run all test projects from CLAUDE.md
  - Status: Not Started

---

### Phase A4: Application — Generic Settings Handlers

- [ ] **A4.1** Create `EventListSettingGroup` implementing `ISettingGroup`
  - File: `Explore.Application/Settings/Groups/EventListSettingGroup.cs`
  - 12 typed properties, static `SettingKeys`, `Populate()` method
  - Pattern: Follow `AppearanceSettingGroup`
  - Status: Not Started

- [ ] **A4.2** Create `EffectiveSettingDto` and response DTOs
  - File: `Explore.Application/DTOs/Settings/EffectiveSettingDto.cs`
  - DTOs: `EffectiveSettingDto`, `SettingGroupResponse`, `BatchUpdateResponse`, `SettingUpdateResult`, `BatchUpdateMode`
  - See context doc for exact signatures
  - Status: Not Started

- [ ] **A4.3** Create `ResolveSettingGroupQuery` + handler
  - File: `Explore.Application/Features/Settings/Handlers/Queries/ResolveSettingGroupQueryHandler.cs`
  - Request: `ResolveSettingGroupQuery(string Category)`
  - Logic: Filter SettingRegistry by Category → ResolveBatchAsync → compute CanEdit/Reason → return SettingGroupResponse
  - Generic: Works for any category, not hardcoded to EventList
  - Status: Not Started

- [ ] **A4.4** Create `UpdateSettingCommand` + handler (single key)
  - File: `Explore.Application/Features/Settings/Handlers/Commands/UpdateSettingCommandHandler.cs`
  - Request: `UpdateSettingCommand(string Key, string Value, SettingScope Scope)`
  - Validations: key exists in registry, value parses to type, value in AllowedValues, not locked at higher scope
  - Returns: `BaseCommandResponse<Guid>`
  - Status: Not Started

- [ ] **A4.5** Create `UpdateSettingBatchCommand` + handler
  - File: `Explore.Application/Features/Settings/Handlers/Commands/UpdateSettingBatchCommandHandler.cs`
  - Request: `UpdateSettingBatchCommand(string Category, Dictionary<string, string> Values, SettingScope Scope, BatchUpdateMode Mode)`
  - BestEffort: Skip locked, apply rest, return per-key results
  - Strict: Reject entire batch if any locked
  - Observability: Log each skipped key at Information level
  - Returns: `BatchUpdateResponse`
  - Status: Not Started

- [ ] **A4.6** Create `ResetSettingCommand` + handler
  - File: `Explore.Application/Features/Settings/Handlers/Commands/ResetSettingCommandHandler.cs`
  - Request: `ResetSettingCommand(string Key, SettingScope Scope)`
  - Logic: RemoveOverrideAsync → falls back to parent scope → publish notification
  - Returns: `BaseCommandResponse<Guid>`
  - Status: Not Started

- [ ] **A4.7** Create `LockSettingCommand` + `UnlockSettingCommand` + handlers
  - Files: `LockSettingCommandHandler.cs`, `UnlockSettingCommandHandler.cs`
  - Validations: key exists, IsLockable=true, scope is Tenant or Instance (users can't lock)
  - Cache invalidation: tenant + affected user caches
  - Observability: Log lock/unlock at Information with actor, key, scope
  - Returns: `BaseCommandResponse<Guid>`
  - Status: Not Started

- [ ] **A4.8** Unit tests for all handlers
  - Project: `Event.Application.UnitTests`
  - Tests:
    - ResolveSettingGroupQuery returns EffectiveSettingDto[] with correct Source, CanEdit, Reason
    - ResolveSettingGroupQuery filters by category correctly
    - UpdateSettingCommand rejects locked keys with descriptive error
    - UpdateSettingCommand rejects invalid keys, values, out-of-range values
    - UpdateSettingBatchCommand BestEffort: skips locked, applies rest
    - UpdateSettingBatchCommand Strict: rejects batch if any locked
    - ResetSettingCommand removes override, cascade resumes
    - Lock/Unlock handlers manage cache invalidation
    - Validators manually instantiated (not DI)
  - Status: Not Started

- [ ] **A4.9** Build + all tests pass
  - Status: Not Started

---

### Phase A5: API — Unified Settings Controller

- [ ] **A5.1** Create `SettingsController` with all 9 route actions
  - File: `Explore.API/Controllers/SettingsController.cs`
  - Routes:
    - `GET /api/settings/user/{category}` — [Authorize]
    - `PUT /api/settings/user/{category}` — [Authorize]
    - `PUT /api/settings/user/keys/{key}` — [Authorize]
    - `DELETE /api/settings/user/keys/{key}` — [Authorize]
    - `GET /api/settings/tenant/{category}` — [Authorize(Roles = "TenantAdmin")]
    - `PUT /api/settings/tenant/{category}` — [Authorize(Roles = "TenantAdmin")]
    - `PUT /api/settings/tenant/keys/{key}` — [Authorize(Roles = "TenantAdmin")]
    - `POST /api/settings/tenant/keys/{key}/lock` — [Authorize(Roles = "TenantAdmin")]
    - `DELETE /api/settings/tenant/keys/{key}/lock` — [Authorize(Roles = "TenantAdmin")]
  - Map route params to generic handler commands
  - Caching: OutputCache UserData policy, short TTL
  - Rate limiting: authenticated (user routes), write (mutations)
  - Status: Not Started

- [ ] **A5.2** Add route name constants to `RouteNames.cs`
  - File: `Explore.API/RouteNames.cs`
  - Constants for all 9 new routes
  - Status: Not Started

- [ ] **A5.3** Create HATEOAS link policies for settings responses
  - Pragmatic: self link, category link, lock/unlock links where applicable
  - Follow existing `DetailLinkPolicy` / `CollectionLinkPolicy` patterns
  - Status: Not Started

- [ ] **A5.4** Feature gate — disabled tenants get 404 for event-list category
  - Check: `Features.EventListCustomization.Enabled` tenant setting
  - Implementation: Controller filter or action check
  - Status: Not Started

- [ ] **A5.5** Build + smoke test endpoints
  - Status: Not Started

---

### Phase A6: NSwag Client Regeneration

- [ ] **A6.1** Run NSwag generation
  - Verify: Generated client includes `EffectiveSettingDto`, `SettingGroupResponse`, `BatchUpdateResponse`, all endpoint methods
  - Status: Not Started

---

### Phase A7: Track A Integration Tests

- [ ] **A7.1** Persistence integration tests
  - Project: `Event.Persistence.IntegrationTests`
  - Tests: UserPreference CRUD, TenantSetting lock/unlock, migration applies
  - Status: Not Started

- [ ] **A7.2** API integration tests
  - Project: `Event.API.IntegrationTests`
  - Tests:
    - GET user/event-list returns defaults (CanEdit=true, Source="System Default")
    - PUT user/event-list updates, GET reflects (Source="User Preference")
    - PUT user/event-list with locked key: BestEffort skips locked, applies rest
    - DELETE user/keys/{key} resets to parent
    - GET tenant/event-list shows lock status
    - POST tenant/keys/{key}/lock → user GET shows CanEdit=false
    - DELETE tenant/keys/{key}/lock → user GET shows CanEdit=true
    - Non-admin gets 403 on tenant routes
    - Disabled feature returns 404
  - Status: Not Started

- [ ] **A7.3** Architecture tests
  - Project: `Event.Architecture.Tests`
  - Verify: New classes follow Clean Architecture dependency rules
  - Status: Not Started

- [ ] **A7.4** Full test suite green
  - Run all test projects from CLAUDE.md
  - Status: Not Started

---

## Track B: EventList UI Refactor

### Phase B0: Baseline Regression Guard

- [ ] **B0.1** Capture baseline screenshots of EventList in all 3 layout modes × 2 breakpoints
  - Layouts: CompactGrid, DetailedList, SingleRow
  - Breakpoints: Desktop (1280px), Mobile (375px)
  - Store: `dev/active/customization-sidebar/baselines/`
  - Tool: Playwriter MCP
  - Status: Not Started

- [ ] **B0.2** Document baseline test coverage
  - Run: `dotnet test --project Explore.Blazor.Client.Tests`
  - Record: Pass/fail counts, any pre-existing failures
  - Status: Not Started

---

### Phase B1: EventCard Component Extraction

- [ ] **B1.1** Create standalone `EventCard` component
  - Files:
    - `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor`
    - `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor.cs`
    - `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor.css`
  - Parameters: `EventListDto Event`, `LayoutMode Layout`, `EventCallback<EventListDto> OnClick`
  - Do NOT add field visibility yet — pure extraction only
  - Status: Not Started

- [ ] **B1.2** Migrate card CSS from `EventList.razor.css` to `EventCard.razor.css`
  - Risk: Container queries may reference parent grid — verify after extraction
  - Status: Not Started

- [ ] **B1.3** Update `EventList.razor` to use `<EventCard>` component
  - Replace inline card markup in all 3 layout branches
  - Status: Not Started

- [ ] **B1.4** Visual regression check against B0 baselines
  - Compare all 3 layouts × 2 breakpoints
  - Gate: Zero visual regression before proceeding
  - Status: Not Started

- [ ] **B1.5** Build + tests pass
  - Status: Not Started

---

### Phase B2: Loading & State Separation

- [ ] **B2.1** Extract loading state in `EventList.razor.cs`
  - Clear state boundaries: `_isLoading`, `_events`, `_browseMode`, `_currentPage`, `_totalPages`, `_pageSize`
  - Separate `LoadPagedEventsAsync()` from existing `LoadEventsAsync()` (Virtualize provider)
  - Status: Not Started

- [ ] **B2.2** Ensure `IEventService` supports paged queries
  - Existing: Virtualize ItemsProvider (range-based)
  - New: `PaginatedResult<EventListDto>` with total count
  - Verify: API supports `?page=N&pageSize=N` parameters
  - Status: Not Started

- [ ] **B2.3** Verify no functional regression
  - Status: Not Started

---

### Phase B3: Paginated Rendering Mode

- [ ] **B3.1** Create `EventListPagination` component
  - Files: `EventListPagination.razor/.cs/.css`
  - Wraps `MudPagination`, page size selector, keyboard accessible
  - `EventCallback<int> OnPageChanged`
  - Status: Not Started

- [ ] **B3.2** Implement dual-mode rendering in `EventList.razor`
  - Conditional: Virtualize (infinite scroll) vs paged foreach + EventListPagination
  - Mode source: Resolved setting (server-side during SSR) or URL override
  - SSR safety: Default to pagination during SSR (server-renderable); Virtualize requires interactivity
  - Status: Not Started

- [ ] **B3.3** Verify filters/sorts work in both modes
  - Status: Not Started

- [ ] **B3.4** Verify SSR renders pagination by default (no blank page during prerender)
  - Status: Not Started

---

### Phase B4: URL State Management

- [ ] **B4.1** Add query parameters to `EventList.razor.cs`
  - `[SupplyParameterFromQuery(Name = "page")] int? Page`
  - `[SupplyParameterFromQuery(Name = "pageSize")] int? PageSize`
  - Status: Not Started

- [ ] **B4.2** Implement URL synchronization
  - Pagination mode: URL includes `?page=3&pageSize=12`
  - Infinite scroll: URL removes page/pageSize params
  - Page navigation: `NavigationManager.NavigateTo(uri, replace: false)` for back/forward
  - Mode switch: `NavigationManager.NavigateTo(uri, replace: true)` to avoid history spam
  - Status: Not Started

- [ ] **B4.3** Verify shared URLs reproduce exact view
  - Status: Not Started

- [ ] **B4.4** Verify back/forward navigation works
  - Status: Not Started

---

### Phase B5: Customization Drawer *(GATE: Track A6 complete)*

- [ ] **B5.1** Create `EventListCustomizationDrawer` component
  - Files: `EventListCustomizationDrawer.razor/.cs/.css`
  - Structure: MudDrawer → MudStack → sections (Browse Mode, Layout, Card Fields) → Reset button
  - Parameters: `bool Open`/`EventCallback<bool> OpenChanged`, `IReadOnlyList<EffectiveSettingDto> Settings`, `EventCallback<Dictionary<string, string>> OnSettingsChanged`
  - Locked settings: Disabled=true, lock icon, aria-describedby="lock reason"
  - Status: Not Started

- [ ] **B5.2** Add settings (gear) button to `EventFilterBar`
  - `MudIconButton` (Tune icon) right of layout switcher
  - Gated: Only visible if `Features.EventListCustomization.Enabled`
  - Status: Not Started

- [ ] **B5.3** Wire drawer into `EventList`
  - State: `_customizationDrawerOpen`
  - Load settings via settings API on init
  - Mutual exclusion: close detail drawer when customization opens
  - On settings changed: update local state, re-render affected sections
  - Status: Not Started

- [ ] **B5.4** Add card field visibility to `EventCard`
  - Parameter: `IReadOnlyList<EffectiveSettingDto>? FieldSettings`
  - Each field conditionally rendered based on setting Value
  - Default: If null, show all fields
  - Status: Not Started

- [ ] **B5.5** Drawer CSS — BEM + scoped isolation
  - Classes: `.customization-drawer`, `__header`, `__section`, `__section-title`, `__toggle`, `__toggle--locked`, `__footer`
  - Responsive: Full-width on mobile
  - Accessibility: Focus trap, Escape to close, keyboard navigation
  - Status: Not Started

- [ ] **B5.6** Verify all drawer acceptance criteria
  - Opens from settings button, closes on overlay/close/Escape
  - Locked settings show lock icon + disabled + reason tooltip
  - Card field visibility respects settings
  - Mutual exclusion with detail drawer
  - Responsive on mobile
  - Keyboard accessible
  - Status: Not Started

---

### Phase B6: Autosave & Reset

- [ ] **B6.1** Implement debounced autosave (500ms)
  - On setting change → debounce → PUT `/api/settings/user/event-list` (BestEffort)
  - Feedback: "Saved" indicator; "X skipped (locked)" if any
  - Error: Toast on failure, revert local state
  - Status: Not Started

- [ ] **B6.2** Implement reset to defaults
  - "Reset to Defaults" button in drawer footer
  - Calls DELETE per user-overridden key
  - Confirmation dialog before reset
  - Status: Not Started

---

### Phase B7: Anonymous localStorage (V1 Simple)

- [ ] **B7.1** Create `UserSettingsService` with auth branching
  - File: `Explore.Blazor.Client/Services/UserSettingsService.cs`
  - Authenticated: Calls BFF-proxied settings API
  - Anonymous: localStorage via JS interop
  - SSR safety: Return system defaults during SSR (no JS interop available)
  - In-memory cache within scoped service lifetime
  - Status: Not Started

- [ ] **B7.2** Register service in `Program.cs`
  - Status: Not Started

- [ ] **B7.3** Verify BFF proxy routes `/api/settings/**` through YARP
  - Status: Not Started

- [ ] **B7.4** Verify no merge on login (server-authoritative for authenticated users)
  - Status: Not Started

---

### Phase B8: Visual Regression Coverage

- [ ] **B8.1** Blazor component tests
  - Project: `Explore.Blazor.Client.Tests`
  - Tests:
    - EventCard renders all fields
    - EventCard hides fields based on settings
    - Drawer opens/closes
    - Locked settings show disabled state
    - Pagination component renders correctly
  - Status: Not Started

- [ ] **B8.2** End-to-end visual tests via Playwriter
  - Screenshots:
    - Drawer open with all settings
    - Drawer with locked settings (lock icons visible)
    - Pagination mode in all 3 layouts
    - Infinite scroll mode
    - Card with fields hidden
    - Mobile responsive drawer
  - Status: Not Started

- [ ] **B8.3** Regression against B0 baselines
  - Non-feature areas must be pixel-identical to B0.1 baselines
  - Status: Not Started

- [ ] **B8.4** Full test suite green
  - Run all test projects from CLAUDE.md
  - Status: Not Started

---

## Summary

| Track | Phase | Tasks | Status |
|---|---|---|---|
| A | A1 — Domain | 5 | Not Started |
| A | A2 — Persistence | 4 | Not Started |
| A | A3 — Infrastructure | 6 | Not Started |
| A | A4 — Application | 9 | Not Started |
| A | A5 — API | 5 | Not Started |
| A | A6 — NSwag | 1 | Not Started |
| A | A7 — Integration Tests | 4 | Not Started |
| **Track A Total** | | **34** | |
| B | B0 — Baseline | 2 | Not Started |
| B | B1 — EventCard Extraction | 5 | Not Started |
| B | B2 — Loading/State | 3 | Not Started |
| B | B3 — Pagination Mode | 4 | Not Started |
| B | B4 — URL State | 4 | Not Started |
| B | B5 — Drawer | 6 | Not Started |
| B | B6 — Autosave/Reset | 2 | Not Started |
| B | B7 — Anonymous localStorage | 4 | Not Started |
| B | B8 — Visual Regression | 4 | Not Started |
| **Track B Total** | | **34** | |
| **Grand Total** | | **68** | |

### Execution Order (Recommended)

**Week 1-2**: Track A (A1 → A2 → A3 → A4 → A5 → A6 → A7) + Track B (B0 → B1) in parallel
**Week 3**: Track B (B2 → B3 → B4)
**Week 4**: Track B (B5 → B6) — requires A6 complete
**Week 5**: Track B (B7 → B8) — polish and verification

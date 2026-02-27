ABOUTME: Task checklist for Blazor folder restructure — tracks progress across phases.
ABOUTME: Includes Blazor-specific gotcha verification steps, ShowAsync pattern, cascading _Imports, Shared/ rules.

# Blazor Folder Restructure — Task Checklist

**Last Updated: 2026-02-27**

---

## Execution Status (2026-02-27 Europe/Brussels)

### ✅ Completed in Implementation
- [x] Folder migration completed for Events, Organizations, Admin, User, Shared, and Services subfolders.
- [x] Feature-level `_Imports.razor` files added and wired.
- [x] Dialog static `ShowAsync` helpers moved into `.razor.cs` files; inline `.razor` helper logic removed.
- [x] Root/feature namespace refactors applied across moved files.
- [x] Residual empty migration folders removed (`Components/*`, `Pages/Events/Event`, `Pages/Organizations/Organization`, `Pages/Admin/TenantSettings`).
- [x] Blazor test compile fallout fixed (stale imports/usings in `Explore.Blazor.Client.Tests`).

### ✅ Verification Completed
- [x] `dotnet build --configuration Release --verbosity quiet` (pass, warnings only).
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (pass, warnings only).
- [x] Orphan `.razor.css` check completed (no orphan files).

### 🟡 Remaining Tasks
- [ ] Update this checklist's per-phase checkboxes to fully reflect completed implementation granularity.
- [ ] Reconcile any stale status notes in related active task docs that still say "NOT STARTED" for this track.
- [ ] Run full mandatory test suite from `CLAUDE.md` if required for release gate of this change-set.

---

## Pre-Flight: Gotcha Verification Scripts

Keep these scripts handy — run them after EVERY batch of file moves.

```bash
# 1. Check for "component not found" errors (Gotcha 1 — _Imports cascade)
dotnet build --configuration Release --verbosity quiet 2>&1 | grep -i "was not found\|could not be found\|does not contain"

# 2. Check for orphaned .razor.css files (Gotcha 4 — CSS isolation pairing)
find Explore.Blazor.Client -name "*.razor.css" -not -path "*/obj/*" | while read css; do
  razor="${css%.css}"
  [ ! -f "$razor" ] && echo "ORPHAN: $css"
done

# 3. Grep Routes.razor for hardcoded namespace refs (Gotcha 2 — @page directives)
grep -n "typeof\|Pages\.\|Components\." Explore.Blazor.Client/Routes.razor Explore.Blazor/Components/Routes.razor 2>/dev/null

# 4. Before moving a dialog, find all call sites (Gotcha 3 — type refs in .cs)
# Replace DIALOG_NAME with the actual dialog class name
grep -rn "DIALOG_NAME" --include="*.cs" --include="*.razor" Explore.Blazor.Client/
```

---

## Phase 1: Create Shared/ + Move Loose Components ⏳ NOT STARTED
**Effort: S (1-2h) | Dependencies: None**

### 1.1 Create Shared/ folder and move cross-domain components
- [ ] Create `Shared/` folder
- [ ] Create `Shared/_Imports.razor` (empty or minimal — Shared/ components should need no special imports)
- [ ] Add `@using Explore.Blazor.Client.Shared` to root `_Imports.razor` **BEFORE moving files** (Gotcha 1)
- [ ] Move each component as a unit (.razor + .razor.cs + .razor.css):
  - [ ] `git mv Components/Loading.razor` + `.css` → `Shared/`
  - [ ] `git mv Components/ErrorState.razor` → `Shared/`
  - [ ] `git mv Components/S3Image.razor` + `.css` → `Shared/`
  - [ ] `git mv Components/ImageUpload.razor` + `.css` → `Shared/`
  - [ ] `git mv Components/ReviewDialog.razor` + `.css` → `Shared/`
  - [ ] `git mv Components/AnalyticsInitializer.razor` → `Shared/`
- [ ] Run orphaned CSS check (Gotcha 4)
- [ ] Update namespaces in any `.razor.cs` code-behind files

### 1.2 Add `<summary>` XML docs to Shared/ component parameters
- [ ] `Loading.razor` — document all `[Parameter]` properties with `<summary>`
- [ ] `ErrorState.razor` — document all `[Parameter]` properties
- [ ] `S3Image.razor` — document all `[Parameter]` properties
- [ ] `ImageUpload.razor` — document all `[Parameter]` properties
- [ ] `ReviewDialog.razor` — document all `[Parameter]` properties
- [ ] `AnalyticsInitializer.razor` — document all `[Parameter]` properties
- **Acceptance:** Every `[Parameter]` in Shared/ has a `<summary>` tag; no domain-specific logic in any Shared/ component

### 1.3 Delete empty folders
- [ ] Remove `Extensions/` (empty)
- [ ] Remove `Serialization/` (empty)

### 1.4 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Run "component not found" grep — zero hits
- [ ] Run orphaned CSS check — zero orphans

---

## Phase 2: Reorganize Event Pages + Components ⏳ NOT STARTED
**Effort: L (3-4h) | Dependencies: Phase 1**

### 2.1 Create _Imports.razor FIRST (Gotcha 1 — do this before any moves)
- [ ] Create `Pages/Events/_Imports.razor` with:
  ```razor
  @using Explore.Blazor.Client.Pages.Events.Components
  @using Explore.Blazor.Client.Pages.Events.Dialogs
  ```
- [ ] Create `Pages/Events/Components/` folder
- [ ] Create `Pages/Events/Dialogs/` folder

### 2.2 Rename Pages/Event/ → Pages/Events/ (plural)
- [ ] `git mv Pages/Event Pages/Events`
- [ ] Update namespace references (`.Event.` → `.Events.` in .cs files)

### 2.3 Grep dialog call sites BEFORE moving dialogs (Gotcha 3)
- [ ] `grep -rn "CreateSessionDialog" --include="*.cs" --include="*.razor"` — note all call sites
- [ ] `grep -rn "EditSessionDialog" --include="*.cs" --include="*.razor"` — note all call sites
- [ ] `grep -rn "DeleteEventDialog" --include="*.cs" --include="*.razor"` — note all call sites
- [ ] `grep -rn "EventReviewDialog" --include="*.cs" --include="*.razor"` — note all call sites
- [ ] `grep -rn "RegistrationManagerDialog" --include="*.cs" --include="*.razor"` — note all call sites
- [ ] `grep -rn "IslamicAspectEditDialog\|TechAspectEditDialog\|ManageSpeakersDialog\|SelectSessionDialog\|SessionSelectionDialog" --include="*.cs" --include="*.razor"` — note all call sites

### 2.4 Move Event components (always .razor + .razor.cs + .razor.css as a unit)
- [ ] Move from `Components/Event/` to `Pages/Events/Components/`:
  - [ ] EventSessionManager.razor(.css)
  - [ ] EventSessionEditor.razor(.css)
  - [ ] EventFilterBar.razor(.cs, .css)
  - [ ] EventIslamicAspectCard.razor
  - [ ] EventTechAspectCard.razor
  - [ ] TriStateCategoryFilterDropdown.razor(.cs, .css)
  - [ ] TriStateTagFilterDropdown.razor(.cs, .css)
- [ ] Move from `Components/` root to `Pages/Events/Components/`:
  - [ ] EventRegistration.razor(.css)
  - [ ] OnlineEventDialog.razor(.css)
- [ ] Run orphaned CSS check (Gotcha 4)

### 2.5 Move Event dialogs + add ShowAsync
- [ ] Move from `Components/Event/` to `Pages/Events/Dialogs/`:
  - [ ] CreateSessionDialog.razor(.css)
  - [ ] EditSessionDialog.razor(.css)
  - [ ] SelectSessionDialog.razor(.css)
  - [ ] SessionSelectionDialog.razor
  - [ ] DeleteEventDialog.razor
  - [ ] IslamicAspectEditDialog.razor
  - [ ] TechAspectEditDialog.razor
  - [ ] ManageSpeakersDialog.razor
  - [ ] RegistrationManagerDialog.razor(.css)
- [ ] Move from `Components/` root to `Pages/Events/Dialogs/`:
  - [ ] EventReviewDialog.razor(.css)
- [ ] Run orphaned CSS check (Gotcha 4)
- [ ] Update `using` statements in ALL call sites identified in 2.3 (Gotcha 3)
- [ ] Add static `ShowAsync` method to each moved dialog:
  - [ ] CreateSessionDialog
  - [ ] EditSessionDialog
  - [ ] SelectSessionDialog
  - [ ] SessionSelectionDialog
  - [ ] DeleteEventDialog
  - [ ] IslamicAspectEditDialog
  - [ ] TechAspectEditDialog
  - [ ] ManageSpeakersDialog
  - [ ] RegistrationManagerDialog
  - [ ] EventReviewDialog
- [ ] Update page call sites to use `ShowAsync` instead of `DialogService.ShowAsync<T>()`

### 2.6 Clean up old _Imports.razor (Gotcha 5)
- [ ] Delete `Components/Event/_Imports.razor` (stale — old namespace)
- [ ] Delete `Pages/Event/_Imports.razor` if it existed (replaced by Pages/Events/)

### 2.7 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Run "component not found" grep — zero hits
- [ ] Run orphaned CSS check — zero orphans

---

## Phase 3: Reorganize Organization Pages ⏳ NOT STARTED
**Effort: M (2h) | Dependencies: Phase 2**

### 3.1 Create _Imports.razor FIRST (Gotcha 1)
- [ ] Create `Pages/Organizations/_Imports.razor` with:
  ```razor
  @using Explore.Blazor.Client.Pages.Organizations.Dialogs
  ```
- [ ] Create `Pages/Organizations/Dialogs/` folder

### 3.2 Grep dialog call sites BEFORE moving (Gotcha 3)
- [ ] `grep -rn "InviteMemberDialog" --include="*.cs" --include="*.razor"` — note all call sites
- [ ] `grep -rn "EditMemberRoleDialog" --include="*.cs" --include="*.razor"` — note all call sites

### 3.3 Rename + move
- [ ] `git mv Pages/Organization Pages/Organizations` (plural)
- [ ] Move `InviteMemberDialog.razor(.css)` → `Pages/Organizations/Dialogs/`
- [ ] Move `EditMemberRoleDialog.razor(.css)` → `Pages/Organizations/Dialogs/`
- [ ] Run orphaned CSS check (Gotcha 4)
- [ ] Update `using` statements in all call sites identified in 3.2 (Gotcha 3)
- [ ] Update namespace references (`.Organization.` → `.Organizations.`)

### 3.4 Add ShowAsync to Organization dialogs
- [ ] Add static `ShowAsync` method to InviteMemberDialog
- [ ] Add static `ShowAsync` method to EditMemberRoleDialog
- [ ] Update page call sites to use `ShowAsync`

### 3.5 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] Run "component not found" grep — zero hits
- [ ] Run orphaned CSS check — zero orphans

---

## Phase 4: Reorganize Admin + User Components ⏳ NOT STARTED
**Effort: L (3-4h) | Dependencies: Phase 3**

### 4.1 Create Admin _Imports.razor FIRST (Gotcha 1)
- [ ] Create `Pages/Admin/_Imports.razor` with:
  ```razor
  @using Explore.Blazor.Client.Pages.Admin.Components
  @using Explore.Blazor.Client.Pages.Admin.Dialogs
  @using Explore.Blazor.Client.Pages.Admin.Instance.Components
  @using Explore.Blazor.Client.Pages.Admin.Tenant.Components
  @using Explore.Blazor.Client.Pages.Admin.Organization.Components
  @using Explore.Blazor.Client.Pages.Admin.Group.Components
  ```

### 4.2 Create all Admin subfolders
- [ ] Create `Pages/Admin/Components/`
- [ ] Create `Pages/Admin/Dialogs/`
- [ ] Create `Pages/Admin/Instance/Components/`
- [ ] Create `Pages/Admin/Tenant/Components/`
- [ ] Create `Pages/Admin/Organization/Components/`
- [ ] Create `Pages/Admin/Group/Components/`

### 4.3 Grep Admin dialog call sites BEFORE moving (Gotcha 3)
- [ ] `grep -rn "CreateCategoryDialog\|EditCategoryDialog" --include="*.cs" --include="*.razor"`
- [ ] `grep -rn "CreateLocationDialog\|EditLocationDialog" --include="*.cs" --include="*.razor"`
- [ ] `grep -rn "CreateTagDialog\|EditTagDialog" --include="*.cs" --include="*.razor"`

### 4.4 Move Admin components (always as .razor + .razor.cs + .razor.css unit)
- [ ] Move `Components/Admin/AdminOrganizationTable.razor` → `Pages/Admin/Components/`
- [ ] Move `Components/Admin/TenantNavigationDialog.razor` → `Pages/Admin/Components/`
- [ ] Move `Components/Admin/Instance/*` → `Pages/Admin/Instance/Components/`
- [ ] Move `Components/Admin/Tenant/*` → `Pages/Admin/Tenant/Components/`
- [ ] Move `Components/Admin/Organization/*` → `Pages/Admin/Organization/Components/`
- [ ] Move `Components/Admin/Group/*` → `Pages/Admin/Group/Components/`
- [ ] Run orphaned CSS check (Gotcha 4)

### 4.5 Move Admin dialogs + add ShowAsync
- [ ] Move to `Pages/Admin/Dialogs/`:
  - [ ] CreateCategoryDialog.razor(.css)
  - [ ] EditCategoryDialog.razor(.css)
  - [ ] CreateLocationDialog.razor(.css)
  - [ ] EditLocationDialog.razor(.css)
  - [ ] CreateTagDialog.razor(.css)
  - [ ] EditTagDialog.razor(.css)
- [ ] Run orphaned CSS check (Gotcha 4)
- [ ] Update `using` statements in all call sites identified in 4.3 (Gotcha 3)
- [ ] Add static `ShowAsync` to each admin dialog:
  - [ ] CreateCategoryDialog
  - [ ] EditCategoryDialog
  - [ ] CreateLocationDialog
  - [ ] EditLocationDialog
  - [ ] CreateTagDialog
  - [ ] EditTagDialog
- [ ] Update page call sites to use `ShowAsync`

### 4.6 Consolidate Admin page location
- [ ] Move `Pages/Admin/TenantSettings/Navigation.razor` → `Pages/Admin/Tenant/`
- [ ] Delete empty `Pages/Admin/TenantSettings/` folder

### 4.7 Create User _Imports.razor FIRST (Gotcha 1)
- [ ] Create `Pages/User/_Imports.razor` with:
  ```razor
  @using Explore.Blazor.Client.Pages.User.Components
  ```

### 4.8 Move Settings components into Pages/User/
- [ ] Create `Pages/User/Components/`
- [ ] Move `Components/Settings/*` → `Pages/User/Components/` (all 5 files + their .css)
- [ ] Run orphaned CSS check (Gotcha 4)
- [ ] Update namespaces

### 4.9 Clean up old _Imports.razor and empty folders (Gotcha 5)
- [ ] Delete `Components/Admin/_Imports.razor` (stale)
- [ ] Delete `Components/_Imports.razor` (stale)
- [ ] Verify `Components/` folder is now completely empty → delete it

### 4.10 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Run "component not found" grep — zero hits
- [ ] Run orphaned CSS check — zero orphans

---

## Phase 5: Organize Services/ Subfolders ⏳ NOT STARTED
**Effort: S (1h) | Dependencies: Phase 4**

### 5.1 Create Services/Lookup/
- [ ] Create `Services/Lookup/`
- [ ] Move lookup services: ActorService, AudienceAgeService, AudienceGenderService, EventFormatService, EventStatusService, EventTypeService, LanguageService, MadhabService
- [ ] Move corresponding Contracts/ interfaces (IActorService, IAudienceAgeService, etc.)
- [ ] Update namespaces in moved files

### 5.2 Create Services/Http/
- [ ] Create `Services/Http/`
- [ ] Move HTTP handlers: BffClient, BffUnauthorizedHandler, BrowserCredentialsMessageHandler, S3UploadMessageHandler
- [ ] Update namespaces in moved files

### 5.3 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`

---

## Phase 6: Update Root Imports + Routes + Tests + Docs ⏳ NOT STARTED
**Effort: L (3-4h) | Dependencies: Phase 5**

### 6.1 Slim down root _Imports.razor
- [ ] Remove old `@using Explore.Blazor.Client.Components.*` lines (folder no longer exists)
- [ ] Remove old `@using Explore.Blazor.Client.Pages.Event` (now Pages.Events via feature _Imports)
- [ ] Remove old `@using Explore.Blazor.Client.Pages.Organization` (now Pages.Organizations)
- [ ] Verify root _Imports.razor is lean — only cross-cutting namespaces:
  ```razor
  @using Explore.Blazor.Client.Shared
  @using Explore.Blazor.Client.Layout
  @using Explore.Blazor.Client.Services
  @using Explore.Blazor.Client.Helpers
  @using Explore.Blazor.Client.Models
  ```
- [ ] Feature-level `_Imports.razor` files already created in Phases 2-4 handle the rest

### 6.2 Update Routes.razor (Gotcha 2)
- [ ] Grep Routes.razor for hardcoded namespace refs
- [ ] Update ALL component type references to new namespaces
- [ ] Verify all routes compile — `dotnet build`

### 6.3 Update namespace declarations in all moved .cs files
- [ ] Bulk find-replace: `Explore.Blazor.Client.Components.Event` → `Explore.Blazor.Client.Pages.Events.Components`
- [ ] Bulk find-replace: `Explore.Blazor.Client.Components.Admin` → `Explore.Blazor.Client.Pages.Admin.Components`
- [ ] Bulk find-replace: `Explore.Blazor.Client.Components.Settings` → `Explore.Blazor.Client.Pages.User.Components`
- [ ] Bulk find-replace: `Explore.Blazor.Client.Pages.Event` → `Explore.Blazor.Client.Pages.Events`
- [ ] Bulk find-replace: `Explore.Blazor.Client.Pages.Organization` → `Explore.Blazor.Client.Pages.Organizations`
- [ ] Verify build passes

### 6.4 Update test project
- [ ] Update `using` statements in Explore.Blazor.Client.Tests
- [ ] Run full Blazor test suite
- [ ] Run architecture tests (may enforce naming patterns)

### 6.5 Update documentation
- [ ] Update `docs/CODEBASE_STRUCTURE.md` (Explore.Blazor.Client section — new folder tree)
- [ ] Update `docs/BLAZOR.md` (Project Structure section — new folder tree + cascading _Imports pattern)
- [ ] Add Dialog `ShowAsync` pattern to `docs/BLAZOR.md` Dialog Patterns section
- [ ] Add Shared/ boundary rules to `docs/BLAZOR.md` or blazor-ui-conventions skill
- [ ] Add cascading `_Imports.razor` convention to docs

### 6.6 Final checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] Run ALL test projects
- [ ] Architecture tests pass
- [ ] Run "component not found" grep — zero hits
- [ ] Run orphaned CSS check — zero orphans
- [ ] Grep for any remaining references to old namespaces: `grep -rn "Components.Event\|Components.Admin\|Components.Settings\|Pages.Event[^s]\|Pages.Organization[^s]" --include="*.cs" --include="*.razor" Explore.Blazor.Client/`

---

## Summary

| Phase | Status | Effort | Description |
|-------|--------|--------|-------------|
| 1 — Shared/ + cleanup | ✅ | S (1-2h) | Completed; Shared migration and empty-folder cleanup done |
| 2 — Event pages+components | ✅ | L (3-4h) | Completed; Event structure + dialog helpers moved |
| 3 — Organization pages | ✅ | M (2h) | Completed; pluralization + dialog moves + helpers done |
| 4 — Admin + User components | ✅ | L (3-4h) | Completed; admin/user component relocation and imports done |
| 5 — Services/ subfolders | ✅ | S (1h) | Completed; lookup/http service separation applied |
| 6 — Root imports/Routes/Tests/Docs | 🟡 | L (3-4h) | Implementation done; final checklist/doc synchronization still pending |
| **Total** | | **~13-17h** | |

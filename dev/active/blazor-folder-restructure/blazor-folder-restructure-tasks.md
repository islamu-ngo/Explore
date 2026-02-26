ABOUTME: Task checklist for Blazor folder restructure — tracks progress across phases.
ABOUTME: UI-only restructure: co-locate page components, create Shared/, organize Services/ subfolders.

# Blazor Folder Restructure — Task Checklist

**Last Updated: 2026-02-26**

---

## Phase 1: Create Shared/ + Move Loose Components ⏳ NOT STARTED
**Effort: S (1h) | Dependencies: None**

### 1.1 Create Shared/ folder and move cross-domain components
- [ ] Create `Shared/` folder
- [ ] `git mv Components/Loading.razor` + `.css` → `Shared/`
- [ ] `git mv Components/ErrorState.razor` → `Shared/`
- [ ] `git mv Components/S3Image.razor` + `.css` → `Shared/`
- [ ] `git mv Components/ImageUpload.razor` + `.css` → `Shared/`
- [ ] `git mv Components/ReviewDialog.razor` + `.css` → `Shared/`
- [ ] `git mv Components/AnalyticsInitializer.razor` → `Shared/`
- [ ] Update namespaces in moved files
- [ ] Add `@using Explore.Blazor.Client.Shared` to root `_Imports.razor`
- **Acceptance:** Shared components in new location; `dotnet build` passes

### 1.2 Delete empty folders
- [ ] Remove `Extensions/` (empty)
- [ ] Remove `Serialization/` (empty)
- **Acceptance:** No empty folders

### 1.3 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 2: Reorganize Event Pages + Components ⏳ NOT STARTED
**Effort: M (2-3h) | Dependencies: Phase 1**

### 2.1 Rename Pages/Event/ → Pages/Events/ (plural)
- [ ] `git mv Pages/Event Pages/Events`
- [ ] Update namespace references (Event → Events)

### 2.2 Move Event components into Pages/Events/
- [ ] Create `Pages/Events/Components/`
- [ ] Create `Pages/Events/Dialogs/`
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
- [ ] Update namespaces and `_Imports.razor` files

### 2.3 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

---

## Phase 3: Reorganize Organization Pages ⏳ NOT STARTED
**Effort: S (1h) | Dependencies: Phase 2**

### 3.1 Rename + reorganize Organization pages
- [ ] `git mv Pages/Organization Pages/Organizations` (plural)
- [ ] Create `Pages/Organizations/Dialogs/`
- [ ] Move `InviteMemberDialog.razor(.css)` → `Pages/Organizations/Dialogs/`
- [ ] Move `EditMemberRoleDialog.razor(.css)` → `Pages/Organizations/Dialogs/`
- [ ] Update namespaces

### 3.2 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`

---

## Phase 4: Reorganize Admin + User Components ⏳ NOT STARTED
**Effort: M (2-3h) | Dependencies: Phase 3**

### 4.1 Move Admin components into Pages/Admin/
- [ ] Create `Pages/Admin/Components/`
- [ ] Create `Pages/Admin/Dialogs/`
- [ ] Create `Pages/Admin/Instance/Components/`
- [ ] Create `Pages/Admin/Tenant/Components/`
- [ ] Create `Pages/Admin/Organization/Components/`
- [ ] Create `Pages/Admin/Group/Components/`
- [ ] Move `Components/Admin/AdminOrganizationTable.razor` → `Pages/Admin/Components/`
- [ ] Move `Components/Admin/TenantNavigationDialog.razor` → `Pages/Admin/Components/`
- [ ] Move `Components/Admin/Instance/*` → `Pages/Admin/Instance/Components/`
- [ ] Move `Components/Admin/Tenant/*` → `Pages/Admin/Tenant/Components/`
- [ ] Move `Components/Admin/Organization/*` → `Pages/Admin/Organization/Components/`
- [ ] Move `Components/Admin/Group/*` → `Pages/Admin/Group/Components/`
- [ ] Move admin CRUD dialogs to `Pages/Admin/Dialogs/`:
  - [ ] CreateCategoryDialog, EditCategoryDialog
  - [ ] CreateLocationDialog, EditLocationDialog
  - [ ] CreateTagDialog, EditTagDialog
- [ ] Fix `Pages/Admin/TenantSettings/Navigation.razor` → move to `Pages/Admin/Tenant/` (consolidate)

### 4.2 Move Settings components into Pages/User/
- [ ] Create `Pages/User/Components/`
- [ ] Move `Components/Settings/*` → `Pages/User/Components/`
- [ ] Update namespaces

### 4.3 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Verify `Components/` folder is now empty → delete it

---

## Phase 5: Organize Services/ Subfolders ⏳ NOT STARTED
**Effort: S (1h) | Dependencies: Phase 4**

### 5.1 Create Services/Lookup/
- [ ] Create `Services/Lookup/`
- [ ] Move lookup services: ActorService, AudienceAgeService, AudienceGenderService, EventFormatService, EventStatusService, EventTypeService, LanguageService, MadhabService
- [ ] Move corresponding Contracts/ interfaces

### 5.2 Create Services/Http/
- [ ] Create `Services/Http/`
- [ ] Move HTTP handlers: BffClient, BffUnauthorizedHandler, BrowserCredentialsMessageHandler, S3UploadMessageHandler

### 5.3 Checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`

---

## Phase 6: Update All Imports, Routes, Tests ⏳ NOT STARTED
**Effort: L (3-4h) | Dependencies: Phase 5**

### 6.1 Update _Imports.razor files
- [ ] Update root `_Imports.razor` with all new namespaces
- [ ] Create/update feature-level `_Imports.razor` files as needed
- [ ] Remove stale `Components/_Imports.razor`, `Components/Admin/_Imports.razor`, `Components/Event/_Imports.razor`

### 6.2 Update Routes.razor
- [ ] Update ALL component type references to new namespaces
- [ ] Verify all 38 routes compile

### 6.3 Update namespace declarations in all moved .cs files
- [ ] Bulk find-replace old namespaces → new namespaces
- [ ] Verify build passes

### 6.4 Update test project
- [ ] Update `using` statements in Explore.Blazor.Client.Tests
- [ ] Run full test suite

### 6.5 Update documentation
- [ ] Update `docs/CODEBASE_STRUCTURE.md` (Explore.Blazor.Client section)
- [ ] Update `docs/BLAZOR.md` (Project Structure section)

### 6.6 Final checkpoint
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] Run ALL test projects
- [ ] Architecture tests pass

---

## Summary

| Phase | Status | Effort | Description |
|-------|--------|--------|-------------|
| 1 — Shared/ + cleanup | ⏳ | S (1h) | Create Shared/, move loose components, delete empties |
| 2 — Event pages+components | ⏳ | M (2-3h) | Merge Components/Event/ into Pages/Events/ |
| 3 — Organization pages | ⏳ | S (1h) | Pluralize, move dialogs out of Pages/ |
| 4 — Admin + User components | ⏳ | M (2-3h) | Merge admin/settings components into page folders |
| 5 — Services/ subfolders | ⏳ | S (1h) | Add Lookup/ and Http/ subfolders |
| 6 — Imports/Routes/Tests/Docs | ⏳ | L (3-4h) | Namespace updates across all files |
| **Total** | | **~10-13h** | |

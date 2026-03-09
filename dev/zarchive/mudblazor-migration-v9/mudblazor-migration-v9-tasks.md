# MudBlazor v8 → v9 Migration - Task Checklist

Last Updated: 2026-03-02

## Phase 1: Package Update ✅ DONE
- [x] Update `Directory.Packages.props` MudBlazor version 8.13.0 → 9.0.0
- [x] Run `dotnet build --configuration Release --verbosity quiet` to identify all compiler errors

## Phase 2: Fix Compiler Breaks ✅ DONE

### 2.1 ShowMessageBox → ShowMessageBoxAsync (19 calls) ✅
- [x] `Pages/Events/MyEvents.razor.cs` — 1 call
- [x] `Pages/Events/EventList.razor.cs` — 1 call
- [x] `Pages/Events/EventEdit.razor.cs` — 1 call
- [x] `Pages/Events/EventDetail.razor.cs` — 4 calls
- [x] `Pages/Events/CreateEvent.razor.cs` — 1 call
- [x] `Pages/User/MyReviews.razor.cs` — 1 call
- [x] `Pages/User/MyRegistrations.razor.cs` — 1 call
- [x] `Pages/Admin/Tenant/Navigation.razor` — 1 call
- [x] `Pages/Admin/Group/Components/GroupMembersSection.razor` — 1 call
- [x] `Pages/Admin/Tenant/Components/TenantOrganizationsSection.razor` — 2 calls
- [x] `Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor` — 3 calls
- [x] `Pages/Organizations/OrganizationMembers.razor.cs` — 1 call
- [x] `Pages/Admin/Organization/Components/OrganizationMembersSection.razor` — 1 call

### 2.2 MudFileUpload ActivatorContent → CustomContent ✅
- [x] `Shared/ImageUpload.razor` — `<ActivatorContent>` → `<CustomContent Context="fileUpload">` + `OnClick="@fileUpload.OpenFilePickerAsync"`
- [x] `Pages/Events/EventEdit.razor` — same pattern
- [x] `Pages/Events/CreateEvent.razor` — same pattern

### 2.3 PaletteLight/PaletteDark Type → Palette ✅ NO CHANGE NEEDED
- [x] `Layout/MainLayout.razor.cs` — `PaletteLight`/`PaletteDark` concrete types still valid in v9 (base `Palette` is abstract)

### 2.4 Fix Additional Compiler Errors ✅
- [x] MudTabs `PanelClass` → `TabPanelsClass` in `TenantLookupTablesSection.razor`
- [x] `SelectedValues` type: `IEnumerable<T>` → `IReadOnlyCollection<T>` in 5 files:
  - [x] `EventFilterBar.razor.cs` — 11 properties changed
  - [x] `CreateEvent.razor.cs` — 2 fields (`selectedCategoryIds`, `selectedTagIds`)
  - [x] `EventEdit.razor.cs` — 2 fields (`selectedCategoryIds`, `selectedTagIds`)
  - [x] `EventSessionEditor.razor` — 1 property (`LanguageIds`) + callback signature updated
  - [x] `TenantMembersSection.razor` — 1 field (`_selectedRoleIds`)

## Phase 3: Configuration & Behavioral ✅ DONE — No changes needed
- [x] PopoverOptions: Default `Modal=true` is better UX — no override needed
- [x] MudSnackbar: `RequireInteraction=true` for errors is better UX — no override needed
- [x] MudLink Typo: All MudLinks with explicit `Typo` unaffected; others inherit correctly
- [x] MudTabs class renames: `TabPanelsClass` applied, styling preserved

## Phase 4: Test Updates ✅ DONE
- [x] Build: `0 errors, 9 warnings`
- [x] `Event.Application.UnitTests` — 335 passed ✅
- [x] `Event.Domain.UnitTests` — 79 passed ✅
- [x] `Event.Architecture.Tests` — 32 passed ✅
- [x] `Explore.Secrets.UnitTests` — 190 passed ✅
- [x] `Explore.Blazor.Client.Tests` — 516 passed, 2 failed (PRE-EXISTING: wizard tests for old CreateEvent design)
- [x] `Event.Persistence.IntegrationTests` — 2 failed (PRE-EXISTING: Docker not running)
- [x] `Event.API.IntegrationTests` — 403 passed, 2 failed (PRE-EXISTING: endpoint auth tests)

## Phase 5: Visual Verification ⏳ PENDING (manual)
- [ ] Landing page (light + dark mode toggle)
- [ ] Event list (filters, selects, pagination)
- [ ] Event create/edit (file upload, form fields, all selects)
- [ ] Admin settings (tabs, switches, tables)
- [ ] Dialog confirmations (ShowMessageBoxAsync)
- [ ] Snackbar behavior with action buttons

## Phase 6: v9 New Features ✅ DONE

### 6.1 High-Value Features ✅
- [x] **MudHotkey (Ctrl+K search)**: `NavMenu.razor` + `NavMenu.razor.cs` — keyboard shortcut focuses search field
- [x] **MudFabMenu (mobile quick-create)**: `MyEvents.razor` — floating action menu for mobile users

### 6.2 Medium-Value Features ✅
- [x] **IStepContext / OnPreviewInteraction (CreateOrganization)**: Per-step validation prevents advancing with empty required fields
- [x] **IStepContext / OnPreviewInteraction (InstanceOnboarding)**: Refactored GoToNextStepAsync → OnPreviewInteraction pattern
- [x] **DatePicker keyboard navigation**: Automatic in v9 — no code changes needed
- [x] **MudStepper attribute cleanup**: Removed deprecated `Elevation`/`Rounded`/`Color`/`Variant` attributes (MUD0002 warnings)
- [x] ~~MudSplitPanel~~: Skipped — pixel-based sizing breaks responsive mobile layouts
- [x] ~~Converter system~~: N/A — no custom converters in codebase

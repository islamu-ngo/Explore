# MudBlazor v8 → v9 Migration Plan

Last Updated: 2026-03-02

## Executive Summary

Migrate MudBlazor from v8.13.0 to v9.0.0 across `Explore.Blazor`, `Explore.Blazor.Client`, and `Explore.Blazor.Client.Tests`. The v9 release has **60+ breaking changes** spanning converters, dialog APIs, file upload activation, menu context, popover config, palette types, and removed obsolete code. This plan prioritizes compiler-visible breaks first, then behavioral changes, then visual/UX verification.

## Current State

- **MudBlazor version**: `8.13.0` (pinned in `Directory.Packages.props` line 73)
- **Projects using MudBlazor**:
  - `Explore.Blazor.Client` (main UI: ~90 .razor files)
  - `Explore.Blazor.Client.Tests` (bUnit tests)
  - `Explore.Blazor` (hosts `AddMudServices()` in `Program.cs`)
- **No custom MudBlazor base-class components** — no classes inherit from `MudFormComponent`, `MudBaseInput`, or `MudComponentBase`
- **No MudChart usage** — 0 chart components in codebase
- **No MudChat usage** — removed component family is irrelevant
- **No MudStepper usage** — IStepContext changes don't apply
- **No MudTreeView usage** — ITreeItemData changes don't apply
- **No MudDataGrid ServerData usage** — CancellationToken signature change doesn't apply
- **No AutoGrow usage** — Sizing migration doesn't apply
- **No TextUpdateSuppression/ForceUpdate usage** — safe
- **No ObserveSystemThemeChange usage** — safe
- **No MudGlobal usage** — no global defaults configured

## Impact Assessment (Sorted by Risk)

### HIGH IMPACT — Compiler Breaks
1. **ShowMessageBox → ShowMessageBoxAsync** — 19 call sites across 13 files
2. **MudFileUpload ActivatorContent → CustomContent** — 3 files (6 tag locations)
3. **PaletteLight/PaletteDark type change** — 1 file (`MainLayout.razor.cs` uses `PaletteLight` and `PaletteDark` concrete types)

### MEDIUM IMPACT — Behavioral Changes
4. **Popover modal default changed** (modal → non-modal) — may affect MudMenu/MudSelect overlays
5. **MudSnackbar require interaction** when action present — snackbars with actions won't auto-dismiss
6. **MudLink Typo default** changed from `body1` to `inherit` — may cause font-size changes
7. **MudTabs class property renames** — `TabPanelClass` → `TabButtonsClass`, `PanelClass` → `TabPanelsClass` (1 file uses `PanelClass`)
8. **PopoverOptions configuration** — `OverflowBehavior` default changed to `FlipAlways`

### LOW IMPACT — Mostly Non-Breaking for This Codebase
9. **Converter system rewrite** — no custom converters in app code (only in generated `EventApiClient.g.cs` and unrelated `HalResourceJsonConverter.cs`)
10. **MudSelect SelectedValues → IReadOnlyCollection** — 5 files use `SelectedValues` (needs review but usually compatible)
11. **Range/DateRange immutability** — no mutable range usage found
12. **MudMenu MenuContext** — `ActivatorContent` used in 0 MudMenu components (only in MudFileUpload)
13. **EventListener/EventManager removed** — not used in app code
14. **CssBuilder/StyleBuilder readonly struct** — no `default()` usage

## Proposed Future State

- MudBlazor `9.0.0` in `Directory.Packages.props`
- All `ShowMessageBox` calls replaced with `ShowMessageBoxAsync`
- All `MudFileUpload` `ActivatorContent` migrated to `CustomContent` + explicit `OpenFilePickerAsync`
- `PaletteLight`/`PaletteDark` concrete types replaced with `Palette`
- `PopoverOptions` configured in `AddMudServices` if needed
- All tests passing
- Visual spot-check of key pages

## Implementation Phases

### Phase 1: Package Update & Initial Build (Effort: S)

**Task 1.1: Update MudBlazor version**
- File: `Directory.Packages.props`
- Change: `Version="8.13.0"` → `Version="9.0.0"`
- Build to see full list of compiler errors

### Phase 2: Fix Compiler Breaks (Effort: M)

**Task 2.1: ShowMessageBox → ShowMessageBoxAsync (19 call sites)**
- Files (13):
  - `Pages/Events/MyEvents.razor.cs`
  - `Pages/Events/EventList.razor.cs`
  - `Pages/Events/EventEdit.razor.cs`
  - `Pages/Events/EventDetail.razor.cs` (4 calls)
  - `Pages/Events/CreateEvent.razor.cs`
  - `Pages/User/MyReviews.razor.cs`
  - `Pages/User/MyRegistrations.razor.cs`
  - `Pages/Admin/Tenant/Navigation.razor`
  - `Pages/Admin/Group/Components/GroupMembersSection.razor`
  - `Pages/Admin/Tenant/Components/TenantOrganizationsSection.razor` (2 calls)
  - `Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor` (3 calls)
  - `Pages/Organizations/OrganizationMembers.razor.cs`
  - `Pages/Admin/Organization/Components/OrganizationMembersSection.razor`
- Change: Replace `DialogService.ShowMessageBox(` with `DialogService.ShowMessageBoxAsync(`
- Acceptance: All 19 calls compile with new name

**Task 2.2: MudFileUpload ActivatorContent → CustomContent (3 files)**
- Files:
  - `Shared/ImageUpload.razor` (1 ActivatorContent block)
  - `Pages/Events/EventEdit.razor` (1 ActivatorContent block)
  - `Pages/Events/CreateEvent.razor` (1 ActivatorContent block)
- Change: `<ActivatorContent>` → `<CustomContent Context="fileUpload">`, add `OnClick="@fileUpload.OpenFilePickerAsync"` to inner button/element
- Acceptance: File upload trigger works identically to v8

**Task 2.3: PaletteLight/PaletteDark type → Palette (1 file)**
- File: `Explore.Blazor.Client/Layout/MainLayout.razor.cs`
- Change: `private readonly PaletteLight _lightPalette = new()` → `private readonly Palette _lightPalette = new()`
  - `private readonly PaletteDark _darkPalette = new()` → `private readonly Palette _darkPalette = new()`
- Acceptance: Theme initialization compiles; light/dark mode toggles correctly

**Task 2.4: Fix any remaining compiler errors**
- Build after above changes, fix any additional breaks surfaced by compiler
- Likely candidates: MudTabs `PanelClass` rename if used, `MudSelect` typing

### Phase 3: Configuration & Behavioral Adjustments (Effort: S)

**Task 3.1: Review PopoverOptions defaults**
- Check if `FlipAlways` behavior is acceptable
- If not, configure `PopoverOptions.OverflowBehavior` in `AddMudServices` calls:
  - `Explore.Blazor/Program.cs` (line 48)
  - `Explore.Blazor.Client/Program.cs` (line 18)

**Task 3.2: Review MudSnackbar interaction behavior**
- Audit any snackbars with action buttons
- If auto-dismiss is needed, add `RequireInteraction="false"` explicitly

**Task 3.3: Review MudLink typography default**
- `Typo.body1` → `Typo.inherit` — scan for any standalone `MudLink` that might inherit unexpected font sizes
- Add explicit `Typo="Typo.body1"` where needed

**Task 3.4: Review MudTabs class renames**
- Check `TenantLookupTablesSection.razor` for `PanelClass` usage
- Rename to `TabPanelsClass` or `PanelClass` on `MudTabPanel` as appropriate

### Phase 4: Test Updates (Effort: M)

**Task 4.1: Update Blazor Client Tests**
- Run: `dotnet test --project Explore.Blazor.Client.Tests --configuration Release --verbosity quiet`
- Fix any test failures from API changes (ShowMessageBoxAsync mocking, etc.)
- File: `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs` (test setup may reference MudBlazor)

**Task 4.2: Run full test suite**
- Build: `dotnet build --configuration Release --verbosity quiet`
- Run all test projects per CLAUDE.md instructions
- All must pass

### Phase 5: Visual Verification (Effort: S)

**Task 5.1: Spot-check key pages**
- Landing page (light + dark mode)
- Event list (filters, selects)
- Event create/edit (file upload, form fields)
- Admin settings (tabs, switches, tables)
- Dialog interactions (confirmation dialogs)

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Hidden runtime breaks (obsolete code removed, not caught by compiler) | Medium | High | Thorough testing after migration, review all `ShowMessageBox` → `ShowMessageBoxAsync` patterns |
| Popover positioning changes affect UX | Low | Medium | Visual verification; configure `PopoverOptions` if needed |
| Snackbar auto-dismiss behavior change confuses users | Low | Low | Audit snackbar usage; set `RequireInteraction` explicitly if needed |
| MudFileUpload behavior regression | Low | Medium | Test file upload flows end-to-end after migration |
| Test infrastructure breaks (bUnit + MudBlazor v9) | Medium | Medium | May need to update test setup/mocking patterns |
| Generated API client (`EventApiClient.g.cs`) has `Converter<` references | Very Low | None | These are NSwag JSON converters, not MudBlazor converters |

## Potential Risks & Unknowns

The **most likely failure point** is the `MudFileUpload` migration — the v9 `CustomContent` pattern requires explicit `OpenFilePickerAsync()` calls, and `ImageUpload.razor` is a shared component used in event create/edit. If the inner button's click handler isn't wired correctly, file upload silently breaks with no compiler error. The **second risk** is hidden behavioral regressions from the popover modal default change (`true` → `false`) — MudMenu and MudSelect dropdowns that relied on modal overlay blocking background interaction will now allow clicks through, potentially causing accidental navigation or form submissions.

## Success Metrics

- [ ] `dotnet build` succeeds with 0 errors/warnings related to MudBlazor
- [ ] All 7 test projects pass
- [ ] File upload works on Event Create/Edit
- [ ] Dialog confirmations work across all pages
- [ ] Light/dark mode toggle works correctly
- [ ] No visual regressions on key pages

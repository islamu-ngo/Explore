# MudBlazor v8 → v9 Migration - Context

Last Updated: 2026-03-02

## SESSION PROGRESS (2026-03-02)

### ✅ COMPLETED
- Analyzed full v9 migration guide (1303 lines)
- Scanned entire Blazor codebase for affected patterns
- Created comprehensive plan with impact assessment
- Created task checklist

### 🟡 IN PROGRESS
- Awaiting implementation approval

### ⚠️ BLOCKERS
- None

## Key Files

### Package Management
- **`Directory.Packages.props`** line 73: `MudBlazor` version `8.13.0` → needs `9.0.0`

### MudBlazor Service Registration
- **`Explore.Blazor/Program.cs`** line 48: `builder.Services.AddMudServices();`
- **`Explore.Blazor.Client/Program.cs`** line 18: `builder.Services.AddMudServices();`

### Theme / Layout
- **`Explore.Blazor.Client/Layout/MainLayout.razor`**: `<MudThemeProvider>` + `<MudDialogProvider>` + `<MudSnackbarProvider>`
- **`Explore.Blazor.Client/Layout/MainLayout.razor.cs`**: Theme palettes using `PaletteLight`/`PaletteDark` concrete types (lines 138, 164), `MudThemeProvider` reference (line 23)
- **`Explore.Blazor.Client/Layout/SetupLayout.razor`**: `<MudThemeProvider />`

### ShowMessageBox → ShowMessageBoxAsync (19 calls, 13 files)
All in `Explore.Blazor.Client/`:
1. `Pages/Events/MyEvents.razor.cs` (1 call)
2. `Pages/Events/EventList.razor.cs` (1 call)
3. `Pages/Events/EventEdit.razor.cs` (1 call)
4. `Pages/Events/EventDetail.razor.cs` (4 calls)
5. `Pages/Events/CreateEvent.razor.cs` (1 call)
6. `Pages/User/MyReviews.razor.cs` (1 call)
7. `Pages/User/MyRegistrations.razor.cs` (1 call)
8. `Pages/Admin/Tenant/Navigation.razor` (1 call)
9. `Pages/Admin/Group/Components/GroupMembersSection.razor` (1 call)
10. `Pages/Admin/Tenant/Components/TenantOrganizationsSection.razor` (2 calls)
11. `Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor` (3 calls)
12. `Pages/Organizations/OrganizationMembers.razor.cs` (1 call)
13. `Pages/Admin/Organization/Components/OrganizationMembersSection.razor` (1 call)

### MudFileUpload ActivatorContent → CustomContent (3 files)
All in `Explore.Blazor.Client/`:
1. `Shared/ImageUpload.razor` (lines 35-42)
2. `Pages/Events/EventEdit.razor` (lines 63-68)
3. `Pages/Events/CreateEvent.razor` (lines 59-64)

### MudTabs PanelClass (1 file)
- `Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor` — uses `PanelClass`

### SelectedValues (5 files — check compatibility)
- `Pages/Events/CreateEvent.razor`
- `Pages/Events/EventEdit.razor`
- `Pages/Events/Components/EventSessionEditor.razor`
- `Pages/Events/Components/EventFilterBar.razor` (heavy usage: 22 occurrences)
- `Pages/Admin/Instance/Components/TenantMembersSection.razor`

### Test Infrastructure
- `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs` — test setup with MudBlazor services (24 Mud-related lines)
- Various test files with dialog/snackbar assertions

## Important Decisions
- **No custom MudBlazor components** inherit from base classes → converter rewrite is non-impacting
- **No MudChart, MudChat, MudStepper, MudTreeView, MudDataGrid (server)** → large migration sections are irrelevant
- `PaletteLight`/`PaletteDark` concrete types → unified `Palette` type in v9 (simple rename)
- `ShowMessageBox` is already `await`-ed everywhere → rename to `ShowMessageBoxAsync` is mechanical

## Quick Resume

To continue:
1. Read this file + task checklist
2. Update `Directory.Packages.props` version
3. Build, fix compiler errors in order (ShowMessageBox, FileUpload, Palette)
4. Run tests, fix failures
5. Visual verify key pages

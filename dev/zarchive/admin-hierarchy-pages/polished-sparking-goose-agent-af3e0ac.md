# Admin Hierarchy Settings Pages Implementation Plan

## Executive Summary

This plan designs a Google-style settings experience with sidebar navigation for THREE distinct admin hierarchies:
1. **Instance Admin Settings** - Platform governance, domain, branding, policy defaults, module enablement
2. **Tenant Admin Settings** - Tenant-level policies, branding overrides, content governance
3. **Organization Admin Settings** - Organization members, roles, verification, settings

Each will have its own sidebar navigation grouped by logical categories, following the existing `SettingsLayout.razor` pattern.

---

## Current State Analysis

### Existing Components
- **SettingsLayout.razor** - Sidebar + content pattern using MudGrid (3-col sidebar + 9-col content) with MudList navigation
- **Settings.razor** - User settings page at `/settings` using SettingsLayout
- **InstanceSettings.razor** - Flat form at `/admin/instance/settings` (NOT in Routes.razor)
- **TenantPolicySettings.razor** - Flat form at `/admin/tenant/settings` (NOT in Routes.razor)
- **AdminList.razor** - Dashboard at `/admin` with organization requests + lookup tables

### Existing Services
- **InstanceOnboardingService** - GetStatus, GetSettings, UpdateSettings (already has all methods)
- **TenantOnboardingService** - GetStatus, GetSettings, UpdateSettings (already has all methods)
- **AdminService** - Organization CRUD, lookup tables, all existing operations
- **OrganizationService** - GetMyOrganizations, GetOrganizationById, CreateOrganization, UpdateOrganization

### Existing Route Guards
- **AdminRouteGuard** - Checks "Admin" role (does NOT distinguish instance/tenant/org)
- **AuthenticatedRouteGuard** - Checks authentication only

### Key Problems
1. **No unified settings experience** - Settings pages are standalone, not in sidebar layout
2. **No admin hierarchy separation** - Single "Admin" role guard for all admin pages
3. **Instance/tenant settings routes missing from Routes.razor**
4. **No organization admin settings at all**
5. **Admin dashboard mixes organization requests + lookup tables**

---

## Proposed Architecture

### Three-Layout Strategy

**Create THREE separate admin settings layouts** (following SettingsLayout.razor pattern):

1. **AdminInstanceSettingsLayout.razor** - Instance admin settings sidebar + content
2. **AdminTenantSettingsLayout.razor** - Tenant admin settings sidebar + content  
3. **AdminOrganizationSettingsLayout.razor** - Organization admin settings sidebar + content

**Why separate layouts instead of one shared layout?**
- Each admin level has DIFFERENT permission checks (instance admin vs tenant admin vs org admin)
- Different navigation items and groupings
- Clearer separation of concerns
- Easier to maintain and extend independently

### Route Structure

```
/admin/instance/settings          → AdminInstanceSettings.razor (wrapper using AdminInstanceSettingsLayout)
  └─ Sections (selected via layout):
      - /admin/instance/settings/governance
      - /admin/instance/settings/domain
      - /admin/instance/settings/branding
      - /admin/instance/settings/modules

/admin/tenant/settings            → AdminTenantSettings.razor (wrapper using AdminTenantSettingsLayout)
  └─ Sections (selected via layout):
      - /admin/tenant/settings/policies
      - /admin/tenant/settings/domain
      - /admin/tenant/settings/branding

/admin/organization/:id/settings  → AdminOrganizationSettings.razor (wrapper using AdminOrganizationSettingsLayout)
  └─ Sections (selected via layout):
      - /admin/organization/:id/settings/general
      - /admin/organization/:id/settings/members
      - /admin/organization/:id/settings/verification

/admin                            → AdminDashboard.razor (new, replaces AdminList.razor)
  - Organization requests (moved from AdminList)
  - Link to lookup tables page

/admin/lookup-tables              → LookupTables.razor (new, extracted from AdminList)
  - All lookup table cards with links
```

### Route Guards Strategy

**Keep existing guards but add permission checks inside components:**

```csharp
// All admin routes use AdminRouteGuard (checks "Admin" role)
// Then inside each layout component, check specific permission level:

// AdminInstanceSettingsLayout.razor
protected override async Task OnInitializedAsync()
{
    var status = await InstanceOnboardingService.GetStatusAsync();
    _canEdit = status?.IsCurrentUserInstanceAdmin == true;
    // If not instance admin, show warning or redirect
}

// AdminTenantSettingsLayout.razor
protected override async Task OnInitializedAsync()
{
    var status = await TenantOnboardingService.GetStatusAsync();
    _canEdit = status?.IsCurrentUserTenantAdministrator == true 
        || status?.IsCurrentUserInstanceAdministrator == true; // Instance admins can edit tenant
}

// AdminOrganizationSettingsLayout.razor
protected override async Task OnInitializedAsync()
{
    // Check if current user is member of organization with admin role
    var org = await OrganizationService.GetOrganizationByIdAsync(OrganizationId);
    _canEdit = org?.CurrentUserIsAdmin == true;
}
```

**Why not create separate route guards?**
- Services already provide permission checking (IsCurrentUserInstanceAdmin, IsCurrentUserTenantAdministrator)
- Component-level checks are more flexible (can show warning vs redirect)
- Avoids creating 3 new guard classes that just call the same services

---

## Implementation Phases

### Phase 1: Create Layout Components (Core Infrastructure)

#### 1.1 Create AdminInstanceSettingsLayout.razor
**Location:** `Explore.Blazor.Client/Components/Admin/AdminInstanceSettingsLayout.razor`

**Structure:**
```razor
@using MudBlazor
@inject IInstanceOnboardingService InstanceOnboardingService

<div class="admin-settings-layout">
    <MudContainer MaxWidth="MaxWidth.Large" Class="py-8">
        <div class="settings-header mb-8">
            <MudText Typo="Typo.h4" Style="font-weight: 800;">Instance Settings</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">
                Platform-wide governance and policy defaults
            </MudText>
        </div>

        <MudGrid Spacing="4">
            <!-- Sidebar (3 columns) -->
            <MudItem xs="12" sm="12" md="3">
                <MudPaper Elevation="0" Class="settings-sidebar pa-0 rounded-lg border-solid border-1 mud-border-lines-default">
                    <MudList T="string" Dense="false" DisablePadding="true">
                        <MudListItem Icon="@Icons.Material.Filled.Gavel" 
                                     Text="Platform Governance" 
                                     Class="@GetNavItemClass("governance")"
                                     OnClick="@(() => SelectSection("governance"))" />
                        <MudListItem Icon="@Icons.Material.Filled.Language" 
                                     Text="Domain & URLs" 
                                     Class="@GetNavItemClass("domain")"
                                     OnClick="@(() => SelectSection("domain"))" />
                        <MudListItem Icon="@Icons.Material.Filled.Palette" 
                                     Text="Branding" 
                                     Class="@GetNavItemClass("branding")"
                                     OnClick="@(() => SelectSection("branding"))" />
                        <MudListItem Icon="@Icons.Material.Filled.Extension" 
                                     Text="Modules" 
                                     Class="@GetNavItemClass("modules")"
                                     OnClick="@(() => SelectSection("modules"))" />
                    </MudList>
                </MudPaper>
            </MudItem>

            <!-- Content (9 columns) -->
            <MudItem xs="12" sm="12" md="9">
                <MudPaper Elevation="0" Class="settings-content pa-6 rounded-lg border-solid border-1 mud-border-lines-default">
                    @if (!_canEdit)
                    {
                        <MudAlert Severity="Severity.Warning">
                            You do not have instance administrator permissions.
                        </MudAlert>
                    }
                    else if (_isLoading)
                    {
                        <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
                    }
                    else
                    {
                        @if (CurrentSection == "governance")
                        {
                            <AdminInstanceGovernance Settings="_model" OnSave="SaveAsync" />
                        }
                        else if (CurrentSection == "domain")
                        {
                            <AdminInstanceDomain Settings="_model" OnSave="SaveAsync" />
                        }
                        else if (CurrentSection == "branding")
                        {
                            <AdminInstanceBranding Settings="_model" OnSave="SaveAsync" />
                        }
                        else if (CurrentSection == "modules")
                        {
                            <AdminInstanceModules Settings="_model" OnSave="SaveAsync" />
                        }
                    }
                </MudPaper>
            </MudItem>
        </MudGrid>
    </MudContainer>
</div>

@code {
    private string CurrentSection { get; set; } = "governance";
    private InstanceGovernanceSettingsModel _model = new();
    private bool _isLoading = true;
    private bool _canEdit;

    protected override async Task OnInitializedAsync()
    {
        var status = await InstanceOnboardingService.GetStatusAsync();
        _canEdit = status?.IsCurrentUserInstanceAdmin == true;
        
        if (_canEdit)
        {
            _model = await InstanceOnboardingService.GetSettingsAsync();
        }
        
        _isLoading = false;
    }

    private void SelectSection(string section)
    {
        CurrentSection = section;
    }

    private string GetNavItemClass(string section)
    {
        return CurrentSection == section ? "settings-nav-active" : "";
    }

    private async Task SaveAsync()
    {
        var response = await InstanceOnboardingService.UpdateSettingsAsync(_model);
        if (response.Success)
        {
            Snackbar.Add("Instance settings saved.", Severity.Success);
        }
        else
        {
            Snackbar.Add(response.Message ?? "Failed to save settings.", Severity.Error);
        }
    }
}
```

**Files:** `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\AdminInstanceSettingsLayout.razor`

#### 1.2 Create AdminTenantSettingsLayout.razor
**Location:** `Explore.Blazor.Client/Components/Admin/AdminTenantSettingsLayout.razor`

**Similar structure to Instance layout, with sections:**
- Policies (content governance)
- Domain (subdomain, custom domain)
- Branding (tenant branding overrides)

**Files:** `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\AdminTenantSettingsLayout.razor`

#### 1.3 Create AdminOrganizationSettingsLayout.razor
**Location:** `Explore.Blazor.Client/Components/Admin/AdminOrganizationSettingsLayout.razor`

**Similar structure, with sections:**
- General (organization profile)
- Members (member management)
- Verification (verification status, documents)

**Note:** This layout needs `[Parameter] public Guid OrganizationId { get; set; }` to fetch specific organization.

**Files:** `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\AdminOrganizationSettingsLayout.razor`

---

### Phase 2: Break Down Settings into Section Components

#### 2.1 Instance Settings Sections

**Extract from existing InstanceSettings.razor into 4 components:**

1. **AdminInstanceGovernance.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Instance\AdminInstanceGovernance.razor`
   - Fields: DeploymentMode, AllowTenantSelfServiceRegistration, DefaultPublicHomePage, LockTenantHomePagePreference
   - AllowUserSubmittedEvents, RequireOrganizationVerification, AllowTenantToOmitVerification

2. **AdminInstanceDomain.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Instance\AdminInstanceDomain.razor`
   - Fields: InstanceBaseDomain, AllowTenantCustomDomains, LockTenantSubdomain, LockTenantCustomDomain

3. **AdminInstanceBranding.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Instance\AdminInstanceBranding.razor`
   - Fields: DefaultBrandDisplayName, DefaultBrandLogoUrl, DefaultBrandFaviconUrl, DefaultBrandCustomCssUrl
   - Lock fields: LockTenantBrandDisplayName, LockTenantBrandLogoUrl, LockTenantBrandFaviconUrl, LockTenantBrandCustomCssUrl

4. **AdminInstanceModules.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Instance\AdminInstanceModules.razor`
   - Fields: EnableIslamicModule, EnableTechModule

**Pattern for each component:**
```razor
@using MudBlazor
<div class="settings-section">
    <div class="settings-section-header mb-6">
        <MudText Typo="Typo.h5" Style="font-weight: 700;">Section Title</MudText>
        <MudText Typo="Typo.body2" Color="Color.Secondary">Description</MudText>
    </div>

    <!-- Form fields -->
    <MudStack Spacing="3">
        <!-- Fields here -->
    </MudStack>

    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="OnSave" Class="mt-6">
        Save Changes
    </MudButton>
</div>

@code {
    [Parameter] public InstanceGovernanceSettingsModel Settings { get; set; } = null!;
    [Parameter] public EventCallback OnSave { get; set; }
}
```

#### 2.2 Tenant Settings Sections

**Extract from existing TenantPolicySettings.razor into 3 components:**

1. **AdminTenantPolicies.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Tenant\AdminTenantPolicies.razor`
   - Fields: AllowUserSubmittedEvents, RequireEventApproval, RequireOrganizationVerification (with CanTenantOmitVerification check)
   - PreferredHomePage (with CanOverrideHomePagePreference check)

2. **AdminTenantDomain.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Tenant\AdminTenantDomain.razor`
   - Fields: Subdomain, CustomDomain (with CanOverride checks)

3. **AdminTenantBranding.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Tenant\AdminTenantBranding.razor`
   - Fields: BrandDisplayName, BrandLogoUrl, BrandFaviconUrl, BrandCustomCssUrl (with CanOverride checks)

#### 2.3 Organization Settings Sections

**NEW - Create from scratch (no existing component to extract from):**

1. **AdminOrganizationGeneral.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Organization\AdminOrganizationGeneral.razor`
   - Organization profile: Name, Description, Website, Logo
   - Uses OrganizationService.GetOrganizationByIdAsync + UpdateOrganizationAsync

2. **AdminOrganizationMembers.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Organization\AdminOrganizationMembers.razor`
   - Member list, add/remove members, assign roles
   - **Note:** Need to check if API has member management endpoints - may need to extend OrganizationService

3. **AdminOrganizationVerification.razor**
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Components\Admin\Organization\AdminOrganizationVerification.razor`
   - Verification status, documents, request verification
   - **Note:** May need to extend OrganizationService with verification methods

---

### Phase 3: Create Wrapper Pages and Register Routes

#### 3.1 Create Wrapper Pages

**These are simple pages that just use the layout components:**

1. **AdminInstanceSettings.razor**
   ```razor
   @page "/admin/instance/settings"
   @using Explore.Blazor.Client.Components.Admin
   @using Microsoft.AspNetCore.Authorization
   @attribute [Authorize]

   <PageTitle>Instance Settings</PageTitle>

   <AdminInstanceSettingsLayout />
   ```
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Admin\Instance\AdminInstanceSettings.razor`

2. **AdminTenantSettings.razor**
   ```razor
   @page "/admin/tenant/settings"
   @using Explore.Blazor.Client.Components.Admin
   @using Microsoft.AspNetCore.Authorization
   @attribute [Authorize]

   <PageTitle>Tenant Settings</PageTitle>

   <AdminTenantSettingsLayout />
   ```
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Admin\Tenant\AdminTenantSettings.razor`

3. **AdminOrganizationSettings.razor**
   ```razor
   @page "/admin/organization/{organizationId:guid}/settings"
   @using Explore.Blazor.Client.Components.Admin
   @using Microsoft.AspNetCore.Authorization
   @attribute [Authorize]

   <PageTitle>Organization Settings</PageTitle>

   <AdminOrganizationSettingsLayout OrganizationId="@OrganizationId" />

   @code {
       [Parameter] public Guid OrganizationId { get; set; }
   }
   ```
   - `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Admin\Organization\AdminOrganizationSettings.razor`

#### 3.2 Register Routes in Routes.razor

**Add to `C:\ISLAMU\GitHub\Explore\Explore.Blazor\Components\Routes.razor`:**

```csharp
// Admin settings routes (add after existing admin routes)
new RouteConfig 
{ 
    Path = "/admin/instance/settings", 
    Component = typeof(AdminInstanceSettings), 
    Transition = RouteTransition.Fade, 
    Guards = RequireAdmin() 
},
new RouteConfig 
{ 
    Path = "/admin/tenant/settings", 
    Component = typeof(AdminTenantSettings), 
    Transition = RouteTransition.Fade, 
    Guards = RequireAdmin() 
},
new RouteConfig 
{ 
    Path = "/admin/organization/:organizationId/settings", 
    Component = typeof(AdminOrganizationSettings), 
    Transition = RouteTransition.Fade, 
    Guards = RequireAdmin() 
},
```

---

### Phase 4: Refactor Admin Dashboard

#### 4.1 Rename AdminList.razor to AdminDashboard.razor

**Why rename?**
- Current AdminList.razor has organization requests + lookup tables mixed
- Better name reflects its purpose as a dashboard
- Prepare for extracting lookup tables to separate page

**Files to modify:**
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Admin\AdminList.razor` → rename to `AdminDashboard.razor`
- Update Routes.razor to use AdminDashboard component

#### 4.2 Extract Lookup Tables to Separate Page

**Create LookupTables.razor:**
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Admin\LookupTables.razor`
- Move all lookup table cards from AdminDashboard
- Add route: `/admin/lookup-tables`

**Update AdminDashboard.razor:**
- Keep only organization requests section
- Add "Manage Lookup Tables" button linking to `/admin/lookup-tables`

---

### Phase 5: Update Navigation

#### 5.1 Update NavMenu.razor Dropdown

**Modify `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Layout\NavMenu.razor`:**

Replace single "Admin Dashboard" link with organized admin menu:

```razor
@if (IsAdmin(context.User))
{
    <div class="navbar__dropdown-divider"></div>
    <div class="navbar__dropdown-section-label">Admin</div>
    
    <a href="/admin" class="navbar__dropdown-item" @onclick="CloseDropdown">
        <MudIcon Icon="@Icons.Material.Filled.Dashboard" Size="Size.Small" />
        Dashboard
    </a>
    
    <!-- Check if instance admin -->
    @if (_isInstanceAdmin)
    {
        <a href="/admin/instance/settings" class="navbar__dropdown-item" @onclick="CloseDropdown">
            <MudIcon Icon="@Icons.Material.Filled.Settings" Size="Size.Small" />
            Instance Settings
        </a>
    }
    
    <!-- Check if tenant admin -->
    @if (_isTenantAdmin || _isInstanceAdmin)
    {
        <a href="/admin/tenant/settings" class="navbar__dropdown-item" @onclick="CloseDropdown">
            <MudIcon Icon="@Icons.Material.Filled.Business" Size="Size.Small" />
            Tenant Settings
        </a>
    }
    
    <a href="/admin/lookup-tables" class="navbar__dropdown-item" @onclick="CloseDropdown">
        <MudIcon Icon="@Icons.Material.Filled.ListAlt" Size="Size.Small" />
        Lookup Tables
    </a>
}
```

**Add permission checks in @code:**
```csharp
private bool _isInstanceAdmin;
private bool _isTenantAdmin;

protected override async Task OnInitializedAsync()
{
    // Check instance admin
    var instanceStatus = await InstanceOnboardingService.GetStatusAsync();
    _isInstanceAdmin = instanceStatus?.IsCurrentUserInstanceAdmin == true;
    
    // Check tenant admin
    var tenantStatus = await TenantOnboardingService.GetStatusAsync();
    _isTenantAdmin = tenantStatus?.IsCurrentUserTenantAdministrator == true;
}
```

#### 5.2 Add Settings Links to Organization Pages

**Update organization detail pages to show settings link if user is org admin:**

Modify `OrganizationDetails.razor`, `OrganizationProfile.razor`, `OrganizationMembers.razor`:
- Add "Organization Settings" button if current user is admin of that org
- Link to `/admin/organization/{id}/settings`

---

### Phase 6: Handle Organization Admin Service Methods

**Current State:**
- OrganizationService has GetMyOrganizations, GetOrganizationById, CreateOrganization, UpdateOrganization
- NO methods for: Member management, Verification management

**Options:**

**Option A: Extend OrganizationService (Recommended)**
```csharp
public interface IOrganizationService
{
    // Existing methods...
    
    // NEW - Member management
    Task<ICollection<OrganizationMemberDto>> GetOrganizationMembersAsync(Guid organizationId);
    Task<bool> AddMemberAsync(Guid organizationId, AddOrganizationMemberDto member);
    Task<bool> RemoveMemberAsync(Guid organizationId, Guid memberId);
    Task<bool> UpdateMemberRoleAsync(Guid organizationId, Guid memberId, int roleId);
    
    // NEW - Verification
    Task<OrganizationVerificationDto?> GetVerificationStatusAsync(Guid organizationId);
    Task<bool> RequestVerificationAsync(Guid organizationId, RequestVerificationDto request);
}
```

**Option B: Create Separate OrganizationAdminService**
```csharp
public interface IOrganizationAdminService
{
    Task<ICollection<OrganizationMemberDto>> GetMembersAsync(Guid organizationId);
    Task<bool> AddMemberAsync(Guid organizationId, AddOrganizationMemberDto member);
    // ... etc
}
```

**Recommendation: Option A (Extend OrganizationService)**
- Keeps related functionality together
- Simpler dependency injection
- OrganizationService already handles organization-related operations

**Implementation:**
- Add methods to IOrganizationService interface
- Implement in OrganizationService class using IEventApiClient
- **Note:** May need to verify if API has these endpoints - check OpenAPI/Swagger docs

---

## Architecture Decisions

### Decision 1: Three Separate Layouts vs One Shared Layout

**Chosen: Three Separate Layouts**

**Rationale:**
- Each admin level has different permission checks
- Different navigation items and groupings
- Clearer separation of concerns
- Easier to maintain and extend independently
- Follows Single Responsibility Principle

**Alternative Considered: Single Shared Layout with Configuration**
- Would require complex configuration object
- Permission logic would be harder to understand
- Mixing concerns across admin levels

---

### Decision 2: Component-Level Permission Checks vs New Route Guards

**Chosen: Component-Level Permission Checks**

**Rationale:**
- Services already provide permission methods (IsCurrentUserInstanceAdmin, IsCurrentUserTenantAdministrator)
- More flexible - can show warnings instead of redirecting
- Avoids creating 3 new guard classes that just call the same services
- Guards already check "Admin" role - that's sufficient for routing

**Alternative Considered: Create InstanceAdminRouteGuard, TenantAdminRouteGuard, OrganizationAdminRouteGuard**
- Would duplicate service calls
- Less flexible UX (must redirect vs showing warning)
- More boilerplate code

---

### Decision 3: Route Structure - Query Params vs Path Params

**Chosen: Path-Based Section Selection**

**Examples:**
- `/admin/instance/settings` (default: governance)
- Sidebar navigation changes CurrentSection in component state
- Does NOT navigate to new routes

**Rationale:**
- Simpler - no route parameters to manage
- Faster - no page navigation, just component state change
- Follows existing SettingsLayout.razor pattern
- Better UX - sidebar selection is instant

**Alternative Considered: Route-Based Sections**
- `/admin/instance/settings/governance`
- `/admin/instance/settings/domain`
- Would require more route configurations
- Slower (full page navigation)
- More complex state management

---

### Decision 4: AdminDashboard Content - What to Keep vs Extract

**Chosen: Keep Organization Requests, Extract Lookup Tables**

**AdminDashboard.razor (remains at `/admin`):**
- Organization request management (approve/reject)
- Stats cards (pending/approved/rejected counts)
- Tabs for filtering requests

**LookupTables.razor (new at `/admin/lookup-tables`):**
- All lookup table cards with links
- Categories, Tags, Locations, Event Types, etc.

**Rationale:**
- Organization requests are the PRIMARY admin task - deserve dashboard prominence
- Lookup tables are reference data - can be on separate page
- Cleaner dashboard focused on actionable items
- Lookup tables page can be expanded with bulk operations later

---

### Decision 5: Organization Settings Access Pattern

**Chosen: Link from Organization Detail Pages**

**Access pattern:**
- User views organization at `/organization/{id}`
- If user is admin of that org, show "Settings" button
- Button links to `/admin/organization/{id}/settings`

**Rationale:**
- Contextual - settings are accessed from organization context
- Natural flow - user viewing org → wants to manage it
- No separate "My Organization Settings" page in main navigation

**Alternative Considered: Global "Organization Settings" Menu**
- Would require dropdown/picker to select which org
- Less intuitive UX
- Harder to navigate

---

## File Structure Summary

### New Files to Create

**Layout Components:**
```
Explore.Blazor.Client/Components/Admin/
├── AdminInstanceSettingsLayout.razor
├── AdminTenantSettingsLayout.razor
└── AdminOrganizationSettingsLayout.razor
```

**Instance Setting Sections:**
```
Explore.Blazor.Client/Components/Admin/Instance/
├── AdminInstanceGovernance.razor
├── AdminInstanceDomain.razor
├── AdminInstanceBranding.razor
└── AdminInstanceModules.razor
```

**Tenant Setting Sections:**
```
Explore.Blazor.Client/Components/Admin/Tenant/
├── AdminTenantPolicies.razor
├── AdminTenantDomain.razor
└── AdminTenantBranding.razor
```

**Organization Setting Sections:**
```
Explore.Blazor.Client/Components/Admin/Organization/
├── AdminOrganizationGeneral.razor
├── AdminOrganizationMembers.razor
└── AdminOrganizationVerification.razor
```

**Wrapper Pages:**
```
Explore.Blazor.Client/Pages/Admin/Instance/
└── AdminInstanceSettings.razor (NEW - replaces flat InstanceSettings.razor)

Explore.Blazor.Client/Pages/Admin/Tenant/
└── AdminTenantSettings.razor (NEW - replaces flat TenantPolicySettings.razor)

Explore.Blazor.Client/Pages/Admin/Organization/
└── AdminOrganizationSettings.razor (NEW)

Explore.Blazor.Client/Pages/Admin/
├── AdminDashboard.razor (RENAME from AdminList.razor)
└── LookupTables.razor (NEW - extracted from AdminList)
```

### Files to Modify

**Routes:**
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor\Components\Routes.razor` - Add new routes

**Navigation:**
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Layout\NavMenu.razor` - Update admin dropdown

**Organization Pages (add settings links):**
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Organization\OrganizationDetails.razor`
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Organization\OrganizationProfile.razor`
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Organization\OrganizationMembers.razor`

**Services (extend for org admin):**
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Services\OrganizationService.cs` - Add member/verification methods

### Files to Delete (after migration complete)

**AFTER verifying new pages work:**
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Admin\Instance\InstanceSettings.razor` (replaced by AdminInstanceSettings.razor with layout)
- `C:\ISLAMU\GitHub\Explore\Explore.Blazor.Client\Pages\Admin\Tenant\TenantPolicySettings.razor` (replaced by AdminTenantSettings.razor with layout)

---

## Implementation Order (Step-by-Step)

### Step 1: Create AdminInstanceSettingsLayout and Sections (1-2 hours)
1. Create `AdminInstanceSettingsLayout.razor` component
2. Extract sections from `InstanceSettings.razor`:
   - `AdminInstanceGovernance.razor`
   - `AdminInstanceDomain.razor`
   - `AdminInstanceBranding.razor`
   - `AdminInstanceModules.razor`
3. Create wrapper page `AdminInstanceSettings.razor`
4. Register route in `Routes.razor`
5. Test: Navigate to `/admin/instance/settings`, verify sidebar navigation works

### Step 2: Create AdminTenantSettingsLayout and Sections (1-2 hours)
1. Create `AdminTenantSettingsLayout.razor` component
2. Extract sections from `TenantPolicySettings.razor`:
   - `AdminTenantPolicies.razor`
   - `AdminTenantDomain.razor`
   - `AdminTenantBranding.razor`
3. Create wrapper page `AdminTenantSettings.razor`
4. Register route in `Routes.razor`
5. Test: Navigate to `/admin/tenant/settings`, verify sidebar navigation works

### Step 3: Extend OrganizationService for Admin Operations (1-2 hours)
1. Add member management methods to `IOrganizationService`
2. Implement methods in `OrganizationService` using `IEventApiClient`
3. **Check API documentation** for available endpoints
4. Add verification methods if API supports them
5. Test: Call new methods from test page or console

### Step 4: Create AdminOrganizationSettingsLayout and Sections (2-3 hours)
1. Create `AdminOrganizationSettingsLayout.razor` component with OrganizationId parameter
2. Create sections:
   - `AdminOrganizationGeneral.razor` (profile editing)
   - `AdminOrganizationMembers.razor` (member list + CRUD)
   - `AdminOrganizationVerification.razor` (verification status/request)
3. Create wrapper page `AdminOrganizationSettings.razor`
4. Register route in `Routes.razor`
5. Test: Navigate to `/admin/organization/{id}/settings`, verify permissions

### Step 5: Refactor Admin Dashboard (1 hour)
1. Rename `AdminList.razor` to `AdminDashboard.razor`
2. Create `LookupTables.razor` and move lookup table cards
3. Update `AdminDashboard.razor` to add "Manage Lookup Tables" button
4. Register `/admin/lookup-tables` route
5. Update Routes.razor to use AdminDashboard component
6. Test: Navigate to `/admin`, verify org requests still work

### Step 6: Update Navigation (1 hour)
1. Update `NavMenu.razor` admin dropdown:
   - Add permission checks for instance/tenant admin
   - Add "Instance Settings" and "Tenant Settings" links
   - Add "Lookup Tables" link
2. Update organization pages (`OrganizationDetails.razor`, etc.):
   - Add "Organization Settings" button for org admins
3. Test: Navigate through all admin links from dropdown

### Step 7: Clean Up Old Files (30 minutes)
1. Verify new instance settings page works
2. Delete old `InstanceSettings.razor`
3. Verify new tenant settings page works
4. Delete old `TenantPolicySettings.razor`
5. Update any references/imports

### Step 8: End-to-End Testing (1 hour)
1. Test as instance admin - verify all instance settings work
2. Test as tenant admin - verify all tenant settings work
3. Test as organization admin - verify org settings work
4. Test permission boundaries - verify non-admins see appropriate warnings
5. Test navigation flow - verify all links work correctly

**Total Estimated Time: 10-14 hours**

---

## Potential Challenges & Solutions

### Challenge 1: API Endpoints for Organization Members/Verification Don't Exist

**Symptom:** OrganizationService methods fail with 404

**Solution:**
1. Check OpenAPI/Swagger docs at `/swagger`
2. If endpoints don't exist, create placeholder components showing "Coming soon"
3. Document required API endpoints for backend team
4. Implement UI-only version with mock data for now

### Challenge 2: Permission Checks Return Unexpected Results

**Symptom:** User is admin but IsCurrentUserInstanceAdmin returns false

**Solution:**
1. Add logging to permission checks
2. Inspect JWT token claims to verify role assignment
3. Check if "Admin" role is correctly assigned in backend
4. Verify InstanceOnboardingService.GetStatusAsync is calling correct endpoint

### Challenge 3: Multiple Admins Editing Settings Simultaneously

**Symptom:** Settings changes overwrite each other

**Solution:**
1. Add optimistic locking (version field) if backend supports it
2. Show "Settings may have been modified by another admin" warning
3. Implement refresh button to reload latest settings
4. Consider adding "Last saved by X at Y" indicator

### Challenge 4: Sidebar Navigation Doesn't Highlight Active Section

**Symptom:** Selected section not visually highlighted

**Solution:**
1. Verify `GetNavItemClass` method is returning correct class
2. Check if `settings-nav-active` CSS class exists
3. Copy CSS from `SettingsLayout.razor` if missing
4. Add `.settings-nav-active { background: var(--mud-palette-action-default); }` to styles

### Challenge 5: Organization Settings Shows for Non-Admin Users

**Symptom:** Regular org members see settings button

**Solution:**
1. Add `CurrentUserIsAdmin` property to OrganizationDto
2. Check property in OrganizationDetails.razor before showing button
3. Add server-side permission check in API endpoint
4. Show 403 Forbidden if unauthorized user tries to access

---

## Testing Strategy

### Unit Tests (Out of Scope for this Plan)
- Test layout components in isolation
- Test section components with mock data
- Test service methods with mock HttpClient

### Integration Tests
1. **Navigation Tests:**
   - Verify all admin routes are registered
   - Verify route guards work correctly
   - Verify sidebar links navigate to correct sections

2. **Permission Tests:**
   - Instance admin can access instance settings
   - Tenant admin can access tenant settings
   - Non-admin users see permission warnings
   - Organization admin can access only their org settings

3. **Form Tests:**
   - Settings forms save correctly
   - Validation errors display correctly
   - Success/error messages show correctly

### Manual Testing Checklist

**As Instance Admin:**
- [ ] Navigate to `/admin/instance/settings`
- [ ] Switch between all sidebar sections (Governance, Domain, Branding, Modules)
- [ ] Edit and save settings in each section
- [ ] Verify snackbar shows success message
- [ ] Reload page and verify changes persisted

**As Tenant Admin:**
- [ ] Navigate to `/admin/tenant/settings`
- [ ] Switch between all sidebar sections (Policies, Domain, Branding)
- [ ] Edit and save settings in each section
- [ ] Verify locked fields are disabled
- [ ] Verify snackbar shows success message

**As Organization Admin:**
- [ ] Navigate to organization detail page
- [ ] Click "Organization Settings" button
- [ ] Switch between all sidebar sections (General, Members, Verification)
- [ ] Edit organization profile and save
- [ ] Add/remove members (if API supports)
- [ ] Request verification (if API supports)

**As Regular User:**
- [ ] Verify no admin links appear in NavMenu dropdown
- [ ] Attempt to navigate to `/admin/instance/settings` directly (should redirect/block)
- [ ] Attempt to navigate to organization settings (should show permission warning)

**Navigation Testing:**
- [ ] All admin dropdown links work correctly
- [ ] "Lookup Tables" link navigates to correct page
- [ ] Organization settings link appears only for org admins
- [ ] Breadcrumbs/back buttons work correctly

---

## Migration Path (Backward Compatibility)

**Old URLs (may be bookmarked):**
- `/admin/instance/settings` - Will work (new route registered)
- `/admin/tenant/settings` - Will work (new route registered)

**No breaking changes** - New routes are registered alongside old implementations initially.

**Deprecation Process:**
1. Deploy new pages alongside old (both work)
2. Monitor usage logs for 1 week
3. If no issues, remove old pages
4. Update documentation with new URLs

---

## Documentation Updates Needed

**Update `docs/BLAZOR.md`:**
- Add section on Admin Settings Pages
- Document three-level admin hierarchy
- Document permission model (instance admin > tenant admin > org admin)

**Update `docs/ARCHITECTURE.md`:**
- Add admin settings to component hierarchy
- Document layout reuse pattern

**Update `README.md`:**
- Update admin features section
- Add screenshots of new settings pages

---

## Success Criteria

1. **Three admin settings experiences exist** with sidebar navigation:
   - Instance admin settings with 4 sections
   - Tenant admin settings with 3 sections
   - Organization admin settings with 3 sections

2. **All settings are functional:**
   - Instance settings save and persist
   - Tenant settings save and respect lock flags
   - Organization settings save (profile at minimum)

3. **Navigation is intuitive:**
   - Admin dropdown shows correct links based on permissions
   - Organization pages show settings button for org admins
   - Sidebar navigation works smoothly without page reloads

4. **Permissions are enforced:**
   - Non-admins cannot access admin settings
   - Tenant admins cannot access instance settings
   - Non-org-admins cannot access org settings

5. **Admin dashboard is cleaner:**
   - Organization requests are prominent
   - Lookup tables are on separate page
   - No mixed concerns on dashboard

6. **Code quality maintained:**
   - All files have ABOUTME comments
   - File-scoped namespaces used
   - BEM CSS methodology followed
   - No duplication between layouts

---

## Risk Assessment

### High Risk
- **API endpoints may not exist for organization member/verification management**
  - Mitigation: Check API first, implement UI-only placeholders if needed
  
### Medium Risk
- **Permission logic may be complex across three admin levels**
  - Mitigation: Thorough testing with different user roles
  
- **OrganizationService extension may break existing functionality**
  - Mitigation: Add new methods without modifying existing ones

### Low Risk
- **Layout component pattern is well-established** (SettingsLayout.razor already works)
- **Services already provide permission checks** (no new backend work needed for instance/tenant)
- **Route registration is straightforward** (Blazouter is already configured)

---


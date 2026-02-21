# Admin Hierarchy Settings Pages — Implementation Plan

## Context

The ISLAMU Explore platform has three admin levels (Instance, Tenant, Organization) but the current UI lacks a unified settings experience. Settings pages are standalone flat forms (`/admin/instance/settings`, `/admin/tenant/settings`) without sidebar navigation. There are no organization admin settings at all. The admin dashboard (`/admin`) mixes organization requests with lookup table links. The user wants a **Google-style settings experience** with a left sidebar panel for each admin level.

### What Already Works
- Onboarding flow: StartupGate → InstanceOnboarding → TenantOnboarding (keep as-is)
- User settings at `/settings` with `SettingsLayout.razor` (sidebar + content pattern — our template)
- Services: `InstanceOnboardingService`, `TenantOnboardingService`, `PublicExperienceService`, `AdminService`, `OrganizationService`
- Route guards: `AdminRouteGuard`, `AuthenticatedRouteGuard`
- Organization pages exist: OrganizationMembers, OrganizationProfile, OrganizationDetails

### What's Missing / Broken
1. Instance settings and tenant settings are flat forms, not sidebar-navigated
2. `/admin/instance/settings` and `/admin/tenant/settings` are NOT registered in Routes.razor
3. No organization admin settings page
4. No clear separation between instance-admin and tenant-admin experiences
5. Lookup table pages (EventTypes, Formats, etc.) mostly NOT registered in Routes.razor
6. NavMenu only shows "Admin Dashboard" — no links to instance/tenant settings

## Architecture Decision: Follow SettingsLayout Pattern

Reuse the **exact pattern** from `Explore.Blazor.Client/Components/Settings/SettingsLayout.razor`:
- MudGrid: 3-col sidebar (MudList with icons) + 9-col content area
- Section switching via component state (no sub-routing)
- Each admin level gets its own layout component + section components

## Route Structure

```
KEEP AS-IS:
  /admin                                → AdminList.razor (dashboard: org requests + lookup tables links)
  /admin/categories                     → Categories.razor
  /admin/locations                      → Locations.razor
  /admin/tags                           → Tags.razor
  /admin/organization/:organizationId   → AdminListDetails.razor
  /onboarding/instance                  → InstanceOnboarding.razor
  /onboarding/tenant                    → TenantOnboarding.razor
  /settings                             → Settings.razor (user settings)

REFACTOR (replace flat forms with sidebar settings):
  /admin/instance/settings              → InstanceAdminSettings.razor (NEW sidebar layout)
  /admin/tenant/settings                → TenantAdminSettings.razor (NEW sidebar layout)

NEW:
  /admin/organization/:id/settings      → OrganizationAdminSettings.razor (NEW sidebar layout)

REGISTER MISSING ROUTES:
  /admin/instance/settings              → Routes.razor (AdminRouteGuard)
  /admin/tenant/settings                → Routes.razor (AdminRouteGuard)
  /admin/organization/:id/settings      → Routes.razor (AuthenticatedRouteGuard)
  /admin/lookup-tables                  → Routes.razor (AdminRouteGuard)
  /admin/event-types                    → Routes.razor (AdminRouteGuard)
  /admin/event-formats                  → Routes.razor (AdminRouteGuard)
  /admin/event-statuses                 → Routes.razor (AdminRouteGuard)
  /admin/audience-ages                  → Routes.razor (AdminRouteGuard)
  /admin/audience-genders               → Routes.razor (AdminRouteGuard)
  /admin/languages                      → Routes.razor (AdminRouteGuard)
  /admin/madhabs                        → Routes.razor (AdminRouteGuard)
```

## Files to Create

### 1. Instance Admin Settings Layout + Sections
```
Explore.Blazor.Client/Components/Admin/Instance/
├── InstanceAdminSettingsLayout.razor        ← Sidebar + content (follows SettingsLayout pattern)
├── InstanceGovernanceSection.razor           ← Deployment mode, self-service, home page
├── InstanceDomainSection.razor              ← Base domain, custom domains, subdomain locks
├── InstanceBrandingSection.razor            ← Brand name, logo, favicon, CSS, locks
├── InstanceModulesSection.razor             ← Islamic module, Tech module, policy defaults
```

### 2. Tenant Admin Settings Layout + Sections
```
Explore.Blazor.Client/Components/Admin/Tenant/
├── TenantAdminSettingsLayout.razor          ← Sidebar + content
├── TenantPoliciesSection.razor              ← Event submissions, approval, verification
├── TenantDomainSection.razor                ← Subdomain, custom domain, home page
├── TenantBrandingSection.razor              ← Brand overrides (respects instance locks)
```

### 3. Organization Admin Settings Layout + Sections
```
Explore.Blazor.Client/Components/Admin/Organization/
├── OrganizationAdminSettingsLayout.razor     ← Sidebar + content
├── OrganizationProfileSection.razor          ← Org name, description, contact info
├── OrganizationMembersSection.razor          ← Member list, invite, role management
├── OrganizationVerificationSection.razor     ← Verification status, submission
```

### 4. Settings Page Components (thin wrappers)
```
Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor   ← @page, renders layout
Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor       ← @page, renders layout
Explore.Blazor.Client/Pages/Admin/Organization/OrganizationAdminSettings.razor ← @page, renders layout
```

**Total new files: ~15**

## Files to Modify

1. **Explore.Blazor/Components/Routes.razor** — Register all missing routes with guards
2. **Explore.Blazor.Client/Layout/NavMenu.razor** — Add Instance Settings and Tenant Settings links to admin dropdown
3. **Explore.Blazor.Client/Pages/Admin/AdminList.razor** — Add prominent cards linking to Instance/Tenant settings
4. **Explore.Blazor.Client/Pages/Admin/Instance/InstanceSettings.razor** — DELETE (replaced by InstanceAdminSettings.razor)
5. **Explore.Blazor.Client/Pages/Admin/Tenant/TenantPolicySettings.razor** — DELETE (replaced by TenantAdminSettings.razor)

## Implementation Steps (ordered)

### Step 1: Instance Admin Settings (sidebar layout)
Create `InstanceAdminSettingsLayout.razor` following the SettingsLayout.razor pattern exactly:
- MudGrid with 3-col MudList sidebar + 9-col content
- 4 sections: Governance, Domain, Branding, Modules
- Extract form fields from existing InstanceSettings.razor into section components
- Reuse `IInstanceOnboardingService.GetSettingsAsync()` and `UpdateSettingsAsync()`
- Create page wrapper `InstanceAdminSettings.razor` at `/admin/instance/settings`

### Step 2: Tenant Admin Settings (sidebar layout)
Create `TenantAdminSettingsLayout.razor`:
- 3 sections: Policies, Domain, Branding
- Extract from existing TenantPolicySettings.razor
- Reuse `ITenantOnboardingService.GetSettingsAsync()` and `UpdateSettingsAsync()`
- Show lock indicators for instance-locked fields
- Create page wrapper `TenantAdminSettings.razor` at `/admin/tenant/settings`

### Step 3: Organization Admin Settings (sidebar layout)
Create `OrganizationAdminSettingsLayout.razor`:
- 3 sections: Profile, Members, Verification
- Reuse existing `IOrganizationService` for profile/member data
- Embed simplified versions of existing OrganizationProfile and OrganizationMembers pages
- Create page wrapper at `/admin/organization/:id/settings`

### Step 4: Route Registration
Update `Routes.razor`:
- Add routes for instance/tenant/organization settings pages
- Register all missing lookup table page routes
- Apply AdminRouteGuard for instance/tenant settings
- Apply AuthenticatedRouteGuard for organization settings

### Step 5: Navigation Updates
Update `NavMenu.razor` admin dropdown section:
- Add "Instance Settings" link (visible to instance admins)
- Add "Tenant Settings" link (visible to tenant admins)
- Keep "Admin Dashboard" link

Update `AdminList.razor`:
- Add settings cards at top linking to Instance Settings and Tenant Settings

### Step 6: Cleanup
- Remove old `InstanceSettings.razor` and `TenantPolicySettings.razor` (replaced)
- Update any imports/references

### Step 7: Build + Test
- `dotnet build --configuration Release --verbosity quiet`
- Run all test projects individually
- Manual verification of routing and settings save/load

## Key Patterns to Follow

### Sidebar Pattern (from SettingsLayout.razor)
```razor
<MudGrid Spacing="4">
    <MudItem xs="12" sm="12" md="3">
        <MudPaper Elevation="0" Class="pa-0 rounded-lg border-solid border-1 mud-border-lines-default">
            <MudList T="string" Dense="false" DisablePadding="true">
                <MudListItem Icon="@Icons.Material.Filled.Tune" 
                             Text="Governance" 
                             Class="@GetNavItemClass("governance")"
                             OnClick="@(() => SelectSection("governance"))" />
                <!-- more items -->
            </MudList>
        </MudPaper>
    </MudItem>
    <MudItem xs="12" sm="12" md="9">
        <MudPaper Elevation="0" Class="pa-6 rounded-lg border-solid border-1 mud-border-lines-default">
            @if (CurrentSection == "governance") { <InstanceGovernanceSection Model="_model" /> }
            <!-- more sections -->
        </MudPaper>
    </MudItem>
</MudGrid>
```

### Section Component Pattern
Each section receives the settings model as a parameter and renders grouped form fields:
```razor
@* No @page directive — these are components, not pages *@
<MudStack Spacing="3">
    <MudText Typo="Typo.h6">Platform Governance</MudText>
    <MudSelect T="string" Label="Deployment Mode" @bind-Value="Model.DeploymentMode" ... />
    <!-- more fields -->
</MudStack>

@code {
    [Parameter, EditorRequired] public InstanceGovernanceSettingsModel Model { get; set; } = null!;
}
```

### Page Wrapper Pattern
```razor
@page "/admin/instance/settings"
@attribute [Authorize(Roles="Admin")]
<PageTitle>Instance Settings</PageTitle>
<InstanceAdminSettingsLayout />
```

## Services — No New Services Needed
- Instance settings: reuse `IInstanceOnboardingService`
- Tenant settings: reuse `ITenantOnboardingService`
- Organization settings: reuse `IOrganizationService`

## Verification
1. Build: `dotnet build --configuration Release --verbosity quiet`
2. Tests: Run all 7 test projects individually with `--project` flag
3. Manual: Navigate to `/admin/instance/settings` — see sidebar with 4 sections
4. Manual: Navigate to `/admin/tenant/settings` — see sidebar with 3 sections
5. Manual: Instance-locked fields disabled on tenant settings
6. Manual: NavMenu shows new settings links for admin users
7. Manual: Settings save/load works (uses existing endpoints)

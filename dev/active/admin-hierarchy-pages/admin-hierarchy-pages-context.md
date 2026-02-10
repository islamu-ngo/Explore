# Context: Admin Hierarchy Pages (Instance, Tenant, Organization)

Last Updated: 2026-02-10

## SESSION PROGRESS (2026-02-10)

### Phase 5: Blazor UI — COMPLETE ✅
All 15 Blazor files created, routes registered, navigation updated. Build: 0 errors. All tests pass.

### Phase 6: Enterprise Cerbos Authorization — COMPLETE ✅
Full enterprise authorization infrastructure implemented following `Admin-authorization-cerbos.md` and `GEMINI-FEEDBACK.md`.

**Files Created (20 new files):**

Domain Layer:
- `Explore.Domain/Enums/ConfigurationScopeEnum.cs` — Scope enum (System, Instance, Tenant, Organization)
- `Explore.Domain/ConfigurationChangeLog.cs` — Audit entity for settings changes

Application Layer:
- `Explore.Application/Contracts/Identity/IAdminContext.cs` — Hybrid JWT + DB admin identity resolution
- `Explore.Application/Contracts/Infrastructure/ICerbosAuthorizationService.cs` — Authorization service contract
- `Explore.Application/Contracts/Infrastructure/IConfigurationChangeLogService.cs` — Audit logging contract
- `Explore.Application/Authorization/IAuthorizedRequest.cs` — MediatR authorization marker interface
- `Explore.Application/Authorization/CerbosAuthorizeAttribute.cs` — Metadata attribute for commands
- `Explore.Application/Exceptions/AuthorizationException.cs` — Maps to HTTP 403
- `Explore.Application/Behaviors/AuthorizationBehavior.cs` — MediatR pipeline (checks IAuthorizedRequest or attribute)

Persistence Layer:
- `Explore.Persistence/Configurations/Entities/ConfigurationChangeLogConfiguration.cs` — EF config with indexes
- `Explore.Application/Contracts/Persistence/IConfigurationChangeLogRepository.cs` — Repository interface
- `Explore.Persistence/Repositories/ConfigurationChangeLogRepository.cs` — Repository implementation

Infrastructure Layer:
- `Explore.Infrastructure/Identity/AdminContext.cs` — Hybrid identity (JWT claims + DB, 5-min cache)
- `Explore.Infrastructure/Services/CerbosAuthorizationService.cs` — Real Cerbos HTTP API client
- `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` — DB-only fallback when Cerbos unavailable
- `Explore.Infrastructure/Services/ConfigurationChangeLogService.cs` — Audit log writer

Cerbos Policies:
- `cerbos/policies/derived_roles.yaml` — instance_admin, tenant_admin, org_admin derived roles
- `cerbos/policies/instance_setting.yaml` — Instance settings resource policy
- `cerbos/policies/tenant_setting.yaml` — Tenant settings with lock check
- `cerbos/policies/organization.yaml` — Organization hierarchy policy

**Files Modified:**
- `ExploreDbContext.cs` — Added DbSet<ConfigurationChangeLog>
- `PersistenceServicesRegistration.cs` — Registered ConfigurationChangeLogRepository
- `ApplicationServicesRegistration.cs` — Registered AuthorizationBehavior pipeline
- `InfrastructureServicesRegistration.cs` — Registered AdminContext, CerbosAuth (conditional), ConfigChangeLog
- `docker-compose.yml` — Added Cerbos sidecar (profile: "authz")

**Architecture Decisions:**
- Cerbos via HTTP REST API (no gRPC SDK NuGet dependency) — avoids .NET 10 compatibility issues
- `Cerbos:Enabled` config toggle — FallbackAuthorizationService used when Cerbos PDP unavailable
- IMemoryCache (not Redis) for AdminContext caching — matches existing SettingsResolver pattern
- AuthorizationBehavior runs after PerformanceBehavior (perf logging includes auth time)
- Secure by default: unknown resource kinds are denied

**Build Status:** 0 errors, 151 tests passing

### Blockers
- None

### Remaining Work (Deferred)
- Wire [CerbosAuthorize] onto controllers
- Add exception middleware for AuthorizationException → 403
- EF migration for ConfigurationChangeLog table
- Unit tests for new services
- Blazor UI lock source metadata indicators

## Implementation Plan (Approved)

See detailed plan: `dev/active/admin-hierarchy-pages/polished-sparking-goose.md`

## Architecture Decision: Follow SettingsLayout Pattern

Reuse the **exact pattern** from `Explore.Blazor.Client/Components/Settings/SettingsLayout.razor`:
- MudGrid: 3-col sidebar (MudList with icons) + 9-col content area
- Section switching via component state (no sub-routing)
- Each admin level gets its own layout component + section components
- Each layout needs its own `.razor.css` (Blazor CSS isolation is scoped per component)

## Files to Create (15 new files)

### Instance Admin Settings
```
Explore.Blazor.Client/Components/Admin/Instance/
├── InstanceAdminSettingsLayout.razor        ← Sidebar layout (4 sections)
├── InstanceAdminSettingsLayout.razor.css    ← Scoped CSS (copy from SettingsLayout.razor.css)
├── InstanceGovernanceSection.razor          ← DeploymentMode, SelfService, HomePage, LockHomePage
├── InstanceDomainSection.razor              ← BaseDomain, CustomDomains, LockSubdomain, LockCustomDomain
├── InstanceBrandingSection.razor            ← Brand fields + 4 lock switches
└── InstanceModulesSection.razor             ← Islamic/Tech modules, verification policies

Explore.Blazor.Client/Pages/Admin/Instance/
└── InstanceAdminSettings.razor              ← @page "/admin/instance/settings" [Authorize(Roles="Admin")]
```

### Tenant Admin Settings
```
Explore.Blazor.Client/Components/Admin/Tenant/
├── TenantAdminSettingsLayout.razor          ← Sidebar layout (3 sections)
├── TenantAdminSettingsLayout.razor.css      ← Scoped CSS
├── TenantPoliciesSection.razor              ← Events, Approval, Verification (with lock indicators)
├── TenantDomainSection.razor                ← HomePage, Subdomain, CustomDomain (with CanOverride*)
└── TenantBrandingSection.razor              ← Brand overrides (with CanOverride*)

Explore.Blazor.Client/Pages/Admin/Tenant/
└── TenantAdminSettings.razor                ← @page "/admin/tenant/settings" [Authorize]
```

### Organization Admin Settings
```
Explore.Blazor.Client/Components/Admin/Organization/
├── OrganizationAdminSettingsLayout.razor     ← Sidebar layout (3 sections)
├── OrganizationAdminSettingsLayout.razor.css ← Scoped CSS
├── OrganizationProfileSection.razor          ← Read-only org overview + link to full profile
├── OrganizationMembersSection.razor          ← Top 5 members + "Manage Members" link
└── OrganizationVerificationSection.razor     ← Approval status display

Explore.Blazor.Client/Pages/Admin/Organization/
└── OrganizationAdminSettings.razor           ← @page "/admin/organization/{OrganizationId:guid}/settings" [Authorize]
```

## Files to Modify (4)

1. **Routes.razor** — Add 11 routes (3 settings + 8 lookup tables) with guards
   - Add `@using Explore.Blazor.Client.Pages.Admin.Instance`
   - Add `@using Explore.Blazor.Client.Pages.Admin.Tenant`
   - Add `@using Explore.Blazor.Client.Pages.Admin.Organization`
   - Add settings routes + all lookup table routes

2. **NavMenu.razor** — Add Instance Settings + Tenant Settings links in admin dropdown

3. **AdminList.razor** — Add Settings cards section before Lookup Tables section

4. **InstanceSettings.razor** and **TenantPolicySettings.razor** — Remove @page directives (replaced by new files) or delete entirely

## Key Patterns to Follow

### Sidebar Pattern (from SettingsLayout.razor)
```razor
<MudGrid Spacing="4">
    <MudItem xs="12" sm="12" md="3">
        <MudPaper Elevation="0" Class="settings-sidebar pa-0 rounded-lg border-solid border-1 mud-border-lines-default">
            <MudList T="string" Dense="false" DisablePadding="true">
                <MudListItem Icon="@Icons.Material.Filled.Tune"
                             Text="Section Name"
                             Class="@GetNavItemClass("section-key")"
                             OnClick="@(() => SelectSection("section-key"))" />
            </MudList>
        </MudPaper>
    </MudItem>
    <MudItem xs="12" sm="12" md="9">
        <MudPaper Elevation="0" Class="settings-content pa-6 rounded-lg border-solid border-1 mud-border-lines-default">
            @if (CurrentSection == "section-key") { <SectionComponent Model="_model" /> }
        </MudPaper>
    </MudItem>
</MudGrid>
```

### Section Component Pattern
```razor
@* ABOUTME: Description *@
@* ABOUTME: Part of the X Admin Settings sidebar layout *@
@using MudBlazor
@using Explore.Blazor.Client.Services

<div class="settings-section">
    <div class="settings-section-header">
        <MudText Typo="Typo.h5" Style="font-weight: 700;">Title</MudText>
        <MudText Typo="Typo.body2" Color="Color.Secondary" Class="mt-1">Description</MudText>
    </div>
    <MudStack Spacing="3">
        <!-- form fields -->
    </MudStack>
</div>

@code {
    [Parameter, EditorRequired]
    public ModelType Model { get; set; } = null!;
}
```

## Services — No New Services Needed
- Instance: `IInstanceOnboardingService` (GetStatusAsync, GetSettingsAsync, UpdateSettingsAsync)
- Tenant: `ITenantOnboardingService` (GetStatusAsync, GetSettingsAsync, UpdateSettingsAsync)
- Organization: `IOrganizationService` (GetOrganizationByIdAsync), `IOrganizationMemberService` (GetMembersAsync)
- Shared: `RoleHelper` from `Explore.Blazor.Client.Helpers` for role colors/names

## Key Models
- `InstanceGovernanceSettingsModel` — in `InstanceOnboardingService.cs`
- `TenantPolicySettingsModel` — in `TenantOnboardingService.cs`
- `InstanceOnboardingStatusModel` — has `IsCurrentUserInstanceAdmin`
- `TenantOnboardingStatusModel` — has `IsCurrentUserTenantAdministrator`, `IsCurrentUserInstanceAdministrator`
- `OrganizationDto` — from generated `EventApiClient.g.cs` (ApprovalStatusId, FullName, Email, etc.)
- `OrganizationMemberDto` — UserFullName, UserEmail, OrganizationRoleId, OrganizationPositionFullName

## Quick Resume Checklist

1. Read this context file
2. Read the plan: `polished-sparking-goose.md`
3. Build first: `dotnet build --configuration Release --verbosity quiet`
4. Create files in order: Instance sections → Instance layout+CSS+page → Tenant sections → Tenant layout+CSS+page → Org sections → Org layout+CSS+page
5. Update Routes.razor, NavMenu.razor, AdminList.razor
6. Remove @page from old InstanceSettings.razor and TenantPolicySettings.razor (or delete them)
7. Build and run all tests

ABOUTME: Plan to restructure the Blazor Client project — UI-only folder organization for a thin client that consumes an API.
ABOUTME: Focuses on intuitive page/component discovery while keeping services/infrastructure in their proper top-level layers.

# Blazor Folder Restructure — Implementation Plan

**Last Updated: 2026-02-26**

---

## Executive Summary

The `Explore.Blazor.Client` project is a **thin UI layer** that consumes the `Explore.API`. It has no business logic — services are API proxies, validators mirror server-side rules, and helpers format display data. The restructure should reflect this reality: **organize the visual layer (pages, components, dialogs) by domain area** while keeping the service/infrastructure layer flat and findable.

The current structure has two main problems:
1. **Pages and their components are split across distant folders** — Event pages in `Pages/Event/` but Event components in `Components/Event/`
2. **Dialogs live in wrong places** — `Pages/Organization/InviteMemberDialog.razor` is a component, not a page

The fix is straightforward: **co-locate pages with their components by domain area**, keep services/validators/helpers as flat top-level folders (they're thin API wrappers, not feature logic).

---

## Current State Analysis

### What's Already Good ✅
- `Layout/` is clean (MainLayout, NavMenu, Footer, AnnouncementBar, SetupLayout)
- `Clients/` is clean (NSwag generated + wrapper)
- `Configuration/` is clean (1 file)
- `Providers/` is clean (1 file — TenantContextProvider)
- `Routing/Guards/` is well-organized (5 guard files)
- Pages are already grouped by domain (`Pages/Event/`, `Pages/Organization/`, etc.)
- Components are already grouped by domain (`Components/Event/`, `Components/Admin/`, etc.)

### What's Broken ❌

| # | Problem | Example |
|---|---------|---------|
| 1 | **Pages split from their components** | Event page in `Pages/Event/`, Event components in `Components/Event/` — two clicks to find related files |
| 2 | **Dialogs in Pages/ folder** | `Pages/Organization/InviteMemberDialog.razor`, `Pages/Organization/EditMemberRoleDialog.razor` |
| 3 | **Root-level loose components** | `Components/EventRegistration.razor`, `Components/OnlineEventDialog.razor`, `Components/ReviewDialog.razor` belong to a domain area but sit at Components root |
| 4 | **Services/ is a flat list of 51 files** | Mix of domain API proxies, infrastructure services, lookup table services, and HTTP handlers |
| 5 | **Empty folders** | `Extensions/`, `Serialization/` |
| 6 | **Settings components separated from Settings page** | `Components/Settings/` has 5 files; the page is in `Pages/User/Settings.razor` |
| 7 | **Inconsistent Admin page nesting** | `Pages/Admin/TenantSettings/Navigation.razor` vs `Pages/Admin/Tenant/TenantAdminSettings.razor` |

---

## Proposed Structure

### Design Principles

1. **Pages and their components live together** — one folder per domain area containing both
2. **Services stay in Services/** — they're API proxies, not business logic; a flat folder with clear naming is best
3. **Shared/ is a strict "generic primitives only" boundary** — no domain logic allowed (see Shared/ Rules below)
4. **Feature-level `_Imports.razor`** — each domain folder gets its own `_Imports.razor` so components/dialogs auto-resolve without root clutter
5. **Dialogs expose a static `ShowAsync` method** — encapsulates DialogParameters/DialogOptions inside the dialog file itself
6. **Infrastructure concerns stay separate** — HTTP handlers, route guards, lookup services
7. **Minimal nesting** — max 3 levels from project root to any file
8. **This is a UI project, not a backend** — no vertical slices, no CQRS, just organized screens

### Proposed Folder Layout

```
Explore.Blazor.Client/
│
├── Pages/                             # ALL routable pages + their child components
│   ├── Events/                        # Event domain area
│   │   ├── EventList.razor(.cs, .css)
│   │   ├── EventDetail.razor(.cs, .css)
│   │   ├── EventEdit.razor(.css)
│   │   ├── CreateEvent.razor(.cs, .css)
│   │   ├── EventCreated.razor(.css)
│   │   ├── MyEvents.razor(.cs, .css)
│   │   ├── Components/               # Components ONLY used by Event pages
│   │   │   ├── EventSessionManager.razor(.css)
│   │   │   ├── EventSessionEditor.razor(.css)
│   │   │   ├── EventFilterBar.razor(.cs, .css)
│   │   │   ├── EventIslamicAspectCard.razor
│   │   │   ├── EventTechAspectCard.razor
│   │   │   ├── EventRegistration.razor(.css)
│   │   │   ├── TriStateCategoryFilterDropdown.razor(.cs, .css)
│   │   │   ├── TriStateTagFilterDropdown.razor(.cs, .css)
│   │   │   └── OnlineEventDialog.razor(.css)
│   │   ├── Dialogs/                   # MudBlazor dialogs for Event pages
│   │   │   ├── CreateSessionDialog.razor(.css)
│   │   │   ├── EditSessionDialog.razor(.css)
│   │   │   ├── SelectSessionDialog.razor(.css)
│   │   │   ├── SessionSelectionDialog.razor
│   │   │   ├── DeleteEventDialog.razor
│   │   │   ├── IslamicAspectEditDialog.razor
│   │   │   ├── TechAspectEditDialog.razor
│   │   │   ├── ManageSpeakersDialog.razor
│   │   │   ├── RegistrationManagerDialog.razor(.css)
│   │   │   └── EventReviewDialog.razor(.css)
│   │   └── _Imports.razor             # Cascading: auto-imports Components/ + Dialogs/ namespaces
│   │
│   ├── Organizations/                 # Organization domain area
│   │   ├── CreateOrganization.razor(.cs, .css)
│   │   ├── OrganizationDetails.razor(.cs, .css)
│   │   ├── OrganizationProfile.razor(.cs, .css)
│   │   ├── OrganizationMembers.razor(.cs, .css)
│   │   ├── OrganizationReviews.razor(.css)
│   │   ├── OrganizationSuccess.razor(.css)
│   │   ├── MyOrganizations.razor(.cs, .css)
│   │   ├── Dialogs/
│   │   │   ├── InviteMemberDialog.razor(.css)     # ← moved FROM Pages/
│   │   │   └── EditMemberRoleDialog.razor(.css)    # ← moved FROM Pages/
│   │   └── _Imports.razor             # Cascading: auto-imports Dialogs/ namespace
│   │
│   ├── Admin/                         # Admin pages + sections
│   │   ├── AdminListDetails.razor(.cs, .css)
│   │   ├── Instance/
│   │   │   ├── InstanceAdminSettings.razor
│   │   │   ├── InstanceSettings.razor
│   │   │   └── Components/           # Instance admin section components
│   │   │       ├── InstanceAdminSettingsLayout.razor(.css)
│   │   │       ├── InstanceBrandingSection.razor
│   │   │       ├── InstanceDomainSection.razor
│   │   │       ├── InstanceGovernanceSection.razor(.css)
│   │   │       ├── InstanceModulesSection.razor
│   │   │       ├── InstanceSmtpSection.razor
│   │   │       └── InstanceStorageSection.razor
│   │   ├── Tenant/
│   │   │   ├── TenantAdminSettings.razor
│   │   │   ├── TenantPolicySettings.razor
│   │   │   ├── Navigation.razor
│   │   │   └── Components/
│   │   │       ├── TenantAdminSettingsLayout.razor(.css)
│   │   │       ├── TenantBrandingSection.razor
│   │   │       ├── TenantDomainSection.razor
│   │   │       ├── TenantLookupTablesSection.razor
│   │   │       ├── TenantOrganizationsSection.razor
│   │   │       └── TenantPoliciesSection.razor
│   │   ├── Organization/
│   │   │   ├── OrganizationAdminSettings.razor
│   │   │   └── Components/
│   │   │       ├── OrganizationAdminSettingsLayout.razor(.css)
│   │   │       ├── OrganizationMembersSection.razor
│   │   │       ├── OrganizationProfileSection.razor
│   │   │       └── OrganizationVerificationSection.razor
│   │   ├── Group/
│   │   │   ├── GroupAdminSettings.razor
│   │   │   └── Components/
│   │   │       ├── GroupAdminSettingsLayout.razor(.css)
│   │   │       ├── GroupMembersSection.razor
│   │   │       └── GroupProfileSection.razor
│   │   ├── Components/               # Shared admin components
│   │   │   ├── AdminOrganizationTable.razor
│   │   │   └── TenantNavigationDialog.razor
│   │   ├── Dialogs/                   # Admin CRUD dialogs (categories, tags, locations)
│   │   │   ├── CreateCategoryDialog.razor(.css)
│   │   │   ├── EditCategoryDialog.razor(.css)
│   │   │   ├── CreateLocationDialog.razor(.css)
│   │   │   ├── EditLocationDialog.razor(.css)
│   │   │   ├── CreateTagDialog.razor(.css)
│   │   │   └── EditTagDialog.razor(.css)
│   │   └── _Imports.razor             # Cascading: auto-imports Components/, Dialogs/, sub-area namespaces
│   │
│   ├── User/                          # User profile + settings
│   │   ├── UserProfile.razor(.cs, .css)
│   │   ├── MyRegistrations.razor(.cs, .css)
│   │   ├── MyReviews.razor(.cs, .css)
│   │   ├── Settings.razor(.cs, .css)
│   │   └── Components/               # Settings sub-sections
│   │       ├── SettingsLayout.razor(.css)
│   │       ├── SettingsPersonalInfo.razor(.css)
│   │       ├── SettingsSecurity.razor(.css)
│   │       ├── SettingsPrivacy.razor(.css)
│   │       └── SettingsNotifications.razor(.css)
│   │   └── _Imports.razor             # Cascading: auto-imports Components/ namespace
│   │
│   ├── Landing/                       # Public landing (unchanged)
│   │   ├── LandingPageForNonUsers.razor(.cs, .css)
│   │   └── LandingPageForUsers.razor(.cs, .css)
│   │
│   ├── Onboarding/                    # Onboarding wizard (unchanged)
│   │   ├── InstanceOnboarding.razor
│   │   ├── TenantOnboarding.razor
│   │   └── StartupGate.razor
│   │
│   ├── Auth/                          # Auth pages (unchanged)
│   │   ├── LoginRedirect.razor
│   │   └── LogoutRedirect.razor
│   │
│   ├── Home.razor(.css)               # Root pages stay at Pages/ root
│   ├── HomeStart.razor
│   └── Setup.razor
│
├── Shared/                            # Cross-domain reusable UI components
│   ├── Loading.razor(.css)
│   ├── ErrorState.razor
│   ├── S3Image.razor(.css)
│   ├── ImageUpload.razor(.css)
│   ├── ReviewDialog.razor(.css)
│   └── AnalyticsInitializer.razor
│
├── Layout/                            # App shell (unchanged)
│   ├── MainLayout.razor(.cs, .css)
│   ├── NavMenu.razor(.cs, .css)
│   ├── Footer.razor(.css)
│   ├── AnnouncementBar.razor(.cs, .css)
│   └── SetupLayout.razor
│
├── Services/                          # API proxy services (flat, grouped by prefix)
│   ├── EventService.cs
│   ├── EventAspectService.cs
│   ├── EventRegistrationService.cs
│   ├── EventSessionSpeakerService.cs
│   ├── EventCreationEligibilityService.cs
│   ├── OrganizationService.cs
│   ├── OrganizationMemberService.cs
│   ├── OrganizationReviewService.cs
│   ├── AdminService.cs
│   ├── CategoryService.cs
│   ├── LocationService.cs
│   ├── TagService.cs
│   ├── GroupService.cs
│   ├── UserService.cs
│   ├── AuthStateService.cs
│   ├── LandingPageService.cs
│   ├── PublicExperienceService.cs
│   ├── InstanceOnboardingService.cs
│   ├── TenantOnboardingService.cs
│   ├── ImageStorageService.cs
│   ├── MapsService.cs
│   ├── StartupRoutingService.cs
│   ├── SidebarState.cs
│   ├── RuntimeRenderPolicyService.cs
│   ├── LookupCacheService.cs
│   ├── TenantNavigationService.cs
│   ├── AnalyticsInterop.cs
│   ├── LazyAssemblyLoader.cs
│   ├── Lookup/                        # Lookup table services (thin wrappers)
│   │   ├── ActorService.cs
│   │   ├── AudienceAgeService.cs
│   │   ├── AudienceGenderService.cs
│   │   ├── EventFormatService.cs
│   │   ├── EventStatusService.cs
│   │   ├── EventTypeService.cs
│   │   ├── LanguageService.cs
│   │   └── MadhabService.cs
│   ├── Http/                          # HTTP pipeline handlers
│   │   ├── BffClient.cs
│   │   ├── BffUnauthorizedHandler.cs
│   │   ├── BrowserCredentialsMessageHandler.cs
│   │   └── S3UploadMessageHandler.cs
│   └── Contracts/                     # Service interfaces (unchanged)
│       ├── IActorService.cs
│       ├── IAudienceAgeService.cs
│       ├── IAudienceGenderService.cs
│       ├── IEventAspectService.cs
│       ├── IEventFormatService.cs
│       ├── IEventSessionSpeakerService.cs
│       ├── IEventStatusService.cs
│       ├── IEventTypeService.cs
│       ├── ILanguageService.cs
│       ├── IMadhabService.cs
│       └── ITenantNavigationService.cs
│
├── Helpers/                           # Display/formatting helpers (unchanged, already well-named)
│   ├── DateTimeHelper.cs
│   ├── DisplayHelper.cs
│   ├── EventAppearanceMetadataHelper.cs
│   ├── EventColorHelper.cs
│   ├── GroupBrandingMetadataHelper.cs
│   ├── HalResourceExtensions.cs
│   ├── ImageHelper.cs
│   ├── OrganizationAppearanceMetadataHelper.cs
│   ├── RoleHelper.cs
│   └── StringHelper.cs
│
├── Validators/                        # Client-side validators (unchanged)
│   ├── CreateEventDtoValidator.cs
│   ├── CreateEventSessionDtoValidator.cs
│   ├── CreateCategoryDtoValidator.cs
│   ├── CreateLocationDtoValidator.cs
│   ├── CreateTagDtoValidator.cs
│   ├── UpdateCategoryDtoValidator.cs
│   ├── UpdateEventSessionDtoValidator.cs
│   ├── UpdateLocationDtoValidator.cs
│   └── UpdateTagDtoValidator.cs
│
├── Models/                            # Client-side models (unchanged)
│   ├── UserInfo.cs
│   ├── TenantContext.cs
│   ├── PaginatedResult.cs
│   ├── LayoutMode.cs
│   ├── CategoryFilterChangedEventArgs.cs
│   ├── TagFilterChangedEventArgs.cs
│   ├── TagFilterState.cs
│   └── Responses/
│       ├── BaseCommandResponse.cs
│       ├── EventAspectEnums.cs
│       └── ServiceResult.cs
│
├── Constants/                         # Constants (unchanged)
│   ├── ApiConstants.cs
│   └── TenantConstants.cs
│
├── Routing/Guards/                    # Route guards (unchanged)
│   ├── AuthenticatedRouteGuard.cs
│   ├── AdminRouteGuard.cs
│   ├── TenantAdminRouteGuard.cs
│   ├── OrgAdminRouteGuard.cs
│   └── GroupAdminRouteGuard.cs
│
├── Clients/                           # NSwag generated (unchanged)
│   ├── EventApiClient.cs
│   └── EventApiClient.g.cs
│
├── Configuration/                     # Config (unchanged)
│   └── TenantConfiguration.cs
│
├── Providers/                         # Cascading providers (unchanged)
│   └── TenantContextProvider.razor
│
├── _Imports.razor
├── Routes.razor
├── Program.cs
└── wwwroot/
```

---

## What Actually Changes (Minimal Moves)

### Changes to Pages/ — Merge components INTO page folders

| Current Location | New Location | Why |
|-----------------|-------------|-----|
| `Components/Event/*` | `Pages/Events/Components/` + `Pages/Events/Dialogs/` | Co-locate with Event pages |
| `Components/Admin/*` | `Pages/Admin/{Area}/Components/` + `Pages/Admin/Dialogs/` | Co-locate with Admin pages |
| `Components/Settings/*` | `Pages/User/Components/` | Co-locate with Settings page |
| `Pages/Organization/InviteMemberDialog.razor` | `Pages/Organizations/Dialogs/` | It's a dialog, not a page |
| `Pages/Organization/EditMemberRoleDialog.razor` | `Pages/Organizations/Dialogs/` | It's a dialog, not a page |
| `Pages/Event/` | `Pages/Events/` (plural) | Consistent plural naming |
| `Pages/Organization/` | `Pages/Organizations/` (plural) | Consistent plural naming |

### Changes to Components/ — Move loose root-level files to Shared/

| Current Location | New Location | Why |
|-----------------|-------------|-----|
| `Components/Loading.razor` | `Shared/Loading.razor` | Cross-domain reusable |
| `Components/ErrorState.razor` | `Shared/ErrorState.razor` | Cross-domain reusable |
| `Components/S3Image.razor` | `Shared/S3Image.razor` | Cross-domain reusable |
| `Components/ImageUpload.razor` | `Shared/ImageUpload.razor` | Cross-domain reusable |
| `Components/ReviewDialog.razor` | `Shared/ReviewDialog.razor` | Used by multiple features |
| `Components/AnalyticsInitializer.razor` | `Shared/AnalyticsInitializer.razor` | App-wide |
| `Components/EventRegistration.razor` | `Pages/Events/Components/` | Event-specific |
| `Components/OnlineEventDialog.razor` | `Pages/Events/Components/` | Event-specific |
| `Components/EventReviewDialog.razor` | `Pages/Events/Dialogs/` | Event-specific dialog |

### Changes to Services/ — Add Lookup/ and Http/ subfolders

| Current Location | New Location | Why |
|-----------------|-------------|-----|
| `Services/ActorService.cs` + 7 more lookup services | `Services/Lookup/` | Thin lookup wrappers — distinct from domain services |
| `Services/BffClient.cs` + 3 HTTP handlers | `Services/Http/` | HTTP pipeline infrastructure |

### Folders to Delete

| Folder | Why |
|--------|-----|
| `Extensions/` | Empty |
| `Serialization/` | Empty |
| `Components/` | All files moved to Pages/{Area}/Components/ or Shared/ |

---

## What Stays Unchanged

- **Layout/** — already good
- **Helpers/** — already good, flat with clear naming
- **Validators/** — already good, flat with clear naming
- **Models/** — already good
- **Constants/** — already good
- **Routing/Guards/** — already good
- **Clients/** — auto-generated, don't touch
- **Configuration/** — already good
- **Providers/** — already good
- **Services/** (domain proxy files) — stay flat at Services/ root, just get Lookup/ and Http/ subfolders

---

## Cascading Feature-Level _Imports.razor

Instead of cluttering the root `_Imports.razor` with dozens of namespaces, **each domain folder gets its own `_Imports.razor`** that auto-imports its Components/ and Dialogs/ subfolders.

### Example: `Pages/Events/_Imports.razor`

```razor
@using Explore.Blazor.Client.Pages.Events.Components
@using Explore.Blazor.Client.Pages.Events.Dialogs
```

### Example: `Pages/Admin/_Imports.razor`

```razor
@using Explore.Blazor.Client.Pages.Admin.Components
@using Explore.Blazor.Client.Pages.Admin.Dialogs
@using Explore.Blazor.Client.Pages.Admin.Instance.Components
@using Explore.Blazor.Client.Pages.Admin.Tenant.Components
@using Explore.Blazor.Client.Pages.Admin.Organization.Components
@using Explore.Blazor.Client.Pages.Admin.Group.Components
```

### Root `_Imports.razor` stays lean

The root `_Imports.razor` only needs:
```razor
@using Explore.Blazor.Client.Shared
@using Explore.Blazor.Client.Layout
@using Explore.Blazor.Client.Services
@using Explore.Blazor.Client.Helpers
@using Explore.Blazor.Client.Models
```

**Why:** Components inside `Pages/Events/` automatically "see" their own Dialogs/ and Components/ without a single `@using` in the .razor files. It localizes scope and keeps page code clean.

---

## Shared/ Component Rules (Strict Boundary)

`Shared/` is NOT a dump bucket. It has explicit rules:

### What Goes In Shared/

| Component | Why It's Shared |
|-----------|----------------|
| `Loading.razor` | Generic spinner — zero domain knowledge |
| `ErrorState.razor` | Generic error display — zero domain knowledge |
| `S3Image.razor` | Generic S3 image renderer — takes a key, renders an image |
| `ImageUpload.razor` | Generic upload widget — takes callbacks, knows nothing about what it's uploading for |
| `ReviewDialog.razor` | Generic review/rating dialog — parameterized for any entity |
| `AnalyticsInitializer.razor` | App-wide analytics bootstrap |

### Rules

1. **No domain logic inside Shared/ components.** If it mentions "Event", "Organization", or any domain entity in its internal logic, it does NOT belong in Shared/.
2. **All Shared/ components must document their parameters** with `<summary>` XML doc tags on `[Parameter]` properties.
3. **Feature-specific components are forbidden in Shared/.** If two domains need the same visual element (e.g., an entity card), it stays in the dominant domain OR becomes a truly generic primitive (e.g., `EntityCard<T>`) with no domain logic.
4. **The test:** Could this component be copy-pasted into a completely different Blazor project and work? If yes → Shared/. If no → it belongs in its domain's Components/ folder.

---

## Dialog Lifecycle Pattern (Static ShowAsync)

Every dialog in a `Dialogs/` folder **must** expose a static `ShowAsync` method that encapsulates its `DialogParameters` and `DialogOptions`.

### Pattern

```csharp
// Inside CreateSessionDialog.razor.cs (or @code block)
public static async Task<DialogResult> ShowAsync(
    IDialogService dialogService,
    CreateEventSessionDto model)
{
    var parameters = new DialogParameters<CreateSessionDialog>
    {
        { x => x.Model, model }
    };

    var options = new DialogOptions
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseOnEscapeKey = true
    };

    var dialog = await dialogService.ShowAsync<CreateSessionDialog>(
        "Create Session", parameters, options);

    return await dialog.Result;
}
```

### Usage in Pages (Clean)

```csharp
// In EventEdit.razor — calling the dialog
var result = await CreateSessionDialog.ShowAsync(DialogService, newSession);
if (!result.Canceled)
{
    // handle success
}
```

### Why This Is Better

| Before (scattered) | After (encapsulated) |
|--------------------|--------------------|
| Page builds `DialogParameters` manually | Dialog owns its own parameters |
| Page knows about `DialogOptions` | Dialog defines its own options |
| Page references `DialogParameters<T>` generic | Page calls one typed method |
| Duplicated parameter setup if dialog used from 2 pages | Single source of truth |

### Implementation Rule

- **New dialogs:** Must have `ShowAsync` from day one
- **Existing dialogs:** Add `ShowAsync` as each dialog is moved during restructure (Phase 2-4)
- **Each dialog's `ShowAsync` returns `Task<DialogResult>`** — caller handles cancellation

---

## Implementation Phases

### Phase 1: Create Shared/ + Move Loose Components (Low Risk)
- Create `Shared/` folder
- Move 6 cross-domain components from `Components/` root to `Shared/`
- Add `<summary>` XML doc tags to all `[Parameter]` properties in Shared/ components
- Delete `Extensions/`, `Serialization/`
- **Effort:** S (1-2h)

### Phase 2: Reorganize Pages/ — Merge Components Into Page Folders
- Rename `Pages/Event/` → `Pages/Events/` (plural)
- Rename `Pages/Organization/` → `Pages/Organizations/` (plural)
- Create `Pages/Events/Components/`, `Pages/Events/Dialogs/`
- Move `Components/Event/*` into `Pages/Events/Components/` and `Pages/Events/Dialogs/`
- Move dialog files out of `Pages/Organizations/` into `Pages/Organizations/Dialogs/`
- Create `Pages/Organizations/Dialogs/`
- Create feature-level `_Imports.razor` for Events/ and Organizations/ (cascading imports)
- Add static `ShowAsync` methods to all moved dialogs
- **Effort:** L (3-4h)

### Phase 3: Reorganize Admin + User Pages — Merge Admin/Settings Components
- Create `Pages/Admin/{Area}/Components/` subfolders
- Move `Components/Admin/Instance/*` → `Pages/Admin/Instance/Components/`
- Move `Components/Admin/Tenant/*` → `Pages/Admin/Tenant/Components/`
- Move `Components/Admin/Organization/*` → `Pages/Admin/Organization/Components/`
- Move `Components/Admin/Group/*` → `Pages/Admin/Group/Components/`
- Move admin dialogs to `Pages/Admin/Dialogs/`
- Move `Components/Settings/*` → `Pages/User/Components/`
- Create feature-level `_Imports.razor` for Admin/ and User/
- Add static `ShowAsync` methods to all admin CRUD dialogs
- **Effort:** L (3-4h)

### Phase 4: Organize Services/ Subfolders
- Create `Services/Lookup/`, move 8 lookup services
- Create `Services/Http/`, move 4 HTTP handlers
- **Effort:** S (1h)

### Phase 5: Update Root Imports + Routes + Namespaces + Tests
- Slim down root `_Imports.razor` (remove old component namespaces, rely on cascading feature imports)
- Update `Routes.razor` component references
- Update namespace declarations in moved files
- Update test project references
- **Effort:** L (3-4h)

### Phase 6: Cleanup + Docs
- Delete empty `Components/` folder
- Update `docs/CODEBASE_STRUCTURE.md` and `docs/BLAZOR.md`
- Add Dialog ShowAsync pattern to `docs/BLAZOR.md` or blazor-ui-conventions skill
- Add Shared/ boundary rules to docs
- Final build + test run
- **Effort:** S (1-2h)

**Total Effort: XL (~13-17h)** — increased from original estimate due to ShowAsync refactoring and _Imports.razor work

---

## Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Domain page folder | **Plural** PascalCase | `Events/`, `Organizations/`, `Admin/` |
| Page files | PascalCase, action-first or entity-first | `EventList.razor`, `CreateEvent.razor`, `MyEvents.razor` |
| Component files | PascalCase, entity prefix | `EventFilterBar.razor`, `EventSessionEditor.razor` |
| Dialog files | PascalCase, `{Action}{Entity}Dialog` | `CreateSessionDialog.razor`, `DeleteEventDialog.razor` |
| Service files | PascalCase, `{Entity}Service.cs` | `EventService.cs` |
| Code-behind | Same name + `.cs` | `EventList.razor.cs` |
| CSS isolation | Same name + `.css` | `EventList.razor.css` |

---

## Blazor-Specific Gotchas (Must-Know for Every Phase)

These are **silent failure points** unique to Blazor. Unlike `.cs` files where the compiler immediately catches missing `using` statements, `.razor` files can fail at runtime or produce confusing build errors. Every phase must account for these.

### Gotcha 1: The `_Imports.razor` Cascade

**The problem:** `_Imports.razor` files cascade downward through the folder tree. Moving a component to `Pages/Events/Components/` means either:
- The **feature-level** `Pages/Events/_Imports.razor` needs `@using Explore.Blazor.Client.Pages.Events.Components`, OR
- The **root** `_Imports.razor` needs it

If neither has it, `.razor` files in `Pages/Events/` won't recognize their own local components — but the **error message won't say "missing using"**, it will say something like `The component 'EventFilterBar' was not found`.

**Mitigation:** After every file move, immediately verify the `_Imports.razor` chain. The cascading feature-level imports (Phase 2-4) solve this permanently, but they must be created **before or during** the move — not after.

**Verification command after each move:**
```bash
dotnet build --configuration Release --verbosity quiet 2>&1 | grep -i "was not found\|could not be found\|does not contain"
```

### Gotcha 2: `@page` Directives vs Folder Structure

**The problem:** Changing the folder from `Pages/Event/EventList.razor` to `Pages/Events/EventList.razor` does **NOT** change the URL route — routes are defined by `@page "/events"` inside the `.razor` file. However:
- `Routes.razor` (or `App.razor` on server) uses `AddAdditionalAssemblies` to scan for routable components. This scans by **assembly**, not folder path — so folder moves don't break route discovery.
- But if `Routes.razor` has any **hardcoded namespace constraints** or explicit `typeof()` references to specific page components, those WILL break.

**Mitigation:** After Phase 4 (page moves), grep `Routes.razor` and `App.razor` for any explicit type references:
```bash
grep -n "typeof\|Pages\.\|Components\." Explore.Blazor.Client/Routes.razor Explore.Blazor/Components/Routes.razor
```

### Gotcha 3: Component Type References in C# Code

**The problem:** When a dialog is opened from C# code via `DialogService.ShowAsync<EventReviewDialog>(...)`, the type `EventReviewDialog` must be resolvable via a `using` statement in the **`.cs` file** (or code-behind), not just the `.razor` file. The `.razor` file gets its usings from the `_Imports.razor` cascade, but the `.razor.cs` code-behind resolves usings from its own `using` block at the top.

This means: moving `EventReviewDialog.razor` to `Pages/Events/Dialogs/` requires updating the `using` statement in **every `.razor.cs` or `.cs` file that calls `DialogService.ShowAsync<EventReviewDialog>`**.

**Mitigation:** Before moving any dialog, grep for all call sites:
```bash
# Find every file that references the dialog by type name
grep -rn "EventReviewDialog" --include="*.cs" --include="*.razor"
```

Then after the move, update every call site's `using` to point to the new namespace. The static `ShowAsync` pattern helps here — once migrated, the call site only needs one `using` for the dialog type.

### Gotcha 4: CSS Isolation File Pairing

**The problem:** CSS isolation works by **exact filename pairing**: `EventList.razor.css` must sit next to `EventList.razor` in the same folder. If you `git mv` the `.razor` file but forget the `.razor.css`, the styles silently disappear — no build error, no runtime error, just unstyled components.

**Mitigation:** Always move `.razor`, `.razor.cs`, and `.razor.css` as a unit. After each phase, verify no orphaned CSS files remain:
```bash
# Find .razor.css files with no matching .razor file in the same folder
find Explore.Blazor.Client -name "*.razor.css" | while read css; do
  razor="${css%.css}"
  [ ! -f "$razor" ] && echo "ORPHAN: $css"
done
```

### Gotcha 5: `_Imports.razor` Files Are Not `.cs` — No Namespace Declaration

**The problem:** Unlike `.cs` files, `_Imports.razor` files don't have a `namespace` declaration. They apply to all `.razor` files in the same directory and below. If you move an `_Imports.razor` from `Components/Event/` to `Pages/Events/Components/`, you must also update the `@using` directives inside it — the old `@using Explore.Blazor.Client.Components.Event` no longer makes sense.

**Mitigation:** When moving `_Imports.razor` files, rewrite them for the new location rather than copying them verbatim.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Blazouter route refs break | Medium | High | Grep Routes.razor for typeof/namespace refs after Phase 4 |
| `_Imports.razor` cascade gap | **High** | **High** | Create feature-level _Imports BEFORE moving components (Gotcha 1) |
| Namespace mismatches in .cs | High | Low | Compiler catches; bulk find-replace |
| Namespace mismatches in .razor | Medium | **Medium** | Build may show cryptic "not found" errors — check _Imports chain (Gotcha 1) |
| Dialog type refs in code-behind | **High** | Medium | Grep all `ShowAsync<Dialog>` / `Show<Dialog>` call sites before each dialog move (Gotcha 3) |
| CSS isolation silently breaks | Medium | Medium | Always move .razor + .razor.cs + .razor.css as a unit; verify no orphans (Gotcha 4) |
| Orphaned old `_Imports.razor` | Medium | Low | Delete old _Imports after move; don't leave stale cascading usings (Gotcha 5) |
| Test references break | High | Medium | Update test usings after each phase |
| Merge conflicts with parallel work | Medium | High | Dedicated branch; merge when no other UI work active |

---

## Why This Structure Works for a UI-Only Project

1. **"Where is the Event list page?"** → `Pages/Events/EventList.razor` — same place, just plural
2. **"Where are the Event page's components?"** → `Pages/Events/Components/` — RIGHT THERE, not in a separate folder tree
3. **"Where is the Event service?"** → `Services/EventService.cs` — same flat list, easy to find
4. **"Where is the BFF client?"** → `Services/Http/BffClient.cs` — infrastructure separated
5. **"Where is the loading spinner?"** → `Shared/Loading.razor` — explicitly shared
6. **"Where are the lookup services?"** → `Services/Lookup/` — thin wrappers grouped together

---

## Potential Risks & Unknowns

The **#1 silent failure point** is Gotcha 1 (`_Imports.razor` cascade). Unlike `.cs` namespace errors that produce clear compiler messages, a missing `@using` in a cascading `_Imports.razor` produces cryptic "component was not found" errors that look like the component doesn't exist. The mitigation — creating feature-level `_Imports.razor` files BEFORE moving components — is non-negotiable. Every phase must follow the order: create `_Imports.razor` → move files → build → verify.

The **#2 highest-risk move** is dialog files (Gotcha 3). Every `DialogService.ShowAsync<SomeDialog>()` call in a `.razor.cs` code-behind needs its `using` statement updated. Unlike `.razor` files that inherit from `_Imports.razor`, code-behind files resolve types from their own `using` block. Miss one and you get a runtime failure, not a build error. The grep-before-move discipline in the tasks checklist catches this.

The **#3 sneaky failure** is CSS isolation (Gotcha 4). Moving `.razor` without its `.razor.css` produces zero errors — the component just silently loses its styles. The orphan-check script in the tasks checklist catches this, but it must run after EVERY batch of moves, not just at the end.

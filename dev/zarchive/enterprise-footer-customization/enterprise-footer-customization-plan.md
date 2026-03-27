<!-- ABOUTME: Strategic implementation plan for enterprise-grade tenant-customizable footer system. -->
<!-- ABOUTME: Covers domain, application, infrastructure, API, and Blazor UI layers. -->

# Enterprise Footer Customization — Implementation Plan

**Last Updated:** 2026-03-22

---

## Executive Summary

The platform currently has a **fully hardcoded** `Footer.razor` with static link columns, static social icons, a hardcoded copyright notice, and a hardcoded description paragraph. Only `BrandDisplayName` and `BrandLogoUrl` are dynamic (pulled from `PublicExperienceService`).

This plan implements a **governed, three-tier footer customization system** that integrates with the existing 5-tier hierarchical settings cascade (`Instance → Tenant`) and the established `TenantNavigationLink`-style DB entity pattern for ordered, tenant-scoped dynamic content.

**Three trust levels:**

| Level | Actor | Capabilities |
|---|---|---|
| **Locked** | Instance admin sets, tenants inherit | Template, copyright, social links — all instance-controlled |
| **Governed** | Tenant admin configures within allowed scope | Custom link groups, description, show/hide blocks |
| **Trusted** (Phase 2) | Instance admin enables per tenant | Newsletter block, restricted HTML fragment block |

**Phase 1 scope:** Settings-driven structured footer with dynamic link groups, social link config, template selection, copyright text. No newsletter or HTML fragment blocks.

**Phase 2 scope (future):** Newsletter block (native API mode), social link URL management, footer preview UI.

---

## Current State Analysis

### What exists today

| File / Entity | Current state |
|---|---|
| `Explore.Blazor.Client/Layout/Footer.razor` | Hardcoded HTML — 3 static link columns, 4 hardcoded social icons, static description and copyright. Only `BrandDisplayName` + `BrandLogoUrl` are dynamic. |
| `Explore.Blazor.Client/Layout/Footer.razor.css` | Scoped BEM CSS exists — `site-footer`, `site-footer__brand`, etc. |
| `Explore.Domain/Settings/Definitions/BrandingSettingDefinitions.cs` | Branding settings: `display_name`, `logo_url`, `favicon_url`, `custom_css_url`. No footer keys. |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | No `Footer` nested class exists. |
| `Explore.Domain/TenantNavigationLink.cs` | Pattern reference: ordered, tenant-scoped nav link entity. Footer link groups will follow this pattern. |
| `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs` | Exposes branding but no footer config. |
| `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` | Resolves branding, analytics, render policy — no footer block. |
| `Explore.Application/Settings/Groups/BrandingSettingGroup.cs` | Pattern reference for `ISettingGroup` implementation. |
| `Explore.Application/Features/TenantOnboarding/Common/TenantPolicySettingHelpers.cs` | Shows how lock-aware settings cascade is implemented. |

### What does NOT exist (verified absent)

- No `FooterSettingDefinitions.cs`
- No `GovernanceSettingKeys.Footer` class
- No `TenantFooterLinkGroup` entity
- No `TenantFooterLink` entity
- No `FooterSettingGroup` ISettingGroup
- No `IFooterLinkGroupRepository` or `IFooterLinkRepository`
- No footer-related CQRS handlers
- No footer admin UI pages

---

## Proposed Future State

### Settings-driven footer architecture

```
Instance SystemSetting (footer.*)          ← instance admin configures + locks
         │
         ▼ (cascade with lock check)
Tenant TenantSetting (footer.*)            ← tenant admin overrides when unlocked
         │
         ▼
FooterLinkGroup + FooterLink entities      ← tenant-specific DB rows, or instance-inherited
         │
         ▼
PublicExperienceSettingsDto (footer block) ← resolved and exposed via existing API contract
         │
         ▼
Footer.razor → template component          ← renders dynamically based on resolved config
```

### Footer settings keys (new)

All keys live under `footer.*` and follow the established `GovernanceSettingKeys` pattern.

| Key | Type | Default | Scope | Description |
|---|---|---|---|---|
| `footer.enabled` | bool | `true` | Tenant | Master footer switch |
| `footer.template` | string | `"standard-3-col"` | Tenant | Layout template key |
| `footer.show_description` | bool | `true` | Tenant | Show brand description block |
| `footer.description_text` | string | `""` | Tenant | Custom description text |
| `footer.show_social_links` | bool | `true` | Tenant | Show social icons bar |
| `footer.social_links` | JSON | `"[]"` | Tenant | JSON array of `{platform, url, label}` |
| `footer.copyright_text` | string | `""` | Tenant | Copyright override (default uses brand name) |
| `footer.show_cookie_settings_link` | bool | `true` | Tenant | Show "Cookie Settings" link in legal column |
| `footer.lock_tenant_template` | bool | `false` | Instance | Prevents tenant template selection |
| `footer.lock_tenant_link_groups` | bool | `false` | Instance | Prevents tenant link group edits |
| `footer.lock_tenant_social_links` | bool | `false` | Instance | Prevents tenant social link overrides |
| `footer.lock_tenant_description` | bool | `false` | Instance | Prevents tenant description edits |
| `footer.lock_tenant_copyright` | bool | `false` | Instance | Prevents tenant copyright edits |

**Supported templates:** `minimal`, `standard-2-col`, `standard-3-col`, `community`

### Footer link group entities

Following the `TenantNavigationLink` entity pattern:

```
TenantFooterLinkGroup
  Id (Guid, UUIDv7)
  TenantId (Guid)             — nullable for instance-level groups
  Title (string, max 100)
  Order (int)
  IsActive (bool)
  CreatedAt/By, UpdatedAt/By  — IAuditableEntity
  Tenant (navigation)         — readonly
  Links (navigation)          — readonly IReadOnlyList<TenantFooterLink>

TenantFooterLink
  Id (Guid, UUIDv7)
  TenantId (Guid)
  FooterLinkGroupId (Guid)
  Label (string, max 100)
  Url (string, max 500)
  OpenInNewTab (bool)
  Order (int)
  IsActive (bool)
  CreatedAt/By, UpdatedAt/By  — IAuditableEntity
  Tenant (navigation)         — readonly
  Group (navigation)          — readonly
```

**Instance-level default groups:** `TenantId = null` rows represent instance defaults. When a tenant has no link groups, the UI falls back to instance defaults (read-only for tenants). When `footer.lock_tenant_link_groups = true`, tenant-created groups are hidden and only instance defaults are shown.

---

## Implementation Phases

### Phase 1: Domain Layer

#### Task 1.1 — Add `GovernanceSettingKeys.Footer`
- **File**: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Action**: Add nested `Footer` static class with string constants for all keys listed above.
- **Acceptance Criteria**:
  - [ ] All 12 footer keys defined as `public const string`
  - [ ] Naming convention follows `footer.lock_tenant_*` for lock flags
  - [ ] No duplicate key values
- **Effort**: S

#### Task 1.2 — Add `FooterSettingDefinitions`
- **File**: `Explore.Domain/Settings/Definitions/FooterSettingDefinitions.cs` (new)
- **Action**: Create static class following `BrandingSettingDefinitions` pattern. Register each key with `SettingDefinition(Key, ValueType, DefaultValue, Category, Description, MaxScope)`.
- **Acceptance Criteria**:
  - [ ] All 12 settings defined with correct `SettingValueType` and `MaxScope`
  - [ ] `footer.social_links` is `SettingValueType.Json`
  - [ ] Lock flags have `MaxScope: SettingScope.Instance`
  - [ ] All configurable settings have `MaxScope: SettingScope.Tenant`
  - [ ] Public `All` property returns `IReadOnlyList<SettingDefinition>`
  - [ ] File has ABOUTME header
- **Effort**: S

#### Task 1.3 — Register `FooterSettingDefinitions` in `SettingRegistry`
- **File**: `Explore.Domain/Settings/SettingRegistry.cs`
- **Action**: Add `all.AddRange(FooterSettingDefinitions.All)` in the static constructor.
- **Acceptance Criteria**:
  - [ ] `SettingRegistry.Contains("footer.enabled")` returns `true`
  - [ ] Existing `SettingRegistryTests.cs` tests still pass after addition
- **Effort**: XS

#### Task 1.4 — Create `TenantFooterLinkGroup` entity
- **File**: `Explore.Domain/TenantFooterLinkGroup.cs` (new)
- **Action**: Create entity implementing `ITenantEntity`, `IAuditableEntity`. Include `TenantId` (nullable Guid for instance-level defaults), `Title`, `Order`, `IsActive`, readonly navigation to `Tenant` and `Links`.
- **Acceptance Criteria**:
  - [ ] Implements `ITenantEntity`, `IAuditableEntity`
  - [ ] `TenantId` is `Guid?` (nullable, null = instance default)
  - [ ] `Links` navigation is `IReadOnlyList<TenantFooterLink>`
  - [ ] `Tenant` navigation is readonly
  - [ ] File has ABOUTME header and file-scoped namespace
- **Effort**: S
- **Note**: Follows `TenantNavigationLink.cs` pattern exactly.

#### Task 1.5 — Create `TenantFooterLink` entity
- **File**: `Explore.Domain/TenantFooterLink.cs` (new)
- **Action**: Create entity implementing `ITenantEntity`, `IAuditableEntity`. Include `FooterLinkGroupId`, `Label`, `Url` (max 500), `OpenInNewTab`, `Order`, `IsActive`, readonly navigation to parent group.
- **Acceptance Criteria**:
  - [ ] `FooterLinkGroupId` (Guid) is a required FK
  - [ ] `Label` max 100 chars, `Url` max 500 chars
  - [ ] `Group` navigation is readonly
  - [ ] File has ABOUTME header and file-scoped namespace
- **Effort**: S

---

### Phase 2: Application Layer

#### Task 2.1 — Create `FooterSettingGroup`
- **File**: `Explore.Application/Settings/Groups/FooterSettingGroup.cs` (new)
- **Action**: Implement `ISettingGroup` following `BrandingSettingGroup` pattern. Add typed properties for all non-lock footer settings. `SocialLinks` deserializes JSON array from raw setting value.
- **Acceptance Criteria**:
  - [ ] Implements `ISettingGroup`
  - [ ] `SettingKeys` static property returns all configurable footer keys
  - [ ] `Populate()` maps resolved settings to typed properties
  - [ ] `SocialLinks` property is `IReadOnlyList<FooterSocialLinkDto>` deserialized from JSON
  - [ ] File has ABOUTME header
- **Effort**: S

#### Task 2.2 — Create footer DTOs
- **Files** (new):
  - `Explore.Application/DTOs/Footer/FooterSettingsDto.cs`
  - `Explore.Application/DTOs/Footer/FooterLinkGroupDto.cs`
  - `Explore.Application/DTOs/Footer/FooterLinkItemDto.cs`
  - `Explore.Application/DTOs/Footer/FooterSocialLinkDto.cs`
  - `Explore.Application/DTOs/Footer/FooterConfigDto.cs` (composite: settings + groups)
- **Acceptance Criteria**:
  - [ ] `FooterSettingsDto` includes all settings fields + `Can*` delegation flags
  - [ ] `FooterLinkGroupDto` includes group metadata + list of `FooterLinkItemDto`
  - [ ] `FooterConfigDto` combines settings + link groups for the public experience endpoint
  - [ ] All files have ABOUTME header and file-scoped namespace
- **Effort**: S

#### Task 2.3 — Define repository contracts
- **Files** (new):
  - `Explore.Application/Contracts/Persistence/IFooterLinkGroupRepository.cs`
  - `Explore.Application/Contracts/Persistence/IFooterLinkRepository.cs`
- **Action**: Follow `ITenantSettingRepository` and `IEventRepository` patterns. Include `GetByTenantId`, `GetById`, `Create`, `Update`, `Delete` methods. `GetByTenantId` returns groups with their links. Include `GetInstanceDefaults` (TenantId = null groups).
- **Acceptance Criteria**:
  - [ ] Each contract method returns entities (not DTOs)
  - [ ] Methods are `Task<T>` or `Task<IReadOnlyList<T>>`
  - [ ] Files have ABOUTME header and file-scoped namespace
- **Effort**: S

#### Task 2.4 — Add footer contracts to `IUnitOfWork`
- **File**: `Explore.Application/Contracts/Persistence/IUnitOfWork.cs`
- **Action**: Add `IFooterLinkGroupRepository FooterLinkGroups { get; }` and `IFooterLinkRepository FooterLinks { get; }` properties.
- **Acceptance Criteria**:
  - [ ] Properties added to `IUnitOfWork` interface
  - [ ] No other changes to the interface
- **Effort**: XS

#### Task 2.5 — Create validators
- **Files** (new):
  - `Explore.Application/DTOs/Footer/Validators/FooterLinkGroupDtoValidator.cs`
  - `Explore.Application/DTOs/Footer/Validators/FooterLinkItemDtoValidator.cs`
  - `Explore.Application/DTOs/Footer/Validators/FooterSettingsDtoValidator.cs`
- **Action**: Use FluentValidation. Validators are manually instantiated (not DI). Follow existing validator patterns.
- **Acceptance Criteria**:
  - [ ] `Title` required, max 100 chars
  - [ ] `Label` required, max 100 chars; `Url` required, max 500, must be URI
  - [ ] Template key must be one of the allowed values
  - [ ] Social links: each `{platform, url}` validated
  - [ ] Files have ABOUTME header
- **Effort**: S

#### Task 2.6 — Create footer link group CQRS

**Queries:**
- `GetFooterLinkGroupsQuery` + `GetFooterLinkGroupsQueryHandler`
  - Returns effective link groups for a tenant (own groups if any, else instance defaults)
  - Respects `footer.lock_tenant_link_groups` — if locked, return instance defaults only
- `GetFooterConfigQuery` + `GetFooterConfigQueryHandler`
  - Returns composite `FooterConfigDto` (settings + link groups) for public consumption

**Commands:**
- `CreateFooterLinkGroupCommand` + handler
- `UpdateFooterLinkGroupCommand` + handler
- `DeleteFooterLinkGroupCommand` + handler
- `ReorderFooterLinkGroupsCommand` + handler
- `CreateFooterLinkCommand` + handler
- `UpdateFooterLinkCommand` + handler
- `DeleteFooterLinkCommand` + handler

All commands return `BaseCommandResponse<Guid>` and enforce tenant authorization.

- **Effort**: L (multiple handlers)
- **Skills**: `cqrs-mediatr-guidelines`

#### Task 2.7 — Create footer settings CQRS

**Queries:**
- `GetFooterGovernanceSettingsQuery` + handler (instance admin — reads settings + lock flags)
- `GetTenantFooterSettingsQuery` + handler (tenant admin — reads effective settings + `CanOverride*` flags)

**Commands:**
- `UpdateFooterGovernanceSettingsCommand` + handler (instance admin)
- `UpdateTenantFooterSettingsCommand` + handler (tenant admin — respects lock flags)

- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`

#### Task 2.8 — Extend `PublicExperienceSettingsDto`
- **File**: `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`
- **Action**: Add a `FooterConfig FooterConfig { get; set; }` property (using new `FooterConfigDto`).
- **Acceptance Criteria**:
  - [ ] Property added without breaking existing fields
  - [ ] Nullable or has a sensible default to avoid breaking callers
- **Effort**: XS

#### Task 2.9 — Extend `GetPublicExperienceSettingsQueryHandler`
- **File**: `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`
- **Action**: Inject `IFooterLinkGroupRepository` and `IHierarchicalSettingsResolver`. Resolve `FooterSettingGroup` and effective link groups. Populate `dto.FooterConfig`.
- **Acceptance Criteria**:
  - [ ] Footer settings resolved through hierarchical resolver
  - [ ] Link groups resolved with lock-flag logic (instance defaults when locked)
  - [ ] Result returned in `FooterConfig` field of `PublicExperienceSettingsDto`
- **Effort**: M

#### Task 2.10 — Update unit tests
- **Files** (updated/new):
  - `Event.Application.UnitTests/Features/PublicExperience/Queries/GetPublicExperienceSettingsQueryHandlerTests.cs`
  - `Event.Application.UnitTests/Features/Footer/Commands/CreateFooterLinkGroupCommandHandlerTests.cs`
  - `Event.Application.UnitTests/Features/Footer/Commands/UpdateFooterLinkGroupCommandHandlerTests.cs`
  - `Event.Application.UnitTests/Features/Footer/Commands/DeleteFooterLinkGroupCommandHandlerTests.cs`
  - `Event.Application.UnitTests/Features/Footer/Queries/GetFooterConfigQueryHandlerTests.cs`
  - `Event.Domain.UnitTests/Settings/SettingRegistryTests.cs`
- **Acceptance Criteria**:
  - [ ] All new handlers have unit tests
  - [ ] Lock flag behavior is tested (locked → tenant override ignored)
  - [ ] Instance default fallback behavior is tested
  - [ ] All existing tests remain green
- **Effort**: M

---

### Phase 3: Infrastructure Layer

#### Task 3.1 — EF Core configuration for `TenantFooterLinkGroup`
- **File**: `Explore.Persistence/Configurations/Entities/TenantFooterLinkGroupConfiguration.cs` (new)
- **Action**: Follow `TenantNavigationLinkConfiguration` as pattern. Configure table name `tenant_footer_link_groups`. Add named query filters for soft delete (if entity is soft-deletable) and tenant filter (handle nullable TenantId — filter only applies when TenantId has a value; null = instance-level, always visible). Index on `(TenantId, Order)`.
- **Acceptance Criteria**:
  - [ ] Table name `tenant_footer_link_groups`
  - [ ] `Title` required, max 100
  - [ ] Correct relationship to `Tenant` (optional FK, nullable `TenantId`)
  - [ ] Index on TenantId + Order
  - [ ] Cascade delete from tenant
  - [ ] Named query filter applies only when TenantId is non-null
- **Effort**: S
- **Skills**: `dotnet-efcore-guidelines`

#### Task 3.2 — EF Core configuration for `TenantFooterLink`
- **File**: `Explore.Persistence/Configurations/Entities/TenantFooterLinkConfiguration.cs` (new)
- **Action**: Configure table `tenant_footer_links`. FK to `TenantFooterLinkGroup` with cascade delete. Index on `(TenantId, FooterLinkGroupId, Order)`.
- **Acceptance Criteria**:
  - [ ] Table name `tenant_footer_links`
  - [ ] `Label` max 100, `Url` max 500
  - [ ] Cascade delete from group
  - [ ] Index on GroupId + Order
- **Effort**: S
- **Skills**: `dotnet-efcore-guidelines`

#### Task 3.3 — Register configurations in `ExploreDbContext`
- **File**: `Explore.Persistence/ExploreDbContext.cs`
- **Action**: Add `DbSet<TenantFooterLinkGroup>` and `DbSet<TenantFooterLink>`. Apply configurations in `OnModelCreating`.
- **Acceptance Criteria**:
  - [ ] Both `DbSet` properties added
  - [ ] Configurations applied
  - [ ] Existing tests still pass
- **Effort**: XS

#### Task 3.4 — Implement `FooterLinkGroupRepository`
- **File**: `Explore.Persistence/Repositories/FooterLinkGroupRepository.cs` (new)
- **Action**: Implement `IFooterLinkGroupRepository`. `GetByTenantId` loads groups with links included. `GetInstanceDefaults` returns groups where `TenantId = null`. All repository methods follow existing repository patterns (no DTOs returned).
- **Acceptance Criteria**:
  - [ ] Includes `.Include(g => g.Links)` on collection queries
  - [ ] Filters by `IsActive = true` on public queries
  - [ ] Returns entities, not DTOs
  - [ ] File has ABOUTME header
- **Effort**: S

#### Task 3.5 — Implement `FooterLinkRepository`
- **File**: `Explore.Persistence/Repositories/FooterLinkRepository.cs` (new)
- **Action**: Implement `IFooterLinkRepository` with CRUD methods.
- **Effort**: S

#### Task 3.6 — Register repositories in `UnitOfWork`
- **File**: `Explore.Persistence/UnitOfWork.cs`
- **Action**: Expose `FooterLinkGroups` and `FooterLinks` properties, backed by concrete repositories.
- **Effort**: XS

#### Task 3.7 — Generate EF Core migration
- **Action**: Run `dotnet ef migrations add AddFooterLinkGroups --project Explore.Persistence --startup-project Explore.API`
- **Acceptance Criteria**:
  - [ ] Migration creates `tenant_footer_link_groups` and `tenant_footer_links` tables
  - [ ] No unwanted changes to existing tables
  - [ ] `dotnet build` passes after migration
- **Effort**: S

---

### Phase 4: API Layer

#### Task 4.1 — Create `FooterController` (tenant-scoped public + admin endpoints)
- **File**: `Explore.API/Controllers/FooterController.cs` (new)
- **Action**: REST sub-resource controller for tenant footer management. Follow existing controller patterns.

Endpoints:

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/footer/config` | `[AllowAnonymous]` | Get resolved footer config (settings + link groups) for current tenant |
| `GET` | `/api/footer/link-groups` | `[AllowAnonymous]` | Get effective link groups for current tenant |
| `POST` | `/api/footer/link-groups` | `[Authorize]` | Create new link group (tenant admin) |
| `PUT` | `/api/footer/link-groups/{id}` | `[Authorize]` | Update link group |
| `DELETE` | `/api/footer/link-groups/{id}` | `[Authorize]` | Delete link group |
| `POST` | `/api/footer/link-groups/{groupId}/links` | `[Authorize]` | Add link to group |
| `PUT` | `/api/footer/link-groups/{groupId}/links/{id}` | `[Authorize]` | Update link |
| `DELETE` | `/api/footer/link-groups/{groupId}/links/{id}` | `[Authorize]` | Delete link |
| `PUT` | `/api/footer/settings` | `[Authorize]` | Update tenant footer settings (tenant admin) |
| `GET` | `/api/footer/settings` | `[Authorize]` | Get tenant footer settings with delegation flags (tenant admin) |

- **Acceptance Criteria**:
  - [ ] `GET /api/footer/config` is anonymous and caches via `[OutputCache(PolicyName = "DetailData")]`
  - [ ] Write endpoints require authenticated user + tenant admin check in handler
  - [ ] Routes registered in `Explore.API/Hateoas/RouteNames.cs`
  - [ ] Controller has ABOUTME header
- **Effort**: M
- **Skills**: `clean-architecture-rules`

#### Task 4.2 — Extend `InstanceSettingsController` for footer governance
- **File**: `Explore.API/Controllers/InstanceSettingsController.cs`
- **Action**: Add `GET /api/instance/settings/footer` and `PUT /api/instance/settings/footer` sub-resource endpoints following the existing module/branding endpoint pattern.
- **Acceptance Criteria**:
  - [ ] GET returns `FooterGovernanceSettingsDto` (settings + lock flags)
  - [ ] PUT applies updates through `UpdateFooterGovernanceSettingsCommand`
  - [ ] Both endpoints require instance admin (`[Authorize]` + `IsInstanceAdmin` check)
- **Effort**: S

#### Task 4.3 — Add route names
- **File**: `Explore.API/Hateoas/RouteNames.cs`
- **Action**: Add `FooterConfig`, `FooterLinkGroupList`, `FooterLinkGroupDetail`, `FooterLinkDetail` constants.
- **Effort**: XS

#### Task 4.4 — Update `swagger.json` (auto-generated)
- **Action**: Rebuild the project — `swagger.json` is auto-generated. Verify new endpoints appear correctly.
- **Effort**: XS

---

### Phase 5: Blazor UI

#### Task 5.1 — Extend `PublicExperienceService`
- **File**: `Explore.Blazor.Client/Services/PublicExperienceService.cs`
- **Action**: Ensure the typed client deserializes the new `FooterConfig` property from the API response.
- **Acceptance Criteria**:
  - [ ] `FooterConfig` is accessible from `PublicExperienceService.GetSettingsAsync()`
  - [ ] Null-safe (existing callers unaffected if footer config is absent)
- **Effort**: XS

#### Task 5.2 — Add footer Blazor client service types
- **File**: `Explore.Blazor.Client/Clients/EventApiClient.g.cs` — regenerated
- **Action**: After API contract update, regenerate the NSwag client to include new footer endpoints.
- **Effort**: S

#### Task 5.3 — Create footer template components
- **Files** (new):
  - `Explore.Blazor.Client/Layout/Footer/FooterStandard3Col.razor` + `.razor.css`
  - `Explore.Blazor.Client/Layout/Footer/FooterStandard2Col.razor` + `.razor.css`
  - `Explore.Blazor.Client/Layout/Footer/FooterMinimal.razor` + `.razor.css`
  - `Explore.Blazor.Client/Layout/Footer/FooterCommunity.razor` + `.razor.css`
- **Action**: Extract the existing footer HTML from `Footer.razor` into `FooterStandard3Col.razor` as the baseline. Create simplified variants for other templates. All use BEM methodology (`site-footer__*`). Accept `FooterConfigDto` as a parameter.
- **Acceptance Criteria**:
  - [ ] Each template accepts `[Parameter] FooterConfigDto Config`
  - [ ] Social links render from `Config.Settings.SocialLinks`
  - [ ] Link groups render from `Config.LinkGroups` in group+item order
  - [ ] Copyright uses `Config.Settings.CopyrightText` with year fallback
  - [ ] Cookie settings link visibility controlled by `Config.Settings.ShowCookieSettingsLink`
  - [ ] BEM class naming, scoped CSS
- **Effort**: M
- **Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 5.4 — Refactor `Footer.razor` to dispatch to template
- **File**: `Explore.Blazor.Client/Layout/Footer.razor`
- **Action**: Replace hardcoded HTML with a `FooterHost` dispatch pattern. Load `FooterConfigDto` from `PublicExperienceService`. Render the correct template component based on `Config.Settings.Template` using a `switch` expression or `DynamicComponent`. Hide entire footer if `Config.Settings.Enabled = false`.
- **Acceptance Criteria**:
  - [ ] Renders correct template based on `footer.template` setting
  - [ ] Falls back to `FooterStandard3Col` if template key unknown
  - [ ] Hides footer entirely when `footer.enabled = false`
  - [ ] Existing brand logo + display name behavior preserved
  - [ ] Cookie settings link click behavior preserved
- **Effort**: M
- **Skills**: `blazor-ui-conventions`

#### Task 5.5 — Create admin UI: Tenant Footer Settings Page
- **File**: `Explore.Blazor.Client/Pages/Admin/TenantFooterSettingsPage.razor` (new)
- **Action**: Admin page accessible to tenant admins. Allows editing: template selection, description toggle + text, copyright text, show/hide social links, social link URL management, link group management (CRUD, reorder). Respects `CanOverride*` flags from API.
- **Acceptance Criteria**:
  - [ ] MudBlazor components, BEM CSS isolation
  - [ ] Shows instance-locked fields as read-only with lock icon
  - [ ] Link group section: add/edit/delete/reorder groups and their links
  - [ ] Save button dispatches correct API calls
  - [ ] Success/error toast feedback
- **Effort**: L
- **Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 5.6 — Create admin UI: Instance Footer Governance Page
- **File**: `Explore.Blazor.Client/Pages/Admin/InstanceFooterGovernancePage.razor` (new)
- **Action**: Instance admin page for setting footer defaults and lock flags. Accessible only to instance admins.
- **Acceptance Criteria**:
  - [ ] Toggle lock flags per setting group
  - [ ] Configure instance-level defaults (template, description, social links)
  - [ ] Manage instance-level default link groups (TenantId = null entities)
  - [ ] Changes propagate to all tenants immediately
- **Effort**: M

---

### Phase 6: Testing

#### Task 6.1 — Unit tests for footer handlers
- See Task 2.10 above for handler unit tests.
- **Additional**: `FooterSettingDefinitions` registration tests in `SettingRegistryTests.cs`

#### Task 6.2 — Integration tests for footer API endpoints
- **File**: `Event.API.IntegrationTests/Features/FooterControllerTests.cs` (new)
- **Action**: Integration tests following existing `ApiEndpointSmokeTests.cs` pattern.
- **Acceptance Criteria**:
  - [ ] `GET /api/footer/config` returns 200 with correct shape
  - [ ] Unauthorized write endpoint returns 401
  - [ ] Tenant admin can create/update/delete link groups
  - [ ] Lock behavior: locked setting cannot be overridden
- **Effort**: M

#### Task 6.3 — Architecture tests (if any new boundaries)
- Verify no domain project references application DTOs, etc.
- **Effort**: XS

---

## Social Link Platform Support

Supported platform values for `footer.social_links` JSON:

| Platform key | Icon |
|---|---|
| `facebook` | `Icons.Custom.Brands.Facebook` |
| `twitter` | `Icons.Custom.Brands.Twitter` |
| `instagram` | `Icons.Custom.Brands.Instagram` |
| `linkedin` | `Icons.Custom.Brands.LinkedIn` |
| `youtube` | `Icons.Material.Filled.PlayCircle` |
| `tiktok` | (custom or Material icon) |
| `bluesky` | (custom) |
| `whatsapp` | (custom or custom icon) |
| `telegram` | (custom) |

In Phase 1, render whatever is in `social_links[]` with an icon lookup. Unknown platforms get a generic link icon.

---

## Phase 2 Newsletter Block — Pre-Design Notes

Research confirms the enterprise-grade integration order for newsletter providers (Mailchimp, ConvertKit/Kit, Beehiiv):

1. **API-proxy mode (recommended):** Your Blazor renders your own form markup. On submit, your API calls the provider's REST API (e.g., Mailchimp Audiences/Members, Kit/ConvertKit subscribe endpoint, Beehiiv subscribe API). You own: markup, validation, accessibility, GDPR double opt-in flag, anti-bot logic, error UX. Provider domain never appears in your CSP.

2. **Webhook-inbound mode:** Provider posts lifecycle events (subscribe, unsubscribe, bounce) to your API endpoint. Use for sync, not sign-up capture. Must verify HMAC signatures + timestamp to prevent replay. Multi-tenant webhook URL must resolve `tenant_id` server-side, never from the request body alone.

3. **Embed/iframe mode (avoid unless unavoidable):** CSP requires `frame-src` whitelist for provider domain. Use `sandbox="allow-forms allow-scripts"` attribute. Introduces XSS risk if provider CDN is compromised. PII (IP, browser fingerprint) flows to provider before your backend. Restrict to trusted admin role only, never default for tenant admins.

**Phase 2 block type design:** `NewsletterSignup` block config will carry `{Mode, Provider, AudienceId, DoubleOptIn}`. Mode enum: `NativeApi`, `WebhookPost`, `TrustedEmbed`. `TrustedEmbed` requires `footer.trusted_embed_enabled = true` at instance level.

---

## What is Explicitly Out of Scope (Phase 1)

- Newsletter block (Phase 2)
- `HtmlFragment` block (advanced/trusted only, Phase 3)
- File-based operator footer package (Phase 3)
- Footer preview mode (Phase 2)
- Footer version history / audit log (Phase 3)
- CSP compatibility warnings (Phase 3)

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| NSwag client not regenerated after DTO changes | High | High | Regenerate client as final step; existing `PublicExperienceSettingsDto` extension is additive |
| EF migration touches existing tables | Low | High | Review migration output before applying; keep migration isolated to new tables |
| `Footer.razor` CSS regressions when splitting into template components | Medium | Medium | Keep original BEM class names; use `site-footer__*` consistently across all templates |
| Nullable `TenantId` on `TenantFooterLinkGroup` breaks existing EF global filters | Medium | High | Configure named query filter to only apply `TenantId` filter when `TenantId != null`; instance defaults bypass filter |
| Setting key collisions with future planned keys | Low | Low | `footer.*` namespace reserved; documented in `GovernanceSettingKeys` |
| Lock flag cascade: tenant deletes link groups when instance locks | Low | Medium | Implement: when `lock_tenant_link_groups` becomes `true`, hide (not delete) tenant groups. Soft-delete or `IsActive = false` |

---

## Success Metrics

1. **Footer renders correctly** from tenant settings (no hardcoded links).
2. **Instance admin can lock** template, links, social links for all tenants.
3. **Tenant admin can customize** link groups, social links, description within allowed scope.
4. **Default link groups** (TenantId = null) are shown when tenant has no custom groups.
5. **All unit and integration tests pass** with no regressions.
6. **Build is clean** (`dotnet build --configuration Release --verbosity quiet` produces no errors/warnings).
7. **Footer template switching** works at runtime without page reload (settings cached, can be refreshed).

---

## Effort Summary

| Phase | Tasks | Effort |
|---|---|---|
| 1. Domain | 5 tasks | ~3h |
| 2. Application | 10 tasks | ~8h |
| 3. Infrastructure | 7 tasks | ~4h |
| 4. API | 4 tasks | ~3h |
| 5. Blazor UI | 6 tasks | ~10h |
| 6. Testing | 3 tasks | ~4h |
| **Total** | **35 tasks** | **~32h** |

---

## Related Documents

- `Explore.Domain/TenantNavigationLink.cs` — entity pattern reference
- `Explore.Application/Settings/Groups/BrandingSettingGroup.cs` — ISettingGroup pattern
- `Explore.Application/Features/TenantOnboarding/Common/TenantPolicySettingHelpers.cs` — lock-aware cascade pattern
- `docs/MULTI_TENANCY.md` — tenant resolution and override rules
- `docs/SECURITY.md` — authorization boundaries
- `docs/BLAZOR.md` — Blazor architecture patterns
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
- `.claude/skills/blazor-ui-conventions/SKILL.md`

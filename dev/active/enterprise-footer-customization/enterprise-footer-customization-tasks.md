<!-- ABOUTME: Task checklist for the enterprise footer customization implementation. -->
<!-- ABOUTME: Update checkboxes as tasks complete; add new tasks as discovered. -->

# Enterprise Footer Customization — Task Checklist

**Last Updated:** 2026-03-22

---

## Phase 1: Domain Layer ⏳ NOT STARTED

### 1.1 — Add `GovernanceSettingKeys.Footer`
- [ ] Add `Footer` nested static class to `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- [ ] All 12 footer keys defined as `public const string`
- [ ] Lock flags follow `footer.lock_tenant_*` naming pattern
- **Effort**: S

### 1.2 — Create `FooterSettingDefinitions`
- [ ] Create `Explore.Domain/Settings/Definitions/FooterSettingDefinitions.cs`
- [ ] File has ABOUTME header + file-scoped namespace
- [ ] All 12 settings defined with correct `SettingValueType` and `MaxScope`
- [ ] `footer.social_links` uses `SettingValueType.Json`
- [ ] Lock flags have `MaxScope: SettingScope.Instance`
- [ ] Public `All` property returns `IReadOnlyList<SettingDefinition>`
- **Effort**: S

### 1.3 — Register `FooterSettingDefinitions` in `SettingRegistry`
- [ ] Add `all.AddRange(FooterSettingDefinitions.All)` in `Explore.Domain/Settings/SettingRegistry.cs`
- [ ] Existing `SettingRegistryTests.cs` still passes
- **Effort**: XS

### 1.4 — Create `TenantFooterLinkGroup` entity
- [ ] Create `Explore.Domain/TenantFooterLinkGroup.cs`
- [ ] ABOUTME header + file-scoped namespace
- [ ] Implements `ITenantEntity`, `IAuditableEntity`
- [ ] `TenantId` is `Guid?` (nullable — null = instance default)
- [ ] `Links` navigation is readonly `IReadOnlyList<TenantFooterLink>`
- [ ] `Tenant` navigation is readonly
- **Effort**: S

### 1.5 — Create `TenantFooterLink` entity
- [ ] Create `Explore.Domain/TenantFooterLink.cs`
- [ ] ABOUTME header + file-scoped namespace
- [ ] Implements `ITenantEntity`, `IAuditableEntity`
- [ ] `FooterLinkGroupId` (Guid) required FK
- [ ] `Label` max 100, `Url` max 500
- [ ] `Group` navigation is readonly
- **Effort**: S

---

## Phase 2: Application Layer ⏳ NOT STARTED

### 2.1 — Create `FooterSettingGroup`
- [ ] Create `Explore.Application/Settings/Groups/FooterSettingGroup.cs`
- [ ] ABOUTME header + file-scoped namespace
- [ ] Implements `ISettingGroup`
- [ ] `SettingKeys` returns all configurable footer keys
- [ ] `SocialLinks` deserializes JSON from raw setting value → `IReadOnlyList<FooterSocialLinkDto>`
- **Effort**: S

### 2.2 — Create footer DTOs
- [ ] Create `Explore.Application/DTOs/Footer/FooterSettingsDto.cs`
- [ ] Create `Explore.Application/DTOs/Footer/FooterLinkGroupDto.cs`
- [ ] Create `Explore.Application/DTOs/Footer/FooterLinkItemDto.cs`
- [ ] Create `Explore.Application/DTOs/Footer/FooterSocialLinkDto.cs` (`Platform`, `Url`, `Label`)
- [ ] Create `Explore.Application/DTOs/Footer/FooterConfigDto.cs` (settings + link groups composite)
- [ ] Create `Explore.Application/DTOs/Footer/FooterGovernanceSettingsDto.cs` (lock flags + instance defaults)
- [ ] All files have ABOUTME header + file-scoped namespace
- **Effort**: S

### 2.3 — Define repository contracts
- [ ] Create `Explore.Application/Contracts/Persistence/IFooterLinkGroupRepository.cs`
- [ ] Create `Explore.Application/Contracts/Persistence/IFooterLinkRepository.cs`
- [ ] Methods return entities, not DTOs
- [ ] `GetByTenantId(Guid tenantId)` on group repo loads groups + links
- [ ] `GetInstanceDefaults()` returns groups where TenantId is null
- [ ] ABOUTME headers + file-scoped namespaces
- **Effort**: S

### 2.4 — Add footer repos to `IUnitOfWork`
- [ ] Add `IFooterLinkGroupRepository FooterLinkGroups { get; }` to `IUnitOfWork`
- [ ] Add `IFooterLinkRepository FooterLinks { get; }` to `IUnitOfWork`
- **Effort**: XS

### 2.5 — Create validators
- [ ] Create `Explore.Application/DTOs/Footer/Validators/FooterLinkGroupDtoValidator.cs`
- [ ] Create `Explore.Application/DTOs/Footer/Validators/FooterLinkItemDtoValidator.cs`
- [ ] Create `Explore.Application/DTOs/Footer/Validators/FooterSettingsDtoValidator.cs`
- [ ] Validators are manually instantiated (not DI)
- [ ] Template key validated against allowed values
- **Effort**: S

### 2.6 — Create footer link group CQRS (queries)
- [ ] Create `GetFooterLinkGroupsRequest` + `GetFooterLinkGroupsRequestHandler`
- [ ] Create `GetFooterConfigQuery` + `GetFooterConfigQueryHandler`
- [ ] `GetFooterConfigQuery` returns `FooterConfigDto` (settings + link groups)
- [ ] Respects `footer.lock_tenant_link_groups` — locked → instance defaults only
- **Effort**: M

### 2.7 — Create footer link group CQRS (commands)
- [ ] Create `CreateFooterLinkGroupCommand` + handler
- [ ] Create `UpdateFooterLinkGroupCommand` + handler
- [ ] Create `DeleteFooterLinkGroupCommand` + handler
- [ ] Create `ReorderFooterLinkGroupsCommand` + handler
- [ ] Create `CreateFooterLinkCommand` + handler
- [ ] Create `UpdateFooterLinkCommand` + handler
- [ ] Create `DeleteFooterLinkCommand` + handler
- [ ] All return `BaseCommandResponse<Guid>`
- [ ] All enforce tenant authorization (tenant admin or instance admin)
- **Effort**: L

### 2.8 — Create footer settings CQRS
- [ ] Create `GetFooterGovernanceSettingsQuery` + handler (instance admin)
- [ ] Create `GetTenantFooterSettingsQuery` + handler (tenant admin — CanOverride* flags)
- [ ] Create `UpdateFooterGovernanceSettingsCommand` + handler (instance admin)
- [ ] Create `UpdateTenantFooterSettingsCommand` + handler (respects lock flags)
- **Effort**: M

### 2.9 — Extend `PublicExperienceSettingsDto`
- [ ] Add `FooterConfig FooterConfig { get; set; }` property to `PublicExperienceSettingsDto`
- [ ] Property initialized to avoid null reference exceptions on existing callers
- **Effort**: XS

### 2.10 — Extend `GetPublicExperienceSettingsQueryHandler`
- [ ] Inject `IFooterLinkGroupRepository`
- [ ] Resolve `FooterSettingGroup` via `_hierarchicalSettingsResolver.ResolveGroupAsync<FooterSettingGroup>`
- [ ] Resolve effective link groups with lock check
- [ ] Populate `dto.FooterConfig`
- **Effort**: M

### 2.11 — Unit tests for new handlers
- [ ] Update `GetPublicExperienceSettingsQueryHandlerTests.cs` for footer config
- [ ] Create `Event.Application.UnitTests/Features/Footer/Commands/CreateFooterLinkGroupCommandHandlerTests.cs`
- [ ] Create `Event.Application.UnitTests/Features/Footer/Commands/UpdateFooterLinkGroupCommandHandlerTests.cs`
- [ ] Create `Event.Application.UnitTests/Features/Footer/Commands/DeleteFooterLinkGroupCommandHandlerTests.cs`
- [ ] Create `Event.Application.UnitTests/Features/Footer/Queries/GetFooterConfigQueryHandlerTests.cs`
- [ ] Update `Event.Domain.UnitTests/Settings/SettingRegistryTests.cs` for new footer keys
- [ ] Lock flag behavior tested: locked → ignore tenant override
- [ ] Instance default fallback tested: no tenant groups → instance defaults returned
- [ ] All tests pass with `dotnet test --project Event.Application.UnitTests`
- **Effort**: M

---

## Phase 3: Infrastructure Layer ⏳ NOT STARTED

### 3.1 — EF Core config for `TenantFooterLinkGroup`
- [ ] Create `Explore.Persistence/Configurations/Entities/TenantFooterLinkGroupConfiguration.cs`
- [ ] Table name `tenant_footer_link_groups`
- [ ] `Title` required, max 100
- [ ] FK to Tenant (optional, SetNull on delete)
- [ ] Index on `(TenantId, Order)`
- [ ] Named query filter handles nullable TenantId (show null + current tenant)
- **Effort**: S

### 3.2 — EF Core config for `TenantFooterLink`
- [ ] Create `Explore.Persistence/Configurations/Entities/TenantFooterLinkConfiguration.cs`
- [ ] Table name `tenant_footer_links`
- [ ] `Label` max 100, `Url` max 500
- [ ] FK to `TenantFooterLinkGroup` with cascade delete
- [ ] Index on `(FooterLinkGroupId, Order)`
- **Effort**: S

### 3.3 — Register in `ExploreDbContext`
- [ ] Add `DbSet<TenantFooterLinkGroup> FooterLinkGroups { get; set; }`
- [ ] Add `DbSet<TenantFooterLink> FooterLinks { get; set; }`
- [ ] Apply both configurations in `OnModelCreating`
- **Effort**: XS

### 3.4 — Implement `FooterLinkGroupRepository`
- [ ] Create `Explore.Persistence/Repositories/FooterLinkGroupRepository.cs`
- [ ] ABOUTME header + file-scoped namespace
- [ ] `GetByTenantId` uses `.Include(g => g.Links)` and orders by `Order`
- [ ] `GetInstanceDefaults` returns groups where `TenantId = null`
- [ ] Returns entities only
- **Effort**: S

### 3.5 — Implement `FooterLinkRepository`
- [ ] Create `Explore.Persistence/Repositories/FooterLinkRepository.cs`
- [ ] ABOUTME header + file-scoped namespace
- [ ] Standard CRUD methods
- **Effort**: S

### 3.6 — Register in `UnitOfWork`
- [ ] Add `FooterLinkGroups` property backed by `FooterLinkGroupRepository`
- [ ] Add `FooterLinks` property backed by `FooterLinkRepository`
- **Effort**: XS

### 3.7 — Generate EF migration
- [ ] Run: `dotnet ef migrations add AddFooterLinkGroups --project Explore.Persistence --startup-project Explore.API`
- [ ] Verify migration output: only creates new tables (no unwanted changes)
- [ ] `dotnet build --configuration Release --verbosity quiet` passes
- **Effort**: S

---

## Phase 4: API Layer ⏳ NOT STARTED

### 4.1 — Create `FooterController`
- [ ] Create `Explore.API/Controllers/FooterController.cs`
- [ ] ABOUTME header + file-scoped namespace
- [ ] `GET /api/footer/config` — `[AllowAnonymous]`, `[OutputCache(PolicyName = "DetailData")]`
- [ ] `GET /api/footer/link-groups` — `[AllowAnonymous]`
- [ ] `POST /api/footer/link-groups` — `[Authorize]`
- [ ] `PUT /api/footer/link-groups/{id}` — `[Authorize]`
- [ ] `DELETE /api/footer/link-groups/{id}` — `[Authorize]`
- [ ] `POST /api/footer/link-groups/{groupId}/links` — `[Authorize]`
- [ ] `PUT /api/footer/link-groups/{groupId}/links/{id}` — `[Authorize]`
- [ ] `DELETE /api/footer/link-groups/{groupId}/links/{id}` — `[Authorize]`
- [ ] `GET /api/footer/settings` — `[Authorize]` (tenant admin)
- [ ] `PUT /api/footer/settings` — `[Authorize]` (tenant admin)
- **Effort**: M

### 4.2 — Extend `InstanceSettingsController`
- [ ] Add `GET /api/instance/settings/footer` endpoint
- [ ] Add `PUT /api/instance/settings/footer` endpoint
- [ ] Both require instance admin check
- **Effort**: S

### 4.3 — Add route names to `RouteNames.cs`
- [ ] `FooterConfig`, `FooterLinkGroupList`, `FooterLinkGroupDetail`, `FooterLinkDetail`
- **Effort**: XS

### 4.4 — Rebuild and verify Swagger
- [ ] `dotnet build --configuration Release --verbosity quiet` passes
- [ ] New endpoints appear in `swagger.json`
- **Effort**: XS

---

## Phase 5: Blazor UI ⏳ NOT STARTED

### 5.1 — Extend `PublicExperienceService`
- [ ] Ensure `FooterConfig` from API response is accessible from `PublicExperienceService`
- [ ] Null-safe — existing callers unaffected
- **Effort**: XS

### 5.2 — Regenerate NSwag client
- [ ] Regenerate `Explore.Blazor.Client/Clients/EventApiClient.g.cs` after API changes
- [ ] Do NOT manually edit the generated file
- **Effort**: S

### 5.3 — Create footer template components
- [ ] Create `Explore.Blazor.Client/Layout/Footer/FooterStandard3Col.razor` + `.razor.css`
  - [ ] Extract current `Footer.razor` HTML into this component
  - [ ] Accept `[Parameter] FooterConfigDto Config`
  - [ ] Render link groups from `Config.LinkGroups` dynamically
  - [ ] Render social links from `Config.Settings.SocialLinks`
  - [ ] Cookie settings link visibility controlled by setting
- [ ] Create `Explore.Blazor.Client/Layout/Footer/FooterStandard2Col.razor` + `.razor.css`
- [ ] Create `Explore.Blazor.Client/Layout/Footer/FooterMinimal.razor` + `.razor.css`
- [ ] Create `Explore.Blazor.Client/Layout/Footer/FooterCommunity.razor` + `.razor.css`
- [ ] All use BEM class names consistent with `site-footer__*`
- **Effort**: M

### 5.4 — Refactor `Footer.razor` to template dispatch
- [ ] Replace hardcoded HTML with template dispatch switch
- [ ] Load `FooterConfigDto` from `PublicExperienceService`
- [ ] Render correct template via `switch` on `Config.Settings.Template`
- [ ] Default to `FooterStandard3Col` for unknown template key
- [ ] Hide entire footer if `Config.Settings.Enabled = false`
- [ ] Preserve `DrawerOpen` parameter + `_drawerOpenCss` CSS
- [ ] Preserve `HandleCookieSettingsClick()` → `CookieConsentState.RequestReopenAsync()`
- **Effort**: M

### 5.5 — Admin UI: Tenant Footer Settings Page
- [ ] Create `Explore.Blazor.Client/Pages/Admin/TenantFooterSettingsPage.razor`
- [ ] Template selection dropdown (disabled when locked by instance)
- [ ] Description toggle + textarea
- [ ] Copyright text field
- [ ] Social link manager (add/edit/delete per platform)
- [ ] Link group manager (add/edit/delete/reorder groups and links)
- [ ] Instance-locked fields shown as readonly with lock icon
- [ ] Save → API call → toast success/error
- [ ] BEM CSS isolation
- **Effort**: L

### 5.6 — Admin UI: Instance Footer Governance Page
- [ ] Create `Explore.Blazor.Client/Pages/Admin/InstanceFooterGovernancePage.razor`
- [ ] Toggle lock flags per setting group
- [ ] Configure instance-level defaults
- [ ] Manage instance-level link groups (TenantId = null)
- [ ] Instance admin only
- **Effort**: M

---

## Phase 6: Testing ⏳ NOT STARTED

### 6.1 — Unit tests (see Phase 2 Task 2.11)

### 6.2 — Integration tests
- [ ] Create `Event.API.IntegrationTests/Features/FooterControllerTests.cs`
- [ ] `GET /api/footer/config` returns 200 with correct shape
- [ ] Unauthenticated write endpoint returns 401
- [ ] Tenant admin CRUD on link groups works
- [ ] Lock behavior: locked setting cannot be overridden via tenant API
- [ ] `dotnet test --project Event.API.IntegrationTests` passes
- **Effort**: M

### 6.3 — Architecture tests check
- [ ] Run `Event.Architecture.Tests` — no new violations
- **Effort**: XS

---

## Final Verification Checklist

- [ ] `dotnet build --configuration Release --verbosity quiet` — no errors/warnings
- [ ] `dotnet test --project Event.Application.UnitTests --configuration Release --verbosity quiet` — all pass
- [ ] `dotnet test --project Event.Domain.UnitTests --configuration Release --verbosity quiet` — all pass
- [ ] `dotnet test --project Event.API.IntegrationTests --configuration Release --verbosity quiet` — all pass
- [ ] `dotnet test --project Event.Architecture.Tests --configuration Release --verbosity quiet` — all pass
- [ ] Footer renders from configured settings (not hardcoded)
- [ ] Template switching works at runtime
- [ ] Lock flags respected end-to-end
- [ ] Instance defaults shown when tenant has no own link groups
- [ ] NSwag client regenerated — no stale generated code
- [ ] All new files have ABOUTME headers
- [ ] All new C# files use file-scoped namespaces

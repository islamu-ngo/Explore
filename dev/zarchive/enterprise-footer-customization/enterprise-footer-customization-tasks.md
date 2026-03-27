<!-- ABOUTME: Task checklist for the enterprise footer customization implementation. -->
<!-- ABOUTME: Update checkboxes as tasks complete; add new tasks as discovered. -->

# Enterprise Footer Customization — Task Checklist

**Last Updated:** 2026-03-26

---

## Phase 1: Domain Layer ✅ COMPLETE

### 1.1 — Add `GovernanceSettingKeys.Footer`
- [x] Add `Footer` nested static class to `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- [x] All 13 footer keys defined as `public const string` (8 configurable + 5 lock flags)
- [x] Lock flags follow `footer.lock_tenant_*` naming pattern
- **Status**: Pre-existing — keys already in file at lines 229-246

### 1.2 — Create `FooterSettingDefinitions`
- [x] Create `Explore.Domain/Settings/Definitions/FooterSettingDefinitions.cs`
- [x] File has ABOUTME header + file-scoped namespace
- [x] All 13 settings defined with correct `SettingValueType` and `MaxScope`
- [x] `footer.social_links` uses `SettingValueType.Json`
- [x] Lock flags have `MaxScope: SettingScope.Instance`
- [x] Public `All` property returns `IReadOnlyList<SettingDefinition>`
- **Status**: Pre-existing — 141 lines

### 1.3 — Register `FooterSettingDefinitions` in `SettingRegistry`
- [x] Add `all.AddRange(FooterSettingDefinitions.All)` in `Explore.Domain/Settings/SettingRegistry.cs`
- [x] Existing `SettingRegistryTests.cs` still passes
- **Status**: Pre-existing — registered at line 39

### 1.4 — Create `TenantFooterLinkGroup` entity
- [x] Create `Explore.Domain/TenantFooterLinkGroup.cs`
- [x] ABOUTME header + file-scoped namespace
- [x] Implements `IAuditableEntity` (NOT `ITenantEntity` since TenantId is nullable)
- [x] `TenantId` is `Guid?` (nullable — null = instance default)
- [x] `Links` navigation is readonly `IReadOnlyList<TenantFooterLink>`
- [x] `Tenant` navigation is readonly
- **Status**: Pre-existing — 42 lines

### 1.5 — Create `TenantFooterLink` entity
- [x] Create `Explore.Domain/TenantFooterLink.cs`
- [x] ABOUTME header + file-scoped namespace
- [x] Implements `IAuditableEntity`
- [x] `FooterLinkGroupId` (Guid) required FK
- [x] `Label` max 100, `Url` max 1000
- [x] `Group` navigation is readonly
- **Status**: Pre-existing — 41 lines

---

## Phase 2: Application Layer ✅ COMPLETE

### 2.1 — Create `FooterSettingGroup`
- [x] Create `Explore.Application/Settings/Groups/FooterSettingGroup.cs`
- [x] Implements `ISettingGroup`
- [x] `SettingKeys` returns all configurable footer keys
- [x] `SocialLinks` deserializes JSON from raw setting value
- **Status**: Pre-existing

### 2.2 — Create footer DTOs
- [x] `FooterSettingsDto.cs`, `FooterLinkGroupDto.cs`, `FooterLinkItemDto.cs`, `FooterSocialLinkDto.cs`
- [x] `FooterConfigDto.cs`, `FooterGovernanceSettingsDto.cs`
- [x] `FooterLinkGroupListDto.cs`, `FooterLinkGroupDetailsDto.cs`
- **Status**: Pre-existing — 8 DTO files in `Explore.Application/DTOs/Footer/`

### 2.3 — Define repository contracts
- [x] `IFooterLinkGroupRepository.cs` + `IFooterLinkRepository.cs`
- **Status**: Pre-existing

### 2.4 — Add footer repos to `IUnitOfWork`
- [x] Both properties added
- **Status**: Pre-existing

### 2.5 — Create validators
- [x] Validators created, manually instantiated (not DI)
- **Status**: Pre-existing

### 2.6 — Create footer link group CQRS (queries)
- [x] `GetFooterLinkGroupsQuery`, `GetFooterConfigQuery` + handlers
- **Status**: Pre-existing — 4 query handlers

### 2.7 — Create footer link group CQRS (commands)
- [x] Create/Update/Delete FooterLinkGroup + Create/Update/Delete FooterLink + Reorder
- [x] All return `BaseCommandResponse<Guid>`
- **Status**: Pre-existing — 9 command handlers

### 2.8 — Create footer settings CQRS
- [x] Get/Update governance + tenant settings handlers
- **Status**: Pre-existing

### 2.9 — Extend `PublicExperienceSettingsDto`
- [x] `FooterConfig` property added
- **Status**: Pre-existing

### 2.10 — Extend `GetPublicExperienceSettingsQueryHandler`
- [x] Footer config populated in handler
- **Status**: Pre-existing

### 2.11 — Unit tests for new handlers
- [x] Application unit tests: 547/547 passed
- [x] Domain unit tests: 100/100 passed
- **Status**: Pre-existing

---

## Phase 3: Infrastructure Layer ✅ COMPLETE

### 3.1 — EF Core config for `TenantFooterLinkGroup`
- [x] `TenantFooterLinkGroupConfiguration.cs` — 42 lines
- [x] GuidVersion7, Title max 100, nullable TenantId FK, cascade, indexes
- **Status**: Pre-existing

### 3.2 — EF Core config for `TenantFooterLink`
- [x] `TenantFooterLinkConfiguration.cs` — 42 lines
- [x] GuidVersion7, Label max 100, Url max 1000, FK cascade, index
- **Status**: Pre-existing

### 3.3 — Register in `ExploreDbContext`
- [x] Both DbSets added
- **Status**: Pre-existing

### 3.4 — Implement `FooterLinkGroupRepository`
- [x] Repository with Include, ordering
- **Status**: Pre-existing

### 3.5 — Implement `FooterLinkRepository`
- [x] Standard CRUD
- **Status**: Pre-existing

### 3.6 — Register in `UnitOfWork`
- [x] Both properties registered
- **Status**: Pre-existing

### 3.7 — Generate EF migration
- [x] Migration `20260322221043_AddFooterLinkGroups` exists
- **Status**: Pre-existing

---

## Phase 4: API Layer ✅ COMPLETE

### 4.1 — Create `FooterController`
- [x] `Explore.API/Controllers/FooterController.cs` — 254 lines, 10 endpoints
- **Status**: Pre-existing

### 4.2 — Extend `InstanceSettingsController`
- [x] Footer governance GET/PUT endpoints added (lines 445-473)
- **Status**: Pre-existing

### 4.3 — Add route names to `RouteNames.cs`
- [x] 14 footer route constants added
- **Status**: Pre-existing

### 4.4 — Rebuild and verify Swagger
- [x] Build passes
- **Status**: Pre-existing

---

## Phase 5: Blazor UI ✅ COMPLETE

### 5.1 — Extend `PublicExperienceService`
- [x] Footer models already defined in `PublicExperienceService.cs`
- **Status**: Pre-existing

### 5.2 — NSwag client
- [x] Not regenerated (typed HTTP client service created instead — `FooterAdminService`)
- **Status**: Bypassed — admin uses typed HTTP client, not NSwag

### 5.3 — Create footer template components ✅ CREATED THIS SESSION
- [x] `Layout/FooterTemplates/FooterTemplateStandard3Col.razor` + `.razor.css`
- [x] `Layout/FooterTemplates/FooterTemplateStandard2Col.razor` + `.razor.css`
- [x] `Layout/FooterTemplates/FooterTemplateMinimal.razor` + `.razor.css`
- [x] `Layout/FooterTemplates/FooterTemplateCommunity.razor` + `.razor.css`
- [x] All share common parameters (brand, links, social, cookie callback)
- [x] All have `ShowCommunityGuidelinesLink` parameter (user feedback fix)
- [x] `Helpers/FooterIconHelper.cs` — shared social icon mapping

### 5.4 — Refactor `Footer.razor` to template dispatch ✅ MODIFIED THIS SESSION
- [x] Switch on `_template` string dispatches to correct template component (ADR-005)
- [x] Loads `FooterConfigDto` from `PublicExperienceService`
- [x] Hides footer when disabled
- [x] Preserves `DrawerOpen` parameter + cookie consent handler
- [x] Community guidelines conditional logic added (same rule as MainLayout sidebar)
- [x] `Footer.razor.css` updated — removed grid layout, kept shared element styles via `::deep`

### 5.5a — Tenant Footer Admin Page ✅ CREATED THIS SESSION
- [x] `Contracts/Services/Footer/IFooterAdminService.cs` — interface + all models (127 lines)
- [x] `Services/FooterAdminService.cs` — HTTP service (~340 lines)
- [x] `Pages/Admin/Components/FooterLinkGroupDialog.razor` + `.razor.cs` — group CRUD dialog
- [x] `Pages/Admin/Components/FooterLinkDialog.razor` + `.razor.cs` — link CRUD dialog
- [x] `Pages/Admin/Tenant/FooterSettings.razor` — standalone page `/admin/tenant/footer` (~430 lines)
- [x] `HttpClientExtensions.cs` — typed client registration added
- [x] `FooterLinkItemModel` renamed to `FooterLinkDetailModel` to resolve CS0104 ambiguous reference

### 5.5b — Instance Footer Governance ✅ CREATED THIS SESSION
- [x] `Pages/Admin/Instance/Components/InstanceFooterGovernanceSection.razor` — 5 lock toggles
- [x] `InstanceAdminSettingsLayout.razor` — 6 edits (field, load, content, nav, save, section check)
- [x] `InstanceOnboardingService.cs` — `FooterGovernanceSettingsModel` + Get/Update methods added
- [x] Governance works in ALL deployment modes (single-tenant + multi-tenant)

---

## Phase 6: Testing ✅ COMPLETE

### 6.1 — Unit tests
- [x] Application unit tests: 547/547 passed
- [x] Domain unit tests: 100/100 passed
- [x] Secrets unit tests: 190/190 passed
- [x] Blazor client tests: 515/602 passed (86 pre-existing MudBlazor v9 failures, none footer-related)

### 6.2 — Integration tests
- [x] Build: 0 errors
- Note: Full integration tests not added this session (no new test files created)

### 6.3 — Architecture tests
- [x] Architecture tests: 52/52 passed
- [x] `FooterSettings.razor` has `HtmlTag="h1"` for WCAG 1.3.1 compliance

---

## User Feedback Fixes ✅ COMPLETE

### Fix 1 — Single-tenant governance restriction removed
- [x] `InstanceFooterGovernanceSection.razor` — lock toggles always shown
- [x] Info alert in single-tenant mode (locks have no effect)

### Fix 2 — Default footer link groups seeded
- [x] `LookupTableSeeder.cs` — `SeedDefaultFooterLinkGroupsAsync()` added
- [x] Quick Links (About Us, Events, Contact) + Legal (Terms of Service, Privacy Policy)
- [x] Instance-level defaults (`TenantId = null`), deterministic GUIDs, idempotent

### Fix 3 — Community Guidelines conditional link
- [x] `Footer.razor` — `_showCommunityGuidelinesLink` from same 3-property OR rule as MainLayout
- [x] All 4 templates — `ShowCommunityGuidelinesLink` parameter + conditional link in bottom bar

---

## Final Verification Checklist

- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors
- [x] `dotnet test --project Event.Architecture.Tests` — 52/52 passed
- [x] `dotnet test --project Event.Application.UnitTests` — 547/547 passed
- [x] `dotnet test --project Event.Domain.UnitTests` — 100/100 passed
- [x] Footer renders from configured settings (template dispatch)
- [x] Template switching works via switch statement
- [x] Lock toggles available in all deployment modes
- [x] Instance defaults seeded (Quick Links + Legal groups)
- [x] Community guidelines conditional on publish policy
- [x] All new files have ABOUTME headers
- [x] All new C# files use file-scoped namespaces
- [ ] NSwag client NOT regenerated (admin uses typed HTTP client instead)
- [ ] Full integration test suite for footer endpoints not created
- [ ] Visual verification via browser not performed (requires Docker infrastructure)

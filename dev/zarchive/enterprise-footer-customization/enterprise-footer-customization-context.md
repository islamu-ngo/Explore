<!-- ABOUTME: Session context and key file map for the enterprise footer customization task. -->
<!-- ABOUTME: Read this before resuming to understand decisions, file locations, and current state. -->

# Enterprise Footer Customization — Context

**Last Updated:** 2026-03-26

---

## SESSION PROGRESS (2026-03-26)

### ✅ ALL PHASES COMPLETE
- Phase 1 (Domain): Entities, setting definitions, registry, governance keys — all pre-existing
- Phase 2 (Application): DTOs, repos, CQRS handlers, validators, mapping — all pre-existing
- Phase 3 (Infrastructure): EF configs, repos, DbContext, migration — all pre-existing
- Phase 4 (API): FooterController (10 endpoints), RouteNames, InstanceSettingsController — all pre-existing
- Phase 5 (Blazor UI): 4 template components, Footer.razor dispatch, admin pages, service layer, dialogs — **CREATED THIS SESSION**
- Phase 6 (Testing): Build 0 errors, architecture 52/52, domain 100/100, app 547/547

### ✅ USER FEEDBACK FIXES COMPLETE
- Fix 1: Single-tenant governance restriction removed — lock toggles always shown
- Fix 2: Default footer link groups seeded (Quick Links + Legal)
- Fix 3: Community guidelines conditional link in all templates

### ⚠️ REMAINING WORK (OPTIONAL)
- NSwag client regeneration (admin pages use typed HTTP client instead)
- Full integration test suite for footer API endpoints
- Visual browser verification (requires Docker infrastructure)
- All changes are **UNCOMMITTED** — need `git add` + commit

---

## Files Created This Session (20 new files)

### Footer Template Components (9 files)
1. `Explore.Blazor.Client/Helpers/FooterIconHelper.cs` — shared social icon mapping
2. `Explore.Blazor.Client/Layout/FooterTemplates/FooterTemplateStandard3Col.razor` + `.razor.css`
3. `Explore.Blazor.Client/Layout/FooterTemplates/FooterTemplateStandard2Col.razor` + `.razor.css`
4. `Explore.Blazor.Client/Layout/FooterTemplates/FooterTemplateMinimal.razor` + `.razor.css`
5. `Explore.Blazor.Client/Layout/FooterTemplates/FooterTemplateCommunity.razor` + `.razor.css`

### Admin Service Layer (2 files)
6. `Explore.Blazor.Client/Contracts/Services/Footer/IFooterAdminService.cs` — interface + all models (127 lines)
7. `Explore.Blazor.Client/Services/FooterAdminService.cs` — HTTP service (~340 lines)

### Admin Dialog Components (4 files)
8. `Explore.Blazor.Client/Pages/Admin/Components/FooterLinkGroupDialog.razor` + `.razor.cs`
9. `Explore.Blazor.Client/Pages/Admin/Components/FooterLinkDialog.razor` + `.razor.cs`

### Admin Pages (2 files)
10. `Explore.Blazor.Client/Pages/Admin/Tenant/FooterSettings.razor` — tenant admin (~430 lines)
11. `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceFooterGovernanceSection.razor`

## Files Modified This Session (9 files)

1. `Explore.Blazor.Client/Layout/Footer.razor` — template dispatch + community guidelines logic
2. `Explore.Blazor.Client/Layout/Footer.razor.css` — removed grid layout, kept shared styles
3. `Explore.Blazor/Extensions/HttpClientExtensions.cs` — added typed client registration
4. `Explore.Blazor.Client/Services/InstanceOnboardingService.cs` — added FooterGovernanceSettingsModel + methods
5. `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor` — 6 edits for footer governance
6. `Explore.Persistence/Seed/LookupTableSeeder.cs` — added SeedDefaultFooterLinkGroupsAsync()

---

## Architecture Decisions

### ADR-001: Footer link groups as DB entities (not JSON blobs)
**Decision:** Use `TenantFooterLinkGroup` + `TenantFooterLink` as first-class DB entities, following `TenantNavigationLink` pattern.

### ADR-002: Nullable `TenantId` on `TenantFooterLinkGroup` for instance defaults
**Decision:** `TenantId` is `Guid?`. When `null`, the group is instance-level default visible to all tenants.
**EF Filter:** `e.TenantId == null || e.TenantId == currentTenantId`

### ADR-003: Footer settings stored as flat governance keys (not JSON blob)
**Decision:** Each configurable setting is individual governance key. Exception: `footer.social_links` is JSON array.

### ADR-004: Phase 1 excludes newsletter and HTML fragment blocks
**Decision:** Deferred to Phase 2/3.

### ADR-005: Footer templates use `switch` in Phase 1
**Decision:** 4 known templates dispatched via switch statement. `DynamicComponent` deferred to Phase 2+.

### ADR-006 (NEW): Footer governance available in all deployment modes
**Decision:** Lock toggles shown in both single-tenant and multi-tenant modes. Info alert in single-tenant explains locks have no effect. Footer settings (defaults, template, links) are configurable regardless of deployment mode.

### ADR-007 (NEW): Default footer seeded via runtime seeding
**Decision:** Default footer link groups (Quick Links + Legal) seeded at runtime via `LookupTableSeeder`, not EF Core `HasData()`. Idempotent — only seeds if no instance-level groups exist.

### ADR-008 (NEW): Community guidelines conditional on publish policy
**Decision:** Community guidelines link shown in footer when `AllowUserSubmittedEvents || AllowOrganizationSubmittedEvents || AllowGroupSubmittedEvents` — same rule as sidebar in `MainLayout.razor.cs`.

### ADR-009 (NEW): Admin typed HTTP client instead of NSwag
**Decision:** Footer admin pages use `IFooterAdminService` (typed HttpClient) instead of NSwag-generated client. Follows `ITenantNavigationService` pattern. Admin models defined in `IFooterAdminService.cs`.

### ADR-010 (NEW): FooterLinkDetailModel naming to avoid ambiguity
**Decision:** Admin service model renamed from `FooterLinkItemModel` to `FooterLinkDetailModel` to resolve CS0104 ambiguous reference with `Services.FooterLinkItemModel` (from `PublicExperienceService.cs` globally imported via `_Imports.razor`).

---

## Key Files Reference

### Domain (Pre-existing)
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` — Footer class at lines 229-246
- `Explore.Domain/Settings/Definitions/FooterSettingDefinitions.cs` — 13 setting definitions
- `Explore.Domain/Settings/SettingRegistry.cs` — registered at line 39
- `Explore.Domain/TenantFooterLinkGroup.cs` — entity with nullable TenantId
- `Explore.Domain/TenantFooterLink.cs` — entity with FK to group

### Application (Pre-existing)
- `Explore.Application/DTOs/Footer/` — 8 DTO files
- `Explore.Application/Features/Footer/` — CQRS commands and queries
- `Explore.Application/Contracts/Persistence/IFooterLinkGroupRepository.cs`
- `Explore.Application/Contracts/Persistence/IFooterLinkRepository.cs`

### Infrastructure (Pre-existing)
- `Explore.Persistence/Configurations/Entities/TenantFooterLinkGroupConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/TenantFooterLinkConfiguration.cs`

### API (Pre-existing)
- `Explore.API/Controllers/FooterController.cs` — 254 lines, 10 endpoints
- `Explore.API/Controllers/InstanceSettingsController.cs` — footer governance at lines 445-473
- `Explore.API/Hateoas/RouteNames.cs` — 14 footer route constants

### Blazor UI (Created This Session)
- `Explore.Blazor.Client/Layout/Footer.razor` — orchestrator with template dispatch
- `Explore.Blazor.Client/Layout/FooterTemplates/` — 4 template components + CSS
- `Explore.Blazor.Client/Helpers/FooterIconHelper.cs` — social icon mapping
- `Explore.Blazor.Client/Contracts/Services/Footer/IFooterAdminService.cs` — admin service interface
- `Explore.Blazor.Client/Services/FooterAdminService.cs` — admin HTTP service
- `Explore.Blazor.Client/Pages/Admin/Tenant/FooterSettings.razor` — tenant admin page
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceFooterGovernanceSection.razor`
- `Explore.Blazor.Client/Pages/Admin/Components/FooterLinkGroupDialog.razor` + `.razor.cs`
- `Explore.Blazor.Client/Pages/Admin/Components/FooterLinkDialog.razor` + `.razor.cs`
- `Explore.Blazor/Extensions/HttpClientExtensions.cs` — typed client registration
- `Explore.Blazor.Client/Services/InstanceOnboardingService.cs` — governance model + methods
- `Explore.Persistence/Seed/LookupTableSeeder.cs` — default footer seed

---

## Footer API Endpoints (Complete)

| Method | Path | Auth | Response |
|--------|------|------|----------|
| GET | `/api/footer/config` | Anonymous | `FooterConfigDto` |
| GET | `/api/footer/link-groups` | Authorize | `List<FooterLinkGroupListDto>` |
| GET | `/api/footer/link-groups/{id}` | Authorize | `FooterLinkGroupDetailsDto` |
| POST | `/api/footer/link-groups` | Authorize | `BaseCommandResponse<Guid>` (201) |
| PUT | `/api/footer/link-groups/{id}` | Authorize | `BaseCommandResponse<Guid>` |
| DELETE | `/api/footer/link-groups/{id}` | Authorize | `bool` |
| POST | `/api/footer/link-groups/reorder` | Authorize | `BaseCommandResponse<Guid>` |
| POST | `/api/footer/link-groups/{groupId}/links` | Authorize | `BaseCommandResponse<Guid>` |
| PUT | `/api/footer/links/{id}` | Authorize | `BaseCommandResponse<Guid>` |
| DELETE | `/api/footer/links/{id}` | Authorize | `bool` |
| PUT | `/api/footer/settings` | Authorize | `BaseCommandResponse<Guid>` |
| GET | `/api/instance/settings/footer-governance` | Instance Admin | `FooterGovernanceSettingsDto` |
| PUT | `/api/instance/settings/footer-governance` | Instance Admin | `BaseCommandResponse<Guid>` |

---

## Settings Keys (13 total)

**Configurable (8):** `footer.enabled`, `footer.template`, `footer.show_description`, `footer.description_text`, `footer.show_social_links`, `footer.social_links` (JSON), `footer.copyright_text`, `footer.show_cookie_settings_link`

**Lock Flags (5):** `footer.lock_tenant_template`, `footer.lock_tenant_link_groups`, `footer.lock_tenant_social_links`, `footer.lock_tenant_description`, `footer.lock_tenant_copyright`

**Templates:** `standard-3-col` (default), `standard-2-col`, `minimal`, `community`

**Social Platforms:** facebook, twitter, instagram, linkedin, youtube, tiktok, bluesky, whatsapp, telegram, github

---

## CSS Architecture

- **Footer.razor.css** — shared element styling via `::deep` (site-footer__* BEM classes) cascading into all template children
- **Each template .razor.css** — layout-specific styles (grid, responsive breakpoints)
- Template-specific BEM blocks: `footer-std2__*`, `footer-community__*`
- Shared element classes: `site-footer__brand`, `site-footer__link`, `site-footer__bottom`, etc.

---

## Default Footer Seed Data

**Quick Links** (order 0, deterministic GUID):
- About Us → `/about`
- Events → `/events`
- Contact → `/contact`

**Legal** (order 1, deterministic GUID):
- Terms of Service → `/terms`
- Privacy Policy → `/privacy`

Both groups have `TenantId = null` (instance-level defaults).

---

## Quick Resume Instructions

1. All 6 phases + 3 user feedback fixes are **COMPLETE**.
2. Changes are **UNCOMMITTED** — run `git status` to see all new/modified files.
3. Build: `dotnet build --configuration Release --verbosity quiet` → 0 errors
4. Tests: Architecture 52/52, Domain 100/100, Application 547/547
5. Optional remaining: NSwag regen, integration tests, visual browser verification
6. Ready for commit and/or PR.

---

## Test Commands (per CLAUDE.md)

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
```

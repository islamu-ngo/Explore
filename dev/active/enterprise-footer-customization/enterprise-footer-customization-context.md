<!-- ABOUTME: Session context and key file map for the enterprise footer customization task. -->
<!-- ABOUTME: Read this before resuming to understand decisions, file locations, and current state. -->

# Enterprise Footer Customization — Context

**Last Updated:** 2026-03-22

---

## SESSION PROGRESS (2026-03-22)

### ✅ COMPLETED
- Full codebase analysis and current state documented
- Plan document created with all phases and tasks
- Context and tasks documents created

### 🟡 IN PROGRESS
- Planning phase only — no implementation started

### ⚠️ BLOCKERS / DECISIONS NEEDED
- None yet — plan is ready for review and implementation start

---

## Architecture Decisions

### ADR-001: Footer link groups as DB entities (not JSON blobs)

**Decision:** Use `TenantFooterLinkGroup` + `TenantFooterLink` as first-class DB entities, exactly like `TenantNavigationLink`.

**Why:** Footer link groups need individual management (add/edit/delete/reorder per item), tenant isolation via EF query filters, and ordered rendering. JSON blob in a setting value would make per-item management awkward and break the repository pattern.

**Alternative rejected:** Storing `[[{title, items:[...]}]]` as a JSON blob in `footer.link_groups` setting. Simpler but not manageable at item granularity and doesn't align with existing nav link entity pattern.

---

### ADR-002: Nullable `TenantId` on `TenantFooterLinkGroup` for instance defaults

**Decision:** `TenantFooterLinkGroup.TenantId` is `Guid?`. When `null`, the group is an instance-level default, visible to all tenants when they have no own groups or when tenant link groups are locked.

**Why:** Mirrors the `EventType` pattern in the domain where `TenantId = null` represents global (instance-level) values visible to all tenants.

**EF Filter implication:** The named `Tenant` query filter must be conditioned: `e.TenantId == null || e.TenantId == currentTenantId`. Requires careful EF config.

---

### ADR-003: Footer settings stored as flat governance keys (not JSON blob)

**Decision:** Each configurable setting (`footer.enabled`, `footer.template`, `footer.copyright_text`, etc.) is stored as an individual governance key row in `SystemSetting` / `TenantSetting`.

**Why:** Consistent with all existing settings (analytics, branding, email). Each key can be independently locked. The hierarchical resolver handles batch loading efficiently.

**Exception:** `footer.social_links` is a JSON array stored in a single key because it's a variable-length list of `{platform, url}` pairs that don't warrant separate DB rows.

---

### ADR-004: Phase 1 excludes newsletter and HTML fragment blocks

**Decision:** Newsletter block (NativeApi mode) and HtmlFragment block are deferred to Phase 2/3.

**Why:** Phase 1 goal is to make the existing structured footer fully configurable. Newsletter requires provider integrations (Mailchimp, ConvertKit, etc.) which is a separate concern. HTML fragment requires sanitization pipeline and elevated trust logic that adds significant complexity.

---

### ADR-005: Footer templates use `switch` in Phase 1; `DynamicComponent` deferred to Phase 2+

**Decision:** Phase 1 uses a `switch` statement in `Footer.razor` to render one of 4 known typed template components. `DynamicComponent` is deferred to Phase 2 when the block registry opens to newsletter and custom block types.

**Why Phase 1 switch:** 4 fixed templates → compile-time safety, simple to reason about.

**Why `DynamicComponent` for Phase 2+:** Research confirms Blazor's design intent — combine `DynamicComponent` for type-unknown-at-runtime dispatch with an `IFooterBlock` interface for type-safe callbacks. Build typed `IFooterBlockParameters` records per block type and serialize to the parameters dictionary at the registry layer, not the call site. This prevents ever modifying the dispatch site when new block types (newsletter, HTML fragment) are added.

**Phase 3 HTML fragment prerequisite:** If `HtmlFragment` block is ever added, **sanitization must happen at write time in the command handler** (not at render time). Use `Ganss.XSS.HtmlSanitizer` (NuGet package `HtmlSanitizer`) to allowlist safe tags/attributes before persisting. `MarkupString` bypasses all Blazor encoding — inline event handlers (`onclick`, `onerror`) execute even without `<script>` tags. CSP is a second line of defense, not a substitute for sanitization.

---

## Key Files Reference

### Files to READ before implementing each phase

**Phase 1 (Domain):**
- `Explore.Domain/TenantNavigationLink.cs` — exact entity pattern to follow
- `Explore.Domain/Settings/Definitions/BrandingSettingDefinitions.cs` — setting definition pattern
- `Explore.Domain/Settings/SettingRegistry.cs` — where to register new definitions
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` — where to add `Footer` class

**Phase 2 (Application):**
- `Explore.Application/Settings/Groups/BrandingSettingGroup.cs` — ISettingGroup pattern
- `Explore.Application/Contracts/Infrastructure/IHierarchicalSettingsResolver.cs` — resolver contract
- `Explore.Application/Features/TenantOnboarding/Common/TenantPolicySettingHelpers.cs` — lock cascade pattern
- `Explore.Application/Contracts/Persistence/IUnitOfWork.cs` — where to add new repo properties
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` — handler to extend
- `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs` — DTO to extend
- `Explore.Application/DTOs/Onboarding/TenantPolicySettingsDto.cs` — pattern for `CanOverride*` flags

**Phase 3 (Infrastructure):**
- `Explore.Persistence/Configurations/Entities/TenantPolicySetConfiguration.cs` — EF config pattern
- `Explore.Persistence/ExploreDbContext.cs` — where to add DbSets
- `Explore.Persistence/UnitOfWork.cs` — where to register repos

**Phase 4 (API):**
- `Explore.API/Controllers/InstanceSettingsController.cs` — pattern for instance admin endpoints
- `Explore.API/Hateoas/RouteNames.cs` — add new route name constants here

**Phase 5 (Blazor):**
- `Explore.Blazor.Client/Layout/Footer.razor` — existing footer to refactor
- `Explore.Blazor.Client/Layout/Footer.razor.css` — existing BEM CSS to preserve
- `Explore.Blazor.Client/Services/PublicExperienceService.cs` — service to extend
- `Explore.Blazor.Client/Layout/MainLayout.razor` — how `Footer.razor` is used (check params passed)

**Testing:**
- `Event.Application.UnitTests/Features/PublicExperience/Queries/GetPublicExperienceSettingsQueryHandlerTests.cs` — existing handler tests to extend
- `Event.Domain.UnitTests/Settings/SettingRegistryTests.cs` — settings registry tests
- `Event.API.IntegrationTests/Features/ApiEndpointSmokeTests.cs` — smoke test pattern

---

## Current Footer.razor Analysis (what must be preserved)

From `Explore.Blazor.Client/Layout/Footer.razor`:

- **Dynamic**: `_brandDisplayName` (from `PublicExperienceService`), `_brandLogoUrl`
- **Hardcoded links**: Platform (Browse Events, Categories, Organizations), Company (About, Contact, Careers), Legal (Privacy, Terms, Cookie Settings)
- **Hardcoded social icons**: Facebook, Twitter, Instagram, LinkedIn (no URLs, no real links)
- **Cookie settings click handler**: `HandleCookieSettingsClick()` → `CookieConsentState.RequestReopenAsync()` — MUST be preserved
- **CSS param**: `DrawerOpen` → `_drawerOpenCss` — MUST be preserved
- **Copyright**: `@DateTime.Now.Year @_brandDisplayName. All rights reserved.` — make configurable

---

## Settings Key Namespace Reserved

All new keys use prefix `footer.` which is currently **unoccupied** in `GovernanceSettingKeys.cs` and `SettingRegistry`.

Lock flag keys use pattern `footer.lock_tenant_{setting_name}` following the `governance.lock_tenant_*` pattern in `TenantDelegation`.

---

## Important: EF Query Filter for Nullable TenantId

The existing EF named filter `Tenant` is defined in `ExploreDbContext` and applies to all `ITenantEntity` entities. The standard filter excludes rows where `TenantId != currentTenantId`.

For `TenantFooterLinkGroup` with nullable `TenantId`, the filter must be:
```csharp
// Pseudocode — adapt to actual ExploreDbContext filter pattern
e => e.TenantId == null || e.TenantId == _tenantContext.TenantId
```

This means: show instance-level groups (null) AND current-tenant groups. Investigate how `EventType` handles this (it also supports `TenantId = null` for global lookup types).

---

## NSwag Client Regeneration

After any change to `Explore.API` DTOs or controller signatures, the generated client at `Explore.Blazor.Client/Clients/EventApiClient.g.cs` must be regenerated. This is typically done via:

```bash
dotnet tool run openapi
```

or via the NSwag tooling configured in the project. Check existing `nswag.json` or build targets.

Do NOT manually edit `EventApiClient.g.cs`.

---

## Quick Resume Instructions

1. Read `enterprise-footer-customization-plan.md` for full phase breakdown.
2. Check `enterprise-footer-customization-tasks.md` for current progress.
3. Verify: `dotnet build --configuration Release --verbosity quiet` passes.
4. Run `Event.Domain.UnitTests` and `Event.Application.UnitTests` to confirm baseline.
5. Start with Phase 1 (Domain) — it has no dependencies.

---

## Test Commands (per CLAUDE.md)

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

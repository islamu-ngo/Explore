ABOUTME: Documentation audit report and implementation plan for the ISLAMU Event platform.
ABOUTME: Senior-level review covering gaps, staleness, new undocumented features, and prioritized work items.

# Documentation Audit & Implementation Plan

> **Prepared by:** Documentation audit (senior tech lead perspective)
> **Date:** 2026-03-27
> **Branch:** develop
> **Scope:** Full `docs/` folder + all new/undocumented features introduced on this branch

---

## Executive Summary

The ISLAMU Event platform has a strong documentation foundation — well above average for an open-source self-hosted project. Most architectural patterns are captured with precision and the `CLAUDE.md` enforcement model (mandatory fetch-before-act) is an industry-grade practice.

However, a significant batch of new features introduced on the current `develop` branch are either **completely undocumented** or only **partially addressed in passing** within existing docs. Additionally, several existing docs have **stale structure maps**, **thin contributor guidance**, and **missing self-hosting onboarding**. These gaps carry real cost: contributors and self-hosters cannot reliably understand or operate the system without digging into source code.

This report audits all 38+ doc files, identifies every gap, and delivers a **prioritized, actionable implementation plan** with exact file names and content scope.

---

## Part 1 — Audit of Existing Documentation

### 1.1 Files in `docs/` and their current state

| File | Lines | Status | Notes |
|------|-------|--------|-------|
| `PROJECT.md` | ~50 | ✅ Current | Federation status accurate, scope well-stated |
| `ARCHITECTURE.md` | ~100 | ⚠️ Stale | Missing generic `OutboxMessage`/`OutboxProcessor`; background services section only mentions `PdsSyncWorker` |
| `CODEBASE_STRUCTURE.md` | ~510 | ⚠️ Stale | Last updated Feb 2026. Missing: `OutboxMessage.cs`, `OutboxProcessor.cs`, `AppButton/AppCard/AppTextField/AppDialogShell/AppIconButton`, `AccessibilityAnnouncerService`, `AccessibilityFocusService`, `AppearanceStyleBuilder`, `DialogOptionsFactory`, `FooterAdminService`, all footer pages/components, `AppearanceEditor.razor`, new CSS files, `OutboxRepository`, `OutboxMessageConfiguration`, `IOutboxRepository`, `IOutboxMessageDispatcher`, `LoggingOutboxMessageDispatcher`, `AuthorizationParityTests.cs`, `AccessibilityConventionTests.cs` |
| `API.md` | ~465 | ⚠️ Incomplete | Background services mentions only `PdsSyncWorker`; footer API endpoints not documented; actor appearance endpoint not documented |
| `BLAZOR.md` | ~190 | ⚠️ Partial | Styling architecture added but brief; wrapper components listed without parameter docs; no `AppearanceEditor` mention; accessibility services not covered |
| `SECURITY.md` | ~170 | ✅ Current | Accurate and comprehensive |
| `CONFIGURATION.md` | ~210 | ✅ Current | Analytics governance keys thorough |
| `OPERATIONS.md` | ~315 | ✅ Current | Analytics operational contract very detailed |
| `DOMAIN.md` | ~150 | ⚠️ Stale | `OutboxMessage` entity not listed; `Actor` appearance fields not listed |
| `QUICK_REFERENCE.md` | ~83 | ✅ Current | Hard constraints accurate |
| `AUTHORIZATION.md` | ~151 | ✅ Current | Three-layer model accurate |
| `AUTHORIZATION_PATTERNS.md` | ~67 | ✅ Current | Accurate but thin |
| `ACCESSIBILITY.md` | ~357 | ✅ Good | Comprehensive; `AccessibilityConventionTests` mentioned; good platform rule coverage |
| `ACCESSIBILITY_ARTIFACTS.md` | — | ✅ New | Statement template + AT matrix. Needs final values (product name, contact) |
| `MULTI_TENANCY.md` | ~126 | ✅ Current | Accurate |
| `DEPLOYMENT_MODES.md` | ~67 | ✅ Current | Accurate |
| `CUSTOM_PROPERTIES.md` | ~258 | ✅ Current | Layer 3 custom properties well-documented |
| `CODEBASE_INSIGHTS.md` | ~629 | ⚠️ Review needed | Large file; may contain patterns superseded by CSS modernization or helper refactors |
| `MODULAR_EVENTS.md` | ~110 | ✅ Current | Module governance accurate |
| `EXTENSIBILITY.md` | ~169 | ✅ Current | Accurate |
| `CONTRIBUTING.md` | **38** | ❌ Too thin | Only covers DTO workflow and PR checklist. Missing: local setup, environment prerequisites, Docker stack, test strategy, branch/commit conventions, architecture test requirements, release process |
| `GOVERNANCE.md` | — | ✅ Current | Policy rules accurate |
| `API_CHANGELOG.md` | — | ⚠️ Unknown | Not read — needs a staleness review |
| `NAMING_CONVENTIONS.md` | — | ✅ Current | Referenced by CLAUDE.md |
| `LOCALIZATION.md` | — | ✅ Current | TMS architecture documented |
| `RENDER_POLICIES.md` | — | ✅ Current | Render policy governance accurate |
| `ADMIN_HIERARCHY.md` | — | ✅ Current | Admin levels accurate |
| `FEDERATION.md` | — | ⚠️ Review | ATProto entities exist; ActivityPub status needs to match PROJECT.md "not fully implemented" language |
| `DEPLOYMENT_TIERS.md` | — | ✅ Current | Analytics tiers accurate |
| `SECURITY.md` | — | ✅ Current | RLS planned section is well-maintained |
| `TROUBLESHOOTING.md` | — | ⚠️ Unknown | Needs staleness review against new features |
| `docs/adr/ADR-001-authorization-provider-architecture.md` | — | ✅ Current | Only 1 ADR — needs more |
| `docs/semantic_versioning/CHANGELOG.md` | — | ✅ Present | Needs entry for develop branch features |
| `docs/semantic_versioning/v0.1.0.md` / `v1.0.0.md` | — | ✅ Present | Version-specific notes |
| `schemas/islamu-event.md` | — | ⚠️ Stale | `outbox_messages` table and `actor` appearance columns (background_color, background_effect, banner_color, banner_picture_id, background_image_id) likely not reflected |

### 1.2 Files Completely Missing (New Features, No Coverage)

These features are fully implemented in code but have **zero documentation** in `docs/`:

| Feature | Implemented In | Missing Doc |
|---------|---------------|-------------|
| Generic Outbox Pattern | `OutboxMessage.cs`, `OutboxProcessor.cs`, `IOutboxRepository`, `IOutboxMessageDispatcher`, `OutboxMessageConfiguration` | `OUTBOX_PATTERN.md` |
| Footer Management System | `FooterAdminService`, `IFooterAdminService`, `FooterTemplates/`, `FooterSettings.razor`, `InstanceFooterGovernanceSection.razor`, `FooterLinkDialog`, `FooterLinkGroupDialog` | `FOOTER_MANAGEMENT.md` |
| Design System / CSS Architecture | `tokens.css`, `layers.css`, `base.css`, `reset.css`, `components.css`, `utilities.css`, `mudblazor-overrides.css` | `DESIGN_SYSTEM.md` |
| MudBlazor Wrapper Components | `AppButton`, `AppCard`, `AppTextField`, `AppDialogShell`, `AppIconButton`, `DialogOptionsFactory` | Section in `DESIGN_SYSTEM.md` or `BLAZOR.md` |
| Accessibility Services | `IAccessibilityAnnouncerService`, `IAccessibilityFocusService`, `AccessibilityAnnouncerService`, `AccessibilityFocusService`, `accessibility.js` | Section in `ACCESSIBILITY.md` (service contracts) |
| AppearanceStyleBuilder | `AppearanceStyleBuilder.cs`, `AppearanceEditor.razor` | Section in `BLAZOR.md` or new `APPEARANCE.md` |
| Actor Appearance Customization | `UpdateActorAppearanceDto`, `UpdateActorAppearanceDtoValidator`, new `Actor` fields | Section in `DOMAIN.md` + `API.md` |
| Secrets Library (`Explore.Secrets`) | Full library with providers, health checks, metrics | `SECRETS.md` |
| Self-Hosting Guide | Docker, Aspire, config checklist, Keycloak setup | `SELF_HOSTING.md` |
| Getting Started (Developer Onboarding) | — | `GETTING_STARTED.md` |
| Testing Strategy | `Event.Architecture.Tests`, `TUnit` framework, test structure, CI | `TESTING.md` |
| Architecture Tests (new) | `AuthorizationParityTests.cs`, `AccessibilityConventionTests.cs` | Section in `TESTING.md` |

---

## Part 2 — Gap Analysis by Audience

### Developer Contributor

**Gaps:**
- No local environment setup from scratch (clone → running app in 1 document)
- `CONTRIBUTING.md` is 38 lines — covers only the DTO workflow and PR checklist
- No guide on running the Docker stack (`docker/keycloak`, `docker/minio`)
- No test strategy document (TUnit, test project roles, fixture patterns, integration vs unit scope)
- No architecture test guide (what `AuthorizationParityTests` enforces, how to add a new resource kind)
- `CODEBASE_STRUCTURE.md` is stale — contributors see the wrong file map

**Priority:** CRITICAL. Every new contributor needs this.

### Self-Hoster / Operator

**Gaps:**
- No `SELF_HOSTING.md` at all — this is the #1 missing document for an open-source self-hosted product
- No checklist of required environment variables / secrets
- No Keycloak realm setup guide
- No Docker Compose reference
- No MinIO / S3 setup guidance
- No Aspire AppHost local orchestration guide
- No upgrade / migration guide
- Analytics operational contract exists in `OPERATIONS.md` but is not discoverable from a fresh self-hoster perspective

**Priority:** CRITICAL. The project cannot realistically be self-hosted without this.

### Feature Developer / API Consumer

**Gaps:**
- Footer API endpoints not documented in `API.md`
- Actor appearance endpoint not documented in `API.md`
- Outbox pattern integration not documented (how to add messages, implement dispatcher)
- `Explore.Secrets` library — no doc for integrating secret providers
- No migration guide for upgrading between versions

**Priority:** HIGH. Active development is blocked without these patterns.

### Frontend / UI Developer

**Gaps:**
- MudBlazor wrapper components have no parameter reference
- CSS token system has no reference (what token to use for what, how to add new tokens)
- AppearanceStyleBuilder has no usage guide
- Accessibility services have no integration checklist
- `DialogOptionsFactory` presets not documented

**Priority:** HIGH. Front-end consistency depends on these.

---

## Part 3 — Implementation Plan

Organized by **priority tier**. Each item has: file path, content scope, estimated lines, and dependencies.

---

### Tier 1 — Critical (Blocking contributors and self-hosters)

---

#### T1-1: `GETTING_STARTED.md` — NEW

**Path:** `docs/GETTING_STARTED.md`
**Audience:** New contributors, first-time self-hosters
**Scope:**
- Prerequisites (.NET 10 SDK, Docker, Node.js if applicable)
- Clone + first build (`dotnet build`)
- Local stack startup using Aspire (`Explore.AppHost`)
- Seeded users / test credentials
- Running tests (the full list from CLAUDE.md)
- First change walkthrough (add a field end-to-end)
- Links to CONTRIBUTING.md, ARCHITECTURE.md, TROUBLESHOOTING.md

**Est. lines:** ~120
**Dependencies:** None

---

#### T1-2: `SELF_HOSTING.md` — NEW

**Path:** `docs/SELF_HOSTING.md`
**Audience:** Operators deploying their own instance
**Scope:**
- Deployment modes: SingleTenant vs MultiTenant quick-picker
- Prerequisites and supported environments
- Docker Compose stack reference (API, Blazor, PostgreSQL, Keycloak, MinIO)
- Required environment variables / secrets table (all `appsettings.json` sections + Infisical)
- Keycloak realm setup (clients, scopes, realm import file location)
- MinIO / S3-compatible storage setup
- Database migration (Event.MigrationService, how to run)
- First boot / setup secret flow
- Reverse proxy configuration (Nginx/Caddy examples, forwarded headers trust)
- Health check endpoints (`/health`, `/alive`, `/metrics`)
- Upgrade procedure (migrations, env var changes between versions)
- Analytics setup quick-start (provider selection, relay mode, CSP)

**Est. lines:** ~300
**Dependencies:** CONFIGURATION.md, DEPLOYMENT_MODES.md, OPERATIONS.md

---

#### T1-3: `CONTRIBUTING.md` — MAJOR EXPANSION (currently 38 lines)

**Path:** `docs/CONTRIBUTING.md`
**Target lines:** ~200
**Additions to make:**
- Prerequisites (exact versions)
- Local environment setup reference (link to GETTING_STARTED.md)
- Branch naming and commit message conventions (already partially there)
- Full required-validation checklist (build + all 7 test project commands)
- DTO workflow (existing — keep and expand)
- Architecture test requirements (what they check, how new resource kinds must be added)
- Accessibility contribution rules (WCAG 2.2 AA, architecture test conventions)
- CSS contribution rules (token tiers, `@layer` ordering, `mudblazor-overrides.css` whitelist)
- Pull request checklist (expand current minimal list)
- Release process overview (semantic versioning, changelog entries)
- Documentation requirements (ABOUTME headers, doc updates alongside feature work)

---

#### T1-4: `TESTING.md` — NEW

**Path:** `docs/TESTING.md`
**Audience:** Contributors writing tests
**Scope:**
- Test framework: TUnit (not xUnit/NUnit) — why, how it differs
- Test project roles:
  - `Event.Application.UnitTests` — handler unit tests, mock-free business logic
  - `Event.Domain.UnitTests` — entity invariant tests
  - `Event.Architecture.Tests` — fitness functions (dependency rules, naming, CQRS pattern, accessibility conventions, authorization parity)
  - `Event.Persistence.IntegrationTests` — repository tests against real DB
  - `Event.API.IntegrationTests` — full API integration tests with WebApplicationFactory
  - `Explore.Blazor.Client.Tests` — bUnit component tests + service tests
  - `Explore.Secrets.UnitTests` — secrets library tests
- How to run each (exact commands from CLAUDE.md)
- TRX report generation for failures
- Architecture tests deep-dive:
  - `CleanArchitectureTests` — dependency rules
  - `CqrsPatternTests` — naming conventions
  - `NamingConventionTests` — project-wide naming
  - `AuthorizationParityTests` — Cerbos ↔ fallback parity enforcement (new)
  - `AccessibilityConventionTests` — page shell / WCAG conventions (new)
- Test data / fixture strategy (seed data, deterministic UUIDs)
- Integration test environment (Testing env rate limiter bypass)
- CI test pipeline (`.github/workflows/test.yml`)
- What NOT to mock (avoid mocking in persistence/API integration tests)

**Est. lines:** ~200
**Dependencies:** None

---

### Tier 2 — High (Feature completeness, active development)

---

#### T2-1: `OUTBOX_PATTERN.md` — NEW

**Path:** `docs/OUTBOX_PATTERN.md`
**Scope:**
- What it is and why (reliable side-effect delivery, decoupled from domain writes)
- Architecture overview: `OutboxMessage` entity → `IOutboxRepository` → `OutboxProcessor` → `IOutboxMessageDispatcher`
- Entity design: fields, `OutboxMessageStatus` enum, retry lifecycle, dead-lettering
- Configuration reference (`OutboxProcessorSettings` — all 7 options with defaults)
- How to write outbox messages (inside a UnitOfWork transaction)
- How to implement a custom dispatcher (`IOutboxMessageDispatcher` contract, routing by `EventType`)
- Error handling: exponential backoff formula, dead-letter behavior, monitoring strategy
- Database setup: PostgreSQL table, JSONB payload, composite index, hygiene (`DeleteCompletedOlderThan`)
- Operational notes: idempotency requirement, at-least-once semantics, `LoggingOutboxMessageDispatcher` placeholder behavior
- Relationship to `PdsSyncWorker` (same poll-lock-dispatch pattern, different entity)

**Est. lines:** ~150
**Dependencies:** ARCHITECTURE.md, DOMAIN.md

---

#### T2-2: `FOOTER_MANAGEMENT.md` — NEW

**Path:** `docs/FOOTER_MANAGEMENT.md`
**Scope:**
- Purpose: per-tenant, instance-governed footer customization
- Architecture: `FooterAdminService` → `/api/footer/*` endpoints → database; `PublicExperienceService` → read path
- Template dispatch: `Footer.razor` switches on template name; available templates: `standard-3-col`, `standard-2-col`, `minimal`, `community`
- Data model: `FooterLinkGroup` → `FooterLink` hierarchy; reorder support
- Social links: supported platforms (Facebook, Twitter/X, Instagram, LinkedIn, YouTube, GitHub, TikTok, Bluesky, WhatsApp, Telegram) + icon resolution via `FooterIconHelper`
- API endpoint reference table (all 11 endpoints)
- Governance / locking: `InstanceFooterGovernanceSection` lock toggles for multi-tenant deployments
- Admin UX: `FooterSettings.razor` tenant admin page, `InstanceFooterGovernanceSection.razor` instance admin section
- Single-tenant behavior (informational alert displayed)

**Est. lines:** ~130
**Dependencies:** API.md, MULTI_TENANCY.md

---

#### T2-3: `DESIGN_SYSTEM.md` — NEW

**Path:** `docs/DESIGN_SYSTEM.md`
**Scope:**

**Section 1 — CSS Layer Architecture**
- `@layer` cascade order: reset → base → tokens → mudblazor-overrides → components → utilities
- Layer responsibilities and interaction with Blazor CSS isolation (unlayered beats all layers)
- Dark mode strategy: MudThemeProvider swaps `--mud-palette-*`; semantic tokens inherit automatically

**Section 2 — Design Token System**
- Tier 1 Primitives: space scale (4px grid), radius, shadows, font families
- Tier 2 Semantic: color aliases, spacing semantics, radius semantics, fluid typography (`clamp()`)
- Tier 3 Component: card, button scoped tokens
- How to add a new token (naming conventions, which tier, update `tokens.css`)
- Accessibility tokens: `--isl-target-min`, `--isl-focus-ring-*`, state colors via `color-mix()`
- Motion and contrast: `@media (prefers-reduced-motion)`, `@media (prefers-contrast: more)`, `@media (forced-colors: active)`

**Section 3 — MudBlazor Wrapper Components**
- `AppButton` — parameter reference, defaults, when to use vs raw MudButton
- `AppCard` — defaults, CSS isolation pattern
- `AppTextField<T>` — defaults, generic type parameter
- `AppIconButton` — defaults
- `AppDialogShell` — structural shell, `ActionsContent` slot
- `DialogOptionsFactory` — preset table (Small, Medium, Confirmation, Editor) with MaxWidth, escape, backdrop, positioning
- Migration guidance from bare MudBlazor components

**Section 4 — MudBlazor Override Policy**
- `mudblazor-overrides.css` whitelist model
- `JUSTIFICATION` comment requirement
- Approved exceptions (drawer container, portal overlay, z-index)
- How to request a new override

**Section 5 — AppearanceStyleBuilder**
- `AppearanceSettings` model fields
- `BuildStyle()`, `BuildHeroStyle()`, `BuildBannerStyle()` methods
- Effect resolution map (`SoftOverlay`, `StrongOverlay`, `Blur`, `None`)
- Dark mode interaction (custom backgrounds override MudTheme surface)
- Usage example

**Est. lines:** ~250
**Dependencies:** BLAZOR.md

---

#### T2-4: Update `ARCHITECTURE.md` — ADD outbox section + update background services

**Changes:**
- Add "Generic Outbox Pattern" section after Federation Status:
  - Entity: `OutboxMessage` in Domain
  - Background worker: `OutboxProcessor` in API (`BackgroundServices/`)
  - Contrast with `PdsSyncWorker` (PDS-specific outbox vs generic outbox)
  - Configuration: `OutboxProcessorSettings`
  - Link to `OUTBOX_PATTERN.md`
- Update "Background Services" note in `Explore.API` structure to include `OutboxProcessor.cs`
- Cross-reference OUTBOX_PATTERN.md at bottom

---

#### T2-5: Update `DOMAIN.md` — Actor appearance fields + OutboxMessage entity

**Changes:**
- Add `OutboxMessage` to entity categories table (generic delivery entity, Guid PK)
- Add Actor appearance fields section:
  - `BackgroundColor`, `BackgroundEffect`, `BannerColor`, `BannerPictureId`, `BackgroundImageId`
  - Nullable all — absence means no customization
  - `BackgroundEffect` enum values: `None`, `SoftOverlay`, `StrongOverlay`, `Blur`

---

#### T2-6: Update `CODEBASE_STRUCTURE.md` — Restore accuracy (major update)

**Changes (Explore.Domain section):**
- Add `OutboxMessage.cs` — generic outbox entity for reliable delivery

**Changes (Explore.Application section):**
- Add `IOutboxMessageDispatcher` and `IOutboxRepository` to Contracts/Infrastructure and Contracts/Persistence
- Add `DTOs/Actor/UpdateActorAppearanceDto.cs` and `Validators/`

**Changes (Explore.Infrastructure section):**
- Add `Outbox/LoggingOutboxMessageDispatcher.cs` and `OutboxProcessorSettings.cs`

**Changes (Explore.Persistence section):**
- Add `Configurations/Entities/OutboxMessageConfiguration.cs`
- Add `Repositories/OutboxRepository.cs`

**Changes (Explore.API section):**
- Update `BackgroundServices/` to list both `PdsSyncWorker.cs` and `OutboxProcessor.cs`

**Changes (Explore.Blazor.Client section):**
- Add `Components/Common/` folder with all 5 wrapper components
- Add `Contracts/Services/Accessibility/` with `IAccessibilityAnnouncerService.cs`, `IAccessibilityFocusService.cs`
- Add `Contracts/Services/Footer/IFooterAdminService.cs`
- Add `Services/Accessibility/` with `AccessibilityAnnouncerService.cs`, `AccessibilityFocusService.cs`
- Add `Services/FooterAdminService.cs`, `Services/DialogOptionsFactory.cs`
- Add `Helpers/AppearanceStyleBuilder.cs` (replaces 3 deleted helpers)
- Add `Shared/AppearanceEditor.razor`
- Add `Layout/FooterTemplates/` folder
- Add `Pages/Admin/Instance/Components/InstanceFooterGovernanceSection.razor`
- Add `Pages/Admin/Tenant/FooterSettings.razor`
- Add `Pages/Admin/Components/FooterLinkDialog.razor`, `FooterLinkGroupDialog.razor`
- Update `wwwroot/js/` to include `accessibility.js`

**Changes (Explore.Blazor section):**
- Update `wwwroot/css/` to reflect new files: `tokens.css`, `layers.css`, `base.css`, `reset.css`, `components.css`, `utilities.css`, `mudblazor-overrides.css` (replacing `StyleGlobal.css`)

**Changes (Event.Architecture.Tests section):**
- Add `AuthorizationParityTests.cs` and `AccessibilityConventionTests.cs`

---

### Tier 3 — Medium (Completeness, long-term maintainability)

---

#### T3-1: `SECRETS.md` — NEW

**Path:** `docs/SECRETS.md`
**Scope:**
- `Explore.Secrets` library purpose (multi-provider secret abstraction)
- Supported providers: `None` (env vars), `Infisical`
- Configuration (`SecretProvider:Provider`, `SecretProvider:Infisical:*`)
- Secret refresh (hosted `SecretRefreshService`, `SecretRefresh:*` config)
- Health check and metrics (`SecretProviderHealthCheck`, `SecretRefreshMetrics`)
- Required secrets table (what must be set for API vs Blazor vs MigrationService)
- How to add a new secret provider
- Key compatibility mapping (Infisical → .NET canonical keys, `TrySet` behavior)

**Est. lines:** ~120
**Dependencies:** CONFIGURATION.md

---

#### T3-2: Update `ACCESSIBILITY.md` — Add service contract integration guide

**Changes:**
- Add section: "Accessibility Services — Integration Guide"
  - When to use `AnnouncePoliteAsync` vs `AnnounceAssertiveAsync` with concrete examples
  - Dialog focus save/restore pattern (`SaveFocusAsync` / `RestoreFocusAsync`)
  - Navigation focus pattern (`FocusOnNavigateAsync` in `OnAfterRenderAsync`)
  - Form validation focus pattern (`FocusByIdAsync` on first invalid field)
  - Live region markup requirements (must exist in MainLayout before module loads)
- Add section: "Architecture Test Coverage"
  - What `AccessibilityConventionTests` enforces (7 tests)
  - How to handle exclusions (settings wrapper pages)
  - RTL advisory test (`ScopedCss_MustNotUsePhysicalDirectionProperties`) and bypass annotation

---

#### T3-3: Update `API.md` — Add footer endpoints + actor appearance endpoint

**Changes:**
- Add "Footer" to Key Endpoint Groups section:
  - `GET /api/footer/config` — fetch footer configuration
  - `GET /api/footer/link-groups` / `/{id}` — link group read endpoints
  - `POST/PUT/DELETE /api/footer/link-groups/*` — CRUD
  - `POST /api/footer/link-groups/reorder` — reorder
  - `POST/PUT/DELETE /api/footer/links/*` — link CRUD within group
  - `PUT /api/footer/settings` — tenant footer settings
- Add actor appearance endpoint to Actors section:
  - `PUT /api/actors/{id}` with `UpdateActorAppearanceDto` payload pattern

---

#### T3-4: Update `schemas/islamu-event.md` — New tables and columns

**Changes:**
- Add `outbox_messages` table definition (all columns, types, indexes, composite index for worker)
- Add appearance columns to `actors` table: `background_color`, `background_effect`, `banner_color`, `banner_picture_id`, `background_image_id`
- Add footer tables (if present in schema): `footer_link_groups`, `footer_links`, `footer_settings`

---

#### T3-5: Add ADRs for major decisions on this branch

**New files in `docs/adr/`:**

**ADR-002: Generic Outbox Pattern for Reliable Side Effects**
- Context: Need reliable at-least-once delivery of side effects (notifications, webhooks) without blocking domain transactions
- Decision: Generic `OutboxMessage` entity + `OutboxProcessor` background worker + `IOutboxMessageDispatcher` abstraction
- Consequences: Idempotent consumers required; dead-letter monitoring needed; `LoggingOutboxMessageDispatcher` as default no-op

**ADR-003: CSS Layer Architecture and Design Token System**
- Context: MudBlazor v9 removed MudGlobal defaults; needed consistent component appearance without fighting MudTheme
- Decision: `@layer` cascade, 3-tier token system in CSS custom properties, MudBlazor wrapper components, override whitelist
- Consequences: Layer ordering is load-order sensitive; unlayered scoped CSS beats all layers (intended); dark mode piggybacks on MudTheme

**ADR-004: Accessibility Architecture (WCAG 2.2 AA)**
- Context: Need programmatic accessibility in Blazor without deprecated `FocusOnNavigate`; need architectural enforcement
- Decision: `IAccessibilityAnnouncerService` / `IAccessibilityFocusService` JS interop services; `AccessibilityConventionTests` architecture fitness functions
- Consequences: Tests fail CI if page shell contract is broken; services must be registered in DI before use; live regions must exist in MainLayout before JS module loads

**ADR-005: Multi-Tier Footer Customization System**
- Context: Instance operators and tenants need customizable footer without code changes
- Decision: FooterAdminService + governance lock system + template dispatch; 4 templates; per-tenant CRUD with instance-level override locks
- Consequences: Footer data on critical render path via PublicExperienceService; template switch must be zero-downtime

---

#### T3-6: Update `docs/semantic_versioning/CHANGELOG.md`

**Changes:**
- Add `[Unreleased]` / next version section with entries for all develop branch features:
  - Generic Outbox Pattern
  - Actor background customization
  - Footer management system (CRUD, governance, templates)
  - CSS design system modernization (tokens, layers, wrapper components)
  - Accessibility services (announcer, focus management, JS module)
  - WCAG 2.2 AA architecture fitness tests
  - Authorization parity architecture tests
  - AppearanceStyleBuilder (replaces 3 metadata helpers)
  - `AppearanceEditor` component
  - MudBlazor v9 upgrade (wrapper components, DialogOptionsFactory)

---

### Tier 4 — Maintenance / polish (Do when opportunity allows)

| Item | File | Action |
|------|------|--------|
| Update `README.md` | `README.md` | Verify 456 lines are current; add DESIGN_SYSTEM.md and SELF_HOSTING.md to doc links |
| Stale review `CODEBASE_INSIGHTS.md` | `docs/CODEBASE_INSIGHTS.md` | 629 lines — audit for superseded patterns (deleted helpers, old CSS patterns) |
| Review `FEDERATION.md` | `docs/FEDERATION.md` | Verify ATProto status matches PROJECT.md language |
| Review `TROUBLESHOOTING.md` | `docs/TROUBLESHOOTING.md` | Add footer/appearance/outbox failure scenarios |
| Review `API_CHANGELOG.md` | `docs/API_CHANGELOG.md` | Add entries for new endpoints |
| Review `AUTHORIZATION_PATTERNS.md` | `docs/AUTHORIZATION_PATTERNS.md` | Cross-reference `AuthorizationParityTests` convention |
| Complete `ACCESSIBILITY_ARTIFACTS.md` | `docs/ACCESSIBILITY_ARTIFACTS.md` | Fill in product name, contact, date, pending test evidence |
| `docs/index.md` | `docs/index.md` | Add new docs to navigation index |

---

## Part 4 — New File Summary Table

| File | Status | Priority | Est. Lines |
|------|--------|----------|-----------|
| `docs/GETTING_STARTED.md` | NEW | T1 — Critical | ~120 |
| `docs/SELF_HOSTING.md` | NEW | T1 — Critical | ~300 |
| `docs/TESTING.md` | NEW | T1 — Critical | ~200 |
| `docs/OUTBOX_PATTERN.md` | NEW | T2 — High | ~150 |
| `docs/FOOTER_MANAGEMENT.md` | NEW | T2 — High | ~130 |
| `docs/DESIGN_SYSTEM.md` | NEW | T2 — High | ~250 |
| `docs/adr/ADR-002-outbox-pattern.md` | NEW | T3 — Medium | ~80 |
| `docs/adr/ADR-003-css-layer-architecture.md` | NEW | T3 — Medium | ~60 |
| `docs/adr/ADR-004-accessibility-architecture.md` | NEW | T3 — Medium | ~60 |
| `docs/adr/ADR-005-footer-customization.md` | NEW | T3 — Medium | ~60 |
| `docs/SECRETS.md` | NEW | T3 — Medium | ~120 |

**Total new docs: 11 files, ~1,530 lines**

---

## Part 5 — Files to Update Summary

| File | Priority | Change Type |
|------|----------|-------------|
| `docs/CONTRIBUTING.md` | T1 — Critical | Major expansion (38 → ~200 lines) |
| `docs/CODEBASE_STRUCTURE.md` | T2 — High | Major update (15+ new entries) |
| `docs/ARCHITECTURE.md` | T2 — High | Add outbox section + background services |
| `docs/DOMAIN.md` | T2 — High | Add OutboxMessage + Actor appearance fields |
| `docs/ACCESSIBILITY.md` | T3 — Medium | Add service integration guide + architecture test section |
| `docs/API.md` | T3 — Medium | Add footer endpoints + actor appearance |
| `docs/BLAZOR.md` | T3 — Medium | Expand styling + add AppearanceStyleBuilder |
| `schemas/islamu-event.md` | T3 — Medium | Add outbox_messages, actor appearance columns, footer tables |
| `docs/semantic_versioning/CHANGELOG.md` | T3 — Medium | Add unreleased entries |
| `docs/index.md` | T4 — Maintenance | Add new doc links |
| `docs/CODEBASE_INSIGHTS.md` | T4 — Maintenance | Audit for superseded patterns |
| `docs/TROUBLESHOOTING.md` | T4 — Maintenance | New failure scenarios |
| `docs/API_CHANGELOG.md` | T4 — Maintenance | New endpoint entries |
| `docs/ACCESSIBILITY_ARTIFACTS.md` | T4 — Maintenance | Fill in final product values |

---

## Part 6 — Recommended Execution Order

If working sequentially, this order maximizes unblocking:

```
1. GETTING_STARTED.md          (unlocks new contributors immediately)
2. CONTRIBUTING.md (expansion)  (sets the standard for everything else)
3. SELF_HOSTING.md             (unlocks operators / early adopters)
4. TESTING.md                  (required for any contributor PR)
5. CODEBASE_STRUCTURE.md (update)  (fixes the wrong mental map)
6. ARCHITECTURE.md (update)    (conceptual completeness)
7. DOMAIN.md (update)          (entity truth)
8. OUTBOX_PATTERN.md           (pattern docs for active feature)
9. DESIGN_SYSTEM.md            (front-end contributor reference)
10. FOOTER_MANAGEMENT.md       (feature doc for active work)
11. SECRETS.md                 (operator + contributor completeness)
12. ACCESSIBILITY.md (update)  (integration guide for open PR work)
13. API.md (update)            (endpoint reference completeness)
14. ADR-002 through ADR-005    (decision history)
15. CHANGELOG.md (update)      (release readiness)
16. Tier 4 maintenance items   (polish)
```

---

## Appendix A — Documentation Quality Standards (Reference)

When writing or updating any doc in this repo, follow these standards:

1. **ABOUTME header** — Two-line `ABOUTME:` comment at the top of every file (CLAUDE.md requirement)
2. **First paragraph = decision** — Lead with what the file covers; never with preamble
3. **Implemented vs Planned** — Always distinguish current code behavior from roadmap items; use `**Status:** Not yet implemented` for planned features
4. **Non-inferable only** — Don't document what can be read from code; document what is surprising, non-obvious, or has a why that isn't evident from the code
5. **Tables for structured data** — Configuration options, endpoint lists, entity fields always in tables
6. **No placeholder prose** — Every section must have real content; omit sections rather than writing "TBD"
7. **Cross-reference** — Add `## Related` section at bottom with links to companion docs
8. **Keep files focused** — If a doc grows beyond ~300 lines, consider splitting by concern

---

## Appendix B — Open-Source Self-Hosted Documentation Checklist

Based on best practices from projects like Gitea, Nextcloud, Mastodon/Akkoma, Discourse, and Matrix Synapse, a complete self-hosted open-source project needs:

| Category | Doc | Status |
|----------|-----|--------|
| Quick Start | `GETTING_STARTED.md` | ❌ Missing |
| Self-Hosting | `SELF_HOSTING.md` | ❌ Missing |
| Contributing | `CONTRIBUTING.md` | ⚠️ Too thin |
| Architecture | `ARCHITECTURE.md` | ✅ Present |
| API Reference | `API.md` + `swagger.json` | ✅ Present |
| Security Policy | `SECURITY.md` | ✅ Present |
| Configuration Reference | `CONFIGURATION.md` | ✅ Present |
| Changelog | `semantic_versioning/CHANGELOG.md` | ✅ Present |
| Testing Guide | `TESTING.md` | ❌ Missing |
| Upgrade Guide | In `SELF_HOSTING.md` | ❌ Missing |
| Troubleshooting | `TROUBLESHOOTING.md` | ✅ Present |
| Design System | `DESIGN_SYSTEM.md` | ❌ Missing |
| Accessibility | `ACCESSIBILITY.md` | ✅ Present |
| License | `LICENSE` (AGPL-3.0) | ✅ Present (assumed) |
| ADRs | `docs/adr/` | ⚠️ Only 1 ADR |
| Secret Management | `SECRETS.md` | ❌ Missing |
| Federation | `FEDERATION.md` | ✅ Present |

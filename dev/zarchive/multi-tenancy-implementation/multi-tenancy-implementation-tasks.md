# Multi-Tenancy Implementation - Task Checklist

> **Last Updated:** 2026-03-08 (v5 — self-review corrections)

---

## Phase 1: Single-Tenant Mode Polish ✅ COMPLETE

- [x] **1.1** Add deployment mode toggle API endpoint (`POST /api/instance/deployment-mode`)
  - Acceptance: ST→MT switch works; MT→ST validates tenant count == 1; cache invalidated
  - Effort: M | Skill: `cqrs-mediatr-guidelines`, `auth-patterns`

- [x] **1.2** Enhance `BlockInSingleTenantAttribute` for conditional route hiding
  - Acceptance: All multi-tenant-only endpoints return 404 in ST; default tenant CRUD still accessible
  - Effort: S

- [x] **1.3** Blazor single-tenant admin UX — hide all MT concepts
  - Acceptance: No "Instance Admin" nav; no "Tenants" section; only "Switch to MT" in tenant settings for instance admin
  - Effort: M | Skill: `blazor-ui-conventions`

- [x] **1.4** Root domain behavior: single-tenant → event list, no portal
  - Acceptance: `https://domain/` → event discovery page directly
  - Effort: S

- [x] **1.5** Instance mode indicator in admin UI
  - Acceptance: Badge shows "Single Tenant" or "Multi-Tenant" mode; contextual action link (Enable SaaS / Manage Platform)
  - Effort: S | Skill: `blazor-ui-conventions`

---

## Phase 2: Resolver Configuration & MT Activation 🟡 IN PROGRESS

- [x] **2.3** Tenant resolver configuration API (system-only settings)
  - `GET/PUT /api/instance/resolver-config`; stored in SystemSetting ONLY (never SettingsResolver cascade)
  - New keys: `routing.resolver_header_enabled`, `routing.resolver_subdomain_enabled`, `routing.resolver_custom_domain_enabled`, `routing.resolver_path_enabled`, `routing.path_prefix`
  - **Fixed priority:** header(1) → custom_domain(2) → subdomain(3) → path(4). Enable/disable per resolver — no reordering.
  - **Validation rules:** At least one resolver enabled; header resolver cannot be disabled (YARP depends on it)
  - Effort: M | Skill: `cqrs-mediatr-guidelines`

- [ ] **2.4** Implement ITenantResolver pipeline + Split TenantContext + TenantSlugCache + YARP propagation (IN PROGRESS)
  - Completed slice: shared `ITenantResolver` contract, shared `Explore.Infrastructure.Services.TenantResolverService`, and API-only `HeaderTenantResolver` wired through `Explore.API/Program.cs`
  - Completed slice: strict-architecture `ITenantLookupSource` + `ITenantSlugCache` contracts, persistence-backed `TenantLookupSource`, and shared `TenantSlugCache`
  - Corrected slice: Blazor path middleware now rewrites/extracts slug only, trusted `X-Tenant-Slug` forwarding is in place, and API-side `ApiTenantResolutionMiddleware` is now responsible for authoritative slug/host tenant resolution
  - Hardening slice: API output cache policies now vary by trusted tenant slug and forwarded host/host headers; stale runtime/docs comments were updated to match the corrected authority model
  - Hardening slice: unresolved multi-tenant `/api` requests now fail closed with `404`, and the remaining active docs were updated to remove the old default-tenant multi-tenant fallback story
  - **ITenantResolver interface:** `Name`, `Priority` (fixed), `ResolveAsync(HttpContext)` — resolvers in correct layers
  - **Blazor host:** routing convenience only (`/t/{slug}` extraction + rewrite + trusted slug forwarding)
  - **API authority:** tenant identity resolution from trusted slug/host context before tenant-scoped data access
  - **Shared infra in `Explore.Infrastructure`:** `TenantResolverService`, `TenantContextAccessor`, `TenantContext`, `TenantSlugCache`
  - **YARP fix:** Update `ForwardTenantHeader` to read from `HttpContext.Items["__resolved_tenant_id"]` and inject X-Tenant-Id
  - **TenantCircuitHandler:** Blazor Server circuit affinity — transfers tenant from HTTP context to circuit-scoped state
  - **TenantSlugCache:** In-memory `ConcurrentDictionary<string, Guid>` for slug→TenantId and domain→TenantId; populated at startup; updated on tenant CRUD
  - **Resolver telemetry:** Structured logs + OpenTelemetry metrics per resolution
  - Effort: XL | Skill: `clean-architecture-rules` | **Depends on:** 2.3

- [ ] **2.5** Path-based tenant resolver middleware (`/t/{slug}/...`) — **in Explore.Blazor**
  - **Only activates when path starts with `/t/`** (with trailing slash) — NO skip list needed
  - Extract slug, resolve via TenantSlugCache, rewrite path; stores in `HttpContext.Items` for YARP
  - Edge cases: `/t` → 404, `/t/` → 404; reserved slug blocklist: `admin`, `instance`, `api`, `auth`, `callback`
  - Effort: M (simplified by prefix-only approach) | **Depends on:** 2.4

- [ ] **2.7** TenantUrlBuilder service (centralized URL generation)
  - `ITenantUrlBuilder` interface in `Explore.Application` + `TenantUrlBuilder` impl in `Explore.Blazor.Client` (wraps NavigationManager)
  - `ServerTenantUrlBuilder` in `Explore.Infrastructure` for non-Blazor contexts (emails, API responses)
  - `BuildUrl("/events")` → `/t/{slug}/events` (path) or `https://{slug}.{domain}/events` (subdomain) or `/events` (single-tenant)
  - `BuildAbsoluteUrl()` for external links (emails, API responses)
  - Effort: M | **Depends on:** 2.3, 2.4

- [ ] **2.1** Multi-tenant activation confirmation wizard (multi-step dialog)
  - Steps: Confirmation → Resolver Selection (incl. path-based) → **Resolver Order Configuration** → Domain Config (if DNS resolvers) → DNS Guide → Activate
  - DNS verification is OPTIONAL — "Skip & Verify Later" prominently shown
  - Effort: L | Skill: `blazor-ui-conventions`, `blazor-css-isolation`

- [ ] **2.2** DNS setup guide component (reusable, provider-specific tabs)
  - Shows: Wildcard A record, CNAME for custom domains, SSL/TLS guidance, copy buttons
  - Only shown when subdomain or custom domain resolver is selected
  - Effort: M

- [ ] **2.6** DNS diagnostics page (`/instance/domains/diagnostics`)
  - Health status per resolver method; subdomain resolution check; SSL status; custom domain validity
  - **Extended diagnostics:** Certificate expiry warnings (<30 days), CNAME loop detection, wildcard coverage verification, HTTP→HTTPS redirect checks
  - Historical check results (last 7 days)
  - Manual re-check button; last check timestamp
  - Effort: L (upgraded from M due to extended diagnostics)

- [ ] **2.8** TenantGuardInterceptor (cross-tenant query safety)
  - EF Core `SaveChangesInterceptor`: throws on ITenantEntity save with `TenantId == Guid.Empty`
  - Defense-in-depth beyond EF query filters; registered in `ExploreDbContext`
  - Effort: S

- [ ] **2.9** Dynamic CORS for self-hosters
  - Replace static `Cors:AllowedOrigins` with `SetIsOriginAllowed()` delegate
  - Reads from: static config + base domain wildcard + tenant custom domains (TenantSlugCache) + `cors.additional_origins` key
  - BFF context: WASM→BFF same-origin (no CORS needed); only API direct access needs CORS
  - Effort: M | **Depends on:** 2.4 (TenantSlugCache)

---

## Phase 3: Platform Admin Dashboard ⏳ NOT STARTED

- [ ] **3.1** Platform admin layout & navigation (dashboard page)
  - Overview cards (tenants, users, events, storage), tenant list table, quick actions
  - Effort: L | Skill: `blazor-ui-conventions`

- [ ] **3.2** Enhanced tenant management page (full lifecycle UI)
  - Create, view details, suspend/unsuspend (with reason), archive, **delete (→ Deleting state)**, purge, **restore (→ Restoring state)**, status badges
  - **Transitional state indicators:** "Deleting..." and "Restoring..." with progress/spinner
  - Effort: L

- [ ] **3.3** Tenant lifecycle transition API (`POST /api/tenant/{id}/transition`)
  - Validates transition rules; creates `TenantLifecycleLog` entry; updates status
  - **Add `Deleting(6)` and `Restoring(7)` to `TenantStatusEnum`**
  - **Allowed transitions:** Provisioning→Active, Active→Suspended|Archived, Suspended→Active|Archived, Archived→Deleting|Restoring, Deleting→Purged (system-only), Restoring→Active (system-only)
  - Effort: M | Skill: `cqrs-mediatr-guidelines`

- [ ] **3.3a** EF Core migration for TenantStatusEnum new values
  - Add migration seeding `Deleting(6)` and `Restoring(7)` to the enum/lookup table
  - Verify PostgreSQL enum type if used, or int-backed column
  - Effort: S | **Depends on:** 3.3

- [ ] **3.4** Platform analytics API (`GET /api/instance/analytics/*`)
  - Overview + per-tenant breakdown incl. quota usage; instance admin only; `[BlockInSingleTenant]`
  - Effort: M

- [ ] **3.5** Tenant impersonation ("View as Tenant Admin")
  - `POST/DELETE /api/instance/impersonate/{tenantId}`; session-scoped; read-only by default; audit logged
  - Prominent "Impersonating: {Name}" banner with stop button
  - **Per-request audit:** `impersonation_user_id`, `impersonated_tenant_id`, `timestamp`, `action` logged for every request during impersonation
  - Effort: L | Skill: `auth-patterns`

- [ ] **3.6** Tenant quotas configuration (3-layer enforcement)
  - Governance keys: `tenants.default_max_events`, `tenants.default_max_storage_mb`, `tenants.default_max_members`
  - Per-tenant overrides via TenantSetting
  - **Layer 1:** Command handler enforcement (block creation when exceeded)
  - **Layer 2:** UI indicators (usage bars, warnings at >80%, disabled buttons at limit)
  - **Layer 3:** Background reconciliation job (periodic consistency verification)
  - Effort: XL (upgraded from L) | Skill: `cqrs-mediatr-guidelines`

---

## Phase 4: Settings Governance UI ⏳ NOT STARTED

- [ ] **4.3** Strongly-typed SettingDefinition registry (replaces attribute approach)
  - `SettingDefinition` class + `SettingRegistry` static class — no reflection, full type safety
  - Architecture test: every GovernanceSettingKeys constant MUST have a `SettingDefinition` in `SettingRegistry.All`
  - Effort: M

- [ ] **4.1** Settings governance page (instance admin)
  - Table grouped by category; lock/unlock toggles; value editing
  - **Show effective value:** 3-column display (System Default, Tenant Override, Effective Value)
  - **Search bar** + **filter toggles** (locked only, overridable only, tenant-visible only)
  - **Tenant context selector:** View per-tenant effective values
  - Effort: L | Skill: `blazor-ui-conventions`

- [ ] **4.2** Tenant settings page (tenant admin) — respects locks
  - Locked settings grayed out with "Set by instance admin"; unlocked editable; "Reset to default" button
  - Quota usage bars (if quotas enabled); search bar
  - Effort: M

---

## Phase 5: Tenant Provisioning Workflows ⏳ NOT STARTED

- [ ] **5.1** Enhance admin-created tenant flow (async provisioning)
  - Create tenant in Provisioning state → enqueue via `Channel<T>` → return immediately
  - **`TenantProvisioningService`** (`BackgroundService`): reads from channel, initializes storage, applies defaults, creates admin, validates DNS → transitions to Active
  - Retry with exponential backoff, 3 attempts max; dead-letter logging on final failure
  - Update `TenantSlugCache` on creation
  - If path resolver active: slug = path segment; preview shows `/t/{slug}/events`
  - **Uses `ITenantUrlBuilder`** for access URL generation
  - Effort: L (upgraded from M due to async provisioning)

- [ ] **5.2** Tenant self-registration API (`POST /api/tenant-registration`)
  - Public endpoint (gated by `tenants.self_service_registration`); creates tenant + admin member
  - Returns access URL(s) based on active resolver(s): subdomain URL and/or path URL (**via `TenantUrlBuilder`**)
  - Effort: L | Skill: `cqrs-mediatr-guidelines`

- [ ] **5.3** Tenant self-registration UI (instance portal)
  - "Create Organization" form with slug input, live availability check
  - **Dynamic preview** based on active resolver: subdomain preview AND/OR path preview
  - Effort: L | Skill: `blazor-ui-conventions`

- [ ] **5.4** Invite-based tenant creation (P2 — deferred)
  - Instance admin sends invite; user completes registration; admin approves
  - Effort: L

---

## Phase 6: Root Domain & Instance Portal ⏳ NOT STARTED

- [ ] **6.1** Instance portal landing page (`/` in multi-tenant mode)
  - Instance branding, login, "Create Org" (if enabled), "Powered by" footer
  - Effort: M

- [ ] **6.2** Tenant domain routing middleware enhancement
  - Base domain → portal (MT) or event list (ST); subdomain → tenant; path `/t/{slug}` → tenant; platform → instance admin
  - Effort: M

- [ ] **6.3** Instance admin context banner
  - Thin top banner when instance admin visits tenant domain; dismissible
  - Effort: S

---

## Phase 7: Multi-Tenant → Single-Tenant Revert ⏳ NOT STARTED

- [ ] **7.1** Revert validation & API extension
  - MT→ST requires tenant count == 1; clears resolver configs; invalidates cache
  - Effort: S

- [ ] **7.2** Revert confirmation UI
  - Pre-check tenant count; show tenant list if >1; confirmation dialog if 1
  - Effort: M

---

## Phase 8: Testing & Documentation ⏳ NOT STARTED

- [ ] **8.1** Unit tests (Application layer)
  - TransitionTenantStatusCommand (**incl. Deleting/Restoring states**), DeploymentMode switch, SettingRegistry validation, TenantRegistration, QuotaEnforcement, **TenantUrlBuilder**, **ITenantResolver implementations**, **TenantSlugCache**, **ProvisionTenantJob**
  - Effort: XL (upgraded from L)

- [ ] **8.2** Integration tests (API layer)
  - Mode switch, lifecycle transitions (**full state machine incl. Deleting/Restoring**), settings cascade, BlockInSingleTenant, registration endpoint, path resolver URL rewriting, impersonation (**audit log verification**), **quota enforcement (create until hit)**, **all ITenantResolver resolvers (subdomain, path, header, custom domain)**, **TenantGuardInterceptor blocks missing TenantId**, **dynamic CORS allows configured origins**, **async provisioning workflow (Provisioning → Active)**
  - Effort: XL (upgraded from L)

- [ ] **8.3** Architecture tests
  - ITenantEntity checks, every GovernanceSettingKeys constant has matching `SettingDefinition` in `SettingRegistry.All`, endpoint attribute coverage, reserved slug blocklist, **TenantSlugCache invalidation on tenant updates**
  - **Cross-tenant protection:** Verify `IgnoreQueryFilters()` only used in instance-admin contexts
  - **Quota gate presence:** Architecture test verifying quota-relevant commands include quota check
  - Effort: M (upgraded from S)

- [ ] **8.4** Routing & resolver tests
  - Path resolver: `/t/islamu/events` resolves + strips prefix
  - Subdomain resolver: `islamu.events.example.org/events` resolves
  - Header resolver: `X-Tenant-Id: {guid}` resolves
  - Custom domain resolver: `events.islamu.org` resolves
  - Edge cases: reserved slug rejection, `/_blazor` skipping, `/_framework` skipping, `/api/*` not path-resolved
  - Enable/disable toggles: test that per-resolver enable/disable correctly includes/excludes resolvers
  - TenantUrlBuilder: generated URLs match active resolver mode
  - Effort: M

- [ ] **8.5** Documentation updates
  - MULTI_TENANCY.md, CONFIGURATION.md, OPERATIONS.md — resolver config (incl. path resolver + enable/disable toggles), provisioning, DNS guide, quotas (3-layer), impersonation, **TenantUrlBuilder usage**, **lifecycle state machine**, **resolver telemetry fields**, **two-service resolver architecture**
  - Effort: M

---

## Summary

| Phase | Tasks | Effort | Priority |
|-------|-------|--------|----------|
| 1. Single-Tenant Polish | 5 | S-M | P0 |
| 2. Resolver Config & MT Activation | 9 | M-XL | P0 |
| 3. Platform Admin Dashboard | 7 | S-XL | P0 |
| 4. Settings Governance UI | 3 | M-L | P0 |
| 5. Tenant Provisioning | 4 | M-L | P1 |
| 6. Instance Portal | 3 | S-M | P1 |
| 7. MT→ST Revert | 2 | S-M | P1 |
| 8. Testing & Docs | 5 | M-L | P0 |
| **Total** | **38 tasks** | | |

---

## Recommended Implementation Order

1. Phase 1 (foundation — everything depends on clean ST mode)
2. Phase 2.3 + 2.4 (resolver config + ITenantResolver pipeline + Split TenantContext + TenantSlugCache + YARP propagation + circuit affinity + telemetry)
3. Phase 2.5 + 2.7 (path resolver middleware `/t/` prefix-only in Blazor + TenantUrlBuilder)
4. Phase 2.8 (TenantGuardInterceptor — cross-tenant safety)
5. Phase 2.9 (Dynamic CORS — self-hoster support)
6. Phase 2.1 + 2.2 + 2.6 (activation wizard + DNS guide + diagnostics)
7. Phase 4.3 (SettingDefinition registry — powers governance UI + quota enforcement)
8. Phase 3.3 + 3.3a + 3.6 (lifecycle API with transitional states + EF migration + quotas 3-layer)
9. Phase 3.1 + 3.2 + 3.4 (platform admin dashboard)
10. Phase 3.5 (tenant impersonation)
11. Phase 4.1 + 4.2 (governance UI with effective value display)
12. Phase 6 (portal — completes the MT experience)
13. Phase 5.1 + 5.2 + 5.3 (async provisioning workflows using BackgroundService + Channel<T>)
14. Phase 7 (revert — safety net)
15. Phase 8 (testing & docs — routing tests, CORS tests, circuit tests — formalize throughout each phase)
16. Phase 5.4 (invite-based — P2 deferred)

# Multi-Tenancy Implementation - Context

> **Last Updated:** 2026-03-08 (v5 — self-review corrections)

## SESSION PROGRESS (2026-03-08)

### ✅ COMPLETED
- Comprehensive codebase analysis of existing multi-tenancy infrastructure
- Deep research on multi-tenancy patterns (Finbuckle, GitLab, Keycloak, Sentry, Supabase)
- Research on DNS setup, settings cascade, tenant provisioning best practices
- Research on middleware ordering, URL generation, lifecycle state machines, resolver telemetry
- Research on dynamic CORS patterns, ITenantResolver extensible pipelines, TenantSlugCache
- Created comprehensive implementation plan (`multi-tenancy-implementation-plan.md`) v4
- Created task checklist (`multi-tenancy-implementation-tasks.md`) v4
- Incorporated 3 rounds of enterprise-grade CTO feedback
- Phase 1.1 complete: added `POST /api/instanceonboarding/deployment-mode` endpoint in `Explore.API/Controllers/InstanceOnboardingController.cs`
- Phase 1.2 complete: `BlockInSingleTenantAttribute` and `RequireMultiTenantAttribute` now resolve runtime deployment mode from `SystemSetting` before falling back to static config
- Phase 1.3 complete: single-tenant UI hiding implemented in `Explore.Blazor.Client/Layout/NavMenu.razor` and `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantAdminSettingsLayout.razor`
- Phase 1.4 complete: root `/` now always resolves to public event discovery in single-tenant mode via `Explore.Blazor.Client/Services/StartupRoutingService.cs`
- Phase 1.5 complete: deployment-mode badge and contextual action added to tenant and instance admin settings layouts

### 🟡 IN PROGRESS
- Phase 2.4: implementing the shared tenant resolver pipeline, split tenant context, YARP propagation, and tenant slug cache foundation

### ✅ RECENTLY COMPLETED
- Added fail-closed multi-tenant behavior in `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`; unresolved `/api` requests now return `404` instead of drifting into default-tenant behavior
- Updated remaining active docs (`docs/QUICK_REFERENCE.md`, `docs/TROUBLESHOOTING.md`, `docs/DEPLOYMENT_MODES.md`, `docs/CODEBASE_INSIGHTS.md`) to describe trusted slug/host authority and fail-closed multi-tenant resolution
- Hardened API output caching in `Explore.API/Program.cs` so read-cache policies now vary by trusted tenant slug and forwarded host/host headers
- Updated stale authority narrative in `docs/BLAZOR.md`, `Explore.API/Services/HeaderTenantResolver.cs`, `Explore.Blazor/Services/BlazorHeaderTenantResolver.cs`, and `Explore.API/Services/TenantContext.cs` to reflect the API-authoritative model without deleting legacy files
- Corrected the authority split: Blazor path middleware now rewrites/extracts slug only, and API tenant resolution moved back toward API-authoritative handling
- Added `Explore.Application/Constants/TenantHeaderNames.cs` for shared `X-Tenant-Id` / `X-Tenant-Slug` header names
- Added `Explore.Blazor/Services/ITenantRouteContextAccessor.cs` + `Explore.Blazor/Services/TenantRouteContextAccessor.cs` for lightweight route slug storage only
- Refactored `Explore.Blazor/Middleware/PathTenantResolverMiddleware.cs` to store tenant slug and rewrite path without cache lookup or tenant ID resolution
- Refactored `Explore.Blazor/Extensions/YarpProxyExtensions.cs` and `Explore.Blazor/Services/CircuitAccessTokenService.cs` to forward trusted tenant slug instead of resolved tenant ID
- Added `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs` so the API can authoritatively resolve tenant identity from trusted slug and host context before application/data access
- Updated Blazor service registration to remove tenant-authoritative cache/resolver wiring while keeping only route-context and resolver-config support
- Verified the architecture correction slice with clean LSP diagnostics and a successful Release solution build
- Added strict-architecture tenant lookup seam with `ITenantLookupSource`, `ITenantSlugCache`, and `TenantLookupRecord`
- Added persistence-backed `Explore.Persistence/Services/TenantLookupSource.cs` to load active tenant slug/domain data without introducing an `Infrastructure -> Persistence` dependency
- Added shared `Explore.Infrastructure/Services/TenantSlugCache.cs` with lazy warm/refresh behavior backed by the new application contract
- Registered `ITenantLookupSource` in `Explore.Persistence/PersistenceServicesRegistration.cs` and `ITenantSlugCache` in `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- Verified the strict-architecture TenantSlugCache slice with clean LSP diagnostics and a successful Release solution build
- Added Blazor-side header resolver in `Explore.Blazor/Services/BlazorHeaderTenantResolver.cs`
- Added Blazor request-level tenant resolution middleware in `Explore.Blazor/Extensions/MiddlewareExtensions.cs` and wired it in `Explore.Blazor/Program.cs`
- Added Blazor Server circuit tenant persistence in `Explore.Blazor/Services/TenantCircuitHandler.cs`
- Updated `Explore.Infrastructure/Services/TenantContextAccessor.cs` to keep tenant context available outside direct `HttpContext.Items` access for circuit-scoped usage
- Updated `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs` to register the shared resolver pipeline on the Blazor host
- Verified the Blazor-side Phase 2.4 slice with clean LSP diagnostics on changed files and a successful Release solution build
- Added shared `ITenantResolver` contract in `Explore.Application/Contracts/Services/ITenantResolver.cs`
- Added shared pipeline-oriented `Explore.Infrastructure/Services/TenantResolverService.cs`
- Refactored the API-specific resolver into `Explore.API/Services/HeaderTenantResolver.cs` so the API host only resolves from `X-Tenant-Id`
- Updated `Explore.API/Program.cs` to register `ITenantResolver` + shared `ITenantResolverService` orchestration
- Verified the new Phase 2.4 slice with clean LSP diagnostics on touched files and a successful Release solution build
- Phase 2.3 complete: added system-only resolver configuration API/service backed by `SystemSetting`
- Added resolver configuration endpoints to `Explore.API/Controllers/InstanceOnboardingController.cs`
- Added `IResolverConfigService` + `ResolverConfigService` with 5-minute memory caching and explicit invalidation
- Added `ResolverConfigurationDto` + `ResolverConfigurationDtoValidator`
- Added `GetResolverConfigurationQuery` / `GetResolverConfigurationQueryHandler`
- Added `UpdateResolverConfigurationCommand` / `UpdateResolverConfigurationCommandHandler`
- Added routing governance keys for header, subdomain, custom domain, path resolver toggles, and path prefix
- Verified Phase 2.3 with clean LSP diagnostics on changed files and a successful `dotnet build --configuration Release --verbosity quiet`

### ⚠️ BLOCKERS
- `Explore.Blazor.Client.Tests` still has 3 pre-existing failing UI tests (`Header_RendersAuthenticatedUserSection_WhenUserIsAuthenticated`, `CreateEvent_OnLastStep_ShowsCreateButton`, `CreateEvent_Initially_ShowsNextButton_AndHidesCreateButton`) unrelated to the resolver slice; Release solution build is green
- Remaining hardening after this slice: decide whether to delete the now-unwired Blazor resolver prototypes when deletion is allowed, and consider replacing tenant header-based cache variation with resolved-tenant-ID variation if direct non-BFF API access becomes a supported scenario

---

## Key Files

### Domain Layer
| File | Purpose |
|------|---------|
| `Explore.Domain/Tenant.cs` | Core tenant entity (Id, FullName, Slug, TenantStatusId, IsActive) |
| `Explore.Domain/TenantSetting.cs` | Tenant-specific setting overrides (TenantId, SettingKey, Value) |
| `Explore.Domain/TenantMember.cs` | User-tenant association with role (UserId, TenantId, RoleId) |
| `Explore.Domain/TenantLifecycleLog.cs` | Audit trail for status transitions (OldStatusId, NewStatusId, Reason) |
| `Explore.Domain/TenantStatus.cs` | Lookup entity (MasterCode, FullName, IsActiveState) |
| `Explore.Domain/TenantNavigationLink.cs` | Per-tenant nav customization |
| `Explore.Domain/TenantInvitation.cs` | Invitation-based tenant provisioning entity |
| `Explore.Domain/Enums/TenantStatusEnum.cs` | Provisioning=1, Active=2, Suspended=3, Archived=4, Purged=5, **Deleting=6, Restoring=7** |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | All governance setting key constants (deployment.*, tenants.*, domains.*, routing.*) |
| `Explore.Domain/SystemSetting.cs` | System-level settings (Key, Value, IsLocked, DefaultValue) |

### Infrastructure Layer
| File | Purpose |
|------|---------|
| `Explore.Infrastructure/DeploymentSettings.cs` | Static config: Mode, DefaultTenantId, HidePlatformAdminInSingleTenant, DefaultTenantSubdomain |
| `Explore.Infrastructure/Services/SettingsResolver.cs` | 2-tier cascade: System → Tenant (with lock check) |
| `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs` | 5-tier cascade: Instance → Tenant → Org → Group → User |

### API Layer
| File | Purpose |
|------|---------|
| `Explore.API/Services/TenantContext.cs` | Legacy monolithic tenant resolution implementation retained in the repo but no longer used by DI; target behavior now flows through shared `Explore.Infrastructure.Services.TenantContext` + shared `TenantResolverService` + API-only `HeaderTenantResolver` |
| `Explore.API/Services/HeaderTenantResolver.cs` | API-only header resolver implementing `ITenantResolver`; reads forwarded `X-Tenant-Id` only |
| `Explore.API/Controllers/TenantController.cs` | Full CRUD for tenants (list, get, count, create, update, delete) |
| `Explore.API/Controllers/InstanceOnboardingController.cs` | Instance setup (status, settings, complete, storage, SMTP, auth) |
| `Explore.API/Controllers/TenantOnboardingController.cs` | Tenant setup (status, settings, complete, save-step) |
| `Explore.API/Filters/BlockInSingleTenantAttribute.cs` | Returns 404/403 for endpoints hidden in single-tenant mode |

### New Files (to be created)
| File | Purpose |
|------|---------|
| `Explore.Application/Contracts/Services/ITenantResolver.cs` | Interface for extensible tenant resolver pipeline (**created**) |
| `Explore.Application/Contracts/Services/ITenantContextAccessor.cs` | Per-request tenant storage interface (like IHttpContextAccessor) |
| `Explore.Application/Contracts/Services/ITenantLookupSource.cs` | Strict-architecture data-loading contract for tenant lookup cache (**created**) |
| `Explore.Application/Contracts/Services/ITenantSlugCache.cs` | Shared cache contract for slug/domain-based resolvers (**created**) |
| `Explore.Application/Contracts/Services/ITenantUrlBuilder.cs` | Interface for centralized tenant-aware URL generation |
| `Explore.Application/Models/Tenants/TenantLookupRecord.cs` | Application-layer data shape for tenant lookup cache hydration (**created**) |
| `Explore.Infrastructure/Services/TenantResolverService.cs` | Orchestrates ITenantResolver pipeline (shared — registered in API now, later in Blazor too) |
| `Explore.Infrastructure/Services/TenantContextAccessor.cs` | Stores per-request resolved tenant via HttpContext.Items (shared) |
| `Explore.Infrastructure/Services/TenantContext.cs` | Read-only tenant exposure for consumers (moved from API, now shared) |
| `Explore.Infrastructure/Services/TenantSlugCache.cs` | In-memory ConcurrentDictionary slug→TenantId and domain→TenantId cache (**created**) |
| `Explore.Blazor/Middleware/PathTenantResolverMiddleware.cs` | Path-based tenant slug extraction + path rewrite only (`/t/{slug}/...` → strip prefix) — Blazor Web App |
| `Explore.Blazor/Services/ITenantRouteContextAccessor.cs` | Lightweight Blazor route-context contract for current tenant slug (**created**) |
| `Explore.Blazor/Services/TenantRouteContextAccessor.cs` | Scoped + HttpContext-backed storage for current tenant slug (**created**) |
| `Explore.Blazor/Services/Resolvers/SubdomainTenantResolver.cs` | Earlier Blazor-side host resolver prototype, now unwired after architecture correction |
| `Explore.Blazor/Services/Resolvers/CustomDomainTenantResolver.cs` | Earlier Blazor-side host resolver prototype, now unwired after architecture correction |
| `Explore.Blazor/Services/BlazorHeaderTenantResolver.cs` | Earlier Blazor-side header resolver prototype, now unwired after the architecture correction |
| `Explore.Blazor/Services/TenantCircuitHandler.cs` | CircuitHandler that preserves tenant slug route context across Blazor Server circuit lifetime (**refactored**) |
| `Explore.API/Services/HeaderTenantResolver.cs` | Earlier API header-ID resolver retained in the repo but no longer registered for standard routing |
| `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs` | API-authoritative async tenant resolution from trusted slug and forwarded host hints (**created**) |
| `Explore.Blazor.Client/Services/TenantUrlBuilder.cs` | Blazor implementation wrapping NavigationManager for tenant-aware navigation |
| `Explore.Infrastructure/Services/ServerTenantUrlBuilder.cs` | Non-Blazor URL generation (emails, API responses) from config |
| `Explore.Persistence/Interceptors/TenantGuardInterceptor.cs` | EF Core SaveChangesInterceptor — verifies TenantId set on all ITenantEntity |
| `Explore.Domain/Settings/SettingDefinition.cs` | Strongly-typed setting definition (Key, DisplayName, Category, etc.) |
| `Explore.Domain/Settings/SettingRegistry.cs` | Central registry of ALL SettingDefinition objects — no reflection |
| `Explore.Domain/Settings/SettingCategory.cs` | Enum for setting categories (Quotas, Deployment, Events, etc.) |
| `Explore.Domain/Settings/SettingValueType.cs` | Enum for value types (String, Int, Bool, etc.) |
| `Explore.Infrastructure/Services/TenantProvisioningService.cs` | BackgroundService with Channel&lt;T&gt; queue for async tenant provisioning |
| `Explore.API/Controllers/TenantImpersonationController.cs` | Instance admin tenant impersonation |
| `Explore.API/Controllers/PlatformAnalyticsController.cs` | Instance-level analytics endpoints |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/DnsDiagnosticsSection.razor` | DNS health diagnostics page |
| `Explore.Persistence/Services/TenantLookupSource.cs` | Persistence implementation of `ITenantLookupSource` using active tenants plus tenant domain settings (**created**) |

### Blazor Layer
| File | Purpose |
|------|---------|
| `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor` | Instance admin settings page (route: `/admin/instance/settings`) |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceTenantsSection.razor` | Tenant list & management |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor` | Governance settings with deployment mode |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceDomainSection.razor` | Base domain, custom domain settings |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceBrandingSection.razor` | Display name, logo, favicon |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceStorageSection.razor` | S3 settings |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSmtpSection.razor` | Email SMTP configuration |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAuthProviderSection.razor` | Auth provider config |
| `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor` | Tenant admin settings (route: `/admin/tenant/settings`) |
| `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantBrandingSection.razor` | Tenant branding override |
| `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantDomainSection.razor` | Tenant subdomain/custom domain |
| `Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantRenderPolicySection.razor` | Render policy override |

### Application Layer (MediatR Commands/Queries)
| Feature | Commands | Queries |
|---------|----------|---------|
| Tenant CRUD | `CreateTenantCommand`, `UpdateTenantCommand`, `DeleteTenantCommand` | `GetTenantListRequest`, `GetTenantDetailsRequest`, `GetActiveTenantCountQuery` |
| Tenant Members | `CreateTenantMemberCommand`, `UpdateTenantMemberCommand`, `DeleteTenantMemberCommand` | - |
| Tenant Nav Links | `CreateTenantNavLinkCommand`, `UpdateTenantNavLinkCommand`, `DeleteTenantNavLinkCommand`, `ReorderTenantNavLinksCommand` | `GetTenantNavLinksQuery` |

### Documentation
| File | Purpose |
|------|---------|
| `docs/MULTI_TENANCY.md` | Deployment modes, tenant resolution, data isolation, override model |
| `docs/ADMIN_HIERARCHY.md` | 4-tier admin roles, authority boundaries, delegation model |
| `docs/CONFIGURATION.md` | Configuration sources, governance settings, deployment mode config |
| `docs/SECURITY.md` | BFF model, JWT auth, authorization boundary, admin claims enrichment |
| `docs/RENDER_POLICIES.md` | Render policy delegation per route group |
| `docs/OPERATIONS.md` | Startup order, rate limiting, health checks, tenant metrics |

---

## Important Decisions Made

1. **Single Blazor App:** All control planes (instance, tenant, public) run in one Blazor project — no separate apps needed. Access controlled by roles and route authorization.

2. **No Marketing Site Dependency:** The platform does NOT bundle or require a marketing site. In single-tenant mode, root domain → event list. In multi-tenant mode, root domain → minimal instance portal.

3. **Resolver Configuration at Runtime:** Instance admin selects which tenant resolution methods are active (subdomain, path, custom domain, header). Stored in **system settings only** (tenant-independent).

4. **⚠️ Resolver Config = System-Only:** `ResolverConfigService` reads ONLY from `SystemSetting` table — never via `SettingsResolver` cascade. This avoids the circular dependency: TenantContext → needs settings, SettingsResolver → needs tenant.

5. **Path-Based Resolver (`/t/{slug}/...`):** DNS-free tenant resolution via URL path prefix. Middleware strips `/t/{slug}` from request path before downstream routing. No DNS configuration needed — simplest deployment option.

6. **DNS Verification is Optional:** The activation wizard does NOT block on DNS verification. Instead, a DNS diagnostics page (`/instance/domains/diagnostics`) provides ongoing health checks.

7. **~~Attribute-Based Settings Metadata~~ (SUPERSEDED by Decision 27):** ~~`[SettingMetadata]` attributes on `GovernanceSettingKeys` constants, discovered via reflection at startup.~~ **Replaced by strongly-typed `SettingDefinition` registry — no reflection.**

8. **Tenant Impersonation:** Instance admin can "view as tenant admin" via session-scoped context. Read-only by default, fully audited. Impersonation context stored in session, not JWT.

9. **Tenant Quotas (3-Layer Enforcement):** `max_events`, `max_storage_mb`, `max_members` — configurable defaults at instance level, per-tenant overrides via `TenantSetting`. **Layer 1:** Command handlers block creation. **Layer 2:** UI indicators prevent attempts at limit. **Layer 3:** Background reconciliation job verifies consistency.

10. **DNS Guide is In-App:** Rather than external docs, the DNS setup guide is embedded in the multi-tenant activation wizard with provider-specific examples. Only shown when subdomain/custom-domain resolver is selected.

11. **~~Settings Metadata Registry (Attribute-Driven)~~ (SUPERSEDED by Decision 27):** ~~A reflection-based registry maps every governance key to display name, type, category, and lockability.~~ **Replaced by `SettingDefinition` objects in `SettingRegistry` — no reflection.**

12. **No Backwards Compatibility Required:** The app is in development — clean breaks are allowed for the multi-tenancy implementation.

13. **Default Tenant ID:** `018e4e5c-7f00-7000-8000-000000000001` — hardcoded fallback in `TenantContext`.

14. **Instance Mode Indicator:** Always-visible badge in admin UI showing "Single Tenant" or "Multi-Tenant" with contextual action link.

15. **~~Configurable Resolver Order~~ (SIMPLIFIED in v5):** ~~`routing.tenant_resolver_order` governance key stores comma-separated resolver priority.~~ **Replaced by fixed priority (header→custom_domain→subdomain→path) with per-resolver enable/disable toggles.** Configurable order adds complexity for a feature almost no one would use.

16. **TenantUrlBuilder Service:** Centralized URL generation (`ITenantUrlBuilder`) that respects the active resolver method. **Interface in `Explore.Application`; implementation in `Explore.Blazor.Client`** (wraps `NavigationManager`). A `ServerTenantUrlBuilder` in Infrastructure handles non-Blazor contexts (emails, API responses).

17. **Transitional Lifecycle States:** `TenantStatusEnum` extended with `Deleting(6)` and `Restoring(7)` — prevents race conditions during async purge/restore operations. `Deleting → Purged` and `Restoring → Active` are system-only transitions triggered after background jobs complete.

18. **Resolver Telemetry:** Every tenant resolution emits structured logs via Serilog + OpenTelemetry: `tenant.resolution.method`, `tenant.resolution.slug`, `tenant.resolution.host`, `tenant.resolution.path`, `tenant.resolution.duration_ms`, `tenant.resolution.cache_hit`. Uses existing `BusinessMetrics` meter for counters.

19. **Organization ≠ Tenant:** Organizations are actors within a tenant that post events. A tenant can contain many organizations. Do NOT confuse org management with tenant management — they are separate domain concepts.

20. **Show Effective Value in Governance UI:** Settings governance page displays 3 columns: System Default, Tenant Override, Effective Value — helps instance admins understand the cascade at a glance.

21. **Cross-Tenant Protection:** Architecture tests enforce that `IgnoreQueryFilters()` is only used in instance-admin contexts. Every repository query for tenant data must use EF query filters.

22. **Middleware Ordering for Path Resolver:** `UsePathBase → PathTenantResolverMiddleware → UseStaticFiles → UseRouting → UseAuth → MapBlazorHub`. Path resolver only activates on `/t/` prefix — no skip list needed.

23. **ITenantResolver Extensible Pipeline:** Interface-based resolver contributors (`SubdomainTenantResolver`, `PathTenantResolver`, `HeaderTenantResolver`, `CustomDomainTenantResolver`). Pipeline iterates in configured priority order; first match wins. Enables future cookie/query-string/JWT-claim/API-key resolvers without touching existing code.

24. **Split TenantContext (3 Components):** `TenantResolverService` (determines tenant via ITenantResolver pipeline), `TenantContextAccessor` (stores per-request tenant like IHttpContextAccessor), `TenantContext` (read-only exposure for consumers). Makes testing much easier — mock `TenantContextAccessor` without full HTTP pipeline.

25. **TenantSlugCache (In-Memory):** `ConcurrentDictionary<string, Guid>` for slug→TenantId and domain→TenantId. Populated at startup, updated on tenant CRUD events. Zero DB queries for tenant resolution on cache hit. Used by all ITenantResolver implementations.

26. **TenantGuardInterceptor:** EF Core `SaveChangesInterceptor` that throws on tenant-scoped entity saves with missing TenantId. Defense-in-depth beyond EF query filters. Catches accidental writes without tenant context.

27. **Strongly-Typed SettingDefinition Registry (Replaces Attributes):** `SettingDefinition` objects registered centrally in `SettingRegistry`. No reflection. Full type safety. Compile-time errors for missing definitions. Architecture test enforces every key has a definition. Pattern used by Keycloak, Elasticsearch, Spring Boot.

28. **Path Resolver `/t/` Prefix Only:** Only activate tenant resolution when path starts with `/t/` (with trailing slash). NO skip list for `/_framework`, `/_blazor`, `/api/*`, `/health`, etc. Everything NOT starting with `/t/` passes through untouched. `/t` namespace reserved exclusively for tenant routing.

29. **Dynamic CORS for Self-Hosters:** `SetIsOriginAllowed()` delegate reads from static config + base domain wildcard + tenant custom domains (from TenantSlugCache). BFF architecture means WASM→Blazor is same-origin (no CORS needed); only API needs CORS for direct consumers.

30. **Async Tenant Provisioning:** `CreateTenantCommand` → Provisioning state → `ProvisionTenantJob` (background) → Active. Avoids slow HTTP responses. Tenant appears immediately with "Provisioning..." status.

31. **BFF/YARP Architecture:** Blazor Web and ASP.NET API are SEPARATE services. WASM HttpClient points to BFF (same-origin), YARP proxies `/api/{**catchall}` to API with bearer token + tenant header + setup secret forwarding. No `/api` path in production — separate domains (e.g., `events.ngo`, `api.ngo`).

32. **Tenant Access Context Pattern (Future):** User belongs to multiple tenants, selects active tenant from picker, session stores `activeTenantId`. Used by Sentry/Supabase/Notion. System already supports header resolver, so this can be added later.

33. **Two-Service Resolver Placement (v5):** Path/subdomain/custom-domain resolvers live in `Explore.Blazor` (browser-facing). API only has header resolver (reads X-Tenant-Id set by YARP). Shared infrastructure (`TenantSlugCache`, `TenantContextAccessor`, `TenantContext`) lives in `Explore.Infrastructure`.

34. **YARP Tenant Propagation (v5):** `ForwardTenantHeader` updated to read resolved tenant from `HttpContext.Items["__resolved_tenant_id"]` (set by Blazor resolver middleware) and inject as `X-Tenant-Id` header on outgoing proxy request. Falls back to incoming header for Tenant Access Context pattern.

35. **Blazor Server Circuit Affinity (v5):** `TenantCircuitHandler` (extends `CircuitHandler`) transfers resolved tenant from initial HTTP context to circuit-scoped state. Ensures SignalR interactions retain correct tenant context across circuit lifetime and reconnections.

36. **TenantUrlBuilder Layer Split (v5):** `ITenantUrlBuilder` interface in `Explore.Application` (pure URL generation). `TenantUrlBuilder` implementation in `Explore.Blazor.Client` (wraps `NavigationManager`). `ServerTenantUrlBuilder` in `Explore.Infrastructure` for non-Blazor contexts (emails, API responses).

37. **Fixed Resolver Priority (v5):** Resolver order is fixed: header(1) → custom_domain(2) → subdomain(3) → path(4). Each resolver can be enabled/disabled via individual governance keys. Configurable ordering removed — adds complexity for a feature almost no one uses.

38. **BackgroundService for Async Jobs (v5):** Async tenant provisioning (and Deleting/Restoring transitions) uses `BackgroundService` with `Channel<T>` queue — Aspire-native, no Hangfire dependency. Retry with exponential backoff, 3 attempts max.

---

## Key Technical Constraints

1. **EF Core query filters are central** — never bypass `Tenant` or `SoftDelete` named filters except through explicit `IgnoreQueryFilters()` in cross-tenant admin queries.

2. **Validators are manually instantiated** — no DI for FluentValidation.

3. **Repositories return entities** — never DTOs. Mapping in handlers.

4. **Commands return `BaseCommandResponse<Guid>`** — follow this pattern for new commands.

5. **Cache TTL is 5 minutes** — governance settings changes take up to 5 min to propagate (or use explicit cache invalidation).

6. **`X-Setup-Secret` header is stripped and re-injected** by YARP — cannot be client-injected.

7. **Rate limiting disabled in Testing environment** — integration tests run without rate limits.

---

## Quick Resume

To continue implementation:
1. Read this file for current state
2. Read `multi-tenancy-implementation-tasks.md` for the task checklist
3. Read `multi-tenancy-implementation-plan.md` for detailed architecture
4. Start with Phase 1 (Single-Tenant Mode Polish) — it's the foundation
5. Build and test: `dotnet build --configuration Release --verbosity quiet`

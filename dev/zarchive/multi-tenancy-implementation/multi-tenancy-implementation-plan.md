# Multi-Tenancy Full Implementation Plan

> **Last Updated:** 2026-03-08 (v5 — self-review corrections: layer placement, YARP propagation, circuit affinity)
>
> Comprehensive plan for completing the single-tenant ↔ multi-tenant runtime switch,
> instance administration control plane, tenant provisioning, DNS-guided setup,
> settings cascade governance, tenant quotas, impersonation, resolver telemetry,
> extensible resolver pipeline (ITenantResolver), TenantSlugCache, split TenantContext,
> TenantGuardInterceptor, strongly-typed SettingDefinition registry, async provisioning,
> dynamic CORS for self-hosters, and BFF/YARP architecture integration.
>
> **v5 critical corrections:**
> - Resolver code placed in correct layers (Blazor Web for path/subdomain, API for header only)
> - YARP tenant propagation gap resolved (Blazor middleware → HttpContext.Items → YARP transform)
> - Blazor Server circuit tenant affinity documented
> - TenantUrlBuilder implementation moved to Blazor layer
> - Background job infrastructure committed to `BackgroundService` (Aspire-native)
> - EF Core migration task added for new TenantStatusEnum values
> - Superseded decisions marked in context.md
>
> **Clarification:** In this project, "Organization" is an actor within a tenant that posts events.
> Organization ≠ Tenant. A tenant can contain many organizations.

---

## Executive Summary

ISLAMU Event already has significant multi-tenancy infrastructure in place:
tenant entity model, `TenantContext` resolution (header → custom domain → subdomain → default),
`SettingsResolver` + `HierarchicalSettingsResolver` cascade, `DeploymentSettings` mode switch,
EF Core named query filters for tenant isolation, `BlockInSingleTenantAttribute`,
instance/tenant onboarding controllers, Blazor admin pages for both instance and tenant settings,
and a YARP BFF proxy layer (Blazor → API) that handles token forwarding and tenant header injection.

**What remains** is completing the end-to-end user experience for:

1. **Single-tenant mode**: App functions as if multi-tenancy doesn't exist. Instance admin sees a hidden "Switch to Multi-Tenant" section in tenant settings. No platform admin page, no subdomain resolution, no tenant list — just a normal app with one tenant. An "Instance Mode: Single Tenant" indicator is shown in admin UI.

2. **Multi-tenant activation flow**: Confirmation screen with DNS setup wizard (verification optional — diagnostics page instead), resolver method selection (subdomain, path-based `/t/{slug}/`, header, custom domain), wildcard DNS instructions, and guided configuration.

3. **Platform Admin dashboard** (multi-tenant only): Dedicated instance-level control plane for managing tenants, governance settings (lock/unlock/override), analytics, tenant quotas (max events/storage/members), tenant impersonation, and platform-wide configuration.

4. **Tenant Admin experience**: Tenant-scoped settings that respect lock/override policies set by instance admin. Quota usage indicators.

5. **Tenant provisioning**: Admin-created (with async background provisioning), self-registration, and invite-based models. When path-based resolver is active, tenant onboarding includes custom path selection (e.g., `/t/islamu/`).

6. **Settings governance UI**: Full CRUD for lockable/overridable settings with visual lock indicators, search, and category filtering. Strongly-typed `SettingDefinition` registry — no reflection, full type safety.

7. **Extensible tenant resolution**: `ITenantResolver` interface-based pipeline with in-memory `TenantSlugCache`, split `TenantContext` responsibilities (resolver → accessor → context), and `TenantGuardInterceptor` for cross-tenant query safety.

8. **Dynamic CORS for self-hosters**: `SetIsOriginAllowed()` delegate reading allowed origins from database at runtime, supporting arbitrary self-hoster domains and tenant custom domains. BFF architecture means WASM→Blazor is same-origin (no CORS needed); only the API needs CORS for direct consumers.

---

## Current State Analysis

### ✅ Already Implemented (Verified in Codebase)

| Component | Location | Status |
|-----------|----------|--------|
| **Tenant entity model** | `Explore.Domain/Tenant.cs`, `TenantSetting.cs`, `TenantMember.cs`, `TenantLifecycleLog.cs`, `TenantStatus.cs` | Complete |
| **TenantStatusEnum** | `Explore.Domain/Enums/TenantStatusEnum.cs` (Provisioning, Active, Suspended, Archived, Purged) | Needs: `Deleting(6)`, `Restoring(7)` transitional states |
| **TenantContext (resolution)** | `Explore.API/Services/TenantContext.cs` — header → custom domain → subdomain → default | Needs: split into `TenantResolverService` + `TenantContextAccessor` + `TenantContext`; `ITenantResolver` pipeline; `TenantSlugCache` |
| **DeploymentSettings** | `Explore.Infrastructure/DeploymentSettings.cs` — Mode, DefaultTenantId, HidePlatformAdminInSingleTenant | Complete |
| **SettingsResolver** | `Explore.Infrastructure/Services/SettingsResolver.cs` — 2-tier: system → tenant | Complete |
| **HierarchicalSettingsResolver** | `Explore.Infrastructure/Services/HierarchicalSettingsResolver.cs` — 5-tier cascade | Complete |
| **GovernanceSettingKeys** | `Explore.Domain/Constants/GovernanceSettingKeys.cs` — deployment.*, tenants.*, domains.*, routing.*, etc. | Complete |
| **EF query filters** | `ExploreDbContext` — named `Tenant` and `SoftDelete` filters | Complete; needs `TenantGuardInterceptor` for defense-in-depth |
| **YARP BFF Proxy** | `Explore.Blazor/Extensions/YarpProxyExtensions.cs` — routes `/api/{**catchall}` to API; token + tenant header + setup secret forwarding | Complete |
| **WASM HttpClient (BFF)** | `Explore.Blazor.Client/Program.cs` — points to BFF base address (self), NOT direct API | Complete |
| **CORS Configuration** | `Explore.API/Program.cs` lines 172-205 — `Cors:AllowedOrigins` from config → InternalAppPolicy/ExternalAppPolicy/DevPolicy | Needs: dynamic CORS for self-hosters |
| **BlockInSingleTenantAttribute** | `Explore.API/Filters/BlockInSingleTenantAttribute.cs` — 404/403 for single-tenant mode | Complete |
| **Instance Onboarding Controller** | `Explore.API/Controllers/InstanceOnboardingController.cs` — status, settings, complete, storage, SMTP, auth | Complete |
| **Tenant Onboarding Controller** | `Explore.API/Controllers/TenantOnboardingController.cs` — status, settings, complete, save-step | Complete |
| **Tenant CRUD API** | `Explore.API/Controllers/TenantController.cs` — list, get, count, create, update, delete | Complete |
| **Tenant CRUD Commands** | `CreateTenantCommand`, `UpdateTenantCommand`, `DeleteTenantCommand`, queries | Complete |
| **Blazor Instance Admin** | `Explore.Blazor.Client/Pages/Admin/Instance/` — settings, tenants, governance, domain, branding, storage, SMTP, auth | Partial |
| **Blazor Tenant Admin** | `Explore.Blazor.Client/Pages/Admin/Tenant/` — settings, branding, domain, render policy | Partial |
| **Admin hierarchy** | 4-tier: Instance Admin → Tenant Admin → Org Admin → User (documented in `ADMIN_HIERARCHY.md`) | Documented |
| **Render policy delegation** | Lock/unlock per route group, tenant override cascade | Complete |

### 🔴 Not Yet Implemented / Needs Completion

| Gap | Description | Priority |
|-----|-------------|----------|
| **Single-tenant UX hiding** | In single-tenant mode, platform admin concepts should be completely invisible except for a "Switch to Multi-Tenant" section | P0 |
| **Instance mode indicator** | Show "Instance Mode: Single Tenant" in admin UI with "Enable SaaS Mode" action | P0 |
| **Multi-tenant activation wizard** | Confirmation dialog with DNS setup guide (optional verification), resolver selection including path-based, domain configuration | P0 |
| **Path-based tenant resolver** | `/t/{slug}/...` resolver for DNS-free deployments; all routes adapt under path prefix | P0 |
| **Platform Admin dashboard** | Dedicated dashboard for instance admin in multi-tenant mode (tenant list with stats, platform analytics, global settings) | P0 |
| **DNS diagnostics page** | `/instance/domains/diagnostics` — subdomain resolution, SSL status, CNAME validity checks | P0 |
| **Tenant resolver configuration UI** | Let instance admin choose resolver methods (subdomain, path, header, custom domain) and configure them | P1 |
| **Settings lock/override governance UI** | Visual interface showing which settings are locked vs overridable, with toggle controls, search & category filter | P0 |
| **Strongly-typed SettingDefinition registry** | `SettingDefinition` objects with Key, DisplayName, Description, Category, ValueType, DefaultValue, Lockable, TenantVisible. Central `SettingRegistry` class — no reflection, full type safety | P0 |
| **ITenantResolver extensible pipeline** | Interface-based resolver contributors (`SubdomainTenantResolver`, `PathTenantResolver`, `HeaderTenantResolver`, `CustomDomainResolver`). Foreach resolver in configured order: try resolve. Enables future cookie/query-string/JWT-claim/API-key resolvers. | P0 |
| **TenantSlugCache (in-memory)** | `Dictionary<string, Guid>` for slug→TenantId and domain→TenantId. Updated on tenant CRUD events. Avoids DB queries on every request | P0 |
| **Split TenantContext** | Split monolithic `TenantContext` into `TenantResolverService` (determines tenant), `TenantContextAccessor` (stores per-request), `TenantContext` (exposes to consumers) | P0 |
| **TenantGuardInterceptor** | EF Core interceptor that checks if query touches tenant table and tenant_id is missing → throw exception. Defense-in-depth for cross-tenant protection | P1 |
| **Async tenant provisioning** | `TenantCreated → ProvisioningJob (background) → Active`. Avoids slow HTTP responses for provisioning that involves storage, email, DNS, module initialization | P1 |
| **Dynamic CORS for self-hosters** | `SetIsOriginAllowed()` delegate reading allowed origins from SystemSetting + tenant custom domains at runtime, cached with invalidation. BFF makes WASM→API CORS-free (same-origin) | P0 |
| **Tenant quotas** | max_events, max_storage_mb, max_members per tenant — configurable by instance admin | P1 |
| **Tenant impersonation** | Instance admin can "view as tenant admin" for support purposes | P1 |
| **Tenant self-registration flow** | Public-facing tenant creation with subdomain/path selection, admin account setup | P1 |
| **Invite-based tenant creation** | Request → approve workflow for tenant provisioning | P2 |
| **Instance portal page** | Root domain page in multi-tenant mode showing login, create org, browse tenants | P1 |
| **Multi-tenant → single-tenant revert** | Validation (tenant count == 1), data cleanup, mode switch | P1 |
| **Instance admin banner** | When instance admin visits a tenant domain, show admin context banner | P2 |

---

## Proposed Architecture

### Control Planes (Single Blazor App)

All control planes run inside the **same Blazor application** — no separate projects needed.
Access is controlled by roles and route authorization.

```
┌──────────────────────────────────────────────────┐
│                   Blazor WebApp                  │
│                                                  │
│  ┌─────────────┐ ┌──────────────┐ ┌───────────┐ │
│  │  Instance    │ │   Tenant     │ │  Public   │ │
│  │  Admin       │ │   Admin      │ │  App      │ │
│  │  /instance/* │ │   /admin/*   │ │  /events  │ │
│  │  (InstanceAd │ │   (TenantAdm │ │  (All     │ │
│  │   min role)  │ │   in role)   │ │   users)  │ │
│  └─────────────┘ └──────────────┘ └───────────┘ │
└──────────────────────────────────────────────────┘
```

### Mode Behavior Matrix

| Behavior | Single-Tenant | Multi-Tenant |
|----------|---------------|--------------|
| Root domain | → Event list (default tenant) | → Instance portal (login, browse, create org) |
| Tenant resolution | Always default tenant | Header → custom domain → subdomain → path (`/t/{slug}`) → default |
| `/admin` route | Tenant admin (same person as instance admin) | Tenant admin (scoped to resolved tenant) |
| `/admin/instance/*` | Hidden except "Switch to MT" in tenant settings | Full platform admin dashboard |
| Instance mode indicator | "Instance Mode: Single Tenant" badge in admin | "Instance Mode: Multi-Tenant" badge in admin |
| Tenant list | Hidden | Visible in instance admin |
| Settings governance | All settings directly editable | Lock/unlock/override per setting with search/filter |
| Self-registration | N/A | Configurable via `tenants.self_service_registration` |
| DNS requirements | Single domain only | Wildcard DNS (subdomain resolver) or none (path/header resolver) |
| Tenant quotas | N/A | Configurable per tenant (max events, storage, members) |
| Tenant impersonation | N/A | Instance admin can "view as tenant admin" |

### Tenant Resolution Strategy (Enhanced — Extensible ITenantResolver Pipeline)

> **⚠️ ARCHITECTURAL CONSTRAINT #1:** Resolver configuration MUST be read from `SystemSetting` table
> ONLY (tenant-independent). Tenant resolution must NEVER depend on tenant settings — this avoids
> the circular dependency: TenantContext → needs settings, SettingsResolver → needs tenant.
> Use `ResolverConfigService` backed by `SystemSetting` with aggressive caching.

> **⚠️ ARCHITECTURAL CONSTRAINT #2 — Two-Service Layer Placement:**
> Blazor Web App and ASP.NET API are **SEPARATE services**. Browser URLs like `/t/islamu/events`
> arrive at the **Blazor Web App**, NOT the API. The API only receives requests via YARP with
> `X-Tenant-Id` header already set.
>
> **Resolvers live in different layers based on where requests arrive:**
> - **Blazor Web App (`Explore.Blazor`):** Path resolver middleware, subdomain resolver, custom domain resolver
>   — these resolve from browser URLs before YARP forwards to the API
> - **API (`Explore.API`):** Header resolver only — reads `X-Tenant-Id` set by YARP
> - **Shared (`Explore.Infrastructure`):** `ITenantResolver` interface, `TenantSlugCache`,
>   `TenantContextAccessor`, `TenantContext` — shared by both services
>
> ```
> Browser → events.ngo/t/islamu/events
>     │
>     ▼
> ┌─────────────────────────────────────────┐
> │         Blazor Web App                  │
> │                                         │
> │  PathTenantResolverMiddleware           │
> │    → resolves tenant from /t/islamu/    │
> │    → strips prefix → /events            │
> │    → stores in HttpContext.Items         │
> │                                         │
> │  YARP Transform (ForwardTenantHeader)   │
> │    → reads from HttpContext.Items        │
> │    → injects X-Tenant-Id: {guid}        │
> │    → forwards to API                    │
> └───────────────┬─────────────────────────┘
>                 │ X-Tenant-Id: {guid}
>                 ▼
> ┌─────────────────────────────────────────┐
> │           ASP.NET API                   │
> │                                         │
> │  HeaderTenantResolver                   │
> │    → reads X-Tenant-Id header           │
> │    → done                               │
> └─────────────────────────────────────────┘
> ```

#### Split TenantContext Architecture

The monolithic `TenantContext` is split into three focused components (mirrors `IHttpContextAccessor` pattern).
**Shared infrastructure** lives in `Explore.Infrastructure` so both Blazor and API can use it:

```
┌────────────────────────────┐
│   TenantResolverService    │ ← Orchestrates ITenantResolver pipeline
│   (Scoped per request)     │    Lives in EACH host (Blazor + API) with different resolvers
│                            │
│   Input: HttpContext        │
│   Output: TenantId + Slug  │
└────────────┬───────────────┘
             │ writes resolved tenant
             ▼
┌────────────────────────────┐
│   TenantContextAccessor    │ ← Stores per-request tenant (like IHttpContextAccessor)
│   (Scoped per request)     │    Lives in Explore.Infrastructure (shared)
│                            │
│   HttpContext.Items based  │
└────────────┬───────────────┘
             │ consumed by
             ▼
┌────────────────────────────┐
│      TenantContext         │ ← Read-only exposure for consumers
│   (Scoped per request)     │    Lives in Explore.Infrastructure (shared)
│                            │
│   TenantId, Slug, Name     │
│   IsMultiTenant, IsResolved│
└────────────────────────────┘
```

**Why split:** Separation makes testing much easier — mock `TenantContextAccessor` in tests
without needing a full HTTP pipeline. Aligns with how ASP.NET Core itself separates
`IHttpContextAccessor` from `HttpContext`.

#### Blazor Server Circuit Tenant Affinity

In Blazor Server mode, the initial HTTP request establishes a SignalR circuit. After that,
all user interactions happen over WebSocket — **no more HTTP requests with path/subdomain info.**

The tenant resolved during the initial HTTP request must persist across the circuit lifetime:

1. **Initial request:** Middleware resolves tenant from path/subdomain → stores in `HttpContext.Items`
2. **Circuit establishment:** `CircuitHandler.OnCircuitOpenedAsync()` reads tenant from `HttpContext.Items`
   → stores in circuit-scoped `TenantCircuitState` (a `Scoped` service in Blazor Server DI)
3. **Subsequent interactions:** SignalR messages use `TenantCircuitState` (not HTTP headers)
4. **Circuit reconnection:** If the circuit drops and reconnects, the initial HTTP request carries
   the path/subdomain again → tenant re-resolved from URL
5. **WASM mode:** No circuit concern — each HTTP request to BFF carries the tenant context independently

```csharp
public class TenantCircuitHandler : CircuitHandler
{
    private readonly TenantContextAccessor _accessor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        // Transfer tenant from HTTP context to circuit-scoped state
        var tenantId = _httpContextAccessor.HttpContext?.Items["__resolved_tenant_id"];
        if (tenantId is Guid id)
            _accessor.SetTenant(id);
        return Task.CompletedTask;
    }
}
```

This ensures Blazor Server components always have access to the correct tenant context
regardless of whether the current interaction is an HTTP request or a SignalR message.

#### ITenantResolver Extensible Pipeline

Instead of hardcoding resolver types, an interface-based pipeline is used:

```csharp
public interface ITenantResolver
{
    string Name { get; }           // "subdomain", "path", "header", "custom_domain"
    int Priority { get; }          // fixed: header(1), custom_domain(2), subdomain(3), path(4)
    Task<TenantResolveResult?> ResolveAsync(HttpContext context);
}
```

Concrete implementations:

```
SubdomainTenantResolver    — extracts slug from host subdomain
PathTenantResolver         — extracts slug from /t/{slug}/ path prefix
HeaderTenantResolver       — reads X-Tenant-Id header
CustomDomainTenantResolver — matches full host against tenant custom domains
```

Pipeline execution (in `TenantResolverService`):

```csharp
foreach (var resolver in configuredResolvers.OrderBy(r => r.Priority))
{
    var result = await resolver.ResolveAsync(context);
    if (result != null) return result;
}
return TenantResolveResult.Default(defaultTenantId);
```

**Future extensibility** — new resolvers can be added without touching existing code:
- `CookieTenantResolver` — reads tenant from cookie (Tenant Access Context pattern)
- `QueryStringTenantResolver` — reads from `?tenant=slug` query param
- `JwtClaimTenantResolver` — reads tenant_id from JWT claims
- `ApiKeyTenantResolver` — resolves tenant from API key

This matches how ABP Framework's `TenantResolveContributor` pipeline works.

#### TenantSlugCache (In-Memory Resolution Cache)

Tenant resolution happens on **every request**. Database indexes help, but we should
avoid DB queries entirely for the hot path:

```csharp
public class TenantSlugCache
{
    private ConcurrentDictionary<string, Guid> _slugCache = new();
    private ConcurrentDictionary<string, Guid> _domainCache = new();

    public Guid? ResolveBySlug(string slug) =>
        _slugCache.TryGetValue(slug, out var id) ? id : null;

    public Guid? ResolveByDomain(string domain) =>
        _domainCache.TryGetValue(domain, out var id) ? id : null;

    public void Rebuild(IReadOnlyList<Tenant> tenants) { ... }
}
```

- **Populated at startup** by loading all active tenants
- **Updated on tenant CRUD events** (create, update, delete, status change)
- **Used by all `ITenantResolver` implementations** before falling back to DB
- **Thread-safe** via `ConcurrentDictionary`
- Large SaaS systems (Sentry, GitLab) all use this pattern

#### Resolver Enable/Disable (Simplified — Fixed Priority)

Resolver priority is **fixed** (not configurable) — the natural order is correct for virtually
all deployments. Making order configurable adds complexity few users would touch:

```
Fixed priority: header(1) → custom_domain(2) → subdomain(3) → path(4)
```

Each resolver can be **enabled/disabled** via governance keys:
```
routing.resolver_header_enabled = true       // always on (YARP uses this)
routing.resolver_custom_domain_enabled = false
routing.resolver_subdomain_enabled = false
routing.resolver_path_enabled = true         // default for simplest setup
```

The instance admin toggles resolvers on/off in the activation wizard — no reordering UI needed.
This covers the real use case (choose which methods are active) without the validation complexity
of arbitrary ordering.

#### Resolution Flowchart

```
Request arrives
    │
    ├── Is deployment mode SingleTenant?
    │   └── YES → Use default tenant → done
    │
    ├── FOR EACH resolver in configured order (via ITenantResolver pipeline):
    │   │
    │   ├── "header" — X-Tenant-Id header present?
    │   │   └── YES → TenantSlugCache lookup → done
    │   │
    │   ├── "custom_domain" — Host matches tenant custom domain?
    │   │   └── YES → TenantSlugCache.ResolveByDomain() → done
    │   │
    │   ├── "subdomain" — Host has subdomain matching tenant slug?
    │   │   └── YES → TenantSlugCache.ResolveBySlug() → done
    │   │   └── Is host the instance base domain? → Instance portal
    │   │
    │   ├── "path" — Path starts with /t/{slug}/...?
    │   │   └── YES → TenantSlugCache.ResolveBySlug(), rewrite path → done
    │   │
    │   └── (continue to next resolver)
    │
    └── Fallback → default tenant
```

#### Resolver Telemetry

Every resolution attempt emits structured telemetry (via existing Serilog + OpenTelemetry stack):

```
tenant.resolution.method = "subdomain"     // which resolver matched
tenant.resolution.slug = "islamu"           // resolved slug
tenant.resolution.host = "islamu.events.example.org"
tenant.resolution.path = "/events"          // original request path
tenant.resolution.duration_ms = 0.42        // resolution latency
tenant.resolution.cache_hit = true          // whether TenantSlugCache was used
```

This enables per-tenant debugging, SLA monitoring, and capacity planning.
Uses existing `BusinessMetrics` singleton on meter `"Explore.Business"` for counters.

#### Path-Based Resolver Details — `/t/` Prefix Only

When `path` resolver is active, tenant resolution **only activates when the path starts
with `/t/`** (with trailing slash). Everything else passes through completely untouched.

**This is the key simplification:** No skip list needed for `/_framework`, `/_blazor`,
`/api/*`, `/health`, `/metrics`, etc. — none of these start with `/t/`, so the middleware
simply never runs for them.

```
┌────────────────────────────────────────────┐
│         Routing Namespace Model            │
│                                            │
│  /t/{slug}/...        → tenant app         │
│  /instance/...        → platform admin     │
│  /admin/...           → tenant admin       │
│  /api/...             → API (via BFF/YARP) │
│  /health              → health probe       │
│  /metrics             → telemetry          │
│  /_framework/...      → Blazor runtime     │
│  /_blazor             → SignalR hub        │
│  /                    → portal / ST home   │
└────────────────────────────────────────────┘
```

The middleware is trivially simple:

```csharp
if (!path.StartsWithSegments("/t", out var remaining) || remaining.Value?.StartsWith('/') != true)
{
    await next(context);
    return;
}
// Extract slug from /t/{slug}/... and resolve
```

**Important:** Check for `/t/` (with trailing slash via `remaining` starting with `/`),
NOT just `/t` — this avoids false matching on paths like `/teams`, `/tags`, etc.

**Edge cases handled:**
- `/t` → 404 (no slug)
- `/t/` → 404 (empty slug)
- `/t//events` → 404 (empty slug)

**Reserved:** The `/t` namespace is exclusively for tenant routing. Nothing else uses it.

### TenantGuardInterceptor (Cross-Tenant Query Safety)

Defense-in-depth beyond EF query filters. An EF Core `SaveChangesInterceptor` that verifies
tenant-scoped entities always have a valid `TenantId`:

```csharp
public class TenantGuardInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(...)
    {
        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                throw new InvalidOperationException("TenantId missing on tenant-scoped entity");
        }
        return base.SavingChanges(...);
    }
}
```

**Also consider a query interceptor** that throws when a query touches a tenant table
without a tenant filter in the WHERE clause. This catches accidental `IgnoreQueryFilters()` usage.

Large SaaS platforms (ABP, Finbuckle) add this protection layer.

### Dynamic CORS for Self-Hosters

**Problem:** Each self-hoster has different domains. In multi-tenant mode with subdomains,
each tenant subdomain also needs to be allowed. Can't hardcode allowed origins.

**Architecture context — BFF reduces CORS surface:**
```
┌─────────────────────┐     same-origin      ┌──────────────────┐
│   Blazor WASM       │ ──────────────────→  │  Blazor Server   │
│   (browser)         │     (no CORS needed)  │  (BFF/YARP)      │
└─────────────────────┘                       └───────┬──────────┘
                                                      │ server-to-server
                                                      │ (no CORS needed)
                                                      ▼
                                              ┌──────────────────┐
                                              │   ASP.NET API    │
                                              └──────────────────┘
```

WASM → BFF is same-origin (no CORS needed). YARP → API is server-to-server (no CORS needed).
**CORS is only needed for the API when accessed directly** by:
- Third-party API consumers
- Mobile apps
- Development tools (Swagger, Postman)

**Solution:** Dynamic CORS via `SetIsOriginAllowed()`:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("DynamicPolicy", builder =>
    {
        builder.SetIsOriginAllowed(origin =>
        {
            // 1. Check static config (Cors:AllowedOrigins)
            if (staticOrigins.Contains(origin)) return true;

            // 2. Check base domain pattern (*.base-domain for subdomains)
            var uri = new Uri(origin);
            if (uri.Host.EndsWith($".{baseDomain}")) return true;

            // 3. Check tenant custom domains (cached from DB)
            if (tenantDomainCache.Contains(uri.Host)) return true;

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
```

**Cache invalidation:** When tenant custom domains change or base domain config changes,
the domain cache is invalidated. Uses the same `TenantSlugCache` infrastructure.

**Governance keys involved:**
- `domains.instance_base_domain` — used for subdomain wildcard matching
- `cors.additional_origins` (new) — for self-hosters to add extra allowed origins

### Strongly-Typed SettingDefinition Registry

**Replaces** the attribute-based `[SettingMetadata]` approach from v3.

Instead of reflection-discovered attributes, settings are defined as objects:

```csharp
public sealed class SettingDefinition
{
    public string Key { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public SettingCategory Category { get; }
    public SettingValueType ValueType { get; }
    public string DefaultValue { get; }
    public bool Lockable { get; }
    public bool TenantVisible { get; }
}
```

Registered centrally:

```csharp
public static class SettingRegistry
{
    public static readonly SettingDefinition TenantsMaxEvents = new(
        key: GovernanceSettingKeys.TenantsDefaultMaxEvents,
        displayName: "Max Events",
        description: "Maximum number of events allowed per tenant",
        category: SettingCategory.Quotas,
        valueType: SettingValueType.Int,
        defaultValue: "500",
        lockable: true,
        tenantVisible: true
    );

    public static IReadOnlyList<SettingDefinition> All => [ TenantsMaxEvents, ... ];
}
```

**Advantages over attributes:**
- ✔ No reflection
- ✔ Full type safety
- ✔ One central registry — easy to discover all settings
- ✔ Easy to document and test
- ✔ Compile-time errors for missing definitions

**Architecture test:** Every key in `GovernanceSettingKeys` MUST have a corresponding
`SettingDefinition` in `SettingRegistry.All`.

Used by Keycloak, Elasticsearch, Spring Boot.

### Async Tenant Provisioning

Tenant creation that involves heavy operations (storage buckets, email configuration,
DNS validation, module initialization) runs as a background job:

```
CreateTenantCommand
    │
    ├── Create Tenant entity (status = Provisioning)
    ├── Create TenantLifecycleLog entry
    ├── Enqueue ProvisionTenantJob
    └── Return TenantId immediately
         │
         │ (background)
         ▼
    ProvisionTenantJob
    │
    ├── Initialize storage bucket
    ├── Configure email settings
    ├── Apply default settings
    ├── Create default tenant admin
    ├── Validate DNS (if subdomain resolver)
    └── Transition → Active
```

**Why async:** Avoids slow HTTP responses during tenant creation. The tenant appears in
the admin dashboard immediately with "Provisioning..." status. Self-registration returns
immediately with a "setting up your workspace" message.

### Tenant Access Context Pattern (Future Consideration)

Large SaaS platforms (Sentry, Supabase, Notion) use a pattern where:
- User belongs to multiple tenants
- User logs in once
- User selects active tenant from a picker
- Session stores `activeTenantId`
- Requests include `X-Tenant-Id` header

The system already supports header resolver, so this can be added later easily.
Consider this for Phase 2 or 3 of a future iteration.

### Settings Governance Model

```
┌──────────────────────────────────────┐
│         SystemSetting (Instance)     │
│  Key: "events.max_capacity"          │
│  Value: "1000"                       │
│  IsLocked: false                     │
│  DefaultValue: "500"                 │
└──────────────┬───────────────────────┘
               │ (if unlocked)
               ▼
┌──────────────────────────────────────┐
│       TenantSetting (Override)       │
│  TenantId: {tenant-guid}            │
│  SettingKey: "events.max_capacity"   │
│  Value: "2000"                       │
└──────────────────────────────────────┘

Resolution: Locked? → System value. Unlocked? → Tenant override ?? System value
```

---

## Implementation Phases

### Phase 1: Single-Tenant Mode Polish (Domain + Application + API + Blazor)

**Goal:** When deployment mode is SingleTenant, the app behaves as if multi-tenancy doesn't exist.
The only exception: instance administrators see a "Switch to Multi-Tenant Mode" section
buried in tenant settings (not prominently displayed).

#### Task 1.1: Add `deployment.mode` Toggle API Endpoint
- **File:** `Explore.API/Controllers/InstanceOnboardingController.cs` (or new `DeploymentModeController.cs`)
- **What:** `POST /api/instance/deployment-mode` — switches between SingleTenant ↔ MultiTenant
- **Validation:**
  - To switch ST → MT: require instance admin role, show confirmation
  - To switch MT → ST: require tenant count == 1, require instance admin role
- **Updates:** `SystemSetting` key `deployment.mode`
- **Invalidates:** Settings cache (`SystemSettings_All`)
- **Effort:** M
- **Skill:** `cqrs-mediatr-guidelines`, `auth-patterns`

#### Task 1.2: Enhance `BlockInSingleTenantAttribute` for Conditional Route Hiding
- **File:** `Explore.API/Filters/BlockInSingleTenantAttribute.cs`
- **What:** Ensure all instance-admin-only endpoints (tenant CRUD, governance, platform analytics) return 404 in single-tenant mode
- **Acceptance:** All `/api/tenant/*` list/create endpoints return 404 in ST mode; individual tenant GET/PUT for the default tenant still works
- **Effort:** S

#### Task 1.3: Blazor Single-Tenant Admin UX
- **Files:** `Explore.Blazor.Client/Pages/Admin/` (various)
- **What:** In single-tenant mode:
  - Hide "Instance Admin" navigation section entirely
  - Hide "Tenants" section in admin sidebar
  - In Tenant Settings page, add a collapsible section at the bottom: "Advanced: Multi-Tenant Mode"
  - This section contains: description text, enable button, and warning dialog
- **Acceptance:** Regular tenant admin sees no multi-tenancy concepts; only instance admin sees the hidden section
- **Effort:** M
- **Skill:** `blazor-ui-conventions`

#### Task 1.4: Root Domain Behavior in Single-Tenant Mode
- **Files:** `Explore.Blazor/Components/App.razor` or routing middleware
- **What:** Root domain `/` redirects to event list page (public home)
- **Acceptance:** `https://events.company.org/` → event discovery page, no portal, no login wall
- **Effort:** S

#### Task 1.5: Instance Mode Indicator in Admin UI
- **File:** Admin layout component (e.g., `AdminSidebar.razor` or `AdminTopBar.razor`)
- **What:** Display "Instance Mode: Single Tenant" badge in admin UI with contextual action:
  - In ST mode: badge + "Enable SaaS Mode" link (opens activation wizard)
  - In MT mode: badge + "Manage Platform" link (goes to instance admin)
- **Acceptance:** Mode is always visible in admin; provides one-click access to relevant action
- **Effort:** S
- **Skill:** `blazor-ui-conventions`

---

### Phase 2: Multi-Tenant Activation Wizard (Application + API + Blazor)

**Goal:** When instance admin clicks "Enable Multi-Tenant Mode," a guided wizard walks them through configuration.

#### Task 2.1: Multi-Tenant Activation Confirmation Dialog
- **File:** New component `Explore.Blazor.Client/Pages/Admin/Instance/Components/MultiTenantActivationWizard.razor`
- **What:** Multi-step dialog:
  - **Step 1 — Confirmation:** Warning text explaining implications (DNS may be needed, irreversible if >1 tenant created). Checkbox: "I understand..."
  - **Step 2 — Resolver Selection:** Choose tenant resolution method(s):
    - ☑ Path-based `/t/{slug}` (simplest — no DNS needed) — enabled by default
    - ☑ Subdomain (recommended for SaaS) — requires wildcard DNS
    - ☑ Custom Domain — tenants bring their own domain
    - ☐ Header (`X-Tenant-Id`) — always on (used internally by YARP), shown as info-only
  - **Step 3 — Domain Configuration:** (only if subdomain or custom domain selected)
    - Instance base domain input (e.g., `events.example.org`)
    - Platform admin subdomain (e.g., `platform.events.example.org` or same as base)
  - **Step 4 — DNS Setup Guide:** (only if subdomain/custom domain selected; read-only instructions)
    - Show required DNS records based on resolver selection
    - Provider-specific tabs (Cloudflare, Route53, GoDaddy, generic)
    - **DNS verification is OPTIONAL** — "Skip & Verify Later" prominently shown
  - **Step 5 — Activate:** Final summary, activate button (no blocking DNS verification)
- **API calls:** `POST /api/instance/deployment-mode` + batch `PUT` governance settings
- **Effort:** L
- **Skill:** `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 2.2: DNS Setup Guide Component
- **File:** New component `Explore.Blazor.Client/Shared/Components/DnsSetupGuide.razor`
- **What:** Reusable component showing DNS configuration instructions:
  - Wildcard A record: `*.events.example.org → {server-ip}`
  - Individual A records for known subdomains
  - CNAME for custom domains
  - SSL/TLS guidance (Let's Encrypt wildcard via DNS-01 challenge, or Caddy/Traefik auto-SSL)
  - Copy-to-clipboard for DNS values
- **Props:** `BaseDomain`, `ServerIp` (auto-detected if possible), `ResolverMethods`
- **Effort:** M

#### Task 2.3: Tenant Resolver Configuration API
- **File:** New or extend `Explore.API/Controllers/InstanceSettingsController.cs`
- **What:** Endpoints to manage resolver configuration:
  - `GET /api/instance/resolver-config` — returns enabled resolvers and their settings
  - `PUT /api/instance/resolver-config` — update resolver settings
- **⚠️ CRITICAL:** Resolver config is stored in `SystemSetting` (system-level) only.
  A dedicated `ResolverConfigService` reads from `SystemSetting` directly — NEVER via `SettingsResolver`
  (which needs a tenant ID). Cache aggressively (5 min + invalidation on config change).
- **Governance keys involved:**
  - `domains.instance_base_domain`
  - `domains.allow_tenant_custom_domain`
  - `routing.resolver_header_enabled` (default: true — always on for YARP)
  - `routing.resolver_subdomain_enabled` (default: false)
  - `routing.resolver_custom_domain_enabled` (default: false)
  - `routing.resolver_path_enabled` (default: true — simplest setup)
  - `routing.path_prefix` (new: default `/t`, configurable)
- **Validation:** At least one resolver must be enabled; header resolver cannot be disabled (YARP depends on it)
- **Effort:** M
- **Skill:** `cqrs-mediatr-guidelines`

#### Task 2.4: Implement ITenantResolver Pipeline + Split TenantContext + TenantSlugCache
- **Files — Shared Infrastructure (used by BOTH Blazor and API):**
  - New `Explore.Application/Contracts/Services/ITenantResolver.cs` — resolver interface
  - New `Explore.Application/Contracts/Services/ITenantContextAccessor.cs` — per-request tenant storage
  - New `Explore.Infrastructure/Services/TenantSlugCache.cs` — in-memory slug/domain → TenantId cache
  - New `Explore.Infrastructure/Services/TenantContextAccessor.cs` — stores per-request tenant in HttpContext.Items
  - Refactored `Explore.Infrastructure/Services/TenantContext.cs` — read-only consumer (moved from API)
  - New `Explore.Infrastructure/Services/TenantResolverService.cs` — orchestrates ITenantResolver pipeline
- **Files — Blazor Web App (browser-facing resolvers):**
  - New `Explore.Blazor/Middleware/PathTenantResolverMiddleware.cs` — extracts tenant from `/t/{slug}/`
  - New `Explore.Blazor/Services/Resolvers/SubdomainTenantResolver.cs` — extracts tenant from host subdomain
  - New `Explore.Blazor/Services/Resolvers/CustomDomainTenantResolver.cs` — matches host against tenant domains
  - New `Explore.Blazor/Services/Resolvers/BlazorHeaderTenantResolver.cs` — reads X-Tenant-Id (for direct API calls to BFF)
  - New `Explore.Blazor/Services/TenantCircuitHandler.cs` — CircuitHandler for Blazor Server tenant affinity
- **Files — API (YARP-forwarded requests only):**
  - New `Explore.API/Services/Resolvers/HeaderTenantResolver.cs` — reads X-Tenant-Id header (set by YARP)
  - Refactored `Explore.API/Services/TenantContext.cs` → delegates to shared `TenantContext` in Infrastructure
- **YARP Tenant Propagation Fix:**
  - Update `Explore.Blazor/Extensions/YarpProxyExtensions.cs` → `ForwardTenantHeader()`:
    - **Current behavior:** Passes through `X-Tenant-Id` from incoming HTTP request headers
    - **New behavior:** Reads resolved tenant from `HttpContext.Items["__resolved_tenant_id"]` (set by Blazor-side resolver middleware) and INJECTS it as `X-Tenant-Id` header on the outgoing proxy request
    - This closes the critical gap: Blazor resolves tenant (path/subdomain/domain) → YARP propagates to API
    ```csharp
    private static void ForwardTenantHeader(RequestTransformContext context)
    {
        // Priority: resolved tenant from middleware > incoming header
        if (context.HttpContext.Items["__resolved_tenant_id"] is Guid resolvedId)
        {
            context.ProxyRequest.Headers.Remove(TenantConstants.TenantIdHeaderName);
            context.ProxyRequest.Headers.Add(TenantConstants.TenantIdHeaderName, resolvedId.ToString());
        }
        else
        {
            // Fallback: pass through incoming header (e.g., from Tenant Access Context)
            var incomingTenantId = context.HttpContext.Request.Headers[TenantConstants.TenantIdHeaderName]
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(incomingTenantId))
            {
                context.ProxyRequest.Headers.Add(TenantConstants.TenantIdHeaderName, incomingTenantId);
            }
        }
    }
    ```
- **What:**
  - **ITenantResolver interface:** Each resolver has `Name`, `Priority` (fixed), and `ResolveAsync(HttpContext)`.
  - **TenantResolverService:** Iterates enabled `ITenantResolver` instances in fixed priority order; first match wins.
    Reads enabled resolvers from `ResolverConfigService` (system settings only).
    **Each host registers different resolvers:** Blazor registers path/subdomain/domain/header; API registers header only.
  - **TenantContextAccessor:** Stores resolved tenant in `HttpContext.Items` (replaces inline caching in old `TenantContext`).
    Scoped per request. Shared infrastructure — used by both hosts.
  - **TenantContext:** Read-only exposure of resolved tenant — `TenantId`, `Slug`, `Name`, `IsMultiTenant`, `IsResolved`.
    All existing consumers use this unchanged. Moved to `Explore.Infrastructure` (shared).
  - **TenantSlugCache:** In-memory `ConcurrentDictionary<string, Guid>` for slug→TenantId and domain→TenantId.
    Populated at startup, updated on tenant CRUD events (create/update/delete/status change).
    All resolvers use cache first, fall back to DB query only on miss.
  - **TenantCircuitHandler:** Blazor Server `CircuitHandler` that transfers resolved tenant from HttpContext
    to circuit-scoped state on circuit open. Ensures SignalR interactions retain tenant context.
  - **Resolver telemetry:** Emit structured logs via Serilog + OpenTelemetry per Proposed Architecture spec.
    Uses existing `BusinessMetrics` singleton.
- **Effort:** XL (major refactor of core resolution infrastructure + cross-service coordination)
- **Depends on:** 2.3 (resolver config)

#### Task 2.5: Path-Based Tenant Resolver Middleware (`/t/{slug}/...`)
- **File:** New `Explore.Blazor/Middleware/PathTenantResolverMiddleware.cs` (**in Blazor Web App, NOT API**)
- **What:** When path resolver is enabled:
  - **Only activates when path starts with `/t/`** (with trailing slash via `StartsWithSegments`)
  - Everything NOT starting with `/t/` passes through completely untouched — **NO skip list needed**
  - Extracts slug from `/t/{slug}/...`
  - Resolves tenant via `TenantSlugCache` (then DB fallback)
  - Rewrites `HttpContext.Request.Path` to strip `/t/{slug}` prefix (e.g., `/t/islamu/events` → `/events`)
  - Stores resolved tenant in `HttpContext.Items["__resolved_tenant_id"]` — YARP reads this and injects `X-Tenant-Id`
  - Stores resolved tenant in `TenantContextAccessor`
  - **Edge cases:**
    - `/t` → 404 (no slug)
    - `/t/` → 404 (empty slug)
    - `/t//events` → 404 (empty slug)
    - Must check `/t/` not just `/t` — avoids false match on `/teams`, `/tags`, etc.
  - **Reserved slugs blocklist:** `admin`, `instance`, `api`, `auth`, `callback`
  - Instance routes (`/instance/*`) and root `/` are never path-resolved (they don't start with `/t/`)
  - Framework paths (`/_framework/*`, `/_blazor`) are never path-resolved (they don't start with `/t/`)
  - API routes (`/api/*`) are never path-resolved (they don't start with `/t/`)
  - **No infrastructure coupling, no skip list, no special middleware rules** — just one prefix check
- **Middleware Ordering:** AFTER `UsePathBase` but BEFORE `UseStaticFiles` and `UseRouting`
- **Effort:** M (downgraded from L — greatly simplified by prefix-only approach)
- **Depends on:** 2.4 (TenantSlugCache, TenantContextAccessor)

#### Task 2.6: DNS Diagnostics Page
- **File:** New `Explore.Blazor.Client/Pages/Admin/Instance/Components/DnsDiagnosticsSection.razor`
- **What:** Instance admin diagnostics page at `/instance/domains/diagnostics`:
  - For each configured resolver method, show health status
  - **Subdomain check:** Resolve `test.{base-domain}` and show result
  - **Custom domain check:** For each tenant custom domain, verify DNS resolution + SSL status
  - **Certificate validity:** Show SSL cert expiry dates and warn if <30 days
  - **CNAME loop detection:** Detect and warn about circular CNAME records
  - **Wildcard coverage:** Verify `*.{base-domain}` resolves correctly
  - **HTTP redirect checks:** Verify HTTP→HTTPS redirects work for each domain
  - **Path resolver:** Always healthy (no DNS needed)
  - Manual "Re-check Now" button
  - Last check timestamp
  - Historical check results (last 7 days)
- **Replaces:** The old "Verify DNS" blocking step in the wizard
- **Effort:** L (upgraded from M due to extended diagnostics)

#### Task 2.8: TenantGuardInterceptor (Cross-Tenant Query Safety)
- **File:** New `Explore.Persistence/Interceptors/TenantGuardInterceptor.cs`
- **What:** EF Core `SaveChangesInterceptor` for defense-in-depth:
  - On `SavingChanges`: iterate `ChangeTracker.Entries<ITenantEntity>()` — if `State == Added` and `TenantId == Guid.Empty`, throw `InvalidOperationException`
  - Prevents accidental writes without tenant context
  - Registered in `ExploreDbContext` via `optionsBuilder.AddInterceptors()`
  - **Future:** Consider a query interceptor that detects `IgnoreQueryFilters()` usage outside instance-admin contexts (architecture tests cover this for now)
- **Effort:** S
- **Depends on:** None (can be done independently)

#### Task 2.9: Dynamic CORS for Self-Hosters
- **File:** `Explore.API/Program.cs` (CORS section, lines 172-205)
- **What:** Replace static `Cors:AllowedOrigins` with dynamic `SetIsOriginAllowed()`:
  - **Delegate reads from:**
    1. Static config `Cors:AllowedOrigins` (preserves existing functionality)
    2. `domains.instance_base_domain` — wildcard match for `*.{base-domain}` (subdomain tenants)
    3. Tenant custom domains from `TenantSlugCache.DomainCache`
    4. New governance key `cors.additional_origins` — extra origins for API direct consumers
  - **Caching:** Domain set cached in memory, invalidated when:
    - Base domain setting changes
    - Tenant custom domain is added/removed
    - `cors.additional_origins` setting changes
  - **BFF context:** WASM → Blazor Server is same-origin (no CORS). YARP → API is server-to-server (no CORS). This CORS policy only matters for **direct API access** (third-party consumers, mobile apps, Swagger).
  - Preserves existing InternalAppPolicy/ExternalAppPolicy/DevPolicy structure
  - New governance key: `cors.additional_origins` (comma-separated list of additional allowed origins)
- **Effort:** M
- **Depends on:** 2.4 (TenantSlugCache for domain lookups)

#### Task 2.7: TenantUrlBuilder Service
- **File — Interface:** New `Explore.Application/Contracts/Services/ITenantUrlBuilder.cs`
- **File — Implementation:** New `Explore.Blazor.Client/Services/TenantUrlBuilder.cs` (**in Blazor layer, NOT Application — wraps NavigationManager**)
- **What:** Centralized URL generation that respects the active resolver method:
  - When path resolver: `BuildUrl("/events") → "/t/{slug}/events"`
  - When subdomain resolver: `BuildUrl("/events") → "https://{slug}.{base-domain}/events"`
  - When custom domain: `BuildUrl("/events") → "https://{custom-domain}/events"`
  - When single-tenant: `BuildUrl("/events") → "/events"` (passthrough)
  - Provides `BuildAbsoluteUrl()` for external links (emails, API responses)
  - **Layer separation:** `ITenantUrlBuilder` interface in Application layer (pure URL string generation).
    `TenantUrlBuilder` implementation in `Explore.Blazor.Client` because it wraps `NavigationManager`.
    For non-Blazor contexts (email templates, API responses), a `ServerTenantUrlBuilder` in Infrastructure
    generates URLs from config without `NavigationManager`.
  - **Blazor integration:** Inject as scoped service; Blazor components use `TenantUrlBuilder.NavigateTo()` instead of raw `NavigationManager.NavigateTo()`
- **Why:** Without this, every `Navigation.NavigateTo($"/event/detail/{id}")` call in the Blazor client generates URLs missing the `/t/{slug}` prefix — broken links in path-resolver mode
- **Effort:** M

---

### Phase 3: Platform Admin Dashboard (Multi-Tenant Mode)

**Goal:** Instance admin gets a dedicated dashboard accessible only in multi-tenant mode.

#### Task 3.1: Platform Admin Layout & Navigation
- **Files:** New layout/section in `Explore.Blazor.Client/Pages/Admin/Instance/`
- **What:** Instance admin dashboard with:
  - **Overview card:** Total tenants, total users across tenants, total events, storage used
  - **Tenant list table:** Name, slug, status, member count, event count, created date, actions
  - **Quick actions:** Create tenant, view tenant, suspend tenant
  - **Navigation sidebar:** Dashboard, Tenants, Settings (Governance), Domains, Branding, Analytics
- **Access control:** `InstanceAdmin` role + `DeploymentMode == MultiTenant`
- **Effort:** L
- **Skill:** `blazor-ui-conventions`

#### Task 3.2: Tenant Management Page (Enhanced)
- **File:** Enhance `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceTenantsSection.razor`
- **What:** Full tenant lifecycle management:
  - Create tenant (name, slug, initial admin user)
  - View tenant details (members, settings, analytics)
  - Suspend/unsuspend tenant (with reason, logged to `TenantLifecycleLog`)
  - Archive tenant
  - Delete/purge tenant (with confirmation)
  - Tenant status badge (Provisioning → Active → Suspended → Archived → Deleting → Purged; Restoring transitions back)
- **API:** Uses existing `TenantController` endpoints + new lifecycle transition endpoint
- **Effort:** L

#### Task 3.3: Tenant Lifecycle Transition API
- **File:** Extend `Explore.API/Controllers/TenantController.cs`
- **What:** `POST /api/tenant/{id}/transition` — body: `{ newStatus, reason }`
  - Validates allowed transitions (e.g., Active → Suspended, not Purged → Active)
  - Creates `TenantLifecycleLog` entry
  - Updates tenant status
  - **Transitional states:** `Deleting(6)` and `Restoring(7)` prevent race conditions:
    - `Archived → Deleting → Purged` (async purge can take time for storage cleanup)
    - `Archived → Restoring → Active` (re-enables tenant; validates data integrity first)
    - No other transition INTO or OUT of `Deleting`/`Restoring` allowed (enforced by state machine)
  - **⚠️ EF Core Migration Required:** Add migration to seed `TenantStatus` lookup rows for `Deleting(6)` and `Restoring(7)`. Update `TenantStatusEnum.cs` and seed data in `ExploreDbContext.OnModelCreating()`.
  - **Allowed transitions matrix:**
    ```
    Provisioning → Active
    Active → Suspended | Archived
    Suspended → Active | Archived
    Archived → Deleting | Restoring
    Deleting → Purged (system-only, triggered after cleanup completes)
    Restoring → Active (system-only, triggered after validation passes)
    ```
- **Command:** New `TransitionTenantStatusCommand`
- **Effort:** M
- **Skill:** `cqrs-mediatr-guidelines`

#### Task 3.4: Platform Analytics API
- **File:** New `Explore.API/Controllers/PlatformAnalyticsController.cs`
- **What:** Instance-level analytics endpoints:
  - `GET /api/instance/analytics/overview` — tenant count, user count, event count, storage
  - `GET /api/instance/analytics/tenants` — per-tenant breakdown (events, users, storage, quota usage)
- **Access:** Instance admin only, `[BlockInSingleTenant]`
- **Effort:** M

#### Task 3.5: Tenant Impersonation ("View as Tenant Admin")
- **File:** New `Explore.API/Controllers/TenantImpersonationController.cs` + Blazor component
- **What:** Instance admin can view any tenant's admin interface as if they were tenant admin:
  - `POST /api/instance/impersonate/{tenantId}` — sets a session-scoped impersonation context
  - `DELETE /api/instance/impersonate` — ends impersonation
  - During impersonation: `TenantContext` returns impersonated tenant; UI shows prominent "Impersonating: {TenantName}" banner with "Stop Impersonating" button
  - Impersonation is **read-only by default** (configurable); all actions logged to audit trail
  - Only instance admin can impersonate; impersonation context stored in session, not JWT
  - **Audit logging:** Every request during impersonation logs:
    ```
    impersonation_user_id      // who is impersonating
    impersonated_tenant_id     // which tenant
    impersonation_started_at   // when session began
    action                     // what was accessed/modified
    ```
- **Acceptance:** Instance admin can troubleshoot tenant settings, see what tenant admin sees
- **Effort:** L
- **Skill:** `auth-patterns`

#### Task 3.6: Tenant Quotas Configuration (3-Layer Enforcement)
- **File:** New governance setting keys + API + UI + background job
- **What:** Per-tenant quotas configurable by instance admin:
  - **Governance keys:** `tenants.default_max_events`, `tenants.default_max_storage_mb`, `tenants.default_max_members`
  - **Per-tenant overrides:** `TenantSetting` entries for individual tenant limits
  - **Layer 1 — Command handlers:** Block creation when quota exceeded (e.g., `CreateEventCommandHandler` checks event count vs limit before persisting). Returns clear error: "Quota exceeded: {current}/{limit} events"
  - **Layer 2 — UI indicators:** Tenant admin sees quota usage bars; when approaching limit (>80%), warning shown. At limit, create buttons disabled with tooltip explaining quota
  - **Layer 3 — Background reconciliation:** Periodic job (via Aspire background service) verifies quota consistency — catches edge cases where concurrent requests bypassed command-level checks. Logs violations but does NOT delete existing data
  - **Quota display:** Instance admin sees quota config per tenant with usage percentages
- **Effort:** XL (upgraded from L due to 3-layer enforcement)
- **Skill:** `cqrs-mediatr-guidelines`

---

### Phase 4: Settings Governance UI

**Goal:** Instance admin can lock/unlock settings and set defaults; tenant admin sees which settings they can override.

#### Task 4.1: Settings Governance Page (Instance Admin)
- **File:** Enhance `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor`
- **What:** Table/accordion of all governance settings grouped by category:
  - Each setting row shows: key, current value, lock toggle (🔒/🔓), default value, description
  - **Show effective value:** For any setting, display 3 columns:
    ```
    System Default: 500
    Tenant Override: 800
    Effective Value: 800  ← what the tenant actually sees
    ```
    When no override exists, effective value equals system default (grayed out)
  - Locked settings show lock icon and tooltip: "Tenants cannot override this setting"
  - Unlocked settings show unlock icon and tooltip: "Tenants may override this setting"
  - Category groups: Deployment, Events, Organizations, Branding, Domains, Email, Storage, Auth, Modules, Analytics, Localization, Quotas
  - **Search bar:** Filter settings by key/display name (important — will grow to 50+ settings)
  - **Filter toggles:** "Show only locked", "Show only overridable", "Show only tenant-visible"
  - **Tenant context selector:** When viewing a specific tenant's governance, show per-tenant effective values
- **API:** Uses existing `SettingsResolver` / `HierarchicalSettingsResolver` lock/set methods
- **Effort:** L
- **Skill:** `blazor-ui-conventions`

#### Task 4.2: Tenant Settings Page (Tenant Admin)
- **File:** Enhance `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor`
- **What:** Shows tenant-overridable settings:
  - Locked settings appear grayed out with lock icon and "Set by instance admin" label
  - Unlocked settings are editable
  - "Reset to default" button per setting to remove tenant override
  - Categories match governance page grouping
  - **Quota indicators:** Show quota usage bars for events, storage, members (if quotas enabled)
  - **Search bar:** Same as governance page
- **Effort:** M

#### Task 4.3: Strongly-Typed SettingDefinition Registry (Replaces Attributes)
- **Files:**
  - New `Explore.Domain/Settings/SettingDefinition.cs` — immutable definition class
  - New `Explore.Domain/Settings/SettingRegistry.cs` — central registry of ALL setting definitions
  - New `Explore.Domain/Settings/SettingCategory.cs` — enum for categories
  - New `Explore.Domain/Settings/SettingValueType.cs` — enum for value types
- **What:** Instead of `[SettingMetadata]` attributes + reflection, use strongly-typed objects:
  ```csharp
  public sealed class SettingDefinition
  {
      public string Key { get; }
      public string DisplayName { get; }
      public string Description { get; }
      public SettingCategory Category { get; }
      public SettingValueType ValueType { get; }
      public string DefaultValue { get; }
      public bool Lockable { get; }
      public bool TenantVisible { get; }
  }
  ```
  Register centrally:
  ```csharp
  public static class SettingRegistry
  {
      public static readonly SettingDefinition TenantsMaxEvents = new(
          key: GovernanceSettingKeys.TenantsDefaultMaxEvents,
          displayName: "Max Events",
          description: "Maximum number of events per tenant",
          category: SettingCategory.Quotas,
          valueType: SettingValueType.Int,
          defaultValue: "500",
          lockable: true,
          tenantVisible: true
      );
      // ... all other settings ...
      public static IReadOnlyList<SettingDefinition> All => [ TenantsMaxEvents, ... ];
      public static SettingDefinition? GetByKey(string key) => ...;
  }
  ```
  - **No reflection** — settings discovered via `SettingRegistry.All`
  - **Full type safety** — compile-time errors for missing definitions
  - **Easy to test** — just iterate `SettingRegistry.All` and validate
  - **Architecture test:** Every key in `GovernanceSettingKeys` MUST have a corresponding entry in `SettingRegistry.All`
  - **Reference:** Pattern used by Keycloak, Elasticsearch, Spring Boot
- **Effort:** M

---

### Phase 5: Tenant Provisioning Workflows

**Goal:** Support three models for creating tenants.

#### Task 5.1: Admin-Created Tenant (Async Provisioning)
- **Enhance:** `CreateTenantCommand` to:
  - Create tenant entity in `Provisioning` state
  - Auto-generate slug from tenant name (kebab-case, uniqueness check)
  - Set initial `TenantLifecycleLog` entry (null → Provisioning)
  - **Enqueue `ProvisionTenantJob` for background processing:**
    - Initialize tenant storage bucket
    - Apply default settings from system settings (including quota defaults)
    - Create initial admin user as `TenantMember` with `TenantAdmin` role
    - Validate DNS (if subdomain resolver active)
    - Transition to Active after all provisioning steps complete
  - Return TenantId immediately (don't wait for provisioning)
  - If path resolver active: slug = path segment; validate URL-safety; preview shows `/t/{slug}/events`
  - **Uses `TenantUrlBuilder`** for access URL generation
  - **Update `TenantSlugCache`** on successful creation
- **Background job:** `ProvisionTenantJob` via `BackgroundService` (Aspire-native — no Hangfire dependency)
  - Implements `IHostedService` / `BackgroundService` with a `Channel<TenantProvisionRequest>` queue
  - On success: Transition Provisioning → Active
  - On failure: Log error, tenant remains in Provisioning with error details
  - Retry logic with exponential backoff (3 attempts max)
  - Same pattern used for Deleting → Purged and Restoring → Active transitions
- **Effort:** L (upgraded from M due to async provisioning)

#### Task 5.2: Tenant Self-Registration Flow
- **File:** New `Explore.API/Controllers/TenantRegistrationController.cs`
- **What:** Public-facing registration endpoint (when `tenants.self_service_registration == true`):
  - `POST /api/tenant-registration` — body: `{ tenantName, slug, adminEmail }`
  - Validates slug availability and format (URL-safe, unique across all tenants)
  - Creates tenant in `Provisioning` state
  - Creates initial admin `TenantMember`
  - Returns tenant details with access URL:
    - If subdomain resolver: `{slug}.events.example.org`
    - If path resolver: `events.example.org/t/{slug}`
    - If both: returns both URLs
  - Optional: require email verification before activation
- **Governance gate:** `tenants.self_service_registration` must be true
- **Effort:** L
- **Skill:** `cqrs-mediatr-guidelines`

#### Task 5.3: Tenant Self-Registration UI
- **File:** New Blazor pages for multi-tenant instance portal
- **What:**
  - Instance portal page (`/`) shows: Welcome message, Login, Create Organization (if self-registration enabled)
  - "Create Organization" form: Organization name, desired slug (with live availability check), admin account
  - **Dynamic preview** based on active resolver:
    - Subdomain: `{slug}.events.example.org`
    - Path: `events.example.org/t/{slug}`
  - Post-creation redirect to new tenant URL
- **Effort:** L
- **Skill:** `blazor-ui-conventions`

#### Task 5.4: Invite-Based Tenant Creation (Future/P2)
- **File:** Extend `TenantInvitation` entity (already exists in domain)
- **What:**
  - Instance admin creates invitation with email
  - Invited user receives link, completes registration
  - Tenant created in Provisioning state
  - Admin approves → Active
- **Effort:** L (deferred to later sprint)

---

### Phase 6: Root Domain & Instance Portal (Multi-Tenant Mode)

**Goal:** When multi-tenant mode is active, the root domain serves as an instance portal.

#### Task 6.1: Instance Portal Landing Page
- **File:** New `Explore.Blazor.Client/Pages/Portal/InstancePortal.razor`
- **What:** Minimal, neutral portal page:
  - Instance name/branding (from governance settings)
  - Login button
  - "Create Organization" button (if self-registration enabled)
  - "Browse Organizations" (if public directory enabled, future)
  - "Powered by ISLAMU Event" footer
- **Route:** `/` (only active when `DeploymentMode == MultiTenant` and no tenant resolved)
- **Effort:** M

#### Task 6.2: Tenant Domain Routing Middleware
- **File:** Enhance routing in `Explore.Blazor/` (**Blazor Web App — browser-facing**)
- **What:** When a request arrives on the base domain (not a tenant subdomain):
  - If single-tenant → serve tenant app (event list)
  - If multi-tenant → serve instance portal
  - If tenant subdomain → resolve tenant and serve tenant app
  - If instance admin subdomain (e.g., `platform.`) → serve instance admin
- **Effort:** M

#### Task 6.3: Instance Admin Context Banner
- **File:** New component `Explore.Blazor.Client/Shared/Components/InstanceAdminBanner.razor`
- **What:** When an instance admin visits a tenant domain:
  - Show a thin top banner: "You are viewing [Tenant Name] as Instance Administrator"
  - "Go to Platform Admin" link
  - Dismissible per session
- **Effort:** S

---

### Phase 7: Multi-Tenant → Single-Tenant Revert

**Goal:** Allow reverting to single-tenant mode when only one tenant exists.

#### Task 7.1: Revert Validation & API
- **File:** Extend deployment mode switch endpoint
- **What:**
  - `POST /api/instance/deployment-mode` with `{ mode: "SingleTenant" }`
  - Validation: `GetActiveTenantCountQuery` must return 1
  - If >1 active tenants: return 400 with message listing tenants that must be deleted/archived
  - On success: update `deployment.mode`, clear resolver configs, invalidate cache
- **Effort:** S

#### Task 7.2: Revert Confirmation UI
- **File:** Instance admin settings
- **What:** "Revert to Single-Tenant" button in platform settings:
  - Pre-check: API call to verify tenant count
  - If multiple tenants: show list with "delete" action per tenant
  - If one tenant: confirmation dialog with warning
  - On confirm: switch mode, redirect to tenant admin
- **Effort:** M

---

### Phase 8: Testing & Documentation

#### Task 8.1: Unit Tests
- **Project:** `Event.Application.UnitTests`
- **What:**
  - `TransitionTenantStatusCommand` handler — valid/invalid transitions (including `Deleting`/`Restoring` states)
  - `DeploymentMode` switch command — validation logic
  - `SettingRegistry` — all keys have definitions (via `SettingRegistry.All`)
  - `TenantRegistration` command — slug validation, governance gate check
  - `TenantUrlBuilder` — URL generation for each resolver mode (path, subdomain, custom domain, single-tenant)
  - **Quota enforcement:** Test that commands reject when quota exceeded
  - **ITenantResolver implementations:** Each resolver correctly extracts tenant from its source
  - **TenantSlugCache:** Populate, lookup, invalidation on CRUD
  - **ProvisionTenantJob:** Success and failure paths
- **Effort:** XL (upgraded from L — many new components)

#### Task 8.2: Integration Tests
- **Project:** `Event.API.IntegrationTests`
- **What:**
  - Deployment mode switch API (ST → MT, MT → ST with tenant count checks)
  - Tenant lifecycle transitions (full state machine: create → active → suspended → archived → deleting → purged)
  - Tenant lifecycle: restoring (archived → restoring → active)
  - Settings cascade (locked vs unlocked, tenant override behavior)
  - `BlockInSingleTenant` filter behavior
  - Tenant registration endpoint (enabled/disabled governance gate)
  - Impersonation endpoints (start/stop, audit log verification)
  - **Quota enforcement integration:** Create until quota hit, verify 400 response
  - **Dynamic CORS:** Verify allowed origins update when tenant domains change
  - **TenantGuardInterceptor:** Verify exception on missing TenantId
  - **Async provisioning:** Verify tenant transitions from Provisioning → Active via background job
- **Effort:** XL (upgraded from L)

#### Task 8.3: Architecture Tests
- **Project:** `Event.Architecture.Tests`
- **What:**
  - All ITenantEntity implementations have TenantId
  - All governance setting keys have corresponding `SettingDefinition` in `SettingRegistry.All` (registry-based, not attribute-based)
  - All instance-admin-only endpoints have `[BlockInSingleTenant]` or role check
  - **Cross-tenant protection:** Every repository query method that accesses tenant data must use EF query filters (verify `IgnoreQueryFilters()` is only in instance-admin contexts)
  - **Quota gate presence:** Architecture test verifying quota-related commands include quota check call
  - **ITenantResolver registration:** All concrete `ITenantResolver` implementations are registered in DI
- **Effort:** M (upgraded from S)

#### Task 8.4: Routing & Resolver Tests
- **Project:** New `Event.API.IntegrationTests/Routing/` folder
- **What:** Test all resolver permutations:
  - **Path resolver:** `/t/islamu/events` resolves to correct tenant + strips prefix
  - **Path resolver edge cases:** `/t` → 404, `/t/` → 404, `/t//events` → 404, `/teams` → NOT path-resolved
  - **Subdomain resolver:** `islamu.events.example.org/events` resolves correctly
  - **Header resolver:** `X-Tenant-Id: {guid}` resolves correctly
  - **Custom domain resolver:** `events.islamu.org` resolves correctly
  - **ITenantResolver pipeline:** Test pipeline ordering, first-match-wins behavior
  - **TenantSlugCache integration:** Verify cache hits, cache misses with DB fallback, cache invalidation
  - **Reserved slug rejection:** Verify `admin`, `instance`, etc. are rejected
  - **Configurable order:** Test that per-resolver enable/disable toggles correctly include/exclude resolvers from pipeline
  - **TenantUrlBuilder:** Generated URLs match active resolver mode
- **Effort:** L (upgraded from M due to expanded coverage)

#### Task 8.5: Documentation Updates
- **Files:** `docs/MULTI_TENANCY.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`
- **What:**
  - Document resolver configuration governance keys
  - Document `ITenantResolver` pipeline and how to add custom resolvers
  - Document `TenantSlugCache` and cache invalidation behavior
  - Document tenant provisioning workflows (async via background jobs)
  - Document DNS setup requirements for self-hosters
  - Update settings cascade documentation with `SettingRegistry` usage
  - Document `TenantUrlBuilder` usage for Blazor components
  - Document lifecycle state machine with transitional states
  - Document resolver telemetry fields for SaaS operators
  - Document dynamic CORS configuration for self-hosters
  - Document BFF architecture (WASM → Blazor Server → YARP → API)
- **Effort:** L (upgraded from M due to expanded scope)

---

## Risk Assessment

### High Risk

| Risk | Mitigation |
|------|------------|
| **Resolver config circular dependency** | `ResolverConfigService` reads from `SystemSetting` ONLY — never via `SettingsResolver`. Tenant resolution is completely tenant-independent. Cache aggressively. |
| **DNS complexity for self-hosters** | Path-based resolver (`/t/{slug}`) requires ZERO DNS config — offer as default/simplest option. DNS guide for subdomain users. Verification is optional. |
| **Cache invalidation on mode switch** | Mode switch clears all settings caches; add integration test verifying cache invalidation |
| **Tenant data isolation after mode switch** | EF query filters remain active regardless of mode; integration tests verify isolation |

### Medium Risk

| Risk | Mitigation |
|------|------------|
| **Subdomain SSL certificates** | Document wildcard cert setup; recommend Caddy/Traefik for auto-SSL; provide Let's Encrypt DNS-01 guide |
| **Custom domain DNS verification** | DNS diagnostics page instead of blocking verification; TXT record verification for custom domains |
| **Settings metadata drift** | Strongly-typed `SettingRegistry` — architecture test enforces every key has a `SettingDefinition` entry. No reflection. |
| **Path resolver URL rewriting** | Middleware only activates on `/t/` prefix — no skip list, no infrastructure coupling. `TenantUrlBuilder` handles link generation. Integration tests. |
| **Impersonation security** | Impersonation is session-scoped, read-only by default, fully audited (user_id + tenant_id + timestamp per request), instance admin only |

### Low Risk

| Risk | Mitigation |
|------|------------|
| **Self-registration abuse** | Rate limiting on registration endpoint; optional CAPTCHA; admin approval mode |
| **Instance admin losing access** | Self-lockout prevention (already in auth provider config); ensure mode switch preserves admin context |
| **Quota enforcement gaps** | 3-layer enforcement: command handlers (block), UI indicators (prevent), background reconciliation (verify). Architecture test ensures presence. |

---

## Performance Considerations

### Required Database Indexes

```sql
-- Tenant resolution performance (critical for every request)
CREATE INDEX IX_Tenant_Slug ON Tenants (Slug) WHERE IsDeleted = false;
CREATE INDEX IX_Tenant_CustomDomain ON Tenants (CustomDomain) WHERE CustomDomain IS NOT NULL AND IsDeleted = false;
CREATE INDEX IX_Tenant_TenantId ON Tenants (Id);

-- Tenant-scoped queries (critical for data isolation)
-- Every table with TenantId MUST have this index:
CREATE INDEX IX_{Table}_TenantId ON {Table} (TenantId);

-- Settings resolution performance
CREATE INDEX IX_SystemSetting_Key ON SystemSettings (Key);
CREATE INDEX IX_TenantSetting_TenantId_Key ON TenantSettings (TenantId, Key);

-- Lifecycle audit
CREATE INDEX IX_TenantLifecycleLog_TenantId ON TenantLifecycleLogs (TenantId, CreatedAt DESC);
```

### Caching Strategy
- **TenantSlugCache (hot path):** In-memory `ConcurrentDictionary<string, Guid>` for slug→TenantId and domain→TenantId. Populated at startup, updated on tenant CRUD events. **Zero DB queries for tenant resolution on cache hit.** This is the primary performance optimization.
- **Resolver config:** Cache `SystemSetting` (resolver method, base domain) for 5 min (reloads on mode switch)
- **Settings cascade:** Existing 5-min TTL in `SettingsResolver` — adequate for most settings
- **Dynamic CORS origins:** Cached set of allowed origins, invalidated on base domain / tenant custom domain changes

---

## Potential Risks & Unknowns

The **most likely complexity point** is Phase 2 (ITenantResolver Pipeline + Split TenantContext + TenantSlugCache).
Splitting the monolithic `TenantContext` into three components (`TenantResolverService`, `TenantContextAccessor`,
`TenantContext`) is a significant refactor that touches every consumer of `TenantContext` in the codebase.
The migration must be systematic — change the interface, update all injection sites, verify tests pass.

**Cross-service coordination (Blazor ↔ API):** The fact that resolvers live in two different services
means the `ITenantResolver` pipeline is registered differently in each host. The Blazor app registers
path/subdomain/domain resolvers; the API registers only the header resolver. The shared infrastructure
(`TenantSlugCache`, `TenantContextAccessor`, `TenantContext`) must be in `Explore.Infrastructure` to
be available to both. This is more complex than a monolith but architecturally correct.

**YARP tenant propagation:** The updated `ForwardTenantHeader` must read from `HttpContext.Items` first
(set by Blazor-side resolver middleware) before falling back to incoming headers. This is a small code
change but critically important — without it, the API never receives the tenant resolved by path/subdomain.
Test this flow end-to-end: path resolution → YARP forwarding → API header reading.

**Blazor Server circuit affinity:** The `TenantCircuitHandler` approach is standard but must be tested
carefully. Edge cases: circuit drops and reconnects to a different path (user bookmarked a different tenant
URL), circuit reconnects after the tenant was suspended, SSR pre-rendering vs interactive mode differences.

The path-based resolver is greatly simplified — only activates on `/t/` prefix, no skip list.
But URL generation via `TenantUrlBuilder` must be thoroughly tested to ensure every Blazor component
uses `TenantUrlBuilder.NavigateTo()` instead of raw `NavigationManager.NavigateTo()`. Missing this
creates broken links in path-resolver mode.

The `TenantSlugCache` is critical for performance but introduces cache consistency concerns. If a tenant
is created/updated/deleted and the cache isn't invalidated, stale resolution results. The cache update
must be transactional with the database write — consider using domain events for invalidation.

**Dynamic CORS** for self-hosters is architecturally simple (delegate-based `SetIsOriginAllowed()`)
but the BFF pattern means CORS is mostly irrelevant for the primary Blazor workflow. Document clearly
that CORS only matters for **direct API access** (third-party consumers, mobile apps, Swagger).

The `TenantGuardInterceptor` is defense-in-depth but could produce false positives if entity creation
code sets `TenantId` after calling `Add()` to the context. Ensure all entity factories set `TenantId`
during construction. The interceptor should clearly identify which entity and property triggered the error.

**Strongly-typed SettingDefinition registry** eliminates reflection but requires discipline:
when adding a new governance key, developers must add both the key constant AND the SettingDefinition.
The architecture test catches this, but it's a compile-time-hidden dependency (only caught at test time).

**Async tenant provisioning** via `BackgroundService` with `Channel<T>` queue. The UI must handle the
`Provisioning` state gracefully — show progress indicators, retry failed provisioning, and allow manual
promotion to Active if provisioning is stuck. `BackgroundService` is Aspire-native and requires no
additional dependencies. Same pattern for Deleting/Restoring transitions.

**Transitional lifecycle states** (`Deleting`, `Restoring`) require background processing to complete.
If the job fails mid-purge, the tenant remains in `Deleting` state indefinitely — need retry logic
and manual admin override to force-complete transitions. **EF Core migration required** for new
`TenantStatus` seed data rows.

**EF Core migration:** Adding `Deleting(6)` and `Restoring(7)` to `TenantStatusEnum` requires a
migration to seed the new `TenantStatus` lookup rows. Run `dotnet ef migrations add AddTransitionalTenantStatuses`
in the Persistence project.

---

## Success Metrics

1. **Single-tenant users** see zero multi-tenancy UI artifacts (except mode indicator badge)
2. **Path-based resolver** allows full multi-tenant operation with ZERO DNS configuration (only `/t/` prefix)
3. **Multi-tenant activation** takes < 10 minutes including DNS setup (with guide) or < 2 minutes with path resolver
4. **Tenant provisioning** (admin-created) returns immediately; background job completes in < 30 seconds
5. **Self-registration** (when enabled) creates a usable tenant in < 60 seconds
6. **Settings governance** changes propagate within 5 minutes (cache TTL)
7. **Tenant resolution** uses zero DB queries on cache hit (TenantSlugCache)
8. **Tenant impersonation** works without page reload
9. **Tenant quotas** block over-limit operations with clear error messages
10. **Every GovernanceSettingKeys constant** has a `SettingDefinition` in `SettingRegistry.All` (enforced by architecture test)
11. **All ITenantResolver implementations** are registered and testable independently
12. **TenantGuardInterceptor** catches missing TenantId on all tenant-scoped entity saves
13. **Dynamic CORS** allows self-hosters to configure arbitrary domains without code changes
14. **All existing tests pass** after implementation
15. **No backwards compatibility needed** (development phase — clean breaks allowed)

---

## Dependencies

| Dependency | Required By | Status |
|------------|-------------|--------|
| Existing Tenant CRUD | Phase 1, 3 | ✅ Available |
| SettingsResolver | Phase 4 | ✅ Available |
| TenantContext | Phase 2, 6 | ✅ Available (needs split into 3 components + ITenantResolver pipeline + TenantSlugCache) |
| GovernanceSettingKeys | Phase 2, 4 | ✅ Available (needs new keys + `SettingDefinition` entries in `SettingRegistry`) |
| Keycloak integration | Phase 5 (admin user creation) | ✅ Available |
| MudBlazor | Phase 2, 3, 4, 5, 6 | ✅ Available |
| URL rewriting middleware | Phase 2.5 (path resolver) | 🔴 New (prefix-only `/t/` matching) |
| TenantUrlBuilder | Phase 2.7 (URL generation) | 🔴 New (centralized tenant-aware URL generation) |
| ITenantResolver interface | Phase 2.4 (extensible pipeline) | 🔴 New (4 concrete implementations) |
| TenantSlugCache | Phase 2.4 (in-memory cache) | 🔴 New (ConcurrentDictionary-based) |
| TenantGuardInterceptor | Phase 2.8 (cross-tenant safety) | 🔴 New (EF Core SaveChangesInterceptor) |
| Dynamic CORS | Phase 2.9 (self-hoster support) | 🔴 New (SetIsOriginAllowed delegate) |
| SettingDefinition + SettingRegistry | Phase 4.3 (settings metadata) | 🔴 New (replaces attribute-based approach) |
| Background job infrastructure | Phase 5.1 (async provisioning) | ⚠️ Needs setup (Aspire/Hangfire) |
| Session/circuit state | Phase 3.5 (impersonation) | ✅ Available (ASP.NET Core sessions) |
| Serilog + OpenTelemetry | Phase 2 (resolver telemetry) | ✅ Available (configured in ServiceDefaults) |
| BusinessMetrics | Phase 2 (resolver counters) | ✅ Available (`Explore.Business` meter) |
| YARP BFF Proxy | Phase 2.9 (CORS context) | ✅ Available (WASM→BFF same-origin) |

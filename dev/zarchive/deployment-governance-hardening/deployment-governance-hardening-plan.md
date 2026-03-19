# Deployment Mode & Governance Hardening — Enterprise Plan

> **Architectural hardening of deployment mode resolution, tenant governance,
> settings cascade, transactional onboarding, and distributed cache.**
>
> No backwards compatibility. No legacy shims. No half-measures.

**Last Updated: 2026-03-17**

---

## Executive Summary

The ISLAMU platform's **role hierarchy** (`RoleScopeEnum`, `PlatformUserRole`,
`TenantMember`), **settings cascade** (`SystemSetting` + `TenantSetting` with lock
semantics), and **authorization pipeline** (Cerbos + `AdminContext` + `CerbosPrincipalBuilder`)
are well-designed at the data model level — genuine enterprise-grade abstractions.

The runtime plumbing that connects them is not. Four failure modes will prevent this
system from operating in any multi-node, zero-downtime environment:

1. **Sync-over-async** in middleware and auth filters — deadlock risk under load
2. **Static volatile cache** — leaks across test hosts, impossible to scope, no TTL
3. **Non-atomic onboarding** — 7+ DB writes with no transaction; partial failure leaves
   inconsistent state that bypasses all auth checks
4. **Two settings systems** — legacy `TenantSettings` entity and governance cascade
   coexist, creating split-brain on tenant policy reads

This plan eliminates all four failure modes and adds the distributed cache infrastructure
needed for HA deployments.

---

## Research Findings

### IOptionsMonitor vs IOptions (Confirmed)

`IOptionsMonitor<T>` is **singleton-safe** and reads `CurrentValue` on every call,
reflecting live `appsettings.json` changes when the config provider has
`reloadOnChange: true`. This is the correct choice for `DeploymentSettings` injected
into a Singleton provider — it allows a DevOps engineer to override the mode via
environment variable without a restart.

`IOptions<T>` is captured at registration time. Wrong for singletons that need to
react to environment variable overrides.

### Outbox Pattern for Side Effects (Confirmed)

EF Core's `SaveChangesInterceptor` with `SavingChangesAsync` override writes outbox
messages **inside the same database transaction** as the business entities. A background
worker then processes the outbox independently. This is the only safe pattern for:

- Cache invalidation after onboarding
- Setup secret locking after onboarding
- Any "fire after commit" side effect

The onboarding handler must **not** directly call `InvalidateCache()` or
`_setupSecretProvider.Lock()` inside the same unit of work. These must be post-commit
side effects driven by an outbox worker.

### Redis via Aspire (Confirmed)

Package: `Aspire.StackExchange.Redis.DistributedCaching` v13.1.2 (latest stable Feb 2026).
Wires `IDistributedCache` to Redis with health checks, logging, and telemetry via
`.AddRedisDistributedCache(connectionName: "cache")`. AppHost uses `.AddRedis("cache")`.
Zero manual plumbing for local dev — Aspire spins up the container automatically.

---

## Current State Analysis

### What to Preserve

| Component | Location | Why Keep |
|-----------|----------|----------|
| `RoleScopeEnum` + `RoleEnum` disjoint ranges | `Explore.Domain/Enums/` | Clean hierarchical model |
| `PlatformUserRole` + `TenantMember` | `Explore.Domain/` | Correct join table design |
| `AdminContext` with 5-min sliding cache | `Explore.Infrastructure/Identity/` | DB-first authority, good TTL balance |
| `CerbosAuthorizationService` + `FallbackAuthorizationService` | `Explore.Infrastructure/Services/` | Provider-agnostic PDP |
| `SystemSetting` + `TenantSetting` + `IsLocked` | `Explore.Domain/` | Sound cascade data model |
| `SettingsResolver` with `SettingSource` enum | `Explore.Infrastructure/Services/` | Correct cascade resolution |
| EF Core named query filters | `Explore.Persistence/ExploreDbContext.cs` | Clean tenant isolation |

### What to Fix

| # | Problem | Root Cause | Risk |
|---|---------|------------|------|
| 1 | `ApiTenantResolutionMiddleware.IsSingleTenantMode()` calls `.GetAwaiter().GetResult()` | Synchronous DB call inside async middleware | Thread pool starvation under load |
| 2 | `BlockInSingleTenantAttribute` has nested `DeploymentModeResolver` querying DB on every request | No caching, sync-over-async | Per-request DB hit, deadlock risk |
| 3 | `_cachedBootstrapDeploymentMode` is `static volatile` with no TTL | Static mutable state outside DI | Test cross-contamination, no multi-instance propagation |
| 4 | Two deployment mode resolution paths (middleware vs filter) can disagree | No shared abstraction | Middleware allows request, filter blocks it |
| 5 | `IDeploymentModeCacheInvalidator?` is nullable in onboarding handler | Optional dependency | Silent failure if not registered |
| 6 | `SetupSecretProvider.IsBootstrapComplete()` uses `.GetAwaiter().GetResult()` | Double-checked locking with sync DB | Thread pool starvation |
| 7 | No EF Core transaction in `CompleteInstanceOnboardingCommandHandler` | 7+ independent DB writes | Partial failure leaves auth state inconsistent |
| 8 | Cache invalidation + `SetupSecretProvider.Lock()` called mid-flow | Side effects before commit | If later writes fail, cache is wrong |
| 9 | No unique constraint on `InstanceBootstrapState.IsCompleted = true` | Missing DB constraint | Concurrent onboarding creates two completed records |
| 10 | `TenantSettings` entity and governance cascade are parallel systems | No migration from structured → cascade | Dual maintenance, split-brain risk |
| 11 | No multi-instance cache propagation | `IMemoryCache` is per-process | Mode change invisible to other replicas |

---

## Target Architecture

### Deployment Mode Resolution Flow (After)

```
Request arrives
      │
      ▼
ApiTenantResolutionMiddleware.InvokeAsync()
      │
      ▼
await _deploymentModeProvider.IsSingleTenantAsync()
      │
      ├─► IOptionsMonitor<DeploymentSettings>.CurrentValue.IsSingleTenant
      │         └─ true? → short-circuit return (env var / appsettings wins)
      │
      ├─► IDistributedCache.GetStringAsync("DeploymentMode:Current")
      │         └─ hit? → deserialize, return (TTL: 30s)
      │
      └─► IInstanceBootstrapStateRepository.GetCurrent() [async, scoped]
                └─ persist to IDistributedCache (30s TTL)
                └─ return mode
```

### Onboarding Commit Flow (After)

```
CompleteInstanceOnboardingCommandHandler.Handle()
      │
      ├─ BeginTransactionAsync()
      │     ├─ EnsureDefaultTenantAsync()
      │     ├─ CreateOnboardingUserAsync()
      │     ├─ ApplySettingsAsync()
      │     ├─ EnsurePlatformAdministratorRoleAsync()
      │     ├─ EnsureDefaultTenantAdministratorAsync()
      │     ├─ CreateBootstrapStateAsync()
      │     └─ SaveChangesAsync() ← all or nothing
      │
      └─ CommitTransactionAsync()
            │
            └─ Post-commit side effects (via Outbox or direct)
                  ├─ _deploymentModeProvider.InvalidateCache()
                  ├─ _adminCacheInvalidator.InvalidateUser()
                  └─ _setupSecretProvider.Lock()
```

---

## Implementation Phases

---

### Phase 0: API Contract Redesign

**Goal**: Replace god-object DTOs and split-purpose controllers with clean, resource-oriented
contracts that accurately reflect the system's domain. No backwards compatibility — the UI
is updated in lockstep.

**Why Phase 0 (before the runtime fixes)**: The current API contracts were shaped by the
bad implementation. `InstanceGovernanceSettingsDto` has 66 properties because the
onboarding handler has 66 things to do in one shot. `DeploymentMode` is a `string`
because the old handler never parsed it to an enum. Fixing the runtime without fixing
the contract means the new clean internals are still exposed through a broken surface.
Phase 0 establishes the target API shape so Phases 1–5 implement against correct types.

---

#### Task 0.1 — Split `InstanceGovernanceSettingsDto` into Sub-Resource DTOs

- **Delete**: `Explore.Application/DTOs/Onboarding/InstanceGovernanceSettingsDto.cs` (66 properties)
- **Create**: Eight focused DTOs in `Explore.Application/DTOs/Instance/`

| New DTO File | Purpose | Key Properties |
|---|---|---|
| `DeploymentModeDto.cs` | Read-only current mode | `Mode: DeploymentMode` (enum), `DefaultTenantId: Guid?` |
| `ModuleSettingsDto.cs` | Feature flags per module | `EventsEnabled`, `GroupsEnabled`, `OrganizationsEnabled`, `FederationEnabled` |
| `EventPolicyDto.cs` | Event lifecycle policy | `RequireApproval`, `DefaultPublishingPolicy` |
| `OrganizationPolicyDto.cs` | Org registration policy | `AllowSelfRegistration`, `RequireVerification` |
| `BrandingSettingsDto.cs` | Instance identity | `InstanceName`, `LogoUrl`, `PrimaryColor` |
| `DomainSettingsDto.cs` | Allowed domains + proxy | `AllowedDomains: string[]`, `EnabledAuthProviders: string[]` |
| `TenantDelegationSettingsDto.cs` | Per-setting override locks | `CanOverrideEventPolicy`, `CanOverrideOrgPolicy`, `CanOverrideModules` |
| `RenderPolicySettingsDto.cs` | UI render preset | `Preset: RenderPolicyPreset` (enum) |

```csharp
// ABOUTME: Focused DTO for instance deployment mode state — replaces DeploymentMode string
// ABOUTME: property in the former 66-property InstanceGovernanceSettingsDto god object.

public sealed record DeploymentModeDto(
    DeploymentMode Mode,      // enum, not string
    Guid? DefaultTenantId);
```

- **Acceptance Criteria**:
  - [ ] All 8 DTOs created in `Explore.Application/DTOs/Instance/`
  - [ ] `DeploymentMode` is `DeploymentMode` enum type (not `string`) in all new DTOs
  - [ ] `RenderPolicyPreset` is typed enum (not `string`)
  - [ ] `SmtpSecurityMode` is typed enum where applicable (not `string`)
  - [ ] No DTO has more than 12 properties
  - [ ] Build passes
- **Effort**: M

---

#### Task 0.2 — Redesign `CompleteInstanceOnboardingRequest`

- **Replace**: `InstanceGovernanceSettingsDto` as the onboarding payload (66 props)
- **Create**: `Explore.Application/DTOs/Onboarding/CompleteInstanceOnboardingRequest.cs`

Onboarding **only** sets what cannot be changed later through the admin UI.
Everything else is settable post-onboarding via sub-resource endpoints.

```csharp
// ABOUTME: Minimal onboarding payload — captures only the decisions made during first-run wizard.
// ABOUTME: All other settings are configurable post-onboarding via instance admin endpoints.

public sealed record CompleteInstanceOnboardingRequest(
    DeploymentMode DeploymentMode,   // enum — the only irreversible choice
    string AdminEmail,
    string AdminPassword,
    string? InstanceName);           // optional branding — can change post-setup
```

- **Acceptance Criteria**:
  - [ ] Request has ≤6 properties (only first-run-irreversible decisions)
  - [ ] `DeploymentMode` is enum not string
  - [ ] Validator: `CreateInstanceOnboardingRequestValidator` validates `DeploymentMode` is a valid enum value
  - [ ] `CompleteInstanceOnboardingCommand` updated to use this request type
- **Effort**: S
- **Depends on**: 0.1

---

#### Task 0.3 — Split `InstanceOnboardingController` into Wizard + Admin Controllers

- **Rename/Split**: `Explore.API/Controllers/InstanceOnboardingController.cs` (21 endpoints)

The current controller mixes two distinct concerns:
1. **First-run wizard** (one-time setup, `IsCompleted` gate)
2. **Ongoing instance admin** (runtime settings management, no wizard gate)

**Create**: `Explore.API/Controllers/InstanceOnboardingController.cs` — wizard only

```
POST   /api/instance/onboarding/complete     CompleteInstanceOnboarding
GET    /api/instance/onboarding/status       GetOnboardingStatus
```

**Create**: `Explore.API/Controllers/InstanceSettingsController.cs` — admin sub-resources

```
GET    /api/instance/settings/modules                GetModuleSettings
PUT    /api/instance/settings/modules                UpdateModuleSettings
GET    /api/instance/settings/events                 GetEventPolicy
PUT    /api/instance/settings/events                 UpdateEventPolicy
GET    /api/instance/settings/organizations          GetOrganizationPolicy
PUT    /api/instance/settings/organizations          UpdateOrganizationPolicy
GET    /api/instance/settings/branding               GetBrandingSettings
PUT    /api/instance/settings/branding               UpdateBrandingSettings
GET    /api/instance/settings/domains                GetDomainSettings
PUT    /api/instance/settings/domains                UpdateDomainSettings
GET    /api/instance/settings/tenant-delegation      GetTenantDelegationSettings
PUT    /api/instance/settings/tenant-delegation      UpdateTenantDelegationSettings
GET    /api/instance/settings/render-policy          GetRenderPolicySettings
PUT    /api/instance/settings/render-policy          UpdateRenderPolicySettings
GET    /api/instance/settings/deployment-mode        GetDeploymentMode
```

Each endpoint maps to a focused query/command using the new sub-resource DTOs from 0.1.

- **Acceptance Criteria**:
  - [ ] `InstanceOnboardingController` has exactly 2 endpoints (status + complete)
  - [ ] `InstanceSettingsController` has RESTful sub-resource endpoints per domain
  - [ ] No `GET /api/instance/governance/settings` god endpoint returning 66 props
  - [ ] `[BlockInSingleTenant]` applied correctly per endpoint (wizard blocked in MT, admin always allowed)
  - [ ] `[Authorize(Roles = PlatformAdmin)]` on all `InstanceSettingsController` write endpoints
  - [ ] HATEOAS `RouteNames` updated
- **Effort**: M
- **Depends on**: 0.1

---

#### Task 0.4 — Fix `TenantPolicySettingsDto` Read/Write Separation

- **File**: `Explore.Application/DTOs/Onboarding/TenantPolicySettingsDto.cs`

Current problem: `CanOverride*` properties are read-only flags derived from instance
governance locks. They are returned on GET but should never appear on PUT bodies —
they're computed, not settable.

**Create**:
- `Explore.Application/DTOs/TenantPolicy/TenantPolicyDto.cs` — GET response (includes `CanOverride*` flags)
- `Explore.Application/DTOs/TenantPolicy/UpdateTenantPolicyRequest.cs` — PUT body (writable fields only)

```csharp
// ABOUTME: Read model for tenant policy — includes effective values AND capability flags from
// ABOUTME: instance governance locks. CanOverride* fields are read-only; use UpdateTenantPolicyRequest to write.

public sealed record TenantPolicyDto(
    bool EventsRequireApproval,
    bool AllowSelfRegistration,
    bool RequireOrganizationVerification,
    // ... writable fields ...
    bool CanOverrideEventPolicy,        // read-only: set by instance admin
    bool CanOverrideOrgPolicy,          // read-only: set by instance admin
    bool CanOverrideGroups);            // read-only: set by instance admin

public sealed record UpdateTenantPolicyRequest(
    bool? EventsRequireApproval,
    bool? AllowSelfRegistration,
    bool? RequireOrganizationVerification);
    // NO CanOverride* — not writable by tenant admin
```

- **Acceptance Criteria**:
  - [ ] GET response includes `CanOverride*` flags
  - [ ] PUT body does not include `CanOverride*` flags
  - [ ] `TenantOnboardingController` updated to use split DTOs
  - [ ] Validator for `UpdateTenantPolicyRequest` created
- **Effort**: S
- **Depends on**: 0.1

---

#### Task 0.5 — Delete `TenantSettingsController`

- **File**: `Explore.API/Controllers/TenantSettingsController.cs`

This controller exposes CRUD for the legacy `TenantSettings` entity. That entity is
being deleted in Phase 4. The correct read/write path after Phase 4 is:
- Read: `ISettingsResolver.GetSettingAsync<T>()` → tenant policy via `TenantPolicyDto`
- Write: `TenantOnboardingController` or future `TenantSettingsController` using governance cascade

**Action**: Mark controller `[Obsolete]` now (so compilation surfaces all callers), then
delete in the same commit as Phase 4.5.

- **Acceptance Criteria**:
  - [ ] `[Obsolete("Replaced by governance cascade. Remove with TenantSettings entity in Phase 4.")]`
    added to controller class
  - [ ] All Blazor service callers identified (preparation for Phase 4 Blazor update)
  - [ ] Zero new usages introduced after this task
- **Effort**: S

---

#### Task 0.6 — Update Blazor Clients for New Contracts

- **Files**: `Explore.Blazor.Client/Clients/EventApiClient.g.cs` (generated), all related services

The generated API client (`EventApiClient.g.cs`) must be regenerated from the updated
`swagger.json` after controller changes. All Blazor services consuming the old god-object
DTO must be updated to call the new sub-resource endpoints.

**Key Blazor changes**:
- `InstanceOnboardingService.cs` — split calls to new `InstanceOnboardingController` and `InstanceSettingsController`
- `AdminService.cs` — update settings read/write calls to sub-resource endpoints
- Instance admin pages — update `@bind` targets to use sub-resource DTOs
- `CompleteInstanceOnboarding` call — send `CompleteInstanceOnboardingRequest` (minimal, not 66 props)

- **Acceptance Criteria**:
  - [ ] `swagger.json` regenerated from updated API
  - [ ] `EventApiClient.g.cs` regenerated (NSwag or Kiota)
  - [ ] All Blazor integration tests pass
  - [ ] Instance admin pages: Modules, Events, Organizations, Branding sections each make targeted sub-resource calls
- **Effort**: L
- **Depends on**: 0.3, 0.4

---

### Phase 1: Unified Deployment Mode Provider

**Goal**: Single async-safe service owns all deployment mode resolution logic.
All consumers (middleware, filters, handlers) call this one interface.

---

#### Task 1.1 — Create `IDeploymentModeProvider` Interface

- **File**: `Explore.Application/Contracts/Services/IDeploymentModeProvider.cs`
- **ABOUTME line**: Unified contract for deployment mode resolution, replaces split paths in middleware and filters.

```csharp
// ABOUTME: Unified contract for async-safe deployment mode resolution across middleware and filters.
// ABOUTME: Replaces static volatile cache and inline DB queries with a single shared provider.

namespace Explore.Application.Contracts.Services;

public interface IDeploymentModeProvider
{
    /// <summary>
    /// Returns the current effective deployment mode.
    /// Resolution order: static config → distributed cache → database.
    /// </summary>
    Task<DeploymentMode> GetCurrentModeAsync(CancellationToken ct = default);

    /// <summary>Returns true when the current mode is SingleTenant.</summary>
    Task<bool> IsSingleTenantAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes the cached mode. The next call re-reads from the database.
    /// Call this after a deployment mode change has been committed.
    /// </summary>
    Task InvalidateCacheAsync();
}
```

- **Acceptance Criteria**:
  - [ ] Interface lives in `Application` layer (zero `Infrastructure`/`API` references)
  - [ ] `InvalidateCacheAsync` is `Task` (not void) — async-safe for distributed cache
  - [ ] Replaces both `IDeploymentModeCacheInvalidator` and inline `DeploymentModeResolver`
- **Effort**: S
- **Skill**: `clean-architecture-rules`

---

#### Task 1.2 — Implement `DeploymentModeProvider`

- **File**: `Explore.Infrastructure/Services/DeploymentModeProvider.cs`
- **Registration**: Singleton in `InfrastructureServicesRegistration.cs`
- **ABOUTME line**: Singleton provider resolving deployment mode from config, distributed cache, then DB.

**Resolution algorithm**:
1. `IOptionsMonitor<DeploymentSettings>.CurrentValue.IsSingleTenant` → `true`? Return `SingleTenant` immediately. No cache write needed — config is the source of truth for locked environments.
2. `IDistributedCache.GetStringAsync(CacheKey)` → hit? Deserialize and return.
3. `IServiceScopeFactory.CreateScope()` → resolve `IInstanceBootstrapStateRepository` (scoped) → `GetCurrent()` async.
4. Write result to `IDistributedCache` with 30-second absolute expiry.
5. Return resolved mode (fallback: `MultiTenant`).

```csharp
// ABOUTME: Singleton deployment mode provider using IOptionsMonitor + IDistributedCache + DB fallback.
// ABOUTME: Replaces the static volatile cache in ApiTenantResolutionMiddleware with a proper DI-managed pattern.

namespace Explore.Infrastructure.Services;

public sealed class DeploymentModeProvider : IDeploymentModeProvider
{
    internal const string CacheKey = "DeploymentMode:Current";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IOptionsMonitor<DeploymentSettings> _settings;
    private readonly IDistributedCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeploymentModeProvider> _logger;

    // ... constructor ...

    public async Task<DeploymentMode> GetCurrentModeAsync(CancellationToken ct = default)
    {
        // Layer 1: static config (env var / appsettings) — highest priority
        if (_settings.CurrentValue.IsSingleTenant)
            return DeploymentMode.SingleTenant;

        // Layer 2: distributed cache
        var cached = await _cache.GetStringAsync(CacheKey, ct);
        if (cached is not null && Enum.TryParse<DeploymentMode>(cached, out var cachedMode))
            return cachedMode;

        // Layer 3: database (scoped repo accessed via factory)
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<IInstanceBootstrapStateRepository>();
        var bootstrap = await repo.GetCurrent();

        var mode = bootstrap?.IsCompleted == true
            && Enum.TryParse<DeploymentMode>(bootstrap.SelectedDeploymentMode, out var dbMode)
            ? dbMode
            : DeploymentMode.MultiTenant;

        await _cache.SetStringAsync(
            CacheKey,
            mode.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            },
            ct);

        _logger.LogDebug("Deployment mode resolved from DB: {Mode}", mode);
        return mode;
    }

    public async Task<bool> IsSingleTenantAsync(CancellationToken ct = default)
        => await GetCurrentModeAsync(ct) == DeploymentMode.SingleTenant;

    public async Task InvalidateCacheAsync()
        => await _cache.RemoveAsync(CacheKey);
}
```

- **Acceptance Criteria**:
  - [ ] Uses `IOptionsMonitor<DeploymentSettings>` (not `IOptions`) for singleton safety + live reload
  - [ ] Uses `IDistributedCache` (backed by Redis via Aspire in prod, `IMemoryCache`-backed in tests)
  - [ ] `IServiceScopeFactory` for scoped repo access from singleton — no captive dependency
  - [ ] Zero `.GetAwaiter().GetResult()` calls
  - [ ] Zero static mutable fields
  - [ ] Unit tests cover: config short-circuit, cache hit, cache miss + DB fallback, cache miss + DB empty
- **Effort**: M
- **Skill**: `clean-architecture-rules`, `dotnet-efcore-guidelines`
- **Depends on**: 1.1

---

#### Task 1.3 — Migrate `ApiTenantResolutionMiddleware`

- **File**: `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`

**Remove**:
- `private static volatile string? _cachedBootstrapDeploymentMode`
- `public static void InvalidateBootstrapCache()`
- `private bool IsSingleTenantMode(IInstanceBootstrapStateRepository ...)`
- `IInstanceBootstrapStateRepository` parameter from `InvokeAsync`

**Add**:
- `IDeploymentModeProvider provider` parameter in `InvokeAsync`
- `if (await provider.IsSingleTenantAsync(context.RequestAborted))` branch

- **Acceptance Criteria**:
  - [ ] Zero static mutable state in file
  - [ ] Zero sync DB calls
  - [ ] `IDeploymentModeProvider` resolved per-request via `InvokeAsync` DI injection
  - [ ] All 450 API integration tests pass
- **Effort**: M
- **Depends on**: 1.2

---

#### Task 1.4 — Migrate `BlockInSingleTenantAttribute`

- **File**: `Explore.API/Filters/BlockInSingleTenantAttribute.cs`

**Remove**: nested `DeploymentModeResolver` class entirely. It contains its own
`ISystemSettingRepository` lookup, its own JSON deserialization fallback, its own
try/catch that swallows errors — this is a complete reimplementation of logic that
now lives in `IDeploymentModeProvider`.

**Replace** with:
```csharp
public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
{
    var provider = context.HttpContext.RequestServices
        .GetRequiredService<IDeploymentModeProvider>();

    if (!await provider.IsSingleTenantAsync(context.HttpContext.RequestAborted))
        return;

    // ... existing 404/403 response logic ...
}
```

- **Acceptance Criteria**:
  - [ ] `DeploymentModeResolver` nested class deleted
  - [ ] No direct repository access in filter
  - [ ] Zero sync-over-async
  - [ ] Filter-specific tests pass for both modes
- **Effort**: S
- **Depends on**: 1.2

---

#### Task 1.5 — Migrate `CompleteInstanceOnboardingCommandHandler`

- **File**: `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`

**Remove**: `IDeploymentModeCacheInvalidator? _deploymentModeCacheInvalidator` (nullable field and optional constructor parameter)

**Add**: `IDeploymentModeProvider _deploymentModeProvider` (required, non-nullable)

**Move** `await _deploymentModeProvider.InvalidateCacheAsync()` to **after** transaction commit (see Phase 3).

- **Acceptance Criteria**:
  - [ ] Non-nullable dependency — missing registration is a startup crash, not silent failure
  - [ ] Cache invalidation called only post-commit
  - [ ] Unit tests updated
- **Effort**: S
- **Depends on**: 1.2

---

#### Task 1.6 — Delete Replaced Interfaces and Implementations

**Files to delete**:
- `Explore.Application/Contracts/Services/IDeploymentModeCacheInvalidator.cs`
- `Explore.API/Services/DeploymentModeCacheInvalidator.cs`

**Registration to remove** from `Explore.API/Program.cs`:
```csharp
// DELETE this line:
builder.Services.AddSingleton<IDeploymentModeCacheInvalidator, ...>();
```

- **Acceptance Criteria**:
  - [ ] Zero references to deleted types (compiler-verified)
  - [ ] Build passes with zero errors
- **Effort**: S
- **Depends on**: 1.3, 1.4, 1.5

---

#### Task 1.7 — Fix `SetupSecretProvider` Async Bootstrap Check

- **File**: `Explore.Infrastructure/Services/SetupSecretProvider.cs`

**Problem**: `IsBootstrapComplete()` uses double-checked locking with `.GetAwaiter().GetResult()`.

**Fix**: Replace with `IDeploymentModeProvider`. Since the setup secret provider needs
to know if onboarding has completed, and `IDeploymentModeProvider` already resolves this
(mode is only set when `IsCompleted = true`), the bootstrap check becomes:

```csharp
// OLD: bool bootstrapComplete = _bootstrapRepo.GetCurrent().GetAwaiter().GetResult()?.IsCompleted == true;
// NEW: provider.GetCurrentModeAsync() returning a non-empty mode means bootstrap completed.
```

If `IDeploymentModeProvider` is too heavyweight for `SetupSecretProvider`, use a
`SemaphoreSlim(1,1)` with async lazy initialization instead of double-checked locking.

- **Acceptance Criteria**:
  - [ ] Zero `.GetAwaiter().GetResult()` calls
  - [ ] Thread-safe (SemaphoreSlim or provider delegation)
  - [ ] `SetupSecretFlowTests` integration tests pass
- **Effort**: S
- **Depends on**: 1.2

---

### Phase 2: Redis Distributed Cache Infrastructure

**Goal**: Replace `IMemoryCache`-only cache backend with Redis via Aspire.
`IDistributedCache` abstraction means: Redis in production, in-memory in tests.

---

#### Task 2.1 — Add Redis to Aspire AppHost

- **File**: `Explore.AppHost/AppHost.cs`

```csharp
var cache = builder.AddRedis("cache")
    .WithRedisInsight();  // optional: Redis Insight dashboard

var api = builder.AddProject<Projects.Explore_API>("api")
    .WithReference(cache)
    .WaitFor(cache);

var blazor = builder.AddProject<Projects.Explore_Blazor>("blazor")
    .WithReference(cache)
    .WaitFor(cache);
```

- **Acceptance Criteria**:
  - [ ] `Aspire.Hosting.Redis` package added to AppHost
  - [ ] Both API and Blazor projects reference the cache resource
  - [ ] `aspire run` starts Redis container automatically (no manual Docker)
- **Effort**: S

---

#### Task 2.2 — Wire `IDistributedCache` in API and Blazor

- **Files**: `Explore.API/Program.cs`, `Explore.Blazor/Program.cs`

```csharp
// In Program.cs of each project:
builder.AddRedisDistributedCache(connectionName: "cache");
```

Package: `Aspire.StackExchange.Redis.DistributedCaching` — adds health check, logging,
telemetry automatically via Aspire's component model.

- **Acceptance Criteria**:
  - [ ] `Aspire.StackExchange.Redis.DistributedCaching` added to both `Explore.API.csproj` and `Explore.Blazor.csproj`
  - [ ] `IDistributedCache` resolvable from DI in both projects
  - [ ] Health check endpoint reports Redis connectivity
- **Effort**: S
- **Depends on**: 2.1

---

#### Task 2.3 — Configure Test Factories to Use In-Memory Distributed Cache

- **Files**: All `*WebApplicationFactory.cs` fixtures

Test factories must NOT connect to Redis. Override `IDistributedCache` with the
in-memory implementation:

```csharp
builder.ConfigureTestServices(services =>
{
    services.RemoveAll<IDistributedCache>();
    services.AddDistributedMemoryCache();
});
```

This keeps `IDeploymentModeProvider` functional in tests while using `SingleTenant`
config (which short-circuits before touching the cache anyway).

- **Acceptance Criteria**:
  - [ ] All 4 factory classes override `IDistributedCache` with in-memory variant
  - [ ] No Redis connection required to run tests
  - [ ] 450 API integration tests pass
- **Effort**: S
- **Depends on**: 2.2

---

### Phase 3: Transactional Onboarding with Post-Commit Side Effects

**Goal**: Onboarding is a single atomic operation. Side effects (cache, lock) execute
only after the transaction commits successfully.

---

#### Task 3.1 — Add `IUnitOfWork` Interface to Application Layer

- **File**: `Explore.Application/Contracts/Persistence/IUnitOfWork.cs`

```csharp
// ABOUTME: Wraps a database transaction to allow multiple repository writes to commit atomically.
// ABOUTME: Used by onboarding handler to guarantee all-or-nothing first-run setup.

namespace Explore.Application.Contracts.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

- **Acceptance Criteria**:
  - [ ] Lives in Application layer (no EF Core reference)
  - [ ] `IAsyncDisposable` for `await using` pattern in handlers
- **Effort**: S
- **Skill**: `dotnet-efcore-guidelines`

---

#### Task 3.2 — Implement `EfCoreUnitOfWork`

- **File**: `Explore.Persistence/EfCoreUnitOfWork.cs`

```csharp
// ABOUTME: EF Core implementation of IUnitOfWork wrapping ExploreDbContext transactions.

public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly ExploreDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public async Task BeginAsync(CancellationToken ct = default)
        => _transaction = await _dbContext.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
        => await _transaction!.CommitAsync(ct);

    public async Task RollbackAsync(CancellationToken ct = default)
        => await _transaction!.RollbackAsync(ct);

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();
    }
}
```

- **Acceptance Criteria**:
  - [ ] Registered as Scoped in `PersistenceServicesRegistration`
  - [ ] Works with InMemory provider in tests (InMemory doesn't support transactions; use `try/catch` or skip transaction for testing environment)
- **Effort**: S
- **Depends on**: 3.1

---

#### Task 3.3 — Wrap Onboarding Handler in Transaction with Post-Commit Side Effects

- **File**: `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`

**Pattern**: All DB writes inside `await uow.BeginAsync()` … `await uow.CommitAsync()`.
Side effects (`InvalidateCacheAsync`, `InvalidateUser`, `Lock`) called **only after commit**.

```csharp
public async Task<BaseCommandResponse<Guid>> Handle(
    CompleteInstanceOnboardingCommand request, CancellationToken cancellationToken)
{
    // ... validation (no DB writes, safe outside tx) ...

    await using var uow = _unitOfWork;
    await uow.BeginAsync(cancellationToken);

    try
    {
        // All DB writes:
        if (isSingleTenant)
        {
            await EnsureDefaultTenantAsync();
            await EnsureDefaultTenantSettingsAsync(defaultTenantId!.Value);
        }
        var user = await _userRepository.GetById(request.UserId)
            ?? await CreateOnboardingUserAsync(request, defaultTenantId);
        await _governanceSettingService.ApplySettingsAsync(...);
        await EnsurePlatformAdministratorRoleAsync(request.UserId);
        if (isSingleTenant) await EnsureDefaultTenantAdministratorAsync(...);
        bootstrap = await PersistBootstrapStateAsync(request, bootstrap);

        await uow.CommitAsync(cancellationToken);
    }
    catch
    {
        await uow.RollbackAsync(cancellationToken);
        throw;
    }

    // ── Post-commit side effects ──────────────────────────────────────────
    // These cannot be rolled back. They run only on confirmed commit.
    // Order: cache first (fastest), then admin, then lock.
    await _deploymentModeProvider.InvalidateCacheAsync();
    _adminCacheInvalidator.InvalidateUser(request.UserId);
    _setupSecretProvider.Lock();

    response.Success = true;
    response.Id = bootstrap.Id;
    return response;
}
```

- **Acceptance Criteria**:
  - [ ] All 7+ DB writes inside single `BeginAsync`/`CommitAsync` block
  - [ ] `InvalidateCacheAsync`, `InvalidateUser`, `Lock` called only after `CommitAsync` returns
  - [ ] Exception mid-transaction triggers `RollbackAsync` and re-throws (no partial state)
  - [ ] Integration test: force failure mid-onboarding, verify DB has no records, verify cache unchanged
  - [ ] Integration test: successful onboarding, verify all records exist, verify cache invalidated
- **Effort**: M
- **Depends on**: 3.2, 1.5

---

#### Task 3.4 — Add Unique Filtered Index on `InstanceBootstrapState`

- **File**: `Explore.Persistence/Configurations/Entities/InstanceBootstrapStateConfiguration.cs`
- **Migration**: New migration `AddBootstrapStateUniqueConstraint`

```csharp
// Partial unique index: only one row where IsCompleted = true
entity.HasIndex(e => e.IsCompleted)
      .HasFilter("\"is_completed\" = true")
      .IsUnique()
      .HasDatabaseName("ix_instance_bootstrap_state_single_completed");
```

- **Acceptance Criteria**:
  - [ ] PostgreSQL partial unique index on `is_completed = true`
  - [ ] Second `CompleteInstanceOnboarding` call returns 409 Conflict (or existing response)
  - [ ] Migration runs cleanly on empty and populated databases
- **Effort**: S
- **Skill**: `dotnet-efcore-guidelines`

---

### Phase 4: Core Governance Policy Hierarchy [NEW ENTERPRISE BLUEPRINT]

> **Wait, this was the old way of thinking!** 
> The plan originally focused purely on the infrastructure layer (eliminating the legacy `TenantSettings` entity) and forcing all configuration into a single, generic settings engine. 
> This approach collapsed Operational Config, Feature Flags, and Core Business Policy into one generic registry, sacrificing Domain-Driven Design and type safety.
> 
> *The following legacy plan is kept purely for historical context. Scroll down to Phase 4 (NEW ENTERPRISE BLUEPRINT) for the target architecture.*

<details>
<summary>Click to view legacy Phase 4 and Phase 5</summary>

**Goal**: Delete the `TenantSettings` entity. All policy reads go through the governance cascade (`SettingsResolver`). No dual maintenance. No split-brain.

#### Task 4.1 — Audit All `TenantSettings`
#### Task 4.2 — Add Missing `SettingDefinition` Entries
#### Task 4.3 — Migrate All Read Sites to `ISettingsResolver`
#### Task 4.4 — Remove `EnsureDefaultTenantSettingsAsync`
#### Task 4.5 — Delete `TenantSettings` Entity and Repository

### Phase 5: Observability and Audit (LEGACY)
#### Task 5.1 — Create `SettingChangedNotification`
#### Task 5.2 — Publish Notification
#### Task 5.3 — Implement `SettingAuditLogHandler`

</details>

---

**Goal**: Stop using a generic "Settings Registry" for core governance. Model the governance hierarchy explicitly using typed domain aggregates, strongly-typed EF Core columns, and separate the concepts of **value inheritance** from **delegation authority**.

#### Task 4.1 — Introduce `PolicySlot<T>` and Core Policy Aggregates

- **Files**: `Explore.Domain/Policies/PolicySlot.cs`, `Explore.Domain/Policies/TenantPolicySet.cs`, etc.

Model governed fields with two explicit concerns: local assignment (Value) and child delegation (OverrideMode).

```csharp
// ABOUTME: Wraps a policy value, separating the value itself from the permission of child scopes to override it.
public sealed record PolicySlot<T>(
    T? LocalValue,
    ChildOverrideMode OverrideMode);

public enum ChildOverrideMode
{
    Allow,
    Deny
}
```

Define explicit aggregates for the scopes:
- `InstancePolicySet`
- `TenantPolicySet`
- `OrganizationPolicySet`

With explicit nested value objects (Complex Types in EF Core):
```csharp
public sealed class EventPolicy
{
    public PolicySlot<bool> RequireApproval { get; set; }
    public PolicySlot<EventPublishingPolicy> DefaultPublishingPolicy { get; set; }
}

public sealed class TenantPolicySet 
{
    public Guid TenantId { get; set; }
    public EventPolicy EventPolicy { get; set; } = new();
    public OrganizationPolicy OrganizationPolicy { get; set; } = new();
    // ... optimistic concurrency token (RowVersion)
}
```

- **Acceptance Criteria**:
  - [ ] Explicit typed Policy classes defined for each major domain area.
  - [ ] No generic `SettingDefinition<T>` used for core policies.

#### Task 4.2 — Implement Typed Persistence in EF Core

- **Files**: `Explore.Persistence/Configurations/Entities/TenantPolicySetConfiguration.cs`

Store core policies in **typed columns**, not `jsonb`. This provides a clearer schema, easier migrations, and better DB constraints.

```csharp
builder.ComplexProperty(x => x.EventPolicy, ep => 
{
    ep.Property(p => p.RequireApproval)
      .HasConversion(
          v => JsonSerializer.Serialize(v, jsonOptions), 
          v => JsonSerializer.Deserialize<PolicySlot<bool>>(v, jsonOptions))
      .HasColumnName("event_require_approval");
});
```
*(Alternatively, map `PolicySlot.LocalValue` and `PolicySlot.OverrideMode` to distinct scalar columns if deeper DB querying is required).*

#### Task 4.3 — Implement Deterministic Resolution Service

- **File**: `Explore.Application/Contracts/Services/IPolicyResolver.cs`

Create a resolver that walks the explicit hierarchy (Instance -> Tenant -> Organization) and returns both the **effective value** and the **effective mutability** (whether the current actor's scope is allowed to change it).

```csharp
public sealed record PolicyDecision<T>(
    T Value,
    bool CanOverride,
    SettingScope SourceScope,
    SettingScope? BlockedByScope);

public interface IPolicyResolver
{
    Task<PolicyDecision<T>> GetFieldDecisionAsync<T>(
        Expression<Func<TenantPolicySet, PolicySlot<T>>> selector, 
        Guid tenantId);
}
```

---

### Phase 5: Feature Flags & Operational Config [NEW ENTERPRISE BLUEPRINT]

**Goal**: Extract application configuration and feature toggles completely out of the domain governance engine.

#### Task 5.1 — OpenFeature Integration for Toggles

- **Packages**: `OpenFeature`, `OpenFeature.Hosting`, `Microsoft.FeatureManagement`
- **Files**: `Explore.Application/Features/Flags/`, `Explore.Blazor/`

Implement a provider-agnostic feature flag abstraction using **OpenFeature**. This allows seamless swapping to FeatBit, Unleash, or PostHog in the future. Fall back to `Microsoft.FeatureManagement` if no external provider is configured.

```csharp
// API / Application Layer
var client = _featureClient.GetClient();
bool isNewSearchEnabled = await client.GetBooleanValueAsync("new-search-api", false, evaluationContext);
```

In Blazor, utilize the `<FeatureGate>` component (or an OpenFeature equivalent wrapper) to conditionally render UI:
```html
<FeatureGate Feature="new-search-ui">
    <NewAdvancedSearchComponent />
</FeatureGate>
```

#### Task 5.2 — Restrict `IOptionsMonitor` to Infrastructure

Ensure `DeploymentSettings`, SMTP credentials, and cache endpoints remain in standard .NET configuration providers (e.g., `appsettings.json`, Azure Key Vault, environment variables) using the `IOptionsMonitor<T>` pattern. Do not mix these into the database governance hierarchy.

---

### Phase 6: Caching, Concurrency, and Audit [NEW ENTERPRISE BLUEPRINT]

**Goal**: Ensure multi-node consistency without relying on at-most-once Redis Pub/Sub for correctness.

#### Task 6.1 — Versioned Cache Keys & Optimistic Concurrency

- Implement `xmin` (PostgreSQL) or `RowVersion` optimistic concurrency on all PolicySet tables.
- Cache keys must incorporate the version stamp: `TenantPolicy:{TenantId}:v{Version}`.
- If a stale read occurs, the cache misses and recomputes deterministically.

#### Task 6.2 — Outbox-Backed Policy Change Events

- Use EF Core Outbox to emit `PolicyChangedDomainEvent` guaranteeing at-least-once delivery.
- Background workers consume the outbox to perform cache invalidation / fan-out as an optimization, rather than a critical path requirement.

---

## Risk Assessment (Updated)

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Blazor NSwag regeneration breaks | High | High | Keep API contracts clean; update Blazor sub-resource DTOs in lockstep. |
| EF Core Complex Types mapping issues | Medium | Medium | Test `PolicySlot<T>` mappings extensively, especially around nullability. |
| OpenFeature provider complexity | Low | Low | Start with `Microsoft.FeatureManagement` provider memory/config backend before introducing FeatBit. |

---

## Effort Summary (Updated)

| Phase | Description | Tasks | Effort | Priority |
|-------|-------------|-------|--------|----------|
| 0 | API Contract Redesign | 6 | XL | Critical |
| 1 | Unified Deployment Mode Provider | 7 | L | Critical |
| 2 | Redis Distributed Cache | 3 | M | High |
| 3 | Transactional Onboarding | 4 | M | High |
| 4 | Core Governance Policy Hierarchy | 3 | XL | High |
| 5 | Feature Flags & Operational Config | 2 | M | High |
| 6 | Caching, Concurrency, and Audit | 2 | M | Medium |
| **Total** | | **27** | | |

---

## Potential Risks & Unknowns

**The highest-risk task is Phase 3.3** (transactional onboarding). The challenge is not
the EF transaction itself — it's the InMemory provider used in test factories. EF Core
`InMemoryDatabase` does not support relational transactions; calling `BeginTransactionAsync`
on it throws `InvalidOperationException`. All three test factory variants use InMemory.
The fix (environment guard or `database.IsInMemory()` check) is straightforward but easy
to miss, and if not handled, every onboarding integration test fails with a confusing
"relational operations not supported" exception rather than a real business failure.

**The second risk is Phase 4** — migrating away from the generic settings resolver to explicit domain objects requires rewriting `TenantPolicySettingService.cs` entirely. We must assure we map all policies explicitly without losing business rule semantics during the transition.
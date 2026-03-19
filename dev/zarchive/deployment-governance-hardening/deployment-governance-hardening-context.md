# Deployment Mode & Governance Hardening — Context

**Last Updated: 2026-03-17**

---

## SESSION PROGRESS (2026-03-18)

### COMPLETED
- All 27/27 tasks across 7 phases complete
- Phase 0: API Contract Redesign — god DTOs split, controllers restructured, Blazor clients updated
- Phase 1: Unified Deployment Mode Provider — IDeploymentModeProvider, IOptionsMonitor, async everywhere
- Phase 2: Redis Distributed Cache — Aspire Redis, IDistributedCache, test factory overrides
- Phase 3: Transactional Onboarding — IUnitOfWork, EfCoreUnitOfWork, filtered unique index
- Phase 4: Core Governance Policy Hierarchy — PolicySlot, typed aggregates, deterministic resolver
- Phase 5: Feature Flags — OpenFeature integration, IFeatureFlagService
- Phase 6: Caching, Concurrency, Audit — versioned cache keys, outbox-backed policy changes
- Task 0.6 verified complete: EventApiClient.g.cs regenerated, InstanceOnboardingService uses sub-resources, admin pages updated, 580/580 Blazor tests pass

### IN PROGRESS
- Nothing. All phases complete.

### BLOCKERS
- None.

---

## Architecture Decisions (Final)

### `IOptionsMonitor<DeploymentSettings>` not `IOptions`
`IOptions<T>` snapshots at registration. `IOptionsMonitor<T>` reads `.CurrentValue` on
each access — correct for a Singleton service. With `reloadOnChange: true` on the JSON
config builder, environment variable overrides (e.g., `Deployment:Mode=SingleTenant`)
take effect without restart. The static config short-circuit in the provider uses this.

### `IDistributedCache` (Redis via Aspire) not `IMemoryCache`
Redis is the correct cache backend for multi-replica deployments. Aspire wires it with
zero boilerplate via `Aspire.StackExchange.Redis.DistributedCaching` (v13.1.2 as of
Feb 2026). Tests use `AddDistributedMemoryCache()` override — same interface, no Redis
needed to run tests.

### Post-Commit Side Effects (No Outbox Needed Here)
Cache invalidation + setup secret lock are post-commit steps, not external I/O calls.
They are safe to call synchronously after `CommitAsync()` returns because:
- If `CommitAsync` throws, execution never reaches them
- If they throw after commit, the DB state is valid (onboarding completed)
- Cache incorrect for ≤30s is acceptable (TTL auto-corrects)
- Full Transactional Outbox pattern is not needed for these in-process calls

### `IUnitOfWork` wraps EF transaction
Application layer contract (`IUnitOfWork`) owns `BeginAsync`/`CommitAsync`/`RollbackAsync`.
EF Core implementation in Persistence layer. InMemory provider guard in `EfCoreUnitOfWork`
for test factories.

---

## Key Files — Phase 1

**`Explore.Infrastructure/DeploymentSettings.cs`** (existing)
- Config POCO bound from `"Deployment"` appsettings section
- `IsSingleTenant` → `Mode == DeploymentMode.SingleTenant`
- Change injection: `IOptions` → `IOptionsMonitor` in `DeploymentModeProvider`

**`Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`** (MODIFY in 1.3)
- Remove static field `_cachedBootstrapDeploymentMode` (line 24)
- Remove `InvalidateBootstrapCache()` static method (line 39-42)
- Remove `IsSingleTenantMode()` private method (lines 202-223)
- Remove `IInstanceBootstrapStateRepository` from `InvokeAsync` parameters (line 44)
- Add `IDeploymentModeProvider provider` to `InvokeAsync`
- Replace line 64 with `if (await provider.IsSingleTenantAsync(context.RequestAborted))`

**`Explore.API/Filters/BlockInSingleTenantAttribute.cs`** (MODIFY in 1.4)
- Delete entire nested `DeploymentModeResolver` class
- Resolve `IDeploymentModeProvider` from `context.HttpContext.RequestServices`
- Replace sync DB check with `await provider.IsSingleTenantAsync()`

**`Explore.Application/Contracts/Services/IDeploymentModeCacheInvalidator.cs`** (DELETE in 1.6)
**`Explore.API/Services/DeploymentModeCacheInvalidator.cs`** (DELETE in 1.6)
**`Explore.API/Program.cs`** (line 161) — remove singleton registration for above

---

## Key Files — Phase 2

**`Explore.AppHost/AppHost.cs`** (MODIFY in 2.1)
- Add Redis resource, reference from API and Blazor projects

**`Explore.API/Program.cs`** (MODIFY in 2.2)
- Add `builder.AddRedisDistributedCache(connectionName: "cache")`
- Package: `Aspire.StackExchange.Redis.DistributedCaching` → `Explore.API.csproj`

**`Explore.Blazor/Program.cs`** (MODIFY in 2.2)
- Same Redis wiring as API

**All `*WebApplicationFactory.cs` files** (MODIFY in 2.3)
- Add to `ConfigureTestServices`: `services.RemoveAll<IDistributedCache>(); services.AddDistributedMemoryCache();`

---

## Key Files — Phase 3

**`Explore.Application/Contracts/Persistence/IUnitOfWork.cs`** (CREATE in 3.1)
**`Explore.Persistence/EfCoreUnitOfWork.cs`** (CREATE in 3.2)
**`CompleteInstanceOnboardingCommandHandler.cs`** (MODIFY in 3.3)
- Critical: InMemory EF provider does not support transactions
- Guard: `if (!_dbContext.Database.IsInMemory()) await uow.BeginAsync(ct);`
- Or: check environment in `EfCoreUnitOfWork.BeginAsync` and skip if InMemory

**`InstanceBootstrapStateConfiguration.cs`** (MODIFY in 3.4)
- Partial unique index: `HasFilter("\"is_completed\" = true")`

---

## Key Files — Phase 4 (NEW ENTERPRISE BLUEPRINT)

**`Explore.Domain/Policies/PolicySlot.cs`** (CREATE in 4.1)
- The core value object wrapping `LocalValue` and `OverrideMode`.
**`Explore.Domain/Policies/TenantPolicySet.cs`** (CREATE in 4.1)
- The typed aggregate root for tenant governance.
**`Explore.Persistence/Configurations/Entities/TenantPolicySetConfiguration.cs`** (CREATE in 4.2)
- Maps the structured policy to strongly-typed columns using EF Core Complex Types.
**`Explore.Application/Contracts/Services/IPolicyResolver.cs`** (CREATE in 4.3)
- The deterministic resolution service returning `PolicyDecision<T>`.

---

## Key Files — Phase 5 (NEW ENTERPRISE BLUEPRINT)

**`Explore.Application/Features/Flags/`** (CREATE in 5.1)
- `OpenFeature` integration for agnostic feature toggles.
**`Explore.Blazor/`** (MODIFY in 5.1)
- Implement or use `<FeatureGate>` wrapper for UI conditionally rendered components.

---

## Key Files — Phase 6 (NEW ENTERPRISE BLUEPRINT)

**`Explore.Domain/Events/PolicyChangedDomainEvent.cs`** (CREATE in 6.2)
- Used by EF Core Outbox to ensure at-least-once policy invalidation propagation.

---

## Critical Implementation Notes

1. **`IServiceScopeFactory` in DeploymentModeProvider**: The provider is Singleton. `IInstanceBootstrapStateRepository` is Scoped (EF Core). Never inject Scoped into Singleton directly — use `IServiceScopeFactory.CreateScope()` and dispose after the DB call.

2. **InMemory EF + Transactions**: All three test factories use `UseInMemoryDatabase`. EF Core InMemory provider throws `InvalidOperationException` on `BeginTransactionAsync`. Either add an environment check or make `EfCoreUnitOfWork.BeginAsync` a no-op for InMemory.

3. **Fire-and-Forget `SettingChangedNotification`**: Use `_ = _mediator.Publish(...)` with `CancellationToken.None` — the notification should not block the request or inherit request cancellation.

4. **`IOptionsMonitor` `.CurrentValue`**: Always access `.CurrentValue` per-call, not cached in a field. The whole point is live reload on each access.

5. **`await using var uow`**: `IUnitOfWork : IAsyncDisposable` — use `await using` so the transaction is disposed if the handler throws before explicit `CommitAsync`/`RollbackAsync`.

---

## Key Files — Phase 0

**`Explore.Application/DTOs/Onboarding/InstanceGovernanceSettingsDto.cs`** (DELETE in 0.1)
- 66-property god object mixing deployment mode, modules, events, orgs, branding, domains, delegation, render policy
- `DeploymentMode` property is `string` not `DeploymentMode` enum

**`Explore.Application/DTOs/Instance/`** (CREATE directory + 8 files in 0.1)
- `DeploymentModeDto.cs`, `ModuleSettingsDto.cs`, `EventPolicyDto.cs`, `OrganizationPolicyDto.cs`
- `BrandingSettingsDto.cs`, `DomainSettingsDto.cs`, `TenantDelegationSettingsDto.cs`, `RenderPolicySettingsDto.cs`

**`Explore.Application/DTOs/Onboarding/CompleteInstanceOnboardingRequest.cs`** (REPLACE in 0.2)
- Replace 66-prop god object; new request has: `DeploymentMode` (enum), `AdminEmail`, `AdminPassword`, `InstanceName?`

**`Explore.API/Controllers/InstanceOnboardingController.cs`** (TRIM to 2 endpoints in 0.3)
- Keep: `POST /complete`, `GET /status`
- Move all settings management to new `InstanceSettingsController`

**`Explore.API/Controllers/InstanceSettingsController.cs`** (CREATE in 0.3)
- RESTful sub-resource endpoints: GET+PUT per domain (modules, events, organizations, branding, domains, tenant-delegation, render-policy, deployment-mode)

**`Explore.Application/DTOs/TenantPolicy/`** (CREATE in 0.4)
- `TenantPolicyDto.cs` — GET response with `CanOverride*` flags (read-only computed)
- `UpdateTenantPolicyRequest.cs` — PUT body without read-only flags

**`Explore.API/Controllers/TenantSettingsController.cs`** (MARK OBSOLETE in 0.5, DELETE in 4.5)
- Legacy CRUD for `TenantSettings` entity — mark `[Obsolete]` now, delete with entity in Phase 4

**`Explore.Blazor.Client/Clients/EventApiClient.g.cs`** (REGENERATE in 0.6)
- NSwag/Kiota generated client — must be regenerated after controller changes

---

## Quick Resume

1. Read this file
2. Check `deployment-governance-hardening-tasks.md` for next unchecked task
3. Start Phase 0, Task 0.1 — split the god DTO
4. After each task: `dotnet build --configuration Release --verbosity quiet`
5. After Phase 0 complete: run `Explore.Blazor.Client.Tests`
6. After Phase 1 complete: run `Event.API.IntegrationTests` (target: 449+ pass)
7. After Phase 2 complete: run all test suites

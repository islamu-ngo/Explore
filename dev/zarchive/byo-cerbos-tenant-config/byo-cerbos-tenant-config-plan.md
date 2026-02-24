# Plan: Per-Tenant Cerbos Configuration — BYO + Instance Isolation

> Last Updated: 2026-02-23

## Executive Summary

Enable per-tenant Cerbos authorization with two modes: **Bring Your Own (BYO)** where tenants provide their own Cerbos PDP endpoint, and **Instance-managed isolation** where the instance's Cerbos PDP handles all tenants with scoped policy overrides. Follows the established cascading settings pattern (like SMTP/S3) with `ISettingsResolver`, `IsLocked` governance, and 5-minute per-tenant caching.

## Current State Analysis

### What exists:
- **Scope field**: Already wired in `CerbosAuthorizationService.BuildResources()` — tenantId passed as `Scope` for per-tenant policy resolution
- **Cascading settings engine**: `ISettingsResolver` → `SystemSetting` (instance default, lockable) → `TenantSetting` (tenant override)
- **SMTP/S3 resolver pattern**: `SmtpConfigResolver`, `S3ConfigResolver` — proven BYO-service architecture
- **RuntimeAuthorizationProvider**: Instance-level switch between Cerbos and Fallback (1-min cache)
- **PolicySyncService**: Pushes dynamically-generated policies to Cerbos Admin API and broadcasts reload
- **CerbosSettings/CerbosAdminApiSettings**: Static config from appsettings.json
- **GovernanceSettingKeys**: Grouped constants for all governance settings
- **InfrastructureSecretSettingKeys**: Secret keys (SMTP credentials, S3 keys)

### What's missing:
- No per-tenant Cerbos endpoint resolution
- No `CerbosConfigResolver` (no equivalent to SmtpConfigResolver for Cerbos)
- No `GovernanceSettingKeys.Cerbos` section
- No BYO Cerbos secret settings keys
- No per-tenant HttpClient management for BYO endpoints
- No Safe-Mode fallback behavior for BYO-Cerbos failure
- No tenant policy customization request mechanism
- Single HttpClient ("CerbosClient") — no dynamic endpoint routing

## Proposed Future State

### Architecture Flow

```
Request arrives → RuntimeAuthorizationProvider
                  │
                  ├─ CerbosConfigResolver: Does tenant have BYO Cerbos?
                  │   ├─ YES → Route to tenant's BYO PDP endpoint
                  │   │        └─ Unreachable?
                  │   │            ├─ failure_mode = "closed" → Safe-Mode (instance admin only)
                  │   │            └─ failure_mode = "open"   → FallbackAuthorizationService
                  │   │
                  │   └─ NO → Use instance-level resolution:
                  │        ├─ Instance mode = "cerbos" → Instance PDP + scope=tenantId
                  │        │    └─ Unreachable → FallbackAuthorizationService
                  │        └─ Instance mode = "local"  → FallbackAuthorizationService
```

### Setting Keys Hierarchy

| Key | Scope | Purpose |
|-----|-------|---------|
| `cerbos.tenant_customization_enabled` | Instance (SystemSetting) | Master switch: allow tenants to customize |
| `cerbos.mode` | Per-tenant (TenantSetting) | `"instance"` (default) or `"custom_endpoint"` (BYO) |
| `cerbos.custom_endpoint` | Per-tenant (TenantSetting) | BYO: tenant's Cerbos PDP HTTP URL |
| `cerbos.failure_mode` | Per-tenant (TenantSetting) | `"closed"` (default) or `"open"` |
| `cerbos.custom_admin_endpoint` | Per-tenant (TenantSetting) | BYO: tenant's Cerbos Admin API URL (optional) |
| `cerbos.custom_admin_username` | Per-tenant (InfrastructureSecret) | BYO: Admin API username |
| `cerbos.custom_admin_password` | Per-tenant (InfrastructureSecret) | BYO: Admin API password |

### BYO Failure Modes

| Mode | Behavior when tenant's PDP is unreachable |
|------|------------------------------------------|
| **`closed`** (default) | Safe-Mode: only instance admin emergency access, deny everything else. Never fall back to instance PDP (tenant policies might be stricter). |
| **`open`** | Fall back to FallbackAuthorizationService (standard RBAC). Tenant accepts the risk of more permissive fallback. |

### Tenant Policy Customization Request (Instance-Managed Mode)

When a tenant uses the instance Cerbos PDP, the tenant admin can *request* policy customizations within their scope. The implementation:
1. Tenant admin submits a `TenantPolicyOverrideRequest` via the API
2. Instance admin reviews and approves/rejects the request
3. On approval, `PolicySyncService` generates a scoped policy variant for that tenant
4. Cerbos resolves the scoped policy for that tenant's requests, falls back to root for others

> **Note**: The actual scoped policy YAML generation and tenant admin request UI are Phase 2. Phase 1 focuses on BYO infrastructure.

---

## Implementation Phases

### Phase 1: Domain & Constants (Effort: S)

**Task 1.1: Add GovernanceSettingKeys.Cerbos**
- File: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- Add `Cerbos` nested class with all setting key constants
- Add flat aliases for backward compatibility
- Acceptance criteria:
  - [ ] `GovernanceSettingKeys.Cerbos.TenantCustomizationEnabled` = `"cerbos.tenant_customization_enabled"`
  - [ ] `GovernanceSettingKeys.Cerbos.Mode` = `"cerbos.mode"`
  - [ ] `GovernanceSettingKeys.Cerbos.CustomEndpoint` = `"cerbos.custom_endpoint"`
  - [ ] `GovernanceSettingKeys.Cerbos.FailureMode` = `"cerbos.failure_mode"`
  - [ ] `GovernanceSettingKeys.Cerbos.CustomAdminEndpoint` = `"cerbos.custom_admin_endpoint"`
  - [ ] Flat aliases added
- Effort: S
- Related Skills: `clean-architecture-rules`

**Task 1.2: Add InfrastructureSecretSettingKeys.Cerbos**
- File: `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`
- Add `Cerbos` nested class for BYO secret credentials
- Acceptance criteria:
  - [ ] `InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername` = `"cerbos.custom_admin_username"`
  - [ ] `InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword` = `"cerbos.custom_admin_password"`
- Effort: S

### Phase 2: Application Layer — Contract & Model (Effort: S)

**Task 2.1: Create ICerbosConfigResolver interface**
- File: `Explore.Application/Contracts/Infrastructure/ICerbosConfigResolver.cs`
- Mirrors `ISmtpConfigResolver` pattern
- Acceptance criteria:
  - [ ] `ResolveAsync(CancellationToken)` returns `CerbosConfiguration?`
  - [ ] `InvalidateCache(Guid? tenantId)` for cache management
  - [ ] Returns null when Cerbos is not configured for tenant
- Effort: S
- Related Skills: `clean-architecture-rules`

**Task 2.2: Create CerbosConfiguration model**
- File: `Explore.Application/Models/CerbosConfiguration.cs`
- POCO resolved from cascading settings
- Acceptance criteria:
  - [ ] `Endpoint` (string) — PDP HTTP URL
  - [ ] `Mode` (enum: Instance, CustomEndpoint)
  - [ ] `FailureMode` (enum: Closed, Open)
  - [ ] `AdminEndpoint` (string?) — Optional Admin API URL
  - [ ] `AdminUsername` / `AdminPassword` (string?) — Optional Admin API creds
  - [ ] `IsInstanceDefault` (bool) — Whether using the instance PDP (for logging/diagnostics)
- Effort: S

**Task 2.3: Create CerbosMode and CerbosFailureMode enums**
- File: `Explore.Application/Models/CerbosConfiguration.cs` (same file)
- Acceptance criteria:
  - [ ] `CerbosMode { Instance = 0, CustomEndpoint = 1 }`
  - [ ] `CerbosFailureMode { Closed = 0, Open = 1 }`
- Effort: S

### Phase 3: Infrastructure Layer — CerbosConfigResolver (Effort: M)

**Task 3.1: Create CerbosConfigResolver**
- File: `Explore.Infrastructure/Services/CerbosConfigResolver.cs`
- Follows `SmtpConfigResolver` pattern exactly
- Acceptance criteria:
  - [ ] Injects `ISettingsResolver`, `ITenantContext`, `IMemoryCache`, `IOptions<CerbosSettings>`
  - [ ] 5-minute per-tenant cache (key: `CerbosConfig:{tenantId}`)
  - [ ] Checks `cerbos.tenant_customization_enabled` first — if false, returns instance default
  - [ ] Reads `cerbos.mode` to determine Instance vs CustomEndpoint
  - [ ] For CustomEndpoint: resolves endpoint, admin creds from settings/secrets
  - [ ] For Instance: returns instance PDP from `CerbosSettings.Endpoint`
  - [ ] Returns null when no Cerbos is configured at all
  - [ ] `InvalidateCache(Guid?)` for cache busting after settings change
- Effort: M
- Related Skills: `clean-architecture-rules`

### Phase 4: Infrastructure Layer — RuntimeAuthorizationProvider Refactor (Effort: L)

**Task 4.1: Refactor RuntimeAuthorizationProvider for BYO routing**
- File: `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`
- The central change: BYO-aware resolution
- Acceptance criteria:
  - [ ] Injects `ICerbosConfigResolver` for per-tenant config resolution
  - [ ] New resolution order:
    1. Resolve per-tenant Cerbos config via `ICerbosConfigResolver`
    2. If tenant has BYO (`CerbosMode.CustomEndpoint`): route to `CerbosAuthorizationService` with tenant endpoint
    3. If instance mode = "cerbos": route to instance `CerbosAuthorizationService`
    4. Else: route to `FallbackAuthorizationService`
  - [ ] BYO failure handling:
    - `FailureMode.Closed` → Safe-Mode via `FallbackAuthorizationService` (instance admin only)
    - `FailureMode.Open` → Standard `FallbackAuthorizationService`
  - [ ] Instance Cerbos failure → Standard `FallbackAuthorizationService` (existing behavior)
  - [ ] Structured logging for routing decisions
- Effort: L
- Related Skills: `clean-architecture-rules`, `error-tracking`

**Task 4.2: Add Safe-Mode to FallbackAuthorizationService**
- File: `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
- New parameter/flag for emergency-only access
- Acceptance criteria:
  - [ ] New method overload or parameter: `IsAllowedAsync(..., bool safeMode = false)`
  - [ ] When safeMode=true: only instance admin allowed, deny everything else
  - [ ] Clear logging when Safe-Mode is active
- Effort: S

**Task 4.3: Per-tenant HttpClient management for BYO endpoints**
- File: `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`
- Dynamic HttpClient creation for BYO endpoints
- Acceptance criteria:
  - [ ] New method or factory to create HttpClient for arbitrary Cerbos endpoint
  - [ ] Accept endpoint URL as parameter (instead of always using "CerbosClient")
  - [ ] Reuse instance client when using instance PDP
  - [ ] Connection timeout and resilience (same 2s timeout, circuit breaker pattern)
- Effort: M
- Related Skills: `error-tracking`

**Task 4.4: CerbosAuthorizationService BYO-aware overloads**
- File: `Explore.Infrastructure/Services/CerbosAuthorizationService.cs`
- Accept `CerbosConfiguration` to determine which endpoint to hit
- Acceptance criteria:
  - [ ] `IsAllowedAsync` / `IsAllowedBatchAsync` can accept a `CerbosConfiguration?` parameter
  - [ ] When config is null or Instance: use default "CerbosClient" (existing behavior)
  - [ ] When config is CustomEndpoint: use BYO HttpClient
  - [ ] Scope still applied (tenantId) regardless of endpoint
- Effort: M

### Phase 5: DI Registration (Effort: S)

**Task 5.1: Register CerbosConfigResolver**
- File: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- Wire up the new resolver
- Acceptance criteria:
  - [ ] `services.AddScoped<ICerbosConfigResolver, CerbosConfigResolver>()`
  - [ ] HttpClient factory for dynamic BYO clients if needed
- Effort: S

### Phase 6: Testing (Effort: L)

**Task 6.1: Unit tests for CerbosConfigResolver**
- Test cascading resolution (instance default, tenant override, locked settings)
- Test cache behavior
- Effort: M

**Task 6.2: Unit tests for RuntimeAuthorizationProvider BYO routing**
- Test routing: BYO → tenant PDP, instance Cerbos, fallback
- Test failure modes: closed → safe-mode, open → fallback
- Effort: M

**Task 6.3: Unit tests for Safe-Mode FallbackAuthorizationService**
- Test that safe-mode only allows instance admin
- Test that normal mode still works as before
- Effort: S

**Task 6.4: Architecture tests pass**
- Verify all existing architecture tests still pass
- Effort: S

**Task 6.5: Full build + all test suites pass**
- Build + run all 7 test projects
- Effort: S

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| BYO tenant PDP returns ALLOW for everything | Medium (tenant-scoped, only affects their data) | Audit logging; tenant accepts responsibility for their own PDP |
| Per-tenant HttpClient leaks | High (memory/connection exhaustion) | Use IHttpClientFactory with careful lifecycle; circuit breaker per endpoint |
| Cache invalidation race conditions | Low | 5-minute TTL is generous; settings changes are infrequent admin actions |
| CerbosConfigResolver adds latency to every request | Medium | 5-minute cache ensures resolver only hits DB every 5 minutes per tenant |
| Breaking existing Cerbos behavior | High | Instance-mode is the default; BYO is opt-in; all existing tests must pass |

## Potential Risks & Unknowns

The most likely complexity point is **Task 4.3/4.4 — per-tenant HttpClient management**. Creating HttpClients dynamically for arbitrary BYO endpoints requires careful lifecycle management. If a tenant changes their endpoint URL, the old client must be disposed and a new one created. The IHttpClientFactory named-client pattern doesn't natively support dynamic endpoints — we may need a `ConcurrentDictionary<string, HttpClient>` with TTL eviction, or a custom `IHttpMessageHandlerFactory`. This is the part most likely to need iteration.

Second risk: the `RuntimeAuthorizationProvider` refactor touches the critical authorization path. Every request flows through it. The refactor must be backward-compatible by default (instance mode unchanged), with BYO as a pure opt-in overlay.

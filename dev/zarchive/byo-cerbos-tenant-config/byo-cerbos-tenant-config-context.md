# Context: Per-Tenant Cerbos Configuration — BYO + Instance Isolation

> Last Updated: 2026-02-23

## Key Decisions

1. **BYO works regardless of instance mode** — Even if the instance uses FallbackAuthorizationService (no Cerbos), a tenant can bring their own Cerbos PDP and it will be used.
2. **Never fall back to instance PDP when BYO fails** — Tenant's policies might be stricter; falling to instance PDP could bypass their security.
3. **BYO failure modes are configurable per tenant** — `closed` (safe-mode: instance admin only) or `open` (standard RBAC fallback).
4. **Follows cascading settings pattern** — Same as SMTP/S3: `ISettingsResolver` → `SystemSetting` (lockable) → `TenantSetting` override.
5. **Instance-managed scoped policies deferred** — Scope field is already wired. Scoped policy YAML generation and tenant admin customization request flow come after BYO.
6. **Tenant admin can request policy customization** — Noted for Phase 2 implementation.

## Key Files

### Domain Layer
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` — Add `Cerbos` section (lines 129-132 is `Security` section)
- `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs` — Add `Cerbos` section for BYO credentials

### Application Layer
- `Explore.Application/Contracts/Infrastructure/ISmtpConfigResolver.cs` — **Pattern reference** for ICerbosConfigResolver
- `Explore.Application/Contracts/Infrastructure/ISettingsResolver.cs` — Cascading settings engine interface
- `Explore.Application/Contracts/Infrastructure/ICerbosConfigResolver.cs` — **NEW**: Per-tenant Cerbos config resolver
- `Explore.Application/Models/SmtpConfiguration.cs` — **Pattern reference** for CerbosConfiguration
- `Explore.Application/Models/CerbosConfiguration.cs` — **NEW**: Resolved Cerbos config POCO

### Infrastructure Layer
- `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` — **Pattern reference** (120 lines) for CerbosConfigResolver
- `Explore.Infrastructure/Services/CerbosConfigResolver.cs` — **NEW**: Resolves per-tenant Cerbos endpoint
- `Explore.Infrastructure/Services/CerbosAuthorizationService.cs` — **MODIFY**: Accept BYO endpoint override
- `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` — **MODIFY**: Add Safe-Mode flag
- `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs` — **MODIFY**: BYO-aware routing
- `Explore.Infrastructure/Services/PolicySyncService.cs` — Future: tenant-scoped policy sync
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs` — **MODIFY**: Register new services

### Configuration
- `cerbos/config/.cerbos.yaml` — Cerbos PDP config (reference only)
- `cerbos/policies/derived_roles.yaml` — 3-level admin hierarchy

## Essential Interface Signatures

### ISmtpConfigResolver (pattern to mirror)
```csharp
public interface ISmtpConfigResolver
{
    Task<SmtpConfiguration?> ResolveAsync(CancellationToken cancellationToken = default);
    void InvalidateCache(Guid? tenantId = null);
}
```

### SmtpConfigResolver resolution flow (pattern to mirror)
```csharp
// 1. Check cache (5-min TTL, key per tenant)
// 2. ISettingsResolver handles cascade:
//    - IsLocked at system level → system value (instance admin control)
//    - Tenant override exists → tenant value (BYO)
//    - Falls back to system default
// 3. Cache result
```

### CerbosSettings (existing)
```csharp
public class CerbosSettings
{
    public const string SectionName = "Cerbos";
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "http://localhost:3592";
}
```

### RuntimeAuthorizationProvider.ResolveProviderAsync (existing — to refactor)
```csharp
// Currently reads GovernanceSettingKeys.AuthorizationProvider → "cerbos" or "local"
// Returns _cerbosProvider or _localProvider
// 1-minute cache
```

### GovernanceSettingKeys pattern
```csharp
public static class Security
{
    public const string AuthorizationProvider = "authorization.provider";
}
```

### InfrastructureSecretSettingKeys pattern
```csharp
public static class Email
{
    public const string SmtpUsername = "email.smtp_username";
    public const string SmtpPassword = "email.smtp_password";
}
```

## Dependencies

- `ISettingsResolver` — existing, no changes needed
- `ITenantContext` — existing, provides current tenant ID
- `IMemoryCache` — existing, used for per-tenant caching
- `IHttpClientFactory` — existing, needs dynamic client support for BYO
- `IAdminContext` — existing, provides current user's admin status (needed for safe-mode)

## Session Progress

- [x] Analyzed codebase: CerbosAuthorizationService, RuntimeAuthorizationProvider, cascading settings
- [x] Analyzed patterns: SmtpConfigResolver, S3ConfigResolver, GovernanceSettingKeys
- [x] Architectural decisions confirmed with user
- [x] Plan created
- [ ] Implementation started

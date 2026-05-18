# Phase 3 Implementation Plan — ISecretResolver + Admin Bindings API + Enterprise Patterns

> **ABOUTME**: Enterprise-grade execution blueprint for Phase 3 of the secrets control-plane refactor.
> **ABOUTME**: Incorporates resilience patterns, audit trail, versioned rotation, HybridCache, structured validation, per-source health, and tenant isolation.

**Branch**: `develop`
**Last commits**: `fc0b2b5a` (Phase 2), `38ce8098` (Phase 1)
**Test baseline to preserve**: 1,305 green (Event.Application.UnitTests 823 + Event.Domain.UnitTests 207 + Event.Architecture.Tests 74 + Explore.Secrets.UnitTests 201)
**Commit target**: `refactor(secrets): phase 3 introduce ISecretResolver + admin bindings API + enterprise patterns`

## Standing User Directives (DO NOT VIOLATE)

1. **NO backward compatibility** — break/fix/iterate (dev mode)
2. **Enterprise-grade quality** — clean architecture, design patterns, highly maintainable
3. **Single Phase 3 commit** at the end
4. **Follow ALL repo conventions** in AGENTS.md + QUICK_REFERENCE.md
5. File-scoped namespaces for new C# files
6. Every file starts with a two-line `ABOUTME:` comment summary
7. Repositories return entities, not DTOs (map in handlers)
8. Validators are manually instantiated (no DI)
9. Commands return `BaseCommandResponse<Guid>` (create/update) or `BaseCommandResponse<bool>` (delete/validate)
10. HAL `_links` is the **exclusive** source of UI action affordance
11. Architecture tests must pass at every step

## Enterprise Architecture Decisions for Phase 3 (ADRs)

### ADR-003: Persistent Audit Trail
Every mutation on `SecretBinding` persists an immutable `SecretBindingAuditEntry` row. Read operations are 1% sampled (logs only). The audit handler runs synchronously before command response.

### ADR-004: Versioned Rotation (Blue/Green)
`SecretBinding` has `Version` (int) and `Status` (Active/Pending/Previous). Only `Status=Active` bindings are resolved. Promotion is atomic: Pending→Active, Active→Previous. Previous bindings are deleted after a grace period.

### ADR-005: HybridCache
Replace `IMemoryCache` with `HybridCache` for per-secret caching. L1 in-process, L2 distributed (Redis in production, in-process in dev). Tag-based invalidation for multi-instance propagation.

### ADR-006: Polly Resilience
Infisical calls wrapped in Polly policies: retry (3x, exponential backoff), circuit breaker (5 failures → 30s open), timeout (10s Infisical, 5s others), bulkhead (20 concurrent). Environment and Inline sources get timeout-only.

### ADR-007: Structured Validation
`SecretValidationCategory` enum provides actionable diagnostics: `SourceReachable`, `SourceUnreachable`, `CredentialValid`, `CredentialInvalid`, `BindingMisconfigured`, `InternalError`, `TtlExpired`. API consumers see the category but NOT the diagnostic message.

### ADR-008: Tenant Isolation
EF Core global query filter on `SecretBinding`: `Scope == Instance || ScopeId == _currentTenantId`. Admin handlers that need cross-tenant visibility use `.IgnoreQueryFilters()` gated by Cerbos.

### ADR-009: Lease/TTL Metadata
`SecretBinding.TtlExpiresAt` (DateTime?) for dynamic secret expiration. `LastRotatedAt` for rotation tracking. Health check degrades when TTL expired.

## Foundation State (Already Committed)

| Asset | Path | Status |
|---|---|---|
| `SecretBinding` entity + factory | `Explore.Domain/Secrets/SecretBinding{,.Factory}.cs` | ✅ Phase 1 |
| `SecretDefinition` + Registry | `Explore.Domain/Secrets/SecretDefinition{,Registry}.cs` | ✅ Phase 1 |
| Enums: `SecretScope`, `SecretSourceType`, `SecretValidationResult` | `Explore.Domain/Enums/` | ✅ Phase 1 |
| `ISecretBindingRepository` + impl | `Explore.Application/Contracts/Persistence/` + `Explore.Persistence/Repositories/` | ✅ Phase 1 |
| EF config + filtered unique indexes | `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs` | ✅ Phase 1 |
| Migration `AddSecretBindingsAndDataProtectionKeys` | `Explore.Persistence/Migrations/` | ✅ Phase 1 |
| `AddExploreDataProtection()` extension | `Explore.Persistence/Extensions/DataProtectionServiceCollectionExtensions.cs` | ✅ Phase 1 |
| `BootstrapSecretLoader` | `Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` | ✅ Phase 2 |

## Files Written But Uncommitted (Phase 3 Runtime — Needs Updates)

The following 14 files are on disk from a previous session. They need updates to incorporate enterprise patterns:

1. `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs` — needs Version + Status + AuditAction
2. `Explore.Application/Contracts/Secrets/ResolvedSecret.cs` — needs Version + TtlExpiresAt
3. `Explore.Application/Contracts/Secrets/ISecretResolver.cs` — needs ResolveRequiredAsync + ValidateAsync
4. `Explore.Application/Contracts/Secrets/ISecretSource.cs` — needs ValidateAsync returning SecretValidationDetail
5. `Explore.Application/Contracts/Secrets/IInfisicalClientFactory.cs` — minor updates
6. `Explore.Secrets/Sources/EnvironmentSecretSource.cs` — needs timeout-only Polly + ValidateAsync update
7. `Explore.Secrets/Sources/InlineSecretSource.cs` — needs timeout-only Polly + ValidateAsync update
8. `Explore.Secrets/Sources/InfisicalSecretSource.cs` — needs full Polly pipeline + ValidateAsync update
9. `Explore.Secrets/Infrastructure/InfisicalClientFactory.cs` — OK as-is (minor updates)
10. `Explore.Secrets/Observability/SecretResolverMetrics.cs` — needs resilience event counters
11. `Explore.Secrets/Services/SecretResolver.cs` — **MAJOR UPDATE**: HybridCache, Status=Active filter, version-aware, ResolveRequiredAsync, ValidateAsync
12. `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs` — **MAJOR UPDATE**: persistent audit trail via IAuditWriter
13. `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs` — **MAJOR UPDATE**: per-source granularity
14. `Explore.Secrets/Extensions/SecretResolutionServiceCollectionExtensions.cs` — **MAJOR UPDATE**: Polly, HybridCache, audit, resilience options

**IMPORTANT**: The following entities/columns need a NEW EF migration (the Phase 1 migration has already been applied). Phase 3 requires an additive migration adding:
- `SecretBinding.Version` (int, default 1)
- `SecretBinding.Status` (int enum: Active/Pending/Previous)
- `SecretBinding.TtlExpiresAt` (DateTime?)
- `SecretBinding.LastRotatedAt` (DateTime?)
- `SecretBinding.LastValidationCategory` (int enum)
- NEW `SecretBindingAuditEntries` table
- Updated filtered unique indexes (include `Status = Active` condition)

## New Files to Create (Enterprise Additions)

- `Explore.Domain/Secrets/SecretBindingAuditEntry.cs`
- `Explore.Domain/Secrets/SecretBindingAuditAction.cs`
- `Explore.Domain/Secrets/SecretBindingStatus.cs`
- `Explore.Domain/Secrets/SecretValidationCategory.cs`
- `Explore.Application/Contracts/Secrets/SecretValidationDetail.cs`
- `Explore.Application/Contracts/Secrets/SecretNotConfiguredException.cs`
- `Explore.Application/Contracts/Persistence/ISecretBindingAuditRepository.cs`
- `Explore.Secrets/Resilience/SecretResiliencePipeline.cs`
- `Explore.Secrets/Resilience/SecretResilienceOptions.cs`
- `Explore.Secrets/Services/SecretBindingAuditWriter.cs`
- `Explore.Secrets/Services/IAuditWriter.cs`
- `Explore.Persistence/Repositories/SecretBindingAuditRepository.cs`
- `Explore.Persistence/Configurations/Entities/SecretBindingAuditEntryConfiguration.cs`
- `Explore.Persistence/Migrations/{timestamp}_AddSecretBindingEnterpriseColumns.cs`

## Template Reference Files (READ FIRST in fresh session)

| New file pattern | Template to read first |
|---|---|
| Command class | `Explore.Application/Features/Categories/Requests/Commands/CreateCategoryCommand.cs` |
| Command handler | `Explore.Application/Features/Categories/Handlers/Commands/CreateCategoryCommandHandler.cs` |
| Query class | `Explore.Application/Features/Categories/Requests/Queries/GetCategoryDetailsRequest.cs` |
| Query handler | `Explore.Application/Features/Categories/Handlers/Queries/GetCategoryDetailsRequestHandler.cs` |
| DTO + Validator | `Explore.Application/DTOs/Category/CreateCategoryDto.cs` + `Validators/CreateCategoryDtoValidator.cs` |
| Controller | `Explore.API/Controllers/CategoryController.cs` |
| HATEOAS link policy | `Explore.API/Hateoas/Policies/CategoryLinkPolicy.cs` |
| HATEOAS assembler | `Explore.API/Hateoas/Assemblers/CategoryResourceAssembler.cs` |
| Cerbos policy | `cerbos/policies/category.yaml` |
| Notification handler | search for `INotificationHandler` in `Explore.Application/` |
| Entity + EF config | `Explore.Domain/Secrets/SecretBinding.cs` (already committed) |
| Audit entity pattern | search for `IAuditableEntity` implementations in `Explore.Domain/` |

Also read once:
- `Explore.API/Hateoas/RouteNames.cs` — exact `#region` style for new routes
- `Explore.Application/PipelineBehaviors/AuthorizationBehavior.cs` — Cerbos integration
- `Explore.Application/Mappings/MappingProfile.cs` — AutoMapper profile location
- `Explore.Application/ApplicationServicesRegistration.cs` — DI registration
- `Explore.Persistence/PersistenceServicesRegistration.cs` — DI registration
- `Explore.API/Program.cs` — `services.AddHybridCache()`, MediatR, and policy wiring blocks

## Implementation Order (Bottom-Up)

Execute in this exact order. Each section is atomic: complete fully before moving on.

---

### 3.0 — Read Templates + Verify Entity Reality (15 min)

**No file changes.** Read template files listed above. Confirm namespace/folder conventions.

**CRITICAL ENTITY REALITY CHECK** — verify these committed facts before writing code:
- `SecretBinding` primary key: `string SettingKey` (NOT `int SecretKeyId`)
- `SecretScope` enum: `Instance = 0, Tenant = 1`
- `SecretSourceType` enum: `Infisical = 0, InlineEncrypted = 1, EnvironmentVariable = 2`
- Factory methods: `CreateInfisical/CreateInlineEncrypted/CreateEnvironmentVariable`, `SwitchTo*`, `RecordValidation`
- Entity is `IAuditableEntity` (NOT `ISoftDeletable`) → hard delete
- Registry is `SecretDefinitionRegistry` with `FrozenDictionary` of known keys + `AllowedScopes`/`AllowedSources`/`IsBootstrap`

---

### 3.1 — EF Migration: Enterprise Schema Extensions

**New migration file**: `Explore.Persistence/Migrations/{timestamp}_AddSecretBindingEnterpriseColumns.cs`

Add to `SecretBindings` table:
- `Version` int NOT NULL DEFAULT 1
- `Status` int NOT NULL DEFAULT 0 (Active)
- `TtlExpiresAt` datetime2 NULL
- `LastRotatedAt` datetime2 NULL
- `LastValidationCategory` int NULL

Create `SecretBindingAuditEntries` table:
- `Id` uniqueidentifier NOT NULL (PK, default NEWSEQUENTIALID())
- `BindingId` uniqueidentifier NOT NULL (FK to SecretBindings)
- `SettingKey` nvarchar(256) NOT NULL
- `Scope` int NOT NULL
- `ScopeId` uniqueidentifier NULL
- `Action` int NOT NULL
- `SourceType` int NOT NULL
- `Version` int NULL
- `PreviousSourceType` int NULL
- `ValidationResult` int NULL
- `ValidationCategory` int NULL
- `DiagnosticMessage` nvarchar(1024) NULL
- `PerformedBy` uniqueidentifier NULL
- `PerformedAt` datetimeoffset NOT NULL DEFAULT NOW
- `IpAddress` nvarchar(45) NULL

Update filtered unique indexes:
- DROP existing `IX_SecretBindings_SettingKey_Instance` and `IX_SecretBindings_SettingKey_Tenant`
- CREATE `IX_SecretBindings_Active_Instance` ON `SecretBindings(SettingKey)` WHERE `Scope = 0 AND Status = 0` (Instance + Active)
- CREATE `IX_SecretBindings_Active_Tenant` ON `SecretBindings(SettingKey, ScopeId)` WHERE `Scope = 1 AND Status = 0` (Tenant + Active)

Add indexes on `SecretBindingAuditEntries`:
- `IX_SecretBindingAuditEntries_SettingKey_PerformedAt` (covering index for audit queries)
- `IX_SecretBindingAuditEntries_BindingId` (FK lookup)

Add CHECK constraints:
- `CK_SecretBindings_Version_Positive`: `Version > 0`
- `CK_SecretBindings_InlineC_NoTtl`: `SourceType <> 1 OR TtlExpiresAt IS NULL` (InlineEncrypted cannot have TTL)

**Verify**: Migration compiles. Run against test DB to confirm schema.

---

### 3.2 — Domain: New Enums + Audit Entity

#### 3.2.1 `Explore.Domain/Secrets/SecretBindingAuditAction.cs`

```csharp
// ABOUTME: Enum for audit action types recorded on every SecretBinding mutation.
// ABOUTME: Enables queryable audit trail for compliance and forensic debugging.

namespace Explore.Domain.Secrets;

public enum SecretBindingAuditAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Validated = 3,
    SourceSwitched = 4,
    VersionPromoted = 5,
    Rotated = 6,
    CacheInvalidated = 7
}
```

#### 3.2.2 `Explore.Domain/Secrets/SecretBindingStatus.cs`

```csharp
// ABOUTME: Lifecycle status for versioned secret rotation (blue/green model).
// ABOUTME: Only Active bindings are resolved by ISecretResolver.

namespace Explore.Domain.Secrets;

public enum SecretBindingStatus
{
    Active = 0,
    Pending = 1,
    Previous = 2
}
```

#### 3.2.3 `Explore.Domain/Secrets/SecretValidationCategory.cs`

```csharp
// ABOUTME: Structured validation categories for actionable diagnostics.
// ABOUTME: UI sees the category; diagnostic message is server-side only (info leakage prevention).

namespace Explore.Domain.Secrets;

public enum SecretValidationCategory
{
    SourceReachable = 0,
    SourceUnreachable = 1,
    CredentialValid = 2,
    CredentialInvalid = 3,
    BindingMisconfigured = 4,
    InternalError = 5,
    TtlExpired = 6
}
```

#### 3.2.4 `Explore.Domain/Secrets/SecretBindingAuditEntry.cs`

```csharp
// ABOUTME: Immutable audit trail entity for SecretBinding mutations.
// ABOUTME: Append-only — no updates, no deletes (GDPR/compliance retention via DBA).

namespace Explore.Domain.Secrets;

public sealed class SecretBindingAuditEntry : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid BindingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public SecretScope Scope { get; set; }
    public Guid? ScopeId { get; set; }
    public SecretBindingAuditAction Action { get; set; }
    public SecretSourceType SourceType { get; set; }
    public int? Version { get; set; }
    public SecretSourceType? PreviousSourceType { get; set; }
    public SecretValidationResult? ValidationResult { get; set; }
    public SecretValidationCategory? ValidationCategory { get; set; }
    public string? DiagnosticMessage { get; set; } // Max 1024, server-side only
    public Guid? PerformedBy { get; set; }
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; set; } // Max 45, for API-initiated actions

    // Navigation
    public SecretBinding? Binding { get; set; }

    // IAuditableEntity (write-once since this is append-only)
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; } // Always null for append-only
    public Guid? UpdatedBy { get; set; }      // Always null for append-only
    public byte[] RowVersion { get; set; } = [];
}
```

#### 3.2.5 Update `Explore.Domain/Secrets/SecretBinding.cs`

Add these properties and update factory methods:

```csharp
// New columns
public int Version { get; private set; } = 1;
public SecretBindingStatus Status { get; private set; } = SecretBindingStatus.Active;
public DateTime? TtlExpiresAt { get; private set; }
public DateTime? LastRotatedAt { get; private set; }
public SecretValidationCategory? LastValidationCategory { get; private set; }

// New factory methods
public static SecretBinding CreateWithPendingVersion(...) { /* Status = Pending, Version = 1 */ }
public void PromoteToActive(Guid performedBy) { Status = SecretBindingStatus.Active; Version++; LastRotatedAt = DateTime.UtcNow; }
public void DemoteToPrevious(Guid performedBy) { Status = SecretBindingStatus.Previous; }

// Updated RecordValidation
public void RecordValidation(SecretValidationResult result, SecretValidationCategory category, string? message = null)
{
    LastValidationResult = result;
    LastValidationCategory = category;
    LastValidationMessage = message?[..Math.Min(message.Length, 512)];
    LastValidatedAt = DateTime.UtcNow;
    LastValidatedBy = performedBy;
}
```

---

### 3.3 — Domain Event Update

**File**: `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs` (already on disk)

Update to include versioning and audit action:

```csharp
public sealed record SecretBindingUpdatedEvent(
    Guid BindingId,
    string SettingKey,
    SecretScope Scope,
    Guid? ScopeId,
    SecretSourceType SourceType,
    SecretBindingChangeKind ChangeKind,
    SecretBindingAuditAction AuditAction, // NEW
    int Version,                           // NEW
    SecretBindingStatus Status,            // NEW
    DateTimeOffset OccurredAt
) : INotification; // If MediatR is not accessible from Domain, keep as plain record
```

**NOTE**: If `INotification` coupling in Domain is a clean-architecture violation, keep the domain event as a plain record and create the Application-layer wrapper `SecretBindingChangedNotification : INotification` as planned.

---

### 3.4 — Application Contracts (Enhanced)

#### 3.4.1 `Explore.Application/Contracts/Secrets/SecretValidationDetail.cs` (NEW)

```csharp
// ABOUTME: Structured validation result with actionable category and internal diagnostic message.
// ABOUTME: API consumers see the category only; diagnostic message is server-side.

namespace Explore.Application.Contracts.Secrets;

public sealed record SecretValidationDetail(
    SecretValidationResult Result,
    SecretValidationCategory Category,
    string? DiagnosticMessage // Server-side ONLY — never exposed in API responses
);
```

#### 3.4.2 `Explore.Application/Contracts/Secrets/SecretNotConfiguredException.cs` (NEW)

```csharp
// ABOUTME: Exception thrown by ISecretResolver.ResolveRequiredAsync when a binding is not configured.
// ABOUTME: Differentiates "not configured" from "configured but source returned null".

namespace Explore.Application.Contracts.Secrets;

public sealed class SecretNotConfiguredException : Exception
{
    public string SettingKey { get; }
    public Guid? TenantId { get; }

    public SecretNotConfiguredException(string settingKey, Guid? tenantId)
        : base($"Secret binding for '{settingKey}' is not configured{(tenantId.HasValue ? $" for tenant {tenantId.Value}" : " at instance scope")}.")
    {
        SettingKey = settingKey;
        TenantId = tenantId;
    }
}
```

#### 3.4.3 `Explore.Application/Contracts/Secrets/ISecretResolver.cs` (UPDATE existing on disk)

```csharp
// ABOUTME: Primary abstraction for resolving a secret value from its declared single source.
// ABOUTME: NO fallback chains — the binding row dictates exactly which source is consulted.

namespace Explore.Application.Contracts.Secrets;

public interface ISecretResolver
{
    Task<ResolvedSecret?> TryResolveAsync(string settingKey, Guid? tenantId, CancellationToken ct);
    Task<ResolvedSecret> ResolveRequiredAsync(string settingKey, Guid? tenantId, CancellationToken ct);
    Task<SecretBindingDescriptor> DescribeAsync(string settingKey, Guid? tenantId, CancellationToken ct);
    Task<IReadOnlyList<SecretBindingDescriptor>> DescribeAllAsync(Guid? tenantId, CancellationToken ct);
    Task InvalidateAsync(string settingKey, Guid? tenantId, CancellationToken ct);
    Task<SecretValidationDetail> ValidateAsync(string settingKey, Guid? tenantId, CancellationToken ct);
}
```

#### 3.4.4 `Explore.Application/Contracts/Secrets/ResolvedSecret.cs` (UPDATE existing)

Add `Version` and `TtlExpiresAt`:

```csharp
public sealed record ResolvedSecret(
    string SettingKey,
    string Value,
    SecretSourceType Source,
    SecretScope Scope,
    Guid? ScopeId,
    int Version,
    DateTimeOffset ResolvedAt,
    DateTimeOffset? TtlExpiresAt
);
```

#### 3.4.5 `Explore.Application/Contracts/Secrets/ISecretSource.cs` (UPDATE existing)

```csharp
public interface ISecretSource
{
    SecretSourceType SourceType { get; }
    Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken ct);
    Task<SecretValidationDetail> ValidateAsync(SecretBinding binding, CancellationToken ct);
}
```

#### 3.4.6 `Explore.Application/Contracts/Persistence/ISecretBindingAuditRepository.cs` (NEW)

```csharp
// ABOUTME: Repository for the append-only audit trail of SecretBinding mutations.
// ABOUTME: No update/delete methods — audit entries are immutable.

namespace Explore.Application.Contracts.Persistence;

public interface ISecretBindingAuditRepository
{
    Task AddAsync(SecretBindingAuditEntry entry, CancellationToken ct);
    Task<IReadOnlyList<SecretBindingAuditEntry>> GetRecentAsync(string settingKey, int count, CancellationToken ct);
}
```

#### 3.4.7 `Explore.Application/Contracts/Secrets/IAuditWriter.cs` (NEW)

```csharp
// ABOUTME: Abstraction for writing audit trail entries from the resolver layer.
// ABOUTME: Implementation writes to ISecretBindingAuditRepository + structured log.

namespace Explore.Application.Contracts.Secrets;

public interface IAuditWriter
{
    Task WriteAsync(SecretBindingAuditEntry entry, CancellationToken ct);
}
```

---

### 3.5 — Resilience Pipeline (Polly)

**File**: `Explore.Secrets/Resilience/SecretResiliencePipeline.cs`

```csharp
// ABOUTME: Polly resilience policies for secret source calls.
// ABOUTME: Infisical gets retry + circuit breaker + timeout + bulkhead.
// ABOUTME: Environment and Inline sources get timeout-only (local operations).

namespace Explore.Secrets.Resilience;

public sealed class SecretResiliencePipeline
{
    public ResiliencePipeline<HttpResponseMessage> InfisicalPipeline { get; }
    public ResiliencePipeline LocalSourcePipeline { get; }

    public SecretResiliencePipeline(SecretResilienceOptions options, ILogger<SecretResiliencePipeline> logger)
    {
        // Build Infisical pipeline: retry + circuit breaker + timeout + bulkhead
        InfisicalPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions { ... }) // 3 retries, 500ms/1s/2s exponential backoff
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions { ... }) // 5 failures → 30s open
            .AddTimeout(TimeSpan.FromSeconds(options.InfisicalTimeoutSeconds))
            .AddConcurrencyLimiter(options.MaxConcurrentInfisicalCalls) // 20
            .Build();

        // Build local pipeline: timeout-only
        LocalSourcePipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(options.LocalSourceTimeoutSeconds))
            .Build();
    }
}
```

**File**: `Explore.Secrets/Resilience/SecretResilienceOptions.cs`

```csharp
// ABOUTME: Configuration options for secret source resilience policies.
// ABOUTME: Binds from SecretProvider:Resilibility configuration section.

namespace Explore.Secrets.Resilience;

public sealed class SecretResilienceOptions
{
    public int RetryCount { get; set; } = 3;
    public double RetryBaseDelaySeconds { get; set; } = 0.5;
    public int CircuitBreakerFailureCount { get; set; } = 5;
    public double CircuitBreakerOpenDurationSeconds { get; set; } = 30;
    public int InfisicalTimeoutSeconds { get; set; } = 10;
    public int LocalSourceTimeoutSeconds { get; set; } = 5;
    public int MaxConcurrentInfisicalCalls { get; set; } = 20;
}
```

**NuGet requirement**: `Microsoft.Extensions.Http.Polly` (or `Polly` + `Polly.Extensions.Http`) — must be added to `Explore.Secrets.csproj`.

---

### 3.6 — Per-Source Implementations (Updated)

All source implementations update to:
1. Use Polly policies from `SecretResiliencePipeline`
2. Return `SecretValidationDetail` from `ValidateAsync`
3. Emit timing metrics to `SecretResolverMetrics`

#### 3.6.1 `EnvironmentSecretSource.cs`
- Wrapped in `LocalResiliencePipeline` (timeout-only)
- `ValidateAsync` returns `SecretValidationDetail(SourceReachable, CredentialValid, null)` when var is set, or `SecretValidationDetail(Success, SourceReachable, null)` when missing
- `GetSecretAsync` emits `resolve.duration_ms` via metrics

#### 3.6.2 `InlineSecretSource.cs`
- Wrapped in `LocalResiliencePipeline` (timeout-only)
- `ValidateAsync` returns `SecretValidationDetail(Success, CredentialValid, null)` on roundtrip, or `(Failure, CredentialInvalid, exc.Message)` on CryptographicException

#### 3.6.3 `InfisicalSecretSource.cs`
- Wrapped in `InfisicalResiliencePipeline` (retry + circuit breaker + timeout + bulkhead)
- `GetSecretAsync` returns null on source error after retry exhaustion
- `ValidateAsync` distinguishes `SourceReachable`/`SourceUnreachable`/`CredentialValid`/`CredentialInvalid`
- Circuit breaker state exposed for health check

---

### 3.7 — Core Resolver (Major Update)

**File**: `Explore.Secrets/Services/SecretResolver.cs` (already on disk, needs complete rewrite)

Key changes:
- **HybridCache** replaces `IMemoryCache`
- **Status = Active filter**: only resolve Active bindings
- **Version-aware cache key**: includes binding version
- **Tag-based invalidation**: `HybridCache.RemoveByTagAsync($"secret-binding:{settingKey}:{scope}:{scopeId}")`
- **ResolveRequiredAsync**: throws `SecretNotConfiguredException` on null
- **ValidateAsync**: returns `SecretValidationDetail`
- **L2 graceful fallback**: if Redis unavailable, HybridCache falls back to L1

---

### 3.8 — Auditing Decorator (Major Update)

**File**: `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs` (already on disk, needs update)

Key changes:
- **Write operations**: every create/update/delete/validate persists `SecretBindingAuditEntry` via `IAuditWriter`
- **Read operations**: 1% sampled (configurable) → structured log only
- **Audit entries include**: `IpAddress` from `IHttpContextAccessor` (when available)
- **Never logs/values**: only key + source + scope + outcome

**File**: `Explore.Secrets/Services/SecretBindingAuditWriter.cs` (NEW)

Implements `IAuditWriter`:
- Persists `SecretBindingAuditEntry` via `ISecretBindingAuditRepository`
- Also emits structured log (for observability)

---

### 3.9 — Health Check (Major Update)

**File**: `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs` (already on disk, needs update)

Key changes:
- Returns `Dictionary<string, HealthStatus>` per source type
- Infisical health includes circuit breaker state
- Degraded conditions: binding with `TtlExpiresAt < DateTime.UtcNow`, binding with `LastValidationResult = Failure` > 1 hour
- Overall: Healthy if all healthy, Degraded if any degraded, Unhealthy if any unhealthy

---

### 3.10 — Tenant Isolation Query Filter

**File**: `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs` (UPDATE committed file)

Add query filter:
```csharp
.HasQueryFilter("TenantSecretIsolation", e =>
    e.Scope == SecretScope.Instance ||
    e.ScopeId == _tenantContext.CurrentTenantId)
```

Where `ITenantContext` provides the current tenant ID from the request pipeline.

**Architecture test**: every `IgnoreQueryFilters()` call on `SecretBinding` must be in a method gated by `[Authorize]` + Cerbos `secret_binding:manage_instance`.

---

### 3.11 — DI Registration (Major Update)

**File**: `Explore.Secrets/Extensions/SecretResolutionServiceCollectionExtensions.cs` (already on disk, needs major update)

Add:
- `services.Configure<SecretResilienceOptions>(configuration.GetSection("SecretProvider:Resilience"))`
- `services.AddSingleton<SecretResiliencePipeline>()`
- `services.AddHybridCache()` (or verify already registered in `Program.cs`)
- `services.AddScoped<IAuditWriter, SecretBindingAuditWriter>()`
- `services.AddScoped<ISecretBindingAuditRepository, SecretBindingAuditRepository>()`
- Polly pipeline registration per source type
- Tenant isolation: `ITenantContext` registration

Wire in `Program.cs` (API + Blazor).

---

### 3.12 — DTOs + Validators

In `Explore.Application/DTOs/SecretBindings/`:

- `SecretBindingDto.cs` — includes `Version`, `Status`, `TtlExpiresAt`, `LastValidationCategory`
- `SecretBindingListDto.cs` — includes `Version`, `Status`
- `CreateSecretBindingDto.cs` — `SourceType`, metadata fields, `InlineSecretValue?`
- `UpdateSecretBindingDto.cs` — includes `InlineSecretValue?` for re-encryption on update
- `PromoteSecretBindingDto.cs` — binding ID only (promotion is explicit action)
- `ValidateSecretBindingDto.cs` — binding ID
- Validators enforce `SecretDefinitionRegistry` constraints + `TtlExpiresAt` null for `InlineEncrypted`

---

### 3.13 — CQRS Commands (with audit + version handling)

In `Explore.Application/Features/SecretBindings/`:

- `CreateSecretBindingCommand` → factory call + audit entry (Created) + domain event
- `UpdateSecretBindingCommand` → detect source-switch + version bump + audit (Updated/SourceSwitched) + domain event
- `DeleteSecretBindingCommand` → hard delete + audit (Deleted) + domain event
- `ValidateSecretBindingCommand` → call `ISecretSource.ValidateAsync` + `RecordValidation` with category + audit (Validated)
- `PromoteSecretBindingCommand` → Status: Pending→Active, Active→Previous, version++ + audit (VersionPromoted) + domain event + cache invalidation

All handlers:
- Publish `SecretBindingChangedNotification` via `IMediator.Publish`
- Write `SecretBindingAuditEntry` via `IAuditWriter`
- Use `SecretDefinitionRegistry` for validation
- Validators manually instantiated (no DI)

---

### 3.14 — CQRS Queries

- `GetSecretBindingListRequest` — paged, filtered by scope/scopeId, includes version/status/TTL
- `GetSecretBindingDetailsRequest` — single binding by ID, includes audit history (last 10 entries)
- `GetAvailableSecretsForOnboardingRequest` — enumerates registry + binding state

Instance admin queries use `.IgnoreQueryFilters()` for cross-tenant visibility.

---

### 3.15 — Notification Handlers

- `InvalidateSecretCacheOnUpdatedHandler` → `ISecretResolver.InvalidateAsync` (tag-based HybridCache invalidation)
- `SecretBindingAuditPersistenceHandler` → persists `SecretBindingAuditEntry` via `IAuditWriter`
- `KeycloakSchemeRefreshHandler` → stub (logs warning, awaits Phase 4)

---

### 3.16 — Controller, HATEOAS, Cerbos

- `SecretBindingsController` with all endpoints including `POST /{id}/promote`
- Route names in `RouteNames.cs`
- `SecretBindingDetailLinkPolicy` + `SecretBindingCollectionLinkPolicy`
- `SecretBindingResourceAssembler`
- Cerbos policy: `secret_binding.yaml` with `view`, `create`, `update`, `delete`, `validate`, `promote` actions

---

### 3.17 — Tests (~60-70 new tests)

Critical test categories:

1. **No-fallback**: binding with `SourceType=EnvironmentVariable` never triggers Infisical SDK calls
2. **No-leak**: API responses never contain plaintext/ciphertext/environment variable values
3. **Resilience**: Infisical returns null after 3 retries; circuit breaker opens; env-var/inline resolve without retry overhead
4. **Audit trail**: every create/update/delete/validate/publish/persist produces a `SecretBindingAuditEntry` with correct action
5. **Version rotation lifecycle**: create Pending → validate → promote → Active version changes → cache invalidated → Previous still accessible during grace period
6. **Tenant isolation**: tenant A cannot resolve tenant B's secrets via `ISecretResolver`; instance admin CAN see all bindings via admin endpoint with `IgnoreQueryFilters()`
7. **Structured validation**: `ValidateAsync` returns `SecretValidationDetail` with `Category` and `DiagnosticMessage`
8. **Per-source health**: health check returns individual source statuses; Infisical unreachable → Degraded (NOT Unhealthy)
9. **HybridCache**: verify tag-based invalidation propagates
10. **Architecture test**: `Domain.Secrets` has no Infisical/DataProtection/Polly refs
11. **Architecture test**: every `IgnoreQueryFilters()` on `SecretBinding` is Cerbos-gated
12. **HybridCache fallback**: Redis unavailable → L1-only resolution still works

---

### 3.18 — Verification (MANDATORY before commit)

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
```

**Acceptance gate**: All pass + new test count ≥ 1,365 (1,305 baseline + ~60 new).

If integration tests added:
```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

---

### 3.19 — Commit

Single Phase 3 commit. **Do NOT push.** Stop and report.

---

### 3.20 — Update Dev-Docs (post-commit)

Edit context file and tasks file to mark Phase 3 complete.

---

## Risk Register (Read Before Starting)

| Risk | Mitigation |
|---|---|
| `SecretBinding` property names differ from this plan | First action: open committed entity file and verify exact names |
| AutoMapper profile location varies | Read `Explore.Application/Mappings/` first |
| `IUnitOfWork` may not exist by that name | Search for `SaveAsync` / `SaveChangesAsync` patterns in existing handlers |
| Cerbos derived role `tenant_admin` may differ | Check `cerbos/derived_roles/explore_admin_roles.yaml` |
| Output cache eviction by tag may not be wired | Fall back to time-based 30-sec invalidation (acceptable for admin UI) |
| Infisical SDK API surface changed | If `Infisical.Sdk` v3 doesn't match `GetSecretRawAsync`, consult docs via Context7 MCP |
| `IDynamicAuthSchemeManager` not yet present | Stub the Keycloak refresh handler (log warning); Phase 4 wires real refresh |
| Domain event vs MediatR `INotification` | Keep domain event as plain record in Domain; wrap with `SecretBindingChangedNotification : INotification` in Application |
| `HybridCache` API surface differs from `IMemoryCache` | Verify `AddHybridCache()` registration and `SetAsync`/`GetOrCreateAsync`/`RemoveByTagAsync` methods |
| Polly `ResiliencePipeline` builder API | Verify against `Microsoft.Extensions.Http.Polly` or `Polly` docs via Context7 MCP |
| `ITenantContext` injection for query filter | May need to create or find the existing tenant context service in the codebase |
| Circuit breaker state sharing across instances | HybridCache L2 + `RemoveByTagAsync` handles cross-instance invalidation; circuit breaker state is per-instance (acceptable) |

## Estimated Scope

- **New files**: ~55
- **Modified files**: ~8
- **Net lines**: ~7,000–9,000 (production) + ~3,000 (tests)
- **Time budget for fresh session**: 8-12 hours of focused work (enterprise additions add ~40% over original plan)
- **Tests added**: ~60-70

## Post-Phase-3 — Phase 4 Preview

Phase 4 will:
- Refactor `AuthProviderConfigurationService` to use `ISecretResolver`
- Wire `IDynamicAuthSchemeManager` to real Keycloak scheme refresh
- Update Onboarding UI with auto-detect chips
- Remove `/internal` endpoint that exposed AppSetting values
- Add batch resolve API for onboarding
- Enforce tenant isolation query filter in handlers
- Migrate existing AppSetting Keycloak rows to SecretBinding rows

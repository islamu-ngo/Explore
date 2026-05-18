ABOUTME: Enterprise-grade strategic plan for refactoring secrets architecture into a control-plane/data-plane separation model.
ABOUTME: Single-source-of-truth SecretBinding registry with resolver, resilience patterns, audit trail, versioned rotation, and tenant isolation. Eliminates the Infisical → env → AppSetting fallback chain.

# Plan: Secrets Refactor — Control Plane / Data Plane Separation

Last Updated: 2026-04-24
Version: 2.0 (Enterprise Revision)

## Executive Summary

The current secrets implementation couples provider lookup, inline DB encryption, and `IConfiguration` overlays into a single global fallback chain (Infisical → env/appsettings → AppSetting). That chain is exactly the ambiguity we must eliminate: any given secret has no single owner, operators cannot tell what is live, and source precedence is a global runtime setting instead of a per-secret decision.

This refactor treats **the database as the control plane** and **Infisical / environment variables / inline-encrypted storage as the data plane**, with enterprise-grade resilience, observability, and tenant isolation:

1. For each secret-backed setting, a `SecretBinding` row in Postgres declares WHERE the value comes from (source type + normalized metadata). No binding = inherits from parent scope (or absent).
2. A single `ISecretResolver` dispatches on the binding's source type and fetches from **exactly one** source. There is no fallback chain and no "DB first then Infisical" conflict logic.
3. Every mutation and resolution is persisted in an immutable audit trail (`SecretBindingAuditEntry`), queryable for compliance and debugging.
4. Bindings support **versioned rotation** (active/pending/previous) for zero-downtime blue/green credential rotation.
5. All external source calls (Infisical) are wrapped in **Polly resilience policies** (retry + circuit breaker + timeout).
6. Caching uses **HybridCache** (L1 memory + L2 distributed) for multi-instance deployment correctness.
7. Validation produces **structured, categorized results** (not binary pass/fail) for actionable diagnostics.
8. Health checks expose **per-source granularity** — operators can see that Infisical is degraded while env-var bindings are healthy.
9. Tenant-scoped bindings are **isolated by EF Core query filter** — no cross-tenant data leakage at the ORM level.
10. The Infisical layout is redesigned to the clean folder-per-concern structure (`api/`, `storage/`, `keycloak/`, `postgresql/`, `smtp/`, `analytics/`, `ai/`).
11. Postgres boot secrets become **five discrete fields** composed via `NpgsqlConnectionStringBuilder`. The legacy `POSTGRESQL_PUBLIC_URL` is deleted.
12. The UI never renders secret values. It renders **state + metadata**: configured/not configured, which source, source-specific metadata, last validation result, last updated by/when.
13. Missing secrets never crash the platform. A minimal deployment (API + Blazor + Postgres) works; every other feature degrades gracefully until its secrets are configured.
14. The onboarding flow reads "what resolves" to auto-select providers and exposes explicit input flows for the three source types.
15. Bindings carry **TTL/lease metadata** for Vault-style dynamic secret expiration tracking.
16. A **file-based secret source** (`/run/secrets/`) supports Docker/Kubernetes secret mounts.

Oracle review confirmed the direction with four key adjustments: (a) split bootstrap secrets from runtime bindings; (b) use a centralized `SecretDefinitionRegistry` as policy source-of-truth; (c) use normalized metadata columns with DB check constraints instead of polymorphic JSON; (d) drop `Module` scope and `Inherited` source type from v1 (absence = inheritance; modules use tenant-scoped namespaced keys).

Delivery is a six-PR sequence. No backward compatibility is preserved; we are in development mode. The destructive EF migration drops `AppSettings` and related infrastructure as part of PR 6.

---

## Architecture Decision Records (ADRs)

### ADR-001: DB as Control Plane, External Sources as Data Plane

**Context**: Current system has a global fallback chain that makes it impossible to determine which source is authoritative for any given secret.

**Decision**: One `SecretBinding` row per (SettingKey, Scope, ScopeId) tuple declares the single source. The resolver dispatches to that source and only that source. If the source returns null, the resolver returns null — it never tries another source.

**Consequences**: Operators see exactly what is live. No ambiguity. Debugging is deterministic. But: operator must explicitly choose a source at binding time; no "set it and forget it" fallback.

### ADR-002: Normalized Metadata Columns over Polymorphic JSON

**Context**: SecretBinding could store source-specific metadata as polymorphic JSON (`{ "envVar": "SMTP_PASSWORD" }` vs `{ "path": "/smtp/", "key": "SMTP_PASSWORD" }`).

**Decision**: Use separate nullable columns (`InfisicalEnvironment`, `InfisicalPath`, `InfisicalKey`, `EnvironmentVariableName`, `InlineCiphertext`, `InlineCiphertextVersion`) with a CHECK constraint enforcing exactly one metadata group per source type.

**Consequences**: Strong type safety, DB-level integrity, simpler EF mappings, indexable metadata. Trade-off: wider row, but secret bindings number in the hundreds at most.

### ADR-003: Persistent Audit Trail (Not Just Structured Logs)

**Context**: The original plan relied on 1% sampled structured logs for audit. Enterprise compliance (SOC 2, ISO 27001) requires persistent, queryable audit trails.

**Decision**: Introduce `SecretBindingAuditEntry` as a separate entity/table. Every mutation (create, update, delete, validate, source-switch, rotation) writes an immutable row. Read operations are still 1% sampled via the decorator but critical operations are fully persisted.

**Consequences**: Full audit trail for who changed what and when. Slightly higher write volume on binding mutations (negligible for hundreds of bindings). Enables compliance reporting and forensic debugging.

### ADR-004: Versioned Rotation (Blue/Green Secret Rotation)

**Context**: Zero-downtime secret rotation requires that a new credential can be staged alongside the current one, and consumers switch atomically.

**Decision**: Add `Version` (int, monotonically increasing) and `Status` (`Active`/`Pending`/`Previous`) columns to `SecretBinding`. Rotation workflow: (1) create Pending version with new credential, (2) validate, (3) atomically promote Pending→Active and demote Active→Previous, (4) cache invalidation event, (5) after grace period, hard-delete Previous.

**Consequences**: Zero-downtime rotation possible. UI shows version history. Trade-off: adds complexity to the resolver (must resolve Active version only). But the resolver already filters by binding, so `Status=Active` is just an additional filter.

### ADR-005: HybridCache over IMemoryCache

**Context**: `IMemoryCache` is local to a single process instance. In multi-instance deployment (API replicas behind load balancer), cache invalidation on one instance doesn't propagate to others.

**Decision**: Use `HybridCache` (already in the codebase dependency) for per-secret caching. HybridCache provides L1 (in-process memory) + L2 (distributed, Redis/SQL) with automatic tag-based invalidation.

**Consequences**: Multi-instance deployments get correct cache semantics. L1 hit is still nanosecond-fast. L2 hit is millisecond-fast. Trade-off: requires Redis or SQL-based L2 provider in production. For single-instance dev, HybridCache falls back to L1-only gracefully.

### ADR-006: Polly Resilience Policies on External Source Calls

**Context**: Infisical API calls can fail transiently (network errors, rate limits, temporary unavailability). The original plan had no retry or circuit-breaking.

**Decision**: Wrap all `ISecretSource` implementations with Polly policies:
- **Retry**: 3 retries with exponential backoff (500ms, 1s, 2s) on `HttpRequestException` and `TimeoutException`.
- **Circuit Breaker**: 5 consecutive failures opens for 30 seconds. Half-open allows one probe; success resets.
- **Timeout**: 10-second per-call timeout on Infisical calls; 5-second on env-var and inline (should be near-instant).

**Consequences**: Transient Infisical failures are absorbed by retry. Circuit breaker prevents cascading failure. Timeouts prevent hanging resolve calls. All resilience metrics are emitted to OpenTelemetry.

### ADR-007: Structured Validation Results

**Context**: The original `SecretValidationResult` enum (`NotValidated`/`Success`/`Failure`) provides no diagnostic information on failure.

**Decision**: Replace with `SecretValidationResult` enum kept for DB storage (backward-compat with existing column) plus a richer `SecretValidationDetail` record returned by `ISecretSource.ValidateAsync`:
```csharp
public sealed record SecretValidationDetail(
    SecretValidationResult Result,
    SecretValidationCategory Category,
    string? DiagnosticMessage);
```
Categories: `SourceReachable`, `SourceUnreachable`, `CredentialValid`, `CredentialInvalid`, `BindingMisconfigured`, `InternalError`, `TtlExpired`.

**Consequences**: UI and API can display actionable diagnostics ("Infisical reachable but credential invalid" vs "Infisical unreachable"). Operators debug faster. `DiagnosticMessage` is logged server-side; UI gets the category only (not the message, to avoid info leakage).

### ADR-008: Tenant Isolation via Query Filter

**Context**: `SecretBinding` rows can be Instance-scoped or Tenant-scoped. Without a query filter, a code path that forgets to filter by tenant could read all tenant secrets.

**Decision**: Add a global EF Core query filter on `SecretBinding` that enforces `Scope == SecretScope.Instance || ScopeId == _currentTenantId` when tenant context is available. Admin endpoints explicitly bypass this filter for cross-tenant management.

**Consequences**: Impossible to accidentally query another tenant's secrets at the ORM level. Admin operations that need cross-tenant view use `IgnoreQueryFilters()`. Trade-off: must ensure admin handlers explicitly opt out of the filter.

### ADR-009: Lease/TTL Metadata on Bindings

**Context**: Vault-style dynamic secrets have expiration times. Even for static secrets, operators want to know when credentials were last rotated.

**Decision**: Add `TtlExpiresAt` (DateTime?) and `LastRotatedAt` (DateTime?) nullable columns to `SecretBinding`. `TtlExpiresAt` is set when the binding points to a dynamic/leased secret (future Vault integration). `LastRotatedAt` is set on every source-switch or version promotion.

**Consequences**: UI can show "expires in 4h" for dynamic secrets. Health check degrades when `TtlExpiresAt < DateTime.UtcNow`. Enables future automated rotation workflows.

### ADR-010: File-Based Secret Source for Docker/Kubernetes

**Context**: Docker and Kubernetes mount secrets as files under `/run/secrets/` (Docker) or configurable paths (K8s). The initial plan had no support for this pattern.

**Decision**: Add `SecretSourceType.File` (value `3`) to the enum in Phase 5 with `FileSecretSource` implementation reading from disk. `SecretBinding` gets `FilePath` (string?) normalized column. This is a SHOULD-HAVE, not blocking for v1.

**Consequences**: K8s-native deployments can mount secrets as files without env vars. Trade-off: adds one more source type to the resolver and UI. Phase 5 extends the CHECK constraint.

---

## Current State Analysis

### Verified Existing Architecture (the anti-pattern to eliminate)

- `Explore.Secrets/Abstractions/ISecretProvider.cs` defines the single global provider abstraction with `SecretProviderType` enum. Only `None`, `Infisical`, and `Environment` are implemented today.
- `Explore.Secrets/Configuration/InfisicalConfigurationSource.cs` + `InfisicalConfigurationProvider.cs` register Infisical values as an `IConfiguration` overlay.
- `Explore.Secrets/Configuration/DbConfigurationSource.cs` + `DbConfigurationProvider.cs` load the `AppSetting` table and decrypt values via `AesEncryptionService` (AES-256-GCM).
- `Explore.Secrets/Services/SecretRefreshService.cs` is a hosted background service that polls Infisical on an interval and updates the provider cache.
- `Explore.API/Extensions/ConfigurationExtensions.cs` maps legacy Infisical names to canonical keys (`POSTGRESQL_PUBLIC_URL`, `ISLAMU_EVENT_S3_*`, `KEYCLOAK_PUBLIC_URL`, etc.).
- Effective resolution precedence today is: **Infisical overlay → `IConfiguration` (env vars + appsettings) → `AppSetting` via `DbConfigurationProvider`**. This is global — no per-secret opt-out. This is what the user rejects.

### Verified Settings Entities (three tables exist today)

- `Explore.Domain/AppSetting.cs` (PK `ConfigKey`, `EncryptedValue`, `KeyVersion`, `IsSensitive`, `Category`, `ValueType`). CHECK constraint blocks `Database:*`, `Security:MasterKey*`, `ConnectionStrings:*` keys.
- `Explore.Domain/SystemSetting.cs` (governance key/value, JSON serialized). Used for instance-scope governance and auth provider secrets (anti-pattern - plain JSON, no encryption).
- `Explore.Domain/TenantSetting.cs` (tenant override for governance keys). Separate from typed `TenantSettingsDocument` payloads; neither storage path should contain secrets.
- `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs` defines the "logical secret key" namespace that currently leaks secret values into `SystemSetting.Value` JSON.

### Verified Consumers

- **SETUP_SECRET**: reads `configuration["SETUP_SECRET"]` env var; auto-generates if missing. Bootstrap-only, stays outside `SecretBinding`.
- **STORAGE_S3_***: reads discrete keys via `IHierarchicalSettingsResolver` falling back to `IConfiguration["S3Settings:*"]`. Scoped, 5-min cache. Null → S3 features disable cleanly.
- **KEYCLOAK_***: reads Keycloak OIDC settings from configuration. Startup-only today; runtime updates must re-register schemes.
- **POSTGRESQL**: single URL string today. Must be refactored to discrete fields composed via `NpgsqlConnectionStringBuilder`.
- **SMTP_***: reads governance keys via `IHierarchicalSettingsResolver`; secret keys (`smtp.username`/`smtp.password`) through same resolver reading `SystemSetting.Value` JSON.
- **ANALYTICS_POSTHOG_***: reads `analytics.*` keys. Per-tenant, scoped. Fire-and-forget false if unavailable.
- **AI_OPENAI_API_KEY / AI_ANTHROPIC_API_KEY**: no consumer exists yet. Infisical folder layout prepared for future work.

### Verified Onboarding Flow

- `Explore.Blazor.Client/Pages/Setup.razor` validates setup token, persists via BFF JS interop.
- `Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor` routes based on completion state.
- `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor` enables/configures Keycloak, ATProto, Google SSO. Extends `KeycloakDetectedFromEnvironment` pattern to every secret-backed provider.
- `Explore.Application/Services/AuthProviderConfigurationService.cs` currently writes secrets into `SystemSetting.Value` as plain JSON (anti-pattern).

### Verified Tests

- `Explore.Secrets.UnitTests/` covers current provider implementations. Many will be deleted in Phase 6.
- `Event.Application.UnitTests/Infrastructure/` covers `SetupSecretProvider`, `SmtpConfigResolver`, `S3ConfigResolver` — need rewrites for the new resolver.
- `Event.API.IntegrationTests/` covers `SetupSecretFlowTests` and `InstanceOnboardingControllerTests`.

### Confirmed Gaps

- No `SecretBinding` entity, table, or repository exists (implemented in Phase 1, committed).
- No `SecretDefinitionRegistry` exists (implemented in Phase 1, committed).
- No `ISecretResolver` dispatch contract (Phase 3, written but uncommitted).
- No persistent audit trail (ADR-003).
- No versioned rotation support (ADR-004).
- No resilience policies on external calls (ADR-006).
- No distributed cache (ADR-005).
- No structured validation categories (ADR-007).
- No per-source health granularity.
- No tenant isolation query filter (ADR-008).
- No TTL/lease metadata (ADR-009).
- No file-based source (ADR-010, deferred to Phase 5).
- No discrete Postgres bootstrap path (implemented in Phase 2, committed).
- No Data Protection-based inline encryption in the resolver (Phase 3, written but uncommitted).

---

## Proposed Future State

### 1. `SecretDefinitionRegistry` (Committed — Phase 1)

A code-defined registry where every secret-backed setting key declares:
- `SettingKey` — canonical key (e.g. `smtp.password`, `storage.s3.secret_access_key`)
- `AllowedScopes` — `{Instance}` or `{Instance, Tenant}`
- `AllowedSourceTypes` — subset of `{Infisical, InlineEncrypted, EnvironmentVariable}` (bootstrap secrets ban `InlineEncrypted`)
- `IsBootstrap` — true for Postgres-connection + setup secret
- `InfisicalDefaults` — `{ Folder, SecretName }` for the clean folder layout
- `EnvironmentVariableDefault` — canonical env var name
- `ValidationKind` — drives the `POST /validate` contract

**Enterprise extension (Phase 3)**: Add `RequiresLease` flag (for future Vault dynamic secrets), `RotationPolicy` enum (`Manual`, `AutomaticOnExpiry`, `BlueGreen`), and `DriftValidation` flag (whether to check this binding on startup).

### 2. `SecretBinding` Entity (Committed foundation — Phase 1, extended in Phase 3)

Committed columns (Phase 1):
- `Id` (Guid v7), `SettingKey`, `Scope`, `ScopeId`, `SourceType`
- `InfisicalEnvironment`, `InfisicalPath`, `InfisicalKey`
- `EnvironmentVariableName`
- `InlineCiphertext`, `InlineCiphertextVersion`
- `IsLocked`, `LastValidationResult`, `LastValidationMessage`, `LastValidatedAt`, `LastValidatedBy`
- `IAuditable` fields

**Enterprise extensions (Phase 3 migration)**:
- `Version` (int, default 1) — monotonically increasing per binding key+scope
- `Status` (`SecretBindingStatus` enum: `Active = 0`, `Pending = 1`, `Previous = 2`) — only one `Active` binding per (SettingKey, Scope, ScopeId)
- `TtlExpiresAt` (DateTime?) — set for dynamic/leased secrets; null for static
- `LastRotatedAt` (DateTime?) — set on source-switch and version promotion
- `LastValidationCategory` (`SecretValidationCategory` enum: `SourceReachable`, `SourceUnreachable`, `CredentialValid`, `CredentialInvalid`, `BindingMisconfigured`, `InternalError`, `TtlExpired`)

Updated CHECK constraints:
- Original: exactly one metadata group per `SourceType`
- New: `Status = 'Active'` must be unique per `(SettingKey, Scope, ScopeId)` (via the existing filtered unique indexes)
- New: `Version > 0`
- New: for `SourceType = InlineEncrypted`, `TtlExpiresAt` must be null (inline secrets don't expire)

Updated filtered unique indexes:
- `UNIQUE (SettingKey) WHERE Scope = 'Instance' AND Status = 'Active' AND IsDeleted = false` (was: just `IsDeleted = false`)
- `UNIQUE (SettingKey, ScopeId) WHERE Scope = 'Tenant' AND Status = 'Active' AND IsDeleted = false`

**Note on `IsDeleted`**: `SecretBinding` is `IAuditableEntity` (NOT `ISoftDeletable`). The previous plan's reference to `IsDeleted` in filtered indexes was incorrect — it applies to other entities that ARE soft-deletable. For `SecretBinding`, the filtered unique indexes simply filter on `Status = 'Active'`.

### 3. `SecretBindingAuditEntry` (New — Phase 3)

Immutable audit entity in `Explore.Domain/Secrets/SecretBindingAuditEntry.cs`:

- `Id` (Guid v7)
- `BindingId` (Guid) — FK to `SecretBinding`
- `SettingKey` (string, max 256) — denormalized for queryability
- `Scope` (`SecretScope` enum)
- `ScopeId` (Guid?)
- `Action` (`SecretBindingAuditAction` enum: `Created`, `Updated`, `Deleted`, `Validated`, `SourceSwitched`, `VersionPromoted`, `Rotated`, `CacheInvalidated`)
- `SourceType` (`SecretSourceType` enum) — source at time of action
- `Version` (int?) — binding version at time of action
- `PreviousSourceType` (`SecretSourceType?`) — for source-switch actions
- `ValidationResult` (`SecretValidationResult?`) — for validated actions
- `ValidationCategory` (`SecretValidationCategory?`) — for validated actions
- `DiagnosticMessage` (string?, max 1024) — internal-only, NOT exposed via API
- `PerformedBy` (Guid?) — user ID from auth context
- `PerformedAt` (DateTimeOffset) — defaults to `DateTimeOffset.UtcNow`
- `IpAddress` (string?, max 45) — for API-initiated actions

This table is **append-only** — no updates, no deletes (except by DBA for GDPR/compliance retention). EF configuration sets `HasNoKey()` or uses a PK with no update/delete convention.

### 4. `ISecretResolver` (Phase 3 — written but uncommitted, now enhanced)

```csharp
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

Enhancements over original:
- `ResolveRequiredAsync` — throws `SecretNotConfiguredException` instead of returning null (for non-optional secrets that MUST resolve)
- `ValidateAsync` — returns structured `SecretValidationDetail` (not just bool)
- Cache uses `HybridCache` (not `IMemoryCache`) keyed on `(settingKey, scope, scopeId)` with 5-minute L1 TTL and tag-based invalidation
- Resolver only considers `Status = Active` bindings; Pending/Previous are invisible to consumers
- All external source calls wrapped in Polly resilience policies

### 5. Resilience Pipeline (Phase 3 — new)

`Explore.Secrets/Resilience/SecretResiliencePipeline.cs`:

- **Retry policy**: 3 retries, exponential backoff (500ms, 1s, 2s) on `HttpRequestException`, `TimeoutException`, `InfisicalApiException` (custom). Jitter via `RetryHelper`.
- **Circuit breaker**: 5 consecutive failures → open for 30 seconds. Half-open allows one probe. Success resets.
- **Timeout policy**: 10s for Infisical, 5s for env-var/inline (defensive, should be near-instant).
- **Bulkhead**: max 20 concurrent Infisical calls (prevents thread pool starvation under load).
- Policies are per-source-type, not global. `EnvironmentSecretSource` and `InlineSecretSource` get timeout-only (no retry needed for local operations).

Configured via `SecretResilienceOptions` bound from `SecretProvider:Resilience` configuration section. All resilience events emit to `SecretResolverMetrics`.

### 6. Caching Strategy (Phase 3 — changed from IMemoryCache to HybridCache)

- **L1**: In-process `MemoryCache` (default HybridCache behavior, 5-minute TTL)
- **L2**: Configured via `AddHybridCache()` (already in codebase). Production uses Redis; development uses in-process only.
- **Cache key format**: `secret:{settingKey}:{scope}:{scopeId:N}` or `secret:{settingKey}:Instance:-`
- **Tag-based invalidation**: Each cache entry tagged with `secret-binding:{settingKey}:{scope}:{scopeId}`. `InvalidateAsync` removes by tag (removes all versions for that binding).
- **Version-aware resolve**: Resolver appends `binding.Version` to cache key component. When a binding's version increments (rotation), the old cache entry naturally expires; the new version gets a new cache key.
- **No-fallback guarantee**: A binding pointing to Infisical that returns null results in a null cached entry. The resolver NEVER tries another source.

### 7. Per-Source Health Granularity (Phase 3 — enhanced)

`SecretResolverHealthCheck` returns per-source status:

```csharp
Dictionary<string, HealthStatus> SourceStatuses { get; }
// e.g. { "EnvironmentVariable": Healthy, "Infisical": Degraded, "InlineEncrypted": Healthy }
```

Overall health: `Healthy` if all sources healthy, `Degraded` if any degraded and none unhealthy, `Unhealthy` if any unhealthy.

Additional degraded conditions:
- A binding with `TtlExpiresAt < DateTime.UtcNow` → source marked `\Degraded`
- A binding with `LastValidationResult = Failure` for more than 1 hour → source marked `Degraded`

### 8. Tenant Isolation (Phase 4 — new)

EF Core global query filter on `SecretBinding`:

```csharp
.HasQueryFilter("TenantSecretIsolation", e =>
    e.Scope == SecretScope.Instance ||
    e.ScopeId == _currentTenantId);
```

Admin query handlers explicitly use `.IgnoreQueryFilters()` when listing all bindings across tenants (requires `[Authorize]` + Cerbos `secret_binding:manage_instance`).

### 9. Infisical Layout (unchanged from Phase 1)

Per user spec — every `SecretDefinitionRegistry` entry maps to its `(Folder, SecretName)`.

### 10. Bootstrap Secret Loader (Committed — Phase 2)

`Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` is the only path Postgres bootstrap takes. Discrete fields, composed via `NpgsqlConnectionStringBuilder`. **Stays outside `SecretBinding`** — bootstrap secrets unlock the DB containing the bindings.

### 11. Inline Encryption via `IDataProtectionProvider` (unchanged)

- Persist keys via `PersistKeysToDbContext<ExploreDbContext>` (committed in Phase 1).
- Purpose string hierarchy: `("Event.Secrets", "Binding", "v1")` plus scope chain.
- `InlineCiphertextVersion` column captures purpose version for future rotation.
- **Disaster recovery note**: DP keys in the same DB = protection against app-layer disclosure, not full DB compromise. Backups must include both ciphertext and keys.

### 12. Blue/Green Rotation Workflow (Phase 5 — new)

```
1. Admin creates a new Pending binding version (SourceType + metadata = new credential)
2. Admin validates the Pending version → sets LastValidationResult/Category
3. Admin promotes: atomically swaps Pending→Active and Active→Previous
4. Cache invalidation event fires → all consumers get new credential
5. After grace period (configurable, default 1 hour), admin deletes Previous
```

The `SecretBinding.Version` + `Status` columns enable this workflow. The resolve path only ever reads `Status = Active` bindings, so Pending credentials are invisible until promotion.

### 13. Lease/TTL Metadata (Phase 3 — new column)

`SecretBinding.TtlExpiresAt` (DateTime?) — set when the binding points to a dynamic/leased secret (Vault dynamic credentials, Infisical rotating secrets). When `TtlExpiresAt < DateTime.UtcNow`:
- Health check marks the source as `Degraded`
- `DescribeAsync` returns `IsExpired: true` in the descriptor
- UI shows "Expired" badge with timestamp

This does NOT auto-rotate — it surfaces expiration for manual intervention. Automated rotation is post-1.0.

### 14. Audit Trail Persistence (Phase 3 — new)

Every write operation on `SecretBinding` persists a `SecretBindingAuditEntry` row via the same MediatR notification handler that does cache invalidation. The audit handler runs synchronously before the command response returns.

Read operations are 1% sampled (configurable via `SecretResolverOptions.AuditSampleRate`, default 0.01) via `AuditingSecretResolverDecorator`. The sample writes to structured logs, NOT to the audit table (to avoid read-path write amplification).

### 15. UI Contract (unchanged)

Admin UI lists every secret from `SecretDefinitionRegistry`. Each card shows state, source, metadata, validation result/category, timestamps, version, and TTL expiry. **Never renders secret values.**

---

## Implementation Phases

### Phase 1 — Foundations (Committed `38ce8098`)

**Goal**: introduce `SecretDefinitionRegistry`, `SecretBinding` entity, schema, repository, and Data Protection plumbing.

**Status**: ✅ COMPLETE. No further changes needed.

### Phase 2 — Bootstrap Split (Committed `fc0b2b5a`)

**Goal**: introduce `BootstrapSecretLoader` for discrete Postgres secrets + setup secret. Remove legacy URL connection string path.

**Status**: ✅ COMPLETE. No further changes needed.

### Phase 3 — Resolver + Admin API + Enterprise (PR 3)

**Goal**: `ISecretResolver` implementation with resilience, `HybridCache`, per-source health, structured validation, persistent audit trail, versioned rotation schema, tenant isolation query filter, and admin CQRS/API.

**Dependencies**: PR 1.

**New over original plan**: ADR-003 (audit trail), ADR-004 (versioned rotation), ADR-005 (HybridCache), ADR-006 (Polly resilience), ADR-007 (structured validation), ADR-008 (tenant query filter), ADR-009 (TTL metadata).

**Tasks**:

1. **3.1** EF migration: add `Version`, `Status`, `TtlExpiresAt`, `LastRotatedAt`, `LastValidationCategory` columns to `SecretBindings`; add `SecretBindingAuditEntries` table; update filtered unique indexes to include `Status = Active`.
2. **3.2** Domain: `SecretBindingAuditEntry` entity + `SecretBindingAuditAction` enum + `SecretValidationCategory` enum + `SecretBindingStatus` enum. Update `SecretBinding` with new columns + factory methods for version promotion.
3. **3.3** Domain: `SecretBindingUpdatedEvent` (already written, update to include Version and Status).
4. **3.4** Application contracts: `ResolvedSecret`, `ISecretResolver` (enhanced with `ResolveRequiredAsync` + `ValidateAsync`), `ISecretSource` (enhanced with `ValidateAsync` returning `SecretValidationDetail`), `IInfisicalClientFactory`.
5. **3.5** Resilience pipeline: `SecretResiliencePipeline` + `SecretResilienceOptions` + Polly integration.
6. **3.6** Per-source implementations: `EnvironmentSecretSource` (timeout-only), `InlineSecretSource` (timeout-only), `InfisicalSecretSource` (full resilience pipeline) — all wrapped in Polly policies.
7. **3.7** Core resolver: `SecretResolver` using `HybridCache`, `Status=Active` filter, and version-aware cache keys. `SecretResolverMetrics` with per-source counters.
8. **3.8** Auditing decorator: `AuditingSecretResolverDecorator` (1% read sampling to logs, all writes/deletes/validations to `SecretBindingAuditEntry` via `IAuditWriter`).
9. **3.9** Health check: `SecretResolverHealthCheck` with per-source status + TTL-expiry degradation.
10. **3.10** Tenant isolation: EF query filter on `SecretBinding` + `ITenantContext` injection.
11. **3.11** Per-source Polly policies configuration + DI registration (`SecretResolutionServiceCollectionExtensions.AddSecretResolution()`).
12. **3.12** Admin CQRS commands: Create/Update/Delete/Validate (with audit trail writes, version handling on update, source-switch detection).
13. **3.13** Admin CQRS queries: List/Details/AvailableForOnboarding (with tenant filter bypass for instance admins).
14. **3.14** Notification handlers: Cache invalidation + audit persistence + Keycloak scheme refresh (stub).
15. **3.15** Controller: `SecretBindingsController` with `[Authorize]`, Cerbos, rate limiting, HAL links.
16. **3.16** HATEOAS policy + assembler.
17. **3.17** Cerbos policy.
18. **3.18** DI wiring in API + Blazor `Program.cs`.
19. **3.19** Tests: no-fallback, no-leak, resilience (circuit breaker states), audit trail, version rotation lifecycle, tenant isolation enforcement, per-source health check.
20. **3.20** Verification: build + all test projects.
21. **3.21** Commit: single Phase 3 commit.

### Phase 4 — Onboarding + Auth + Tenant Isolation (PR 4)

**Goal**: move Keycloak/Google/ATProto secrets from `SystemSetting` JSON onto `SecretBinding`; explicit Keycloak scheme refresh on binding update; tenant isolation enforcement; remove `/auth-provider-configuration/internal` endpoint; batch resolve endpoint for onboarding.

**Dependencies**: PR 3.

**New over original plan**: Batch resolve API for onboarding (check multiple secrets in one call).

### Phase 5 — Consumer Migration + File Source + Drift Detection (PR 5)

**Goal**: refactor all consumers to `ISecretResolver`; add `FileSecretSource` for Docker/K8s; add configuration drift detection at startup.

**Dependencies**: PR 3.

**New over original plan**: ADR-010 (File-Based Secret Source), startup drift detection (`SecretDefinitionRegistry` vs active bindings reconciliation).

### Phase 6 — Deletion + Docs + Key Rotation Procedure (PR 6)

**Goal**: delete legacy configuration providers, refresh/rotation services, AES/key-rotation code, compatibility mappings, obsolete tests. Write Data Protection key rotation procedure. Rewrite docs.

**Dependencies**: PRs 1–5.

**New over original plan**: Documented key rotation procedure for `IDataProtectionProvider` (purpose-string version bump workflow + test). Load/performance test scaffolding. Security test scaffolding.

---

## Risk Assessment And Mitigation Strategies

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| 1 | Postgres NULL unique-index semantics | High | Two partial indexes for Instance and Tenant scopes, filtered on `Status = Active`. Verified with integration tests. |
| 2 | DP key ring disaster recovery | High | `docs/SECRETS.md` documents that `DataProtectionKeys` must be in every backup; integration test round-trips ciphertext across simulated DB recreation. |
| 3 | Bootstrap / runtime boundary drift | High | `SecretDefinitionRegistry` enforces `AllowedSourceTypes` at binding write time; architecture test asserts no bootstrap-flagged key allows `InlineEncrypted`. |
| 4 | Stale cache after source switch or version promotion | High | `SecretBindingUpdatedEvent` → `InvalidateAsync` synchronously in handler before command response. HybridCache tag-based invalidation handles multi-instance propagation. |
| 5 | Validation endpoint information leakage | Medium | Generic validation messages for API consumers; detailed diagnostics only in audit trail and server logs. `POST /validate` rate-limited. |
| 6 | Source-type switching UX confusion | Medium | UI explicit confirmation: "Switching replaces the current binding. Inline-encrypted values are write-only and cannot be recovered." |
| 7 | Keycloak scheme refresh timing | High | `SecretBindingUpdatedEvent` → `KeycloakSchemeRefreshHandler` with tests covering mid-flight OIDC exchange. |
| 8 | PostHog analytics silent-fail masking outage | Low | Validation state surfaced on admin card + OpenTelemetry metric; health check degrades if `Failure` > 1 hour. |
| 9 | Setup-secret edge case | Low | `ISetupSecretProvider` stays outside `SecretBinding`; dedicated admin component reads `IsAutoGenerated`/`IsTimedOut`/`GetExpiration`. |
| 10 | Per-secret cache TTL drift with Infisical rotation | Medium | Document 5-min cache TTL in `docs/SECRETS.md`. Expose `POST /api/SecretBindings/{key}/refresh-cache` admin endpoint for forced eviction. Infisical webhook support deferred. |
| 11 | **Circuit breaker blocks all Infisical secrets when one path fails (NEW)** | Medium | Circuit breaker is per-source, not per-binding. If the Infisical service is down, ALL Infisical bindings return null. Mitigation: env-var/inline fallback is explicit (operator switches SourceType), not automatic. Health check surfaces circuit state. |
| 12 | **HybridCache L2 requires Redis in production (NEW)** | Low | Development mode falls back to L1-only gracefully. Production deployment docs must include Redis configuration. |
| 13 | **Version rotation atomicity — concurrent promotions (NEW)** | Medium | `Status = Active` partial unique index prevents two Active versions for the same (SettingKey, Scope, ScopeId). Promotion handler uses `UPDATE ... SET Status = 'Active' WHERE ...` in a single transaction with `SET Status = 'Previous' WHERE Status = 'Active'` as the first statement. |
| 14 | **Audit table growth (NEW)** | Low | Secret bindings number in the hundreds; mutations are operator-driven (not per-request). Estimated <1,000 rows/day even in active rotation scenarios. Add index on `(SettingKey, PerformedAt)` for queryability. |
| 15 | **Tenant isolation filter bypass in admin endpoints (NEW)** | High | Architecture test asserts every `SecretBindings` query handler that uses `IgnoreQueryFilters()` is decorated with `[Authorize]` + Cerbos `secret_binding:manage_instance`. |

---

## Success Metrics

- **Zero fallback paths** — automated test asserts that a binding with `SourceType=EnvironmentVariable` never triggers an Infisical call and vice versa.
- **Zero secret-value leaks in UI/logs** — API-contract test asserts no response from `/api/SecretBindings` or `/validate` contains ciphertext or plaintext.
- **Minimal deployment works** — integration test: API + Blazor + Postgres with no Infisical, no S3, no SMTP, no PostHog; every page loads; email/S3/analytics report "Not configured".
- **Onboarding auto-detection** — integration test: Keycloak secrets in env vars → onboarding page shows "Auto-detected" chip.
- **Zero `InfrastructureSecretSettingKeys` references** after Phase 5 — grep-based architecture test.
- **Zero legacy secret code** after Phase 6 — architecture test asserts no references to `AppSetting`, `DbConfigurationProvider`, `AesEncryptionService`, `KeyRotationService`, `SecretRefreshService`.
- **Audit trail completeness (NEW)** — integration test: every create/update/delete/validate/write operation on SecretBindings produces a corresponding `SecretBindingAuditEntry` row with correct action, user, and timestamp.
- **Resilience verification (NEW)** — unit test: Infisical source returns null after 3 retries + circuit breaker opens after 5 consecutive failures. Unit test: env-var source resolves in <1ms (no resilience overhead).
- **Tenant isolation (NEW)** — integration test: tenant A cannot see tenant B's secrets via the resolver; instance admin CAN see all secrets via admin endpoint with `IgnoreQueryFilters`.
- **Version rotation lifecycle (NEW)** — integration test: create Pending → validate → promote → verify Active version changed → cache invalidated → Previous version still accessible for 1-hour grace period.
- **Lighthouse + bUnit accessibility scores** unchanged on the new admin Secrets page.

---

## Required Resources And Dependencies

- NuGet: `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` (committed Phase 1).
- NuGet: `Microsoft.Extensions.Caching.Hybrid` (already in codebase).
- NuGet: `Infisical.Sdk` v3.0.4 (stays).
- NuGet: `Polly` + `Polly.Extensions.Http` (new — resilience pipeline).
- Cerbos policy updates: `secret_binding.yaml` resource.
- EF Core migration tooling.
- Redis (for HybridCache L2 in production; optional for development).

---

## Effort Estimates

| Phase | Effort | Notes |
|-------|--------|-------|
| Phase 1 — Foundations | ✅ COMPLETE | Committed `38ce8098`. |
| Phase 2 — Bootstrap split | ✅ COMPLETE | Committed `fc0b2b5a`. |
| Phase 3 — Resolver + Admin API + Enterprise | **XL** | Resilience, HybridCache, audit trail, versioned rotation schema, tenant isolation, structured validation, per-source health, all CQRS + controller + tests. Highest-risk PR. |
| Phase 4 — Onboarding + Auth + Tenant Isolation | **L** | Keycloak scheme-refresh, onboarding UI, batch resolve, tenant filter bypass. |
| Phase 5 — Consumer Migration + File Source + Drift | **L** | Consumer cutover, FileSecretSource, startup drift detection. |
| Phase 6 — Deletion + Docs + Key Rotation | **M** | Destructive migration, doc rewrite, key rotation procedure. |

---

## Post-1.0 Backlog (Explicitly Deferred)

- Infisical webhook integration (cache has `InvalidateAsync` hook; webhook endpoint to be added).
- `Module`-scoped bindings.
- `Inherited` as a persisted source type (computed by resolver today).
- Additional providers (Vault, Azure Key Vault, AWS Secrets Manager).
- RLS for `SecretBindings` (row-level security in Postgres for defense-in-depth).
- Automated rotation workflows (manual via UI supported; automated rotation post-1.0).
- Import/Export API for bulk secret binding management.
- Dynamic module registration (runtime loading of `SecretDefinition` entries).
- Load/performance test suite for resolve path (>10K ops/sec target).
- Security penetration testing of admin API and resolve path.
- Vault dynamic secrets integration (TTL/lease support is schema-ready but provider not implemented).
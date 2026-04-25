ABOUTME: Detailed task checklist for the Secrets Refactor (Control Plane / Data Plane) with enterprise improvements.
ABOUTME: Six-phase, six-PR plan. Each phase is independently reviewable. Tasks within a phase ordered by dependency.

# Secrets Refactor — Task Checklist

Last Updated: 2026-04-24 (Enterprise Revision v2.0)

Legend: `S` = <2h, `M` = 2-6h, `L` = 6-12h, `XL` = 12h+. Check tasks off as they merge.

---

## Phase 1 — Foundations (PR 1) ✅ COMMITTED `38ce8098`

**PR title**: `refactor(secrets): introduce SecretBinding + SecretDefinitionRegistry foundations`

### 1.1 Create `SecretDefinitionRegistry` ✅
- **File**: `Explore.Domain/Secrets/SecretDefinitionRegistry.cs` + `SecretDefinition.cs`.
- **Acceptance Criteria**:
  - [x] Registry exposes `IReadOnlyList<SecretDefinition> All` and `SecretDefinition Get(string settingKey)`.
  - [x] Each definition has `SettingKey`, `AllowedScopes`, `AllowedSourceTypes`, `IsBootstrap`, `InfisicalDefaults`, `EnvironmentVariableDefault`, `ValidationKind`.
  - [x] Seeded with all required keys.
  - [x] Bootstrap keys never allow `InlineEncrypted`.
  - [x] File-scoped namespace + `ABOUTME:` header.
- **Effort**: M
- **Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

### 1.2 Create `SecretBinding` entity + enums ✅
- **Files**: `Explore.Domain/Secrets/{SecretBinding, SecretScope, SecretSourceType, SecretValidationResult}.cs`.
- **Acceptance Criteria**:
  - [x] `SecretBinding` implements `IAuditableEntity` + `IRowVersionable`.
  - [x] `SecretBinding.Id` is UUIDv7.
  - [x] Navigation properties readonly; writes via repository.
  - [x] Domain event raised on mutation.
  - [x] No default values in entity body.
  - [x] File-scoped namespaces + `ABOUTME:` headers.
- **Effort**: M

### 1.3 EF configuration + CHECK + filtered unique indexes ✅
- **File**: `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs`.
- **Acceptance Criteria**:
  - [x] Columns with explicit max-length for strings.
  - [x] CHECK constraint for source-type-exclusive metadata.
  - [x] Two partial unique indexes for Instance and Tenant scopes.
  - [x] Auditable columns conventionally configured.
- **Effort**: M

### 1.4 Create `ISecretBindingRepository` + implementation ✅
- **Files**: `Explore.Application/Contracts/Persistence/ISecretBindingRepository.cs`, `Explore.Persistence/Repositories/SecretBindingRepository.cs`.
- **Acceptance Criteria**:
  - [x] Methods: `GetAsync`, `ListAsync`, `ListInstanceAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.
  - [x] Returns entities, not DTOs.
- **Effort**: S

### 1.5 Register Data Protection with EF keyring ✅
- **Files**: `Explore.Persistence/Extensions/DataProtectionServiceCollectionExtensions.cs`, `Explore.Persistence/PersistenceServicesRegistration.cs`.
- **Acceptance Criteria**:
  - [x] `PersistKeysToDbContext<ExploreDbContext>()` with `SetApplicationName("islamu-event")`.
  - [x] `DataProtectionKeys` table in EF migration.
  - [x] Round-trip verified.
- **Effort**: S

### 1.6 Registry-enforced domain invariants ✅
- **File**: `Explore.Domain/Secrets/SecretBinding.cs` factory methods + `Event.Domain.UnitTests/Entities/SecretBindingTests.cs`.
- **Acceptance Criteria**:
  - [x] 17 unit tests covering each failure mode.
- **Effort**: M

### 1.7 EF migration for SecretBindings + DataProtectionKeys ✅
- **Acceptance Criteria**:
  - [x] Both tables + filtered unique indexes created.
  - [x] Tests pass.
- **Effort**: S

### 1.8 Architecture test ✅
- **Acceptance Criteria**:
  - [x] Architecture tests pass (74 green).
- **Effort**: S

### 1.9 PR 1 verification ✅
- **Acceptance Criteria**:
  - [x] Domain unit tests pass (207).
  - [x] Architecture tests pass (74).
  - [x] Application unit tests pass (823).
  - [x] Secrets unit tests pass (201).

---

## Phase 2 — Bootstrap Split (PR 2) ✅ COMMITTED `fc0b2b5a`

**PR title**: `refactor(secrets): discrete Postgres bootstrap via NpgsqlConnectionStringBuilder`

### 2.1 `BootstrapSecretLoader` ✅
- **Acceptance Criteria**:
  - [x] Given a registry key marked `IsBootstrap=true`, fetches from Infisical (if bootstrap config present) else environment variable else `IConfiguration` section.
  - [x] Never touches `ISecretResolver` or `SecretBinding`.
  - [x] Exposes `LoadPostgresConnectionString()` returning `BootstrapPostgresCredentials` with composed `NpgsqlConnectionStringBuilder`.
  - [x] Fails with a structured log message listing each missing discrete field and the source attempted.
- **Effort**: M

### 2.2 Refactor `PersistenceServicesRegistration` ✅
- **Acceptance Criteria**:
  - [x] No longer reads `ConnectionStrings:DefaultConnection`.
  - [x] Calls `BootstrapSecretLoader.LoadPostgresConnectionString()` synchronously.
  - [x] Preserves `AddPooledDbContextFactory<ExploreDbContext>` behavior.
- **Effort**: S

### 2.3 Remove `POSTGRESQL_PUBLIC_URL` from config mapping ✅
- **Acceptance Criteria**:
  - [x] Deleted `POSTGRESQL_PUBLIC_URL` mappings from API, Blazor, and MigrationService config extensions.
  - [x] S3/Keycloak mappings kept with architectural-invariant comments.
- **Effort**: S

### 2.4 Update infra files ✅
- **Acceptance Criteria**:
  - [x] `docker-compose.yml` has `x-postgres-bootstrap-env` anchor.
  - [x] Aspire `AppHost.cs` passes discrete Postgres env vars.
- **Effort**: S

### 2.5 `BootstrapSecretLoaderTests` ✅
- **Acceptance Criteria**:
  - [x] Covers all three source fallbacks, missing-field failures, SslMode/TrustServerCertificate/DefaultPort.
  - [x] 11 tests.
- **Effort**: M

### 2.6 PR 2 verification ✅
- **Acceptance Criteria**:
  - [x] Build clean, all 1,305 tests green, 0 regressions.
- **Effort**: S

---

## Phase 3 — Resolver + Admin API + Enterprise (PR 3) 🟡 IN PROGRESS

**PR title**: `refactor(secrets): phase 3 introduce ISecretResolver + admin bindings API + enterprise patterns`

**⚠️ SESSION HANDOFF STATE:**
- Runtime pipeline (3.1–3.6 equivalent) is **WRITTEN TO DISK but NOT COMMITTED**. 14 new files + 1 modified csproj.
- Build verified clean on `Explore.Secrets` project.
- Solution-wide build + tests NOT yet re-run.
- Enterprise additions (audit trail, versioned rotation, resilience, HybridCache, structured validation, per-source health, tenant isolation) are **NOT YET STARTED**.
- See `phase-3-implementation-plan.md` for the full file-by-file execution blueprint.
- Single Phase 3 commit at end (no splitting), per user directive.

### 3.1 EF migration — enterprise schema extensions 🆕
- **Files**: `Explore.Persistence/Migrations/{timestamp}_AddSecretBindingEnterpriseColumns.cs`.
- **Acceptance Criteria**:
  - [ ] `SecretBindings` table: add `Version` (int, default 1), `Status` (int enum: Active=0, Pending=1, Previous=2), `TtlExpiresAt` (DateTime?), `LastRotatedAt` (DateTime?), `LastValidationCategory` (int enum).
  - [ ] `SecretBindingAuditEntries` table: all columns per ADR-003.
  - [ ] Update filtered unique indexes to include `Status = Active` condition.
  - [ ] CHECK constraint: `SourceType = InlineEncrypted → TtlExpiresAt IS NULL`.
  - [ ] CHECK constraint: `Version > 0`.
  - [ ] Index on `SecretBindingAuditEntries(SettingKey, PerformedAt)`.
- **Effort**: L
- **Skills**: `dotnet-efcore-guidelines`

### 3.2 Domain — audit + versioning + validation enums 🆕
- **Files**: `Explore.Domain/Secrets/SecretBindingAuditEntry.cs`, `Explore.Domain/Secrets/SecretBindingAuditAction.cs`, `Explore.Domain/Secrets/SecretBindingStatus.cs`, `Explore.Domain/Secrets/SecretValidationCategory.cs`.
- **Acceptance Criteria**:
  - [ ] `SecretBindingAuditEntry` is append-only entity (no update/delete convention in EF).
  - [ ] `SecretBindingAuditAction` enum: `Created`, `Updated`, `Deleted`, `Validated`, `SourceSwitched`, `VersionPromoted`, `Rotated`, `CacheInvalidated`.
  - [ ] `SecretBindingStatus` enum: `Active = 0`, `Pending = 1`, `Previous = 2`.
  - [ ] `SecretValidationCategory` enum: `SourceReachable`, `SourceUnreachable`, `CredentialValid`, `CredentialInvalid`, `BindingMisconfigured`, `InternalError`, `TtlExpired`.
  - [ ] `SecretBinding` factory methods updated: `CreateWithPendingVersion`, `PromoteToActive`, `DemoteToPrevious`, `RecordValidation` updated to accept `SecretValidationCategory`.
- **Effort**: L
- **Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

### 3.3 Domain event — update for versioning 🆕
- **File**: `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs` (already on disk, needs update).
- **Acceptance Criteria**:
  - [ ] Add `int Version` and `SecretBindingStatus Status` to event record.
  - [ ] Add `SecretBindingAuditAction ChangeAction` property (audit-specific, not just `ChangeKind`).
- **Effort**: S

### 3.4 Application contracts — enhanced resolver interface 🆕
- **Files**: Update `Explore.Application/Contracts/Secrets/ISecretResolver.cs`, `ISecretSource.cs`, `ResolvedSecret.cs`; add `SecretValidationDetail.cs`, `SecretBindingDescriptor.cs` (already partially on disk).
- **Acceptance Criteria**:
  - [ ] `ISecretResolver` has `ResolveRequiredAsync` (throws `SecretNotConfiguredException` on null).
  - [ ] `ISecretResolver` has `ValidateAsync` returning `SecretValidationDetail`.
  - [ ] `ISecretSource.ValidateAsync` returns `SecretValidationDetail` (not just bool).
  - [ ] `SecretValidationDetail` record includes `Result`, `Category`, `DiagnosticMessage`.
  - [ ] `ResolvedSecret` includes `Version` and `TtlExpiresAt` fields.
  - [ ] `SecretNotConfiguredException` custom exception class.
- **Effort**: M
- **Skills**: `clean-architecture-rules`

### 3.5 Resilience pipeline — Polly integration 🆕
- **Files**: `Explore.Secrets/Resilience/SecretResiliencePipeline.cs`, `Explore.Secrets/Resilience/SecretResilienceOptions.cs`.
- **Acceptance Criteria**:
  - [ ] Retry policy: 3 retries, exponential backoff (500ms, 1s, 2s) on `HttpRequestException`, `TimeoutException`, custom `InfisicalApiException`.
  - [ ] Circuit breaker: 5 consecutive failures → open for 30 seconds. Half-open allows one probe. Success resets.
  - [ ] Timeout: 10s for Infisical, 5s for env-var/inline (defensive).
  - [ ] Bulkhead: max 20 concurrent Infisical calls.
  - [ ] All resilience events emit to `SecretResolverMetrics`.
  - [ ] Options bindable from `SecretProvider:Resilience` config section.
  - [ ] `EnvironmentSecretSource` and `InlineSecretSource` get timeout-only policy (no retry/circuit-breaker needed for local operations).
- **Effort**: L
- **Skills**: `error-tracking`

### 3.6 Per-source implementations — with resilience 🆕
- **Files**: `Explore.Secrets/Sources/EnvironmentSecretSource.cs` (on disk, update), `InlineSecretSource.cs` (on disk, update), `InfisicalSecretSource.cs` (on disk, update).
- **Acceptance Criteria**:
  - [ ] `InfisicalSecretSource` calls are wrapped in `SecretResiliencePipeline.GetInfisicalPolicy()`.
  - [ ] `EnvironmentSecretSource` and `InlineSecretSource` are wrapped in timeout-only policy.
  - [ ] All sources implement `ValidateAsync` returning `SecretValidationDetail`.
  - [ ] `InfisicalSecretSource.GetSecretAsync` catches `InfisicalApiException` and returns `SecretValidationDetail(Category: SourceUnreachable, DiagnosticMessage: ...)`.
  - [ ] All source `GetSecretAsync` and `ValidateAsync` methods emit timing to `SecretResolverMetrics.resolve_duration_ms`.
- **Effort**: L
- **Skills**: `error-tracking`, `auth-patterns`

### 3.7 Core resolver with HybridCache + version-aware resolve 🆕
- **File**: `Explore.Secrets/Services/SecretResolver.cs` (on disk, major update needed).
- **Acceptance Criteria**:
  - [ ] Uses `HybridCache` instead of `IMemoryCache` for per-secret caching.
  - [ ] Cache key format: `secret:{settingKey}:{scope}:{scopeId:N}`.
  - [ ] Tags each cache entry with `secret-binding:{settingKey}:{scope}:{scopeId:N}` for tag-based invalidation.
  - [ ] Resolves only `Status = Active` bindings. Pending/Previous are invisible.
  - [ ] `InvalidateAsync` uses `HybridCache.RemoveByTagAsync()` for distributed cache invalidation.
  - [ ] `ResolveRequiredAsync` throws `SecretNotConfiguredException` when binding not found.
  - [ ] `ValidateAsync` returns `SecretValidationDetail` with categories.
  - [ ] Falls back gracefully when L2 (Redis) is unavailable (L1-only).
  - [ ] Per-source metrics emitted on every resolve.
- **Effort**: XL
- **Skills**: `clean-architecture-rules`, `error-tracking`

### 3.8 Auditing decorator — persistent audit trail 🆕
- **File**: `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs` (on disk, major update needed).
- **Acceptance Criteria**:
  - [ ] All write/delete/validate operations persist `SecretBindingAuditEntry` via `IAuditWriter`.
  - [ ] `IAuditWriter` interface injected — implementation writes to `ISecretBindingAuditRepository`.
  - [ ] Read operations sampled at `SecretResolverOptions.AuditSampleRate` (default 0.01) → structured logs only.
  - [ ] NEVER logs/plaintext/ciphertext in audit entries.
  - [ ] Audit entries include `IpAddress` from `IHttpContextAccessor` (when available).
- **Effort**: M
- **Skills**: `error-tracking`, `auth-patterns`

### 3.9 Health check — per-source granularity 🆕
- **File**: `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs` (on disk, major update needed).
- **Acceptance Criteria**:
  - [ ] Returns `Dictionary<string, HealthStatus>` per source type.
  - [ ] Overall: Healthy if all healthy, Degraded if any degraded and none unhealthy, Unhealthy if any unhealthy.
  - [ ] Degraded conditions: source unreachable, binding with `TtlExpiresAt < DateTime.UtcNow`, binding with `LastValidationResult = Failure` for >1 hour.
  - [ ] Circuit breaker state included in health data (per Infisical source).
- **Effort**: M
- **Skills**: `error-tracking`

### 3.10 Tenant isolation — EF query filter 🆕
- **Files**: `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs` (update), `Explore.Application/Services/ITenantContext.cs` (or use existing).
- **Acceptance Criteria**:
  - [ ] `.HasQueryFilter("TenantSecretIsolation", e => e.Scope == SecretScope.Instance || e.ScopeId == _currentTenantId)`.
  - [ ] Admin query handlers that need cross-tenant view use `.IgnoreQueryFilters()`.
  - [ ] Architecture test: every `IgnoreQueryFilters()` call on `SecretBinding` is in a method decorated with `[Authorize]` + Cerbos `secret_binding:manage_instance`.
- **Effort**: M
- **Skills**: `dotnet-efcore-guidelines`, `auth-patterns`

### 3.11 DI registration + resilience configuration 🆕
- **File**: `Explore.Secrets/Extensions/SecretResolutionServiceCollectionExtensions.cs` (on disk, major update needed).
- **Acceptance Criteria**:
  - [ ] Registers `HybridCache` (or uses existing `AddHybridCache()` from `Program.cs`).
  - [ ] Registers Polly resilience pipeline per source type.
  - [ ] Registers `SecretResilienceOptions` from config.
  - [ ] Registers `IAuditWriter` → `SecretBindingAuditWriter`.
  - [ ] Registers `ISecretBindingAuditRepository`.
  - [ ] Registers all source implementations (`EnvironmentSecretSource`, `InlineSecretSource`, `InfisicalSecretSource`) as `ISecretSource`.
  - [ ] Registers `SecretBindingStatus` enum converter.
  - [ ] Registers `SecretResolver` (concrete) + `AuditingSecretResolverDecorator` (public `ISecretResolver`).
  - [ ] Registers `SecretResolverMetrics` singleton.
  - [ ] Registers health check with per-source granularity.
- **Effort**: M

### 3.12 Admin CQRS — commands (with audit trail + version handling)
- **Files**: `Explore.Application/Features/SecretBindings/Commands/CreateSecretBindingCommand*.cs`, `UpdateSecretBindingCommand*.cs`, `DeleteSecretBindingCommand*.cs`, `ValidateSecretBindingCommand*.cs`.
- **Acceptance Criteria**:
  - [ ] Each command returns `BaseCommandResponse<Guid>` (create/update) or `BaseCommandResponse<bool>` (delete/validate).
  - [ ] Handlers validate against `SecretDefinitionRegistry` + `ISecretBindingRepository`.
  - [ ] Validators manually instantiated per project rule.
  - [ ] `CreateSecretBindingCommand`: `SourceType=InlineEncrypted` encrypts plaintext via `IDataProtector`, stores ciphertext, discards plaintext (no return of plaintext).
  - [ ] `UpdateSecretBindingCommand`: detects source-type switch, publishes `SourceSwitched` audit action, handles version increment.
  - [ ] `ValidateSecretBindingCommand`: returns `SecretValidationDetail` with categories, updates `LastValidationResult`, `LastValidationCategory`, `LastValidatedAt/By/Message`.
  - [ ] All mutations raise `SecretBindingChangedNotification` via `IMediator.Publish` AND write `SecretBindingAuditEntry` via `IAuditWriter`.
- **Effort**: XL
- **Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

### 3.13 Admin CQRS — queries
- **Files**: `Explore.Application/Features/SecretBindings/Queries/GetSecretBindingListRequestHandler.cs`, `GetSecretBindingDetailsRequestHandler.cs`, `GetAvailableSecretsForOnboardingRequestHandler.cs`.
- **Acceptance Criteria**:
  - [ ] DTOs never include `InlineCiphertext`, resolved plaintext, or env var value.
  - [ ] DTOs include `Version`, `Status`, `TtlExpiresAt`, `LastValidationCategory`.
  - [ ] `GetAvailableSecretsForOnboardingQuery` returns list with `IsBound` and `AutoDetected` flags.
  - [ ] Instance admin queries use `.IgnoreQueryFilters()` for cross-tenant visibility.
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`

### 3.14 Notification handlers + audit persistence
- **Files**: `Explore.Application/Notifications/Secrets/SecretBindingChangedNotification.cs`, `Explore.Application/Notifications/SecretBindingCacheInvalidationHandler.cs`, `Explore.Application/Notifications/SecretBindingAuditPersistenceHandler.cs`, `Explore.Application/Notifications/KeycloakSchemeRefreshHandler.cs`.
- **Acceptance Criteria**:
  - [ ] Cache-invalidation handler calls `ISecretResolver.InvalidateAsync` synchronously.
  - [ ] Audit-persistence handler writes `SecretBindingAuditEntry` via `IAuditWriter`.
  - [ ] Keycloak handler stubbed (logs warning, awaits Phase 4).
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`

### 3.15 `SecretBindingsController`
- **File**: `Explore.API/Controllers/SecretBindingsController.cs`.
- **Acceptance Criteria**:
  - [ ] `GET /api/SecretBindings` `[Authorize]` + Cerbos `secret_binding:read`.
  - [ ] `GET /api/SecretBindings/{id:guid}` same auth.
  - [ ] `POST /api/SecretBindings` create, `[Authorize]` + Cerbos `secret_binding:write`.
  - [ ] `PUT /api/SecretBindings/{id:guid}` update, same auth.
  - [ ] `DELETE /api/SecretBindings/{id:guid}` same auth.
  - [ ] `POST /api/SecretBindings/{id:guid}/validate` `[Authorize]` + `write` rate limit.
  - [ ] `POST /api/SecretBindings/{id:guid}/promote` promote Pending→Active (new enterprise endpoint).
  - [ ] Responses never contain plaintext/ciphertext.
  - [ ] HAL links via `SecretBindingLinkPolicy`.
- **Effort**: L
- **Skills**: `auth-patterns`, `cqrs-mediatr-guidelines`

### 3.16 HATEOAS policy + assembler
- **Files**: `Explore.API/Hateoas/Policies/SecretBindingLinkPolicy.cs`, `Explore.API/Hateoas/Assemblers/SecretBindingResourceAssembler.cs`.
- **Acceptance Criteria**:
  - [ ] Matches `yield return` pattern.
  - [ ] Named routes registered in `RouteNames`.
  - [ ] Promote link only shown for bindings with `Status = Pending`.
- **Effort**: S

### 3.17 Cerbos policy
- **File**: `cerbos/policies/secret_binding.yaml`.
- **Acceptance Criteria**:
  - [ ] Resource `secret_binding` with actions `view`, `create`, `update`, `delete`, `validate`, `promote`.
  - [ ] Instance admin: all actions on all scopes.
  - [ ] Tenant admin: all actions on tenant-scope only (`tenantId` matches).
  - [ ] Default deny.
- **Effort**: S

### 3.18 DTOs + Validators
- **Files**: `Explore.Application/DTOs/SecretBindings/` (SecretBindingDto, SecretBindingListDto, CreateSecretBindingDto, UpdateSecretBindingDto, PromoteSecretBindingDto, ValidateSecretBindingDto + validators).
- **Acceptance Criteria**:
  - [ ] SecretBindingDto includes `Version`, `Status`, `TtlExpiresAt`, `LastValidationCategory`.
  - [ ] CreateSecretBindingDto: `SourceType`, metadata fields, `InlineSecretValue?` (plaintext — handler protects + discards).
  - [ ] PromoteSecretBindingDto: binding ID only (promotion is explicit action).
  - [ ] Validators enforce `SecretDefinitionRegistry` constraints.
  - [ ] Validators reject `InlineEncrypted` for bootstrap keys.
  - [ ] Validators reject `TtlExpiresAt` for `InlineEncrypted` source type.
- **Effort**: L
- **Skills**: `cqrs-mediatr-guidelines`

### 3.19 Tests — enterprise patterns 🆕
- **Files**: `Explore.Secrets.UnitTests/` + `Event.Application.UnitTests/Features/SecretBindings/` + `Event.Architecture.Tests/`.
- **Acceptance Criteria**:
  - [ ] **No-fallback**: binding with `SourceType=EnvironmentVariable` never triggers Infisical SDK calls.
  - [ ] **No-leak**: serialized API responses assert no plaintext, no ciphertext, no env var value.
  - [ ] **Resilience**: Infisical source returns null after 3 retries; circuit breaker opens after 5 failures; env-var/inline sources resolve without resilience overhead.
  - [ ] **Audit trail**: every create/update/delete/validate/publish/write persists a `SecretBindingAuditEntry` with correct action, user, and timestamp.
  - [ ] **Version rotation lifecycle**: create Pending → validate → promote → verify Active version changed → cache invalidated → Previous still accessible during grace period.
  - [ ] **Tenant isolation**: tenant A cannot resolve tenant B's secrets via resolver; instance admin CAN see all bindings via admin endpoint with `IgnoreQueryFilters`.
  - [ ] **Structured validation**: `ValidateAsync` returns `SecretValidationDetail` with `Category` and `DiagnosticMessage`.
  - [ ] **Per-source health**: health check returns individual source statuses; Infisical unreachable → Degraded (not Unhealthy).
  - [ ] **HybridCache**: verify cache invalidation via tags propagates.
  - [ ] Architecture test: Domain.Secrets has no Infisical/DataProtection/Polly refs.
  - [ ] Architecture test: every `IgnoreQueryFilters()` on `SecretBinding` is Cerbos-gated.
- **Effort**: XL
- **Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`, `auth-patterns`

### 3.20 PR 3 verification
- **Acceptance Criteria**:
  - [ ] Build clean (0 errors, 0 warnings in Release).
  - [ ] All test projects pass individually.
  - [ ] New test count ≥ baseline + 60 (28 existing Phase 1-2 + ~60 new Phase 3 tests).
  - [ ] Cerbos policy compiles.
- **Effort**: S

---

## Phase 4 — Onboarding + Auth + Tenant Isolation (PR 4)

**PR title**: `refactor(secrets): route onboarding auth secrets through SecretBinding + tenant isolation enforcement`

### 4.1 Refactor `AuthProviderConfigurationService`
- **File**: `Explore.Application/Services/AuthProviderConfigurationService.cs`.
- **Acceptance Criteria**:
  - [ ] Non-secret enable flags stay in `SystemSetting`.
  - [ ] Secret writes dispatch `IMediator.Send(new UpdateSecretBindingCommand(...))`.
  - [ ] `ReadConfigurationAsync()` returns redacted + descriptor metadata from `ISecretResolver.DescribeAsync`.
  - [ ] `ReadConfigurationWithSecretsAsync()` removed.
- **Effort**: L
- **Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`, `blazor-bff-patterns`

### 4.2 Update onboarding command handlers
- **File**: `Explore.Application/Features/InstanceOnboarding/SaveAuthProviderConfigurationCommandHandler.cs`.
- **Acceptance Criteria**:
  - [ ] Calls new service contract.
  - [ ] Validator updated.
  - [ ] Integration tests pass (or updated).
- **Effort**: M

### 4.3 Remove `/auth-provider-configuration/internal` endpoint
- **File**: `Explore.API/Controllers/InstanceOnboardingController.cs`.
- **Acceptance Criteria**:
  - [ ] Endpoint deleted.
  - [ ] BFF caller updated to use `ISecretResolver` directly.
- **Effort**: M

### 4.4 `DynamicAuthSchemeManager` reads from resolver
- **File**: `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`.
- **Acceptance Criteria**:
  - [ ] Reads `auth.keycloak.client_secret` via `ISecretResolver.TryResolveAsync`.
  - [ ] `RefreshSchemeAsync` re-resolves and rebuilds OIDC handler.
  - [ ] Tests: secret missing → scheme disabled; secret updated → handler uses new secret on next request.
- **Effort**: L

### 4.5 Batch resolve API for onboarding 🆕
- **File**: `Explore.API/Controllers/SecretBindingsController.cs` (add endpoint).
- **Acceptance Criteria**:
  - [ ] `POST /api/SecretBindings/batch-resolve` accepts list of `(SettingKey, Scope, ScopeId?)` and returns list of `(SettingKey, IsConfigured, SourceType?)` — never values.
  - [ ] Used by onboarding UI to check all relevant secrets in one call.
  - [ ] Rate-limited to `write` policy.
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`

### 4.6 Onboarding UI — auto-detect chips
- **Files**: `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor` + `.razor.cs`.
- **Acceptance Criteria**:
  - [ ] Calls `GetAvailableSecretsForOnboardingQuery` on init.
  - [ ] Auto-detect chips per provider when secret resolves.
  - [ ] Input form never shows previously stored value after save.
  - [ ] A11y-compliant.
- **Effort**: L

### 4.7 Tenant isolation enforcement tests 🆕
- **Files**: `Event.API.IntegrationTests/Features/SecretBindingsTenantIsolationTests.cs`.
- **Acceptance Criteria**:
  - [ ] Tenant-scoped binding handler returns only the tenant's own secrets.
  - [ ] Instance-scoped binding handler returns all instance secrets (with Cerbos auth).
  - [ ] Cross-tenant attempt (tenant A tries to resolve tenant B's key) returns null, not 500.
- **Effort**: M

### 4.8 PR 4 verification
- **Effort**: S

---

## Phase 5 — Consumer Migration + File Source + Drift Detection (PR 5)

**PR title**: `refactor(secrets): migrate consumers to ISecretResolver + add FileSecretSource + drift detection`

### 5.1 `S3ConfigResolver` cutover
- **Acceptance Criteria**:
  - [ ] Reads `storage.s3.*` via `ISecretResolver.TryResolveAsync`.
  - [ ] Returns null when required secrets missing.
  - [ ] Tests updated.
- **Effort**: M

### 5.2 `SmtpConfigResolver` cutover
- **Acceptance Criteria**:
  - [ ] `smtp.username/password` resolved via `ISecretResolver`; remainder via governance.
  - [ ] Null host or missing secret → returns null.
- **Effort**: M

### 5.3 `AnalyticsConfigResolver` cutover
- **Acceptance Criteria**:
  - [ ] `analytics.posthog.public_key` / `host` resolved via `ISecretResolver`.
  - [ ] Fire-and-forget graceful fail preserved.
- **Effort**: S

### 5.4 Client-lifecycle audit for SMTP / S3 / PostHog
- **Acceptance Criteria**:
  - [ ] No singleton captures resolved credentials for process lifetime.
  - [ ] Scoped resolution per operation or subscribe to `SecretBindingChangedNotification` to dispose.
- **Effort**: M

### 5.5 `FileSecretSource` implementation 🆕
- **Files**: `Explore.Secrets/Sources/FileSecretSource.cs`, `Explore.Domain/Secrets/SecretSourceType.cs` (add `File = 3`), `Explore.Domain/Secrets/SecretDefinition.cs` (update factory), `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs` (add `FilePath` column + update CHECK constraint).
- **Acceptance Criteria**:
  - [ ] Reads files from disk using `binding.FilePath` (e.g., `/run/secrets/smtp_password`).
  - [ ] Validates file exists and is readable.
  - [ ] EF migration adds `FilePath` column + updates CHECK constraint for `SourceType = File`.
  - [ ] `SecretDefinitionRegistry` entries updated with `AllowedSourceTypes` including `File` for Docker/K8s-appropriate secrets.
  - [ ] Resilience: timeout-only (file I/O should be near-instant).
- **Effort**: M
- **Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

### 5.6 Configuration drift detection 🆕
- **Files**: `Explore.Secrets/Services/SecretDriftDetector.cs`, `Explore.Secrets/Services/SecretDriftDetectorHostedService.cs`.
- **Acceptance Criteria**:
  - [ ] At startup and periodically (configurable interval, default 1 hour), compares active `SecretBinding` rows against `SecretDefinitionRegistry.GetAll()`.
  - [ ] Logs structured warning for: binding with no registry entry (orphan), registry entry with no binding (unconfigured), binding pointing to unreachable source.
  - [ ] Emits `secrets.drift.detected` OpenTelemetry counter.
  - [ ] Does NOT block startup or resolve calls on failure.
- **Effort**: M
- **Skills**: `error-tracking`

### 5.7 Delete unused secret mappings
- **Acceptance Criteria**:
  - [ ] Delete remaining S3 + Keycloak compat mappings.
  - [ ] Tests that relied on compat mappings deleted or rewritten.
- **Effort**: S

### 5.8 PR 5 verification + graceful-degradation tests
- **Acceptance Criteria**:
  - [ ] Minimal deployment test: SMTP/S3/PostHog unconfigured → health green, pages load, email/S3/analytics "not configured".
  - [ ] All test projects pass.
- **Effort**: M

---

## Phase 6 — Deletion + Docs + Key Rotation Procedure (PR 6)

**PR title**: `refactor(secrets): delete legacy providers + destructive migration + key rotation docs`

### 6.1 Destructive EF migration — drop `AppSettings`
- **Acceptance Criteria**:
  - [ ] Drops `AppSettings` table and indexes.
  - [ ] Deletes `SystemSetting` rows for `InfrastructureSecretSettingKeys.*` namespace.
  - [ ] Migration reversibility deliberately not supported (dev mode).
- **Effort**: M

### 6.2 Delete legacy types
- **Acceptance Criteria**:
  - [ ] All listed files removed.
  - [ ] Solution builds.
  - [ ] No references remain via grep/architecture test.
- **Effort**: M

### 6.3 Delete `InfrastructureSecretSettingKeys`
- **Acceptance Criteria**:
  - [ ] File deleted.
  - [ ] All usages replaced with `SecretDefinitionRegistry` keys.
- **Effort**: S

### 6.4 Adapt observability
- **Acceptance Criteria**:
  - [ ] Old metrics/health checks deleted.
  - [ ] New names documented in `docs/SECRETS.md`.
- **Effort**: S

### 6.5 Architecture regression test
- **Acceptance Criteria**:
  - [ ] Asserts no reference to deleted types.
  - [ ] Asserts `ISecretResolver` is the only registered secret-fetch contract.
  - [ ] Asserts `SecretBindingAuditEntry` is written for all mutations.
- **Effort**: S

### 6.6 Document Data Protection key rotation procedure 🆕
- **File**: `docs/SECRETS.md` (section: Key Rotation).
- **Acceptance Criteria**:
  - [ ] Step-by-step procedure: (1) Create new purpose string version (e.g., `v2`), (2) Deploy with both `v1` and `v2` protectors registered, (3) Migrate inline values via admin API, (4) Verify all bindings validate, (5) Remove `v1` protector registration.
  - [ ] Test that verifies the rotation workflow end-to-end.
  - [ ] Disaster recovery note: DP keys and ciphertext must be backed up together.
- **Effort**: M
- **Skills**: `auth-patterns`

### 6.7 Rewrite `docs/SECRETS.md`
- **Acceptance Criteria**:
  - [ ] Documents control-plane/data-plane model, `SecretDefinitionRegistry`, `SecretBinding`, `ISecretResolver`, three source types, blue/green rotation workflow, audit trail, resilience, versioning, TTL, UI contract, bootstrap path, Infisical folder layout, DP key rotation, `FileSecretSource`, drift detection.
  - [ ] Removes references to deleted types.
- **Effort**: L

### 6.8 Update companion docs
- **Files**: `docs/CONFIGURATION.md`, `docs/QUICK_REFERENCE.md`, `docs/TROUBLESHOOTING.md`.
- **Acceptance Criteria**:
  - [ ] `CONFIGURATION.md` replaces `AddInfisicalCompatibility` / `AddSecretManagement` with `AddSecretResolution` + `BootstrapSecretLoader`.
  - [ ] `QUICK_REFERENCE.md` adds new invariants: "no fallback chain", "audit trail on all mutations", "HybridCache for multi-instance", "Polly resilience on Infisical calls", "tenant isolation via query filter".
  - [ ] `TROUBLESHOOTING.md` adds failure modes: DP key-ring lost, Infisical unreachable (circuit breaker states), validation categories explained, TTL expiry handling, version rotation promotion.
- **Effort**: M

### 6.9 Load/performance test scaffolding 🆕
- **File**: `Explore.Secrets.Tests/Performance/SecretResolverBenchmarkTests.cs`.
- **Acceptance Criteria**:
  - [ ] Benchmark: resolve 10,000 secrets with HybridCache L1 hit (target: <1ms per resolve).
  - [ ] Benchmark: resolve with Infisical circuit breaker open (target: <5ms fallback to cache).
  - [ ] Stress: 100 concurrent resolve calls (target: no deadlocks, no exceptions).
- **Effort**: M

### 6.10 Security test scaffolding 🆕
- **File**: `Event.API.IntegrationTests/Security/SecretBindingsSecurityTests.cs`.
- **Acceptance Criteria**:
  - [ ] Anonymous GET returns 401.
  - [ ] Non-admin user gets 403.
  - [ ] Tenant A cannot resolve tenant B's secrets.
  - [ ] Admin API responses never contain plaintext/ciphertext (regex scan).
  - [ ] Rate limiting enforced on validate and promote endpoints.
- **Effort**: M

### 6.11 PR 6 verification
- **Acceptance Criteria**:
  - [ ] `dotnet build --configuration Release --verbosity quiet` passes.
  - [ ] All test projects in CLAUDE.md pass individually.
  - [ ] Manual smoke: fresh DB, minimal deployment boots, onboarding completes, admin Secrets page lists all registry entries.
- **Effort**: S

---

## Meta

### Branch strategy
- One feature branch per PR, off `develop`.
- Branches named `refactor/secrets-phase-{N}-{slug}`.
- Each PR includes its own commit(s) per `conventional-commit` skill.

### Review cycle
- Phase 3 and Phase 4 PRs get an extra Oracle consultation before merge (concurrency + Keycloak dynamic scheme + resilience patterns).
- Phase 6 gets an `ai-slop-remover` skill pass.

### Out of scope (deliberately deferred to post-1.0)
- Infisical webhook integration (cache has `InvalidateAsync` hook; webhook endpoint later).
- `Module`-scoped bindings.
- `Inherited` as a persisted source type (computed by resolver).
- Additional providers (Vault, Azure Key Vault, AWS Secrets Manager).
- RLS for `SecretBindings` (tracked under post-1.0).
- Automated secret rotation workflows (manual + blue/green via UI supported; automated rotation later).
- Import/Export API for bulk binding management.
- Dynamic `SecretDefinition` registration (runtime loading).
- Vault dynamic secrets with lease management (schema ready, provider not implemented).
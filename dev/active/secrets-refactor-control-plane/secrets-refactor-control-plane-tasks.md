ABOUTME: Detailed task checklist for the Secrets Refactor (Control Plane / Data Plane) with acceptance criteria, effort, and skill references.
ABOUTME: Six-phase, six-PR plan. Each phase is independently reviewable. Tasks within a phase ordered by dependency.

# Secrets Refactor - Task Checklist

Last Updated: 2026-04-18

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
  - [x] `SecretBinding` implements `IAuditableEntity` + `IRowVersionable` matching repo convention.
  - [x] `SecretBinding.Id` is UUIDv7.
  - [x] Navigation properties readonly; writes via repository.
  - [x] Domain event raised on mutation.
  - [x] No default values in entity body.
  - [x] File-scoped namespaces + `ABOUTME:` headers.
- **Effort**: M
- **Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

### 1.3 EF configuration + CHECK + filtered unique indexes ✅
- **File**: `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs`.
- **Acceptance Criteria**:
  - [x] Columns with explicit max-length for strings.
  - [x] Named `HasQueryFilter("SoftDelete", e => !e.IsDeleted)`.
  - [x] CHECK constraint for source-type-exclusive metadata.
  - [x] Two partial unique indexes for Instance and Tenant scopes.
  - [x] Auditable columns conventionally configured.
- **Effort**: M
- **Skills**: `dotnet-efcore-guidelines`, `clean-architecture-rules`

### 1.4 Create `ISecretBindingRepository` + implementation ✅
- **Files**: `Explore.Application/Contracts/Persistence/ISecretBindingRepository.cs`, `Explore.Persistence/Repositories/SecretBindingRepository.cs`.
- **Acceptance Criteria**:
  - [x] Methods: `GetAsync`, `ListAsync`, `ListInstanceAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.
  - [x] Returns entities, not DTOs.
  - [x] Repository does not swallow exceptions.
- **Effort**: S
- **Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`

### 1.5 Register Data Protection with EF keyring ✅
- **Files**: `Explore.Persistence/Extensions/DataProtectionServiceCollectionExtensions.cs`, `Explore.Persistence/PersistenceServicesRegistration.cs`.
- **Acceptance Criteria**:
  - [x] `PersistKeysToDbContext<ExploreDbContext>()` with `SetApplicationName("islamu-event")`.
  - [x] `DataProtectionKeys` table in EF migration.
  - [x] Round-trip verified.
- **Effort**: S
- **Skills**: `dotnet-efcore-guidelines`, `auth-patterns`

### 1.6 Registry-enforced domain invariants ✅
- **File**: `Explore.Domain/Secrets/SecretBinding.cs` factory methods + `Event.Domain.UnitTests/Entities/SecretBindingTests.cs`.
- **Acceptance Criteria**:
  - [x] `SecretBinding.Create` throws on unknown key, disallowed scope, disallowed source type, bootstrap+InlineEncrypted.
  - [x] 17 unit tests cover each failure mode.
- **Effort**: M
- **Skills**: `clean-architecture-rules`

### 1.7 EF migration for SecretBindings + DataProtectionKeys ✅
- **File**: `Explore.Persistence/Migrations/20260418154035_AddSecretBindingsAndDataProtectionKeys.cs`.
- **Acceptance Criteria**:
  - [x] Both tables + filtered unique indexes created.
  - [x] Tests pass.
- **Effort**: S
- **Skills**: `dotnet-efcore-guidelines`

### 1.8 Architecture test ✅
- **File**: `Event.Architecture.Tests/SecretsArchitectureTests.cs` (part of Phase 1 scope — namespace layer enforcement tested alongside).
- **Acceptance Criteria**:
  - [x] Architecture tests pass (74 green).
- **Effort**: S
- **Skills**: `clean-architecture-rules`

### 1.9 PR 1 verification ✅
- **Acceptance Criteria**:
  - [x] Build passes.
  - [x] Domain unit tests pass (207).
  - [x] Architecture tests pass (74).
  - [x] Application unit tests pass (823).
  - [x] Secrets unit tests pass (201).

---

## Phase 2 — Bootstrap Split (PR 2) ✅ COMMITTED `fc0b2b5a`

**PR title**: `refactor(secrets): discrete Postgres bootstrap via NpgsqlConnectionStringBuilder`

### 2.1 `BootstrapSecretLoader` ✅
- **File**: `Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` + `BootstrapPostgresCredentials.cs`.
- **Acceptance Criteria**:
  - [x] Given a registry key marked `IsBootstrap=true`, fetches from Infisical (if bootstrap config present) else environment variable else `IConfiguration` section.
  - [x] Never touches `ISecretResolver` or `SecretBinding`.
  - [x] Exposes `LoadPostgresConnectionString()` returning `BootstrapPostgresCredentials` with composed NpgsqlConnectionStringBuilder (SslMode=Prefer + TrustServerCertificate=true, DefaultPort=5432).
  - [x] Fails with a structured log message listing each missing discrete field and the source attempted.
- **Effort**: M
- **Skills**: `dotnet-efcore-guidelines`, `auth-patterns`

### 2.2 Refactor `PersistenceServicesRegistration` ✅
- **File**: `Explore.Persistence/PersistenceServicesRegistration.cs` + `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`.
- **Acceptance Criteria**:
  - [x] No longer reads `ConnectionStrings:DefaultConnection`.
  - [x] Calls `BootstrapSecretLoader.LoadPostgresConnectionString()` synchronously.
  - [x] Preserves `AddPooledDbContextFactory<ExploreDbContext>` behavior.
- **Effort**: S
- **Skills**: `dotnet-efcore-guidelines`

### 2.3 Remove `POSTGRESQL_PUBLIC_URL` from config mapping ✅
- **Files**: `Explore.API/Extensions/ConfigurationExtensions.cs`, `Explore.Blazor/Extensions/ConfigurationExtension.cs`, `Event.MigrationService/Extensions/ConfigurationExtensions.cs`, `Event.MigrationService/Program.cs`.
- **Acceptance Criteria**:
  - [x] Deleted `POSTGRESQL_PUBLIC_URL` → `ConnectionStrings:DefaultConnection` mapping from API and Blazor config extensions.
  - [x] MigrationService now uses `AddDiscretePostgresBootstrap()` via BootstrapSecretLoader.
  - [x] S3/Keycloak mappings kept temporarily with architectural-invariant comments.
- **Effort**: S
- **Skills**: `clean-architecture-rules`

### 2.4 Update infra files ✅
- **Files**: `docker-compose.yml`, `Explore.AppHost/AppHost.cs`.
- **Acceptance Criteria**:
  - [x] `docker-compose.yml` has `x-postgres-bootstrap-env` anchor with discrete `POSTGRESQL_HOST/PORT/DATABASE/USERNAME/PASSWORD`; removed `POSTGRESQL_PUBLIC_URL`.
  - [x] `docker-compose.yml` canonicalized `x-secrets-env` to `SecretProvider__Infisical__*` format.
  - [x] Aspire `AppHost.cs` passes discrete Postgres env vars + updated banner.
- **Effort**: S
- **Skills**: (infra)

### 2.5 `BootstrapSecretLoaderTests` ✅
- **File**: `Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs`.
- **Acceptance Criteria**:
  - [x] Covers all three source fallbacks in isolation.
  - [x] Covers missing-field failure modes.
  - [x] Verifies `SslMode=Prefer` and `Trust Server Certificate=True` in composed connection string.
  - [x] Verifies DefaultPort=5432 fallback.
  - [x] Verifies mixed-source labels.
- **Effort**: M
- **Skills**: `dotnet-efcore-guidelines`

### 2.6 PR 2 verification ✅
- **Acceptance Criteria**:
  - [x] Build clean (0 errors, 0 warnings in Release).
  - [x] All 1,305 tests green (Event.Application.UnitTests 823, Event.Domain.UnitTests 207, Event.Architecture.Tests 74, Explore.Secrets.UnitTests 201).
  - [x] 0 regressions.
- **Effort**: S

---

## Phase 3 — Resolver + Admin API (PR 3) 🟡 IN PROGRESS (mid-flight handoff)

**PR title**: `refactor(secrets): phase 3 introduce ISecretResolver + admin bindings API`

**⚠️ SESSION HANDOFF STATE (read before continuing):**
- Runtime pipeline (3.1–3.4) is **WRITTEN TO DISK but NOT COMMITTED**. 14 new files + 1 modified csproj sitting untracked.
- Build verified clean on `Explore.Secrets` project (0 errors). Solution-wide build + tests NOT yet re-run.
- Admin surface (3.5–3.12) is **NOT STARTED**.
- Next session: follow `phase-3-implementation-plan.md` sections 3.7–3.21 AND the numbered 3.5–3.12 items below. (Plan file uses finer sub-task numbering 3.1–3.21; tasks file uses 3.1–3.12 — same work, different granularity.)
- See `secrets-refactor-control-plane-context.md` → "SESSION PROGRESS" → "IN PROGRESS" for the full file list and entity-reality-check notes.
- Single Phase 3 commit at end (no splitting), per user directive.

### 3.1 Per-source abstractions + implementations 🟡 WRITTEN, UNCOMMITTED
- **Files**: `Explore.Application/Contracts/Secrets/IInfisicalSecretSource.cs`, `Explore.Secrets/Services/{InfisicalSecretSource, EnvironmentSecretSource, InlineSecretSource}.cs`.
- **Acceptance Criteria**:
  - [ ] `IInfisicalSecretSource.TryFetchAsync(environment, path, key, ct)` returns plaintext or null.
  - [ ] Uses `Infisical.Sdk` v3 Universal Auth with bootstrap `Infisical:*` config.
  - [ ] `EnvironmentSecretSource.TryFetch(variableName)` wraps `Environment.GetEnvironmentVariable`.
  - [ ] `InlineSecretSource.TryUnprotect(ciphertext, version)` uses `IDataProtectionProvider.CreateProtector(new[] { "Event.Secrets", "Binding", version })`. Catches `CryptographicException` → returns null + metric.
- **Effort**: L
- **Skills**: `auth-patterns`, `dotnet-efcore-guidelines`

### 3.2 `SecretResolver` service 🟡 WRITTEN, UNCOMMITTED
- **File**: `Explore.Secrets/Services/SecretResolver.cs`.
- **Acceptance Criteria**:
  - [ ] Implements `ISecretResolver`.
  - [ ] `TryResolveAsync(settingKey, tenantId, ct)` looks up `SecretBinding` at (Scope=Tenant, tenantId) first (if `tenantId` provided), falls through to (Scope=Instance, ScopeId=null), else returns null.
  - [ ] Dispatches on `SourceType` to exactly one source; **does not fall back** to another source on miss.
  - [ ] Uses `IMemoryCache` with 5-min TTL keyed on `(settingKey, tenantId, bindingUpdatedAt)`.
  - [ ] Exposes `InvalidateAsync(settingKey, tenantId, ct)` that evicts all cache entries matching the key.
  - [ ] `DescribeAsync` returns state + metadata (never the value) incl. computed `IsInherited` flag.
- **Effort**: XL
- **Skills**: `clean-architecture-rules`, `cqrs-mediatr-guidelines`

### 3.3 Auditing decorator 🟡 WRITTEN, UNCOMMITTED
- **File**: `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs`.
- **Acceptance Criteria**:
  - [ ] Wraps `ISecretResolver`.
  - [ ] Audits every write/delete/validate synchronously.
  - [ ] Audits read failures synchronously.
  - [ ] Samples successful reads at configurable rate (default 1%).
  - [ ] Never logs plaintext or ciphertext.
  - [ ] Redacts secret values from any exception text that may flow through.
- **Effort**: M
- **Skills**: `error-tracking`, `auth-patterns`

### 3.4 Metrics + health check adaptation 🟡 WRITTEN, UNCOMMITTED
- **Files**: `Explore.Secrets/Observability/SecretResolverMetrics.cs`, `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs`.
- **Acceptance Criteria**:
  - [ ] `Meter("Event.Secrets.Resolver")` with counters (`resolve_total`, `resolve_failure_total`, `validate_total`, `cache_hit_total`, `cache_miss_total`) and histogram (`resolve_duration_ms`).
  - [ ] Health check `secret_resolver` tagged `secrets`, Healthy when Infisical source reachable; Degraded otherwise.
- **Effort**: M
- **Skills**: `error-tracking`

### 3.5 Admin CQRS — commands
- **Files**: `Explore.Application/Features/Secrets/Commands/` (Create/Update/Delete/Validate with `*CommandValidator.cs`, `*CommandHandler.cs`).
- **Acceptance Criteria**:
  - [ ] Each command returns `BaseCommandResponse<Guid>` (create/update) or `BaseCommandResponse<bool>` (delete/validate).
  - [ ] Handlers validate against `SecretDefinitionRegistry` + `ISecretBindingRepository`.
  - [ ] Validators are manually instantiated per project rule.
  - [ ] `CreateSecretBindingCommand` for `SourceType=InlineEncrypted` receives plaintext, protects it immediately, stores ciphertext, discards plaintext (no return of plaintext).
  - [ ] `ValidateSecretBindingCommand` calls `ISecretResolver.TryResolveAsync` under a short timeout, updates `LastValidationResult/At/Message/By`; discards the fetched value.
  - [ ] All mutations raise `SecretBindingUpdatedEvent` via `IMediator.Publish`.
- **Effort**: L
- **Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

### 3.6 Admin CQRS — queries
- **Files**: `Explore.Application/Features/Secrets/Queries/GetSecretBindingsQueryHandler.cs`, `DescribeSecretBindingQueryHandler.cs`, `GetAvailableSecretsForOnboardingQueryHandler.cs`.
- **Acceptance Criteria**:
  - [ ] Handlers return `SecretBindingDescriptor` / `SecretBindingDto`.
  - [ ] DTOs never include `InlineCiphertext`, resolved plaintext, or env var value.
  - [ ] `GetAvailableSecretsForOnboardingQuery` returns a list of `{ SettingKey, AutoDetected (bool), Source? }` filtered to the onboarding-relevant keys (Keycloak, SMTP, S3, PostHog, AI).
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`

### 3.7 Cache invalidation + Keycloak scheme refresh handlers
- **Files**: `Explore.Application/Notifications/SecretBindingCacheInvalidationHandler.cs`, `Explore.Application/Notifications/KeycloakSchemeRefreshHandler.cs`.
- **Acceptance Criteria**:
  - [ ] Cache-invalidation handler calls `ISecretResolver.InvalidateAsync` synchronously before returning.
  - [ ] Keycloak handler calls `IDynamicAuthSchemeManager.RefreshSchemeAsync("Keycloak")` when the updated key is `auth.keycloak.client_secret` (and similar for Google).
  - [ ] Both are `INotificationHandler<SecretBindingUpdatedEvent>`.
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`

### 3.8 `SecretBindingsController`
- **File**: `Explore.API/Controllers/SecretBindingsController.cs`.
- **Acceptance Criteria**:
  - [ ] `GET /api/SecretBindings?scope=...&scopeId=...` `[Authorize]` + Cerbos action `secret_binding:read`.
  - [ ] `GET /api/SecretBindings/{settingKey}` same auth.
  - [ ] `PUT /api/SecretBindings/{settingKey}` `[Authorize]` + Cerbos `secret_binding:write` + `write` rate limit policy.
  - [ ] `DELETE /api/SecretBindings/{settingKey}` same auth.
  - [ ] `POST /api/SecretBindings/{settingKey}/validate` `[Authorize]` + `write` rate limit.
  - [ ] Responses never contain plaintext / ciphertext.
  - [ ] Uses `BaseCommandResponse<>` shape.
  - [ ] HAL links via `SecretBindingLinkPolicy`.
- **Effort**: L
- **Skills**: `auth-patterns`, `cqrs-mediatr-guidelines`

### 3.9 HATEOAS policy + assembler
- **Files**: `Explore.API/Hateoas/Policies/SecretBindingLinkPolicy.cs`, `Explore.API/Hateoas/Assemblers/SecretBindingResourceAssembler.cs`.
- **Acceptance Criteria**:
  - [ ] Matches `yield return` pattern used by other policies.
  - [ ] Named routes registered in `RouteNames`.
- **Effort**: S
- **Skills**: `clean-architecture-rules`

### 3.10 Cerbos policy
- **File**: `cerbos/policies/secret_binding.yaml`.
- **Acceptance Criteria**:
  - [ ] Resource `secret_binding` with actions `read`, `write`.
  - [ ] Instance admin: read/write all scopes.
  - [ ] Tenant admin: read/write tenant-scope only (`tenantId` matches).
  - [ ] Default deny.
- **Effort**: S
- **Skills**: `auth-patterns`

### 3.11 Tests — no-fallback + no-leak
- **Files**: `Explore.Secrets.UnitTests/NoFallbackTests.cs`, `NoValueExposureTests.cs`, `SecretResolverTests.cs`, `InfisicalSecretSourceTests.cs`, `EnvironmentSecretSourceTests.cs`, `InlineSecretSourceTests.cs`, `Event.Application.UnitTests/Features/Secrets/*Tests.cs`, `Event.API.IntegrationTests/Features/SecretBindingsControllerTests.cs`.
- **Acceptance Criteria**:
  - [ ] No-fallback: a binding with `SourceType=EnvironmentVariable` never triggers Infisical SDK calls.
  - [ ] No-leak: serialized API responses assert no plaintext, no ciphertext, no `SMTP_PASSWORD` var value.
  - [ ] `ValidateSecretBindingCommand` updates state, but the command response contains generic message only.
  - [ ] Rate-limit regression test against `PUT` / `POST /validate`.
- **Effort**: L
- **Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`

### 3.12 PR 3 verification
- **Acceptance Criteria**:
  - [ ] Build clean.
  - [ ] All test projects pass.
  - [ ] New Cerbos policy compiles (`cerbos compile cerbos/policies`).
- **Effort**: S

---

## Phase 4 — Onboarding + Auth (PR 4)

**PR title**: `refactor(secrets): route onboarding auth secrets through SecretBinding`

### 4.1 Refactor `AuthProviderConfigurationService`
- **File**: `Explore.Application/Services/AuthProviderConfigurationService.cs`.
- **Acceptance Criteria**:
  - [ ] Non-secret enable flags stay in `SystemSetting`.
  - [ ] Secret writes now dispatch `IMediator.Send(new UpdateSecretBindingCommand(...))` - never write secrets into `SystemSetting.Value`.
  - [ ] `ReadConfigurationAsync()` returns redacted + descriptor metadata from `ISecretResolver.DescribeAsync`.
  - [ ] `ReadConfigurationWithSecretsAsync()` removed (no longer needed - BFF will resolve directly).
  - [ ] `IsConfiguredAsync()` unchanged behavior.
- **Effort**: L
- **Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`, `blazor-bff-patterns`

### 4.2 Update onboarding command handlers
- **File**: `Explore.Application/Features/InstanceOnboarding/SaveAuthProviderConfigurationCommandHandler.cs`.
- **Acceptance Criteria**:
  - [ ] Calls new service contract.
  - [ ] Validator updated if changed.
  - [ ] Existing integration tests still pass (or are updated).
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`

### 4.3 Remove `/auth-provider-configuration/internal` endpoint
- **File**: `Explore.API/Controllers/InstanceOnboardingController.cs`.
- **Acceptance Criteria**:
  - [ ] Endpoint deleted.
  - [ ] Related integration tests either deleted or rewritten.
  - [ ] Any BFF caller updated to use `ISecretResolver` directly (or BFF endpoint proxying `GET /api/SecretBindings`).
- **Effort**: M
- **Skills**: `auth-patterns`, `blazor-bff-patterns`

### 4.4 `DynamicAuthSchemeManager` reads from resolver
- **File**: `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`.
- **Acceptance Criteria**:
  - [ ] Reads `auth.keycloak.client_secret` (and Google counterpart) via `ISecretResolver.TryResolveAsync` at scheme registration time.
  - [ ] `RefreshSchemeAsync` re-resolves and rebuilds the in-memory OIDC handler.
  - [ ] Tests cover: secret missing → scheme disabled; secret updated → handler uses new secret on next request; tests assert in-flight OIDC exchange not disrupted.
- **Effort**: L
- **Skills**: `auth-patterns`, `blazor-bff-patterns`

### 4.5 Onboarding UI — auto-detect chips
- **Files**: `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor` + `.razor.cs`.
- **Acceptance Criteria**:
  - [ ] Calls `GetAvailableSecretsForOnboardingQuery` on init.
  - [ ] For each provider (Keycloak, Google SSO): shows `Auto-detected` chip + disables toggle when secret resolves; otherwise shows input form (inline-encrypted default source).
  - [ ] Input form never shows previously stored value after save.
  - [ ] A11y-compliant: labels, aria-describedby, proper focus management.
- **Effort**: L
- **Skills**: `blazor-ui-conventions`, `blazor-bff-patterns`, `accessibility`

### 4.6 Tests — onboarding auto-detection + no-leak
- **Files**: `Event.API.IntegrationTests/Features/OnboardingAutoDetectTests.cs`, `Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs` (updated).
- **Acceptance Criteria**:
  - [ ] Configures Keycloak secrets in env vars, asserts onboarding status shows auto-detected for Keycloak.
  - [ ] Posts a new Keycloak secret via onboarding, asserts binding created with `SourceType=InlineEncrypted`, asserts resolved value matches what was posted.
  - [ ] No response contains plaintext.
- **Effort**: M
- **Skills**: `blazor-bff-patterns`, `cqrs-mediatr-guidelines`

### 4.7 PR 4 verification
- **Effort**: S

---

## Phase 5 — Consumer Migration (PR 5)

**PR title**: `refactor(secrets): migrate S3/SMTP/Analytics resolvers to ISecretResolver`

### 5.1 `S3ConfigResolver` cutover
- **File**: `Explore.Infrastructure/Services/S3ConfigResolver.cs`.
- **Acceptance Criteria**:
  - [ ] Reads `storage.s3.*` via `ISecretResolver.TryResolveAsync` (secrets) and `IHierarchicalSettingsResolver` (non-secret config).
  - [ ] Returns `null` config object when required secrets missing → callers already handle this.
  - [ ] Tests in `Event.Application.UnitTests/Infrastructure/S3ConfigResolverTests.cs` updated.
- **Effort**: M
- **Skills**: `clean-architecture-rules`

### 5.2 `SmtpConfigResolver` cutover
- **File**: `Explore.Infrastructure/Services/SmtpConfigResolver.cs`.
- **Acceptance Criteria**:
  - [ ] `smtp.username/password` resolved via `ISecretResolver`; remainder via governance.
  - [ ] Null host or missing secret → returns null; `SmtpEmailService` logs warning per existing pattern.
  - [ ] Tests updated.
- **Effort**: M
- **Skills**: `clean-architecture-rules`

### 5.3 `AnalyticsConfigResolver` cutover
- **File**: `Explore.Infrastructure/Services/AnalyticsConfigResolver.cs`.
- **Acceptance Criteria**:
  - [ ] `analytics.posthog.public_key`, `analytics.posthog.host` resolved via `ISecretResolver` (if secrets - confirm with registry which are secret).
  - [ ] Fire-and-forget graceful fail preserved.
- **Effort**: S
- **Skills**: `clean-architecture-rules`

### 5.4 Client-lifecycle audit for SMTP / S3 / PostHog
- **Files**: `Explore.Infrastructure/Services/{SmtpEmailService, S3StorageService, PostHogAnalyticsProvider}.cs`.
- **Acceptance Criteria**:
  - [ ] No singleton captures resolved credentials for process lifetime.
  - [ ] Scoped resolution per operation; if a client must be long-lived, it re-reads `ISecretResolver` on each operation or subscribes to `SecretBindingUpdatedEvent` to dispose.
- **Effort**: M
- **Skills**: `clean-architecture-rules`, `error-tracking`

### 5.5 Delete unused secret mappings
- **Files**: `Explore.API/Extensions/ConfigurationExtensions.cs`, `Explore.Blazor/Extensions/ConfigurationExtensions.cs`.
- **Acceptance Criteria**:
  - [ ] Delete remaining S3 + Keycloak compat mappings (phase 2 left them; now remove).
  - [ ] Tests that relied on compat mappings deleted or rewritten.
- **Effort**: S
- **Skills**: `clean-architecture-rules`

### 5.6 PR 5 verification + graceful-degradation tests
- **Files**: `Event.API.IntegrationTests/Features/MinimalDeploymentTests.cs` (new).
- **Acceptance Criteria**:
  - [ ] Spins up with no SMTP, no S3, no PostHog secrets - health endpoint green, home page loads, attempts to send email → safe no-op with log.
  - [ ] Other test projects pass.
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`

---

## Phase 6 — Deletion + Docs (PR 6)

**PR title**: `refactor(secrets): delete legacy providers + destructive migration`

### 6.1 Destructive EF migration — drop `AppSettings`
- **File**: `Explore.Persistence/Migrations/{timestamp}_DropAppSettingsAndLegacySecretRows.cs`.
- **Acceptance Criteria**:
  - [ ] Drops `AppSettings` table and indexes.
  - [ ] Deletes any `SystemSetting` row whose key starts with `email.smtp_username`, `email.smtp_password`, `s3.access_key_id`, `s3.secret_access_key`, `cerbos.custom_admin_*`, `auth.keycloak_client_secret`, `auth.google_client_secret` (the old `InfrastructureSecretSettingKeys` namespace).
  - [ ] Migration reversibility deliberately not supported (dev mode).
- **Effort**: M
- **Skills**: `dotnet-efcore-guidelines`

### 6.2 Delete legacy types
- **Files**: See `secrets-refactor-control-plane-context.md` > "Key Files - Deleted (PR 6)".
- **Acceptance Criteria**:
  - [ ] All listed files removed.
  - [ ] Solution builds.
  - [ ] No references remain via `grep`/architecture test.
- **Effort**: M
- **Skills**: `clean-architecture-rules`

### 6.3 Delete `InfrastructureSecretSettingKeys`
- **File**: `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`.
- **Acceptance Criteria**:
  - [ ] File deleted.
  - [ ] All usages replaced with `SecretDefinitionRegistry` keys during Phase 4/5 already; confirm none remain.
- **Effort**: S
- **Skills**: `clean-architecture-rules`

### 6.4 Adapt observability
- **Files**: `Explore.Secrets/Observability/SecretResolverMetrics.cs` (finalized), `SecretResolverHealthCheck.cs`.
- **Acceptance Criteria**:
  - [ ] Old `SecretRefreshMetrics`, `SecretProviderHealthCheck` deleted.
  - [ ] Metric/health names documented in `docs/SECRETS.md`.
- **Effort**: S
- **Skills**: `error-tracking`

### 6.5 Architecture regression test
- **File**: `Event.Architecture.Tests/SecretsArchitectureTests.cs` (extended).
- **Acceptance Criteria**:
  - [ ] Asserts no reference to `AppSetting`, `DbConfigurationProvider`, `InfisicalConfigurationProvider`, `AesEncryptionService`, `KeyRotationService`, `SecretRefreshService`, `InfrastructureSecretSettingKeys` anywhere in the solution.
  - [ ] Asserts `ISecretResolver` is the only registered secret-fetch contract.
- **Effort**: S
- **Skills**: `clean-architecture-rules`

### 6.6 Rewrite `docs/SECRETS.md`
- **File**: `docs/SECRETS.md`.
- **Acceptance Criteria**:
  - [ ] Documents the new control-plane/data-plane model, `SecretDefinitionRegistry`, `SecretBinding`, `ISecretResolver`, the three source types, UI contract, bootstrap path, Infisical folder layout, disaster-recovery note for DP keys.
  - [ ] Removes references to deleted types.
- **Effort**: M
- **Skills**: `error-tracking` (observability sections)

### 6.7 Update companion docs
- **Files**: `docs/CONFIGURATION.md`, `docs/QUICK_REFERENCE.md`, `docs/TROUBLESHOOTING.md`.
- **Acceptance Criteria**:
  - [ ] `CONFIGURATION.md` replaces `AddInfisicalCompatibility` / `AddSecretManagement` sections with `AddSecretResolution` + `BootstrapSecretLoader`.
  - [ ] `QUICK_REFERENCE.md` adds the new invariant "no fallback chain - one binding, one source, one fetch".
  - [ ] `TROUBLESHOOTING.md` adds common failure modes (DP key-ring lost, Infisical unreachable, validation failure semantics).
- **Effort**: M
- **Skills**: (docs)

### 6.8 PR 6 verification
- **Acceptance Criteria**:
  - [ ] `dotnet build --configuration Release --verbosity quiet` passes.
  - [ ] All test projects in CLAUDE.md pass individually.
  - [ ] Manual smoke: fresh DB, minimal deployment boots, onboarding completes, admin Secrets page lists all registry entries with correct state.
- **Effort**: M

---

## Meta

### Branch strategy
- One feature branch per PR, off `main`.
- Branches named `refactor/secrets-phase-{N}-{slug}` using `gitkraken-cli` skill conventions.
- Each PR includes its own commit(s) per `conventional-commit` skill (`refactor(secrets): ...`).

### Review cycle
- Phase 3 and Phase 4 PRs get an extra Oracle consultation before merge (concurrency + Keycloak dynamic scheme).
- Phase 6 gets an `ai-slop-remover` skill pass to ensure deleted-file list + doc rewrite are clean.

### Out of scope (deliberately deferred)
- Infisical webhook integration (cache has `InvalidateAsync` hook; webhook endpoint to be added in a follow-up sprint).
- `Module`-scoped bindings.
- `Inherited` as a persisted source type.
- Additional providers (Vault, Azure Key Vault, AWS Secrets Manager).
- RLS for `SecretBindings` (tracked under post-1.0 plan per `docs/SECURITY.md`).
- Automatic rotation workflows (manual via UI supported; automated rotation post-1.0).

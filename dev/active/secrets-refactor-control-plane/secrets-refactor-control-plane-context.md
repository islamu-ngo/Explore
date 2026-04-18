ABOUTME: Session context and key reference map for the Secrets Refactor (Control Plane / Data Plane) task - quick-resume aid for multi-session work.
ABOUTME: Links to every file being introduced, replaced, or deleted so any session can pick up the refactor without repeating exploration.

# Secrets Refactor - Context

Last Updated: 2026-04-18 (session handoff mid-Phase 3)

## SESSION PROGRESS

### ✅ COMPLETED
- Research: 5 parallel agents mapped current state - `Explore.Secrets` internals, all consumers, onboarding flow, Infisical SDK best practices, Data Protection / Npgsql patterns.
- Oracle architecture review - verdict: ship direction with four adjustments: (a) split bootstrap from runtime, (b) introduce `SecretDefinitionRegistry`, (c) normalized metadata columns not polymorphic JSON, (d) drop `Module` scope and `Inherited` source type from v1.
- Plan file authored: `secrets-refactor-control-plane-plan.md`.
- Tasks file authored: `secrets-refactor-control-plane-tasks.md`.
- **Phase 1 — COMMITTED as `38ce8098`** on branch `develop`: SecretBinding + SecretDefinitionRegistry foundations (24 files changed, 16,650 insertions, 2 deletions).
  - All 8 sub-tasks (1.1–1.8) complete: enums, entity, registry, EF config, repository, Data Protection keyring, unit tests, architecture scope gate test.
  - 1,305 tests green across 4 projects.
- **Phase 2 — COMMITTED as `fc0b2b5a`** on branch `develop`: Discrete Postgres bootstrap via NpgsqlConnectionStringBuilder (12 files changed, 831 insertions, 157 deletions).
  - All 6 sub-tasks (2.1–2.6) complete: BootstrapSecretLoader, NpgsqlConnectionStringBuilder composition, PersistenceServicesRegistration + Blazor DI refactor, POSTGRESQL_PUBLIC_URL removal from config mappings, docker-compose + AppHost discrete env vars, BootstrapSecretLoaderTests (11 tests).
  - 1,305 tests green, 0 regressions.

### 🟡 IN PROGRESS — Phase 3 (mid-flight, context exhausted — session handed off)

**Phase 3 runtime pipeline (sub-tasks 3.1–3.6) — COMPLETE but UNCOMMITTED.** 14 files created, 1 modified. Build verified clean (0 errors on `Explore.Secrets` project).

Files written and sitting on disk (NOT in any commit yet):

**Domain layer (1 new file):**
- `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs` — sealed record `(Guid BindingId, string SettingKey, SecretScope Scope, Guid? ScopeId, SecretSourceType SourceType, SecretBindingChangeKind ChangeKind, DateTimeOffset OccurredAt)` + `SecretBindingChangeKind` enum `{ Created=0, Updated=1, Deleted=2, SourceSwitched=3, Validated=4 }`. Zero external deps.

**Application layer (4 new files, all under `Explore.Application/Contracts/Secrets/`):**
- `ResolvedSecret.cs` — sealed record `(string SettingKey, string Value, SecretSourceType Source, SecretScope Scope, Guid? ScopeId, DateTimeOffset ResolvedAt)`.
- `ISecretResolver.cs` — `Task<ResolvedSecret?> ResolveAsync(string settingKey, Guid? tenantId, CancellationToken ct)` + `Task InvalidateAsync(string settingKey, SecretScope scope, Guid? scopeId, CancellationToken ct)`.
- `ISecretSource.cs` — `SecretSourceType SourceType { get; }` + `Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken ct)` + `Task<SecretValidationResult> ValidateAsync(SecretBinding binding, CancellationToken ct)`.
- `IInfisicalClientFactory.cs` — `Task<IInfisicalClient?> GetClientAsync(CancellationToken ct)` returning a factory-agnostic `IInfisicalClient` with `Task<string?> GetSecretAsync(string env, string folderPath, string secretName, CancellationToken ct)`.

**Secrets infrastructure (9 new files + 1 modified):**
- `Explore.Secrets/Sources/EnvironmentSecretSource.cs` — sealed `: ISecretSource`, `SourceType = EnvironmentVariable`. Uses `Environment.GetEnvironmentVariable(binding.EnvironmentVariableName)`.
- `Explore.Secrets/Sources/InlineSecretSource.cs` — sealed `: ISecretSource`, `SourceType = InlineEncrypted`. Uses `IDataProtectionProvider.CreateProtector(ProtectorPurpose)`. `public static readonly string[] ProtectorPurpose = ["Event.Secrets", "Binding", "v1"]`. Static `Protect(IDataProtectionProvider provider, string plaintext)` helper for handlers. `ValidateAsync` decrypts + checks non-empty.
- `Explore.Secrets/Sources/InfisicalSecretSource.cs` — sealed `: ISecretSource`, `SourceType = Infisical`. Uses `IInfisicalClientFactory`. **Never throws** on source errors (returns null). Rethrows `OperationCanceledException`.
- `Explore.Secrets/Infrastructure/InfisicalClientFactory.cs` — sealed `: IInfisicalClientFactory, IAsyncDisposable`. Thread-safe lazy init with `SemaphoreSlim`. Returns null when unconfigured (missing `ClientId`/`ClientSecret`/`ProjectId`). On auth failure: logs error, resets state, returns null. Inner `InfisicalClientFacade` adapts `IInfisicalClient` to the SDK's `ListAsync` + `SecretKey` filter pattern (SDK has no direct single-secret Get). Defensive disposal: `if (_client is IAsyncDisposable a) await a.DisposeAsync(); else if (_client is IDisposable d) d.Dispose();`.
- `Explore.Secrets/Observability/SecretResolverMetrics.cs` — OTel `Meter("Event.Secrets")` + counters (`resolve.success`, `resolve.miss`, `resolve.error`, `cache.hit`, `cache.miss`) + histogram (`resolve.duration_ms`). Tags: `source`, `has_tenant`. **Never includes secret values as tags.**
- `Explore.Secrets/Services/SecretResolver.cs` — **core no-fallback dispatcher**. Injects `ISecretBindingRepository`, `FrozenDictionary<SecretSourceType, ISecretSource>`, `IMemoryCache` (5-min TTL), `SecretResolverMetrics`. Algorithm: (1) resolve binding in Tenant→Instance hierarchy, (2) dispatch to exactly ONE source by `binding.SourceType` — NO FALLBACK, (3) cache resolved value. Cache key: `secret::{settingKey}::{scope}::{scopeId:N}` or `secret::{settingKey}::Instance::-`.
- `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs` — wraps `ISecretResolver`. Samples successful reads (every 100th via atomic counter). **NEVER logs secret values** — only key + source + scope + outcome. Always logs misses and invalidations.
- `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs` — `IHealthCheck`. Returns `HealthCheckResult.Degraded` (NOT Unhealthy) on failure — platform designed to run without external secret sources.
- `Explore.Secrets/Extensions/SecretResolutionServiceCollectionExtensions.cs` — `AddSecretResolution(IConfiguration config)` composition root:
  - `Configure<InfisicalOptions>` from `SecretProvider:Infisical` section
  - `AddSingleton<IInfisicalClientFactory, InfisicalClientFactory>`
  - `AddSingleton<ISecretSource, EnvironmentSecretSource>()`, same for Inline + Infisical (enumerable singletons)
  - `AddScoped<SecretResolver>()` (the concrete)
  - `AddScoped<ISecretResolver>(sp => new AuditingSecretResolverDecorator(sp.GetRequiredService<SecretResolver>(), ...))` — public-facing interface
  - `AddSingleton<SecretResolverMetrics>()`
  - `AddMemoryCache()`
  - `AddHealthChecks().AddCheck<SecretResolverHealthCheck>("secret-resolver", failureStatus: HealthStatus.Degraded, tags: ["secrets", "infrastructure"])`
- `Explore.Secrets/Explore.Secrets.csproj` — **MODIFIED**: dropped `net9.0` TFM (now `<TargetFramework>net10.0</TargetFramework>`), added `<ProjectReference Include="..\Explore.Application\Explore.Application.csproj" />`.

**Build verification done:** `dotnet build Explore.Secrets/Explore.Secrets.csproj --configuration Release` → 0 errors, 66 warnings (all pre-existing + 3 new CA1873 on log-template arg evaluation matching existing codebase style). LSP false positives on a few files (Blazor Components/App, Persistence IDataProtectionKeyContext, Secrets 'Application not found') verified as compiler-clean via `dotnet build`.

**Phase 3 admin surface (sub-tasks 3.7–3.21) — NOT STARTED.**

### 📋 NEXT SESSION ENTRY POINT — PICK UP HERE

**Read in this order:**
1. This file (you are here).
2. `dev/active/secrets-refactor-control-plane/phase-3-implementation-plan.md` — full 909-line execution blueprint.
3. `dev/active/secrets-refactor-control-plane/secrets-refactor-control-plane-tasks.md` — checkbox list.
4. Verify the 14 uncommitted Phase 3 runtime files still exist via `git status`.

**Remaining work (in order):**
- **3.7** DTOs + Validators → `Explore.Application/DTOs/SecretBindings/` (SecretBindingDto, SecretBindingListDto, CreateSecretBindingDto, UpdateSecretBindingDto, ValidateSecretBindingDto + FluentValidation rules enforcing `SecretDefinitionRegistry` constraints).
- **3.8** AutoMapper profile → `Explore.Application/Profiles/SecretBindingMappingProfile.cs` (SecretBinding → Dto/ListDto, look up `SecretKeyName` from registry via AfterMap).
- **3.9** CQRS Commands (Create/Update/Delete/Validate) — `IRequest<BaseCommandResponse<Guid>>, ISecureRequest` + `[AuthorizeResource("secret_binding", AuthorizationActions.X)]`. **IMPORTANT**: Repository base class `Create/Update/Delete` call `SaveChangesAsync` internally — no separate `IUnitOfWork`. Handlers publish `SecretBindingChangedNotification` (Application-layer `INotification` wrapper around Domain event — keeps Domain pure).
- **3.10** CQRS Queries — `GetSecretBindingListRequest` (paginated, HybridCache 30s), `GetSecretBindingDetailsRequest`, `GetAvailableSecretsForOnboardingRequest` (enumerates `SecretDefinitionRegistry.GetAll()`).
- **3.11** Notification handlers — `Explore.Application/Features/SecretBindings/Handlers/Notifications/InvalidateSecretCacheOnUpdatedHandler.cs` + `Explore.Application/Notifications/Secrets/SecretBindingChangedNotification.cs` wrapper.
- **3.12** `ResourceDescriptors.cs` + `ResourceKinds.cs` — add `SecretBinding = "secret_binding"`.
- **3.13** `Explore.API/Hateoas/RouteNames.cs` — add `#region Secret Binding Routes` (~7 constants).
- **3.14** `Explore.API/Controllers/SecretBindingsController.cs` — all `[Authorize]` (admin-only, Cerbos enforces). 6 endpoints.
- **3.15** HATEOAS — `Explore.API/Hateoas/Policies/SecretBindingLinkPolicy.cs` (detail + collection) + `Assemblers/SecretBindingResourceAssembler.cs`.
- **3.16** Cerbos policy `cerbos/policies/secret_binding.yaml` (check `cerbos/schemas/` folder first to decide if per-resource JSON schema needed).
- **3.17** DI wiring — add `AddSecretResolution(configuration)` to `Explore.API/Program.cs` and `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`. Also register HATEOAS link policies + assembler.
- **3.18** Tests (~40–50 new) — no-fallback dispatch, no-value-leak (regex on decorator logs), architecture tests (Domain.Secrets has no Infisical/DataProtection refs), unit + integration.
- **3.19** Full verification — build + 4 test projects (target ≥1,345 tests green, baseline was 1,305).
- **3.20** Commit — `git add` only Phase 3 files. Message: `refactor(secrets): phase 3 introduce ISecretResolver + admin bindings API`. Single commit for entire phase.
- **3.21** Update dev-docs (this file + tasks file + journal entry if notable decisions).

**Key entity reality check** (from committed Phase 1 code — verify before writing DTOs):
- `SecretBinding` primary key: `string SettingKey` (NOT `int SecretKeyId` as the plan mentions in places — the plan was written before entity finalized).
- `SecretScope` enum: `Instance = 0, Tenant = 1` (serialized as ints).
- `SecretSourceType` enum: `Infisical = 0, InlineEncrypted = 1, EnvironmentVariable = 2`.
- Factory methods on entity: `CreateInfisical/CreateInlineEncrypted/CreateEnvironmentVariable`, `SwitchToInfisical/SwitchToInlineEncrypted/SwitchToEnvironmentVariable`, `RecordValidation`.
- Entity is `IAuditableEntity` (NOT `ISoftDeletable`) → `Delete` hard-deletes.
- Registry is `SecretDefinitionRegistry` with `FrozenDictionary` of known keys + `AllowedScopes`/`AllowedSources`/`IsBootstrap` flags.

**Verbatim user directives still in force:**
- "do not delegate to subagent, just do it all yourself" (m0007 of earlier session)
- "do not care about backward compatibility at all we are in development mode"
- "No stops - push through the entire phase" → single Phase 3 commit at end
- "Follow the repo's conventions and all the industrie best practises, all the design patterns and principles, clean architecture, entreprise grade quality, highly maintainable codebase"
- One commit per phase, NO push.

### ⚠️ BLOCKERS
- **None** — clean handoff. Runtime files are on disk, ready for admin surface to be layered on top before a single Phase 3 commit.

## Quick Resume

1. Read `secrets-refactor-control-plane-plan.md` (executive summary + phases).
2. Read `secrets-refactor-control-plane-tasks.md` and find the first unchecked task.
3. Mark the task `in_progress` via `todowrite`.
4. Follow the task's file path + acceptance criteria.
5. Run `dotnet build --configuration Release --verbosity quiet`, then the relevant test project individually per CLAUDE.md.
6. On PR close, update this file (`### ✅ COMPLETED`) and the tasks file.

## Key Files - New (to be created)

### Domain layer (`Explore.Domain/`)
- `Explore.Domain/Secrets/SecretBinding.cs` - new entity (DB control plane).
- `Explore.Domain/Secrets/SecretScope.cs` - new enum `{ Instance = 1, Tenant = 2 }`.
- `Explore.Domain/Secrets/SecretSourceType.cs` - new enum `{ Infisical = 1, InlineEncrypted = 2, EnvironmentVariable = 3 }`.
- `Explore.Domain/Secrets/SecretValidationResult.cs` - new enum `{ NotValidated = 0, Success = 1, Failure = 2 }`.
- `Explore.Domain/Secrets/SecretDefinitionRegistry.cs` - static registry of every known secret + allowed scopes/sources/bootstrap flag + Infisical defaults.
- `Explore.Domain/Secrets/SecretDefinition.cs` - record describing one registry entry.
- `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs` - domain event for cache eviction + auth scheme refresh.

### Application layer (`Explore.Application/`)
- `Explore.Application/Contracts/Secrets/ISecretResolver.cs` - runtime resolver contract.
- `Explore.Application/Contracts/Secrets/IInfisicalSecretSource.cs` - per-secret Infisical contract (not bulk IConfiguration).
- `Explore.Application/Contracts/Persistence/ISecretBindingRepository.cs` - repository contract.
- `Explore.Application/DTOs/Secrets/ResolvedSecret.cs` - `(Value, SourceType, Metadata, ResolvedAt)`.
- `Explore.Application/DTOs/Secrets/SecretBindingDescriptor.cs` - UI-facing state+metadata (never the value).
- `Explore.Application/DTOs/Secrets/SecretBindingDto.cs` - API transport DTO.
- `Explore.Application/Features/Secrets/Commands/CreateSecretBinding*.cs` - Command + Handler + Validator.
- `Explore.Application/Features/Secrets/Commands/UpdateSecretBinding*.cs`.
- `Explore.Application/Features/Secrets/Commands/DeleteSecretBinding*.cs`.
- `Explore.Application/Features/Secrets/Commands/ValidateSecretBinding*.cs`.
- `Explore.Application/Features/Secrets/Queries/GetSecretBindingsQueryHandler.cs`.
- `Explore.Application/Features/Secrets/Queries/DescribeSecretBindingQueryHandler.cs`.
- `Explore.Application/Features/Secrets/Queries/GetAvailableSecretsForOnboardingQueryHandler.cs`.
- `Explore.Application/Notifications/SecretBindingCacheInvalidationHandler.cs` - `INotificationHandler<SecretBindingUpdatedEvent>`.
- `Explore.Application/Notifications/KeycloakSchemeRefreshHandler.cs` - re-registers OIDC scheme on binding update.

### Secrets / Infrastructure (`Explore.Secrets/`)
- `Explore.Secrets/Services/SecretResolver.cs` - `ISecretResolver` impl. Dispatches on source type. Per-secret `IMemoryCache` 5-min TTL.
- `Explore.Secrets/Services/InfisicalSecretSource.cs` - `IInfisicalSecretSource` impl wrapping `Infisical.Sdk`.
- `Explore.Secrets/Services/EnvironmentSecretSource.cs` - `Environment.GetEnvironmentVariable` reader.
- `Explore.Secrets/Services/InlineSecretSource.cs` - `IDataProtectionProvider.CreateProtector().Unprotect`.
- `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs` - adapted from existing `AuditingSecretProviderDecorator`, tighter read-audit strategy (audit writes always; sample reads).
- `Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` - discrete POSTGRESQL_* + SETUP_SECRET pre-DbContext loader.
- `Explore.Secrets/Bootstrap/NpgsqlConnectionStringBuilderFactory.cs` - composes `Host=;Port=;Database=;Username=;Password=;SSL Mode=Prefer;Trust Server Certificate=true`.
- `Explore.Secrets/Observability/SecretResolverMetrics.cs` - adapted from `SecretRefreshMetrics`.
- `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs` - adapted from `SecretProviderHealthCheck`.
- `Explore.Secrets/Extensions/ServiceCollectionExtensions.cs` - new `AddSecretResolution()` single entry point.

### Persistence (`Explore.Persistence/`)
- `Explore.Persistence/Repositories/SecretBindingRepository.cs` - queries by (SettingKey, Scope, ScopeId).
- `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs` - EF config + CHECK constraint + two filtered unique indexes (one per Scope).
- `Explore.Persistence/Migrations/{timestamp}_SecretBindingsAndDataProtectionKeys.cs` - destructive migration (PR 6 - combines schema changes).
- DataProtectionKeys table created via `PersistKeysToDbContext<ExploreDbContext>` EF Core key-ring default schema.

### API layer (`Explore.API/`)
- `Explore.API/Controllers/SecretBindingsController.cs` - `/api/SecretBindings` CRUD + validate endpoints.
- `Explore.API/Hateoas/Policies/SecretBindingLinkPolicy.cs` - HATEOAS links per repo convention.
- `Explore.API/Hateoas/Assemblers/SecretBindingResourceAssembler.cs`.
- Cerbos policy: `cerbos/policies/secret_binding.yaml`.

### Blazor (`Explore.Blazor.Client/`)
- `Explore.Blazor.Client/Pages/Admin/Instance/InstanceSecrets.razor` + `.razor.cs` + `.razor.css`.
- `Explore.Blazor.Client/Pages/Admin/Tenant/TenantSecrets.razor` + `.razor.cs` + `.razor.css`.
- `Explore.Blazor.Client/Pages/Admin/Components/SecretBindingCard.razor` + `.razor.cs` + `.razor.css` (shared between instance + tenant).
- `Explore.Blazor.Client/Pages/Admin/Components/SecretSourceEditDialog.razor` - modal for the three input flows.
- `Explore.Blazor.Client/Services/SecretBindingsService.cs` - BFF client.
- `Explore.Blazor/Extensions/BffSecretBindingEndpoints.cs` - Blazor server-side endpoints proxying to API if required.

### Tests
- `Explore.Secrets.UnitTests/SecretResolverTests.cs`.
- `Explore.Secrets.UnitTests/InfisicalSecretSourceTests.cs`.
- `Explore.Secrets.UnitTests/EnvironmentSecretSourceTests.cs`.
- `Explore.Secrets.UnitTests/InlineSecretSourceTests.cs`.
- `Explore.Secrets.UnitTests/BootstrapSecretLoaderTests.cs`.
- `Explore.Secrets.UnitTests/NoFallbackTests.cs` - asserts a binding with a given source type never reads from a different source.
- `Explore.Secrets.UnitTests/NoValueExposureTests.cs` - asserts descriptors and API responses never contain the plaintext or ciphertext.
- `Event.Application.UnitTests/Features/Secrets/*.cs` - handlers and validators.
- `Event.Application.UnitTests/Infrastructure/SecretResolverCacheInvalidationHandlerTests.cs`.
- `Explore.Persistence.IntegrationTests/SecretBindingRepositoryTests.cs` - filtered unique index semantics.
- `Event.API.IntegrationTests/Features/SecretBindingsControllerTests.cs`.
- `Event.API.IntegrationTests/Features/OnboardingAutoDetectTests.cs`.
- `Event.Architecture.Tests/SecretsArchitectureTests.cs` - asserts bootstrap-flagged keys ban `InlineEncrypted`, no reference to deleted types after PR 6.

## Key Files - Modified

### Consumers refactored in Phase 5
- `Explore.Infrastructure/Services/S3ConfigResolver.cs` - swap `IConfiguration` fallback for `ISecretResolver.TryResolveAsync`.
- `Explore.Infrastructure/Services/SmtpConfigResolver.cs` - swap secret reads for `ISecretResolver`.
- `Explore.Infrastructure/Services/AnalyticsConfigResolver.cs`.
- `Explore.Blazor/Services/DynamicAuthSchemeManager.cs` - read from `ISecretResolver`; subscribe to `SecretBindingUpdatedEvent`.
- `Explore.API/Extensions/AuthenticationExtensions.cs` + `Explore.Blazor/Extensions/AuthenticationExtensions.cs` - remove the compat-mapping path.

### Onboarding refactored in Phase 4
- `Explore.Application/Services/AuthProviderConfigurationService.cs` - route secrets to `SecretBinding` writes via `IMediator.Send(new UpdateSecretBindingCommand(...))` instead of `SystemSetting.Value` JSON.
- `Explore.Application/Features/InstanceOnboarding/SaveAuthProviderConfigurationCommandHandler.cs`.
- `Explore.API/Controllers/InstanceOnboardingController.cs` - remove the `/auth-provider-configuration/internal` endpoint.
- `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor` + `.razor.cs` - consume `GetAvailableSecretsForOnboardingQuery` for auto-detection chips per provider.

### Bootstrap refactored in Phase 2 — COMMITTED as `fc0b2b5a`
- `Explore.Secrets/Bootstrap/BootstrapPostgresCredentials.cs` - sealed record (ConnectionString, Source, LoadedAt).
- `Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` - static LoadPostgresConnectionString (Infisical→env→config), TryLoadInfisicalPostgresFolder, NpgsqlConnectionStringBuilder composition (SslMode=Prefer, TrustServerCertificate=true), DefaultPort=5432.
- `Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs` - 11 tests (config resolution, env resolution, missing-field errors, port defaults, mixed-source labels).
- `Explore.Persistence/PersistenceServicesRegistration.cs` - replaced hardcoded ConnectionStrings:DefaultConnection with BootstrapSecretLoader short-circuit.
- `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs` - same BootstrapSecretLoader short-circuit.
- `Explore.Persistence/ExploreDbContextFactory.cs` - fully rewritten: removed AddInfisical/POSTGRESQL_PUBLIC_URL, now uses BootstrapSecretLoader with proper error messages.
- `Explore.API/Extensions/ConfigurationExtensions.cs` - removed POSTGRESQL_PUBLIC_URL mapping + rawDbUrl variable, added architectural-invariant comment.
- `Explore.Blazor/Extensions/ConfigurationExtension.cs` - same removal + invariant comment.
- `Event.MigrationService/Extensions/ConfigurationExtensions.cs` - entirely rewritten: removed AddInfisicalMigrationCompatibility, added AddDiscretePostgresBootstrap using BootstrapSecretLoader.
- `Event.MigrationService/Program.cs` - changed AddInfisicalMigrationCompatibility() → AddDiscretePostgresBootstrap().
- `Explore.AppHost/AppHost.cs` - updated ABOUTME comment + Console.WriteLine banner.
- `docker-compose.yml` - added x-postgres-bootstrap-env anchor with discrete POSTGRESQL_HOST/PORT/DATABASE/USERNAME/PASSWORD, canonicalized x-secrets-env to SecretProvider__Infisical__* format, removed pre-built ConnectionStrings__DefaultConnection from api+blazor services.

## Key Files - Deleted (PR 6)

- `Explore.Domain/AppSetting.cs`.
- `Explore.Domain/Enums/AppSettingValueTypeEnum.cs`.
- `Explore.Persistence/Repositories/AppSettingRepository.cs`.
- `Explore.Application/Contracts/Persistence/IAppSettingRepository.cs`.
- `Explore.Persistence/Configurations/Entities/AppSettingConfiguration.cs`.
- `Explore.Secrets/Configuration/DbConfigurationSource.cs`.
- `Explore.Secrets/Configuration/DbConfigurationProvider.cs`.
- `Explore.Secrets/Configuration/InfisicalConfigurationSource.cs`.
- `Explore.Secrets/Configuration/InfisicalConfigurationProvider.cs`.
- `Explore.Secrets/Services/AesEncryptionService.cs`.
- `Explore.Secrets/Services/KeyRotationService.cs`.
- `Explore.Secrets/Services/RotationAwareHttpClientFactory.cs`.
- `Explore.Secrets/Services/RotationAwareDbContextFactory.cs`.
- `Explore.Secrets/Services/SecretRefreshService.cs`.
- `Explore.Secrets/Configuration/EncryptionOptions.cs`.
- `Explore.Secrets/Configuration/RotationOptions.cs`.
- `Explore.Secrets/Configuration/SecretRefreshOptions.cs`.
- `Explore.Secrets/Providers/InfisicalSecretProvider.cs` - replaced by per-secret `InfisicalSecretSource`.
- `Explore.Secrets/Providers/EnvironmentSecretProvider.cs` - replaced by `EnvironmentSecretSource`.
- `Explore.Secrets/Abstractions/ISecretProvider.cs` - replaced by `ISecretResolver` + source interfaces.
- `Explore.Secrets/Abstractions/SecretProviderType.cs` - `SecretSourceType` is per-binding, not global.
- `Explore.Secrets/Services/SecretProviderFactory.cs`.
- `Explore.API/Extensions/ConfigurationExtensions.cs` (`AddInfisicalCompatibility` + `ApplyCompatibilityMapping`).
- `Explore.Blazor/Extensions/ConfigurationExtensions.cs` (`AddInfisicalBlazorCompatibility`).
- `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs`.
- `Explore.Secrets.UnitTests/AesEncryptionServiceTests.cs`, `KeyRotationServiceTests.cs`, `SecretRefreshServiceTests.cs`, `SecretProviderFactoryTests.cs`, `InfisicalSecretProviderTests.cs`, `EnvironmentSecretProviderTests.cs`, `SecretRefreshMetricsTests.cs`, `SecretProviderOptionsValidatorTests.cs`.

## Key Files - Kept (Adapted)

- `Explore.Infrastructure/Services/SetupSecretProvider.cs` - bootstrap only; stays outside `SecretBinding`.
- `Explore.Secrets/Abstractions/SecretValue.cs` - reused by `ResolvedSecret` or replaced cleanly.
- `Explore.Secrets/Observability/SecretRefreshMetrics.cs` → renamed to `SecretResolverMetrics.cs`.
- `Explore.Secrets/Observability/SecretProviderHealthCheck.cs` → renamed to `SecretResolverHealthCheck.cs`.
- `Explore.Secrets/Validation/RequiredSecretsValidator.cs` - adapted to use `SecretDefinitionRegistry`.
- `Explore.Secrets.UnitTests/AuditingSecretProviderDecoratorTests.cs` → adapted to `AuditingSecretResolverDecoratorTests.cs`.
- `Infisical.Sdk` NuGet - kept.

## Architectural Decisions (ADRs summary)

1. **DB = control plane, Infisical/env/inline = data planes** - the single binding row says WHERE to fetch; there is no fallback chain.
2. **Normalized metadata columns, not polymorphic JSON** - DB integrity, simpler EF mappings, DB-level CHECK enforces exactly-one metadata group per source type.
3. **Drop `Inherited` source type** - absence of a binding at the child scope means inherit; computed flag in the descriptor DTO represents "inherited" in the UI.
4. **Drop `Module` scope for v1** - modules today are tenant capabilities; future module secrets use tenant-scoped namespaced keys.
5. **Bootstrap split** - Postgres + setup secret use a separate `BootstrapSecretLoader`; they never traverse `ISecretResolver`.
6. **Registry-driven invariants** - `SecretDefinitionRegistry` is the allowlist; unknown keys are rejected, bootstrap keys ban `InlineEncrypted`.
7. **Data Protection via EF keyring** - `PersistKeysToDbContext<ExploreDbContext>`; keeps minimal deployment simple. Disaster recovery implication documented.
8. **Validate-and-discard** - validation fetches once, records success/failure + timestamp/user, and never snapshots the value in DB.
9. **Audit strategy** - audit all writes/deletes/validation; sample reads to avoid log floods and secondary leak risk.
10. **Webhook integration deferred** - cache has explicit `InvalidateAsync` hook; a future Infisical webhook endpoint can plug in without foundational changes.
11. **Filtered unique indexes for Postgres null semantics** - separate partial indexes for `Scope=Instance` and `Scope=Tenant` to avoid duplicate Instance rows.
12. **Keycloak scheme refresh is explicit** - `SecretBindingUpdatedEvent` → `KeycloakSchemeRefreshHandler` → `DynamicAuthSchemeManager.RefreshSchemeAsync`.

## Open Questions (tracked for PR reviews)

1. Should the admin UI expose a "Copy Infisical path from registry default" button for convenience? (Probably yes; low cost.)
2. For tenant-scoped bindings, does the binding descriptor expose the instance fallback's metadata for transparency? (Default: yes, because operators debug across scopes; gate behind `tenant:read_instance_secret_metadata` action in Cerbos.)
3. Does the setup secret UI get migrated into the new `/Admin/Secrets` page, or stay in the setup/onboarding flow only? (Phase 4 decides; leaning: stays in setup flow but mirrors the card design for consistency.)
4. When the DB migration drops `AppSettings`, do we also drop the 31-row CHECK constraint reference in `AppSettingConfiguration`? (Trivially yes - the whole file is deleted.)

## Reference Docs To Open When Working On Specific Tasks

- `docs/ARCHITECTURE.md` - Clean Architecture enforcement.
- `docs/SECURITY.md` - BFF + multi-client audience validation + Cerbos.
- `docs/CONFIGURATION.md` - current static settings + compat mapping patterns being removed.
- `docs/CODEBASE_STRUCTURE.md` - project-layer boundaries.
- `docs/QUICK_REFERENCE.md` - critical rules (repository returns entities, auditing, soft-delete, command response shapes).
- `docs/SECRETS.md` - will be rewritten at end of Phase 6.
- `.claude/skills/auth-patterns/SKILL.md` - OIDC + BFF security.
- `.claude/skills/blazor-bff-patterns/SKILL.md` - BFF endpoint patterns.
- `.claude/skills/clean-architecture-rules/SKILL.md` - layer dependency rules.
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md` - command/query shapes.
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md` - EF configuration + migrations.
- `.claude/skills/error-tracking/SKILL.md` - observability integration.
- `.claude/skills/blazor-ui-conventions/SKILL.md` - MudBlazor + BEM.
- `.claude/skills/blazor-css-isolation/SKILL.md` - `.razor.css` scoping rules.
- `.claude/skills/design-system/SKILL.md` - 3-tier design tokens for the Secrets page.
- `.claude/skills/accessibility/SKILL.md` - a11y checks for the confirm dialogs.
- `.claude/skills/agentic-research/SKILL.md` - for any follow-up investigation.
- `.claude/skills/gitkraken-cli/SKILL.md` + `.claude/skills/conventional-commit/SKILL.md` - commit / PR conventions.

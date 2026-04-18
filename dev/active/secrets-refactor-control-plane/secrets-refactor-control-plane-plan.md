ABOUTME: Strategic plan to refactor secrets architecture so the database is the pure control plane (metadata + routing) and Infisical/env/inline are isolated data planes with exactly one active source per secret per scope.
ABOUTME: Eliminates the current Infisical → env → AppSetting fallback chain and replaces it with a registry-driven SecretBinding model, discrete Postgres bootstrap, and a UI contract that never exposes secret values.

# Plan: Secrets Refactor - Control Plane / Data Plane Separation

Last Updated: 2026-04-18

## Executive Summary

The current secrets implementation (under `Explore.Secrets/`) couples provider lookup, inline DB encryption, and `IConfiguration` overlays into a single global fallback chain (Infisical → env/appsettings → AppSetting). That chain is exactly the ambiguity the user wants removed. Any given secret has no single owner; operators cannot tell what is live; and source precedence is a global runtime setting instead of a per-secret decision.

This refactor treats **the database as the control plane** and **Infisical / environment variables / inline-encrypted storage as the data plane**:

1. For each secret-backed setting, a `SecretBinding` row in Postgres says WHERE the value comes from (source type + normalized metadata). No binding = inherits from parent scope (or absent).
2. A single `ISecretResolver` dispatches on the binding's source type and fetches from **exactly one** source. There is no fallback chain and no "DB first then Infisical" conflict logic.
3. The Infisical layout is redesigned to the user's clean, folder-per-concern structure (`api/`, `storage/`, `keycloak/`, `postgresql/`, `smtp/`, `analytics/`, `ai/`).
4. Postgres boot secrets become **five discrete fields** (`POSTGRESQL_HOST|PORT|DATABASE|USERNAME|PASSWORD`), composed in code via `NpgsqlConnectionStringBuilder`. The legacy `POSTGRESQL_PUBLIC_URL` is deleted.
5. The UI never renders secret values. It renders **state + metadata**: configured/not configured, which source, source-specific metadata (env+path+key, variable name), last validation result, last updated by/when.
6. Missing secrets never crash the platform. A minimal deployment (API + Blazor + Postgres) works; every other feature degrades gracefully until its secrets are configured via UI or Infisical.
7. The onboarding flow reads "what resolves" to auto-select providers (e.g. Keycloak auto-detected when its secrets resolve) and exposes explicit input flows for the three source types (reference Infisical, store inline-encrypted, point at env variable).

Oracle review confirmed the direction and added four key adjustments that are incorporated into this plan: (a) **split bootstrap secrets from runtime bindings** (Postgres password cannot live in the same DB it unlocks); (b) use a **central `SecretDefinitionRegistry`** as the policy source-of-truth; (c) use **normalized metadata columns with DB check constraints** instead of polymorphic JSON; (d) **drop `Module` scope and `Inherited` source type from v1** (absence = inheritance; modules use tenant-scoped namespaced keys).

Delivery is a six-PR sequence (Foundations → Bootstrap split → Resolver + admin API → Onboarding + auth → Consumer migration → Cleanup + docs). No backward compatibility is preserved; we are in development mode. The destructive EF migration drops `AppSettings` and related infrastructure as part of PR 6.

## Current State Analysis

### Verified Existing Architecture (the anti-pattern to eliminate)

- `Explore.Secrets/Abstractions/ISecretProvider.cs` defines the single global provider abstraction with `SecretProviderType` enum (`None`, `Infisical`, `Vault`, `AzureKeyVault`, `AwsSecretsManager`). Only `None`, `Infisical`, and `Environment` are implemented today.
- `Explore.Secrets/Configuration/InfisicalConfigurationSource.cs` + `InfisicalConfigurationProvider.cs` register Infisical values as an `IConfiguration` overlay. Paths are hardcoded in `Explore.API/Extensions/ConfigurationExtensions.cs` as `["/keycloak", "/postgresql", "/api", "/blazor"]` and name conversion is `SCREAMING_SNAKE_CASE ↔ .NET:Colon:Sections`.
- `Explore.Secrets/Configuration/DbConfigurationSource.cs` + `DbConfigurationProvider.cs` load the `AppSetting` table and decrypt values via `AesEncryptionService` (AES-256-GCM, base64(nonce[12] + tag[16] + ciphertext), `KeyVersion` tracked per row, rotation via `KeyRotationService`).
- `Explore.Secrets/Services/SecretRefreshService.cs` is a hosted background service that polls Infisical on an interval and updates the provider cache. Exponential backoff driven by `SecretRefreshOptions`.
- `Explore.API/Extensions/ConfigurationExtensions.cs` (`AddInfisicalCompatibility` + `ApplyCompatibilityMapping`) maps legacy Infisical names (`POSTGRESQL_PUBLIC_URL`, `ISLAMU_EVENT_S3_*`, `KEYCLOAK_PUBLIC_URL`, etc.) to canonical keys (`ConnectionStrings:DefaultConnection`, `Keycloak:*`, `S3Settings:*`). This file plus its Blazor sibling (`Explore.Blazor/Extensions/ConfigurationExtensions.cs`) embodies the legacy naming that will be deleted.
- Effective resolution precedence today is: **Infisical overlay → `IConfiguration` (env vars + appsettings + user secrets) → `AppSetting` via `DbConfigurationProvider`**. This is global - no per-secret opt-out. This is what the user rejects.

### Verified Settings Entities (three tables exist today)

- `Explore.Domain/AppSetting.cs` (PK `ConfigKey`, `EncryptedValue`, `KeyVersion`, `EncryptedAt`, `EncryptedBy`, `IsSensitive`, `Description`, `Category`, `ValueType`). The corresponding EF configuration `Explore.Persistence/Configurations/Entities/AppSettingConfiguration.cs` enforces a CHECK constraint blocking `Database:*`, `Security:MasterKey*`, and `ConnectionStrings:*` keys. The matching repository is `Explore.Persistence/Repositories/AppSettingRepository.cs`.
- `Explore.Domain/SystemSetting.cs` (governance key/value, JSON serialized). Used for instance-scope governance and today for auth provider secrets (anti-pattern - plain JSON, no app-layer encryption). Repository: `Explore.Persistence/Repositories/SystemSettingRepository.cs`.
- `Explore.Domain/TenantSetting.cs` (tenant override for governance keys). Repository: `Explore.Persistence/Repositories/TenantSettingRepository.cs`. **Separate** entity: `Explore.Domain/TenantSettings.cs` (plural) is not related to secrets.
- `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs` defines the "logical secret key" namespace that currently leaks secret values into `SystemSetting.Value` JSON: `email.smtp_username`, `email.smtp_password`, `s3.access_key_id`, `s3.secret_access_key`, `cerbos.custom_admin_username`, `cerbos.custom_admin_password`, `auth.keycloak_client_secret`, `auth.google_client_secret`.
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` contains non-secret governance keys (branding, auth feature flags like `auth.keycloak_enabled`, analytics `analytics.*`, events, etc.) - these stay put.

### Verified Consumers And Their Current Resolution Shapes

- **SETUP_SECRET** (`Explore.Infrastructure/Services/SetupSecretProvider.cs`): reads `configuration["SETUP_SECRET"]` env var; if missing auto-generates a 32-char crypto token valid for 60 minutes and logs it at API startup. Registered as singleton `ISetupSecretProvider`. Timing-safe compare. **Bootstrap-only** by design - stays outside `SecretBinding`.
- **STORAGE_S3_\*** (`Explore.Infrastructure/Services/S3ConfigResolver.cs`): reads discrete keys via `IHierarchicalSettingsResolver` falling back to `IConfiguration["S3Settings:*"]`. Scoped, 5-min cache. Null → S3 features disable cleanly.
- **KEYCLOAK_\*** (`Explore.Blazor/Extensions/AuthenticationExtensions.cs` + `Explore.API/Extensions/AuthenticationExtensions.cs`): reads `Keycloak:Authority`, `Keycloak:MetadataAddress`, `Keycloak:ClientId`, `Keycloak:ClientSecret`, `Keycloak:Realm`, `Keycloak:RequireHttpsMetadata`. Dynamic scheme registration via `Explore.Blazor/Services/DynamicAuthSchemeManager`. Startup-only today; runtime updates must explicitly re-register schemes.
- **POSTGRESQL** (`Explore.Persistence/PersistenceServicesRegistration.cs` line 31): **single URL string** today (`ConnectionStrings:DefaultConnection`). `AddPooledDbContextFactory<ExploreDbContext>().UseNpgsql(connStr)`. Startup-only. Must be refactored to discrete fields composed via `NpgsqlConnectionStringBuilder`.
- **SMTP_\*** (`Explore.Infrastructure/Services/SmtpConfigResolver.cs` + `SmtpEmailService.cs`): reads governance keys (`email.smtp_host`, `email.smtp_port`, `email.smtp_security`, `email.smtp_from_address`, `email.smtp_from_name`, etc.) via `IHierarchicalSettingsResolver`; reads secret keys (`email.smtp_username`, `email.smtp_password`) via the same resolver that currently reads `SystemSetting.Value` JSON. Scoped, 5-min cache. Host empty → email disables cleanly.
- **ANALYTICS_POSTHOG_\*** (`Explore.Infrastructure/Services/AnalyticsConfigResolver.cs` + `PostHogAnalyticsProvider.cs`): reads `analytics.api_key`, `analytics.endpoint_url`, `analytics.provider`, `analytics.is_enabled`, optionally `analytics.personal_api_key`. Per-tenant, scoped. Fire-and-forget false if unavailable.
- **AI_OPENAI_API_KEY / AI_ANTHROPIC_API_KEY**: **no consumer exists today**. User wants the Infisical folder layout prepared for future work; no refactor of nonexistent code required.
- **Infisical bootstrap itself**: `Explore.Secrets` reads `Infisical:Url`, `Infisical:ProjectId`, `Infisical:ClientId`, `Infisical:ClientSecret`, `Infisical:Environment`, `Infisical:Paths:0..n` from user secrets or environment. These remain bootstrap-level (they wire up the Infisical client for the resolver).

### Verified Onboarding Flow

- `Explore.Blazor.Client/Pages/Setup.razor` validates the setup token, persists via BFF JS interop (`persistSetupSecret`), detects providers, routes to `/onboarding/instance` or a provider login.
- `Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor` is the routing brain (incomplete → setup, otherwise auth/instance/settings or events).
- `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor` enables/configures Keycloak (OIDC), ATProto Login, Google SSO. It already exposes a `KeycloakDetectedFromEnvironment` flag that disables the toggle and shows an "Auto-detected" chip; we extend this concept to every secret-backed provider.
- `Explore.Application/Features/InstanceOnboarding/` hosts `SaveAuthProviderConfigurationCommand`, `CompleteInstanceOnboardingCommand`, `GetAuthProviderConfigurationQuery`, and update commands (all `BaseCommandResponse<Guid>`).
- `Explore.API/Controllers/InstanceOnboardingController.cs` exposes `GET /api/InstanceOnboarding/status`, `POST /api/InstanceOnboarding/validate-secret`, `POST /api/InstanceOnboarding/complete`, `PUT /api/InstanceOnboarding/auth-provider-configuration`, and the **BFF-internal** `GET /api/InstanceOnboarding/auth-provider-configuration/internal` (latter returns secret values for the BFF; must be removed in favor of resolver-based runtime reads).
- `Explore.Application/Services/AuthProviderConfigurationService.cs` currently writes secrets into `SystemSetting.Value` as plain JSON strings (anti-pattern). `IsLocked=true` prevents tenant override. Category `"Authentication"`.
- `Explore.Infrastructure/Services/ModuleService.cs` tracks module enablement (`ModuleDefinition` + `TenantCapability` + `IslamicAspectFilter` + `TechAspectFilter`). Not secret-coupled; no change required.

### Verified Tests

- `Explore.Secrets.UnitTests/` covers `SecretProviderFactoryTests`, `SecretRefreshServiceTests`, `AesEncryptionServiceTests`, `KeyRotationServiceTests`, `InfisicalSecretProviderTests`, `EnvironmentSecretProviderTests`, `AuditingSecretProviderDecoratorTests`, `SecretRefreshMetricsTests`, `SecretProviderHealthCheckTests`, `SecretProviderOptionsValidatorTests`. Many of these will be deleted in PR 6.
- `Event.Application.UnitTests/Infrastructure/` contains `SetupSecretProviderTests`, `SmtpConfigResolverTests`, `S3ConfigResolverTests` - these need rewrites to target the new resolver.
- `Event.API.IntegrationTests/Features/` contains `SetupSecretFlowTests` and `InstanceOnboardingControllerTests` - extended with SecretBinding CRUD and no-fallback / no-leak tests in PR 3.

### Confirmed Gaps

- No `SecretBinding` entity, table, or repository exists.
- No `SecretDefinitionRegistry` exists; allowed scopes/source types per secret are implicit and scattered across consumer resolvers.
- No resolver API surfaces per-secret metadata (state, source, last validation) for the UI.
- No discrete Postgres bootstrap path; today's flow relies on the URL-form connection string.
- No Data Protection-based inline encryption (today's inline encryption is the legacy `AesEncryptionService` tied to `AppSetting`).
- No Infisical cache invalidation hook (only polling via `SecretRefreshService`).
- No validation endpoint that proves a binding actually resolves without exposing the fetched value.

## Proposed Future State

### 1. `SecretDefinitionRegistry` (policy source-of-truth)

A code-defined registry in `Explore.Domain/Secrets/SecretDefinitionRegistry.cs` where every secret-backed setting key declares:

- `SettingKey` - canonical key (e.g. `smtp.password`, `storage.s3.secret_access_key`, `auth.keycloak.client_secret`, `analytics.posthog.api_key`, `ai.openai.api_key`, `ai.anthropic.api_key`).
- `AllowedScopes` - `{Instance}` or `{Instance, Tenant}` per user.
- `AllowedSourceTypes` - subset of `{Infisical, InlineEncrypted, EnvironmentVariable}` (Postgres bootstrap secrets ban `InlineEncrypted` by invariant).
- `IsBootstrap` - true for Postgres-connection + setup secret; they never go through the runtime resolver.
- `InfisicalDefaults` - `{ Folder, SecretName }` for the user's layout (e.g. `smtp.password` → `{ Folder="smtp", SecretName="SMTP_PASSWORD" }`).
- `EnvironmentVariableDefault` - canonical env var name for the "point at env var" UI flow default (e.g. `SMTP_PASSWORD`).
- `ValidationKind` - enum to drive the `POST /validate` contract (e.g. `StringNonEmpty`, `UrlReachable`, `Smtp Handshake`).

The registry is the **one place** that lists every secret the system knows about. Anything not in the registry is rejected at binding write time.

### 2. `SecretBinding` entity (DB control plane)

New entity at `Explore.Domain/Secrets/SecretBinding.cs`:

- `Id` (Guid v7)
- `SettingKey` (string, max 256) - references an entry in the `SecretDefinitionRegistry`.
- `Scope` (`SecretScope` enum: `Instance` = 1, `Tenant` = 2).
- `ScopeId` (`Guid?`) - null for Instance; tenant id for Tenant.
- `SourceType` (`SecretSourceType` enum: `Infisical` = 1, `InlineEncrypted` = 2, `EnvironmentVariable` = 3). **No `Inherited`.**
- Normalized metadata columns (Oracle recommendation - not polymorphic JSON):
  - `InfisicalEnvironment` (string?)
  - `InfisicalPath` (string?)
  - `InfisicalKey` (string?)
  - `EnvironmentVariableName` (string?)
  - `InlineCiphertext` (string?) - base64, encrypted via `IDataProtectionProvider`.
  - `InlineCiphertextVersion` (string?) - purpose-string version for key rotation ergonomics.
- `IsLocked` (bool) - instance binding with `IsLocked=true` prevents tenant override.
- `LastValidationResult` (`SecretValidationResult` enum: `NotValidated`, `Success`, `Failure`).
- `LastValidationMessage` (string?, max 512) - generic/phrased to avoid info leakage.
- `LastValidatedAt` (DateTime?), `LastValidatedBy` (Guid?).
- `IAuditable` fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `RowVersion`.
- **Filtered unique index** (Postgres-specific): `(SettingKey, Scope, ScopeId)` filtered on `IsDeleted = false`, with `ScopeId IS NULL` treated as a concrete value via `COALESCE` on a sentinel GUID or a separate partial index for Instance scope (handles Postgres NULL unique semantics - Oracle risk #1).
- **CHECK constraint** enforcing exactly one metadata group populated per `SourceType`.

### 3. `ISecretResolver` (single runtime contract)

New contract in `Explore.Application/Contracts/Secrets/ISecretResolver.cs`:

```csharp
public interface ISecretResolver
{
    Task<ResolvedSecret?> TryResolveAsync(string settingKey, Guid? tenantId, CancellationToken ct);
    Task<ResolvedSecret> ResolveRequiredAsync(string settingKey, Guid? tenantId, CancellationToken ct);
    Task<SecretBindingDescriptor> DescribeAsync(string settingKey, Guid? tenantId, CancellationToken ct);
    Task<IReadOnlyList<SecretBindingDescriptor>> DescribeAllAsync(Guid? tenantId, CancellationToken ct);
    Task InvalidateAsync(string settingKey, Guid? tenantId, CancellationToken ct);
}
```

- `TryResolveAsync` looks up the winning binding (tenant override → instance → absent), dispatches on `SourceType`, fetches from **exactly one** source, returns `ResolvedSecret(Value, SourceType, Metadata, ResolvedAt)`. **No fallback chain.**
- `DescribeAsync` returns state + metadata only (never the value): `{ SettingKey, IsConfigured, IsInherited, ResolvedScope, SourceType, PublicMetadata (env/path/key or var name or "stored in platform"), LastValidation, LastValidatedAt, LastValidatedBy }`.
- `InvalidateAsync` explicit cache eviction hook, prepares for future webhook integration.

Implementation: `Explore.Secrets/Services/SecretResolver.cs` wires Infisical (`IInfisicalSecretSource`), env var (`Environment.GetEnvironmentVariable`), and inline (`IDataProtectionProvider.CreateProtector(...).Unprotect`). Per-secret `IMemoryCache` entry with 5-minute TTL keyed on `(settingKey, tenantId, source fingerprint)`.

### 4. Infisical layout (replaces legacy)

Per user spec - code maps every `SecretDefinitionRegistry` entry to its `(Folder, SecretName)`:

- `api/SETUP_SECRET` (bootstrap only)
- `storage/STORAGE_S3_{ENDPOINT, PUBLIC_ENDPOINT, BUCKET_NAME, ACCESS_KEY_ID, SECRET_ACCESS_KEY, REGION}`
- `keycloak/KEYCLOAK_{REALM, CLIENT_ID, CLIENT_SECRET, ADMIN_USERNAME, ADMIN_PASSWORD, DB_PASSWORD}`
- `postgresql/POSTGRESQL_{HOST, PORT, DATABASE, USERNAME, PASSWORD}` (bootstrap only)
- `smtp/SMTP_{HOST, PORT, USERNAME, PASSWORD, FROM_ADDRESS, FROM_NAME}`
- `analytics/ANALYTICS_POSTHOG_{PUBLIC_KEY, HOST}`
- `ai/AI_{OPENAI_API_KEY, ANTHROPIC_API_KEY}`

The legacy names (`POSTGRESQL_PUBLIC_URL`, `ISLAMU_EVENT_*`, `EXPLORE_BLAZOR_SERVER_CLIENT_SECRET`, `KEYCLOAK_PUBLIC_URL`/`KEYCLOAK_BASE_URL`) are deleted from config mapping and `docker-compose.yml`.

### 5. Bootstrap Secret Loader

New `Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` is the **only** path Postgres bootstrap takes:

1. If Infisical bootstrap config is present (`Infisical:ClientId` + `ClientSecret` + `ProjectId`), attempt Infisical `postgresql/POSTGRESQL_*` reads first.
2. Otherwise fall back to environment variables (`POSTGRESQL_HOST`, etc.) then `appsettings.json` sections.
3. Compose via `NpgsqlConnectionStringBuilder { Host, Port, Database, Username, Password, SslMode = SslMode.Prefer, TrustServerCertificate = true }.ConnectionString`.
4. Refuses to start if any of the five fields is missing - logs which fields are missing and which source was attempted.

The setup secret follows the same bootstrap pattern: Infisical → env → auto-generate (existing behavior preserved).

### 6. Inline encryption via `IDataProtectionProvider`

- Persist keys via `PersistKeysToDbContext<ExploreDbContext>` (EF Core key ring) so the minimal deployment (API + Blazor + Postgres) works without additional infrastructure.
- Purpose string hierarchy: `("Event.Secrets", "Binding", "v1")` plus scope chain (`Instance` or `Tenant:{id}`).
- `InlineCiphertextVersion` column captures the purpose version so future rotation is possible.
- **Disaster recovery note documented**: DP keys in the same DB = protection against app-layer disclosure, not full DB compromise. Backups must include both ciphertext and keys.

### 7. UI Contract

Admin UI (under `Admin/Instance/Secrets` and `Admin/Tenant/Secrets`) lists every secret from `SecretDefinitionRegistry`. Each card shows:

- **State**: `Configured` / `Not configured` / `Inherited from instance` (for tenant scope).
- **Source**: `Infisical` / `Platform DB` / `Environment variable`.
- **Source-specific metadata**:
  - Infisical → environment, path, key.
  - Environment variable → variable name + "found"/"missing" validation.
  - Platform DB → no additional metadata (never the value).
- **Last validation result** + timestamp + user.
- **Last updated at / by**.

Input flows per source type (explicit; no auto-migration between sources):

- **Reference Infisical**: user enters (or confirms registry default) environment + path + key. App performs `validate-binding` handshake - fetches once, confirms success, discards value.
- **Store inline-encrypted**: user types the secret once; `IDataProtector.Protect(plaintext)` runs; ciphertext is written to `InlineCiphertext`; the plaintext is discarded. Existing value is never re-displayed after save.
- **Point at environment variable**: user enters variable name (or confirms registry default). App validates env var is present (but does not read its value into the response).

Source switching is explicit: switching from Infisical to Inline, for example, invalidates the cache entry and replaces the binding entirely.

### 8. Onboarding integration

- `IAuthProviderConfigurationService` is refactored to drive off `SecretBinding`s (`auth.keycloak.client_secret`, `auth.google.client_secret`) instead of writing secrets into `SystemSetting.Value`. The non-secret enable/disable flags (`auth.keycloak_enabled`, etc.) stay in `SystemSetting` as today.
- New query `GetAvailableSecretsQueryHandler` returns `DescribeAllAsync()` filtered to the keys the onboarding screen cares about (Keycloak, SMTP, S3, PostHog, AI). The UI shows an "Auto-detected" chip per provider whose secrets already resolve (mirroring today's `KeycloakDetectedFromEnvironment` pattern).
- The `GET /api/InstanceOnboarding/auth-provider-configuration/internal` endpoint that currently returns secret values to the BFF is **removed**. The BFF reads via `ISecretResolver.TryResolveAsync` directly.

### 9. Consumer migration

All runtime consumers shift to `ISecretResolver.TryResolveAsync(settingKey, tenantId, ct)`:

- `S3ConfigResolver` → resolves `storage.s3.*` bindings.
- `SmtpConfigResolver` → resolves `smtp.*` bindings (governance host/port/security keys stay in `IHierarchicalSettingsResolver`; only `smtp.username` / `smtp.password` shift to the binding resolver).
- `AnalyticsConfigResolver` → resolves `analytics.posthog.api_key` / `analytics.posthog.personal_api_key`.
- `DynamicAuthSchemeManager` → resolves `auth.keycloak.client_secret` and `auth.google.client_secret`. Scheme registration is re-run explicitly when a binding update occurs (via domain event `SecretBindingUpdatedEvent` → `Explore.Application/Notifications/KeycloakSchemeRefreshHandler`).
- Every consumer preserves its existing graceful-degradation pattern: `TryResolveAsync` returning `null` disables the feature (as today with SmtpEmailService and the analytics provider).

### 10. Deletions (no backward compatibility - dev mode)

PR 6 deletes:
- `Explore.Domain/AppSetting.cs` + its EF config + repo + interface.
- `Explore.Secrets/Configuration/{DbConfigurationSource, DbConfigurationProvider, InfisicalConfigurationSource, InfisicalConfigurationProvider}.cs`.
- `Explore.Secrets/Services/{AesEncryptionService, KeyRotationService, RotationAwareHttpClientFactory, RotationAwareDbContextFactory, SecretRefreshService}.cs`.
- `Explore.Secrets/Configuration/{EncryptionOptions, RotationOptions, SecretRefreshOptions}.cs`.
- `Explore.API/Extensions/ConfigurationExtensions.cs` (`AddInfisicalCompatibility` + `ApplyCompatibilityMapping`).
- `Explore.Blazor/Extensions/ConfigurationExtensions.cs` (`AddInfisicalBlazorCompatibility`).
- `Explore.Domain/Constants/InfrastructureSecretSettingKeys.cs` (replaced by `SecretDefinitionRegistry` canonical keys).
- Legacy tests in `Explore.Secrets.UnitTests/` that target deleted classes.
- Legacy Infisical name references in `docker-compose.yml` and `appsettings.*.json`.

Kept (adapted):
- `Infisical.Sdk` integration, wrapped in `IInfisicalSecretSource` per-secret contract.
- `AuditingSecretProviderDecorator` - adapted into `AuditingSecretResolverDecorator` with tightened read-audit strategy.
- `SecretRefreshMetrics` - adapted to the new resolver (counters + histograms).
- `SecretProviderHealthCheck` - adapted to the new resolver (Infisical reachability).
- `ISetupSecretProvider` (bootstrap-only; stays outside `SecretBinding`).

### 11. Destructive EF Migration

PR 6 includes a single destructive migration:
- Drops `AppSettings` table and all indexes.
- Adds `SecretBindings` table (all columns + indexes + CHECK + filtered unique).
- Adds `DataProtectionKeys` table (standard `PersistKeysToDbContext<T>` schema).
- Drops secret-holding `SystemSetting` rows for `InfrastructureSecretSettingKeys.*` (in a seed script, migrate semantics not data).

No data migration. Dev mode.

## Implementation Phases

### Phase 1 — Foundations (PR 1)

**Goal**: introduce `SecretDefinitionRegistry`, `SecretBinding` entity, schema, repository, and Data Protection plumbing. No consumer cutover, no resolver yet.

**Dependencies**: none.

### Phase 2 — Bootstrap Split (PR 2)

**Goal**: introduce `BootstrapSecretLoader` for discrete Postgres secrets + setup secret. Remove legacy URL connection string path.

**Dependencies**: PR 1 (no hard coupling - can parallelize if needed).

### Phase 3 — Resolver + Admin API (PR 3)

**Goal**: `ISecretResolver` implementation, per-source Infisical/env/inline sources, admin CQRS (`CreateSecretBindingCommand`, `UpdateSecretBindingCommand`, `DeleteSecretBindingCommand`, `ValidateSecretBindingCommand`, `GetSecretBindingsQuery`, `DescribeSecretBindingQuery`), controller with rate limiting + audit.

**Dependencies**: PR 1.

### Phase 4 — Onboarding + Auth (PR 4)

**Goal**: move Keycloak/Google/ATProto secrets from `SystemSetting` JSON onto `SecretBinding`; explicit Keycloak scheme refresh on binding update; remove `/auth-provider-configuration/internal` endpoint.

**Dependencies**: PR 3.

### Phase 5 — Consumer Migration (PR 5)

**Goal**: refactor `SmtpConfigResolver`, `S3ConfigResolver`, `AnalyticsConfigResolver` to call `ISecretResolver.TryResolveAsync`. Remove `InfrastructureSecretSettingKeys` writes.

**Dependencies**: PR 3.

### Phase 6 — Deletion + Docs (PR 6)

**Goal**: delete legacy configuration providers, refresh/rotation services, AES/key-rotation code, compatibility mappings, obsolete tests. Rewrite `docs/SECRETS.md` + update `docs/CONFIGURATION.md`. Destructive migration.

**Dependencies**: PRs 1–5.

## Detailed Tasks

See `secrets-refactor-control-plane-tasks.md` for the full per-task checklist with acceptance criteria, effort, and skill references. High-level effort map:

- Phase 1: ~8 tasks, **M–L** total.
- Phase 2: ~5 tasks, **M** total.
- Phase 3: ~12 tasks, **L–XL** total.
- Phase 4: ~7 tasks, **M–L** total.
- Phase 5: ~6 tasks, **M** total.
- Phase 6: ~8 tasks, **M** total.

Total effort: **XL** (roughly 3–5 engineering days for a focused senior working alone; longer under TDD and multi-PR reviews).

## Risk Assessment And Mitigation Strategies

1. **Postgres NULL unique-index semantics** - a plain `UNIQUE (SettingKey, Scope, ScopeId)` index allows duplicate Instance rows in Postgres because `NULL != NULL`. **Mitigation**: two partial indexes - `UNIQUE (SettingKey) WHERE Scope = 'Instance'` and `UNIQUE (SettingKey, ScopeId) WHERE Scope = 'Tenant' AND IsDeleted = false`. Verified with `Explore.Persistence.IntegrationTests`.
2. **DP key ring disaster recovery** - if the DB is restored but DP keys are lost (or vice versa), all inline-encrypted secrets are permanently unreadable. **Mitigation**: `docs/SECRETS.md` explicitly documents that `DataProtectionKeys` must be part of every backup; add an integration test that round-trips a ciphertext across a simulated `DbContext` recreation.
3. **Bootstrap / runtime boundary drift** - a future developer could accidentally allow `postgresql.password` to be stored as `InlineEncrypted` (impossible - it unlocks the DB containing its own ciphertext). **Mitigation**: `SecretDefinitionRegistry` enforces `AllowedSourceTypes` at binding write time; an architecture test in `Event.Architecture.Tests` asserts that no bootstrap-flagged key has `InlineEncrypted` in its `AllowedSourceTypes`.
4. **Stale cache after source switch** - changing a binding from Infisical to Inline must evict the old cache entry immediately or consumers briefly serve the old source. **Mitigation**: `UpdateSecretBindingCommandHandler` raises `SecretBindingUpdatedEvent`; `SecretResolverCacheInvalidationHandler` calls `InvalidateAsync` synchronously before returning the command response.
5. **Validation endpoint information leakage** - "Env var missing" vs "Infisical path wrong" can become a discovery oracle. **Mitigation**: generic validation messages (`Binding configured` / `Binding could not resolve`); detailed diagnostics only in server logs gated behind audit; rate-limit `POST /validate` per IP + per user (reuse `write` + `setup_secret` policies).
6. **Source-type switching UX confusion** - admins may think switching from Inline to Infisical "layers" them. **Mitigation**: UI explicitly shows "Switching source will replace the current binding. Inline-encrypted values are write-only and cannot be recovered after switch." in a confirm dialog. A11y-compliant MudDialog.
7. **Keycloak scheme refresh timing** - a Keycloak client-secret binding update must explicitly trigger `DynamicAuthSchemeManager.RefreshSchemeAsync`; otherwise the already-built OIDC handler keeps the stale secret. **Mitigation**: `SecretBindingUpdatedEvent` subscription in `KeycloakSchemeRefreshHandler` with tests.
8. **PostHog analytics silent-fail masking outage** - if analytics bindings are misconfigured, fire-and-forget false returns hide the problem. **Mitigation**: validation state is surfaced on the binding's admin card and emitted as an OpenTelemetry metric; health check degrades the `secrets` tag if validation is `Failure` for more than an hour.
9. **Setup-secret edge case** - setup secret is bootstrap but its UI needs binding-like state (auto-generated vs env vs Infisical). **Mitigation**: `ISetupSecretProvider` stays outside `SecretBinding`; its state is rendered via a dedicated admin component that reads from the provider's existing `IsAutoGenerated` / `IsTimedOut` / `GetExpiration` contract.
10. **Per-secret cache TTL drift with Infisical rotation** - Infisical rotated secrets become live only after cache expires (5 min). **Mitigation**: document the TTL in `docs/SECRETS.md`; expose `POST /api/SecretBindings/{key}/refresh-cache` (admin only) for forced eviction until a webhook subscription is added in a later sprint.

## Success Metrics

- **Zero fallback paths** - automated test asserts that a binding with `SourceType=EnvironmentVariable` never triggers an Infisical call and vice versa.
- **Zero secret-value leaks in UI/logs** - API-contract test asserts no response from `/api/SecretBindings` or `/validate` contains the ciphertext or the plaintext.
- **Minimal deployment works** - integration test spins up API + Blazor + Postgres with no Infisical, no S3, no SMTP, no PostHog configured, and every page still loads; email/S3/analytics features report "Not configured" in their UI.
- **Onboarding auto-detection** - integration test configures Keycloak secrets in env vars, boots onboarding page, asserts the Keycloak card shows the "Auto-detected" chip and the toggle is locked.
- **Zero `InfrastructureSecretSettingKeys` references** after PR 5 - grep-based architecture test.
- **Zero `AppSetting`, `DbConfigurationProvider`, `AesEncryptionService`, `KeyRotationService`, `SecretRefreshService` references** after PR 6 - grep-based architecture test.
- **Lighthouse + bUnit accessibility scores** unchanged on the new admin Secrets page.

## Required Resources And Dependencies

- NuGet: `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` (if not already transitively present via `Explore.Persistence`).
- NuGet: existing `Infisical.Sdk` (v3.0.4) - stays.
- Existing `IMemoryCache` + `HybridCache` - reused.
- Cerbos policy updates: new resource `secret_binding` in `cerbos/policies/secret_binding.yaml` (instance admin: read/write; tenant admin: read/write tenant-scope only; anonymous: none).
- EF Core migration tooling.
- Documentation rewrite of `docs/SECRETS.md`; minor update of `docs/CONFIGURATION.md`, `docs/QUICK_REFERENCE.md`, `docs/TROUBLESHOOTING.md`.

## Effort Estimates

| Phase | Effort | Notes |
|-------|--------|-------|
| Phase 1 — Foundations | **L** | Registry + entity + EF config + repo + DP wiring. No consumer cutover. |
| Phase 2 — Bootstrap split | **M** | Discrete POSTGRESQL_* loader + `NpgsqlConnectionStringBuilder`. |
| Phase 3 — Resolver + admin API | **XL** | Three source implementations + CQRS + controller + tests. Highest-risk PR. |
| Phase 4 — Onboarding + auth | **L** | Keycloak scheme-refresh and onboarding UI rework. |
| Phase 5 — Consumer migration | **M** | Mechanical + tests per consumer. |
| Phase 6 — Deletion + docs | **M** | Destructive migration + doc rewrite. |

## Potential Risks & Unknowns

The **single most likely area to become complex** is **Phase 3's concurrency and cache-invalidation correctness**: a binding update arriving while a resolver is mid-flight must never serve the old source, and the `SecretBindingUpdatedEvent` → `InvalidateAsync` path must be synchronous with the command response. The `Infisical.Sdk` client's internal cache and our per-secret `IMemoryCache` create two cache layers; if the wrong one is not evicted, the stale-cache risk (#4) becomes real in production.

The **second most likely soft spot** is **Keycloak dynamic scheme refresh** (Phase 4): the existing `DynamicAuthSchemeManager` was built for startup-time registration, and making it gracefully swap a handler's `ClientSecret` without dropping in-flight OIDC exchanges needs careful testing with a real Keycloak container. Expect this to require an extra integration test pass and a follow-up consultation with Oracle if the first implementation trips.

The **third is the filtered unique index semantics** (risk #1): getting the Postgres partial index + EF configuration + `Explore.Persistence.IntegrationTests` aligned requires attention; a wrong index lets two "Instance" rows for the same `SettingKey` coexist undetected.

These are the three places to spend extra review cycles.

ABOUTME: Session context and key reference map for the Secrets Refactor (Control Plane / Data Plane) task - quick-resume aid for multi-session work.
ABOUTME: Links to every file being introduced, replaced, or deleted so any session can pick up the refactor without repeating exploration.

# Secrets Refactor - Context

Last Updated: 2026-04-18

## SESSION PROGRESS

### ✅ COMPLETED
- Research: 5 parallel agents mapped current state (see (b1), (b2)) - `Explore.Secrets` internals, all consumers (SETUP_SECRET, S3, Keycloak, Postgres, SMTP, Analytics, AI-not-implemented), onboarding flow, Infisical SDK best practices, Data Protection / Npgsql patterns.
- Oracle architecture review (see (b3)) - verdict: ship direction with four adjustments: (a) split bootstrap from runtime, (b) introduce `SecretDefinitionRegistry`, (c) normalized metadata columns not polymorphic JSON, (d) drop `Module` scope and `Inherited` source type from v1.
- Plan file authored: `secrets-refactor-control-plane-plan.md`.
- Tasks file authored: `secrets-refactor-control-plane-tasks.md`.

### 🟡 IN PROGRESS
- Awaiting user approval of the plan before any code edits.

### ⚠️ BLOCKERS
- **None** once user signs off on the plan. Per CLAUDE.md rule: "Never assume an exception. Get explicit permission before breaking or bending any rule" and the user's original `go refactor` directive means proceed with implementation after plan acceptance.

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

### Bootstrap refactored in Phase 2
- `Explore.Persistence/PersistenceServicesRegistration.cs` - read discrete Postgres fields via `BootstrapSecretLoader`, compose via `NpgsqlConnectionStringBuilder`.
- `Explore.API/Program.cs` - swap `AddInfisicalCompatibility` for `AddBootstrapSecretLoader`; drop compat mapping call.
- `Explore.Blazor/Program.cs` - same.
- `Explore.AppHost/AppHost.cs` - pass discrete Postgres env vars to services (not URL form).
- `docker-compose.yml` - discrete `POSTGRESQL_*` env vars; drop `POSTGRESQL_PUBLIC_URL`; rename Infisical S3/Keycloak names to user's layout.

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

# Phase 3 Implementation Plan — ISecretResolver + Admin Bindings API

> **ABOUTME**: Tight, file-by-file execution blueprint for Phase 3 of the secrets control-plane refactor.
> **ABOUTME**: Designed for a fresh session to execute without re-discovering templates or patterns.

**Branch**: `develop`
**Last commits**: `fc0b2b5a` (Phase 2), `38ce8098` (Phase 1)
**Test baseline to preserve**: 1,305 green (Event.Application.UnitTests 823 + Event.Domain.UnitTests 207 + Event.Architecture.Tests 74 + Explore.Secrets.UnitTests 201)
**Commit target**: `refactor(secrets): phase 3 introduce ISecretResolver + admin bindings API`

## Standing User Directives (DO NOT VIOLATE)

1. **NO delegation** — execute all work yourself (m0007)
2. **NO backward compatibility** — break/fix/iterate (dev mode)
3. **Enterprise-grade quality** — clean architecture, design patterns, highly maintainable
4. **Single Phase 3 commit** at the end (m0229), no intermediate stops
5. **Use Tavily MCP for research, Context7 MCP for library docs** when needed
6. Follow ALL repo conventions in CLAUDE.md + QUICK_REFERENCE.md

## Foundation State (Already Committed)

| Asset | Path | Status |
|---|---|---|
| `SecretBinding` entity + factory | `Explore.Domain/Secrets/SecretBinding{,.Factory}.cs` | ✅ Phase 1 |
| `SecretDefinition` + Registry | `Explore.Domain/Secrets/SecretDefinition{,Registry}.cs` | ✅ Phase 1 |
| Enums: `SecretScope`, `SecretSourceType`, `SecretValidationResult` | `Explore.Domain/Enums/` | ✅ Phase 1 |
| `ISecretBindingRepository` | `Explore.Application/Contracts/Persistence/` | ✅ Phase 1 |
| `SecretBindingRepository` impl | `Explore.Persistence/Repositories/` | ✅ Phase 1 |
| EF config + filtered unique indexes | `Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs` | ✅ Phase 1 |
| Migration `AddSecretBindingsAndDataProtectionKeys` | `Explore.Persistence/Migrations/` | ✅ Phase 1 |
| `AddExploreDataProtection()` extension | `Explore.Persistence/Extensions/DataProtectionServiceCollectionExtensions.cs` | ✅ Phase 1 |
| `BootstrapSecretLoader` | `Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs` | ✅ Phase 2 |

## Template Reference Files (READ FIRST in fresh session)

Before writing any new file, READ the corresponding template:

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
| Notification handler | search for `INotificationHandler` in `Explore.Application/` to find one |

Also read once:
- `Explore.API/Hateoas/RouteNames.cs` — to know exact `#region` style for new routes
- `Explore.Application/PipelineBehaviors/AuthorizationBehavior.cs` — confirm Cerbos integration shape
- `Explore.Application/Mappings/MappingProfile.cs` (or `*Profile.cs` files) — to know where to add SecretBinding ↔ DTO mappings
- `Explore.Application/ApplicationServicesRegistration.cs` — DI registration pattern
- `Explore.Persistence/PersistenceServicesRegistration.cs` — DI registration pattern
- `Explore.API/Program.cs` — find the `services.AddHybridCache()`, MediatR, and policy wiring blocks for Phase 3 wiring

## Implementation Order (Bottom-Up)

Execute in this exact order. Each section is atomic: complete fully before moving on.

---

### 3.0 — Read Templates + Existing Mapping Profile (15 min)

**No file changes.** Just read the template files listed above. Confirm the namespace/folder conventions match what you'll create.

Verify:
- `Explore.Application/Mappings/` — find the AutoMapper profile to extend (or create `SecretBindingProfile.cs` if pattern is one-profile-per-feature)
- `Explore.Application/Authorization/AuthorizationActions.cs` — verify `Create/Update/Delete/View` constants exist (likely yes)
- `Explore.Application/Authorization/ResourceDescriptors.cs` — note pattern for adding `SecretBinding` descriptor
- `Explore.Application/Responses/BaseCommandResponse.cs` — confirm shape (5 fields per the research)

---

### 3.1 — Domain Event (1 file)

**Path**: `Explore.Domain/Secrets/Events/SecretBindingUpdatedEvent.cs`

```csharp
// ABOUTME: Domain event raised when a SecretBinding is created, updated, or deleted.
// ABOUTME: Triggers cache invalidation and downstream resolver refreshes.

namespace Explore.Domain.Secrets.Events;

public sealed record SecretBindingUpdatedEvent(
    Guid BindingId,
    int SecretKeyId,
    SecretScope Scope,
    Guid? ScopeId,
    SecretBindingChangeKind ChangeKind,
    DateTimeOffset OccurredAt);

public enum SecretBindingChangeKind
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    SourceSwitched = 3
}
```

**Verify**: Domain has zero deps. Add `using` only for `System` types. The enum lives in same file (small, cohesive).

---

### 3.2 — Application Contracts (4 files)

#### 3.2.1 `Explore.Application/Contracts/Secrets/ResolvedSecret.cs`

```csharp
// ABOUTME: Immutable record returned by ISecretResolver containing the materialized secret value
// ABOUTME: plus provenance metadata for audit and observability.

namespace Explore.Application.Contracts.Secrets;

public sealed record ResolvedSecret(
    int SecretKeyId,
    string Value,
    SecretSourceType Source,
    DateTimeOffset ResolvedAt,
    DateTimeOffset? ExpiresAt,
    string? VersionHint);
```

(Reference `Explore.Domain.Enums.SecretSourceType` via using.)

#### 3.2.2 `Explore.Application/Contracts/Secrets/ISecretResolver.cs`

```csharp
// ABOUTME: Primary abstraction for resolving a secret value from its declared single source.
// ABOUTME: NO fallback chains — the binding row dictates exactly which source is consulted.

namespace Explore.Application.Contracts.Secrets;

public interface ISecretResolver
{
    Task<ResolvedSecret?> ResolveAsync(int secretKeyId, Guid? tenantId, CancellationToken cancellationToken);
    Task InvalidateAsync(int secretKeyId, Guid? tenantId, CancellationToken cancellationToken);
}
```

#### 3.2.3 `Explore.Application/Contracts/Secrets/ISecretSource.cs`

```csharp
// ABOUTME: Marker base for per-source secret retrieval. Each implementation handles exactly one SecretSourceType.
// ABOUTME: Returns null when the secret is not found at the source (never throws for missing).

namespace Explore.Application.Contracts.Secrets;

public interface ISecretSource
{
    SecretSourceType SourceType { get; }
    Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken);
    Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken);
}
```

#### 3.2.4 `Explore.Application/Contracts/Secrets/IInfisicalClientFactory.cs`

```csharp
// ABOUTME: Factory abstraction so the Infisical SDK client lifetime is owned by Infrastructure
// ABOUTME: while Application code remains library-agnostic and unit-testable.

namespace Explore.Application.Contracts.Secrets;

public interface IInfisicalClientFactory
{
    Task<IInfisicalClient> GetClientAsync(CancellationToken cancellationToken);
}

public interface IInfisicalClient : IAsyncDisposable
{
    Task<string?> GetSecretRawAsync(string projectId, string environment, string folderPath, string secretName, CancellationToken cancellationToken);
}
```

---

### 3.3 — Per-Source Implementations (3 files)

All in `Explore.Secrets/Sources/`:

#### 3.3.1 `EnvironmentSecretSource.cs`

```csharp
// ABOUTME: Resolves secrets from process environment variables using the binding's EnvVarName metadata.
// ABOUTME: Always available, no external deps; suitable for bootstrap and local dev.

namespace Explore.Secrets.Sources;

public sealed class EnvironmentSecretSource : ISecretSource
{
    public SecretSourceType SourceType => SecretSourceType.EnvironmentVariable;

    public Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (string.IsNullOrWhiteSpace(binding.EnvironmentVariableName))
            return Task.FromResult<string?>(null);
        var raw = Environment.GetEnvironmentVariable(binding.EnvironmentVariableName);
        return Task.FromResult(string.IsNullOrEmpty(raw) ? null : raw);
    }

    public async Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken)
        => !string.IsNullOrEmpty(await GetSecretAsync(binding, cancellationToken));
}
```

(NOTE: confirm exact property name on `SecretBinding` — it may be `EnvVarName` or similar. Read the entity file first.)

#### 3.3.2 `InlineSecretSource.cs`

Uses `IDataProtectionProvider` with purpose `("Event.Secrets", "Binding", "v1")`. Reads `binding.InlineEncryptedValue` (verify exact property name), unprotects, returns plaintext.

```csharp
// ABOUTME: Resolves secrets stored inline as DataProtection-encrypted ciphertext on the SecretBinding row.
// ABOUTME: Bootstrap secrets (DB connection) MUST NOT use this source — see SecretDefinition.AllowsInlineEncrypted.

namespace Explore.Secrets.Sources;

public sealed class InlineSecretSource : ISecretSource
{
    private readonly IDataProtector _protector;

    public InlineSecretSource(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector("Event.Secrets", "Binding", "v1");
    }

    public SecretSourceType SourceType => SecretSourceType.InlineEncrypted;

    public Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (string.IsNullOrEmpty(binding.InlineEncryptedValue))
            return Task.FromResult<string?>(null);
        try
        {
            var plaintext = _protector.Unprotect(binding.InlineEncryptedValue);
            return Task.FromResult<string?>(plaintext);
        }
        catch (CryptographicException)
        {
            return Task.FromResult<string?>(null);
        }
    }

    public async Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken)
        => !string.IsNullOrEmpty(await GetSecretAsync(binding, cancellationToken));
}
```

#### 3.3.3 `InfisicalSecretSource.cs`

Reads `binding.InfisicalProjectId`, `binding.InfisicalEnvironment`, `binding.InfisicalFolderPath`, `binding.InfisicalSecretName` (verify names). Resolves through `IInfisicalClientFactory.GetClientAsync()`.

```csharp
// ABOUTME: Resolves secrets via the Infisical Universal Auth API using the binding's project/env/folder/name metadata.
// ABOUTME: Returns null on missing secret; logs and returns null on transient errors (resolver may fall through to cache).

namespace Explore.Secrets.Sources;

public sealed class InfisicalSecretSource : ISecretSource
{
    private readonly IInfisicalClientFactory _clientFactory;
    private readonly ILogger<InfisicalSecretSource> _logger;

    public InfisicalSecretSource(IInfisicalClientFactory clientFactory, ILogger<InfisicalSecretSource> logger) { ... }

    public SecretSourceType SourceType => SecretSourceType.Infisical;

    public async Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken) { ... }
    public async Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken) { ... }
}
```

---

### 3.4 — Infisical Client Implementation (1 file)

**Path**: `Explore.Secrets/Infrastructure/InfisicalClientFactory.cs`

Wraps `Infisical.Sdk.InfisicalSdk`. Uses `IOptions<InfisicalOptions>` for clientId/clientSecret/siteUrl. Caches authenticated client. Defensive disposal pattern (the SDK is NOT IDisposable directly per Phase 1 research):

```csharp
if (_client is IAsyncDisposable a) await a.DisposeAsync();
else if (_client is IDisposable d) d.Dispose();
```

Also create `Explore.Secrets/Infrastructure/InfisicalOptions.cs`:

```csharp
public sealed class InfisicalOptions
{
    public string? SiteUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
```

---

### 3.5 — Resolver + Decorators (3 files)

#### 3.5.1 `Explore.Secrets/Services/SecretResolver.cs`

Core dispatch. Composition: `IEnumerable<ISecretSource> sources`, `ISecretBindingRepository bindings`, `IMemoryCache cache`, `ILogger`. Uses 5-min TTL keyed by `($"sec:{secretKeyId}:{tenantId ?? Guid.Empty}")`.

Algorithm:
1. Look up binding in repo by (secretKeyId, tenantId scope).
2. If no binding → return null (NEVER fall through to another source).
3. Find the source whose `SourceType == binding.SourceType` from the registered `IEnumerable<ISecretSource>`.
4. If no matching source registered → log error, return null.
5. Call `source.GetSecretAsync(binding, ct)`.
6. Cache result (only when non-null) with 5-min TTL.
7. Return `ResolvedSecret(...)`.

`InvalidateAsync` → `cache.Remove(key)`.

#### 3.5.2 `Explore.Secrets/Services/AuditingSecretResolverDecorator.cs`

Decorator wrapping `ISecretResolver`. Sample reads (e.g., 1 in 50 via `RandomNumberGenerator`) emit a structured log + an `secret_binding_audit_logs` row (or just structured log for v1; defer DB audit to Phase 5 if it adds scope). For v1, log to ILogger + emit OpenTelemetry counter. NO secret value in the log — only `(secretKeyId, source, tenantId, success)`.

#### 3.5.3 `Explore.Secrets/Services/CompositeSecretResolverRegistration.cs`

Static extension class with `AddSecretResolver(this IServiceCollection services)` that:
- Registers `EnvironmentSecretSource`, `InlineSecretSource`, `InfisicalSecretSource` as `ISecretSource`
- Registers `InfisicalClientFactory` as singleton `IInfisicalClientFactory`
- Registers concrete `SecretResolver`
- Wraps with `AuditingSecretResolverDecorator` as the public `ISecretResolver`
- Configures `services.Configure<InfisicalOptions>(configuration.GetSection("SecretProvider:Infisical"))`

Wire from `Program.cs` (API + Blazor).

---

### 3.6 — Observability (2 files)

#### 3.6.1 `Explore.Secrets/Observability/SecretResolverMetrics.cs`

Static class exposing OpenTelemetry `Meter` named `Event.Secrets`. Counters:
- `secrets.resolve.success` (tags: source, has_tenant)
- `secrets.resolve.miss` (tags: source, has_tenant)
- `secrets.resolve.error` (tags: source, error_kind)
- `secrets.cache.hit` (tags: scope)
- `secrets.cache.miss` (tags: scope)

Histogram: `secrets.resolve.duration_ms` (tags: source).

`SecretResolver` and `AuditingSecretResolverDecorator` consume these.

#### 3.6.2 `Explore.Secrets/HealthChecks/SecretResolverHealthCheck.cs`

`IHealthCheck`. Iterates registered `IEnumerable<ISecretSource>`. For each, attempts a lightweight ping (e.g., `EnvironmentSecretSource` → no-op success; `InfisicalSecretSource` → `await _clientFactory.GetClientAsync()`; `InlineSecretSource` → confirms `_protector` non-null). Returns `HealthCheckResult.Degraded` if any source fails (NOT Unhealthy — secrets may be intentionally absent).

Wire in `Program.cs`: `services.AddHealthChecks().AddCheck<SecretResolverHealthCheck>("secret-resolver", tags: ["secrets", "ready"]);`

---

### 3.7 — DTOs + Validators (5 files)

In `Explore.Application/DTOs/SecretBindings/`:

#### 3.7.1 `SecretBindingDto.cs` (read-only detail DTO)

Fields: `Id`, `SecretKeyId`, `SecretKeyName` (registry lookup), `Scope`, `ScopeId`, `SourceType`, `EnvironmentVariableName`, `InfisicalProjectId/Environment/FolderPath/SecretName`, `LastValidationResult`, `LastValidatedAt`, `CreatedAt`, `UpdatedAt`. **NEVER includes the secret value.**

#### 3.7.2 `SecretBindingListDto.cs`

Subset for collection display: `Id`, `SecretKeyId`, `SecretKeyName`, `Scope`, `SourceType`, `LastValidationResult`, `LastValidatedAt`, `UpdatedAt`.

#### 3.7.3 `CreateSecretBindingDto.cs`

Mutation input: `SecretKeyId`, `Scope`, `ScopeId?`, `SourceType`, `EnvironmentVariableName?`, `InfisicalProjectId?/Environment?/FolderPath?/SecretName?`, `InlineSecretValue?` (plaintext — handler protects + discards).

#### 3.7.4 `UpdateSecretBindingDto.cs`

Same as Create plus `Id`. Used to switch sources or update metadata.

#### 3.7.5 `Validators/CreateSecretBindingDtoValidator.cs` and `UpdateSecretBindingDtoValidator.cs`

FluentValidation. Rules:
- `SecretKeyId` must exist in `SecretDefinitionRegistry`.
- `SourceType` must be in `definition.AllowedSources`.
- `Scope` must be in `definition.AllowedScopes`.
- `ScopeId` required when `Scope == Tenant`, must be null when `Scope == Instance`.
- Per-source field validation:
  - `Infisical`: project/env/secretName required, folderPath optional.
  - `EnvironmentVariable`: `EnvironmentVariableName` required.
  - `InlineEncrypted`: `InlineSecretValue` required AND `definition.AllowsInlineEncrypted == true` (bootstrap secrets fail here).
- Inject `ISecretBindingRepository` (manually instantiated in handler) to check uniqueness against the filtered indexes.

---

### 3.8 — AutoMapper Profile (1 file)

**Path**: `Explore.Application/Mappings/SecretBindingProfile.cs` (or extend existing `MappingProfile.cs` — read first to know which).

Maps:
- `SecretBinding` → `SecretBindingDto` (looks up `SecretKeyName` from registry — use `AfterMap` or custom resolver)
- `SecretBinding` → `SecretBindingListDto`
- `CreateSecretBindingDto` → factory call (do not map directly; handler uses `SecretBinding.Create(...)` factory)

---

### 3.9 — CQRS Commands (4 commands × 2 files = 8 files)

In `Explore.Application/Features/SecretBindings/`:

#### 3.9.1 Folder structure
```
Handlers/Commands/
  CreateSecretBindingCommandHandler.cs
  UpdateSecretBindingCommandHandler.cs
  DeleteSecretBindingCommandHandler.cs
  ValidateSecretBindingCommandHandler.cs
Handlers/Queries/
  GetSecretBindingListRequestHandler.cs
  GetSecretBindingDetailsRequestHandler.cs
  GetAvailableSecretsForOnboardingRequestHandler.cs
Requests/Commands/
  CreateSecretBindingCommand.cs
  UpdateSecretBindingCommand.cs
  DeleteSecretBindingCommand.cs
  ValidateSecretBindingCommand.cs
Requests/Queries/
  GetSecretBindingListRequest.cs
  GetSecretBindingDetailsRequest.cs
  GetAvailableSecretsForOnboardingRequest.cs
```

#### 3.9.2 Command shape (template `CreateCategoryCommand.cs`)

```csharp
[AuthorizeResource("secret_binding", AuthorizationActions.Create)]
public class CreateSecretBindingCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateSecretBindingDto Dto { get; set; }
    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        Dto.Scope == SecretScope.Tenant && Dto.ScopeId.HasValue
            ? new Dictionary<string, object> { ["tenantId"] = Dto.ScopeId.Value.ToString() }
            : null;
}
```

#### 3.9.3 `CreateSecretBindingCommandHandler.cs` algorithm

1. Manual validate: `var validator = new CreateSecretBindingDtoValidator(_repo); var result = await validator.ValidateAsync(request.Dto, ct);` — on failure, return failure response with `Errors`.
2. Lookup definition: `var def = SecretDefinitionRegistry.Get(request.Dto.SecretKeyId);` — if null, return failure with `FailureCode = "SECRET_KEY_UNKNOWN"`.
3. Encrypt inline secret if `SourceType == InlineEncrypted`: `var protector = _dataProtectionProvider.CreateProtector("Event.Secrets","Binding","v1"); var ciphertext = protector.Protect(request.Dto.InlineSecretValue!);`
4. Call factory: `var binding = SecretBinding.Create(...)` passing all metadata. Factory enforces invariants from registry.
5. `_repo.Create(binding); await _unitOfWork.SaveAsync(ct);`
6. Publish `SecretBindingUpdatedEvent(binding.Id, ..., ChangeKind.Created, DateTimeOffset.UtcNow)` via MediatR.
7. Return `new BaseCommandResponse<Guid> { Success = true, Id = binding.Id, Message = "Secret binding created." }`.

#### 3.9.4 `UpdateSecretBindingCommandHandler.cs`

Similar but loads existing binding, calls `binding.Switch(...)` factory method (or property setters via factory), publishes `ChangeKind.Updated` or `ChangeKind.SourceSwitched` based on whether `SourceType` changed.

#### 3.9.5 `DeleteSecretBindingCommandHandler.cs`

Loads binding, `_repo.Delete(binding)`, saves, publishes `ChangeKind.Deleted`. Returns `BaseCommandResponse<bool>`.

#### 3.9.6 `ValidateSecretBindingCommandHandler.cs`

Loads binding, finds matching `ISecretSource` from injected `IEnumerable<ISecretSource>`, calls `source.ValidateAsync(binding, ct)`. Updates `binding.RecordValidation(success ? Success : Failure)`. Saves. Returns response with success flag + message.

---

### 3.10 — CQRS Queries (3 queries × 2 files = 6 files)

#### 3.10.1 `GetSecretBindingListRequest.cs`

```csharp
public class GetSecretBindingListRequest : IRequest<PaginatedResult<SecretBindingListDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public SecretScope? ScopeFilter { get; set; }
    public Guid? ScopeIdFilter { get; set; }
}
```

Handler: pages through repo, maps to DTO, includes registry-derived `SecretKeyName`. Uses HybridCache with key `secret_bindings:list:{scopeFilter}:{scopeIdFilter}:{page}:{pageSize}` 30-sec TTL.

#### 3.10.2 `GetSecretBindingDetailsRequest.cs`

Loads single binding by Id. Returns `SecretBindingDto?` (null if not found — controller maps to 404).

#### 3.10.3 `GetAvailableSecretsForOnboardingRequest.cs`

Returns `IReadOnlyList<AvailableSecretDto>` enumerated from `SecretDefinitionRegistry.GetAll()` filtered by `def.AllowedScopes` and current request's scope context. Each item: `SecretKeyId`, `SecretKeyName`, `Description`, `IsBootstrap`, `AllowedSources`, `AllowedScopes`, `IsBound` (true if a binding exists for this key+scope), `CurrentSourceType?`.

This drives the Onboarding UI's "what secrets need configuring" panel.

---

### 3.11 — Notification Handlers (2 files)

In `Explore.Application/Features/SecretBindings/Handlers/Notifications/`:

#### 3.11.1 `InvalidateSecretCacheOnUpdatedHandler.cs`

```csharp
// ABOUTME: Invalidates the in-memory ISecretResolver cache when a SecretBinding changes.
// ABOUTME: Listens to SecretBindingUpdatedEvent.

public sealed class InvalidateSecretCacheOnUpdatedHandler : INotificationHandler<SecretBindingUpdatedEvent>
{
    private readonly ISecretResolver _resolver;
    public InvalidateSecretCacheOnUpdatedHandler(ISecretResolver resolver) { _resolver = resolver; }
    public async Task Handle(SecretBindingUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var tenantId = notification.Scope == SecretScope.Tenant ? notification.ScopeId : null;
        await _resolver.InvalidateAsync(notification.SecretKeyId, tenantId, cancellationToken);
    }
}
```

NOTE: `SecretBindingUpdatedEvent` must implement `INotification` (MediatR) — adjust the record to inherit `INotification`. (Re-edit the domain file in section 3.1 to add `: INotification` if MediatR is referenced from Domain — IF NOT, create a thin wrapper notification in Application layer instead. Confirm clean-arch rule: Domain has zero deps. **Decision: create wrapper in Application.**)

So actually:

- Domain stays pure: `SecretBindingUpdatedEvent` is a plain record in Domain.
- Application has `Explore.Application/Notifications/Secrets/SecretBindingChangedNotification.cs` implementing `INotification`, wrapping the domain event.
- Command handlers publish the wrapper.

Adjust 3.1 accordingly: keep domain event as plain record. Add wrapper file:

**Path**: `Explore.Application/Notifications/Secrets/SecretBindingChangedNotification.cs`

```csharp
public sealed record SecretBindingChangedNotification(SecretBindingUpdatedEvent Event) : INotification;
```

#### 3.11.2 `RefreshKeycloakSchemeOnAuthSecretUpdatedHandler.cs`

Listens to `SecretBindingChangedNotification`. If `notification.Event.SecretKeyId` corresponds to a Keycloak/auth-related secret (check definition.Category or hardcoded set), calls `IDynamicAuthSchemeManager.RefreshSchemesAsync()`. **Verify** if `IDynamicAuthSchemeManager` exists in current code — Phase 4 plan mentions it. If not yet present, **stub this handler** to log a warning "Keycloak scheme refresh requested for {SecretKey}, awaiting Phase 4 IDynamicAuthSchemeManager wire-up." Don't block Phase 3 on Phase 4 work.

---

### 3.12 — Authorization Resource Descriptor (1 modification)

Edit `Explore.Application/Authorization/ResourceDescriptors.cs` to add:

```csharp
public const string SecretBinding = "secret_binding";
```

Verify the file's existing pattern first.

---

### 3.13 — RouteNames (1 modification)

Edit `Explore.API/Hateoas/RouteNames.cs`. Add a new region:

```csharp
#region Secret Binding Routes
public const string GetSecretBindings = "GetSecretBindings";
public const string GetSecretBindingById = "GetSecretBindingById";
public const string CreateSecretBinding = "CreateSecretBinding";
public const string UpdateSecretBinding = "UpdateSecretBinding";
public const string DeleteSecretBinding = "DeleteSecretBinding";
public const string ValidateSecretBinding = "ValidateSecretBinding";
public const string GetAvailableSecretsForOnboarding = "GetAvailableSecretsForOnboarding";
#endregion
```

---

### 3.14 — Controller (1 file)

**Path**: `Explore.API/Controllers/SecretBindingsController.cs`

Template: `CategoryController.cs` exactly. Key differences:

- ALL endpoints `[Authorize]` (no `[AllowAnonymous]` — admin only). Cerbos enforces actual permission via pipeline behavior.
- Constructor injects: `IMediator`, `ILogger<SecretBindingsController>`, `IResourceAssembler<SecretBindingDto, SecretBindingListDto>`.
- Routes:
  - `GET /api/SecretBindings` → list (output cache `ListData`)
  - `GET /api/SecretBindings/{id:guid}` → detail (output cache `DetailData`)
  - `GET /api/SecretBindings/available-for-onboarding` → `IReadOnlyList<AvailableSecretDto>` (no HAL wrapping needed — flat list)
  - `POST /api/SecretBindings` → create, returns `CreatedAtRoute(RouteNames.GetSecretBindingById, ...)`
  - `PUT /api/SecretBindings/{id:guid}` → update, validates id match
  - `POST /api/SecretBindings/{id:guid}/validate` → validate (write op because it updates `LastValidatedAt`)
  - `DELETE /api/SecretBindings/{id:guid}` → delete, returns `NoContent()`
- All endpoints have `[EndpointSummary]` + `[EndpointDescription]` for OpenAPI.
- Output cache: `[OutputCache(PolicyName = "ListData")]` on list, `"DetailData"` on detail. Mutation endpoints invalidate via output cache eviction tag (use `EvictByTagAsync` in handlers OR cache version key).

---

### 3.15 — HATEOAS Policy + Assembler (2 files)

#### 3.15.1 `Explore.API/Hateoas/Policies/SecretBindingLinkPolicy.cs`

Two classes (per template):
- `SecretBindingDetailLinkPolicy : ILinkPolicy<SecretBindingDto>` — yields self, edit, delete, validate links with `RequirePermission(AuthorizationActions.Update/Delete, ResourceDescriptors.SecretBinding, dto)`.
- `SecretBindingCollectionLinkPolicy : ICollectionLinkPolicy<SecretBindingListDto>` — yields self, create link with `RequirePermission(AuthorizationActions.Create, typeof(SecretBindingDto), ResourceDescriptors.SecretBinding)`, item-level edit/delete links.

#### 3.15.2 `Explore.API/Hateoas/Assemblers/SecretBindingResourceAssembler.cs`

19-line stub matching `CategoryResourceAssembler.cs`:

```csharp
public sealed class SecretBindingResourceAssembler : ResourceAssemblerBase<SecretBindingDto, SecretBindingListDto>
{
    public SecretBindingResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<SecretBindingDto> detailLinkPolicy,
        ICollectionLinkPolicy<SecretBindingListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy) { }
}
```

Wire in `Program.cs` (find the existing block where `CategoryResourceAssembler` and link policies register):

```csharp
services.AddScoped<ILinkPolicy<SecretBindingDto>, SecretBindingDetailLinkPolicy>();
services.AddScoped<ICollectionLinkPolicy<SecretBindingListDto>, SecretBindingCollectionLinkPolicy>();
services.AddScoped<IResourceAssembler<SecretBindingDto, SecretBindingListDto>, SecretBindingResourceAssembler>();
```

---

### 3.16 — Cerbos Policy (1 file)

**Path**: `cerbos/policies/secret_binding.yaml`

```yaml
# ABOUTME: Authorization policy for SecretBinding admin resource.
# ABOUTME: Instance admins can manage all bindings; tenant admins can only manage tenant-scoped bindings for their tenant.
apiVersion: api.cerbos.dev/v1
resourcePolicy:
  resource: "secret_binding"
  version: "default"
  importDerivedRoles:
    - explore_admin_roles
  schemas:
    principalSchema:
      ref: cerbos:///principal.json
    resourceSchema:
      ref: cerbos:///secret_binding.json
  rules:
    - actions: ["*"]
      effect: EFFECT_ALLOW
      derivedRoles: [instance_admin]
    - actions: ["view", "create", "update", "delete", "validate"]
      effect: EFFECT_ALLOW
      derivedRoles: [tenant_admin]
      condition:
        match:
          expr: request.resource.attr.tenantId == request.principal.attr.tenantId
    # No role below tenant_admin gets any access to secret bindings.
```

Also create `cerbos/schemas/secret_binding.json` if the existing schemas folder pattern requires per-resource JSON schemas (check first by `ls cerbos/schemas/`).

---

### 3.17 — DI Wiring (3 modifications)

#### 3.17.1 `Explore.Secrets/Extensions/SecretsServiceCollectionExtensions.cs` (new file)

```csharp
public static class SecretsServiceCollectionExtensions
{
    public static IServiceCollection AddSecretResolution(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InfisicalOptions>(configuration.GetSection("SecretProvider:Infisical"));

        services.AddSingleton<IInfisicalClientFactory, InfisicalClientFactory>();

        services.AddScoped<ISecretSource, EnvironmentSecretSource>();
        services.AddScoped<ISecretSource, InlineSecretSource>();
        services.AddScoped<ISecretSource, InfisicalSecretSource>();

        services.AddScoped<SecretResolver>();
        services.AddScoped<ISecretResolver>(sp =>
            new AuditingSecretResolverDecorator(
                sp.GetRequiredService<SecretResolver>(),
                sp.GetRequiredService<ILogger<AuditingSecretResolverDecorator>>()));

        services.AddMemoryCache();

        services.AddHealthChecks()
            .AddCheck<SecretResolverHealthCheck>("secret-resolver", tags: new[] { "secrets", "ready" });

        return services;
    }
}
```

#### 3.17.2 Edit `Explore.API/Program.cs`

Add `services.AddSecretResolution(builder.Configuration);` near the existing `AddExploreDataProtection` call. Also register the SecretBinding HATEOAS triplet.

#### 3.17.3 Edit `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`

Add the same `services.AddSecretResolution(builder.Configuration);` after the existing data protection wiring.

---

### 3.18 — Tests

Add tests in this order. Target: ~40-50 new tests total.

#### 3.18.1 `Explore.Secrets.UnitTests/Sources/EnvironmentSecretSourceTests.cs` (~6 tests)
- Returns null when EnvVarName empty
- Returns null when env var missing
- Returns value when env var set
- Validate true when value present
- Validate false when missing
- SourceType == EnvironmentVariable

#### 3.18.2 `Explore.Secrets.UnitTests/Sources/InlineSecretSourceTests.cs` (~5 tests)
- Returns null when InlineEncryptedValue empty
- Returns null when ciphertext invalid (CryptographicException)
- Returns plaintext after Protect roundtrip
- Validate true after roundtrip
- SourceType == InlineEncrypted

#### 3.18.3 `Explore.Secrets.UnitTests/Sources/InfisicalSecretSourceTests.cs` (~5 tests)
Use mock `IInfisicalClientFactory` returning fake `IInfisicalClient`.
- Resolves via client factory
- Returns null when client returns null
- Catches client exceptions, returns null
- Validate uses GetSecret success
- SourceType == Infisical

#### 3.18.4 `Explore.Secrets.UnitTests/Services/SecretResolverTests.cs` (~10 tests) — **CRITICAL**
- Returns null when binding not found (NEVER falls through)
- Returns null when no source registered for binding's SourceType (NEVER falls through)
- Dispatches to correct source by SourceType
- Caches non-null results 5 min
- Does NOT cache null results
- InvalidateAsync removes cache entry
- Different tenantIds use different cache keys
- Returns ResolvedSecret with correct provenance
- Logs error when source missing
- **No-fallback test**: when binding says Infisical but Infisical source returns null, resolver returns null (does NOT try Environment or Inline)

#### 3.18.5 `Explore.Secrets.UnitTests/Services/AuditingSecretResolverDecoratorTests.cs` (~4 tests)
- Forwards calls to inner resolver
- **Never logs the secret value** (regex check on log output)
- Increments metrics on success
- Increments metrics on miss

#### 3.18.6 `Event.Application.UnitTests/Features/SecretBindings/Commands/CreateSecretBindingCommandHandlerTests.cs` (~8 tests)
- Validation failure returns BaseCommandResponse with Errors
- Unknown SecretKeyId returns FailureCode=SECRET_KEY_UNKNOWN
- Bootstrap secret + InlineEncrypted returns failure (factory throws)
- Successful create returns Success=true with new Id
- Publishes SecretBindingChangedNotification
- Inline secret encrypted via DataProtection (cannot read plaintext from saved entity)
- Tenant-scoped binding requires ScopeId
- Instance-scoped binding rejects ScopeId

#### 3.18.7 `Event.Application.UnitTests/Features/SecretBindings/Queries/GetAvailableSecretsForOnboardingRequestHandlerTests.cs` (~3 tests)
- Returns all registry entries when no bindings exist
- Marks IsBound=true for keys with existing bindings
- Filters by scope correctly

#### 3.18.8 `Event.Application.UnitTests/Notifications/InvalidateSecretCacheOnUpdatedHandlerTests.cs` (~2 tests)
- Calls resolver.InvalidateAsync with correct key+tenantId
- Tenant scope passes ScopeId; Instance scope passes null

#### 3.18.9 `Event.Architecture.Tests/SecretsArchitectureTests.cs` (~3 tests)
- Domain.Secrets has no Infisical/DataProtection refs
- Application.Contracts.Secrets has no Persistence refs
- ISecretSource implementations all in Explore.Secrets namespace

#### 3.18.10 (DEFER to Phase 3.5 if time-pressed) `Event.API.IntegrationTests/Features/SecretBindings/SecretBindingsControllerTests.cs`
- Anonymous GET returns 401
- Admin GET list returns paginated HAL collection
- Admin POST creates binding
- Admin POST validate returns success after binding creation
- Non-admin user gets 403

---

### 3.19 — Verification (MANDATORY before commit)

Run in this exact order:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
```

**Acceptance gate**: All pass + new tests count ≥ 1,345 (1,305 baseline + ~40 new).

If integration tests added:
```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

---

### 3.20 — Commit

```bash
git add \
  Explore.Domain/Secrets/Events/ \
  Explore.Application/Contracts/Secrets/ \
  Explore.Application/Notifications/Secrets/ \
  Explore.Application/DTOs/SecretBindings/ \
  Explore.Application/Features/SecretBindings/ \
  Explore.Application/Mappings/SecretBindingProfile.cs \
  Explore.Application/Authorization/ResourceDescriptors.cs \
  Explore.Secrets/Sources/ \
  Explore.Secrets/Services/ \
  Explore.Secrets/Observability/ \
  Explore.Secrets/HealthChecks/ \
  Explore.Secrets/Infrastructure/ \
  Explore.Secrets/Extensions/ \
  Explore.API/Controllers/SecretBindingsController.cs \
  Explore.API/Hateoas/RouteNames.cs \
  Explore.API/Hateoas/Policies/SecretBindingLinkPolicy.cs \
  Explore.API/Hateoas/Assemblers/SecretBindingResourceAssembler.cs \
  Explore.API/Program.cs \
  Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs \
  cerbos/policies/secret_binding.yaml \
  cerbos/schemas/secret_binding.json \
  Explore.Secrets.UnitTests/Sources/ \
  Explore.Secrets.UnitTests/Services/ \
  Event.Application.UnitTests/Features/SecretBindings/ \
  Event.Application.UnitTests/Notifications/ \
  Event.Architecture.Tests/SecretsArchitectureTests.cs

git status   # confirm ONLY Phase 3 files staged

git commit -m "refactor(secrets): phase 3 introduce ISecretResolver + admin bindings API

Introduces single-source-of-truth secret resolution per SecretBinding row.
Each binding declares its source (Infisical | InlineEncrypted | EnvironmentVariable)
and the resolver dispatches to that source ONLY — no fallback chains.

Adds:
- ISecretResolver + per-source ISecretSource implementations
- DataProtection-backed InlineSecretSource (purpose Event.Secrets/Binding/v1)
- SecretResolver with IMemoryCache 5-min TTL + AuditingSecretResolverDecorator
- SecretResolverMetrics (OpenTelemetry) + SecretResolverHealthCheck
- CQRS Commands: Create/Update/Delete/Validate (BaseCommandResponse)
- CQRS Queries: List/Details/AvailableForOnboarding
- SecretBindingChangedNotification + cache invalidation handler
- /api/SecretBindings admin REST API with HAL + HATEOAS policies
- Cerbos policy (instance_admin all, tenant_admin scoped to own tenant)
- 40+ unit + architecture tests covering no-fallback and no-leak invariants"
```

**Do NOT push.** Stop and report.

---

### 3.21 — Update Dev-Docs (post-commit)

Edit `dev/active/secrets-refactor-control-plane/secrets-refactor-control-plane-context.md`:
- Mark Phase 3 ✅ COMMITTED with commit hash
- Update SESSION PROGRESS section
- Note any deviations from this plan

Edit `dev/active/secrets-refactor-control-plane/secrets-refactor-control-plane-tasks.md`:
- Mark all Phase 3 tasks `[x]` with phase header `✅ COMMITTED <hash>`
- Record final test count

Move/delete this `phase-3-implementation-plan.md` file (it has served its purpose) OR archive to `dev/_journal/`.

---

## Risk Register (Read Before Starting)

| Risk | Mitigation |
|---|---|
| `SecretBinding` property names differ from this plan | First action: open `Explore.Domain/Secrets/SecretBinding.cs` and adjust source files to match exact property names |
| AutoMapper profile location varies | Read `Explore.Application/Mappings/` first; either add new profile or extend existing |
| `IUnitOfWork` may not exist by that name | Search for `SaveAsync` / `SaveChangesAsync` patterns in existing handlers; use whatever pattern Categories handler uses |
| Cerbos derived role `tenant_admin` may differ | Check `cerbos/derived_roles/explore_admin_roles.yaml` for exact role name |
| Output cache eviction by tag may not be wired | If absent, fall back to time-based 30-sec invalidation (acceptable for admin UI) |
| Infisical SDK API surface changed | If `Infisical.Sdk` v3 doesn't match `GetSecretRawAsync`, consult docs via Context7 MCP, adapt the wrapper |
| `IDynamicAuthSchemeManager` not yet present | Stub the Keycloak refresh notification handler (log warning); Phase 4 wires real refresh |
| Test project namespace conventions | Check existing tests in `Event.Application.UnitTests/Features/Categories/` for namespace + base class patterns |
| Domain event vs MediatR INotification coupling | Keep domain event pure record in Domain; wrap with `SecretBindingChangedNotification : INotification` in Application layer |

## Estimated Scope

- **New files**: ~40
- **Modified files**: ~6
- **Net lines**: ~5,000–7,000 (production) + ~2,000 (tests)
- **Time budget for fresh session**: 4-6 hours of focused work
- **Tests added**: ~40-50

## Post-Phase-3 — Phase 4 Preview

Phase 4 will:
- Refactor `AuthProviderConfigurationService` to use `ISecretResolver`
- Wire `IDynamicAuthSchemeManager` to real Keycloak scheme refresh
- Update Onboarding UI (`AuthProviderConfiguration.razor`) to auto-detect bound vs unbound secrets
- Remove the `/internal` endpoint that exposed AppSetting reads
- Migrate existing AppSetting Keycloak rows to SecretBinding rows (data migration)

Do NOT touch any Phase 4 work in Phase 3. Phase 3 is purely additive — legacy paths still function.

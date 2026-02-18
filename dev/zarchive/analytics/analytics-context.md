# Pluggable Analytics System - Context

**Last Updated:** 2026-02-17 20:45 Europe/Brussels

---

## SESSION PROGRESS (2026-02-17)

### Completed This Session
- Re-aligned naming to repo convention and request: `AnalyticsProviderType` replaced with `AnalyticsProviderEnum`.
- Restored lookup-table entity and persistence wiring:
  - `Explore.Domain/AnalyticsProvider.cs`
  - `Explore.Persistence/Configurations/Entities/AnalyticsProviderConfiguration.cs`
  - `Explore.Persistence/ExploreDbContext.cs`
  - `Explore.Persistence/Seed/LookupTableSeeder.cs`
- Provider set finalized in enum and seeding:
  - `None = 0`, `Posthog = 1`, `Plausible = 2`, `Rybbit = 3`, `RudderStack = 4`.
- Runtime/infrastructure updated to include all providers:
  - `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
  - `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`
  - `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- Blazor bootstrap switched to provider string routing and supports `posthog`/`plausible`/`rybbit`/`rudderstack`:
  - `Explore.Blazor.Client/Components/AnalyticsInitializer.razor`
  - `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
- Removed stale constant `AnalyticsProviderId` from `Explore.Domain/Constants/GovernanceSettingKeys.cs`.
- Updated analytics-related tests to current contract and settings shape:
  - `Event.Application.UnitTests/Infrastructure/RuntimeAnalyticsProviderTests.cs`
  - `Event.Application.UnitTests/Infrastructure/AnalyticsConfigResolverTests.cs`
  - `Event.Application.UnitTests/Infrastructure/AnalyticsProviderEdgeCaseTests.cs`
  - `Event.Application.UnitTests/Infrastructure/NullAnalyticsProviderTests.cs`
  - `Event.Application.UnitTests/Features/PublicExperience/Queries/GetPublicExperienceSettingsQueryHandlerTests.cs`
  - `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs`

### Verification Executed
- `dotnet build --configuration Release --verbosity quiet` ran with warnings only (no new analytics-specific compile errors).
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` re-run after 2 failing assertions; assertions fixed and tests proceeded without analytics failures.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passed.

### Key Decisions This Session
- Keep settings key as string provider selector (`analytics.provider`) with allowed values, not integer provider id.
- Keep `AnalyticsProvider` lookup table for convention and extensibility while runtime resolution stays string-driven.
- Keep provider abstraction thin and resilient: provider failures never break business logic.

### Unfinished Work / Next Immediate Steps
1. Add/confirm EF migration artifacts for restored `AnalyticsProvider` lookup table where release workflow requires migrations.
2. Update docs (`docs/CONFIGURATION.md`, `CLAUDE.md`) with final provider list and key semantics.
3. Add integration-level provider-switch test (cache/TTL behavior).
4. Validate CSP rules for PostHog/Plausible/Rybbit/RudderStack script/connect hosts.

### Handoff Notes
- Last analytics-specific edits centered on provider wiring and test alignment.
- Exact files touched most recently:
  - `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`
  - `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
  - `Event.Application.UnitTests/Infrastructure/RuntimeAnalyticsProviderTests.cs`
- Resume commands:
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

---

## Key Architecture Decisions

### Decision 1: Follow RuntimeAuthorizationProvider Pattern (NOT Keyed Services)

**Why:** The codebase already has a production-proven pattern for runtime-switchable providers:
- `RuntimeAuthorizationProvider` delegates to `CerbosAuthorizationService` or `FallbackAuthorizationService`
- Resolution is based on `SystemSetting` with 1-minute `IMemoryCache` cache
- Falls back to safe default on errors

**Keyed Services** (.NET 8+) were considered but rejected because:
- The existing codebase doesn't use them anywhere
- Keyed services resolve at DI build time, not at runtime per-tenant
- Per-tenant provider switching requires runtime resolution from DB settings

### Decision 2: Use Cascading Settings for Configuration

**Why:** Analytics configuration naturally fits the existing 3-tier cascade:
```
SystemSetting (instance default: analytics.provider_id = 0)
    ↓ (if not locked and tenant override exists)
TenantSetting (tenant override: analytics.provider_id = 1)
    ↓ (if no override)
SystemSetting (fallback to default)
```

Instance admin can **lock** the provider choice for all tenants (SaaS-wide PostHog), or allow tenants to choose their own.

### Decision 3: PostHog as Primary, Instance-Level Key for MVP

**Why:** PostHog SDK registers `IPostHogClient` as a singleton. Creating per-tenant PostHog clients with different API keys adds significant complexity. For MVP:
- Use one PostHog project API key for the entire instance
- Differentiate tenants via PostHog `Group` analytics (tenant as a group)
- Defer per-tenant PostHog projects to a future iteration

### Decision 3b: Plausible Support Is API-First and Thin

**Why:** Plausible is a good self-hosted/lightweight option but does not expose the same capability surface as PostHog.
- Implement `Track` and `PageView` via Plausible Events API
- Do not force unsupported capabilities (feature flags, advanced identify semantics)
- Keep graceful no-op defaults for unsupported interface methods

### Decision 4: Analytics Failures Must NEVER Break Business Logic

**Why:** Analytics is non-critical infrastructure. All analytics calls must be:
- Wrapped in `try/catch` with `ILogger.LogWarning`
- Non-blocking dispatch via background queue/worker for potentially slow providers
- Fall back to `NullAnalyticsProvider` on any error
- Never awaited in the critical request path

### Decision 5: Feature Flags via Separate Interface

**Why:** Not all analytics providers support feature flags:
- PostHog: Full feature flag support
- Plausible: No feature flag support
- RudderStack: No feature flag support
- None: No feature flag support

Graceful degradation: `IAnalyticsFeatureFlagProvider.IsFeatureEnabledAsync()` returns `false` (or caller-provided default) for providers that don't support flags, preventing crashes when switching providers.

### Decision 6: RudderStack Must Avoid Process-Wide Static Singleton

**Why:** Static singleton initialization can leak tenant configuration in a multi-tenant request pipeline.
- Do not rely on global `RudderAnalytics.Initialize()` for tenant-specific keys
- Use provider-owned client lifecycle with per-config isolation
- Cache clients safely by config fingerprint if pooling is needed

### Decision 7: Blazor Must Bootstrap Analytics From Initial Settings Payload

**Why:** Avoid loading the wrong SDK on first render.
- Include `AnalyticsProviderEnum`, `AnalyticsEnabled`, `AnalyticsPublicApiKey`, `AnalyticsEndpointUrl` in the same initial payload used by public experience settings
- Initialize JS bridge only after this payload is loaded
- Missing/invalid config must degrade to no-op mode

### Decision 8: Consent Gate Is a First-Class Requirement

**Why:** EU/GDPR contexts require explicit control.
- Add `hasConsent` gating to tracking flow
- If consent is false, providers do not send events
- Keep behavior deterministic and testable across all providers

---

## Key Files (Existing - Read Before Implementation)

### DI Registration & Patterns

| File | Purpose | Why Read It |
|------|---------|-------------|
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Extension method for all infrastructure DI | Add analytics registration here. Follow comment style. |
| `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs` | Runtime-switchable provider wrapper | **PRIMARY REFERENCE.** Copy this pattern for RuntimeAnalyticsProvider. |
| `Explore.Infrastructure/Mail/SmtpConfigResolver.cs` | SMTP config resolver with cascading settings + cache | **SECONDARY REFERENCE** for AnalyticsConfigResolver. |
| `Explore.Infrastructure/Storage/S3ConfigResolver.cs` | S3 config resolver with cascading settings | Similar pattern to follow. |

### Domain Layer

| File | Purpose | Why Read It |
|------|---------|-------------|
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Canonical setting keys | Add analytics keys here. |
| `Explore.Domain/AnalyticsProvider.cs` | Lookup table entity pattern | Follow `ApprovalStatus`/`Madhab` lookup-table style for provider source of truth. |
| `Explore.Domain/SystemSetting.cs` | System setting entity | Understand IsLocked, AllowedValues, ValueType, Category. |
| `Explore.Domain/TenantSettings.cs` | Tenant settings entity (minimal) | Reference only — we're NOT adding fields here. |
| `Explore.Domain/Modules/TenantCapability.cs` | Module governance entity | ConfigurationJson pattern for module-specific config. |

### Application Layer

| File | Purpose | Why Read It |
|------|---------|-------------|
| `Explore.Application/Contracts/Infrastructure/IAuthorizationProvider.cs` | Provider interface pattern | Follow for IAnalyticsProvider interface design. |
| `Explore.Application/Contracts/Infrastructure/ISmtpConfigResolver.cs` | Config resolver interface | Follow for IAnalyticsConfigResolver. |
| `Explore.Application/Contracts/Infrastructure/IS3ConfigResolver.cs` | Config resolver interface | Follow for IAnalyticsConfigResolver. |
| `Explore.Application/Contracts/Infrastructure/IModuleService.cs` | Module governance contract | Reference for module-based analytics enablement. |
| `Explore.Application/Contracts/Strategies/IEventStrategy.cs` | Strategy interface pattern | Reference only — analytics doesn't use this pattern. |
| `Explore.Application/Models/` | POCO models directory | Place AnalyticsConfiguration here. |

### Infrastructure Layer

| File | Purpose | Why Read It |
|------|---------|-------------|
| `Explore.Infrastructure/Services/SettingsResolver.cs` | Cascading settings resolver | Core dependency for AnalyticsConfigResolver. |
| `Explore.Infrastructure/Services/CerbosAuthorizationService.cs` | Cerbos authorization provider | Example of a concrete provider implementation. |
| `Explore.Infrastructure/Strategies/IslamicEventStrategy.cs` | Concrete event strategy | Reference for module-specific implementation. |

---

## Key Files (To Create)

### Domain Layer
- `Explore.Domain/AnalyticsProvider.cs`

### Application Layer
- `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`
- `Explore.Application/Contracts/Infrastructure/IAnalyticsFeatureFlagProvider.cs`
- `Explore.Application/Contracts/Infrastructure/IAnalyticsConfigResolver.cs`
- `Explore.Application/Models/AnalyticsConfiguration.cs`

### Infrastructure Layer
- `Explore.Infrastructure/Analytics/NullAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
- `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`

### Blazor
- `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
- `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
- `Explore.Blazor.Client/Components/AnalyticsInitializer.razor`

---

## Essential Interface Signatures

### IAnalyticsProvider

```csharp
namespace Explore.Application.Contracts.Infrastructure;

public interface IAnalyticsProvider
{
    Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null, CancellationToken ct = default);
    Task TrackAsync(string distinctId, string eventName, IDictionary<string, object>? properties = null, bool hasConsent = true, CancellationToken ct = default);
    Task PageViewAsync(string distinctId, string pagePath, IDictionary<string, object>? properties = null, CancellationToken ct = default);
    Task GroupIdentifyAsync(string groupType, string groupKey, IDictionary<string, object>? properties = null, CancellationToken ct = default);
}
```

### IAnalyticsFeatureFlagProvider

```csharp
namespace Explore.Application.Contracts.Infrastructure;

public interface IAnalyticsFeatureFlagProvider
{
    Task<bool> IsFeatureEnabledAsync(string featureKey, string distinctId, bool defaultValue = false, CancellationToken ct = default);
    Task<object?> GetFeatureFlagPayloadAsync(string featureKey, string distinctId, CancellationToken ct = default);
}
```

### IAnalyticsConfigResolver

```csharp
namespace Explore.Application.Contracts.Infrastructure;

public interface IAnalyticsConfigResolver
{
    Task<AnalyticsConfiguration> ResolveAsync(CancellationToken ct = default);
    void InvalidateCache(Guid? tenantId = null);
}
```

### AnalyticsConfiguration

```csharp
namespace Explore.Application.Models;

public class AnalyticsConfiguration
{
    public int ProviderId { get; set; } = 0;
    public bool IsEnabled { get; set; }
    public string? ApiKey { get; set; }
    public string? EndpointUrl { get; set; }
    public string? PersonalApiKey { get; set; }
}
```

### API Enum Class (`AnalyticsProviderEnum`)

```csharp
namespace Explore.API;

public enum AnalyticsProviderEnum
{
    None = 0,
    PostHog = 1,
    Plausible = 2,
    RudderStack = 3
}
```

### RuntimeAnalyticsProvider (Core Pattern)

```csharp
// Follows RuntimeAuthorizationProvider pattern exactly
public sealed class RuntimeAnalyticsProvider : IAnalyticsProvider, IAnalyticsFeatureFlagProvider
{
    private readonly PostHogAnalyticsProvider _postHogProvider;
    private readonly PlausibleAnalyticsProvider _plausibleProvider;
    private readonly RudderStackAnalyticsProvider _rudderStackProvider;
    private readonly NullAnalyticsProvider _nullProvider;
    private readonly IAnalyticsConfigResolver _configResolver;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RuntimeAnalyticsProvider> _logger;

    private const string CacheKey = "AnalyticsProvider_Resolved";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private async Task<IAnalyticsProvider> ResolveProviderAsync(CancellationToken ct)
    {
        var config = await _configResolver.ResolveAsync(ct);
        if (!config.IsEnabled) return _nullProvider;

        return config.Provider switch
        {
            AnalyticsProviderEnum.PostHog => _postHogProvider,
            AnalyticsProviderEnum.Plausible => _plausibleProvider,
            AnalyticsProviderEnum.RudderStack => _rudderStackProvider,
            _ => _nullProvider
        };
    }
}
```

---

## PostHog .NET SDK Quick Reference

| Feature | API | Notes |
|---------|-----|-------|
| **Package** | `PostHog.AspNetCore` v2.2.3+ | Targets net8.0+ |
| **Init** | `builder.AddPostHog()` | Registers IPostHogClient as singleton |
| **Config** | `appsettings.json` → `PostHog:ProjectApiKey`, `PostHog:HostUrl` | Default host: `https://us.i.posthog.com` |
| **Capture** | `posthog.Capture(distinctId, eventName, properties)` | Sync, non-blocking, returns bool |
| **Identify** | `await posthog.IdentifyAsync(distinctId, properties)` | Async |
| **Page View** | `posthog.CapturePageView(distinctId, url)` | Sync |
| **Group** | `await posthog.GroupIdentifyAsync(type, key, name, properties)` | Async, max 5 group types |
| **Feature Flag** | `await posthog.IsFeatureEnabledAsync(key, distinctId)` | Async, needs PersonalApiKey for local eval |
| **Batching** | `FlushAt=20`, `FlushInterval=30s`, `MaxBatchSize=100` | Configurable |
| **Super Props** | `PostHog:SuperProperties` in config | Sent with every event |
| **Flush** | `await posthog.FlushAsync()` | Force immediate send |
| **Dispose** | `IAsyncDisposable` | Auto-flushes on dispose |

## RudderStack .NET SDK Quick Reference

| Feature | API | Notes |
|---------|-----|-------|
| **Package** | `RudderAnalytics` v2.0.0+ | Static singleton pattern |
| **Init** | `RudderAnalytics.Initialize(writeKey, new RudderConfig(dataPlaneUrl))` | Static |
| **Track** | `RudderAnalytics.Client.Track(userId, eventName, properties)` | Sync |
| **Identify** | `RudderAnalytics.Client.Identify(userId, traits)` | Sync |
| **Page** | `RudderAnalytics.Client.Page(userId, category, name)` | Sync |
| **Group** | `RudderAnalytics.Client.Group(userId, groupId, traits)` | Sync |
| **Flush** | `RudderAnalytics.Client.Flush()` | Sync |

## Plausible Quick Reference

| Feature | API | Notes |
|---------|-----|-------|
| **Event endpoint** | `POST /api/event` | Supports custom events and pageviews |
| **Self-hosted** | Configurable base URL | Keep endpoint in settings (`analytics.endpoint_url`) |
| **Auth** | Public domain/key based event payload | Do not send secrets to browser for server-side calls |
| **Feature flags** | N/A | Always graceful default (`false`) |

---

## Dependencies & Constraints

- **PostHog SDK targets net8.0**: .NET 10 project should be compatible (backward compatible), but verify with build
- **RudderStack SDK**: Verify .NET 10 compatibility (last published dates may be old)
- **Multi-tenant PostHog client pooling**: Deferred to future iteration. MVP uses instance-level API key.
- **Provider switch SLA**: Cache should allow switch to apply within 60 seconds.
- **RudderStack isolation**: Avoid process-wide static initialization for tenant-bound settings.
- **Blazor WASM JS interop**: PostHog JS SDK loads from CDN — verify CSP headers allow it

---

## External References (Validated)

- `https://posthog.com/docs/libraries/dotnet`
- `https://posthog.com/docs/feature-flags`
- `https://plausible.io/docs/events-api`
- `https://plausible.io/docs/custom-event-goals`
- `https://www.rudderstack.com/docs/api/http-api/`
- Context7: `/jbogard/mediatr`
- Context7: `/ardalis/cleanarchitecture`

---

## Quick Resume Instructions

To continue implementation:
1. Read this file for context and key decisions
2. Check `analytics-tasks.md` for current progress
3. Start with Phase 1 (Domain Layer) — it's the smallest and unblocks everything
4. Follow `RuntimeAuthorizationProvider.cs` as the primary reference for the switchable pattern
5. Follow `SmtpConfigResolver.cs` as the primary reference for config resolution

---

## Context Reset Handoff (2026-02-15 21:20 Europe/Brussels)

### What was being worked on when context limit approached
- Tightening analytics bootstrap/runtime safety and adding focused unit tests.

### Exact current state
- Analytics feature foundation is implemented and compiles.
- Application unit tests pass with analytics additions (`256 passed`).
- Solution builds successfully with warnings.
- Integration-level analytics switch tests and UI degradation tests are still pending.

### Commands to run on restart
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet`
- `dotnet build --configuration Release --no-restore /clp:ErrorsOnly`

### Temporary workarounds to replace later
- JS bridge currently uses direct CDN script loading; production CSP alignment is not yet documented/enforced in this task.

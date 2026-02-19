# Pluggable Analytics System - Implementation Plan

**Last Updated:** 2026-02-15

---

## Executive Summary

Implement a **multi-tenant, pluggable analytics system** that allows each tenant (or the instance admin) to choose their analytics provider - PostHog, Plausible, RudderStack, or None - without code changes. The system follows the existing **RuntimeAuthorizationProvider pattern** (runtime-switchable via cascading settings + cache) and integrates as a first-class module via the **TenantCapability** governance model.

**Key Design Principle:** **Thin Abstraction.** We abstract the *intent* ("User Signed Up", "Event Created") not every feature of every analytics tool. Provider-specific capabilities (e.g., PostHog feature flags) are exposed via optional interfaces, not forced into the common contract.

---

## Current State Analysis

### What Exists

| Component | Location | Status |
|-----------|----------|--------|
| **Cascading Settings Engine** | `ISettingsResolver` / `SettingsResolver` | Production-ready. System → Tenant with lock semantics. |
| **Module Governance** | `IModuleService` / `TenantCapability` | Production-ready. Enable/disable modules per tenant with JSONB config. |
| **GovernanceSettingKeys** | `Explore.Domain/Constants/GovernanceSettingKeys.cs` | 48 canonical keys. No analytics keys yet. |
| **Runtime Provider Switching** | `RuntimeAuthorizationProvider` | Production-ready. Delegates to Cerbos or Fallback via SystemSetting with 1-min cache. |
| **Strategy Pattern** | `IEventStrategy` / `StrategyResolver` | Production-ready. Module-keyed strategies filtered by tenant capabilities. |
| **Infrastructure DI Registration** | `InfrastructureServicesRegistration.cs` | 117 lines. Extension method pattern with comments. |
| **TenantSettings Entity** | `Explore.Domain/TenantSettings.cs` | Minimal (Id, TenantId, Tenant). No analytics fields. |
| **TenantCapability Entity** | `Explore.Domain/Modules/TenantCapability.cs` | Full entity with `ConfigurationJson` (JSONB), audit fields, module governance. |
| **SystemSetting Entity** | `Explore.Domain/SystemSetting.cs` | Full entity with IsLocked, AllowedValues, ValueType, Category, DisplayOrder. |

### What Does NOT Exist (Verified)

| Component | Action Required |
|-----------|----------------|
| `IAnalyticsProvider` interface | Create in `Explore.Application/Contracts/Infrastructure/` |
| `AnalyticsProvider` lookup table + API enum class | Create Domain lookup entity + API enum class with `Enum` suffix |
| Analytics setting keys | Add to `GovernanceSettingKeys.cs` |
| PostHog/Plausible/RudderStack provider implementations | Create in `Explore.Infrastructure/Analytics/` |
| `RuntimeAnalyticsProvider` (switchable wrapper) | Create in `Explore.Infrastructure/Analytics/` |
| `IAnalyticsConfigResolver` | Create in `Explore.Application/Contracts/Infrastructure/` |
| `AnalyticsConfigResolver` | Create in `Explore.Infrastructure/Analytics/` |
| Analytics DI registration | Add to `InfrastructureServicesRegistration.cs` |
| Blazor JS interop for client-side tracking | Create in `Explore.Blazor.Client/` |
| Seed data for analytics SystemSettings | Add to onboarding/seeder |

---

## Proposed Architecture

### Layer Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                           │
│  Explore.API          │  Explore.Blazor / Blazor.Client         │
│  - MediatR handlers   │  - AnalyticsInterop.razor component    │
│    inject              │  - analytics-bridge.js (JS interop)    │
│    IAnalyticsProvider  │  - Injects provider JS snippet         │
└────────────┬───────────┴────────────────────┬───────────────────┘
             │                                │
┌────────────▼────────────────────────────────▼───────────────────┐
│                    APPLICATION LAYER                             │
│  Explore.Application                                            │
│  - IAnalyticsProvider (interface)                               │
│  - IAnalyticsConfigResolver (interface)                         │
│  - AnalyticsEvent record (domain event data)                    │
│  - MediatR behavior: AnalyticsTrackingBehavior (optional)       │
└────────────┬────────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────────┐
│                    DOMAIN LAYER                                  │
│  Explore.Domain                                                 │
│  - AnalyticsProvider lookup table (None, PostHog, Plausible, RudderStack) │
│  - GovernanceSettingKeys.Analytics* constants                   │
└─────────────────────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                          │
│  Explore.Infrastructure/Analytics/                              │
│  - RuntimeAnalyticsProvider (IAnalyticsProvider)                 │
│    ├── PostHogAnalyticsProvider                                 │
│    ├── PlausibleAnalyticsProvider                               │
│    ├── RudderStackAnalyticsProvider                             │
│    └── NullAnalyticsProvider                                    │
│  - AnalyticsConfigResolver (IAnalyticsConfigResolver)           │
│  - AnalyticsConfiguration (POCO for deserialized JSONB)         │
└─────────────────────────────────────────────────────────────────┘
```

### Provider Resolution Flow

```
Request arrives
    │
    ▼
RuntimeAnalyticsProvider.TrackAsync(...)
    │
    ├─ 1. Check IMemoryCache for resolved provider (1-min TTL)
    │
    ├─ 2. Cache miss → IAnalyticsConfigResolver.ResolveAsync()
    │     │
    │     ├─ Read SystemSetting "analytics.provider_id" (instance default)
    │     ├─ Read TenantSetting override (if exists & not locked)
    │     ├─ Read TenantCapability "Mod_Analytics" ConfigurationJson
    │     └─ Return AnalyticsConfiguration { Provider, ApiKey, EndpointUrl }
    │
    ├─ 3. Switch on Provider:
    │     ├─ PostHog → PostHogAnalyticsProvider (IPostHogClient)
    │     ├─ Plausible → PlausibleAnalyticsProvider
    │     ├─ RudderStack → RudderStackAnalyticsProvider
    │     └─ None → NullAnalyticsProvider
    │
    └─ 4. Delegate call to resolved provider
```

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Where to put interface** | `Application/Contracts/Infrastructure/` | Follows IEmailService, IS3ConfigResolver, IAuthorizationProvider pattern |
| **Where to put implementations** | `Infrastructure/Analytics/` | New subfolder, follows `Infrastructure/Mail/`, `Infrastructure/Storage/` pattern |
| **Configuration storage** | Cascading Settings (SystemSetting → TenantSetting) + TenantCapability ConfigurationJson | Follows SMTP, S3 pattern. Instance admin can lock provider choice. |
| **Provider resolution** | RuntimeAnalyticsProvider with IMemoryCache | Follows RuntimeAuthorizationProvider pattern exactly |
| **PostHog SDK** | `PostHog.AspNetCore` NuGet package (v2.2.3+) | Official SDK with DI support, feature flags, .NET Feature Management, batching |
| **Plausible provider** | Thin HTTP client to Plausible Events API | Plausible has no server-side feature flags; keep event/page tracking only and treat feature flags as unsupported capability. |
| **RudderStack SDK lifecycle** | Avoid static global `RudderAnalytics.Initialize()` | Use provider-owned client wrapper/factory keyed by tenant config to prevent cross-tenant key leakage. |
| **Feature flags** | Optional capability via `IAnalyticsFeatureFlagProvider` | Not all providers support flags. PostHog does; Plausible/RudderStack do not. Safe default returns `false` (or provided default value), never throws. |
| **Enum class location** | `Explore.API` enum class `AnalyticsProviderEnum` in `AnalyticsProviderEnum.cs` | Matches existing repo enum naming (e.g., `ApprovalStatusEnum`) while source of truth remains lookup table rows |
| **Delivery model** | Non-blocking + bounded background queue | Analytics never blocks business flows; enqueue quickly and process with retries/timeouts/structured warning logs. |
| **DI lifetime** | Scoped runtime resolver + singleton background dispatcher | Matches existing infrastructure style while supporting robust throughput and backpressure. |
| **No Keyed Services** | Runtime resolution via settings + cache | Codebase convention. Keyed services don't support per-tenant switching. |

---

## Implementation Phases

### Phase 1: Domain Layer (Effort: S)

**Goal:** Define the analytics vocabulary and setting keys.

#### Task 1.1: Create Analytics Provider Lookup Table + API Enum Class
- **File:** `Explore.Domain/AnalyticsProvider.cs`, `Explore.API/*/AnalyticsProviderEnum.cs` (final API enum location to match existing API enum organization)
- **Acceptance Criteria:**
  - [ ] Lookup table entity `AnalyticsProvider` has `Id`, `MasterCode`, `FullName`, `Description` (same style as `ApprovalStatus`, `Madhab`)
  - [ ] API enum class is named `AnalyticsProviderEnum`
  - [ ] API enum values map to lookup IDs: `None = 0`, `PostHog = 1`, `Plausible = 2`, `RudderStack = 3`
  - [ ] ABOUTME comment at top
- **Effort:** S
- **Dependencies:** None
- **Related Skills:** `clean-architecture-rules`

#### Task 1.2: Add Analytics Setting Keys to `GovernanceSettingKeys`
- **File:** `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Acceptance Criteria:**
  - [ ] Add `AnalyticsProviderId = "analytics.provider_id"` (integer lookup FK, no hardcoded provider strings)
  - [ ] Add `AnalyticsEnabled = "analytics.enabled"` (boolean)
  - [ ] Add `AnalyticsApiKey = "analytics.api_key"` (string, lockable for SaaS-wide key)
  - [ ] Add `AnalyticsEndpointUrl = "analytics.endpoint_url"` (string, for self-hosted)
  - [ ] Add `AnalyticsPersonalApiKey = "analytics.personal_api_key"` (string, for feature flags)
  - [ ] Grouped with comment `// Analytics` matching existing style
- **Effort:** S
- **Dependencies:** None
- **Related Skills:** `clean-architecture-rules`

---

### Phase 2: Application Layer (Effort: M)

**Goal:** Define the analytics contracts that the Infrastructure layer will implement.

#### Task 2.1: Create `IAnalyticsProvider` Interface
- **File:** `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`
- **Acceptance Criteria:**
  - [ ] Methods:
    - `Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null, CancellationToken ct = default)`
    - `Task TrackAsync(string distinctId, string eventName, IDictionary<string, object>? properties = null, bool hasConsent = true, CancellationToken ct = default)`
    - `Task PageViewAsync(string distinctId, string pagePath, IDictionary<string, object>? properties = null, CancellationToken ct = default)`
    - `Task GroupIdentifyAsync(string groupType, string groupKey, IDictionary<string, object>? properties = null, CancellationToken ct = default)`
  - [ ] ABOUTME comments, XML docs, CancellationToken on all methods
  - [ ] File-scoped namespace: `namespace Explore.Application.Contracts.Infrastructure;`
  - [ ] No provider-specific types in the interface (thin abstraction)
- **Effort:** S
- **Dependencies:** None
- **Related Skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`

#### Task 2.2: Create `IAnalyticsFeatureFlagProvider` Interface (Optional Capability)
- **File:** `Explore.Application/Contracts/Infrastructure/IAnalyticsFeatureFlagProvider.cs`
- **Acceptance Criteria:**
  - [ ] Methods:
    - `Task<bool> IsFeatureEnabledAsync(string featureKey, string distinctId, bool defaultValue = false, CancellationToken ct = default)`
    - `Task<object?> GetFeatureFlagPayloadAsync(string featureKey, string distinctId, CancellationToken ct = default)`
  - [ ] Separate interface - not all providers support feature flags
  - [ ] Unsupported providers return safe defaults (`false` / `defaultValue`, `null` payload) and never throw
  - [ ] XML docs explaining graceful degradation
- **Effort:** S
- **Dependencies:** None
- **Related Skills:** `clean-architecture-rules`

#### Task 2.3: Create `IAnalyticsConfigResolver` Interface
- **File:** `Explore.Application/Contracts/Infrastructure/IAnalyticsConfigResolver.cs`
- **Acceptance Criteria:**
  - [ ] Methods:
    - `Task<AnalyticsConfiguration> ResolveAsync(CancellationToken ct = default)`
    - `void InvalidateCache(Guid? tenantId = null)`
  - [ ] Follows `ISmtpConfigResolver` and `IS3ConfigResolver` pattern exactly
- **Effort:** S
- **Dependencies:** Task 2.1
- **Related Skills:** `clean-architecture-rules`

#### Task 2.4: Create `AnalyticsConfiguration` POCO
- **File:** `Explore.Application/Models/AnalyticsConfiguration.cs`
- **Acceptance Criteria:**
  - [ ] Properties:
    - `int ProviderId` (lookup id)
    - `bool IsEnabled`
    - `string? ApiKey`
    - `string? EndpointUrl`
    - `string? PersonalApiKey` (for feature flags)
  - [ ] Located in `Explore.Application/Models/` (follows `SmtpConfiguration` pattern)
  - [ ] Nullable strings for optional fields
- **Effort:** S
- **Dependencies:** Task 1.1
- **Related Skills:** `clean-architecture-rules`

---

### Phase 3: Infrastructure Layer — Providers (Effort: L)

**Goal:** Implement concrete analytics providers and the runtime switcher.

#### Task 3.1: Create `NullAnalyticsProvider`
- **File:** `Explore.Infrastructure/Analytics/NullAnalyticsProvider.cs`
- **Acceptance Criteria:**
  - [ ] Implements `IAnalyticsProvider` AND `IAnalyticsFeatureFlagProvider`
  - [ ] All methods are no-ops (return `Task.CompletedTask` or safe defaults)
  - [ ] `IsFeatureEnabledAsync` returns `false` (safe default — feature disabled)
  - [ ] Logged at `Debug` level: "Analytics disabled: {EventName} not tracked"
  - [ ] ABOUTME comment
- **Effort:** S
- **Dependencies:** Task 2.1, Task 2.2
- **Related Skills:** `clean-architecture-rules`

#### Task 3.2: Create `PostHogAnalyticsProvider`
- **File:** `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
- **Acceptance Criteria:**
  - [ ] Implements `IAnalyticsProvider` AND `IAnalyticsFeatureFlagProvider`
  - [ ] Wraps `IPostHogClient` from `PostHog.AspNetCore` NuGet package
  - [ ] `IdentifyAsync` → `posthog.IdentifyAsync(distinctId, userProperties)`
  - [ ] `TrackAsync` → `posthog.CaptureAsync(distinctId, eventName, properties)`
  - [ ] `PageViewAsync` → `posthog.CaptureAsync(distinctId, "$pageview", { "$current_url": pagePath })`
  - [ ] `GroupIdentifyAsync` → `posthog.GroupIdentifyAsync(groupType, groupKey, properties)`
  - [ ] `IsFeatureEnabledAsync` → `posthog.IsFeatureEnabledAsync(featureKey, distinctId)`
  - [ ] `GetFeatureFlagPayloadAsync` → `posthog.GetFeatureFlagPayloadAsync(featureKey, distinctId)`
  - [ ] Error handling: catch + log + swallow (analytics failures must NEVER break business logic)
  - [ ] ABOUTME comment
- **Effort:** M
- **Dependencies:** Task 2.1, Task 2.2, PostHog NuGet package
- **Related Skills:** `clean-architecture-rules`, `error-tracking`

#### Task 3.3: Create `RudderStackAnalyticsProvider`
- **File:** `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`
- **Acceptance Criteria:**
  - [ ] Implements `IAnalyticsProvider` only (no feature flags)
  - [ ] Wraps RudderStack .NET SDK (`RudderAnalytics` NuGet) through a provider-owned client wrapper
  - [ ] `IdentifyAsync` -> Rudder client identify
  - [ ] `TrackAsync` -> Rudder client track
  - [ ] `PageViewAsync` -> Rudder client page
  - [ ] `GroupIdentifyAsync` -> Rudder client group
  - [ ] Does NOT use global static singleton state shared across tenants
  - [ ] Supports both Cloud and Self-Hosted via `dataPlaneUrl` configuration
  - [ ] Error handling: catch + log + swallow
  - [ ] ABOUTME comment
- **Effort:** M
- **Dependencies:** Task 2.1, RudderStack NuGet package
- **Related Skills:** `clean-architecture-rules`, `error-tracking`

#### Task 3.4: Create `PlausibleAnalyticsProvider`
- **File:** `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
- **Acceptance Criteria:**
  - [ ] Implements `IAnalyticsProvider` only (no feature flags)
  - [ ] Uses typed `HttpClient` to call Plausible Events API
  - [ ] `TrackAsync` maps to Plausible custom event payload
  - [ ] `PageViewAsync` maps to Plausible pageview payload
  - [ ] If `hasConsent` is false, method exits early without network call
  - [ ] Supports self-hosted Plausible via configurable endpoint
  - [ ] Error handling: catch + log + swallow
  - [ ] ABOUTME comment
- **Effort:** M
- **Dependencies:** Task 2.1
- **Related Skills:** `clean-architecture-rules`, `error-tracking`

#### Task 3.5: Create `AnalyticsConfigResolver`
- **File:** `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
- **Acceptance Criteria:**
  - [ ] Implements `IAnalyticsConfigResolver`
  - [ ] Follows `SmtpConfigResolver` pattern: inject `ISettingsResolver`, `ITenantContext`, `IMemoryCache`
  - [ ] Resolves analytics config from cascading settings (System → Tenant)
  - [ ] Cache key includes TenantId: `$"AnalyticsConfig_{tenantId}"`
  - [ ] Cache duration: 1 minute (matches RuntimeAuthorizationProvider switching expectations)
  - [ ] `InvalidateCache` removes cached config for specific tenant or all
  - [ ] Falls back to `AnalyticsProviderEnum.None` (id `0`) if no setting exists
  - [ ] ABOUTME comment
- **Effort:** M
- **Dependencies:** Task 1.2, Task 2.3, Task 2.4
- **Related Skills:** `clean-architecture-rules`

#### Task 3.6: Create `RuntimeAnalyticsProvider`
- **File:** `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
- **Acceptance Criteria:**
  - [ ] Implements `IAnalyticsProvider` AND `IAnalyticsFeatureFlagProvider`
  - [ ] Follows `RuntimeAuthorizationProvider` pattern exactly:
    - Inject all concrete providers + `IAnalyticsConfigResolver` + `IMemoryCache` + `ILogger`
    - `ResolveProviderAsync()` uses cache + config resolver to pick the right provider
    - `ResolveProviderAsync()` supports: PostHog, Plausible, RudderStack, None
    - All interface methods delegate to resolved provider
    - Error handling: catch provider errors, log warning, fall back to NullAnalyticsProvider
  - [ ] `IAnalyticsFeatureFlagProvider` methods: check if resolved provider implements the interface, delegate if so, return safe default if not
  - [ ] `TrackAsync(... hasConsent)` short-circuits to no-op when consent is not granted
  - [ ] ABOUTME comment
- **Effort:** M
- **Dependencies:** Task 3.1, Task 3.2, Task 3.3, Task 3.4, Task 3.5
- **Related Skills:** `clean-architecture-rules`, `error-tracking`

#### Task 3.7: Register Analytics Services in DI
- **File:** `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- **Acceptance Criteria:**
  - [ ] Add PostHog SDK registration: conditional based on config
  - [ ] Add typed `HttpClient` registration for Plausible provider
  - [ ] Add RudderStack provider-owned client wrapper registration (no process-wide static initialize)
  - [ ] Register all concrete providers as Scoped
  - [ ] Register analytics background dispatcher/queue (bounded channel) for non-blocking delivery
  - [ ] Register `IAnalyticsConfigResolver` as Scoped
  - [ ] Register `IAnalyticsProvider` → `RuntimeAnalyticsProvider` as Scoped
  - [ ] Register `IAnalyticsFeatureFlagProvider` → `RuntimeAnalyticsProvider` as Scoped
  - [ ] Grouped with comment: `// Analytics providers (runtime-switchable via SystemSetting "analytics.provider_id")`
  - [ ] Follows existing registration style with descriptive comments
- **Effort:** S
- **Dependencies:** Task 3.6
- **Related Skills:** `clean-architecture-rules`

---

### Phase 4: Configuration & Seed Data (Effort: M)

**Goal:** Add analytics settings to the configuration pipeline and onboarding.

#### Task 4.1: Add Analytics SystemSetting Seed Data
- **File:** Onboarding handler or database seeder (verify location in codebase)
- **Acceptance Criteria:**
  - [ ] Seed/ensure `analytics.provider_id` with value `0`, category `"Analytics"`, ValueType `Integer`, IsLocked `false`
  - [ ] Seed `AnalyticsProvider` lookup table rows via lookup seeder pattern (no hardcoded provider strings in resolver logic)
  - [ ] Seed `analytics.enabled` with value `"false"`, ValueType `Boolean`
  - [ ] Seed `analytics.api_key` with value `""`, ValueType `String`, IsLocked `false`
  - [ ] Seed `analytics.endpoint_url` with value `""`, ValueType `String`
  - [ ] Seed `analytics.personal_api_key` with value `""`, ValueType `String`
  - [ ] DisplayOrder set to group Analytics settings together
- **Effort:** S
- **Dependencies:** Task 1.2
- **Related Skills:** `dotnet-efcore-guidelines`

#### Task 4.2: Add `appsettings.json` Configuration Section (Optional)
- **File:** `Explore.API/appsettings.json`, `Explore.Blazor/appsettings.json`
- **Acceptance Criteria:**
  - [ ] Add `"Analytics"` section with `Provider`, `ApiKey`, `EndpointUrl` for bootstrap/fallback
  - [ ] Document that DB-based SystemSetting takes precedence
  - [ ] This is only used as a fallback before onboarding is complete
- **Effort:** S
- **Dependencies:** None

#### Task 4.3: Extend Public Experience Payload for Analytics Bootstrap
- **File:** `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`, `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`, `Explore.Blazor.Client/Services/PublicExperienceService.cs`
- **Acceptance Criteria:**
  - [ ] Include `AnalyticsProviderEnum`, `AnalyticsEnabled`, `AnalyticsPublicApiKey`, and `AnalyticsEndpointUrl` in the initial settings payload
  - [ ] Values come from resolved tenant-effective analytics settings, not hardcoded appsettings
  - [ ] Missing/invalid keys degrade to no-op client mode (no SDK load)
- **Effort:** M
- **Dependencies:** Task 3.5

---

### Phase 5: Blazor Frontend Integration (Effort: M)

**Goal:** Enable client-side event tracking from Blazor WASM via JS interop.

#### Task 5.1: Create `analytics-bridge.js`
- **File:** `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
- **Acceptance Criteria:**
  - [ ] `initAnalytics(providerType, apiKey, endpointUrl)` - dynamically loads provider JS SDK
  - [ ] `trackEvent(eventName, properties)` — delegates to loaded provider
  - [ ] `identifyUser(distinctId, traits)` — delegates to loaded provider
  - [ ] `trackPageView(pagePath)` — delegates to loaded provider
  - [ ] Handles "none" provider gracefully (no-op)
  - [ ] PostHog: loads `posthog-js` via CDN, initializes with `posthog.init(apiKey, { api_host: endpointUrl })`
  - [ ] Plausible: loads plausible script only when provider is Plausible and public key/domain config exists
  - [ ] RudderStack: loads `rudder-analytics.js` via CDN
- **Effort:** M
- **Dependencies:** None
- **Related Skills:** `blazor-ui-conventions`

#### Task 5.2: Create `AnalyticsInterop` Blazor Service
- **File:** `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
- **Acceptance Criteria:**
  - [ ] Injectable service wrapping `IJSRuntime` calls to `analytics-bridge.js`
  - [ ] Methods mirror `IAnalyticsProvider`: `TrackAsync`, `IdentifyAsync`, `PageViewAsync`
  - [ ] Initialized on app startup with provider config from server payload (`/api/PublicExperience/settings`)
  - [ ] Error handling: catch JS interop failures, log, swallow
- **Effort:** S
- **Dependencies:** Task 5.1
- **Related Skills:** `blazor-ui-conventions`, `blazor-bff-patterns`

#### Task 5.3: Create `AnalyticsInitializer` Component
- **File:** `Explore.Blazor.Client/Components/AnalyticsInitializer.razor`
- **Acceptance Criteria:**
  - [ ] Renders in `MainLayout.razor` (or `App.razor`)
  - [ ] On `OnAfterRenderAsync(firstRender)`, calls `AnalyticsInterop.InitAsync(config)`
  - [ ] Gets analytics config from initial settings payload to avoid first-load race
  - [ ] Only loads JS SDK if provider != None
- **Effort:** S
- **Dependencies:** Task 5.2
- **Related Skills:** `blazor-ui-conventions`

#### Task 5.4: Add Consent-Aware Client Behavior
- **File:** `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`, `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
- **Acceptance Criteria:**
  - [ ] Bridge accepts consent state before emitting events
  - [ ] No event/page/identify call is sent when consent is false
  - [ ] Consent transitions (opt-in/opt-out) are handled without app reload
- **Effort:** S
- **Dependencies:** Task 5.1, Task 5.2

---

### Phase 6: Testing (Effort: L)

**Goal:** Comprehensive test coverage for all analytics components.

#### Task 6.1: Unit Tests — NullAnalyticsProvider
- **File:** `Event.Application.UnitTests/Analytics/NullAnalyticsProviderTests.cs`
- **Acceptance Criteria:**
  - [ ] All methods return without throwing
  - [ ] `IsFeatureEnabledAsync` returns `false`
- **Effort:** S
- **Dependencies:** Task 3.1

#### Task 6.2: Unit Tests — RuntimeAnalyticsProvider
- **File:** `Event.Application.UnitTests/Analytics/RuntimeAnalyticsProviderTests.cs`
- **Acceptance Criteria:**
  - [ ] Resolves PostHog provider when setting = "posthog"
  - [ ] Resolves RudderStack provider when setting = "rudderstack"
  - [ ] Resolves Null provider when setting = "none" or missing
  - [ ] Falls back to Null on provider error
  - [ ] Caches resolved provider
- **Effort:** M
- **Dependencies:** Task 3.5

#### Task 6.3: Unit Tests — AnalyticsConfigResolver
- **File:** `Event.Application.UnitTests/Analytics/AnalyticsConfigResolverTests.cs`
- **Acceptance Criteria:**
  - [ ] Resolves from SystemSetting
  - [ ] Tenant override takes precedence when not locked
  - [ ] System locked setting cannot be overridden
  - [ ] Cache invalidation works
  - [ ] Returns None when no settings exist
- **Effort:** M
- **Dependencies:** Task 3.4

#### Task 6.4: Unit Tests — PostHogAnalyticsProvider
- **File:** `Event.Application.UnitTests/Analytics/PostHogAnalyticsProviderTests.cs`
- **Acceptance Criteria:**
  - [ ] Track calls delegate to IPostHogClient
  - [ ] Identify calls delegate correctly
  - [ ] Feature flag calls delegate correctly
  - [ ] Errors are caught and logged, not thrown
- **Effort:** M
- **Dependencies:** Task 3.2

#### Task 6.5: Architecture Tests
- **File:** `Event.Architecture.Tests/AnalyticsArchitectureTests.cs`
- **Acceptance Criteria:**
  - [ ] `IAnalyticsProvider` is in Application layer
  - [ ] Provider implementations are in Infrastructure layer
  - [ ] No Infrastructure references from Domain or Application
  - [ ] Analytics lookup table entity is in Domain layer
  - [ ] API enum class `AnalyticsProviderEnum` exists in API layer
- **Effort:** S
- **Dependencies:** All previous phases

#### Task 6.6: Integration Test - "The Provider Switch"
- **File:** `Event.API.IntegrationTests/Analytics/AnalyticsProviderSwitchTests.cs`
- **Acceptance Criteria:**
  - [ ] Start with provider = PostHog and verify tracking calls are routed to PostHog provider
  - [ ] Update DB setting to provider = None
  - [ ] Verify `RuntimeAnalyticsProvider` stops dispatching analytics within 60 seconds (cache TTL)

#### Task 6.7: UI Graceful Degradation
- **File:** `Explore.Blazor.Client.Tests/Analytics/AnalyticsBootstrapTests.cs`
- **Acceptance Criteria:**
  - [ ] UI does not throw or flicker when analytics API key is missing
  - [ ] UI does not throw or flicker when analytics key is invalid
  - [ ] Client remains interactive and tracking bridge stays in no-op mode

#### Task 6.8: Consent Compliance Regression
- **File:** `Event.Application.UnitTests/Analytics/AnalyticsConsentTests.cs`
- **Acceptance Criteria:**
  - [ ] `TrackAsync` with `hasConsent=false` does not call provider SDK/HTTP client
  - [ ] Providers remain functional when consent is true

---

### Phase 7: Documentation (Effort: S)

#### Task 7.1: Update `docs/CONFIGURATION.md`
- Add Analytics section with SystemSetting keys, configuration examples, and self-hosted setup

#### Task 7.2: Update `CLAUDE.md` or relevant docs
- Add analytics to the technology stack table
- Reference analytics skills/patterns

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **PostHog .NET SDK is pre-release** | High | Medium | SDK is actively maintained by PostHog team, targets net8.0+. Pin to specific version. Wrap all calls in try/catch. |
| **RudderStack static singleton misuse** | Medium | High | Never use process-wide static initialization in multi-tenant mode. Use provider-owned clients keyed by tenant config and isolate lifecycle in DI. |
| **Plausible capability mismatch** | High | Low | Keep Plausible support intentionally thin (track/page only), no feature flags, and safe default behavior for unsupported calls. |
| **Analytics failures breaking business logic** | Low | Critical | **All analytics calls are fire-and-forget with try/catch.** Never await analytics in the critical path. Never let analytics exceptions propagate. |
| **Per-tenant PostHog client instances** | Medium | Medium | PostHog SDK is designed as a singleton. For multi-tenant with different API keys, may need client pooling or per-request client creation. Investigate SDK support for dynamic keys. |
| **JS SDK version conflicts** | Low | Low | Load provider JS SDKs dynamically only when needed. Pin CDN versions. |
| **Cache invalidation race conditions** | Low | Low | Use `IMemoryCache` with absolute expiration. Worst case: stale config for up to 60 seconds after admin changes provider. |
| **Consent compliance regressions** | Medium | High | Add consent gate to tracking contracts and regression tests for opt-out behavior. |

---

## Potential Risks & Unknowns

**The most likely point of complexity is per-tenant PostHog client management.** The PostHog .NET SDK (`PostHog.AspNetCore`) registers `IPostHogClient` as a singleton via `builder.AddPostHog()`. This works perfectly for a single-tenant deployment where one API key serves the entire instance. However, in a multi-tenant SaaS deployment where each tenant may have their own PostHog project (different API key + host), we cannot use the built-in DI registration. Instead, we'll need to either:

1. **Use `PostHogClient` directly** (bypass DI singleton) and create instances per-tenant with caching, OR
2. **Use the instance-level PostHog key** and differentiate tenants via PostHog groups/properties (simpler but less isolated)

The recommended approach is **option 2 for MVP** (instance-level key with tenant as a group property) and defer per-tenant PostHog projects to a future iteration. This avoids the complexity of managing multiple SDK client instances.

A secondary risk is that the **RudderStack .NET SDK** is singleton-oriented and may encourage global initialization patterns. The implementation should avoid global static state and keep tenant configuration isolation at the provider boundary.

For **Plausible**, the integration is API-first and intentionally narrow. It should remain a thin adapter (events/pageviews) without trying to emulate provider features that do not exist (feature flags, complex user profiles).

---

## Success Metrics

| Metric | Target |
|--------|--------|
| All analytics calls are fire-and-forget | No analytics failure can break business logic |
| Provider switch takes effect within 60 seconds | Via cache expiration |
| Zero-downtime provider switching | No restart required |
| Tenant can choose provider independently | Via TenantSetting override (if not locked) |
| Feature flags gracefully degrade | Returns `false` when provider doesn't support flags |
| Consent gate enforced | No tracking emitted when consent is false |
| Blazor first-load bootstrap is race-safe | Provider and key are known before SDK init |
| All tests pass | Unit + Architecture tests green |
| Build succeeds | `dotnet build --configuration Release` clean |

---

## NuGet Packages Required

| Package | Version | Purpose |
|---------|---------|---------|
| `PostHog.AspNetCore` | 2.2.3+ | PostHog .NET SDK with ASP.NET Core DI, feature flags, .NET Feature Management |
| `PostHog` | 2.2.3+ | Core PostHog library (dependency of AspNetCore package, targets netstandard2.1 + net8.0) |
| `RudderAnalytics` | 2.0.0+ | RudderStack .NET SDK (use provider-owned client lifecycle; avoid global singleton init) |

`Plausible` integration is HTTP-based (no required server SDK package).

---

## External Validation Sources

- PostHog .NET SDK docs: `https://posthog.com/docs/libraries/dotnet`
- PostHog feature flags docs: `https://posthog.com/docs/feature-flags`
- Plausible events API docs: `https://plausible.io/docs/events-api`
- Plausible custom events docs: `https://plausible.io/docs/custom-event-goals`
- RudderStack docs (HTTP API + identify/event model): `https://www.rudderstack.com/docs/api/http-api/`
- MediatR CQRS/pipeline guidance (Context7): `/jbogard/mediatr`
- Clean Architecture dependency guidance (Context7): `/ardalis/cleanarchitecture`

---

## Effort Estimates

| Phase | Effort | Time Estimate |
|-------|--------|---------------|
| Phase 1: Domain Layer | S | 30 min |
| Phase 2: Application Layer | M | 1-2 hours |
| Phase 3: Infrastructure Layer | L | 4-6 hours |
| Phase 4: Configuration & Seed Data | M | 1-2 hours |
| Phase 5: Blazor Frontend | M | 2-3 hours |
| Phase 6: Testing | L | 3-4 hours |
| Phase 7: Documentation | S | 30 min |
| **Total** | **XL** | **~12-18 hours** |

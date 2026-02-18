# Pluggable Analytics System - Task Checklist

**Last Updated:** 2026-02-17 20:45 Europe/Brussels

---

## Phase 1: Domain Layer (Effort: S) ✅ COMPLETE

- [x] ✅ **Task 1.1:** Create `AnalyticsProvider` lookup table + enum `AnalyticsProviderEnum`
  - Files: `Explore.Domain/AnalyticsProvider.cs`, `Explore.Domain/Enums/AnalyticsProviderEnum.cs`
    - Values: `None = 0`, `Posthog = 1`, `Plausible = 2`, `Rybbit = 3`, `RudderStack = 4`
  - Follow `ApprovalStatus`/`Madhab` lookup-table pattern; enum class uses `Enum` suffix
  - Dependencies: None

- [x] ✅ **Task 1.2:** Add analytics setting keys to `GovernanceSettingKeys`
  - File: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
  - Keys: `analytics.provider`, `analytics.enabled`, `analytics.api_key`, `analytics.endpoint_url`, `analytics.personal_api_key`
  - `analytics.provider` is string selector with allowed values (`none`, `posthog`, `plausible`, `rybbit`, `rudderstack`)
  - Grouped under `// Analytics` comment
  - Dependencies: None

---

## Phase 2: Application Layer (Effort: M) ✅ COMPLETE

- [x] ✅ **Task 2.1:** Create `IAnalyticsProvider` interface
  - File: `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`
  - Methods: `IdentifyAsync`, `TrackAsync(...)`, `PageViewAsync`, `GroupIdentifyAsync`
  - ABOUTME, XML docs, CancellationToken on all methods, no provider-specific types
  - Dependencies: None

- [x] ✅ **Task 2.2:** Create `IAnalyticsFeatureFlagProvider` interface
  - File: `Explore.Application/Contracts/Infrastructure/IAnalyticsFeatureFlagProvider.cs`
  - Methods: `IsFeatureEnabledAsync(...)`, `GetFeatureFlagPayloadAsync`
  - Separate interface (not all providers support flags), XML docs for graceful degradation
  - Unsupported providers return safe defaults and NEVER throw `NotImplementedException`
  - Dependencies: None

- [x] ✅ **Task 2.3:** Create `IAnalyticsConfigResolver` interface
  - File: `Explore.Application/Contracts/Infrastructure/IAnalyticsConfigResolver.cs`
  - Methods: `ResolveAsync`, `InvalidateCache`
  - Follows `ISmtpConfigResolver` / `IS3ConfigResolver` pattern
  - Dependencies: Task 2.1

- [x] ✅ **Task 2.4:** Create `AnalyticsConfiguration` POCO
  - File: `Explore.Application/Models/AnalyticsConfiguration.cs`
  - Properties: `Provider` (`AnalyticsProviderEnum`), `IsEnabled`, `ApiKey?`, `EndpointUrl?`, `PersonalApiKey?`
  - Follows `SmtpConfiguration` pattern
  - Dependencies: Task 1.1

---

## Phase 3: Infrastructure Layer - Providers (Effort: L) ✅ COMPLETE

- [x] ✅ **Task 3.1:** Create `NullAnalyticsProvider`
  - File: `Explore.Infrastructure/Analytics/NullAnalyticsProvider.cs`
  - Implements both `IAnalyticsProvider` and `IAnalyticsFeatureFlagProvider`
  - All methods are no-ops, `IsFeatureEnabledAsync` returns `false`
  - Debug-level logging
  - Dependencies: Task 2.1, Task 2.2

- [x] ✅ **Task 3.2:** Create `PostHogAnalyticsProvider`
  - File: `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
  - Implements both `IAnalyticsProvider` and `IAnalyticsFeatureFlagProvider`
  - Wraps `IPostHogClient` from `PostHog.AspNetCore` (v2.2.3+)
  - Error handling: catch + log + swallow (analytics NEVER breaks business logic)
  - Dependencies: Task 2.1, Task 2.2, PostHog NuGet

- [x] ✅ **Task 3.3:** Create `RudderStackAnalyticsProvider`
  - File: `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`
  - Implements `IAnalyticsProvider` only (no feature flags)
  - Wraps `RudderAnalytics` NuGet (v2.0.0+), supports Cloud + Self-Hosted via dataPlaneUrl
  - Use provider-owned client lifecycle; DO NOT use process-wide static singleton shared across tenants
  - Error handling: catch + log + swallow
  - Dependencies: Task 2.1, RudderStack NuGet

- [x] ✅ **Task 3.4:** Create `PlausibleAnalyticsProvider`
  - File: `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
  - Implements `IAnalyticsProvider` only (no feature flags)
  - Uses Plausible Events API (`/api/event`) via typed `HttpClient`
  - `TrackAsync` no-op when provider is not active or configuration is incomplete
  - Supports self-hosted Plausible endpoint
  - Error handling: catch + log + swallow
  - Dependencies: Task 2.1

- [x] ✅ **Task 3.5:** Create `AnalyticsConfigResolver`
  - File: `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
  - Implements `IAnalyticsConfigResolver`
  - Follows `SmtpConfigResolver` pattern: `ISettingsResolver` + `ITenantContext` + `IMemoryCache`
  - Cache key includes TenantId, falls back to `None`
  - Dependencies: Task 1.2, Task 2.3, Task 2.4

- [x] ✅ **Task 3.6:** Create `RuntimeAnalyticsProvider`
  - File: `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
  - Implements both `IAnalyticsProvider` and `IAnalyticsFeatureFlagProvider`
  - Follows `RuntimeAuthorizationProvider` pattern exactly (cache + config resolver + delegate)
  - Supports provider switch: PostHog / Plausible / Rybbit / RudderStack / None
  - Feature flag methods check if resolved provider implements interface, safe default if not
  - Error handling: fall back to `NullAnalyticsProvider` on provider errors
  - Dependencies: Task 3.1, Task 3.2, Task 3.3, Task 3.4, Task 3.5

- [x] ✅ **Task 3.7:** Register analytics services in DI
  - File: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
  - Register all concrete providers, config resolver, `IAnalyticsProvider` + `IAnalyticsFeatureFlagProvider` -> `RuntimeAnalyticsProvider` (all Scoped)
  - Add typed `HttpClient` for Plausible, Rybbit, and RudderStack
  - Grouped with descriptive comment
  - Dependencies: Task 3.6

---

## Phase 4: Configuration & Seed Data (Effort: M) 🟡 IN PROGRESS

- [x] ✅ **Task 4.1:** Add analytics `SystemSetting` seed data
  - File: Onboarding handler or database seeder (verify location)
  - Seed: `analytics.provider` (`"none"`), `analytics.enabled` (`false`), `analytics.api_key`, `analytics.endpoint_url`, `analytics.personal_api_key`
  - Seed `AnalyticsProvider` lookup rows in lookup-table seeder pattern
  - Dependencies: Task 1.2

- [ ] **Task 4.2:** Add `appsettings.json` configuration section (optional)
  - Files: `Explore.API/appsettings.json`, `Explore.Blazor/appsettings.json`
  - Add `"Analytics"` section as bootstrap/fallback (DB SystemSetting takes precedence)
  - Dependencies: None

- [x] ✅ **Task 4.3:** Extend initial public settings payload for analytics bootstrap
  - Files: `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`, `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`, `Explore.Blazor.Client/Services/PublicExperienceService.cs`
  - Include `AnalyticsProvider` (string), `AnalyticsEnabled`, `AnalyticsPublicApiKey`, `AnalyticsEndpointUrl`
  - Ensure missing/invalid config degrades to no-op client mode
  - Dependencies: Task 3.5

---

## Phase 5: Blazor Frontend Integration (Effort: M) ✅ COMPLETE

- [x] ✅ **Task 5.1:** Create `analytics-bridge.js`
  - File: `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
  - Functions: `initAnalytics`, `trackEvent`, `identifyUser`, `trackPageView`
  - Dynamically loads PostHog JS SDK, Plausible script, Rybbit script, or RudderStack JS SDK
  - Handles "none" provider gracefully (no-op)
  - Dependencies: None

- [x] ✅ **Task 5.2:** Create `AnalyticsInterop` Blazor service
  - File: `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
  - Wraps `IJSRuntime` calls to `analytics-bridge.js`
  - Methods mirror `IAnalyticsProvider`, error handling: catch JS interop failures
  - Initialize from initial server payload to avoid first-load race
  - Dependencies: Task 5.1

- [x] ✅ **Task 5.3:** Create `AnalyticsInitializer` component
  - File: `Explore.Blazor.Client/Components/AnalyticsInitializer.razor`
  - Renders in `MainLayout.razor`, initializes on `OnAfterRenderAsync(firstRender)`
  - Only loads JS SDK if provider != None
  - Must not flicker/crash when key missing or invalid
  - Dependencies: Task 5.2

- [x] ✅ **Task 5.4:** Add consent-aware client analytics behavior
  - Files: `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`, `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
  - No tracking emitted when provider is disabled/none/missing key
  - Dependencies: Task 5.1, Task 5.2

---

## Phase 6: Testing (Effort: L) 🟡 IN PROGRESS

- [x] ✅ **Task 6.1:** Unit tests - `NullAnalyticsProvider`
  - File: `Event.Application.UnitTests/Analytics/NullAnalyticsProviderTests.cs`
  - All methods return without throwing, `IsFeatureEnabledAsync` returns `false`
  - Dependencies: Task 3.1

- [x] ✅ **Task 6.2:** Unit tests - `RuntimeAnalyticsProvider`
  - File: `Event.Application.UnitTests/Analytics/RuntimeAnalyticsProviderTests.cs`
  - Resolves correct provider per setting, falls back to Null on error, caches resolved provider
  - Dependencies: Task 3.5

- [x] ✅ **Task 6.3:** Unit tests - `AnalyticsConfigResolver`
  - File: `Event.Application.UnitTests/Analytics/AnalyticsConfigResolverTests.cs`
  - SystemSetting resolution, tenant override, lock semantics, cache invalidation, default to None
  - Dependencies: Task 3.4

- [ ] **Task 6.4:** Unit tests - `PostHogAnalyticsProvider`
  - File: `Event.Application.UnitTests/Analytics/PostHogAnalyticsProviderTests.cs`
  - Track/Identify/FeatureFlag delegation, error catch + log + swallow
  - Dependencies: Task 3.2

- [ ] **Task 6.5:** Architecture tests
  - File: `Event.Architecture.Tests/AnalyticsArchitectureTests.cs`
  - Interface in Application layer, implementations in Infrastructure, no cross-layer violations
  - Dependencies: All previous phases

- [ ] **Task 6.6:** Integration Test - "The Provider Switch"
  - Verify changing DB setting from PostHog to None stops dispatching within 60 seconds (1-min cache)

- [ ] **Task 6.7:** UI Graceful Degradation
  - Verify Blazor UI does not flicker/crash when analytics API key is missing or invalid

- [ ] **Task 6.8:** Provider Degradation Regression
  - Verify disabled or missing-key config prevents network calls to PostHog/Plausible/Rybbit/RudderStack

---

## Phase 7: Documentation (Effort: S) 🟡 IN PROGRESS

- [ ] **Task 7.1:** Update `docs/CONFIGURATION.md`
  - Add Analytics section with SystemSetting keys, config examples, self-hosted setup

- [ ] **Task 7.2:** Update `CLAUDE.md` or relevant docs

---

## Session Discoveries / New Tasks (2026-02-15)

- [x] ✅ **Task 6.9 (New):** Add unit tests for provider-specific edge paths
  - Cover `PostHogAnalyticsProvider` feature-flag payload parsing and non-success HTTP behavior.
  - Cover `PlausibleAnalyticsProvider` API-key-required active-state behavior.

- [x] ✅ **Task 6.10 (New):** Add Blazor client tests for analytics bootstrap degradation
  - Verify initializer/interop flow remains stable when `AnalyticsEnabled=true` but key missing.
  - Verify no exception/flicker path under missing endpoint or unsupported provider values.

- [ ] **Task 4.4 (New):** Confirm EF migration artifact for analytics lookup/settings
  - Ensure schema migration exists and is applied in deployment/test workflows.

- [ ] **Task 7.3 (New):** Add CSP guidance for analytics script hosts
  - Document required `script-src`/`connect-src` host entries for PostHog/Plausible/Rybbit/RudderStack.
  - Add analytics to technology stack table, reference analytics patterns

- [x] ✅ **Task 6.11 (New):** Fix prerender DI resolution for `AnalyticsInitializer`
  - Added server-side no-op `IAnalyticsInterop` implementation for SSR.
  - Registered `IAnalyticsInterop` in `Explore.Blazor/Program.cs` so `InteractiveAuto` prerender can resolve component injection.

- [ ] **Task 6.12 (New):** Re-run host-level build after unrelated `OrganizationRoleId` compile errors are resolved
  - Current host build failure is blocked by non-analytics compile issues in `Explore.Blazor.Client` files.

---

## Quick Resume

**To continue implementation:**
1. Read `analytics-plan.md` for full architecture details and acceptance criteria
2. Read `analytics-context.md` for key decisions, interface signatures, and SDK references
3. Start with Phase 1 (Domain Layer) — smallest scope, no dependencies
4. Each phase builds on the previous; follow the dependency chain in order

**Key patterns to follow:**
- `RuntimeAuthorizationProvider` → model for `RuntimeAnalyticsProvider`
- `ISmtpConfigResolver` / `IS3ConfigResolver` → model for `IAnalyticsConfigResolver`
- `GovernanceSettingKeys` → where to add analytics constants
- `InfrastructureServicesRegistration.cs` → where to register DI

**NuGet packages to add:**
- `PostHog.AspNetCore` (v2.2.3+)
- `RudderAnalytics` (v2.0.0+)

**No required server SDK packages:**
- `Plausible` (use Events API via typed `HttpClient`)
- `Rybbit` (use HTTP track API via typed `HttpClient`)
- [x] ✅ **Task 3.4b:** Create `RybbitAnalyticsProvider`
  - File: `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs`
  - Implements `IAnalyticsProvider` only (no feature flags)
  - Uses Rybbit HTTP track endpoint (`/api/track`) with provider-safe no-op fallback
  - Dependencies: Task 2.1

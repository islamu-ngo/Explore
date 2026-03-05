ABOUTME: Working context log for analytics abstraction roadmap execution.
ABOUTME: Tracks verified files, decisions, progress, and immediate resume steps.

# Analytics Abstraction Roadmap - Context

Last Updated: 2026-03-05

## SESSION PROGRESS (2026-03-05)

### COMPLETED

- Verified existing analytics abstraction across Application/Infrastructure/Blazor using direct code search and reads.
- Gathered external provider evidence for PostHog, Plausible, and Rybbit using Tavily-backed research and specialist runs.
- Confirmed runtime provider resolution flow and existing test coverage.
- Created planning artifact: `dev/active/analytics-abstraction-roadmap/analytics-abstraction-roadmap-plan.md`.

### IN PROGRESS

- Consolidating actionable execution checklist and phased task sequencing.

### BLOCKERS

- None currently blocking planning.

## Key Verified Files

### Application contracts and models

- `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`
- `Explore.Application/Contracts/Infrastructure/IAnalyticsFeatureFlagProvider.cs`
- `Explore.Application/Contracts/Infrastructure/IAnalyticsConfigResolver.cs`
- `Explore.Application/Models/AnalyticsConfiguration.cs`
- `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs`

### Domain enums and setting definitions

- `Explore.Domain/Enums/AnalyticsProviderEnum.cs`
- `Explore.Domain/Settings/Definitions/AnalyticsSettingDefinitions.cs`
- `Explore.Domain/Constants/GovernanceSettingKeys.cs`

### Infrastructure implementations

- `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
- `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/NullAnalyticsProvider.cs`
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`

### Blazor frontend and interop

- `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor`
- `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
- `Explore.Blazor.Client/Contracts/Interop/IAnalyticsInterop.cs`
- `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
- `Explore.Blazor.Client/Layout/MainLayout.razor`

### Tests

- `Event.Application.UnitTests/Infrastructure/RuntimeAnalyticsProviderTests.cs`
- `Event.Application.UnitTests/Infrastructure/AnalyticsConfigResolverTests.cs`
- `Event.Application.UnitTests/Infrastructure/AnalyticsProviderEdgeCaseTests.cs`
- `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs`

## Important Decisions

1. Keep analytics separate from OTel/Grafana observability concerns.
2. Evolve existing runtime abstraction incrementally rather than replacing it.
3. Use capability-gated behavior for provider-specific features.
4. Prioritize fixing key mismatch (`analytics.endpoint_url` alignment) before adding new provider logic.

## External Evidence References

- PostHog .NET + JS docs:
  - https://posthog.com/docs/libraries/dotnet
  - https://posthog.com/docs/libraries/js
- Plausible Events API:
  - https://plausible.io/docs/events-api
- Rybbit docs:
  - https://rybbit.com/docs

## Technical Constraints to Respect

- Clean Architecture: contracts in Application, implementations in Infrastructure, UI interop in Blazor client/server adapters.
- No analytics failure may break business command/query paths.
- Tenant-safe config resolution must stay centralized in settings resolvers.

## Quick Resume

1. Read `dev/active/analytics-abstraction-roadmap/analytics-abstraction-roadmap-plan.md`.
2. Start with Task A (settings key alignment) in tasks checklist.
3. Implement and test each phase incrementally with existing unit and bUnit suites.

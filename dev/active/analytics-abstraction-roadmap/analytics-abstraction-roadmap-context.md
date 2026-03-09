ABOUTME: Working context log for the analytics abstraction roadmap refresh.
ABOUTME: Captures verified implementation state, research findings, decisions, and the next execution entry points.

# Analytics Abstraction Roadmap - Context

Last Updated: 2026-03-08

## SESSION PROGRESS (2026-03-08)

### COMPLETED

- Re-read the active roadmap docs and repo planning conventions.
- Re-read core project docs relevant to this roadmap refresh:
  - `docs/PROJECT.md`
  - `docs/ARCHITECTURE.md`
  - `docs/CONFIGURATION.md`
  - `docs/OPERATIONS.md`
  - `docs/MULTI_TENANCY.md`
  - `docs/LOCALIZATION.md`
- Verified the existing analytics abstraction is already implemented across Domain, Application, Infrastructure, Blazor, and tests.
- Confirmed the main initial internal correctness gap was the settings-key mismatch in `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs`.
- Gathered external evidence showing the roadmap needed a stronger self-hosting emphasis around proxying, CSP, ad blockers, privacy defaults, and capability mismatch.
- Updated all three roadmap artifacts to reflect a hardening plan instead of a greenfield build plan.
- Implemented Phase 0 Task A1 by aligning `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs` with canonical analytics governance keys from `GovernanceSettingKeys.Analytics`.
- Added `Event.Application.UnitTests/Settings/AnalyticsSettingGroupTests.cs` to lock in canonical key usage, canonical population behavior, and rejection of legacy `analytics.endpoint` / `analytics.site_id` inputs.
- Verified changed files with clean LSP diagnostics.
- Re-ran `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` successfully: 385/385 passing.
- Implemented Phase 1 foundation for analytics governance and privacy:
  - added `analytics.consent_mode` to governance keys and setting definitions;
  - added `AnalyticsConsentMode`, a canonical analytics event catalog, and `AnalyticsGovernanceService` in the Application layer;
  - switched `SaveTenantOnboardingStepCommandHandler` to emit through the shared governance layer rather than raw ad hoc payload construction;
  - exposed consent mode and identify capability in public bootstrap DTOs/models and threaded them into the Blazor analytics bridge init path.
- Added Phase 1 test coverage in:
  - `Event.Application.UnitTests/Services/AnalyticsGovernanceServiceTests.cs`
  - `Event.Application.UnitTests/Features/TenantOnboarding/Commands/SaveTenantOnboardingStepCommandHandlerTests.cs`
  - updated `Event.Application.UnitTests/Infrastructure/AnalyticsConfigResolverTests.cs`
  - updated `Event.Application.UnitTests/Features/PublicExperience/Queries/GetPublicExperienceSettingsQueryHandlerTests.cs`
  - updated `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs`
- Verified Phase 1 changed files with clean LSP diagnostics.
- Re-ran verification sequentially after stopping locked `dotnet` processes:
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed: 79/79.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 389/389.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` still has 3 pre-existing unrelated failures (`NavMenu_ShowsUserProfile_WhenAuthenticated`, `CreateEvent_OnLastStep_ShowsCreateButton`, `CreateEvent_Initially_ShowsNextButton_AndHidesCreateButton`).
- Implemented Phase 2 transport hardening in code:
  - added explicit `direct` / `proxy` / `relay` transport-mode threading through config resolution, public bootstrap, and Blazor interop;
  - added first-party relay ingestion through `Explore.API/Controllers/AnalyticsRelayController.cs` and `RelayAnalyticsEventCommandHandler`;
  - changed the default relay browser path to opaque `POST /api/a/t` to reduce blocker-friendly naming;
  - fixed public bootstrap readiness so `relay` mode remains enabled without a browser-facing analytics API key;
  - aligned the JS relay bridge with the server contract by emitting pageview/custom events only;
  - added a dedicated `AnalyticsRelay` fixed-window rate limit policy for the anonymous relay endpoint.
- Added/updated Phase 2 verification coverage in:
  - `Event.Application.UnitTests/Features/PublicExperience/Commands/RelayAnalyticsEventCommandHandlerTests.cs`
  - `Event.Application.UnitTests/Features/PublicExperience/Queries/GetPublicExperienceSettingsQueryHandlerTests.cs`
- Re-ran verification after the Phase 2 fixes:
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed: 391/391.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` still shows the same 3 pre-existing unrelated failures.
- Updated transport/runbook docs to close the Phase 2 documentation gap:
  - `docs/CONFIGURATION.md`
  - `docs/OPERATIONS.md`
  - `docs/BLAZOR.md`
  - `docs/API.md`
- Completed Phase 3 capability-positioning research using both internal repo inspection and fresh official-doc research:
  - confirmed `PostHog` is the only currently validated rich provider in this set with native feature flags and documented server/browser parity;
  - confirmed `Plausible` intentionally excludes identify/group semantics and should stay the lightweight tier;
  - confirmed current `Rybbit` repo behavior (server track/pageview only) is more conservative than its browser docs, and official docs do not justify server/group/feature-flag parity;
  - confirmed `RudderStack` supports the broader event-spec surface in code/docs but is best positioned operationally as a CDP/router rather than a direct analytics-backend replacement.
- Updated Phase 3-facing docs and roadmap artifacts to reflect that evidence:
  - `docs/CODEBASE_INSIGHTS.md`
  - `docs/OPERATIONS.md`
  - `dev/active/analytics-abstraction-roadmap/analytics-abstraction-roadmap-plan.md`
  - `dev/active/analytics-abstraction-roadmap/analytics-abstraction-roadmap-tasks.md`
  - clarified lowest-common-denominator semantics in `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`.
- Completed Phase 4 Blazor/BFF integration hardening:
  - `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor` now owns automatic pageview tracking after successful analytics initialization;
  - pageviews are emitted on initial load and `NavigationManager.LocationChanged`, use normalized path-only routes, and include `navigation_source`, `tenant_id`, and prior-path `page_referrer` when available;
  - disabled analytics no longer subscribe to navigation tracking;
  - `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs` now verifies initial pageview, disabled no-pageview behavior, and programmatic navigation referrer behavior.
- Re-ran verification after the Phase 4 implementation:
  - `dotnet build --configuration Release --verbosity quiet` passed.
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity minimal --report-trx --report-trx-filename analytics-client-results.trx` passed for the new analytics tests; the project still has the same 3 unrelated pre-existing failures (`NavMenu_ShowsUserProfile_WhenAuthenticated`, `CreateEvent_OnLastStep_ShowsCreateButton`, `CreateEvent_Initially_ShowsNextButton_AndHidesCreateButton`).
- Completed Phase 4/5 contract and operator-documentation pass:
  - `docs/BLAZOR.md` now documents analytics readiness rules, `AnalyticsInitializer` pageview ownership, and client-vs-server event boundaries;
  - `docs/OPERATIONS.md` now includes analytics runbook guidance for enable/disable, provider switching, proxy/relay verification, CSP/ad-block triage, and the explicit outbox deferral decision;
  - `docs/TROUBLESHOOTING.md` now includes analytics-specific triage steps;
  - `Explore.Blazor.Client/Pages/User/Components/SettingsPrivacy.razor` now reflects the future analytics/privacy UX contract in its placeholder copy.

### IN PROGRESS

- All roadmap phases are now implemented for this hardening pass; remaining work is only future follow-up if buffered/outbox delivery is promoted from deferred to committed scope.

### BLOCKERS

- No blockers for planning.
- No active blockers after the current hardening pass.

## Verified Current Architecture

### Domain and governance

- `Explore.Domain/AnalyticsProvider.cs`
- `Explore.Domain/Enums/AnalyticsProviderEnum.cs`
- `Explore.Domain/Settings/Definitions/AnalyticsSettingDefinitions.cs`
- `Explore.Domain/Constants/GovernanceSettingKeys.cs`

### Application layer

- `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`
- `Explore.Application/Contracts/Infrastructure/IAnalyticsFeatureFlagProvider.cs`
- `Explore.Application/Contracts/Infrastructure/IAnalyticsConfigResolver.cs`
- `Explore.Application/Models/AnalyticsConfiguration.cs`
- `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs`

### Infrastructure layer

- `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
- `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`
- `Explore.Infrastructure/Analytics/NullAnalyticsProvider.cs`
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`

### Blazor and bootstrap

- `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor`
- `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
- `Explore.Blazor.Client/Contracts/Interop/IAnalyticsInterop.cs`
- `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
- `Explore.Blazor/Services/ServerAnalyticsInterop.cs`
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`
- `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`

### Tests

- `Event.Application.UnitTests/Infrastructure/RuntimeAnalyticsProviderTests.cs`
- `Event.Application.UnitTests/Infrastructure/AnalyticsConfigResolverTests.cs`
- `Event.Application.UnitTests/Infrastructure/AnalyticsProviderEdgeCaseTests.cs`
- `Event.Application.UnitTests/Infrastructure/NullAnalyticsProviderTests.cs`
- `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs`

## Key Findings That Changed The Plan

1. The repo already follows the right structural pattern for this problem. The localization/TMS system in `docs/LOCALIZATION.md` is the strongest internal reference for multi-provider external-service abstraction with self-hoster tiers.
2. The current roadmap understated self-hosting realities. Proxying, CSP, ad blockers, and disable-by-default deployments are not edge cases here; they are normal operator scenarios.
3. The base analytics contract should stay thin. Existing journal guidance confirms this and also warns against stale provider-id coupling inside concrete providers.
4. Public bootstrap already computes analytics readiness before client initialization. The plan should build on that instead of redesigning the bootstrap flow.
5. The UI already hints at future privacy controls in `Explore.Blazor.Client/Pages/User/Components/SettingsPrivacy.razor`, but no backend/client consent model exists yet. That gap now needs explicit roadmap coverage.

## Important Decisions In This Refresh

1. Reposition the roadmap from "build abstraction" to "harden the existing abstraction."
2. Treat disabled analytics as a first-class deployment tier, not a fallback footnote.
3. Separate lightweight web analytics from richer product analytics in roadmap language and tasking.
4. Make proxy-first and first-party transport guidance a core workstream, not an operational appendix.
5. Make privacy-safe defaults and property governance precede deeper provider parity work.
6. Keep OpenTelemetry/business metrics explicitly separate from this roadmap.

## Specific Gaps To Address During Execution

### Correctness gaps

- `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs` has been corrected to use canonical keys.
- The follow-up audit found no remaining stale runtime references; only intentional historical mentions in active roadmap docs, archive docs, and the regression test remain.

### Contract and governance gaps

- No canonical event catalog or naming policy.
- Initial canonical event catalog now exists for onboarding and shared property keys.
- Initial property allowlist/PII filtering now exists in `AnalyticsGovernanceService`.
- Initial consent model now exists (`anonymous`, `pseudonymous`, `identified`) and is exposed to the public bootstrap path.

### Self-hosting and transport gaps

- Explicit transport modes now exist in config/bootstrap: `direct`, `proxy`, `relay`.
- The public bootstrap path now keeps analytics enabled for `relay` mode even when no browser-facing API key is present.
- Browser relay transport uses first-party opaque path `POST /api/a/t` and has its own fixed-window API rate limit.
- JS bridge degradation is now documented: direct/proxy script failures fall back to no-op; relay mode avoids provider script loading entirely.

### Capability gaps

- Plausible is intentionally limited, but the roadmap previously treated parity as the target.
- Rybbit docs show a richer client/proxy surface than the current provider uses.
- RudderStack exists in code but is under-documented in the current plan relative to rollout expectations.

Capability status update after Phase 3 research:

- `PostHog` remains the richest validated provider and the only one in this set with native feature flags.
- `Plausible` should remain a deliberate lightweight tier with documented no-op identify/group behavior.
- `Rybbit` should not be promoted beyond server track/pageview until official docs justify deeper server-side support.
- `RudderStack` should be described as advanced/pipeline-oriented rather than a direct substitute for analytics backends with native dashboards/flags.

## External Evidence References

- PostHog JS docs: `https://posthog.com/docs/libraries/js`
- PostHog proxy reference: `https://posthog.com/docs/advanced/proxy/proxy-reference`
- PostHog self-hosting: `https://posthog.com/docs/self-host`
- PostHog behind proxy: `https://posthog.com/docs/self-host/configure/running-behind-proxy`
- Plausible proxy/ad-block docs: `https://plausible.io/docs/proxy/introduction`
- Plausible Events API: `https://plausible.io/docs/events-api`
- Plausible custom property guidance: `https://plausible.io/docs/custom-props/introduction`
- Rybbit script docs: `https://rybbit.com/docs/script`
- Rybbit proxy docs: `https://rybbit.com/docs/proxy-guide`
- Rybbit architecture docs: `https://rybbit.com/docs/architecture`
- Rybbit self-hosting docs: `https://rybbit.com/docs/self-hosting`

## Build / Validation Notes

- `dotnet build --configuration Release --verbosity quiet` succeeded during this planning session.
- The build still emits many pre-existing warnings outside this roadmap scope; none were introduced by this documentation update.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed after the A1 implementation change.
- Runtime-facing docs now publish the canonical analytics keys, provider capability tiers, and self-hoster deployment tiers in `docs/CONFIGURATION.md`, `docs/CODEBASE_INSIGHTS.md`, and `docs/OPERATIONS.md`.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity minimal --report-trx --report-trx-filename analytics-client-results.trx` now fails only with the same 3 unrelated pre-existing tests; the new analytics initializer coverage passes.

## Quick Resume

1. Read `dev/active/analytics-abstraction-roadmap/analytics-abstraction-roadmap-plan.md`.
2. Treat the roadmap hardening pass as complete through Phase 5.
3. Use `docs/LOCALIZATION.md` as the main internal pattern reference if a future follow-up adds deeper provider features or buffered delivery.
4. If work resumes here, the next explicit decision is whether to promote deferred outbox delivery into a committed milestone.

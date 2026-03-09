ABOUTME: Execution checklist for hardening the existing analytics abstraction for self-hostable deployments.
ABOUTME: Tracks the next implementation work with explicit emphasis on governance, privacy, transport, and honest provider capabilities.

# Analytics Abstraction Roadmap - Tasks

Last Updated: 2026-03-08

## Phase 0 - Re-baseline the Existing Abstraction

- [x] Task A1: Fix `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs` to use canonical analytics governance keys.
  - Acceptance: `analytics.endpoint_url` is used consistently; no roadmap or code references depend on `analytics.endpoint` or phantom `analytics.site_id` for runtime analytics config.
  - Evidence: `AnalyticsSettingGroup` now uses `GovernanceSettingKeys.Analytics.*`; `Event.Application.UnitTests/Settings/AnalyticsSettingGroupTests.cs` passes; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed (385/385).
  - Dependencies: none.
  - Effort: 2-4h.
- [x] Task A2: Publish a verified provider/capability matrix for `None`, `PostHog`, `Plausible`, `Rybbit`, and `RudderStack`.
  - Acceptance: matrix distinguishes core vs optional capabilities and explicitly marks no-op behavior.
  - Evidence: provider capability tiers published in `docs/CODEBASE_INSIGHTS.md` and `docs/OPERATIONS.md`; content cross-checked against runtime providers and resolver behavior.
  - Dependencies: A1.
  - Effort: 1-2h.
- [x] Task A3: Publish deployment tiers for disabled, lightweight, richer, and proxy-first analytics modes.
  - Acceptance: roadmap/docs clearly support self-hosters who do not want heavy analytics infrastructure.
  - Evidence: self-hoster deployment tiers published in `docs/OPERATIONS.md`; canonical key and operator configuration guidance published in `docs/CONFIGURATION.md`.
  - Dependencies: A2.
  - Effort: 1-2h.

## Phase 1 - Event Contract, Privacy, and Governance Baseline

- [x] Task B1: Define a canonical event taxonomy and ownership model for client vs server emitters.
  - Acceptance: event names, event categories, and emitter responsibilities are documented and ready for typed implementation.
  - Evidence: canonical onboarding event catalog + shared property keys implemented in `Explore.Application/Analytics/AnalyticsEvents.cs`; current server emitter now uses the shared definition.
  - Dependencies: A2.
  - Effort: 3-5h.
- [x] Task B2: Define property governance rules including PII exclusion and allowed dimensions.
  - Acceptance: allowlist/denylist guidance exists; provider-specific constraints such as Plausible custom property limits are accounted for.
  - Evidence: `Explore.Application/Services/AnalyticsGovernanceService.cs` enforces event-specific allowlists plus sensitive-key filtering; `Event.Application.UnitTests/Services/AnalyticsGovernanceServiceTests.cs` covers sanitization.
  - Dependencies: B1.
  - Effort: 2-4h.
- [x] Task B3: Define privacy modes and consent gates for anonymous, pseudonymous, and identified tracking.
  - Acceptance: identify/group calls are policy-gated instead of being silently assumed valid everywhere.
  - Evidence: `analytics.consent_mode` added to governance settings; `AnalyticsGovernanceService` gates identify/group support; public bootstrap now includes `AnalyticsConsentMode` and `AnalyticsAllowIdentify`.
  - Dependencies: B1-B2.
  - Effort: 3-5h.

## Phase 2 - Transport and Self-Hosting Hardening

- [x] Task C1: Define transport modes: direct vendor script, proxied first-party path, and server relay fallback.
  - Acceptance: architecture docs describe when each mode is used and what operators must configure.
  - Evidence: `analytics.transport_mode` now resolves through `AnalyticsConfigResolver`, flows through `GetPublicExperienceSettingsQueryHandler`, `PublicExperienceSettingsDto`, `AnalyticsInitializer`, and `analytics-bridge.js`; docs updated in `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `docs/BLAZOR.md`.
  - Dependencies: A3, B3.
  - Effort: 3-5h.
- [x] Task C2: Add reverse proxy and CSP guidance for self-hosters.
  - Acceptance: runbook covers path naming, host/header forwarding, HTTPS assumptions, and CSP/script-host requirements.
  - Evidence: operator guidance added to `docs/OPERATIONS.md`; API rate-limit + endpoint notes added to `docs/API.md`; config notes updated in `docs/CONFIGURATION.md`.
  - Dependencies: C1.
  - Effort: 3-5h.
- [x] Task C3: Define blocked-script and failed-endpoint behavior for the Blazor bootstrap and JS bridge.
  - Acceptance: initialization/degradation behavior is explicit for ad blockers, CSP failures, and provider unavailability.
  - Evidence: `analytics-bridge.js` now degrades direct/proxy failures to no-op and uses first-party relay transport; relay bootstrap readiness is covered in `GetPublicExperienceSettingsQueryHandlerTests`; bridge/init expectations documented in `docs/BLAZOR.md` and `docs/OPERATIONS.md`.
  - Dependencies: C1.
  - Effort: 2-4h.

## Phase 3 - Provider Capability Hardening

- [x] Task D1: Keep PostHog as the richest validated provider and document its extra obligations.
  - Acceptance: feature flags, proxy requirements, and CSP implications are explicitly documented.
  - Evidence: internal/external Phase 3 research confirms PostHog as the only currently validated rich provider with native feature flags; provider notes updated in `docs/OPERATIONS.md`, `docs/CODEBASE_INSIGHTS.md`, and roadmap artifacts.
  - Dependencies: A2, C2.
  - Effort: 2-4h.
- [x] Task D2: Keep Plausible as a deliberate lightweight tier rather than a parity target.
  - Acceptance: docs state that `identify` and `groupIdentify` remain unsupported/no-op by design.
  - Evidence: official Plausible docs and current repo implementation align; docs now frame Plausible as lightweight-by-design rather than incomplete.
  - Dependencies: A2, B2.
  - Effort: 1-2h.
- [x] Task D3: Validate whether `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs` should expand beyond track/pageview.
  - Acceptance: roadmap explicitly chooses either deeper support or documented no-op behavior based on verified external docs and actual testing.
  - Evidence: current decision is to keep server-side Rybbit support at track/pageview and document browser-side richer behavior as non-parity; decision recorded in roadmap/context/docs.
  - Dependencies: A2, C1.
  - Effort: 3-5h.
- [x] Task D4: Validate the rollout position of `Explore.Infrastructure/Analytics/RudderStackAnalyticsProvider.cs`.
  - Acceptance: roadmap decides whether RudderStack stays first-class or is marked advanced/experimental until stronger validation exists.
  - Evidence: current positioning is advanced/pipeline-oriented and rollout-with-caution, not direct PostHog parity; provider matrix and roadmap updated consistently.
  - Dependencies: A2.
  - Effort: 2-4h.

## Phase 4 - Blazor, BFF, and Event Integration

- [x] Task E1: Align public bootstrap payload and analytics readiness rules with the new privacy/transport contract.
  - Acceptance: `GetPublicExperienceSettingsQueryHandler` and `PublicExperienceSettingsDto` responsibilities are documented and implementation-ready.
  - Evidence: `docs/BLAZOR.md` now documents readiness rules for disabled/none/relay states and the bootstrap fields consumed by `AnalyticsInitializer`.
  - Dependencies: B3, C1.
  - Effort: 2-4h.
- [x] Task E2: Define client vs server event responsibilities and correlation rules.
  - Acceptance: plan states what belongs in browser pageview/custom events vs server business events and how they should relate.
  - Evidence: `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor` now owns pageview tracking; `docs/BLAZOR.md` and `docs/OPERATIONS.md` now document browser pageviews vs authoritative server business events.
  - Dependencies: B1, C3.
  - Effort: 3-5h.
- [x] Task E3: Map future privacy settings UX to concrete backend/client behavior.
  - Acceptance: `SettingsPrivacy.razor` placeholder can evolve from a documented contract instead of ad hoc switches.
  - Evidence: `Explore.Blazor.Client/Pages/User/Components/SettingsPrivacy.razor` now references operator policy and consent-mode-backed analytics behavior; roadmap context captures the UX contract.
  - Dependencies: B3, E1.
  - Effort: 2-3h.

## Phase 5 - Reliability, Ops, and Test Evidence

- [x] Task F1: Add execution-phase tests for key mismatch fixes and capability gating.
  - Acceptance: unit tests cover canonical key usage, safe no-op behavior, and capability-driven branching.
  - Evidence: canonical key tests, governance/config resolver tests, relay/bootstrap tests, and `Event.Application.UnitTests` pass at 391/391.
  - Dependencies: A1, B3, D-series.
  - Effort: 3-5h.
- [x] Task F2: Add transport-mode and bootstrap degradation tests.
  - Acceptance: test coverage exists for disabled analytics, blocked scripts, missing keys, and provider failures.
  - Evidence: `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs` covers initialization, null settings, thrown settings load, disabled analytics no-pageview behavior, and navigation pageview tracking; transport/provider degradation expectations remain covered by earlier provider edge-case and bootstrap tests.
  - Dependencies: C3, E1.
  - Effort: 3-5h.
- [x] Task F3: Publish operator runbook for enable, disable, proxy, switch-provider, and incident fallback flows.
  - Acceptance: self-hosters have a concrete checklist and troubleshooting flow.
  - Evidence: `docs/OPERATIONS.md` now includes the analytics rollout/incident runbook; `docs/TROUBLESHOOTING.md` adds analytics-specific triage steps.
  - Dependencies: C2, D-series.
  - Effort: 2-4h.
- [x] Task F4: Decide whether buffered/outbox delivery belongs in this milestone or a follow-up.
  - Acceptance: roadmap clearly marks this as committed now or deferred with rationale.
  - Evidence: roadmap plan/context/docs now explicitly defer buffered/outbox analytics delivery to a follow-up milestone and keep the current hardening pass best-effort by design.
  - Dependencies: E2.
  - Effort: 1-2h.

## Verification Checklist For Future Execution

- [x] Run `lsp_diagnostics` on all changed C# files.
- [x] Run `dotnet build --configuration Release --verbosity quiet`.
- [ ] Run at minimum:
  - [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (still fails with 3 pre-existing unrelated tests)
- [x] Add or update tests covering canonical key usage and provider-safe degradation.
- [x] Verify no analytics failure can break user-facing command/query flows.
- [x] Verify `Explore.Blazor.Client.Tests` failures are limited to the same 3 pre-existing unrelated tests after the Phase 4 pageview work.

## Quick Resume

1. Start with Phase 0, not provider expansion.
2. This hardening pass is now complete through Phase 5.
3. The next follow-up, if needed, is buffered/outbox delivery as a separate milestone.
4. Continue treating Plausible as intentionally lightweight, and validate any deeper Rybbit/RudderStack commitments before documenting them as guarantees.

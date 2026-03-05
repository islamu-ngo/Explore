ABOUTME: Strategic implementation plan for evolving multi-provider analytics abstraction in .NET + Blazor.
ABOUTME: Captures verified current state, phased architecture work, risks, metrics, and delivery estimates.

# Analytics Abstraction Roadmap - Plan

Last Updated: 2026-03-05

## Executive Summary

This plan hardens and standardizes analytics abstraction for PostHog, Plausible, and Rybbit across server and Blazor client layers without breaking Clean Architecture boundaries. The current implementation already supports runtime provider selection and client bridge initialization, but has key gaps: incomplete provider implementations, naming/config mismatches, and missing governance/reliability controls. The roadmap prioritizes incremental delivery with strong test and rollout gates.

## Current State Analysis (Verified)

### Existing, verified implementation

- Abstraction contracts exist in Application:
  - `Explore.Application/Contracts/Infrastructure/IAnalyticsProvider.cs`
  - `Explore.Application/Contracts/Infrastructure/IAnalyticsFeatureFlagProvider.cs`
  - `Explore.Application/Contracts/Infrastructure/IAnalyticsConfigResolver.cs`
- Runtime resolver and provider dispatch exist in Infrastructure:
  - `Explore.Infrastructure/Analytics/RuntimeAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs`
- Concrete providers exist:
  - `Explore.Infrastructure/Analytics/PostHogAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/PlausibleAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs`
  - `Explore.Infrastructure/Analytics/NullAnalyticsProvider.cs`
- Blazor analytics bootstrap and interop exist:
  - `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor`
  - `Explore.Blazor.Client/Contracts/Interop/IAnalyticsInterop.cs`
  - `Explore.Blazor.Client/Services/AnalyticsInterop.cs`
  - `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js`
  - `Explore.Blazor.Client/Layout/MainLayout.razor`
- Key tests already exist:
  - `Event.Application.UnitTests/Infrastructure/RuntimeAnalyticsProviderTests.cs`
  - `Event.Application.UnitTests/Infrastructure/AnalyticsConfigResolverTests.cs`
  - `Event.Application.UnitTests/Infrastructure/AnalyticsProviderEdgeCaseTests.cs`
  - `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs`

### Known gaps and issues

- Rybbit and RudderStack providers are present but incomplete/stubbed for full production behavior.
- Settings key mismatch exists:
  - Governance/definitions use `analytics.endpoint_url`
  - `AnalyticsSettingGroup` still references legacy key naming.
- Capability surface is uneven (feature flags are effectively PostHog-specific).
- Server vs client event taxonomy and consent policy are not fully codified.

## Proposed Future State

1. Keep analytics separate from OTel observability (Grafana remains system health source of truth).
2. Preserve current runtime abstraction, but evolve to capability-driven behavior for provider-specific features.
3. Establish a canonical event contract and event naming policy to prevent drift.
4. Complete provider parity for core operations (`track`, `identify`, `pageview`) with predictable no-op behavior for unsupported capabilities.
5. Enforce config consistency and tenant-safe resolution paths.

## Implementation Phases (By Clean Architecture Layers)

### Phase 0 - Baseline and Alignment (Cross-cutting)

- Scope:
  - Fix settings-key inconsistency and validate all analytics keys end-to-end.
  - Document provider capability matrix and event taxonomy baseline.
- Acceptance criteria:
  - All analytics keys resolve consistently through resolver + settings group.
  - Capability matrix published in dev docs and referenced by tasks.
- Dependencies: none.
- Effort: 0.5-1 day.

### Phase 1 - Application Layer Hardening

- Scope:
  - Add canonical `AnalyticsEvent` model and capability contract.
  - Keep existing interfaces backward-compatible; add typed path for new work.
- Acceptance criteria:
  - Handlers can emit typed event contract without infrastructure coupling.
  - Capability checks prevent unsupported feature calls from throwing.
- Dependencies: Phase 0.
- Effort: 1-1.5 days.

### Phase 2 - Infrastructure Provider Completion

- Scope:
  - Complete Rybbit provider implementation against official API/script-compatible semantics.
  - Normalize Plausible/PostHog mapping behavior and retry/error handling policy.
- Acceptance criteria:
  - Provider contract tests pass for PostHog, Plausible, Rybbit, Null.
  - Disabled or misconfigured provider never breaks command/query flow.
- Dependencies: Phase 1.
- Effort: 1.5-2.5 days.

### Phase 3 - Blazor Client and BFF Integration

- Scope:
  - Formalize Blazor client event entry points through interop bridge.
  - Ensure runtime initialization is consent-aware and robust under script failures/ad blockers.
- Acceptance criteria:
  - `AnalyticsInitializer` init path works for enabled providers and degrades safely when unavailable.
  - Client pageviews and custom events can be correlated to server events where policy allows.
- Dependencies: Phases 1-2.
- Effort: 1-1.5 days.

### Phase 4 - Governance, Reliability, and Rollout

- Scope:
  - Add optional buffered delivery for high-value server events.
  - Add policy guardrails for PII allowlist and kill-switch.
  - Add runbook docs and rollout checklist per environment.
- Acceptance criteria:
  - Analytics can be disabled globally/tenant without deployment.
  - Failures are observable in logs/metrics and do not affect user-facing flows.
- Dependencies: Phases 0-3.
- Effort: 1.5-2 days.

## Detailed Task Matrix

### Task A - Fix settings key mismatch

- Work: align `AnalyticsSettingGroup` keys with `AnalyticsSettingDefinitions` + governance constants.
- Acceptance criteria: resolver reads and returns expected key values in tests.
- Dependencies: none.
- Effort: 2-4 hours.

### Task B - Introduce capability model

- Work: add provider capability metadata (`supportsFeatureFlags`, `supportsIdentify`, etc.).
- Acceptance criteria: runtime provider branch behavior is capability-driven.
- Dependencies: Task A.
- Effort: 4-6 hours.

### Task C - Complete Rybbit provider

- Work: implement core event API mapping and error handling.
- Acceptance criteria: integration-style tests verify payload and headers.
- Dependencies: Tasks A-B.
- Effort: 6-10 hours.

### Task D - Blazor bridge hardening

- Work: strengthen `analytics-bridge.js` initialization/fallback behavior and parity with backend provider selection.
- Acceptance criteria: component + bUnit tests pass for init success/failure paths.
- Dependencies: Tasks B-C.
- Effort: 4-6 hours.

### Task E - Governance and rollout docs

- Work: add privacy/consent constraints and operational runbook.
- Acceptance criteria: docs include enable/disable, incident fallback, and provider switch procedures.
- Dependencies: Tasks A-D.
- Effort: 3-5 hours.

## Risk Mitigation

- Provider API drift: isolate vendor mapping in provider adapters and keep contract tests.
- Event schema drift: centralize event names/properties in typed contracts.
- Privacy regressions: explicit allowlist and tenant-level kill-switch.
- Runtime instability: non-blocking analytics calls and safe null fallback.

## Success Metrics

- 100% pass for analytics contract tests across all enabled providers.
- Zero user-facing request failures attributed to analytics provider errors.
- Key settings (`provider`, `enabled`, `endpoint_url`) resolve consistently across tenant/system scope.
- Verified event flow from Blazor pageview + server command event for each selected provider.

## Resources and Dependencies

- Official documentation:
  - https://posthog.com/docs/libraries/dotnet
  - https://posthog.com/docs/libraries/js
  - https://plausible.io/docs/events-api
  - https://rybbit.com/docs
- Internal dependencies:
  - Settings resolver and hierarchical settings resolution
  - Blazor interop module loading and lifecycle
  - Existing unit and bUnit test suites

## Overall Estimate

- Total implementation: 5.5 to 8.5 engineering days (single engineer, including tests and docs).
- If buffered delivery/outbox is expanded to all event classes immediately: add 1.5-2.5 days.

## Potential Risks and Unknowns

Rybbit server-side capabilities, auth behavior under self-hosted deployments, and parity expectations with PostHog feature-flag semantics may require a validation spike before finalizing provider parity commitments. If any capability cannot be implemented consistently, the plan defaults to explicit capability reporting plus documented no-op behavior rather than hidden degradation.

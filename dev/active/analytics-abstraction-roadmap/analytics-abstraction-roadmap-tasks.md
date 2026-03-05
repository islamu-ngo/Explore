ABOUTME: Execution checklist for analytics abstraction roadmap with phase/status tracking.
ABOUTME: Includes acceptance criteria, dependencies, and estimated effort per task.

# Analytics Abstraction Roadmap - Tasks

Last Updated: 2026-03-05

## Phase 0 - Baseline and Alignment (IN PROGRESS)

- [ ] Task A1: Align analytics settings keys in `Explore.Application/Settings/Groups/AnalyticsSettingGroup.cs` with definitions/constants.
  - Acceptance: all analytics keys match governance/definitions, resolver tests updated and passing.
  - Dependencies: none.
  - Effort: 2-4h.
- [ ] Task A2: Publish provider capability matrix (PostHog/Plausible/Rybbit) in active docs.
  - Acceptance: core + optional capabilities clearly marked with source links.
  - Dependencies: A1.
  - Effort: 1-2h.

## Phase 1 - Application Layer Hardening (NOT STARTED)

- [ ] Task B1: Add canonical typed `AnalyticsEvent` model under Application models/contracts.
  - Acceptance: handlers can emit typed events without infrastructure references.
  - Dependencies: A1.
  - Effort: 3-5h.
- [ ] Task B2: Introduce provider capability contract and safe default behavior.
  - Acceptance: unsupported provider features return safe defaults and are test-covered.
  - Dependencies: B1.
  - Effort: 3-4h.

## Phase 2 - Infrastructure Provider Completion (NOT STARTED)

- [ ] Task C1: Complete `Explore.Infrastructure/Analytics/RybbitAnalyticsProvider.cs` core methods.
  - Acceptance: track/identify/pageview paths implemented and contract-tested.
  - Dependencies: B1-B2.
  - Effort: 6-10h.
- [ ] Task C2: Normalize error handling, headers, and endpoint behavior across providers.
  - Acceptance: provider tests verify non-fatal failures and consistent mapping behavior.
  - Dependencies: C1.
  - Effort: 4-6h.

## Phase 3 - Blazor and Interop Robustness (NOT STARTED)

- [ ] Task D1: Harden `Explore.Blazor.Client/wwwroot/js/analytics-bridge.js` init/fallback logic.
  - Acceptance: no runtime exception bubbles to UI when scripts fail or are blocked.
  - Dependencies: C1-C2.
  - Effort: 3-5h.
- [ ] Task D2: Extend `Explore.Blazor.Client.Tests/Components/AnalyticsInitializerTests.cs` for failure/consent paths.
  - Acceptance: bUnit coverage for init success/failure and disabled-provider scenarios.
  - Dependencies: D1.
  - Effort: 2-4h.

## Phase 4 - Governance, Reliability, and Rollout (NOT STARTED)

- [ ] Task E1: Add event/property allowlist guidance and kill-switch operational procedure.
  - Acceptance: documented runbook and config procedure for emergency disable.
  - Dependencies: A2, C2.
  - Effort: 2-3h.
- [ ] Task E2: Add optional buffered/outbox strategy proposal for high-value server events.
  - Acceptance: architecture decision note with phased rollout and validation plan.
  - Dependencies: C2.
  - Effort: 3-5h.

## Verification Checklist (for implementation phase)

- [ ] Run `lsp_diagnostics` on all changed files.
- [ ] Run `dotnet build --configuration Release --verbosity quiet`.
- [ ] Run relevant test projects (at minimum Application unit tests + Blazor client tests for touched areas).
- [ ] Confirm no regressions in existing analytics provider routing tests.

## Quick Resume

1. Complete Task A1 first (settings key alignment is the highest-risk mismatch).
2. Move to typed event + capability contract tasks (B1-B2).
3. Finish provider completion and Blazor hardening (C, D) before governance add-ons (E).

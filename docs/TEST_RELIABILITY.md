ABOUTME: Tracks flaky, deferred, and manual runtime test debt with owner and removal criteria.
ABOUTME: Keeps scheduled and manual lanes advisory until reliability evidence supports promotion.

# Test Reliability

> **Audience:** Maintainers | Release operators | Contributors | AI agents
> **Status:** Implemented tracking policy
> **Owner:** Platform/Ops
> **Last Verified:** 2026-06-01 Europe/Brussels
> **Source Anchors:** `.github/workflows/e2e.yml`, `Event.API.IntegrationTests/Features/`, `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`

This inventory keeps runtime, stress, and E2E reliability debt visible. A skipped or repeatedly flaky runtime lane may not be promoted to a required merge gate until it has an owner, first-seen date, evidence source, and removal or promotion criteria.

API contract-specific skipped tests are tracked separately in [API_CONTRACT_TEST_DEBT.md](API_CONTRACT_TEST_DEBT.md) because they affect OpenAPI contract promotion.

## Tracking Rules

- Every runtime, stress, E2E, or manual visual skip must include a `Category:` and `Removal:` reason in code.
- Every known reliability debt item must be listed here with an owner and first-seen date.
- Scheduled/manual lanes stay advisory until all blocking flakes in their promotion path are resolved or explicitly baselined with a removal condition.
- Do not delete a skipped test just to make a lane green. Either enable it, keep it tracked here, or replace it with stronger active coverage.
- When a tracked item is fixed, remove the skip in code and update this document in the same PR.

## Current Reliability Inventory

| ID | Category | Tests / scope | Owner | First seen | Evidence source | Current behavior | Promotion or removal criteria |
|---|---|---|---|---|---|---|---|
| TR-001 | API integration teardown | `SetupAuthProviderConfigurationFlow_SaveThenComplete_ShouldExposeConfiguredAndProtectPublicReadAfterCompletion` | Platform/Ops + API integration owner | 2026-06-01 | `Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs` | Test is skipped because the OpenFeature SDK shutdown path throws `ChannelClosedException` during `WebApplicationFactory` disposal. | Upgrade/fix OpenFeature shutdown handling, verify the test passes without teardown failure, remove the skip, and keep the test in the API integration lane. |
| TR-002 | Stress/runtime teardown | `RateLimited_ShouldReturnProblemDetailsBody` | Platform/Ops + security/runtime owner | 2026-06-01 | `Event.API.IntegrationTests/Features/StressRateLimitingTests.cs` | Test is skipped because the OpenFeature SDK shutdown path throws `ChannelClosedException` during `WebApplicationFactory` disposal after the runtime assertion path passes. | Resolve OpenFeature shutdown failure, rerun the stress test without teardown failure, and remove the skip. |
| TR-003 | Stress limiter coverage | `SetupSecretValidationRepeatedAttemptsShouldEventuallyReturnTooManyRequests` | Platform/Ops + security/runtime owner | 2026-06-01 | `Event.API.IntegrationTests/Features/StressRateLimitingTests.cs` | Test is skipped until the Stress host enforces the setup-secret endpoint limiter; metadata coverage guards policy wiring meanwhile. | Fix Stress host limiter setup for the setup-secret endpoint, prove repeated invalid attempts return `429`, and remove the skip. |
| TR-004 | Manual visual E2E baseline | Sidebar/dock visual baseline tests in `SidebarLayoutVisualTests` | Platform/Ops + Blazor client owner | 2026-06-01 | `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`; `.github/workflows/e2e.yml`; `docs/DOCK_LAYOUT.md` | Eight Playwright visual baseline tests remain skipped until the Aspire-backed visual baseline lane has seeded event data and approved screenshot storage. | Provide seeded event data, approve screenshot storage/retention, add visual artifact triage guidance, then enable the visual baseline tests in the nightly/manual E2E lane before considering any required status promotion. |

## Promotion Checklist

Before promoting E2E, stress, security, or runtime lanes from advisory/manual/nightly to required:

- All blocking items in the table above are resolved or explicitly accepted as non-blocking with owner/date/removal condition.
- The workflow retains TRX, logs, screenshots/videos/traces where applicable, and Docker/runtime diagnostics.
- At least three recent scheduled/manual runs are reviewed for repeat failures before promotion.
- `docs/CI_CD_GOVERNANCE.md` and `docs/CI_CD_RUNBOOKS.md` explain first triage steps and approved rerun paths.
- Required-check names are verified in GitHub branch protection or ruleset settings before enforcement.

## E2E Runtime Evidence Summary

`.github/workflows/e2e.yml` remains a manual/nightly lane. Every run writes a GitHub step summary pointing to the retained `e2e-runtime-evidence` artifact, including the test outcome, `e2e-test.log`, TRX path, Playwright artifacts, Docker diagnostics, and this reliability inventory.

Use those summaries to compare repeated failures before promoting the lane. If the same failure repeats across scheduled runs, add or update an inventory row with owner, first-seen date, evidence source, and removal criteria before treating the failure as accepted baseline noise.

## Scheduled Failure Trend Summaries

Scheduled and manual runtime/security lanes must leave enough summary data to compare repeat failures across runs without immediately reproducing locally.

| Lane | Workflow | Summary evidence | Trend action |
|---|---|---|---|
| E2E runtime | `.github/workflows/e2e.yml` | Trigger, ref, commit, E2E outcome, retained `e2e-runtime-evidence`, TRX, Playwright artifacts, Docker diagnostics, and this inventory. | If the same failure repeats across scheduled runs, add or update a `TR-*` row with owner, first-seen date, evidence source, and removal criteria. |
| Security integration / Cerbos policy | `.github/workflows/security-tests.yml` | Trigger, ref, commit, Security/Cerbos step outcomes, retained `security-test-evidence`, TRX, logs, and this inventory. | If a scheduled failure repeats, assign API/security or policy owner, record first-seen date, and link the retained evidence before promotion or baseline acceptance. |
| Performance smoke | `.github/workflows/performance-smoke.yml` | Trigger, ref, commit, BenchmarkDotNet suite/outcome, build log, benchmark log, and retained BenchmarkDotNet results. | Keep advisory until enough scheduled runs prove stable signal; add explicit thresholds before making performance regressions blocking. |

This is not a replacement for fixing flaky tests. It is the evidence contract that prevents scheduled failures from becoming invisible noise.

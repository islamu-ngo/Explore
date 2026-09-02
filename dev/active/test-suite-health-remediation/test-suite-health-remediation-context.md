<!-- ABOUTME: Active working memory for the test-suite health remediation workstream. -->
<!-- ABOUTME: Records review state, resume order, validation baseline, decisions, blockers, and handoffs. -->

# Test Suite Health Remediation — Context

Last Updated: 2026-09-02 Europe/Brussels

## Review State
- I-VSD report: `islamic-value-sensitive-design/i-vsd-test-suite-health-remediation.md`
- I-VSD reviewed input revision: `9776cda0654511f5ba07ad096d15f3a307d8ce9d8bcaad0c8256ee33b6f52a6a`
- I-VSD status / disposition: `current` / `plan-aligned`
- CTO review: Reviewed — Approved with required changes (see `test-suite-health-remediation-cto-review.md`); triad rewritten to eliminate commit placeholders and shard Phase 6 execution
- User approval: Awaiting approval

## SESSION PROGRESS (2026-09-02 Europe/Brussels)

### ✅ COMPLETED
- Executed all 22 test projects at `f49dea080` with Podman wired per `.agents/rules/tests.md:43`.
- Classified every failure group by root cause with isolation proofs, not inference.
- Created the planning triad and the I-VSD planning report.
- Resolved both material planning branches from repository evidence (contract-test assembly anchor; BFF shell availability invariant), so no user question was required to finalize scope.
- Senior CTO Review completed (`test-suite-health-remediation-cto-review.md`): approved with required changes.
- Rewrote planning triad to eliminate Phase 7 & 8 commit contract placeholders, mandate sharded class-filtered execution for Task 6.1 to prevent 30-minute timeouts, and resolve the BFF shell redirect to `/setup` via `MiddlewareExtensions.cs:288-295`.

### 🟡 IN PROGRESS
- Awaiting user review and approval of the rewritten implementation plan.

### ⏭️ NEXT
1. User reviews and approves the plan.
2. First implementation agent starts with Task 1.1 (Phase 1 — Deterministic Secrets Lane).
3. Refresh this context after the first implementation slice.

### ⚠️ BLOCKERS
- None blocking planning. Persistence baseline lower bound (≥108 failures) will be measured cleanly via Task 6.1 sharded, class-filtered execution passes.

## Quick Resume
1. Read this context and `test-suite-health-remediation-tasks.md`.
2. Read only the current phase, constraints, or changed decisions from `test-suite-health-remediation-plan.md`; do not reread the full unchanged plan on every resume.
3. Start from the first unchecked high-priority task unless the user overrides it.
4. Keep `tasks.md` current during implementation and update context/plan only at their defined triggers.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `tests/Explore.Secrets.UnitTests/Bootstrap/BootstrapSecretLoaderTests.cs` | Existing | Test | Secret provider fail-closed assertions | Source of the lane hang (unbounded egress) |
| `tests/Event.API.IntegrationTests/Features/SetupSecretAuthorizationMatrixTests.cs` | Existing | Test | Setup-secret authorization matrix | `SeedInstanceAdminAsync:166` seeds non-UUIDv7 |
| `tests/Event.Application.UnitTests/Features/ConfigurationManifest/ConfigurationManifestContractTests.cs` | Existing | Test | Wire-contract identity/scope guard | Resolves types from the wrong assembly |
| `src/Event.Wire.Contracts/ConfigurationPortability/ConfigurationManifestV1Alpha2.cs` | Existing | Wire contract | Declares the manifest contract types | Correct assembly anchor for Phase 3 |
| `src/Explore.Domain/InstanceBootstrapState.cs` | Existing | Domain | UUIDv7 identifier invariant | Correct; must not be relaxed |
| `tests/Event.Architecture.Tests/AuthorizationSurfaceGuardrailTests.cs` | Existing | Test | Authorization classification guard | Reports the unclassified claim command |
| `src/Explore.Application/Features/InstanceOnboarding/.../ClaimConfiguredInstanceAdministratorCommand.cs` | Existing | Application | Instance-administrator claim | Tier 1; needs real classification |
| `src/Explore.Application/Features/Notifications/Handlers/Queries/GetWebPushPublicConfigurationQueryHandler.cs` | Existing | Application | Web Push public config query | Returns disabled representation in Phase 8 |
| `src/Explore.API/Controllers/NotificationController.cs` | Existing | API | Public Web Push reads | Returns 500 when unconfigured |
| `src/Explore.Persistence/ConfigurationManifestPersistenceServicesRegistration.cs` | Existing | Persistence | Manifest persistence DI | Phase 7 startup graph dependency |
| `src/Explore.Infrastructure/ConfigurationManifest/ConfigurationManifestStartupServicesRegistration.cs` | Existing | Infrastructure | Manifest startup DI | Phase 7 deferred composition root |
| `src/Explore.Blazor/Extensions/MiddlewareExtensions.cs` | Existing | BFF | Middleware & startup redirect | Lines 288-295 `HandleStartupRedirectAsync` redirects `/` to `/setup` |
| `src/Event.Web.BffHosting/Authentication/EventBffAuthenticationExtensions.cs` | Existing | BFF | Auth/authorization composition | Shell 302 investigation start point |
| `tests/Explore.Blazor.IntegrationTests/Endpoints/BffNoKeycloakResilienceTests.cs` | Existing | Test | Air-gapped resilience invariant | Assertion is correct; production regressed |
| `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ReleaseInputPolicyTests.cs` | Existing | Test | Change-fragment policy validation | Stale `docs/releases/changes` path |
| `.agents/contract/intents.yaml` | Existing | Agent contract | Intent registry | Stale `agentic-workflow-control-plane` reference |
| `docs/internal/TESTING.md` | Existing | Docs | Lane inventory target | Phase 10 primary target |

## Key Decisions
- **D1** Repoint the ConfigurationManifest contract tests to `Event.Wire.Contracts`; do not delete the cohort.
- **D2** Fix API fixture lifetime rather than serializing the assembly.
- **D3** Align fixtures to UUIDv7; never relax the domain guard.
- **D4** Restore anonymous BFF shell availability in production; keep the test assertion intact.
- **D5** Model unconfigured optional capability as a successful disabled state, not 404/503/500.
- **D6** Classify the mutating instance-administrator command; never allowlist it.
- **D7** Remove network egress from the secrets unit lane.
- **D8** Seed lookup data in the persistence fixture; never drop the constraint.

## Constraints And Rules To Remember
- Matched intents: `test-suite-rationalization` (Phases 1, 2, 3, 4, 6, 10), `bff-auth-bug` (Phase 9), labeled fallback contract (Phases 5, 7, 8).
- `test-suite-rationalization` forbids `src/**`; phases touching `src/**` run under their own intent or the fallback contract, never under it.
- Never hand-edit migrations, model snapshots, OpenAPI artifacts, or generated clients.
- Fail-closed disposition rule `IVSD-M001`: no security, tenant-isolation, privacy, money, concurrency, or state-machine cohort may be deleted or weakened without a passing stronger replacement.
- No test may perform outbound network I/O; no fast lane may acquire a Docker/browser/Aspire/live-provider prerequisite.
- Greenfield: no compatibility shim, deprecated alias, or legacy adapter.
- No ad-hoc Python/Node helper scripts; Bash and native edit tools only.

## Validation Baseline

Per phase: one Release build plus at most one project test, run once after the phase's tasks.

Baseline measured at `f49dea080` (Release, Podman wired, `TMPDIR` relocated):

| Project | Baseline | Phase owning remediation |
|---|---|---|
| Event.Domain.UnitTests | 1113 pass | — (healthy) |
| Explore.Blazor.Client.Tests | 2622 pass, 1 skipped | — (healthy) |
| Event.Setup.Core.Tests | 72 pass | — (healthy) |
| Event.SetupAssistant.Tests | 52 pass | — (healthy) |
| Event.SetupAssistant.Cli.Tests | 19 pass | — (healthy) |
| Event.SetupAssistant.Terminal.Tests | 13 pass | — (healthy) |
| Event.SetupAssistant.Desktop.Tests | 9 pass | — (healthy) |
| Event.SetupAssistant.Browser.Tests | 4 pass | — (healthy) |
| Event.Standalone.IntegrationTests | 49 pass | — (healthy) |
| Event.Wire.Contracts.UnitTests | 35 pass | — (healthy) |
| Explore.Diagnostic.UnitTests | 21 pass | — (healthy) |
| Explore.GeneratedContracts.Tests | 8 pass | — (healthy) |
| Event.Domain.AddOns.MutationTests | 12 pass | — (healthy) |
| Event.Domain.Recovery.MutationTests | 7 pass | — (healthy) |
| Explore.Infrastructure.Tests | 1664 pass / 2 fail | Phase 7 |
| Event.Application.UnitTests | 2010 pass / 10 fail | Phase 3 |
| Event.Architecture.Tests | 573 pass / 3 fail | Phases 4, 5 |
| ISLAMU.ReleaseEngineering.Tests | 243 pass / 1 fail | Phase 4 |
| Explore.Blazor.IntegrationTests | 553 pass / 7 fail | Phase 9 |
| Event.API.IntegrationTests | 2361 pass / 62 fail | Phases 2, 8 |
| Event.Persistence.IntegrationTests | **≥108 fail (lower bound, run truncated)** | Phase 6 |
| Explore.Secrets.UnitTests | **hangs, 0 executed** | Phase 1 |

Root-cause split of the 62 API failures: 72 `ObjectDisposedException` occurrences across 5 classes (cascade collateral — `TagControllerTests` passes 9/9 in isolation), 26 UUIDv7 fixture rejections, plus genuine assertions including the Web Push 500s.

**Honest reporting note:** the Persistence figure is a lower bound, not a measurement. `Explore.Secrets.UnitTests` has never been observed executing a single test in this environment. Neither may be reported as green or as a known-good count until Task 6.1 and Phase 1 produce real numbers.

**Measured lane-duration finding (2026-09-02):** `Event.Persistence.IntegrationTests` failed to complete a full run **twice**, on two independent attempts, each terminated by a 30-minute deadline with no run summary emitted. The lane therefore exceeds 30 minutes wall-clock on a 16-core workstation with rootless Podman. This is evidence in its own right, and it upgrades the Phase 6 risk from "may hide multiple root causes" to "is additionally a runtime problem that will hit CI deadlines". Task 6.1 must budget for a long-running or sharded execution — for example a single class-filtered pass per root-cause group — rather than assuming one full-lane run will produce the baseline.

## Current Known Risks / Unknowns
- Persistence lane exceeds 30 minutes wall-clock and may hide 3+ root causes; Task 6.1 mitigates this by running sharded class-filtered baseline passes per group rather than a monolithic run.
- The BFF shell redirect target is confirmed as `/setup` via `HandleStartupRedirectAsync` in `MiddlewareExtensions.cs:288-295`; Task 9.1 resolves why the onboarding status mock does not prevent the redirect for static shell routes.
- The other 6 Blazor failures may not share the shell root cause — Task 9.4 dispositions each individually.
- Phase 7 and Phase 8 commit paths are fully enumerated and locked down; all angle-bracket placeholders have been eliminated.
- Overlap with `dev/active/setup-assistant-security-and-portability` (updated the same day) is tracked to prevent merge conflicts during Phase 9.

## Handoff Notes

### Handoff — 2026-09-02 Europe/Brussels
- **Current state:** Planning triad and I-VSD planning report authored. No runtime or test code has been changed by this workstream.
- **Next action:** User reviews the plan. On approval, implementation starts at Task 1.1.
- **Blockers:** None for planning. Persistence baseline is a lower bound pending Task 6.1.
- **Modified files:** `dev/active/test-suite-health-remediation/{plan,context,tasks}.md` (new), `islamic-value-sensitive-design/i-vsd-test-suite-health-remediation.md` (new).
- **Validation:** No build or test command was run as part of authoring these artifacts, per the planning scope gate. The recorded baseline comes from the investigation that preceded planning.
- **Documentation impact:** Phase 10 owns all documentation updates; nothing was changed yet.
- **Risks:** See Current Known Risks above; Phase 6 is the most likely to expand.
- **Notes for next contributor/agent:**
  - **Unrelated dirty files to avoid staging:** modified `islamic-value-sensitive-design/i-vsd-event-resource-consultancy-report.md`; untracked `gitbook-*.png` screenshots at repository root; untracked `islamic-value-sensitive-design/i-vsd-minimum-attendee-threshold-consultation.md` and `i-vsd-tenancy-experience-directory-vs-dedicatedportal.md`. None of these belong to this workstream.
  - **Environment:** export `TMPDIR` to a directory with free space plus the Podman variables from `.agents/rules/tests.md:43` before running any container-backed lane. Without them the suite reports a fabricated mass regression that looks like catastrophic breakage.
  - Run `eng/release/tests/ISLAMU.ReleaseEngineering.Tests` explicitly; a `tests/**` glob does not find it.

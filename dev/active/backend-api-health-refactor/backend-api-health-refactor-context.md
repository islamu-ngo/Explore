<!-- ABOUTME: Current operational context for the backend/API health refactor workstream. -->
<!-- ABOUTME: Records the 2026-07-03 Senior CTO re-baseline, decisions, risks, and next implementation slice. -->

# Backend API Health Refactor - Context

Last Updated: 2026-07-03 Europe/Brussels

## SESSION PROGRESS (2026-07-03 Europe/Brussels)

### Completed

- Re-read the repo contract, Senior CTO workflow, dev-docs contract, core docs, relevant path rules, and related architecture/CQRS/EF/auth/observability skills.
- Used Context7 for official ASP.NET Core health-check documentation.
- Used Tavily for current Microsoft Learn health-check and OpenTelemetry/Prometheus metrics documentation.
- Used CodeGraph to verify the current shared health endpoint implementation in `Explore.ServiceDefaults/Extensions.cs`.
- Re-baselined `backend-api-health-refactor-plan.md` around current repo reality and Senior CTO rewrite guidance.
- Rewrote this context file and the task checklist so plan/context/tasks agree.
- User approved moving from planning into implementation by asking to fully implement the plan.
- Recorded the current worktree as heavily dirty, with many unrelated modified/untracked files outside this workstream. This slice touched only the health writer/docs/test files listed in the handoff below, plus a minimal compile fix in an already-modified storage upload validator.
- Selected the first implementation boundary: Phase 2.5 and 2.6, because shared health response redaction and `/health/ready` docs drift were source-grounded, bounded, and not dependent on the still-stale endpoint inventory.
- Implemented `Explore.ServiceDefaults.HealthChecks.HealthCheckResponseWriter` and routed `/health` and `/alive` through it. Raw `Exception.Message` no longer appears in health JSON; suspicious health descriptions and sensitive data values are redacted at the shared serialization boundary.
- Added focused API integration tests for the shared health JSON writer.
- Reconciled the stale product-doc health path references in `docs/MCP_DEBUGGING.md` and `docs/CONFIGURATION.md` from `/health/ready` to `/health`.
- Fixed a pre-existing compile blocker in `CreateStorageUploadSessionDtoValidator` by changing `char` plus `StringComparison` calls to string overloads.

### In Progress

- Full plan implementation remains active.
- Phase 0.3 endpoint inventory/risk-register reconciliation is still open.
- Phase 0.4 full blocker recheck is still partial: this slice proved ServiceDefaults/Application/API health-test build paths and the repo-level Release build, but did not run architecture tests, Docker/Testcontainers tests, or the whole API integration suite.

### Next

1. Reconcile `endpoint-inventory.md` and `backend-contract-risk-register.md` with current source/OpenAPI.
2. Recheck architecture context failures, Blazor build state, and Docker/Testcontainers API integration blockers.
3. Select the next small slice from Phase 1 or Phase 2 after inventory/risk reconciliation.

### Blockers

- No user blocker.
- The worktree remains heavily dirty with unrelated changes. Continue avoiding unrelated source normalization or reverts.
- Full verification is not yet known. Focused tests and repo build passed, but architecture, Docker/Testcontainers, and whole-suite API integration blockers remain to be rechecked.

## Quick Resume

1. Read `backend-api-health-refactor-plan.md`.
2. Read `backend-api-health-refactor-tasks.md`.
3. Inspect current `git status --short` before editing source.
4. Continue Phase 0.3/0.4 before starting security/HAL/controller decomposition work.
5. Keep plan/context/tasks current after every meaningful implementation slice.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/active/backend-api-health-refactor/backend-api-health-refactor-plan.md` | Existing | Dev docs | Strategic source of truth. | Re-baselined 2026-07-03. |
| `dev/active/backend-api-health-refactor/backend-api-health-refactor-context.md` | Existing | Dev docs | Current state, decisions, blockers, handoff. | This file. |
| `dev/active/backend-api-health-refactor/backend-api-health-refactor-tasks.md` | Existing | Dev docs | Tactical checklist. | Rewritten to remove stale/bad task shape. |
| `dev/active/backend-api-health-refactor/endpoint-inventory.md` | Existing | Planning artifact | Endpoint contract inventory. | Useful but potentially stale; reconcile from current source/OpenAPI. |
| `dev/active/backend-api-health-refactor/backend-contract-risk-register.md` | Existing | Planning artifact | API/auth/tenant/contract risk register. | Keep as detailed risk ledger; update per slice. |
| `dev/active/backend-api-health-refactor/authorization-policy-matrix.md` | Existing | Planning artifact | Resource/action/API/HAL/Cerbos mapping. | Must be validated against current `ResourceKinds`/`AuthorizationActions`. |
| `dev/active/backend-api-health-refactor/tenant-execution-model.md` | Existing | Planning artifact | Tenant execution/bypass/bootstrap model. | Use as design input; do not blindly implement an enum. |
| `dev/active/backend-api-health-refactor/api-error-catalog.md` | Existing | Planning artifact | ProblemDetails code catalog. | Align with implemented `ApiProblemCodes`. |
| `Explore.ServiceDefaults/Extensions.cs` | Existing | Service defaults | Maps `/health`, `/alive`, `/metrics` and delegates health JSON serialization. | Updated 2026-07-03. |
| `Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs` | New | Service defaults | Shared bounded/redacted health JSON writer for readiness and liveness. | Added 2026-07-03. |
| `Event.API.IntegrationTests/Features/HealthCheckResponseWriterTests.cs` | New | Tests | Proves shared health JSON shape and redaction behavior. | Added 2026-07-03. |
| `docs/API.md` | Existing | Docs | API architecture and contract. | Defines runtime operational endpoints and generation workflow. |
| `docs/OPERATIONS.md` | Existing | Docs | Operator health/readiness/metrics/runbooks. | Treat as operator contract. |
| `docs/MULTI_TENANCY.md` | Existing | Docs | Tenant resolution and fail-closed filters. | Current tenant filter behavior anchor. |

## Key Decisions

1. Preserve `/health`, `/alive`, and `/metrics` as the project operational contract.
2. Treat `/health/ready` references as docs drift unless a separate migration plan is approved.
3. Shared health payload redaction is now enforced by `HealthCheckResponseWriter`; individual checks should still emit bounded data.
4. Run Phase 0 re-baseline before further source edits.
5. Security/auth/HAL correctness must precede controller decomposition.
6. Behavior risks need unit/integration tests; architecture tests are for structural invariants.
7. Do not implement a tenant execution enum unless it is the simplest way to solve concrete bypass/system-scope audit needs.
8. Persistence cleanup must be evidence-led and endpoint/repository scoped.

## Constraints And Rules To Remember

- Repositories return entities, not DTOs.
- Query handlers return DTO/list/page/null contracts, not command response envelopes.
- Commands use `BaseCommandResponse<TId>` or established delete/result patterns.
- Validators are manually instantiated.
- Controllers are thin API transport/representation boundaries.
- HAL links are the source of truth for Blazor mutation affordances.
- Tenant filters fail closed; bypasses need bounded predicates and reasons.
- Generated OpenAPI/client/inventory artifacts are regenerated, not hand-edited.
- Health/logs/metrics/traces must not expose raw secrets, endpoints, provider response bodies, object keys, filesystem paths, tenant/user identifiers, prompts, or exception text.

## Validation Baseline

For this planning rewrite:

- Docs were updated only under `dev/active/backend-api-health-refactor/`.
- No build or test suite was required for this docs-only re-baseline.
- Verify with `git diff -- dev/active/backend-api-health-refactor/backend-api-health-refactor-plan.md dev/active/backend-api-health-refactor/backend-api-health-refactor-context.md dev/active/backend-api-health-refactor/backend-api-health-refactor-tasks.md`.

For future source implementation, use the touched intent's project-level tests:

- API/contract/auth/HAL: `Event.API.IntegrationTests` and `Event.Architecture.Tests`.
- Application/CQRS: `Event.Application.UnitTests` and `Event.Architecture.Tests`.
- Persistence/query/migration: `Event.Persistence.IntegrationTests` and `Event.Architecture.Tests`.
- Blazor HAL affordances: `Explore.Blazor.Client.Tests` plus API HAL tests.
- Full build before handoff when source changed: `dotnet build --configuration Release --verbosity quiet`.

Do not use solution-level `dotnet test`.

## Current Known Risks / Unknowns

- The worktree has many unrelated changes. Future agents must avoid reverting or normalizing them.
- The endpoint inventory is likely stale relative to current source and generated OpenAPI.
- Shared health response redaction is implemented and covered by focused tests; remaining risk is individual health checks adding new sensitive `Data` keys that should be caught by the writer tests or future targeted tests.
- Product docs were updated from `/health/ready` to `/health`; workstream docs still mention `/health/ready` only as rejected/deferred examples. `Explore.AppHost` still probes external infrastructure paths such as Cerbos/MinIO and should not be treated as API readiness aliases.
- Prior verification blockers may be stale or caused by unrelated worktree state.
- Existing support artifacts have valuable details but should not override freshly verified source.

## External Research Used

- Context7: `/dotnet/aspnetcore.docs` for ASP.NET Core health checks, readiness/liveness tags, `HealthCheckOptions`, response writer/status mapping.
- Tavily: Microsoft Learn ASP.NET Core health checks for readiness/liveness semantics and probe filtering.
- Tavily: Microsoft Learn ASP.NET Core metrics for OpenTelemetry + Prometheus `/metrics` guidance.

## Handoff Notes

### Handoff - 2026-07-03 Europe/Brussels

- **Current state:** Planning rewrite plus first source slice are complete. `/health` and `/alive` now share a redacting health JSON writer.
- **Next action:** Finish Phase 0.3 inventory/risk reconciliation and Phase 0.4 full blocker recheck before larger security/HAL work.
- **Blockers:** Dirty worktree and unknown full-suite test state remain. Focused health/Application tests and full Release build pass.
- **Modified files in this slice:** `Explore.ServiceDefaults/Extensions.cs`, `Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs`, `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`, `Event.API.IntegrationTests/Features/HealthCheckResponseWriterTests.cs`, `Explore.Application/DTOs/StorageObject/Validators/CreateStorageUploadSessionDtoValidator.cs`, `docs/MCP_DEBUGGING.md`, `docs/CONFIGURATION.md`, this context file, task checklist, and plan.
- **Validation:** `dotnet build Explore.ServiceDefaults/Explore.ServiceDefaults.csproj --configuration Release --verbosity quiet`; `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`; `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/CreateStorageUploadSessionDtoValidatorTests/*" --minimum-expected-tests 1`; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/HealthCheckResponseWriterTests/*" --minimum-expected-tests 1`; LSP diagnostics clean on modified source/test files; `git diff --check` clean; `rg -n "/health/ready|/health/live" docs` returns no product-doc matches.
- **Documentation impact:** Product docs now point MCP readiness checks at `/health`, not `/health/ready`.
- **Risks:** Test/build output still has many pre-existing warnings and package vulnerability warnings. Do not treat those as introduced by the health slice without diff evidence.
- **Notes for next contributor/agent:** `rg -n "/health/ready|/health/live" docs` should be empty. Broader `Explore.*` searches still find intentional external dependency probes in `Explore.AppHost` such as MinIO/Cerbos.

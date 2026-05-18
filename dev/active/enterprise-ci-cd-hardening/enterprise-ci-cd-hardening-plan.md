ABOUTME: Implementation plan for hardening GitHub Actions into an enterprise-grade CI/CD system.
ABOUTME: Captures phased workflow, OpenAPI contract guard, deployment safety, and supply-chain controls.

# Enterprise CI/CD Hardening Plan

Last Updated: 2026-05-07

## Executive Summary

The current GitHub Actions setup provides a useful foundation: reusable build/test logic, CodeQL, Cerbos policy checks, security integration tests, dependency review, reusable container builds, protected deploy jobs, advisory E2E runtime lanes, and Coolify deployment workflows. PR1-PR9 plus the Phase 1B/1C follow-ups now address the first correctness, hygiene, security-gate, container-evidence, deployment-protection, runtime-evidence, governance-documentation, and action supply-chain gaps: fast tests include the infrastructure suite, GitHub Actions restore uses locked NuGet restore, TRX/OpenAPI/security/container/deploy/E2E evidence is produced, fast/integration test summaries identify project outcomes and artifact names, OpenAPI drift is guarded, workflow permissions are explicit, missing timeouts/concurrency are added, Cerbos no longer uses a mutable binary version, C# CodeQL scans a built Release graph, Dependabot update automation is in place, external actions are SHA-pinned with version comments, deployable images emit digests/SBOM/provenance/GHCR attestations/Trivy scan output, deploy jobs use GitHub Environments plus hardened Coolify webhook calls, and `docs/CI_CD_GOVERNANCE.md` documents required checks, repository settings, fork PR policy, generated-artifact review, badges, locked restore, and retention. Remaining enterprise gaps include coverage, Dockerfile locked-restore normalization, full deploy workflow consolidation, Coolify digest/tag consumption, mandatory production smoke URL enforcement, and GitHub repository settings verification.

This plan upgrades CI/CD in phases so reliability comes before strict enforcement. The first milestone keeps current workflow names stable, fixes deterministic build/test behavior, and introduces a dedicated OpenAPI contract guard that regenerates `Explore.API/swagger.json` and the NSwag client only when API-surface changes require it, then fails CI if generated artifacts were not committed. Later phases add least privilege, artifacts, security scanning, container provenance, protected deployments, smoke checks, and governance documentation.

**Execution constraint:** this is not a single implementation PR. Deliver it as small, reviewable PRs with stable required-check names until branch protection is explicitly migrated.

## Current State

### Existing Workflows

- `.github/workflows/test.yml` is the main PR/push entrypoint for `main` and `develop`; it calls `./.github/workflows/_build-test.yml`.
- `.github/workflows/_build-test.yml` restores, audits packages, runs `dotnet format`, builds Release, and runs a subset of test projects, with optional integration tests.
- `.github/workflows/security-tests.yml` runs security and policy-contract TUnit categories for selected auth/Cerbos paths.
- `.github/workflows/cerbos-policy-check.yml` compiles Cerbos policies with a fixed Cerbos binary version.
- `.github/workflows/codeql.yml` now analyzes C# with a manual Release build and covers `main`, `develop`, and merge queues.
- `.github/workflows/deploy-coolify.yml` and `.github/workflows/deploy-coolify-develop.yml` now call reusable `_container-build.yml` for image build evidence, then deploy through GitHub Environment-gated Coolify webhook jobs with environment-level concurrency, hardened curl, bounded optional smoke checks, deploy summaries, and 90-day deployment evidence artifacts.
- `.github/workflows/e2e.yml` now runs Aspire-backed Playwright E2E manually and nightly, without becoming a PR-required gate.
- `.github/workflows/agent-context.yml` validates AI-context governance docs using `global.json`.

### Existing OpenAPI Contract Path

- `Explore.API/Program.cs` wires `builder.Services.AddOpenApi("event-api", ...)` and `app.MapOpenApi()` in Development/Testing.
- `Explore.API/Explore.API.csproj` refreshes `Explore.API/swagger.json` through ASP.NET Core build-time OpenAPI generation.
- `Explore.Blazor.Client/Explore.Blazor.Client.csproj` runs NSwag before compile when `Explore.API/swagger.json` changes.
- `Explore.Blazor.Client/nswag.json` generates `Explore.Blazor.Client/Clients/EventApiClient.g.cs` from `Explore.API/swagger.json`.
- `Event.API.IntegrationTests/Features/ContractInvariantsTests.cs`, `Event.Architecture.Tests/ApiContractArchitectureTests.cs`, `Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs`, and `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs` already contain related guardrails, though some contract/client naming checks are skipped pending stabilization.
- `dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md` is generated from `/openapi/event-api.json` and should be kept in sync when inventory generation is intentionally part of a PR.

### Important Gaps

- PR2 now adds `.github/workflows/openapi-contract.yml` for OpenAPI/client drift; advisory `oasdiff`, Spectral, and deterministic action-inventory drift remain future work.
- PR2 now also runs the stable `OpenApiDocument_*` contract invariant subset inside `.github/workflows/openapi-contract.yml`; advisory `oasdiff`, Spectral, and deterministic action-inventory drift remain future work.
- PR1 now runs `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` in CI and documents the 10-project inventory in `docs/TESTING.md`.
- PR7 now adds `.github/workflows/e2e.yml` for manual/nightly `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj` execution.
- PR3 gives workflows/jobs explicit least-privilege `permissions`.
- PR1/PR2/PR5/PR6/PR7 now upload TRX, OpenAPI/client drift, container digest/scan, deployment, and E2E runtime evidence artifacts; the Phase 1B follow-up adds fast/integration test evidence summaries; coverage and richer policy outputs remain future work.
- GitHub Actions restore and deployable Docker build restore now use `dotnet restore --locked-mode`; clean CI image builds still need to validate the container path because the local workspace has unrelated compile/package issues.
- Deployments now use GitHub Environment bindings in workflow YAML, hardened webhook calls, bounded optional smoke checks, deployment summaries, and deploy concurrency keyed by environment. Production required reviewers, branch restrictions, and environment secrets must still be configured in GitHub repository settings.
- External actions are full-SHA pinned with same-line version comments; local reusable workflows remain path-based.
- Deploy workflows now build through a reusable container builder that emits digests/SBOM/provenance, but Coolify still deploys according to its pre-existing configured image/tag source until digest or immutable-SHA tag consumption is confirmed.

## Future State

### Migration Strategy

The long-term architecture can converge on cleaner workflow names, but the first implementation milestones must preserve current required-check names where possible. Workflow renames are a repository-settings migration because branch protection and required checks may reference existing job/workflow names.

**Initial rule:** improve existing workflows first, add new standalone workflows only where necessary, and defer consolidation/renaming until branch protection is coordinated.

Recommended implementation PR sequence:

1. **PR 1 — CI correctness baseline:** improve `_build-test.yml` and `test.yml`, add `Explore.Infrastructure.Tests`, update `docs/TESTING.md`, add TRX artifacts, and stop Postgres for non-integration lanes.
2. **PR 2 — OpenAPI drift guard:** add dedicated always-running `openapi-contract.yml` with exact regeneration and diff commands.
3. **PR 3 — Workflow hygiene:** add explicit permissions, timeouts, concurrency, security path-filter fixes, and fixed Cerbos version.
4. **PR 4 — Dependency/security review:** add Dependabot update automation, C# CodeQL manual build, dependency review, and security evidence artifacts.
5. **PR 5 — Container provenance:** extract reusable image build logic, capture digests, add SBOM/provenance, scan images, and retain build evidence while keeping Coolify deploy semantics stable.
6. **PR 6 — Deployment protection:** add GitHub Environments, environment secrets, deploy concurrency, Coolify webhook hardening, digest/tag consumption decision, and smoke checks.
7. **PR 7 — Nightly E2E/runtime lanes:** add manual/nightly Aspire-backed E2E and runtime evidence without blocking PRs initially.
8. **PR 8 — Governance documentation:** document branch protection, required/advisory gates, GitHub Environment settings, fork PR policy, generated-artifact review, artifact retention, and badge policy.
9. **PR 9 — Action SHA pinning:** pin external GitHub Actions to full-length commit SHAs with same-line version comments while keeping local reusable workflows path-based.

The target architecture is a small set of focused, reusable workflows with explicit responsibilities:

| Workflow | Purpose | Required? | Notes |
|---|---|---|---|
| `.github/workflows/ci.yml` | Fast PR gate: restore, format, build, unit/component/architecture tests | Yes | Thin orchestrator calling reusable jobs |
| `.github/workflows/integration.yml` | API, persistence, BFF integration tests | Yes on relevant PRs once stable | Avoid skipped required workflows by keeping a lightweight always-running gate |
| `.github/workflows/openapi-contract.yml` | Regenerate OpenAPI + NSwag client, then fail on drift; action inventory joins after deterministic output exists | Yes | Dedicated contract guard requested by user |
| `.github/workflows/security.yml` | security tests, Cerbos policy checks, dependency review, secret-safety checks | Yes | Can call existing security lanes initially |
| `.github/workflows/codeql.yml` | CodeQL with C# build/autobuild and scheduled scans | Yes | Preserve least privilege |
| `.github/workflows/dependency-review.yml` | Review dependency changes for vulnerabilities before merge | Yes on PRs | Complements NuGet audit |
| `.github/workflows/e2e.yml` | Aspire-backed browser/E2E checks | Nightly/manual initially | Promote to required after reliability proven |
| `.github/workflows/_container-build.yml` | Reusable API/UI image build, scan, SBOM, provenance, and digest evidence | Required before deploy callers | Upload build summaries and digests |
| `.github/workflows/deploy.yml` | Environment-protected staging/production deploy using validated digests | Protected | One reusable deploy path with environment inputs |

Enterprise-grade means every important merge/deploy decision is reproducible, least-privileged, auditable, and tied to a commit, generated artifact, image digest, environment, approver, and verification result.

### CI Ownership Model

Every workflow must have an explicit owner category so failures route to the right maintainers.

| Area | Owner | Examples |
|---|---|---|
| Build/test | Core maintainers | `_build-test.yml`, `test.yml`, architecture/unit/component tests |
| OpenAPI contract | API/platform maintainers | `openapi-contract.yml`, `swagger.json`, generated NSwag client |
| Security/Cerbos | Security/platform maintainers | `security-tests.yml`, `cerbos-policy-check.yml`, CodeQL/security workflows |
| Deployment | Release operators | Coolify deploy workflows, protected environments, smoke checks |
| E2E/runtime | UI/platform maintainers | Aspire-backed browser/runtime workflows |

### Required vs Advisory Matrix

Not every enterprise-grade signal should block every PR. Use this matrix to avoid turning CI into “everything blocks everything.”

| Check | PR required now | Required later | Advisory/nightly |
|---|---:|---:|---:|
| Build Release | Yes | Yes | No |
| Unit/component/architecture tests | Yes | Yes | No |
| `Explore.Infrastructure.Tests` | Yes after PR 1 | Yes | No |
| API/Persistence integration | Only once stable for relevant lanes | Yes | No |
| OpenAPI drift | Yes after PR 2 | Yes | No |
| `oasdiff` breaking-change report | No | Yes after API policy/versioning rules | Yes |
| Spectral/OpenAPI lint | No | Maybe after rules stabilize | Yes |
| E2E/browser journeys | No | Maybe after reliability data | Yes |
| Container SBOM/provenance | No for normal PRs | Yes before deploy | No |
| Production smoke checks | Deploy-only | Yes for deploy | No |

## Non-Negotiable Principles

1. **Repository conventions first**: follow `AGENTS.md`, `docs/TESTING.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, and `docs/CONFIGURATION.md` before importing generic CI patterns.
2. **Per-project tests only**: never introduce solution-level `dotnet test`; CI must run project-specific `dotnet test --project ...` lanes.
3. **OpenAPI artifacts are generated, never hand-edited**: `Explore.API/swagger.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` must be refreshed by deterministic commands and committed when API-surface changes require it.
4. **Fast checks first, strict checks after stability**: do not make flaky long-running lanes merge-blocking until reliability is proven with artifacts and trend data.
5. **Least privilege by default**: top-level workflow permissions should be read-only or `{}`; jobs elevate only when required.
6. **Deploy immutable outputs**: staging/production must deploy previously validated image digests, not mutable `latest` tags as the source of truth.
7. **Environment protection is mandatory for production**: use GitHub Environments, required reviewers, branch restrictions, environment-scoped secrets, and deployment concurrency.
8. **Evidence is part of the deliverable**: test results, coverage, OpenAPI diffs, policy outputs, SBOMs, provenance, and deployment smoke logs must be retained as artifacts or job summaries.
9. **Fork PRs are untrusted**: external fork PRs must run validation without deployment secrets, registry write credentials, environment secrets, or privileged tokens; do not use `pull_request_target` unless a separate threat-model review approves the exact usage.
10. **Generated drift requires human review**: CI detects stale generated files; reviewers decide whether operation ID, route, authorization metadata, DTO shape, HAL action/link, and generated-client method changes are acceptable.

## Phase Plan

### Phase 0 — Baseline Inventory and Policy Decisions

**Goal:** Lock the intended workflow architecture and required-check names before editing YAML.

**Tasks:**
- Document current required branch checks and intended future names in `docs/RELEASE_CHECKLIST.md` or a dedicated CI/CD operations note.
- Confirm GitHub Environments to create: `staging` and `production`.
- Decide whether `develop` uses required reviewers or only environment-scoped secrets and deploy concurrency.
- Decide whether Codecov/SonarCloud badges in `README.md` will be implemented or removed.
- Decide required check policy for path-sensitive checks; avoid required workflows that are skipped by `paths` filters.

**Acceptance Criteria:**
- Target workflow names and required-check names are documented.
- Branch protection implications are clear before workflow renames land.
- Deployment approval rules are documented for staging and production.

**Effort:** S

### Phase 1A — Test Inventory Correctness

**Goal:** Make the fast PR test inventory correct before adding broader evidence or restore-policy changes.

**Tasks:**
- Add `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` to the CI project list.
- Update `docs/TESTING.md` so the documented test inventory matches the repo’s actual test projects.
- Keep per-project `dotnet test --project ...`; do not introduce solution-level `dotnet test`.
- Keep `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj` out of the fast PR lane; add it in Phase 7 as nightly/manual.

**Acceptance Criteria:**
- `test.yml` runs all fast required test projects, including `Explore.Infrastructure.Tests`.
- Documentation test inventory matches actual test projects.
- No solution-level `dotnet test` appears in workflows.

**Effort:** S/M

### Phase 1B — Test Evidence Artifacts

**Goal:** Make CI failures inspectable without rerunning locally.

**Tasks:**
- Add TRX output to each per-project test command using the repository's TUnit/Microsoft.Testing.Platform syntax: `-- --report-trx --report-trx-filename <project>.trx`.
- Upload TRX artifacts on success and failure.
- Add failure summaries that list failed projects and artifact locations.
- Defer coverage until TRX/test artifacts are stable; do not couple coverage-badge work to the first CI correctness PR.

**Acceptance Criteria:**
- CI uploads TRX artifacts for every fast test project.
- Failed runs expose enough evidence for triage from GitHub Actions.
- Coverage remains explicitly deferred or separately planned.

**Effort:** S/M

### Phase 1C — CI Efficiency and Restore Policy

**Goal:** Reduce wasted CI work and introduce deterministic restore only after lock-file coverage is understood.

**Tasks:**
- Refactor `_build-test.yml` so Postgres starts only for integration lanes.
- Evaluate `dotnet restore --locked-mode`; enable it in GitHub Actions and deployable Dockerfile restore layers where all relevant lock files support it.
- Keep `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj` out of the fast PR lane; add it in Phase 7 as nightly/manual.
- Align `agent-context.yml` with `global.json` by using `actions/setup-dotnet@v4` with `global-json-file: global.json` and cache settings.
- Add `permissions: contents: read` to read-only validation workflows.

**Acceptance Criteria:**
- Non-integration CI no longer starts Postgres unnecessarily.
- `agent-context.yml` uses the same SDK pin as the rest of the repo.
- Locked restore behavior is documented before it is made universal.

**Effort:** M

### Phase 2 — Dedicated OpenAPI Contract Guard

**Goal:** Ensure API-surface changes regenerate OpenAPI and the Blazor client when required, while avoiding false positives for unrelated changes.

**Recommended Workflow:** `.github/workflows/openapi-contract.yml`

**Trigger Strategy:**
- Always support `workflow_dispatch`.
- Include `pull_request` and `merge_group`.
- Prefer an always-running lightweight required workflow/job over `paths`-skipped required workflows. A first job can detect relevant changes and report `no-op` success when no API contract surface changed.

**Relevant Change Detection Inputs:**
- `Explore.API/Controllers/**`
- `Explore.API/OpenApi/**`
- `Explore.API/Attributes/**`
- `Explore.API/Extensions/ApiVersioningExtensions.cs`
- `Explore.API/Program.cs`
- `Explore.Application/**/*.cs` for DTO/request/response/query/command changes exposed through controllers
- `Explore.Domain/**/*.cs` only where domain types leak into API DTOs or OpenAPI transformers
- `Explore.API/Explore.API.csproj`
- `Event.API.IntegrationTests/Features/ApiContractInventoryGeneratorTests.cs`
- `Explore.Blazor.Client/nswag.json`
- `Explore.Blazor.Client/Explore.Blazor.Client.csproj`
- `Explore.API/swagger.json`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md`

**Guard Steps:**
1. Checkout with full enough history to compare the base branch when needed.
2. Setup .NET from `global.json` with NuGet cache.
3. Restore using locked mode where compatible.
4. Build required API/test/client projects.
5. Build `Explore.API/Explore.API.csproj` to refresh `Explore.API/swagger.json` through build-time OpenAPI generation.
6. Build `Explore.Blazor.Client/Explore.Blazor.Client.csproj` so the existing `GenerateApiClient` target runs NSwag and refreshes `Explore.Blazor.Client/Clients/EventApiClient.g.cs` when `Explore.API/swagger.json` changed.
7. Defer `Event.API.IntegrationTests/Features/ApiContractInventoryGeneratorTests.cs` from the strict PR2 guard until its timestamped output is normalized or regenerated in a deterministic mode.
8. Run contract invariant tests that are stable today; explicitly leave currently skipped tests alone until their stabilization story enables them.
9. Fail on uncommitted generated drift:
   - `git diff --exit-code -- Explore.API/swagger.json Explore.Blazor.Client/Clients/EventApiClient.g.cs`
10. Upload generated OpenAPI, generated client diff, TRX reports, and logs as artifacts when the guard runs.
11. Add a clear job summary explaining which generated files must be committed.

**Exact Command Pattern:**

```bash
dotnet build Explore.API/Explore.API.csproj \
  --configuration Release \
  --no-restore \
  --verbosity minimal

dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj \
  --configuration Release \
  --verbosity quiet

git diff --exit-code -- \
  Explore.API/swagger.json \
  Explore.Blazor.Client/Clients/EventApiClient.g.cs
```

If action inventory generation is included in the guard, extend the diff command to include `dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md` after running the inventory generator test.
For PR2, exclude the action inventory from the strict drift guard because the current generator writes a timestamped `Generated:` line; promote it only after normalizing that output or adding deterministic generation mode.

**Breaking-Change Layer:**
- Pre-1.0: run `oasdiff` as advisory or warning artifact against the base branch.
- At/after 1.0: make breaking-change detection blocking unless the PR includes approved versioning/changelog evidence.
- Keep Spectral/OpenAPI linting optional at first; promote once rules are stable.

**Acceptance Criteria:**
- API contract changes fail PRs unless regenerated artifacts are committed in the same PR.
- Unrelated changes pass without forcing regeneration.
- Drift failures tell contributors exactly which command or test to run and which files to commit.
- The guard uses the existing repo path: `Explore.API` build-time OpenAPI generation → `Explore.API/swagger.json` → NSwag → `EventApiClient.g.cs`.
- Running the OpenAPI guard twice on the same commit produces zero diff on the second run.

**Effort:** M

### Phase 3 — Workflow Hygiene and Least-Privilege Permissions

**Goal:** Reduce GitHub Actions attack surface with low-noise, high-value hardening before broad SHA pinning.

**Tasks:**
- Add explicit top-level/job-level `permissions` to every workflow.
- Replace `cerbos/cerbos-setup-action@v1` with a deterministic Cerbos version instead of `version: "latest"`.
- Add `timeout-minutes` and `concurrency` to all workflows/jobs where missing.
- Audit path filters so security-relevant files are not excluded from security checks.
- Add `merge_group` to required workflows if the repository uses GitHub merge queue.

**Acceptance Criteria:**
- Every workflow/job declares least-privilege permissions.
- Cerbos no longer uses mutable `latest`.
- Required checks are not accidentally bypassed or left pending due to path-filter behavior.

**Effort:** M

### Phase 4 — Action Pinning, Security Scanning, CodeQL, and Dependency Evidence

**Goal:** Make security gates meaningful and reviewable.

**Tasks:**
- Configure Dependabot/Renovate for `github-actions` updates before introducing full action SHA pinning.
- Keep full-length SHA pinning as a follow-up slice after update automation is observed and branch-protection impacts are known.
- Update `codeql.yml` so C# analysis moves from `build-mode: none` to manual build or autobuild if generated code, source generators, project-specific build flags, or private feeds affect the C# code graph. For this repo, prefer manual build because the canonical Release build and preview SDK are already pinned by `global.json`.
- Include PR coverage for `develop` if `develop` remains a protected integration branch.
- Add dependency review where available for pull requests.
- Add secret scanning/push protection at repository/org settings level; document this as required platform configuration.
- Expand `security-tests.yml` paths to include BFF token forwarding, API authentication middleware, forwarded-header trust, setup-secret handling, security headers, Keycloak realm/config changes, and authorization provider implementations.
- Upload CodeQL/SARIF/security test outputs where GitHub does not already preserve them.

**Acceptance Criteria:**
- Update automation exists before any required gate is converted to full-length action SHA references.
- C# CodeQL analyzes built code when the build graph materially affects analysis accuracy.
- Security path filters cover actual auth/security trust-boundary files.
- Security findings are visible and triageable before being made broadly blocking.

**Effort:** M

### Phase 5 — Container Build, SBOM, Provenance, and Image Promotion

**Goal:** Build once, verify once, deploy the same immutable artifact.

**Tasks:**
- Extract common API/UI image build logic from both Coolify deploy workflows into reusable `_container-build.yml`.
- Build and push images by commit SHA and immutable digest; keep `latest`/`develop` tags as convenience aliases only.
- Enable Buildx SBOM/provenance and GitHub artifact attestations for GHCR-published images.
- Add image vulnerability scanning before the Coolify deploy webhook is triggered.
- Store image digests as workflow outputs, retained JSON evidence, and job summaries.
- Upload build records, digest evidence, and vulnerability scan output with retention.
- Review Dockerfiles for base-image digest strategy and labels without blocking the first workflow refactor.

**Deployment Artifact Decision Gate:**
- Confirm whether the current Coolify webhook can deploy an explicit image digest during Phase 6.
- If yes, deploy the digest directly.
- If no, push an immutable commit-SHA tag, configure Coolify to deploy that exact tag, and record the resolved digest after deploy.
- Never use mutable `latest` as the production source of truth.

**Acceptance Criteria:**
- Container build jobs emit immutable digest evidence, Buildx SBOM/provenance, GHCR artifact attestations, and vulnerability scan output for each deployable image.
- Coolify deployment still uses the existing webhook-configured image source until Phase 6 decides digest vs immutable commit-SHA tag consumption.
- Job summaries name the image, digest, tags, commit SHA, and source workflow run.

**Effort:** L

### Phase 6 — Protected Deployments and Post-Deploy Smoke Checks

**Goal:** Make staging and production deployments safe, auditable, and reversible.

**Tasks:**
- Add `environment: staging` and `environment: production` jobs while keeping the current workflow files stable.
- Configure production required reviewers, branch restrictions, environment-scoped secrets, and optional wait timers in GitHub settings.
- Add deploy concurrency by environment.
- Replace raw curl deploy calls with timeout, retry, HTTP status validation, redacted logging, transport-failure summaries, and explicit failure handling.
- Add bounded post-deploy smoke checks against `/alive` and `/health` when environment URL variables are configured; follow `docs/OPERATIONS.md` semantics and keep retry budgets inside the deploy job timeout.
- Add rollback instructions, environment URLs, health result, and workflow links to job summaries and retained deploy artifacts.
- Document the digest-preferred deployment path and immutable SHA tag fallback until Coolify capability is confirmed.

**Acceptance Criteria:**
- Production deploy requires protected environment approval after GitHub repository environment rules are configured.
- Only one deployment per environment runs at a time.
- Deployment fails with retained summary/artifact evidence if Coolify webhook or configured smoke checks fail.
- Job summary contains environment, commit SHA, health result, workflow link, and rollback note.

**Effort:** L

### Phase 7 — Nightly/Manual E2E, Runtime, and Drift Lanes

**Goal:** Cover expensive or environment-heavy checks without slowing the fast PR loop.

**Tasks:**
- Add nightly/manual `e2e.yml` using Aspire orchestration where appropriate:
  - the current E2E project starts AppHost internally via `Aspire.Hosting.Testing`
  - do not start a duplicate external AppHost for this suite
  - per-project E2E test execution
  - TRX, Playwright browser artifacts, test logs, and Docker diagnostics on failure
- Run `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj` in this lane.
- Add scheduled full integration/auth/Cerbos/policy contract runs.
- Add scheduled OpenAPI breaking-change report against default branch if not yet blocking.

**Acceptance Criteria:**
- E2E lane runs manually and nightly with artifacts.
- Long-running failures are visible without blocking every PR until reliability is proven.
- Runtime-lane TRX, browser artifacts, test logs, and Docker diagnostics are retained on failure.

**Effort:** M/L

### Phase 8 — Documentation, Release Checklist, and Branch Protection Alignment

**Goal:** Make the new CI/CD contract maintainable.

**Tasks:**
- Add `docs/CI_CD_GOVERNANCE.md` as the central source for branch protection, required/advisory gates, GitHub Environment settings, fork PR policy, generated-artifact review, artifact retention, and badge policy.
- Update `docs/TESTING.md` to include `Explore.Infrastructure.Tests`, advisory runtime lanes, and CI/CD governance links.
- Update `docs/GOVERNANCE.md` to link the CI/CD governance reference and exact OpenAPI drift behavior.
- Update `docs/RELEASE_CHECKLIST.md` with SBOM/provenance, deployment approval, smoke-check, branch-protection, and repository-security evidence requirements.
- Update `docs/OPERATIONS.md` with deployment protection and CI smoke-check expectations.
- Add contributor instructions for resolving OpenAPI drift in `docs/TROUBLESHOOTING.md`.
- Remove README badges for coverage/SonarCloud until those workflows are implemented.
- Configure repository branch protection required checks after workflow names stabilize.

**Acceptance Criteria:**
- Contributor docs explain how to fix each new CI failure class.
- Required checks match final workflow/job names.
- Release checklist includes artifact, approval, digest, and smoke-check evidence.
- README badges represent implemented gates only.

**Effort:** M

## OpenAPI Contract Guard Details

### Why This Guard Exists

The project already treats OpenAPI as a governed artifact: controller signatures, routes, API versioning, `ProducesResponseType`, operation IDs, endpoint classifications, and DTO shapes are part of the public contract. Because `Explore.Blazor.Client` regenerates its typed client from `Explore.API/swagger.json`, stale OpenAPI artifacts can cause generated-client drift and hidden integration failures.

### What Counts as an API-Surface Change

Regeneration is expected when a PR changes emitted contract behavior, including:

- route templates, HTTP verbs, API versions, or version readers;
- request DTOs, response DTOs, validation-visible shape, enum/lookup exposure, pagination models, or problem details;
- authorization metadata that appears in OpenAPI;
- operation IDs, endpoint classification, tags, schema transformers, HAL wrappers, or OpenAPI document settings;
- NSwag settings that affect `EventApiClient.g.cs`;
- generated action inventory logic.

Regeneration is not expected for unrelated implementation-only changes that do not alter the emitted schema or generated client.

### Local Developer Fix Path

The CI failure should tell contributors to run the repository-approved regeneration path, not hand-edit files:

```bash
dotnet build Explore.API/Explore.API.csproj \
  --configuration Release \
  --verbosity minimal

dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj \
  --configuration Release \
  --verbosity quiet

git diff --exit-code -- \
  Explore.API/swagger.json \
  Explore.Blazor.Client/Clients/EventApiClient.g.cs
```

Then review the generated diff for intentionality and commit generated artifacts with the API change. If the action inventory is part of the PR, run the inventory generator and include `dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md` in the review/diff.

### Generated Artifact Review Rules

Reviewers must inspect generated artifact diffs for:

- operation ID changes;
- route additions, removals, or template changes;
- authorization/security metadata changes;
- request/response DTO shape changes;
- HAL link/action/schema changes;
- generated client method renames or suffix drift;
- removed or renamed endpoints.

### CI False-Positive Controls

- Use `Explore.API` build-time OpenAPI generation instead of a local developer HTTPS profile or retired integration-test exporter.
- Keep generated JSON pretty-printed and deterministic.
- Normalize or document known volatile fields before enforcing strict diff.
- Avoid skipped required workflows; prefer always-running required guard with internal no-op detection.
- Upload generated artifacts on failure so reviewers can inspect drift without rerunning CI.
- Prove determinism before requiring the guard by running it twice on the same commit and verifying the second run produces zero diff.

## Fork and External PR Security Policy

- External fork PRs run validation with read-only `GITHUB_TOKEN` permissions and without deployment secrets, registry write credentials, environment secrets, or privileged tokens.
- Use `pull_request` for untrusted code validation. Do not use `pull_request_target` for build/test/generation jobs unless a separate threat-model review proves checked-out code cannot exfiltrate secrets or modify privileged execution.
- Deploy, package-publish, attestation, and environment-secret jobs must run only from trusted branches/tags or approved environments.
- Artifacts uploaded from fork PRs are evidence only; do not consume them in privileged deployment workflows without rebuilding from trusted refs.

## Artifact Retention Policy

| Artifact | Retention |
|---|---:|
| TRX test results and test logs | 14–30 days |
| OpenAPI drift artifacts and generated diffs | 30 days |
| Security scan outputs not already retained by GitHub | 90 days |
| SBOM/provenance for release images | Release lifetime / long-lived |
| Deployment logs, summaries, smoke-check evidence | At least 90 days; longer for production |

## Dependencies

- GitHub repository/admin settings for branch protection, required checks, environments, secret scanning, and push protection.
- Clean Docker image build validation for the locked restore path after unrelated workspace compile/package issues are resolved.
- Stable OpenAPI generation path through `Explore.API` build-time OpenAPI generation and NSwag.
- Decision on coverage provider and README badges: Codecov/SonarCloud implementation vs badge removal.
- Decision on container registry capabilities for attestations and digest-based deployment.
- Coolify webhook behavior and health endpoint reachability from GitHub-hosted runners.

## Success Metrics

- 100% of required workflows declare explicit least-privilege permissions.
- 100% of fast test projects run in CI, including `Explore.Infrastructure.Tests`.
- OpenAPI drift guard blocks stale `Explore.API/swagger.json` and `EventApiClient.g.cs` diffs.
- TRX artifacts are available for every test job.
- Coverage artifacts or documented badge removal resolves README badge mismatch.
- Production deploys require protected environment approval.
- Deploy summaries include commit SHA, image digest, environment, approver, and smoke-check status.
- Container images include SBOM/provenance evidence before production deployment.
- E2E/nightly lane retains logs/artifacts for failures.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| OpenAPI generation is non-deterministic | False CI failures | Use `Explore.API` build-time OpenAPI generation, normalize output, upload failure artifacts, enforce after stability |
| Path-filtered required workflow is skipped | PRs blocked with pending checks | Use always-running required workflow with internal no-op job |
| Too many checks slow PRs | Developer friction | Keep fast PR gate mandatory; schedule or manually trigger expensive lanes first |
| Integration/E2E flakiness | Loss of trust in gates | Isolate services, use Aspire logs/artifacts, promote to required only after reliability data |
| Static Coolify secrets remain | Deployment credential exposure risk | Move secrets to GitHub Environments; prefer OIDC where registry/platform supports it |
| Deploy rebuilds differ from tested artifacts | Production mismatch | Build once, record digest, deploy digest |
| Action SHA pinning increases maintenance | Stale actions if unmanaged | Add Dependabot/Renovate updates for GitHub Actions |
| CodeQL build mode increases runtime | Longer security scans | Keep CodeQL separate and scheduled; tune after first stable runs |
| Correct NuGet audit surfaces existing vulnerabilities | CI becomes red before package remediation is done | Keep the supported JSON audit implementation, remediate vulnerable packages, and decide whether transitive findings are required or advisory before branch-protection enforcement |

## External Research Sources Used

- GitHub reusable workflows: https://docs.github.com/en/actions/reference/workflows-and-actions/reusable-workflows
- GitHub workflow syntax and permissions: https://docs.github.com/actions/reference/workflows-and-actions/workflow-syntax
- GitHub security hardening for Actions: https://docs.github.com/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions
- GitHub OIDC reference: https://docs.github.com/en/actions/reference/security/oidc
- GitHub deployment environments: https://docs.github.com/en/actions/concepts/use-cases/deploying-with-github-actions
- GitHub concurrency: https://docs.github.com/en/actions/using-jobs/using-concurrency
- GitHub required status checks: https://docs.github.com/en/articles/about-required-status-checks
- GitHub required-check troubleshooting and skipped workflow behavior: https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/troubleshooting-required-status-checks
- GitHub deployment environments and environment secrets: https://docs.github.com/en/actions/reference/environments
- GitHub artifact attestations: https://docs.github.com/actions/concepts/security/artifact-attestations
- GitHub CodeQL build modes for compiled languages: https://docs.github.com/code-security/reference/code-scanning/codeql/codeql-build-options-and-steps-for-compiled-languages
- GitHub dependency review action: https://github.com/actions/dependency-review-action
- GitHub artifact attestations and Docker attestations: https://docs.docker.com/build/ci/github-actions/attestations/
- Microsoft ASP.NET Core OpenAPI docs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0
- Microsoft OpenAPI document usage docs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0
- Microsoft .NET OpenAPI/versioning blog: https://devblogs.microsoft.com/dotnet/api-versioning-in-dotnet-10-applications/
- Spectral CLI docs: https://github.com/stoplightio/spectral/blob/9b095b58ea5e313ba025afb26755f61f6852f038/docs/guides/2-cli.md
- oasdiff docs/action: https://github.com/oasdiff/oasdiff/blob/7e23358418ec870d9dd94bcaaa9e370afce92732/docs/DIFF.md and https://github.com/oasdiff/oasdiff-action/blob/dece4e9d3b17145ba3850f21a611b0cc9c96c2d1/README.md
- Docker reusable Buildx workflow example: https://github.com/docker/github-builder/blob/c2782c55efa56a01b9c30021db8f5ec3993228a3/.github/workflows/build.yml
- .NET Docker test workflow example: https://github.com/dotnet/dotnet-docker/blob/77ef40ed3fbce43828ccec2b143671487a0fbf64/.github/workflows/update-dependencies-tests.yml

## Effort Estimate

- Phase 0: S
- Phase 1A: S/M
- Phase 1B: S/M
- Phase 1C: M
- Phase 2: M
- Phase 3: M
- Phase 4: M
- Phase 5: L
- Phase 6: L
- Phase 7: M/L
- Phase 8: M

Overall rollout: **XL**, best delivered in incremental PRs. First useful milestone is **M/L**: Phase 1 + Phase 2 + minimal least-privilege permissions.

ABOUTME: Task breakdown for implementing enterprise-grade GitHub Actions hardening.
ABOUTME: Tracks phased acceptance criteria including the dedicated OpenAPI contract guard.

# Enterprise CI/CD Hardening Tasks

Last Updated: 2026-05-07

## Phase 0 — Baseline Decisions

### Tasks

- [ ] Inventory current GitHub branch-protection required checks.
- [ ] Decide final workflow/job names before renaming or replacing existing workflows.
- [ ] Create/confirm GitHub Environments: `staging`, `production`.
- [ ] Define production environment rules: required reviewers, branch restrictions, environment-scoped secrets, optional wait timer.
- [ ] Decide whether `develop` staging deployments need reviewers or only concurrency + environment secrets.
- [ ] Decide whether Codecov/SonarCloud badges will be implemented or removed from `README.md`.
- [x] Document required-check strategy for path-sensitive workflows to avoid skipped required workflow pending states.
- [x] Document CI ownership model:
  - [x] Build/test: core maintainers.
  - [x] OpenAPI contract: API/platform maintainers.
  - [x] Security/Cerbos: security/platform maintainers.
  - [x] Deployment: release operators.
  - [x] E2E/runtime: UI/platform maintainers.
- [x] Document required vs advisory matrix before adding new blocking checks.
- [ ] Confirm first implementation PRs keep current workflow names unless branch protection is migrated.

### Acceptance Criteria

- [x] Required-check migration plan is documented.
- [x] Deployment environment policy is documented.
- [x] Badge/coverage strategy is decided.
- [ ] Workflow rename/consolidation is explicitly deferred or paired with branch-protection migration.
- [ ] Required/advisory status is defined for build, tests, OpenAPI drift, `oasdiff`, Spectral, E2E, SBOM/provenance, and production smoke checks.

## Phase 1A — Test Inventory Correctness

### Tasks

- [x] Add `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` to CI.
- [x] Update `docs/TESTING.md` to list `Explore.Infrastructure.Tests`.
- [x] Keep per-project test execution; do not introduce solution-level `dotnet test`.
- [x] Keep `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj` out of the fast PR lane.

### Acceptance Criteria

- [x] Fast PR CI runs every non-E2E required test project.
- [x] Documentation test inventory matches actual test projects.
- [x] No workflow uses solution-level `dotnet test`.

## Phase 1B — Test Evidence Artifacts

### Tasks

- [x] Add TRX logging to each `dotnet test --project ...` call.
- [x] Use deterministic TUnit result files under each project's `TestResults` output.
- [x] Upload TRX artifacts on success and failure.
- [x] Add failure summaries with failed project names and artifact links.
- [ ] Keep coverage evaluation out of this PR unless TRX artifacts are already stable.

### Acceptance Criteria

- [x] CI artifacts include test results.
- [x] Failed test jobs can be triaged from GitHub Actions artifacts without immediate rerun.

## Phase 1C — CI Efficiency and Restore Policy

### Tasks

- [x] Add explicit read-only permissions to `test.yml`, `_build-test.yml`, and `agent-context.yml`.
- [x] Update `agent-context.yml` to use `global-json-file: global.json`.
- [x] Refactor `_build-test.yml` so Postgres starts only for integration lanes.
- [x] Evaluate `dotnet restore --locked-mode`; enable where all relevant lock files support it or split lock-file normalization into a separate PR.
- [x] Normalize deployable Dockerfiles so container build stages copy root restore inputs, project files, and relevant `packages.lock.json` files before `dotnet restore --locked-mode`.

### Acceptance Criteria

- [x] Non-integration PR CI does not start unnecessary database services.
- [x] `agent-context.yml` uses the repository `global.json` SDK pin.
- [x] Locked restore behavior is documented before it becomes universal.
- [x] API and Blazor Dockerfiles use locked restore with all required restore inputs present before restore.

## Phase 2 — Dedicated OpenAPI Contract Guard

### Tasks

- [x] Add `.github/workflows/openapi-contract.yml` or a dedicated required job in an existing orchestrator.
- [x] Include triggers: `pull_request`, `merge_group`, and `workflow_dispatch`.
- [x] Implement an always-running no-op detector for unrelated changes instead of relying only on path-filtered required workflows.
- [x] Widen detector coverage for API project/startup/config files and API integration test project metadata so contract-affecting changes do not silently no-op.
- [x] Setup .NET using `global.json` and NuGet cache.
- [x] Restore/build required API, integration-test, and Blazor client projects.
- [x] Regenerate the OpenAPI contract through the API build-time OpenAPI target:
  - [x] `dotnet build Explore.API/Explore.API.csproj --configuration Release --no-restore --verbosity minimal`
- [x] Build `Explore.Blazor.Client/Explore.Blazor.Client.csproj` to trigger NSwag and regenerate `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
  - [x] `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet`
- [ ] Run `Event.API.IntegrationTests/Features/ApiContractInventoryGeneratorTests.cs` only after its timestamped output is normalized or deterministic mode exists.
- [x] Run stable OpenAPI contract invariant tests.
- [ ] Do not unskip currently skipped contract/client tests until a separate stabilization task proves them reliable.
- [x] Add drift check: `git diff --exit-code -- Explore.API/swagger.json Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- [ ] Include `dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md` in the diff only when generated in the job.
- [x] Prove determinism by running the guard twice on the same commit and verifying the second run produces zero diff.
- [x] Upload generated OpenAPI/client artifacts and TRX reports when the guard runs.
- [x] Add job summary explaining exact local fix steps.
- [ ] Add advisory `oasdiff` report against base branch.
- [ ] Add Spectral/OpenAPI linting only after rules are agreed and low-noise.
- [ ] Add generated-artifact review guidance for operation IDs, routes, auth metadata, DTO shapes, HAL links/actions, generated client method renames, and removed/renamed endpoints.

### Acceptance Criteria

- [x] API-surface changes fail CI when generated artifacts are stale.
- [x] Unrelated PRs pass the OpenAPI guard without unnecessary regeneration requirements.
- [x] Drift failures identify exact generated files that must be committed.
- [x] Generated artifacts are available from the workflow run.
- [x] Second run on unchanged generated artifacts produces no diff.

## Phase 3 — Workflow Hygiene and Least-Privilege Permissions

### Tasks

- [x] Add explicit `permissions` to every workflow/job.
- [x] Use `contents: read` for validation-only jobs.
- [ ] Restrict `security-events: write` to CodeQL/SARIF publishing jobs.
- [x] Restrict `packages: write`, `attestations: write`, and `id-token: write` to image/attestation/deployment jobs.
- [x] Replace Cerbos `latest` with a fixed Cerbos version.
- [x] Add missing `timeout-minutes`.
- [x] Add missing `concurrency` groups, especially deploy-by-environment.
- [x] Review path filters for security blind spots.
- [x] Add external fork PR policy: no deployment secrets, registry write credentials, environment secrets, or privileged tokens for untrusted PRs.
- [x] Avoid `pull_request_target` for untrusted build/test/generation jobs unless a threat-model review approves the exact pattern.

### Acceptance Criteria

- [x] No workflow relies on implicit broad token permissions.
- [x] Cerbos no longer uses mutable `latest`.
- [x] Fork PR validation is read-only and cannot access deploy credentials.

## Phase 4 — Action Pinning and Security Gates

### Tasks

- [x] Configure Dependabot or Renovate for `github-actions` updates.
- [x] Pin external GitHub Actions to full-length SHAs after update automation exists.
- [x] Update `codeql.yml` so C# uses manual build or autobuild if generated code, source generators, project-specific build flags, or private feeds affect the code graph.
- [x] Prefer manual CodeQL build for this repo unless implementation proves autobuild handles the pinned preview SDK and canonical build correctly.
- [x] Include `develop` PR coverage if `develop` remains protected.
- [x] Add dependency review workflow if available for the repository plan.
- [ ] Confirm secret scanning and push protection at repository/org settings level.
- [x] Expand `security-tests.yml` path coverage for BFF/API auth and trust-boundary files.
- [x] Upload security test and Cerbos policy logs as artifacts; TRX-style test results remain a later enhancement if required for these lanes.
- [x] Keep Cerbos policy checks deterministic and artifact-backed.

### Acceptance Criteria

- [x] CodeQL scans built C# code.
- [x] No required gate uses unmanaged mutable external action references.
- [x] Action update automation is documented and enabled.
- [x] Auth/security path filters cover BFF token forwarding, forwarded headers, setup-secret handling, Keycloak config, runtime authorization providers, and Cerbos policy code.
- [x] Security failures are inspectable from artifacts/logs.

## Phase 5 — Containers, SBOM, Provenance

### Tasks

- [x] Extract common image build logic from production/develop deploy workflows into `_container-build.yml`.
- [x] Build API/UI images once per deploy workflow run using Buildx.
- [x] Tag images by commit SHA and record immutable digests.
- [x] Keep `latest` and `develop` tags only as convenience aliases.
- [x] Add SBOM generation through Buildx `sbom: true`.
- [x] Add Buildx provenance and GitHub artifact attestations for GHCR image digests.
- [x] Add image vulnerability scanning before the Coolify deployment webhook is triggered.
- [x] Upload build records, digest evidence, and vulnerability scan output artifacts.
- [x] Write image digest outputs to job summary.
- [ ] Complete Coolify capability decision gate:
  - [ ] Confirm whether Coolify webhook can deploy an explicit image digest.
  - [ ] If yes, deploy the digest directly.
  - [ ] If no, deploy immutable commit-SHA tag and record resolved digest after deploy.
  - [ ] Ensure mutable `latest` is not production source of truth.

### Acceptance Criteria

- [ ] Deployments consume validated image digests.
- [x] Coolify digest/tag fallback decision is documented before production deploy changes merge.
- [x] Each deployable image has SBOM/provenance evidence.
- [x] Image build logs and metadata are retained.

## Phase 6 — Protected Deployments

### Tasks

- [ ] Replace duplicated deploy workflows with one reusable deploy workflow.
- [ ] Add environment input: `staging` or `production`.
- [x] Add `environment: staging` and `environment: production` on deploy jobs.
- [x] Document deployment secrets as GitHub environment secrets while preserving current names.
- [ ] Prefer OIDC where registry/deployment target supports it.
- [x] Add deploy concurrency by environment.
- [x] Harden Coolify webhook call with timeout, retry, HTTP status validation, transport-failure summaries, and redacted logs.
- [x] Add bounded post-deploy `/alive` and `/health` smoke checks when environment URL variables are configured.
- [x] Add deployment summary with commit SHA, environment, health result, workflow link, and rollback instructions.
- [x] Define deployment artifact/log retention of at least 90 days for production evidence.

### Acceptance Criteria

- [ ] Production deployment requires environment approval in GitHub repository settings.
- [x] Only one deployment per environment can run at once.
- [x] Failed webhook or configured smoke check fails the deploy workflow with retained summary/artifact evidence.
- [x] Deployment record is auditable from job summary/artifacts.

## Phase 7 — Nightly and Manual Runtime Lanes

### Tasks

- [x] Add manual/nightly E2E workflow.
- [x] Use Aspire orchestration where applicable; current E2E suite starts AppHost internally through `Aspire.Hosting.Testing`, so no external `aspire start` wrapper is used.
- [x] Run `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj`.
- [x] Upload TRX, Playwright traces/screenshots/videos, test logs, and Docker diagnostics on failure.
- [x] Add scheduled full security/auth/Cerbos/policy contract run.
- [ ] Add scheduled OpenAPI breaking-change report if not yet blocking.

### Acceptance Criteria

- [x] E2E workflow can run manually and on schedule.
- [x] Failure artifacts are sufficient to debug without rerunning immediately.
- [x] Long-running lanes are not merge-blocking until reliability is proven.

## Phase 8 — Documentation and Governance Updates

### Tasks

- [x] Update `docs/TESTING.md` with new CI lanes and all test projects.
- [x] Update `docs/GOVERNANCE.md` with exact OpenAPI drift guard behavior.
- [x] Update `docs/TROUBLESHOOTING.md` with OpenAPI drift fix steps.
- [x] Update `docs/RELEASE_CHECKLIST.md` with SBOM/provenance/deploy approval/smoke-check evidence.
- [x] Update `docs/OPERATIONS.md` if smoke-check semantics or deployment runbooks change.
- [x] Document branch-protection required checks after workflow names stabilize.
- [x] Add or update `.github/dependabot.yml` documentation for GitHub Actions updates.
- [x] Add `docs/CI_CD_GOVERNANCE.md` as the central required/advisory checks, fork PR policy, generated-artifact review, and artifact-retention reference.
- [x] Remove README Codecov/SonarCloud badges until coverage/SonarCloud workflows exist.

### Acceptance Criteria

- [x] Contributors can fix CI failures using docs alone.
- [x] Release checklist captures all new enterprise evidence.
- [x] Branch protection required checks match final workflow/job names.
- [x] Artifact retention policy is documented:
  - [x] TRX/test logs: 14–30 days.
  - [x] OpenAPI drift artifacts: 30 days.
  - [x] Security outputs: 30 days in workflow artifacts; repository security alerts retained by GitHub.
  - [x] SBOM/provenance: release lifetime / long-lived outside expiring GitHub artifacts.
  - [x] Deploy logs/summaries: at least 90 days, longer for production.

## Final Verification Checklist

- [ ] All modified workflow files are syntactically valid YAML.
- [ ] All changed workflows use explicit least-privilege permissions.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] Per-project tests pass for affected lanes.
- [ ] OpenAPI guard detects stale generated artifacts in a controlled test PR or local simulation.
- [ ] OpenAPI guard runs twice on the same commit with zero diff on the second run.
- [ ] TRX/artifact upload works on failure.
- [ ] CodeQL/security workflows still publish expected results.
- [x] Container workflow emits image digest, SBOM, and provenance.
- [ ] Deploy workflow records environment and smoke-check status; image digest consumption/recording remains tied to the Coolify digest-vs-immutable-tag decision gate.
- [x] Documentation updates link to exact workflows and generated artifact paths.

## Potential Risks & Unknowns

- [ ] Branch-protection settings are outside the local repo and must be verified in GitHub.
- [ ] GitHub Environment configuration is outside the local repo and must be created/verified in GitHub.
- [ ] Coolify webhook support for digest-based deploys needs confirmation.
- [ ] If digest deploy is unsupported, immutable commit-SHA tag deployment plus resolved-digest recording must be implemented.
- [ ] Docker image build validation still needs a clean CI run because the local workspace has unrelated compile/package issues.
- [ ] The corrected NuGet audit currently detects existing vulnerabilities; package remediation or an explicit required/advisory decision is needed before CI can be fully green.
- [ ] OpenAPI output may need normalization before strict diff is reliable.
- [ ] Some existing OpenAPI/client tests are skipped and may require separate stabilization.
- [ ] OIDC adoption depends on registry/deploy target support.
- [ ] E2E runtime stability must be measured before making it required.

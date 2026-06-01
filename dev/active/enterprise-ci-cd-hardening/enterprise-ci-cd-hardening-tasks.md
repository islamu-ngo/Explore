ABOUTME: Execution checklist for the re-baselined enterprise CI/CD hardening program.
ABOUTME: Tracks required phases, acceptance criteria, verification, and unresolved operational risks.

# Enterprise CI/CD Hardening - Task Checklist

Last Updated: 2026-06-01 Europe/Brussels

## Status Summary

- [x] Re-baseline old plan/context/tasks with senior CTO feedback.
- [x] Verify current workflow inventory.
- [x] Record research/tooling limitations for Tavily and Context7.
- [x] Run repository build baseline; latest rerun is green with warnings. Architecture tests are red because unrelated untracked AI integration code defines `AiChatRequest` outside the CQRS query namespace convention.
- [x] Add deep CTO feedback for CLA/DCO, `schemas/openapi.json`, workflow security, digest promotion, release evidence, and repository settings.
- [x] Implement Phase 0 contract and ownership baseline. Local contract/check-name inventory, CODEOWNERS owner resolution, and settings evidence are complete; repository-side settings remediation remains in Phase 8.
- [x] Implement Phase 1 current defect fixes. `Build & Test`, CodeQL, Security Integration, Cerbos Policy, and agent-context no-op wrappers plus NuGet vulnerability remediation are complete locally; repository-side required-check verification remains in Phase 8.
- [x] Implement Phase 2 CLA legal contribution gate. CLA-only posture, broad ISLAMU nonprofit inbound rights, contributor signing docs, metadata-only workflow enforcement, and C# validation are implemented locally; branch-protection requirement remains Phase 8.
- [x] Implement workflow lint/security gate. C# helper scripts, SHA-pin policy, Dependabot maintenance policy validation, blocking `actionlint`, blocking `zizmor`, retained workflow security evidence, advisory OpenSSF Scorecard SARIF evidence, and bounded `gitleaks` feedback are implemented locally; repository-side required-check configuration still needs verification.
- [x] Implement Phase 4 cache/evidence/license/triage slices now completed locally: cache-poisoning policy validation, dependency vulnerability direct/transitive/severity summaries, dependency license policy scanning, deploy-caller contract validation, fast/integration build warning logs, Security/Cerbos TRX evidence, artifact triage guidance, artifact-only coverage collection, and coverage badge policy.
- [x] Implement Phase 5 OpenAPI evidence slices. Canonical OpenAPI/client regeneration already blocks stale artifacts and deterministic second-run drift; `docs/API_CHANGELOG.md` now defines the required evidence for intentional breaking API changes; `OpenAPI Contract Guard` now fails PR/push/merge-queue runs when `oasdiff` detects breaking changes without a same-diff changelog update, while scheduled/manual reports remain evidence-only; skipped API contract tests are governed by `docs/API_CONTRACT_TEST_DEBT.md` and a C# inventory validator.
- [ ] Implement container supply-chain evidence hardening. Docker action pins were verified against current major-version tags; digest JSON, SBOM/provenance registry evidence, immutable primary-registry promotion evidence, Trivy text/SARIF artifacts, checksum manifests, pre-deploy GHCR attestation verification, selected SLSA-compatible attestation evidence path, Docker base image digest policy, and public Coolify v4.x digest/hash support research are implemented; exact live Coolify consumption proof remains.
- [ ] Implement digest promotion and deploy consolidation. Local Coolify deploy shell is now centralized through a composite action with deployment-freeze override evidence, required production smoke checks, and deploy-time expected digest resolution from retained promotion artifacts; live Coolify-side digest/tag consumption proof remains.
- [x] Implement Phase 9 reliability tracking locally. `docs/TEST_RELIABILITY.md` now inventories runtime, stress, E2E, and manual visual reliability debt, and E2E/Security/Cerbos summaries retain trend-ready owner/date/evidence guidance before advisory lanes can be promoted.
- [x] Implement Phase 10 release evidence and release-impact local gates. Release evidence bundle generation now emits JSON, Markdown, checksums, and a GitHub Release evidence section; `.github/workflows/release-impact.yml` and `.github/scripts/validate-release-impact-pr.cs` require PR-template evidence for security, migration, configuration, OpenAPI, and operator-impact changes. Repository-side required-check configuration remains Phase 8.
- [ ] Verify GitHub repository settings. API evidence was captured on 2026-06-01 and shows required repository controls are not yet release-ready: branch protection/status checks are missing and Actions policy allows all actions. Dependabot security updates / automated security fixes are enabled; `staging` and `production` environments now exist with branch/tag policies and production reviewer protection; CODEOWNERS uses `@amirakrari`, whose repo permission API reports `admin`; scheduled/manual drift evidence is implemented through `.github/workflows/repository-settings.yml`.

## Implementation Maintenance Rules

- [x] Update this tasks file after each completed implementation slice.
- [x] Update `enterprise-ci-cd-hardening-context.md` after every major decision, blocker, or verification run.
- [x] Update `enterprise-ci-cd-hardening-plan.md` when architecture or sequencing changes.
- [ ] Keep workflow changes small enough to review.
- [ ] Do not touch unrelated dirty worktree files.
- [ ] Do not mark the workstream complete until Coolify consumption proof, repository settings evidence, and release evidence maturity are done.

## Phase 0 - Contract And Repository Settings Baseline

- [x] Add `ci-cd-change` intent to `.claude/contract/intents.yaml`.
- [x] Include expected `must_read_docs`: `docs/CI_CD_GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `docs/RELEASE_CHECKLIST.md`, `docs/QUICK_REFERENCE.md`.
- [x] Include expected paths: `.github/**`, `Dockerfile` files, `Directory.Packages.props`, `global.json`, CI/CD docs, legal contribution docs.
- [x] Include minimum tests: architecture docs tests. Full Release build remains the session baseline and currently passes with warnings.
- [x] Add `.github/CODEOWNERS`.
- [x] Protect `.github/**`, Dockerfiles, dependency manifests, release docs, legal contribution docs, and CI/CD governance docs with owner review through `.github/CODEOWNERS` using `@amirakrari`, because the planned `@islamu-ngo/platform-ops` team does not currently resolve.
- [x] Add repository settings evidence checklist to `docs/CI_CD_GOVERNANCE.md` or a dedicated tracked note.
- [x] Confirm required check names before renaming workflows. `docs/CI_CD_GOVERNANCE.md` lists stable workflow/job names for `Build & Test`, `Workflow Security`, `OpenAPI Contract Guard`, CodeQL matrix jobs, `Contributor License Agreement`, `Release Impact Check`, Security Integration, Cerbos, and agent-context. Repository-side required-check configuration remains Phase 8.
- [x] Rename the new `Workflow Security` job before repository-side branch protection makes its check name stable.
- [x] Add legal contribution governance decision record placeholder: CLA vs DCO vs both, inbound scope, patent language, signature storage, and privacy retention.

### Phase 0 Acceptance

- [x] CI/CD changes have a first-class Contribution Contract route.
- [x] Privileged workflow/deployment/legal files require owner review.
- [x] Repository settings verification is explicitly tracked.

## Phase 1 - Current Pipeline Defect Fixes

- [x] Remove or replace any legacy `_build-test.yml` OpenAPI drift check for `Explore.API/swagger.json`.
- [x] Confirm `openapi-contract.yml` remains the only canonical drift gate for:
  - [x] `schemas/openapi.json`
  - [x] `docs/API_CONTRACT_INVENTORY.md`
  - [x] `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- [x] Add `merge_group` to `test.yml` if merge queue is enabled or planned.
- [x] Replace the `Build & Test` path-skipped required workflow design with an always-running detector/no-op wrapper before marking it required.
- [x] Replace Security Integration, Cerbos Policy, and agent-context path-skipped workflow designs with always-present detector/no-op wrappers before marking those workflows required.
- [x] Replace CodeQL trigger-level `paths-ignore` with an always-present detector/no-op wrapper before marking CodeQL globally required.
- [x] Correct stale `swagger.json` wording in `docs/CONTRIBUTING.md`.
- [x] Reconcile docs and workflow artifact names for the `_build-test.yml` / `openapi-contract.yml` split.
- [x] Decide NuGet vulnerability policy:
  - [x] Remediate current `MailKit` advisory warnings and keep audit blocking.
  - [x] Document that temporary advisory exceptions require owner, date, advisory URL, affected package/version, compensating control, and removal condition before weakening the workflow.

### Phase 1 Acceptance

- [x] No dead/stale OpenAPI guard remains.
- [x] Docs consistently use `schemas/openapi.json` as the canonical root schema artifact for this workflow cleanup slice.
- [x] Required checks cannot be skipped into permanent pending state locally. `Build & Test`, CodeQL, Security Integration, Cerbos Policy, and agent-context now use always-present detector/no-op designs; repository-side required-check names still need verification before branch protection is updated.
- [x] NuGet audit behavior is intentional and documented.

## Phase 2 - Contributor Legal Gate (CLA)

- [x] Read and summarize `https://contributoragreements.org/` in `docs/legal/CONTRIBUTION_GOVERNANCE.md`.
- [x] Read and summarize `https://contributoragreements.org/legal.html` in `docs/legal/CONTRIBUTION_GOVERNANCE.md`.
- [x] Read and summarize `https://contributoragreements.org/agreement-chooser.html` in `docs/legal/CONTRIBUTION_GOVERNANCE.md`.
- [x] Read and summarize `https://cla-assistant.io/` in `docs/legal/CONTRIBUTION_GOVERNANCE.md`.
- [x] Read and summarize `https://github.com/contributor-assistant/github-action` in `docs/legal/CONTRIBUTION_GOVERNANCE.md`.
- [x] Read GitHub `pull_request_target` security documentation before writing `.github/workflows/cla.yml`; documented the metadata-only threat-model requirements in `docs/legal/CONTRIBUTION_GOVERNANCE.md`.
- [x] Decide contribution legal posture with owner/legal reviewer:
  - [x] CLA only;
  - [ ] DCO only;
  - [ ] CLA plus DCO;
  - [ ] inbound=outbound with no separate agreement.
- [x] Draft or approve actual `docs/legal/CLA.md`.
- [x] Do not use SAP's sample CLA as the production agreement.
- [x] Decide signature storage: pull request body plus GitHub PR audit trail; no signature writes to protected source branches.
- [x] Avoid `contributor-assistant/github-action`; the upstream repository is archived, so the implementation uses repository-owned `.github/scripts/validate-cla-pr.cs` instead.
- [x] Add `.github/workflows/cla.yml` after threat model approval.
- [x] Avoid `contributor-assistant/github-action`, so no archived third-party CLA action pin is needed.
- [x] Scope permissions to the smallest viable set: `contents: read` and `pull-requests: read`.
- [x] Ensure the CLA workflow does not checkout, build, test, cache, or execute untrusted PR head code. It checks out the trusted base commit only and validates PR metadata.
- [x] Avoid broad `bot*` allowlists; explicitly allow only `dependabot[bot]` and `github-actions[bot]`.
- [x] Document token choice: read-only `GITHUB_TOKEN`, no signature writes.
- [x] Add enforcing `.github/PULL_REQUEST_TEMPLATE.md` CLA language with checked agreement and `CLA Signature: @github-username` evidence.
- [x] Update `docs/CONTRIBUTING.md` with the active CLA signing requirement and metadata-only validation workflow.
- [ ] Add branch protection requirement for the CLA status check after stability is proven.
- [x] Add privacy/retention note for signature metadata.

### Phase 2 Acceptance

- [x] Contributor legal status is visible as a PR check through `Contributor License Agreement`.
- [x] CLA docs and PR template agree.
- [x] The workflow cannot run untrusted PR code with write credentials.
- [x] Signature storage is auditable and not mixed into protected source branch changes.

## Phase 3 - Workflow Quality And Supply-Chain Guard

- [x] Add `.github/workflows/workflow-security.yml` or equivalent.
- [x] Run `actionlint` against `.github/workflows`.
- [x] Run `zizmor` against `.github/workflows` as retained blocking evidence for medium-or-higher findings.
- [x] Upload SARIF or retained artifacts for workflow security findings.
- [x] Add a check that rejects external `uses:` references not pinned to full-length SHAs.
- [x] Preserve path-based local reusable workflow calls.
- [x] Add/update Dependabot rules so action SHA updates remain maintainable.
- [x] Keep repository-owned CI helper scripts as file-based C# scripts run with `dotnet run <script>.cs -- <args>` and script-local `#:property RestorePackagesWithLockFile=false` directives.
- [x] Add OpenSSF Scorecard scheduled/SARIF evidence as an advisory lane with retained artifact output; promotion to required remains deferred until repository permissions and signal quality are proven.
- [x] Add `gitleaks` local secret-scanning feedback lane. PR/push/merge-queue ranges block on newly introduced leaks; scheduled/manual history scans are advisory until legacy findings are triaged or baselined.
- [x] Add `pinact` or a custom policy check if it improves SHA-pin enforcement. Implemented as `.github/scripts/validate-action-pins.cs`, which `Workflow Security` runs to reject external `uses:` references unless they are full-SHA pinned with same-line version comments while allowing local reusable workflows/actions.

### Phase 3 Acceptance

- [x] Workflow YAML changes are linted by blocking `actionlint`.
- [x] High-confidence workflow security issues block or are explicitly baselined. Local `zizmor` `1.25.2` verification now reports no medium-or-higher findings, and `Workflow Security` fails when the SARIF or text scan exits nonzero.
- [ ] Unpinned external actions cannot merge. The local `Workflow Security` gate exists, has a stable workflow/job display name, and passes; repository-side required-check configuration still needs verification before this acceptance item is complete.

## Phase 4 - Build/Test Evidence, Coverage, License, And Dependency Integrity

- [x] Keep TRX upload and job summaries for all fast and integration test lanes. Fast, integration, OpenAPI, E2E, and security integration lanes now retain TRX where applicable; `Security Integration Tests` also writes a summary with log/TRX evidence for Security and Cerbos policy-contract test lanes.
- [x] Add coverage collection only after lane stability is confirmed. `.github/workflows/coverage.yml` now collects artifact-only Cobertura coverage for the stable `Event.Domain.UnitTests` lane on schedule/manual dispatch and retains coverage, TRX, HTML, build log, and test log evidence without making coverage a required PR gate.
- [x] Decide coverage publication: artifact-only, Codecov, SonarCloud, or another provider. Initial policy is artifact-only when coverage is added; Codecov/SonarCloud/coverage badges stay forbidden until a verified workflow publishes the backing data.
- [x] Add analyzer/warnings report artifact or warnings budget. `_build-test.yml` now retains fast and integration build logs with the TRX artifacts so compiler/analyzer warning output is reviewable without rerunning locally; a numeric warnings budget remains future work.
- [x] Split dependency gates by severity/direct/transitive package type. `_build-test.yml` retains raw NuGet vulnerability JSON and a markdown summary under `artifacts/dependencies/**`; `.github/scripts/validate-nuget-vulnerabilities.cs` still blocks any vulnerable direct or transitive package, but the retained evidence now separates package relationship and advisory severity for triage.
- [x] Keep NuGet vulnerability report parsing in `.github/scripts/validate-nuget-vulnerabilities.cs` instead of embedded workflow script blocks.
- [x] Add dependency license policy scanning and document AGPL-compatible allow/deny rules. `_build-test.yml` now runs `.github/scripts/validate-dependency-license-policy.cs`, which scans product NuGet lock files, blocks denied/unknown licenses without explicit exceptions, and fails future product npm/container OS dependency surfaces until dedicated license scanning exists.
- [x] Add cache-poisoning controls for fork PRs and trusted deploy/publish workflows. `Workflow Security` now runs `.github/scripts/validate-workflow-cache-policy.cs`, which rejects direct `actions/cache`, rejects Docker GHA cache writes outside `_container-build.yml`, and rejects `setup-dotnet cache: true` in privileged deploy/container/release workflows.
- [x] Run integration tests for deploy callers and reliable scheduled lanes. `Workflow Security` now runs `.github/scripts/validate-deploy-workflow-contract.cs`, which validates production/staging deploy callers download retained build evidence, resolve expected digests, pass promotion evidence and freeze/override inputs, require production smoke checks, and call the shared Coolify deploy action for API and UI. Live Coolify consumption proof remains Phase 7/8 work.
- [x] Document how maintainers triage each artifact type in `docs/CI_CD_GOVERNANCE.md#artifact-triage-guide`.

### Phase 4 Acceptance

- [x] CI failures are triageable from GitHub Actions without immediate local rerun. Fast/integration build logs, TRX files, security/Cerbos TRX/logs, workflow-security evidence, secret scanning, Scorecard, container evidence, and deployment summaries all have retained artifacts or summaries.
- [x] Dependency vulnerability and license risk policy is explicit. `AutoMapper` and `MediatR` RPL-1.5 runtime exceptions remain visible debt that must be replaced or legally approved before alternative-license distribution.
- [x] Coverage/badge policy reflects implemented tooling only. `docs/CI_CD_GOVERNANCE.md#coverage-publication-policy` keeps current coverage artifact-only through `Coverage Evidence`, and the badge policy forbids Codecov/SonarCloud/coverage badges without verified backing workflows.

## Phase 5 - OpenAPI Contract Guard V2

- [x] Keep deterministic regeneration for:
  - [x] `schemas/openapi.json`
  - [x] `docs/API_CONTRACT_INVENTORY.md`
  - [x] `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- [x] Add `oasdiff` report against the base branch. `OpenAPI Contract Guard` installs checksum-verified `oasdiff` v1.18.1 and retains markdown/JSON reports in `openapi-contract-guard`.
- [x] Add Spectral only after lint rules are documented and low-noise. `.spectral.yaml` defines advisory rules for API title/version, operation IDs, operation tags, and response descriptions; `OpenAPI Contract Guard` retains JSON/Markdown Spectral reports without making findings blocking yet.
- [x] Resolve stale skipped API contract test ambiguity by governing all `Category: API contract` skips through `docs/API_CONTRACT_TEST_DEBT.md` and `.github/scripts/validate-api-contract-skip-inventory.cs`. The two current RouteName coverage skips remain intentionally deferred to `dev/active/api-contract-stabilization` Phase 3 with explicit owners/removal conditions; they must be enabled or removed when that stabilization work lands.
- [x] Promote breaking-change detection to blocking when policy is ready. PR/push/merge-queue runs now fail when `oasdiff` detects breaking changes without a same-diff `docs/API_CHANGELOG.md` update; breaking changes with changelog evidence remain reviewer/release evidence, and scheduled/manual runs remain evidence-only.
- [x] Require `docs/API_CHANGELOG.md` evidence for intentional breaking API changes through the manual review policy in `docs/API_CHANGELOG.md#breaking-change-evidence` and `docs/CI_CD_GOVERNANCE.md#openapi-breaking-change-evidence`.

### Phase 5 Acceptance

- [x] Stale generated artifacts block through `OpenAPI Contract Guard` drift checks for `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- [x] Breaking-change evidence is visible in PRs when intentional breaking changes follow the required `docs/API_CHANGELOG.md` evidence contract. Missing changelog evidence now blocks detected `oasdiff` breaking changes on PR/push/merge-queue runs; changeloged findings and Spectral report artifacts remain reviewer evidence.
- [x] OpenAPI/client regeneration remains deterministic on second run through the existing `OpenAPI Contract Guard` second-run drift checks.

## Phase 6 - Container Build, SBOM, Provenance, And Attestation Verification

- [x] Update Docker actions through Dependabot or a controlled PR, then pin SHAs. Verified current `_container-build.yml` pins match the current major-version refs for `docker/setup-buildx-action@v3`, `docker/login-action@v3`, `docker/metadata-action@v5`, and `docker/build-push-action@v6`; future updates remain covered by Dependabot plus SHA-pin validation.
- [x] Export SBOM/provenance registry evidence as downloadable artifacts via OCI inspect/index output.
- [x] Add Trivy SARIF output as retained container evidence; code-scanning upload remains repository-permissions dependent.
- [x] Pin Docker base images by digest and enforce the weekly Dependabot Docker update cadence through `Workflow Security`.
- [x] Verify GitHub artifact attestations before deployment through `_container-build.yml` `gh attestation verify` against the pushed GHCR digest, repository, signer workflow, source ref/digest, SLSA provenance predicate, and GitHub-hosted runner trust boundary.
- [x] Add SLSA provenance verification or document why GitHub artifact attestation is the chosen SLSA-compatible evidence path. GitHub artifact attestations are the selected SLSA-compatible provenance evidence path; `_container-build.yml` verifies the pushed GHCR digest with `gh attestation verify` against repository, signer workflow, source ref/digest, SLSA provenance predicate, and GitHub-hosted runner trust constraints.
- [x] Add release artifact integrity manifests with checksums for retained container evidence.
- [x] Record image digest, immutable promotion tags, scan result artifact paths, SBOM/provenance references, and attestation verification result in job summaries. Digest, tags, immutable promotion proof, SBOM/provenance artifact references, Trivy artifact paths, attestation verification artifact path, and checksum references are recorded in the reusable build summary.
- [x] Keep container digest evidence JSON generation in `.github/scripts/write-container-digest-evidence.cs` instead of embedded workflow script blocks.
- [x] Decide ATCR credential strategy: use scoped `ATCR_PASSWORD` environment secrets with documented 90-day/event-driven rotation until ATCR supports GitHub OIDC or another non-interactive short-lived credential exchange.
- [x] Verify ATCR OIDC/short-lived credential support status from public docs. Public ATCR docs describe ATProto OAuth/DPoP, the Docker credential helper/device flow, short-lived registry JWTs behind the helper, and fallback `docker login` with an ATProto app password, but no GitHub Actions OIDC federation path for CI pushes.
- [x] Scoped environment secret with documented rotation remains the current CI push strategy because no non-interactive GitHub OIDC path is documented for ATCR.

### Phase 6 Acceptance

- [x] Deploy cannot start without scan and attestation verification in the current workflow dependency graph because deploy jobs require `build-and-push`, which now blocks on Trivy and `gh attestation verify`.
- [x] Release evidence includes image digest and supply-chain artifacts. Container build artifacts now include digest JSON, immutable promotion evidence, OCI inspect/index evidence, Trivy text/SARIF, attestation verification JSON, and checksums.
- [x] Mutable tags are convenience aliases only. The reusable build records `sha-*` / `dev-*` primary-registry promotion tags and verifies they resolve to the built digest; mutable `latest` / `develop` tags remain non-authoritative aliases.

## Phase 7 - Unified Digest-Based Deploy Promotion

- [x] Create one reusable deploy execution path for staging and production through `.github/actions/deploy-coolify` while preserving caller workflow environment approvals and secrets.
- [x] Remove duplicated deploy shell logic from `deploy-coolify.yml` and `deploy-coolify-develop.yml` by moving Coolify webhook, smoke-check, redacted failure, and summary behavior into the local composite action.
- [x] Pass environment, component, digest, smoke URLs, and webhook secret names as explicit inputs. Current deploy jobs resolve the expected digest from retained container promotion artifacts and pass it to `.github/actions/deploy-coolify` with environment, component, smoke URL, webhook/token, registry, image, and immutable tag prefix.
- [x] Confirm whether Coolify can deploy `image@sha256:<digest>`. Public Coolify v4.x source/UI evidence shows Docker Image apps support SHA-256 hash input: `DockerImageParser` parses `image@sha256:<digest>`, the UI labels the field `Docker Image Tag or Hash`, and deployment code normalizes `sha256-*` values into digest references.
- [ ] If Coolify supports digests, configure deployment by digest.
- [x] If Coolify digest consumption is not yet configured/proven, configure immutable commit-SHA tag deployment and record resolved digest before deploy. `_container-build.yml` now records primary-registry `sha-*` / `dev-*` promotion tags and verifies each tag resolves to the built digest before dependent deploy jobs can start; post-deploy Coolify consumption proof remains separate.
- [x] Make production smoke checks mandatory. Production deploy action calls now require configured smoke URLs for deployed components, and both `/alive` and `/health` must pass before deployment evidence reports success.
- [ ] Keep production protected by GitHub Environment approval and branch restrictions.
- [x] Add deployment freeze/manual override policy with audit notes for urgent security releases. `DEPLOYMENT_FREEZE=true` blocks webhook calls unless a manual `workflow_dispatch` run supplies `override_reason`; the local deploy action writes freeze state and override reason to retained evidence.
- [x] Ensure deployment summaries include environment, component, commit SHA, digest, workflow run, smoke result, and rollback note. Coolify deploy summaries now include environment, component, commit SHA, expected full-commit immutable image tag, expected image digest, promotion evidence path, webhook result, smoke result, whether smoke was required, deployment-freeze state, override reason, workflow run, and rollback note. Exact Coolify-consumed digest remains pending Coolify consumption proof.

### Phase 7 Acceptance

- [x] One deploy execution implementation serves staging and production through `.github/actions/deploy-coolify`; the two caller workflows remain for their distinct triggers, environments, and approvals.
- [ ] Production deployment evidence proves exactly what digest was deployed. Deploy jobs now resolve and record the expected digest from retained build-side immutable tag promotion proof; Coolify-side consumption proof remains open.
- [x] Deployment failures retain redacted webhook/smoke output summaries and upload `artifacts/deploy/**` through both Coolify deploy workflows.

## Phase 8 - Repository And Organization Policy Enforcement

- [x] Verify branch/ruleset protection for `main`. 2026-06-01 API evidence: branch protection endpoint returns 404; active `main` ruleset only includes deletion, non-fast-forward, and Copilot code-review rules. Missing PR/review/required-check controls.
- [x] Verify branch/ruleset protection for `develop`. 2026-06-01 API evidence: branch protection endpoint returns 404 and no `develop` ruleset was returned. Missing expected controls.
- [x] Verify required checks match workflow/job names. 2026-06-01 API evidence: no required status checks are configured yet; check-name migration remains open.
- [x] Verify merge queue settings if used. 2026-06-01 API evidence: repository rulesets do not include a merge-queue rule.
- [x] Verify `staging` environment exists. 2026-06-01 remediation: `staging` environment was created and custom deployment branch policy allows `develop`.
- [x] Verify `production` environment exists. 2026-06-01 remediation: `production` environment was created.
- [x] Verify production required reviewers and branch/tag restrictions. 2026-06-01 remediation: `production` requires reviewer `@amirakrari`; custom deployment branch policies allow `main` and `v*`.
- [x] Verify environment secrets/variables are scoped correctly. 2026-06-01 evidence: `staging` and `production` environments now exist; secret values remain redacted and must be verified by maintainers in the GitHub UI before release.
- [x] Verify action policy for allowed actions and SHA pinning where available. 2026-06-01 API evidence: Actions are enabled and `allowed_actions` is `all`; repository policy is not restricted to SHA-pinned/verified actions.
- [x] Verify secret scanning. 2026-06-01 API evidence: secret scanning is enabled.
- [x] Verify push protection. 2026-06-01 API evidence: push protection is enabled.
- [x] Verify dependency graph. 2026-06-01 API evidence: vulnerability alerts endpoint returns 204, indicating dependency graph/dependency alerts are enabled.
- [x] Verify Dependabot security updates. 2026-06-01 remediation: automated security fixes were enabled through the GitHub API; refreshed repository-settings drift evidence no longer reports a Dependabot finding.
- [x] Verify CodeQL/code scanning alerts. 2026-06-01 API evidence: code-scanning alerts API is accessible and returned one open CodeQL alert.
- [x] Add scheduled repository-settings drift check if GitHub API permissions allow it. `.github/workflows/repository-settings.yml` runs scheduled/manual drift checks through `.github/scripts/validate-repository-settings.cs` and retains redacted `repository-settings-evidence`; current expected failures reflect missing repository-side controls.
- [x] Record redacted evidence in the context file or release checklist. The 2026-06-01 API evidence snapshot is recorded in `docs/CI_CD_GOVERNANCE.md` and this task file; context captures the release blocker status.

### Phase 8 Acceptance

- [ ] YAML and GitHub settings agree.
- [ ] Repository settings evidence is current enough for a release decision.

## Phase 9 - Runtime, E2E, Performance, And Release Evidence Maturity

- [x] Keep E2E manual/nightly until reliability supports promotion. `.github/workflows/e2e.yml` remains `workflow_dispatch`/nightly-only and now writes an E2E runtime evidence summary that points to logs, TRX, Playwright artifacts, Docker diagnostics, and `docs/TEST_RELIABILITY.md`.
- [x] Add trend summaries for E2E/security/runtime failures. E2E and Security/Cerbos scheduled/manual lanes now write summaries with trigger/ref/commit/outcomes, retained artifact pointers, and `docs/TEST_RELIABILITY.md` trend-action guidance.
- [x] Add scheduled OpenAPI breaking-change report if not already blocking. `OpenAPI Contract Guard` now runs weekly and generates retained `oasdiff` evidence without failing scheduled/manual runs.
- [x] Add scheduled performance/benchmark smoke lanes for high-risk endpoints/pages. `.github/workflows/performance-smoke.yml` runs `Event.Benchmarks` `ApiEndpointBenchmarks` in BenchmarkDotNet ShortRun mode on schedule/manual dispatch and retains logs/results as advisory evidence.
- [x] Add flaky-test tracking with owner, first-seen date, and promotion/removal criteria. `docs/TEST_RELIABILITY.md` tracks OpenFeature shutdown skips, Stress setup-secret limiter coverage, and manual visual E2E baseline skips with owners and promotion/removal criteria.
- [x] Ensure security/Cerbos scheduled failures are triaged with owner/date. `docs/TEST_RELIABILITY.md#scheduled-failure-trend-summaries` requires repeated scheduled Security/Cerbos failures to be assigned to an API/security or policy owner with first-seen date and retained evidence before promotion or baseline acceptance.
- [x] Ensure release notes copy or link long-lived evidence outside expiring GitHub artifacts when required. `.github/scripts/generate-release-evidence-bundle.cs` now emits `release-evidence-release-notes.md`, a copy/paste GitHub Release evidence section that points to attached manifest, summary, and checksum files.

### Phase 9 Acceptance

- [x] Runtime lanes provide actionable evidence without slowing every PR. E2E remains manual/nightly; Security/Cerbos remains detector/no-op for PRs and scheduled/manual for drift, with retained summaries pointing to logs, TRX, diagnostics, and reliability inventory.
- [x] Release evidence survives beyond GitHub artifact expiration where needed when the manual release process is followed: `generate-release-evidence-bundle.cs` emits long-lived JSON, Markdown, checksum, and release-notes evidence files that must be attached to the GitHub Release or copied to durable storage.

## Phase 10 - Release Automation, Compliance, And Maintainer Experience

- [x] Decide release model: manual tags, GitHub Releases, Release Drafter, semantic versioning, or conventional commits. Current model is manual semantic-version tags plus manually authored GitHub Releases; `docs/semantic_versioning/CHANGELOG.md` remains version-history source of truth and `docs/RELEASE_CHECKLIST.md#release-model` defines the release process.
- [x] Add `.github/workflows/release.yml` if release automation is selected. Release automation is not selected yet; `.github/workflows/release.yml`, Release Drafter, and semantic-release remain deferred until the release evidence bundle format is stable.
- [x] Generate release evidence bundle with commit SHA, image digests, SBOM/provenance, attestations, scans, OpenAPI diff, test summary, CLA status, and deployment smoke result. `.github/scripts/generate-release-evidence-bundle.cs` now turns downloaded CI/CD artifacts into `release-evidence.json`, `release-evidence.md`, and `release-evidence-checksums.sha256` for manual GitHub Releases.
- [x] Attach long-lived evidence or links to GitHub Releases. The current manual release process requires attaching `release-evidence.json`, `release-evidence.md`, `release-evidence-checksums.sha256`, and `release-evidence-release-notes.md`, and pasting the generated release-notes evidence section into the GitHub Release body.
- [x] Add changelog/release-note checks for security, migrations, config, OpenAPI, and operator-impact changes. `Release Impact Check` validates the PR template metadata from trusted base code and requires matching checkbox/details evidence for release-impacting paths.
- [x] Add maintainer runbooks for re-running failed gates without bypassing controls. `docs/CI_CD_RUNBOOKS.md` now documents evidence-first triage, approved rerun paths, bypass policy, and emergency override requirements for CI/CD gates.

### Phase 10 Acceptance

- [x] Release can be audited from durable evidence without relying on expired workflow artifacts when release operators attach the generated evidence bundle files and paste `release-evidence-release-notes.md` into the GitHub Release body.
- [x] Maintainers have documented override and rerun paths with approval requirements. `docs/CI_CD_RUNBOOKS.md` requires owner/date/reason/compensating-control/removal-condition evidence for emergency overrides.

## Verification Checklist

For docs-only plan updates:

- [ ] `dotnet build --configuration Release --verbosity quiet` passes. Latest run is red due unrelated application/analyzer errors in the dirty worktree; the affected `Explore.Infrastructure` project build passes after `MailKit` remediation.
- [x] Touched-file `git diff --check` passes.
- [ ] Architecture docs/context tests pass if docs/governance files are changed beyond this workstream. Latest architecture run is red due unrelated untracked `AiChatRequest` CQRS naming drift.

For workflow implementation PRs:

- [x] `actionlint` passes. Local verification used downloaded `actionlint` `1.7.12` with the checked release archive SHA-256.
- [x] `zizmor` passes or findings are explicitly baselined. Local verification used `zizmor` `1.25.2` in a temporary virtual environment; after checkout credential hardening and template-injection remediation it reports no medium-or-higher findings.
- [x] External actions remain full-SHA pinned; `dotnet run .github/scripts/validate-action-pins.cs -- .github/workflows` passed locally.
- [x] GitHub Actions SHA-pin maintenance remains covered by Dependabot; `dotnet run .github/scripts/validate-dependabot-policy.cs -- .github/dependabot.yml` passed locally.
- [x] NuGet audit report parsing remains covered by C# script; `dotnet run .github/scripts/validate-nuget-vulnerabilities.cs -- /tmp/nuget-vulnerabilities.json` passed locally.
- [x] Container digest evidence generation remains covered by C# script; `dotnet run .github/scripts/write-container-digest-evidence.cs` passed locally with representative environment input.
- [x] Immutable deployment tag promotion evidence generation remains covered by C# script; `dotnet run .github/scripts/write-image-promotion-evidence.cs` passed locally with representative environment input.
- [x] Edited workflow YAML parses locally with PyYAML.
- [x] CLA workflow threat model is documented before enabling `pull_request_target`.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] Affected per-project tests pass.
- [x] `dotnet restore --locked-mode` passes after package/lock-file regeneration.
- [x] `dotnet list Explore.sln package --vulnerable --include-transitive --format json --output-version 1 --no-restore` reports `vulnerable-packages=0` after `MailKit` remediation.
- [ ] OpenAPI guard runs twice with zero second-run diff when contract paths are touched. Latest local guard run reached the OpenAPI invariant phase and failed before determinism because `OpenApiDocument_PublicHalDetailResourceSchemasAreNotEmpty` found empty public HAL detail schemas for AI/storage wrappers (`HalResourceOfAiAssistantBootstrapDto`, `HalResourceOfAiConversationDto`, `HalResourceOfAiRunDto`, `HalResourceOfInstanceStorageSettingsDto`, `HalResourceOfStorageObjectDto`, `HalResourceOfTenantStorageSettingsDto`) in the current dirty worktree.
- [x] Container build emits digest, SBOM/provenance, immutable promotion, scan output, base image digest pins, and attestation verification evidence. Current implementation emits digest JSON, primary-registry promotion evidence, OCI inspect/index evidence, Trivy text/SARIF artifacts, GHCR attestations, and `gh attestation verify` JSON; Dockerfiles use tag-plus-digest .NET base image references.
- [ ] Deploy verifies attestation/digest before invoking Coolify. Attestation and immutable tag digest verification are enforced through the required `build-and-push` dependency; deploy jobs now resolve expected image digests from retained promotion evidence before calling Coolify. Exact digest consumption by Coolify remains open.
- [ ] Staging deploy smoke checks pass before production changes are considered.

## Remaining / Deferred Work

- [x] Tavily MCP research refresh when Tavily is available. 2026-06-01 refresh returned useful GitHub environment/attestation documentation snippets and noisy Coolify/ATCR results; Coolify digest support was verified primarily from source evidence.
- [x] Context7 documentation refresh when quota is available. 2026-06-01 refresh for GitHub Actions confirmed `merge_group` required-check usage, environment required reviewers, and `gh attestation verify oci://... -R <repo>` container attestation verification guidance.
- [x] Legal contribution posture selected: CLA only with broad inbound rights for ISLAMU nonprofit alternative licensing.
- [x] Maintainer decision on archived CLA Assistant action risk: avoided in favor of repository-owned C# validator.
- [x] Coolify digest support verification. Public Coolify v4.x source/UI evidence shows Docker Image apps support SHA-256 hash input and normalize digest references to `image@sha256:<digest>`; live ISLAMU Coolify consumption proof remains a separate Phase 7 blocker.
- [x] ATCR OIDC support verification. Public ATCR docs do not document GitHub Actions OIDC federation for CI image pushes; keep scoped `ATCR_PASSWORD` with 90-day/event-driven rotation until ATCR publishes a non-interactive short-lived credential path.
- [x] Repository settings verification through GitHub UI/API. 2026-06-01 API evidence captured the current gaps; remediation remains Phase 8.
- [x] NuGet vulnerability remediation and explicit blocking audit policy.
- [x] Coverage provider decision. Current coverage publication is artifact-only through `Coverage Evidence`; hosted coverage providers and badges stay blocked until a verified workflow and owner are documented.
- [ ] Promotion of Spectral, E2E, Scorecard, history-wide secret scanning, and release automation gates from advisory to blocking after reliability is proven. `oasdiff` is now partially blocking for missing changelog evidence on PR/push/merge-queue runs.

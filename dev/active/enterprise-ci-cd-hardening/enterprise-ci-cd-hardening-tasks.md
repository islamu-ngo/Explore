ABOUTME: Execution checklist for the re-baselined enterprise CI/CD hardening program.
ABOUTME: Tracks required phases, acceptance criteria, verification, and unresolved operational risks.

# Enterprise CI/CD Hardening - Task Checklist

Last Updated: 2026-05-30 Europe/Brussels

## Status Summary

- [x] Re-baseline old plan/context/tasks with senior CTO feedback.
- [x] Verify current workflow inventory.
- [x] Record research/tooling limitations for Tavily and Context7.
- [x] Run repository build baseline; latest rerun is green with warnings. Architecture tests are red because unrelated untracked AI integration code defines `AiChatRequest` outside the CQRS query namespace convention.
- [x] Add deep CTO feedback for CLA/DCO, `schemas/openapi.json`, workflow security, digest promotion, release evidence, and repository settings.
- [ ] Implement Phase 0 contract and ownership baseline. First slice complete; legal decision and external settings evidence remain.
- [ ] Implement Phase 1 current defect fixes. `Build & Test` no-op wrapper and NuGet vulnerability remediation are complete; broader required-check hardening remains.
- [ ] Implement Phase 2 CLA/DCO legal contribution gate.
- [x] Implement workflow lint/security gate. C# helper scripts, SHA-pin policy, Dependabot maintenance policy validation, blocking `actionlint`, blocking `zizmor`, and retained workflow security evidence are implemented locally; repository-side required-check configuration still needs verification.
- [ ] Implement container supply-chain evidence hardening. Digest JSON, SBOM/provenance registry evidence, immutable primary-registry promotion evidence, Trivy text/SARIF artifacts, checksum manifests, pre-deploy GHCR attestation verification, and Docker base image digest policy are implemented; exact Coolify digest consumption remains.
- [ ] Implement digest promotion and deploy consolidation.
- [ ] Verify GitHub repository settings.

## Implementation Maintenance Rules

- [x] Update this tasks file after each completed implementation slice.
- [x] Update `enterprise-ci-cd-hardening-context.md` after every major decision, blocker, or verification run.
- [x] Update `enterprise-ci-cd-hardening-plan.md` when architecture or sequencing changes.
- [ ] Keep workflow changes small enough to review.
- [ ] Do not touch unrelated dirty worktree files.
- [ ] Do not mark the workstream complete until CLA/DCO policy, digest deployment, workflow security linting, and repository settings evidence are done.

## Phase 0 - Contract And Repository Settings Baseline

- [x] Add `ci-cd-change` intent to `.claude/contract/intents.yaml`.
- [x] Include expected `must_read_docs`: `docs/CI_CD_GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `docs/RELEASE_CHECKLIST.md`, `docs/QUICK_REFERENCE.md`.
- [x] Include expected paths: `.github/**`, `Dockerfile` files, `Directory.Packages.props`, `global.json`, CI/CD docs, legal contribution docs.
- [x] Include minimum tests: architecture docs tests. Full Release build remains the session baseline and currently passes with warnings.
- [x] Add `.github/CODEOWNERS`.
- [x] Protect `.github/**`, Dockerfiles, dependency manifests, release docs, legal contribution docs, and CI/CD governance docs with owner review.
- [x] Add repository settings evidence checklist to `docs/CI_CD_GOVERNANCE.md` or a dedicated tracked note.
- [ ] Confirm required check names before renaming workflows.
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
- [ ] Replace any other path-skipped workflow designs before marking those workflows globally required.
- [x] Correct stale `swagger.json` wording in `docs/CONTRIBUTING.md`.
- [x] Reconcile docs and workflow artifact names for the `_build-test.yml` / `openapi-contract.yml` split.
- [x] Decide NuGet vulnerability policy:
  - [x] Remediate current `MailKit` advisory warnings and keep audit blocking.
  - [x] Document that temporary advisory exceptions require owner, date, advisory URL, affected package/version, compensating control, and removal condition before weakening the workflow.

### Phase 1 Acceptance

- [x] No dead/stale OpenAPI guard remains.
- [x] Docs consistently use `schemas/openapi.json` as the canonical root schema artifact for this workflow cleanup slice.
- [ ] Required checks cannot be skipped into permanent pending state. `Build & Test` is fixed; other candidate required workflows still need review before being marked globally required.
- [x] NuGet audit behavior is intentional and documented.

## Phase 2 - Contributor Legal Gate (CLA/DCO)

- [ ] Read and summarize `https://contributoragreements.org/`.
- [ ] Read and summarize `https://contributoragreements.org/legal.html`.
- [ ] Read and summarize `https://contributoragreements.org/agreement-chooser.html`.
- [ ] Read and summarize `https://cla-assistant.io/`.
- [ ] Read and summarize `https://github.com/contributor-assistant/github-action`.
- [ ] Read GitHub `pull_request_target` security documentation before writing `.github/workflows/cla.yml`.
- [ ] Decide contribution legal posture with owner/legal reviewer:
  - [ ] CLA only;
  - [ ] DCO only;
  - [ ] CLA plus DCO;
  - [ ] inbound=outbound with no separate agreement.
- [ ] Draft or approve actual `docs/legal/CLA.md` or `docs/legal/DCO.md`.
- [ ] Do not use SAP's sample CLA as the production agreement.
- [ ] Decide signature storage:
  - [ ] remote private signatures repository; or
  - [ ] dedicated unprotected signatures branch; or
  - [ ] same repository only with explicit risk acceptance.
- [ ] If using `contributor-assistant/github-action`, document that the upstream repository is archived and decide:
  - [ ] accept archived action risk;
  - [ ] fork/vendor it;
  - [ ] choose a maintained alternative.
- [ ] Add `.github/workflows/cla.yml` only after threat model approval.
- [ ] Pin `contributor-assistant/github-action` to a full commit SHA, not `@v2.6.1`.
- [ ] Scope permissions to the smallest viable set.
- [ ] Ensure the CLA workflow does not checkout, build, test, cache, or execute untrusted PR head code.
- [ ] Avoid broad `bot*` allowlists; explicitly allow only known trusted bots.
- [ ] Document token choice: `GITHUB_TOKEN`, fine-grained PAT, GitHub App token, or remote repo credential.
- [ ] Update `.github/PULL_REQUEST_TEMPLATE.md` with CLA/DCO language.
- [ ] Update `docs/CONTRIBUTING.md` with signing instructions.
- [ ] Add branch protection requirement for the CLA status check after stability is proven.
- [ ] Add privacy/retention note for signature metadata.

### Phase 2 Acceptance

- [ ] Contributor legal status is visible as a PR check.
- [ ] CLA/DCO docs and PR template agree.
- [ ] The workflow cannot run untrusted PR code with write credentials.
- [ ] Signature storage is auditable and not mixed into protected source branch changes.

## Phase 3 - Workflow Quality And Supply-Chain Guard

- [x] Add `.github/workflows/workflow-security.yml` or equivalent.
- [x] Run `actionlint` against `.github/workflows`.
- [x] Run `zizmor` against `.github/workflows` as retained blocking evidence for medium-or-higher findings.
- [x] Upload SARIF or retained artifacts for workflow security findings.
- [x] Add a check that rejects external `uses:` references not pinned to full-length SHAs.
- [x] Preserve path-based local reusable workflow calls.
- [x] Add/update Dependabot rules so action SHA updates remain maintainable.
- [x] Keep repository-owned CI helper scripts as file-based C# scripts run with `dotnet run <script>.cs -- <args>` and script-local `#:property RestorePackagesWithLockFile=false` directives.
- [ ] Add OpenSSF Scorecard scheduled/SARIF evidence if repository permissions support it.
- [ ] Add `gitleaks` or equivalent local secret-scanning feedback lane if low-noise.
- [ ] Add `pinact` or a custom policy check if it improves SHA-pin enforcement.

### Phase 3 Acceptance

- [x] Workflow YAML changes are linted by blocking `actionlint`.
- [x] High-confidence workflow security issues block or are explicitly baselined. Local `zizmor` `1.25.2` verification now reports no medium-or-higher findings, and `Workflow Security` fails when the SARIF or text scan exits nonzero.
- [ ] Unpinned external actions cannot merge. The local `Workflow Security` gate exists, has a stable workflow/job display name, and passes; repository-side required-check configuration still needs verification before this acceptance item is complete.

## Phase 4 - Build/Test Evidence, Coverage, License, And Dependency Integrity

- [ ] Keep TRX upload and job summaries for all fast and integration test lanes.
- [ ] Add coverage collection only after lane stability is confirmed.
- [ ] Decide coverage publication: artifact-only, Codecov, SonarCloud, or another provider.
- [ ] Add analyzer/warnings report artifact or warnings budget.
- [ ] Split dependency gates by severity/direct/transitive package type.
- [x] Keep NuGet vulnerability report parsing in `.github/scripts/validate-nuget-vulnerabilities.cs` instead of embedded workflow script blocks.
- [ ] Add dependency license policy scanning and document AGPL-compatible allow/deny rules.
- [ ] Add cache-poisoning controls for fork PRs and trusted deploy/publish workflows.
- [ ] Run integration tests for deploy callers and reliable scheduled lanes.
- [ ] Document how maintainers triage each artifact type.

### Phase 4 Acceptance

- [ ] CI failures are triageable from GitHub Actions without immediate local rerun.
- [ ] Dependency vulnerability and license risk policy is explicit.
- [ ] Coverage/badge policy reflects implemented tooling only.

## Phase 5 - OpenAPI Contract Guard V2

- [ ] Keep deterministic regeneration for:
  - [ ] `schemas/openapi.json`
  - [ ] `docs/API_CONTRACT_INVENTORY.md`
  - [ ] `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- [ ] Add advisory `oasdiff` report against the base branch.
- [ ] Add Spectral only after lint rules are documented and low-noise.
- [ ] Resolve or delete skipped/stale API contract tests that block stronger enforcement.
- [ ] Promote breaking-change detection to blocking when policy is ready.
- [ ] Require `docs/API_CHANGELOG.md` evidence for intentional breaking API changes.

### Phase 5 Acceptance

- [ ] Stale generated artifacts block.
- [ ] Breaking-change evidence is visible in PRs.
- [ ] OpenAPI/client regeneration remains deterministic on second run.

## Phase 6 - Container Build, SBOM, Provenance, And Attestation Verification

- [ ] Update Docker actions through Dependabot or a controlled PR, then pin SHAs.
- [x] Export SBOM/provenance registry evidence as downloadable artifacts via OCI inspect/index output.
- [x] Add Trivy SARIF output as retained container evidence; code-scanning upload remains repository-permissions dependent.
- [x] Pin Docker base images by digest and enforce the weekly Dependabot Docker update cadence through `Workflow Security`.
- [x] Verify GitHub artifact attestations before deployment through `_container-build.yml` `gh attestation verify` against the pushed GHCR digest, repository, signer workflow, source ref/digest, SLSA provenance predicate, and GitHub-hosted runner trust boundary.
- [ ] Add SLSA provenance verification or document why GitHub artifact attestation is the chosen SLSA-compatible evidence path.
- [x] Add release artifact integrity manifests with checksums for retained container evidence.
- [x] Record image digest, immutable promotion tags, scan result artifact paths, SBOM/provenance references, and attestation verification result in job summaries. Digest, tags, immutable promotion proof, SBOM/provenance artifact references, Trivy artifact paths, attestation verification artifact path, and checksum references are recorded in the reusable build summary.
- [x] Keep container digest evidence JSON generation in `.github/scripts/write-container-digest-evidence.cs` instead of embedded workflow script blocks.
- [ ] Decide ATCR credential strategy:
  - [ ] OIDC/short-lived credential if supported; or
  - [ ] scoped environment secret with documented rotation if OIDC is unavailable.

### Phase 6 Acceptance

- [x] Deploy cannot start without scan and attestation verification in the current workflow dependency graph because deploy jobs require `build-and-push`, which now blocks on Trivy and `gh attestation verify`.
- [x] Release evidence includes image digest and supply-chain artifacts. Container build artifacts now include digest JSON, immutable promotion evidence, OCI inspect/index evidence, Trivy text/SARIF, attestation verification JSON, and checksums.
- [x] Mutable tags are convenience aliases only. The reusable build records `sha-*` / `dev-*` primary-registry promotion tags and verifies they resolve to the built digest; mutable `latest` / `develop` tags remain non-authoritative aliases.

## Phase 7 - Unified Digest-Based Deploy Promotion

- [ ] Create one reusable deploy workflow/path for staging and production.
- [ ] Remove duplicated deploy shell logic from `deploy-coolify.yml` and `deploy-coolify-develop.yml`.
- [ ] Pass environment, component, digest, smoke URLs, and webhook secret names as explicit inputs.
- [ ] Confirm whether Coolify can deploy `image@sha256:<digest>`.
- [ ] If Coolify supports digests, configure deployment by digest.
- [x] If Coolify does not support digests, configure immutable commit-SHA tag deployment and record resolved digest before deploy. `_container-build.yml` now records primary-registry `sha-*` / `dev-*` promotion tags and verifies each tag resolves to the built digest before dependent deploy jobs can start; post-deploy Coolify consumption proof remains separate.
- [ ] Make production smoke checks mandatory when production URL variables exist.
- [ ] Keep production protected by GitHub Environment approval and branch restrictions.
- [ ] Add deployment freeze/manual override policy with audit notes for urgent security releases.
- [ ] Ensure deployment summaries include environment, component, commit SHA, digest, workflow run, smoke result, and rollback note. Container build summaries now include digest and immutable promotion evidence; Coolify deploy summaries still need digest/tag promotion evidence.

### Phase 7 Acceptance

- [ ] One deploy implementation serves staging and production.
- [ ] Production deployment evidence proves exactly what digest was deployed. Build-side immutable tag promotion proof exists; Coolify-side consumption proof remains open.
- [ ] Deployment failures retain redacted logs and smoke evidence.

## Phase 8 - Repository And Organization Policy Enforcement

- [ ] Verify branch/ruleset protection for `main`.
- [ ] Verify branch/ruleset protection for `develop`.
- [ ] Verify required checks match workflow/job names.
- [ ] Verify merge queue settings if used.
- [ ] Verify `staging` environment exists.
- [ ] Verify `production` environment exists.
- [ ] Verify production required reviewers and branch/tag restrictions.
- [ ] Verify environment secrets/variables are scoped correctly.
- [ ] Verify action policy for allowed actions and SHA pinning where available.
- [ ] Verify secret scanning.
- [ ] Verify push protection.
- [ ] Verify dependency graph.
- [ ] Verify Dependabot security updates.
- [ ] Verify CodeQL/code scanning alerts.
- [ ] Add scheduled repository-settings drift check if GitHub API permissions allow it.
- [ ] Record redacted evidence in the context file or release checklist.

### Phase 8 Acceptance

- [ ] YAML and GitHub settings agree.
- [ ] Repository settings evidence is current enough for a release decision.

## Phase 9 - Runtime, E2E, Performance, And Release Evidence Maturity

- [ ] Keep E2E manual/nightly until reliability supports promotion.
- [ ] Add trend summaries for E2E/security/runtime failures.
- [ ] Add scheduled OpenAPI breaking-change report if not already blocking.
- [ ] Add scheduled performance/benchmark smoke lanes for high-risk endpoints/pages.
- [ ] Add flaky-test tracking with owner, first-seen date, and promotion/removal criteria.
- [ ] Ensure security/Cerbos scheduled failures are triaged with owner/date.
- [ ] Ensure release notes copy or link long-lived evidence outside expiring GitHub artifacts when required.

### Phase 9 Acceptance

- [ ] Runtime lanes provide actionable evidence without slowing every PR.
- [ ] Release evidence survives beyond GitHub artifact expiration where needed.

## Phase 10 - Release Automation, Compliance, And Maintainer Experience

- [ ] Decide release model: manual tags, GitHub Releases, Release Drafter, semantic versioning, or conventional commits.
- [ ] Add `.github/workflows/release.yml` if release automation is selected.
- [ ] Generate release evidence bundle with commit SHA, image digests, SBOM/provenance, attestations, scans, OpenAPI diff, test summary, CLA status, and deployment smoke result.
- [ ] Attach long-lived evidence or links to GitHub Releases.
- [ ] Add changelog/release-note checks for security, migrations, config, OpenAPI, and operator-impact changes.
- [ ] Add maintainer runbooks for re-running failed gates without bypassing controls.

### Phase 10 Acceptance

- [ ] Release can be audited from durable evidence without relying on expired workflow artifacts.
- [ ] Maintainers have documented override and rerun paths with approval requirements.

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
- [ ] CLA workflow threat model is documented before enabling `pull_request_target`.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] Affected per-project tests pass.
- [x] `dotnet restore --locked-mode` passes after package/lock-file regeneration.
- [x] `dotnet list Explore.sln package --vulnerable --include-transitive --format json --output-version 1 --no-restore` reports `vulnerable-packages=0` after `MailKit` remediation.
- [ ] OpenAPI guard runs twice with zero second-run diff when contract paths are touched.
- [x] Container build emits digest, SBOM/provenance, immutable promotion, scan output, base image digest pins, and attestation verification evidence. Current implementation emits digest JSON, primary-registry promotion evidence, OCI inspect/index evidence, Trivy text/SARIF artifacts, GHCR attestations, and `gh attestation verify` JSON; Dockerfiles use tag-plus-digest .NET base image references.
- [ ] Deploy verifies attestation/digest before invoking Coolify. Attestation verification is enforced through the required `build-and-push` dependency; exact digest consumption by Coolify remains open.
- [ ] Staging deploy smoke checks pass before production changes are considered.

## Remaining / Deferred Work

- [ ] Tavily MCP research refresh when Tavily is available.
- [ ] Context7 documentation refresh when quota is available.
- [ ] Legal review of CLA/DCO posture.
- [ ] Maintainer decision on archived CLA Assistant action risk.
- [ ] Coolify digest support verification.
- [ ] ATCR OIDC support verification.
- [ ] Repository settings verification through GitHub UI/API.
- [x] NuGet vulnerability remediation and explicit blocking audit policy.
- [ ] Coverage provider decision.
- [ ] Promotion of `oasdiff`, Spectral, E2E, Scorecard, and release automation gates from advisory to blocking after reliability is proven.

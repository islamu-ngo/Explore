ABOUTME: Governance reference for GitHub Actions checks, branch protection, environments, and CI/CD evidence.
ABOUTME: Separates repository settings from workflow YAML so required gates stay auditable and maintainable.

# CI/CD Governance

> **Audience:** Maintainers | Release operators | Contributors | AI agents
> **Status:** Implemented + repository-settings required
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-07
> **Source Anchors:** `.github/workflows/`, `.github/dependabot.yml`, `docs/TESTING.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/RELEASE_CHECKLIST.md`

This page is the source of truth for GitHub Actions governance. Workflow YAML defines what runs; GitHub repository settings define which checks are required, which environments require approval, and which organization security features are enabled.

## Required Repository Settings

These controls cannot be fully enforced from workflow YAML and must be configured in GitHub repository or organization settings.

### Branch Protection / Rulesets

Protect `main` and `develop` with a ruleset or branch-protection rule that requires pull requests and current status checks before merge.

Recommended checks after the PR1-PR8 governance baseline stabilizes:

| Check | Workflow | Job name | Branch-protection status | Notes |
|---|---|---|---:|---|
| Fast build/test | `Build & Test` (`.github/workflows/test.yml`) | `run-tests` / reusable `build-and-test` | Ready after repository-side check-name verification | Always-present wrapper detects build/test-relevant paths and intentionally no-ops for dedicated docs/schema/ops-only changes. Keep name stable unless branch protection is migrated. |
| Workflow security | `Workflow Security` (`.github/workflows/workflow-security.yml`) | `Workflow Security` | Ready after repository-side check-name verification | Always-present wrapper validates action SHA pins, Dependabot action-update policy, actionlint, and medium-or-higher zizmor findings for workflow-governance changes. |
| OpenAPI drift | `OpenAPI Contract Guard` | `OpenAPI Contract Guard` | Required | Always-present workflow with internal no-op detection for unrelated changes. |
| Code scanning | `CodeQL Advanced` | `Analyze (csharp)`, `Analyze (javascript-typescript)`, `Analyze (actions)` | Ready after repository-side check-name verification | Always-present detector keeps matrix checks present, runs analysis for CodeQL-relevant changes and schedule/manual dispatch, and intentionally no-ops for ignored docs/ops-only paths. C# uses manual Release build. Keep schedule enabled. |
| Dependency review | `Dependency Review` | `Dependency Review` | Yes for PRs | Reviews dependency changes; read-only token permissions. |
| Security integration | `Security Integration Tests` | `Security Integration Tests` | Ready after repository-side check-name verification | Always-present wrapper detects security-relevant paths, runs on schedule/manual dispatch, supports merge queue, and intentionally no-ops for unrelated changes. |
| Cerbos policy | `Cerbos Policy Validation` | `Cerbos Policy Validation` | Ready after repository-side check-name verification | Always-present wrapper detects authorization-policy/code paths, runs on schedule/manual dispatch, supports merge queue, and intentionally no-ops for unrelated changes. |
| Agent context | `agent-context` | `Validate AI-Context Contract` | Ready after repository-side check-name verification | Always-present wrapper detects AI context/docs/rule paths, supports merge queue/manual dispatch, and intentionally no-ops for unrelated changes. |

Do **not** require workflows that are skipped by `paths` filters unless the workflow contains an always-running no-op job. GitHub can leave skipped required checks pending, which blocks merges without useful feedback. `Build & Test`, `CodeQL Advanced`, `Security Integration Tests`, `Cerbos Policy Validation`, and `agent-context` now use workflow-internal change detection instead of trigger-level path skips, so their checks remain present while avoiding unnecessary work for unrelated paths.

If merge queue is enabled, ensure required workflows that gate merges include `merge_group`. `Build & Test`, `OpenAPI Contract Guard`, `CodeQL Advanced`, `Security Integration Tests`, `Cerbos Policy Validation`, and `agent-context` include merge-queue triggers; add the same event to any newly required workflow before marking it merge-queue-required.

### Repository Settings Evidence Checklist

Record evidence for these settings before treating the repository as enterprise-ready. Store screenshots, GitHub API output, or maintainer attestations with the release or security evidence package; workflow YAML alone is not sufficient.

| Control | Expected setting | Evidence required | Current evidence |
|---|---|---|---|
| Default branch protection / ruleset | `main` requires pull requests, current required checks, linear history or reviewed merge policy, and stale review dismissal when available. | Ruleset export, branch protection API output, or maintainer screenshot. | Not yet verified. |
| Development branch protection / ruleset | `develop` requires pull requests and current required checks before merge. | Ruleset export, branch protection API output, or maintainer screenshot. | Not yet verified. |
| Required check names | Check names match the table above and are stable before branch protection is updated. | Branch protection required-check export. | Not yet verified. |
| Merge queue | Enabled only after all required workflows have `merge_group` or always-present wrappers. | Ruleset export showing queue status and required checks. | Not yet verified. |
| Environments | `staging` and `production` exist with environment-scoped secrets. Production requires reviewers and branch/tag restrictions. | Environment settings screenshot/API output with secret names redacted. | Not yet verified. |
| Security features | Secret scanning, push protection, Dependabot security updates, dependency graph, and CodeQL alerts are enabled. | Security settings screenshot/API output. | Not yet verified. |
| CODEOWNERS owner resolution | Every team/user referenced by `.github/CODEOWNERS` exists and has write access. | GitHub CODEOWNERS validation or maintainer confirmation. | Not yet verified. |

### Deployment Environments

Create GitHub Environments named exactly:

- `staging`
- `production`

Configure environment secrets and variables as described in [CONFIGURATION.md](CONFIGURATION.md#deployment-cicd-secrets). Production must require reviewer approval and should restrict deployments to `main` and version tags. Staging can deploy automatically from `develop` unless the release process requires review.

Workflow YAML references the environments, but reviewers, branch restrictions, wait timers, and environment-scoped secrets are GitHub settings.

### Security Features

Confirm these GitHub security features at repository or organization level:

- Secret scanning enabled.
- Push protection enabled where available.
- Dependabot security updates enabled.
- Dependency graph enabled.
- Code scanning alerts enabled for CodeQL results.

### Contributor Legal Governance

The repository uses a CLA-only contribution posture. Every non-bot contributor must sign the [ISLAMU Event Contributor License Agreement](legal/CLA.md), which grants ISLAMU nonprofit broad rights to provide, sell, sublicense, and relicense ISLAMU Event under alternative terms when social-impact or operational needs require it.

The decision record in [CONTRIBUTION_GOVERNANCE.md](legal/CONTRIBUTION_GOVERNANCE.md) captures the legal posture, inbound copyright/patent scope, signature storage model, bot allowlist, archived CLA Assistant risk decision, and `pull_request_target` threat model.

`.github/workflows/cla.yml` is metadata-only. It uses `pull_request_target`, checks out the trusted base commit only, runs repository-owned `.github/scripts/validate-cla-pr.cs`, and never checks out or executes pull-request head code. It uses read-only `GITHUB_TOKEN` scopes and stores signature evidence in the pull request body plus GitHub PR audit trail.

### GitHub Actions Supply-Chain Pins

External `uses:` references in `.github/workflows/*.yml` are pinned to full-length commit SHAs with a same-line version comment, for example `owner/action@<sha> # vX.Y.Z`. This makes the executable action reference immutable while preserving human-readable upgrade intent.

Local reusable workflows remain path-based (`./.github/workflows/...`) because they are controlled by this repository's review history. Dependabot's `github-actions` ecosystem in `.github/dependabot.yml` keeps external SHA pins maintainable through a weekly grouped update lane with conventional `ci` commit messages.

`Workflow Security` enforces this policy with `.github/scripts/validate-action-pins.cs` and `.github/scripts/validate-dependabot-policy.cs`, both run as file-based C# scripts with `dotnet run <script>.cs -- <args>`. The check always reports a status, scans workflow-security inputs when `.github/workflows/**`, `.github/actions/**`, `.github/scripts/**`, `.github/dependabot.yml`, deployable Dockerfiles, or this governance document changes, and intentionally no-ops for unrelated changes. Do not add external actions without a full SHA and a same-line version comment, and do not remove the `github-actions` Dependabot update lane without replacing it with an equivalent pinned-action maintenance process.

Deployable Dockerfiles must use tag-plus-digest base image references, for example `mcr.microsoft.com/dotnet/aspnet:10.0@sha256:<digest>`. The human-readable tag preserves maintainer intent while the digest fixes the resolved image. `Workflow Security` enforces this with `.github/scripts/validate-dockerfile-base-images.cs` for `Explore.API/Dockerfile` and `Explore.Blazor/Dockerfile`. Dependabot's `docker` ecosystem entries update those digests weekly through grouped `docker-base-images` PRs.

Repository-owned helper scripts under `.github/scripts/` must be file-based C# scripts (`*.cs`) unless a future change documents why C# is not viable. Keep shell blocks in workflows for orchestration only; policy, JSON parsing, and evidence-generation logic belongs in C# so it uses the repository's pinned .NET SDK and remains reviewable by the same maintainers as the application code. Each helper script declares `#:property RestorePackagesWithLockFile=false` so ad hoc `dotnet run <script>.cs -- <args>` execution does not create transient `.github/scripts/packages.lock.json` files. Third-party tools can still use their required runtime, such as `zizmor` running from an isolated Python virtual environment.

### Workflow Static Analysis Policy

`Workflow Security` treats workflow definitions as security-sensitive code. For workflow-governance changes it:

- sets up the pinned .NET SDK from `global.json`, then runs the local C# validators for action pins and Dependabot `github-actions` update coverage;
- installs `actionlint` `1.7.12` from the upstream release archive after checking the expected SHA-256 digest, then blocks on workflow syntax, expression, and shell-in-workflow lint findings;
- installs `zizmor` `1.25.2` in an isolated Python virtual environment, runs it offline, exports SARIF/text evidence, and blocks on medium-or-higher severity findings;
- uploads `workflow-security-evidence` for 30 days.

If future `zizmor` findings must be temporarily accepted, document each exception with owner, date, rule ID, affected workflow, compensating control, and removal condition before weakening the workflow.

### Local Secret-Scanning Feedback

`Secret Scanning` runs `gitleaks` `8.30.1` from the upstream release archive after verifying the Linux x64 archive SHA-256. Pull request, push, and merge-queue runs scan only the relevant commit range and fail when newly introduced secrets are detected. Scheduled and manual runs scan repository history as advisory evidence because the existing history currently contains legacy findings that need triage before this lane can become globally blocking.

The workflow redacts findings, retains SARIF/text output in `secret-scanning-evidence`, and does not replace GitHub secret scanning or push protection. Repository or organization secret scanning and push protection remain required settings in the evidence checklist above.

### NuGet Locked Restore Policy

GitHub Actions restore steps and deployable Docker build stages use `dotnet restore --locked-mode`. All tracked project files have committed `packages.lock.json` files, and `Directory.Build.props` enables `RestorePackagesWithLockFile` plus CI-only `RestoreLockedMode` for `GITHUB_ACTIONS`.

Package input changes must commit the matching lock-file changes in the same PR. Regenerate lock files with normal restore or `dotnet restore --force-evaluate`; never hand-edit `packages.lock.json`.

Dockerfiles must copy the root restore inputs (`global.json`, `Directory.Build.props`, `Directory.Packages.props`), project files, and relevant `packages.lock.json` files before running `dotnet restore --locked-mode` inside the build stage. This preserves Docker layer caching while keeping NuGet resolution deterministic.

### NuGet Vulnerability Audit Policy

Fast CI runs `dotnet list Explore.sln package --vulnerable --include-transitive --format json --output-version 1 --no-restore` after locked restore, then parses the report with `.github/scripts/validate-nuget-vulnerabilities.cs`. Any vulnerable direct or transitive package reported by NuGet fails the `Build & Test` lane; temporary advisory exceptions require an owner, date, advisory URL, affected package/version, compensating control, and removal condition recorded in this document before the workflow may be weakened.

The current policy is remediation-first. `MailKit` was upgraded from `4.15.1` to `4.16.0` to clear GitHub Advisory `GHSA-9j88-vvj5-vhgr` / `CVE-2026-41319` rather than making the audit advisory.

## Required vs Advisory Gates

| Gate | Required | Advisory / scheduled | Promotion rule |
|---|---:|---:|---|
| Release build + fast tests | Yes | No | Required for all code PRs. |
| Infrastructure unit tests | Yes | No | Included in fast CI. |
| PostgreSQL-backed integration tests | Conditional | Deploy callers | Required for integration/deploy callers; add a schedule only after reliability and runtime cost are acceptable. |
| OpenAPI generated-artifact drift | Yes | No | Required after PR2 baseline. |
| `oasdiff` breaking-change report | No | Future | Keep advisory until versioning and contract tests stabilize. |
| Spectral/OpenAPI lint | No | Future | Add only after rules are agreed and low-noise. |
| Security/Cerbos path workflows | Conditional | Yes | Always-present wrappers intentionally no-op for unrelated changes; nightly schedule covers drift outside path-relevant PRs. |
| OpenSSF Scorecard | No | Yes | Scheduled/manual supply-chain posture evidence. Uploads SARIF to code scanning and retains `scorecard-evidence`; keep advisory until repository permissions and signal quality are proven. |
| Local secret scanning | New findings only | Yes | `gitleaks` blocks on PR/push/merge-queue ranges for newly introduced leaks and keeps scheduled/manual history scans advisory until legacy findings are triaged or baselined. |
| E2E browser/runtime tests | No | Yes | Manual/nightly until reliability data justifies required status. |
| Container SBOM/provenance/Trivy/attestation/promotion verification | Deploy-only | No | Required before deployment workflows call Coolify; retained evidence includes registry manifest/index output, immutable primary-registry tag promotion evidence, vulnerability scan artifacts, attestation verification JSON, and checksum manifests. |
| Production smoke checks | Deploy-only | No | Required for production deploys; `PRODUCTION_API_URL` and `PRODUCTION_UI_URL` must be configured and both `/alive` and `/health` must pass for deployed components. Staging smoke checks run when staging URL variables are configured. |

The reusable container build must verify each pushed GHCR digest with `gh attestation verify` before any dependent Coolify deploy job can start. Verification must constrain the expected repository, reusable signer workflow, source ref, source digest, SLSA provenance predicate, and GitHub-hosted runner trust boundary; do not rely on workflow-controlled predicate fields as the sole trust source.

The reusable container build must also record immutable primary-registry promotion evidence before deploy jobs can start. When Coolify digest deployment is not yet proven, the temporary fallback is an immutable `sha-*` production tag or `dev-*` staging tag whose primary-registry reference is inspected and verified to resolve to the built digest. Mutable tags such as `latest` and `develop` remain convenience aliases only and must not be treated as release evidence.

Coolify deploy workflows use the local composite action `.github/actions/deploy-coolify` for webhook triggering, smoke checks, redacted failure summaries, and retained deployment evidence. Before invoking Coolify, deploy jobs download the retained `container-build-*` evidence, resolve the component's full-commit immutable tag and digest with `.github/scripts/resolve-deploy-image-evidence.cs`, and pass that digest into the deploy action. The production workflow publishes and records full-commit `sha-${{ github.sha }}` immutable tags; staging publishes and records full-commit `dev-${{ github.sha }}` immutable tags. Production calls set `require-smoke-check: "true"`, so missing production smoke URLs block the webhook call and both `/alive` and `/health` must return `200` for the deployed component. Staging keeps smoke URLs optional but uses the same `/alive` and `/health` checks when configured. This centralizes deploy behavior while keeping environment-scoped secrets and approvals on the caller jobs. Coolify-side proof that the platform consumed that exact digest or full-commit tag remains required before final release readiness.

Deployment freeze control is an operator-owned GitHub Environment/Repository variable named `DEPLOYMENT_FREEZE`. When it is set to `true`, `.github/actions/deploy-coolify` refuses to call the Coolify webhook unless a manual `workflow_dispatch` run supplies `override_reason`. The override reason is written to the retained deployment summary so urgent security releases are auditable without weakening environment approvals.

## Fork Pull Request Policy

External fork pull requests are untrusted.

- Do not use `pull_request_target` for build, test, generation, container, or deployment workflows unless a separate threat-model review approves the exact pattern.
- Fork PRs must not receive deployment secrets, registry write credentials, environment secrets, OIDC tokens, or privileged `GITHUB_TOKEN` scopes.
- Validation jobs should use `permissions: contents: read` unless a specific GitHub API write is required and safe for the event.
- Deployment workflows must remain push/manual/environment-gated only.

## Generated Artifact Review Rules

Generated files are reviewed product artifacts, not disposable output.

| Artifact | Generated by | Review focus |
|---|---|---|
| `schemas/openapi.json` | `OpenAPI Contract Guard` / `Explore.API` build-time OpenAPI generation | Routes, verbs, response types, auth metadata, endpoint classification, schema shape. |
| `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | NSwag target in `Explore.Blazor.Client.csproj` | Method names, renamed/removed operations, optional API-version parameters, generated client ergonomics. |
| Container digest JSON | `_container-build.yml` via `.github/scripts/write-container-digest-evidence.cs` | Image name, digest, commit SHA, tags, workflow run, scan evidence. |
| Docker base image pins | `Workflow Security` via `.github/scripts/validate-dockerfile-base-images.cs` | Deployable Dockerfiles keep explicit tag-plus-digest base references and Dependabot Docker update coverage. |
| Container OCI inspect/index evidence | `_container-build.yml` via `docker buildx imagetools inspect` | Downloadable registry evidence for the digest that carries Buildx SBOM/provenance attestations. |
| Container immutable promotion evidence | `_container-build.yml` via `.github/scripts/write-image-promotion-evidence.cs` and `docker buildx imagetools inspect` | Primary-registry `sha-*` / `dev-*` tag references and proof that each resolves to the built digest. |
| Container Trivy SARIF | `_container-build.yml` via `aquasecurity/trivy-action` | Critical/high vulnerability evidence in a machine-readable retained artifact. |
| Container attestation verification JSON | `_container-build.yml` via `gh attestation verify` | Verification evidence for the pushed GHCR digest, constrained to the repository, reusable signer workflow, source ref/digest, SLSA provenance predicate, and GitHub-hosted runner trust boundary. |
| Container evidence checksum manifest | `_container-build.yml` via `.github/scripts/write-artifact-checksums.cs` | SHA-256 integrity manifest for retained digest, OCI, Trivy, and related container evidence artifacts. |
| Deployment summaries | `.github/actions/deploy-coolify` via Coolify deploy jobs | Environment, component, commit SHA, expected immutable image tag, expected image digest, promotion evidence path, webhook result, smoke-check result, whether smoke was required, deployment-freeze state, override reason, workflow run, rollback note. |

Never hand-edit OpenAPI or NSwag generated client artifacts. Regenerate them through the workflow-compatible commands in [TROUBLESHOOTING.md](TROUBLESHOOTING.md#openapi--nswag-drift).

## Artifact Retention Policy

| Evidence | Workflow(s) | Retention |
|---|---|---:|
| Fast/integration TRX | `Build & Test (Reusable)` | 14 days |
| OpenAPI drift artifacts | `OpenAPI Contract Guard` | 30 days |
| Workflow security evidence | `Workflow Security` | 30 days |
| OpenSSF Scorecard SARIF | `OpenSSF Scorecard` | 30 days |
| Secret-scanning SARIF/text evidence | `Secret Scanning` | 30 days |
| Security and Cerbos logs | `Security Integration Tests`, `Cerbos Policy Validation` | 30 days |
| E2E TRX, traces, screenshots, videos, Docker diagnostics | `E2E Runtime Tests` | 30 days |
| Container digest, OCI inspect/index output, immutable promotion evidence, Trivy text/SARIF output, attestation verification JSON, checksum manifest, SBOM/provenance evidence | `Container Build (Reusable)` | 90 days; preserve release evidence externally for release lifetime |
| Deployment summaries/logs | Coolify deploy workflows | 90 days minimum |

Release notes must copy or link long-lived evidence that GitHub artifact retention will eventually delete.

## Badge Policy

README badges must represent implemented gates. Do not show Codecov or SonarCloud badges until workflows upload coverage or publish SonarCloud analysis. Re-add those badges only in the same PR that introduces and verifies the corresponding workflow.

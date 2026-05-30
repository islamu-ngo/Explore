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
| Code scanning | `CodeQL Advanced` | `Analyze (csharp)`, `Analyze (javascript-typescript)`, `Analyze (actions)` | Require only after path filters are removed or an always-running no-op wrapper exists | C# uses manual Release build. Keep schedule enabled. |
| Dependency review | `Dependency Review` | `Dependency Review` | Yes for PRs | Reviews dependency changes; read-only token permissions. |
| Security integration | `Security Integration Tests` | `Security Integration Tests` | Required only for relevant paths initially | Also scheduled nightly; do not make path-skipped workflow required unless a lightweight always-run wrapper is added. |
| Cerbos policy | `Cerbos Policy Validation` | `Cerbos Policy Validation` | Required only for policy/authz paths initially | Also scheduled nightly; do not make globally required while path-filtered. |
| Agent context | `agent-context` | `Validate AI-Context Contract` | Required only for agent/docs-context changes | Validates AI governance docs; do not make globally required while path-filtered. |

Do **not** require workflows that are skipped by `paths` filters unless the workflow contains an always-running no-op job. GitHub can leave skipped required checks pending, which blocks merges without useful feedback. `Build & Test` now uses workflow-internal change detection instead of trigger-level path skips, so its required check remains present while avoiding unnecessary restore/build/test work for paths owned by dedicated workflows.

If merge queue is enabled, ensure required workflows that gate merges include `merge_group`. `Build & Test`, `OpenAPI Contract Guard`, and `CodeQL Advanced` include merge-queue triggers; add the same event to any newly required workflow before marking it merge-queue-required.

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

The repository has not yet chosen a contributor legal gate. Do not add an enforcing CLA/DCO workflow until the project owner or legal reviewer records the decision.

The decision record must include:

- whether the project uses CLA only, DCO only, CLA plus DCO, or inbound=outbound without a separate agreement;
- inbound license and patent scope;
- the approved legal document path, such as `docs/legal/CLA.md` or `docs/legal/DCO.md`;
- signature storage location, access model, and privacy retention period;
- bot allowlist policy;
- token model for any automation;
- threat model for any `pull_request_target` workflow.

Until that record exists, legal checks may be planned but must not block contributors.

### GitHub Actions Supply-Chain Pins

External `uses:` references in `.github/workflows/*.yml` are pinned to full-length commit SHAs with a same-line version comment, for example `owner/action@<sha> # vX.Y.Z`. This makes the executable action reference immutable while preserving human-readable upgrade intent.

Local reusable workflows remain path-based (`./.github/workflows/...`) because they are controlled by this repository's review history. Dependabot's `github-actions` ecosystem in `.github/dependabot.yml` keeps external SHA pins maintainable through a weekly grouped update lane with conventional `ci` commit messages.

`Workflow Security` enforces this policy with `.github/scripts/validate-action-pins.cs` and `.github/scripts/validate-dependabot-policy.cs`, both run as file-based C# scripts with `dotnet run <script>.cs -- <args>`. The check always reports a status, scans workflow-security inputs when `.github/workflows/**`, `.github/scripts/**`, `.github/dependabot.yml`, deployable Dockerfiles, or this governance document changes, and intentionally no-ops for unrelated changes. Do not add external actions without a full SHA and a same-line version comment, and do not remove the `github-actions` Dependabot update lane without replacing it with an equivalent pinned-action maintenance process.

Deployable Dockerfiles must use tag-plus-digest base image references, for example `mcr.microsoft.com/dotnet/aspnet:10.0@sha256:<digest>`. The human-readable tag preserves maintainer intent while the digest fixes the resolved image. `Workflow Security` enforces this with `.github/scripts/validate-dockerfile-base-images.cs` for `Explore.API/Dockerfile` and `Explore.Blazor/Dockerfile`. Dependabot's `docker` ecosystem entries update those digests weekly through grouped `docker-base-images` PRs.

Repository-owned helper scripts under `.github/scripts/` must be file-based C# scripts (`*.cs`) unless a future change documents why C# is not viable. Keep shell blocks in workflows for orchestration only; policy, JSON parsing, and evidence-generation logic belongs in C# so it uses the repository's pinned .NET SDK and remains reviewable by the same maintainers as the application code. Each helper script declares `#:property RestorePackagesWithLockFile=false` so ad hoc `dotnet run <script>.cs -- <args>` execution does not create transient `.github/scripts/packages.lock.json` files. Third-party tools can still use their required runtime, such as `zizmor` running from an isolated Python virtual environment.

### Workflow Static Analysis Policy

`Workflow Security` treats workflow definitions as security-sensitive code. For workflow-governance changes it:

- sets up the pinned .NET SDK from `global.json`, then runs the local C# validators for action pins and Dependabot `github-actions` update coverage;
- installs `actionlint` `1.7.12` from the upstream release archive after checking the expected SHA-256 digest, then blocks on workflow syntax, expression, and shell-in-workflow lint findings;
- installs `zizmor` `1.25.2` in an isolated Python virtual environment, runs it offline, exports SARIF/text evidence, and blocks on medium-or-higher severity findings;
- uploads `workflow-security-evidence` for 30 days.

If future `zizmor` findings must be temporarily accepted, document each exception with owner, date, rule ID, affected workflow, compensating control, and removal condition before weakening the workflow.

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
| Security/Cerbos path workflows | Conditional | Yes | Required for matching paths; nightly schedule covers drift outside path filters. |
| E2E browser/runtime tests | No | Yes | Manual/nightly until reliability data justifies required status. |
| Container SBOM/provenance/Trivy/attestation verification | Deploy-only | No | Required before deployment workflows call Coolify; retained evidence includes registry manifest/index output, vulnerability scan artifacts, attestation verification JSON, and checksum manifests. |
| Production smoke checks | Deploy-only | No | Required when environment URL variables are configured. |

The reusable container build must verify each pushed GHCR digest with `gh attestation verify` before any dependent Coolify deploy job can start. Verification must constrain the expected repository, reusable signer workflow, source ref, source digest, SLSA provenance predicate, and GitHub-hosted runner trust boundary; do not rely on workflow-controlled predicate fields as the sole trust source.

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
| Container Trivy SARIF | `_container-build.yml` via `aquasecurity/trivy-action` | Critical/high vulnerability evidence in a machine-readable retained artifact. |
| Container attestation verification JSON | `_container-build.yml` via `gh attestation verify` | Verification evidence for the pushed GHCR digest, constrained to the repository, reusable signer workflow, source ref/digest, SLSA provenance predicate, and GitHub-hosted runner trust boundary. |
| Container evidence checksum manifest | `_container-build.yml` via `.github/scripts/write-artifact-checksums.cs` | SHA-256 integrity manifest for retained digest, OCI, Trivy, and related container evidence artifacts. |
| Deployment summaries | Coolify deploy jobs | Environment, commit SHA, webhook result, smoke-check result, rollback note. |

Never hand-edit OpenAPI or NSwag generated client artifacts. Regenerate them through the workflow-compatible commands in [TROUBLESHOOTING.md](TROUBLESHOOTING.md#openapi--nswag-drift).

## Artifact Retention Policy

| Evidence | Workflow(s) | Retention |
|---|---|---:|
| Fast/integration TRX | `Build & Test (Reusable)` | 14 days |
| OpenAPI drift artifacts | `OpenAPI Contract Guard` | 30 days |
| Workflow security evidence | `Workflow Security` | 30 days |
| Security and Cerbos logs | `Security Integration Tests`, `Cerbos Policy Validation` | 30 days |
| E2E TRX, traces, screenshots, videos, Docker diagnostics | `E2E Runtime Tests` | 30 days |
| Container digest, OCI inspect/index output, Trivy text/SARIF output, attestation verification JSON, checksum manifest, SBOM/provenance evidence | `Container Build (Reusable)` | 90 days; preserve release evidence externally for release lifetime |
| Deployment summaries/logs | Coolify deploy workflows | 90 days minimum |

Release notes must copy or link long-lived evidence that GitHub artifact retention will eventually delete.

## Badge Policy

README badges must represent implemented gates. Do not show Codecov or SonarCloud badges until workflows upload coverage or publish SonarCloud analysis. Re-add those badges only in the same PR that introduces and verifies the corresponding workflow.

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
| Fast build/test | `Build & Test` (`.github/workflows/test.yml`) | `run-tests` / reusable `build-and-test` | Require only after path filters are removed or an always-running no-op wrapper exists | Main fast PR gate for code paths. Keep name stable unless branch protection is migrated. |
| OpenAPI drift | `OpenAPI Contract Guard` | `OpenAPI Contract Guard` | Required | Always-present workflow with internal no-op detection for unrelated changes. |
| Code scanning | `CodeQL Advanced` | `Analyze (csharp)`, `Analyze (javascript-typescript)`, `Analyze (actions)` | Require only after path filters are removed or an always-running no-op wrapper exists | C# uses manual Release build. Keep schedule enabled. |
| Dependency review | `Dependency Review` | `Dependency Review` | Yes for PRs | Reviews dependency changes; read-only token permissions. |
| Security integration | `Security Integration Tests` | `Security Integration Tests` | Required only for relevant paths initially | Also scheduled nightly; do not make path-skipped workflow required unless a lightweight always-run wrapper is added. |
| Cerbos policy | `Cerbos Policy Validation` | `Cerbos Policy Validation` | Required only for policy/authz paths initially | Also scheduled nightly; do not make globally required while path-filtered. |
| Agent context | `agent-context` | `Validate AI-Context Contract` | Required only for agent/docs-context changes | Validates AI governance docs; do not make globally required while path-filtered. |

Do **not** require workflows that are skipped by `paths` filters unless the workflow contains an always-running no-op job. GitHub can leave skipped required checks pending, which blocks merges without useful feedback.

If merge queue is enabled, ensure required workflows that gate merges include `merge_group`. `OpenAPI Contract Guard` and `CodeQL Advanced` already do; add it to other required workflows before marking them merge-queue-required.

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

### GitHub Actions Supply-Chain Pins

External `uses:` references in `.github/workflows/*.yml` are pinned to full-length commit SHAs with a same-line version comment, for example `owner/action@<sha> # vX.Y.Z`. This makes the executable action reference immutable while preserving human-readable upgrade intent.

Local reusable workflows remain path-based (`./.github/workflows/...`) because they are controlled by this repository's review history. Dependabot's `github-actions` ecosystem in `.github/dependabot.yml` keeps external SHA pins maintainable.

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
| Container SBOM/provenance/Trivy | Deploy-only | No | Required before deployment workflows call Coolify. |
| Production smoke checks | Deploy-only | No | Required when environment URL variables are configured. |

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
| `Explore.API/swagger.json` | `OpenAPI Contract Guard` / `Explore.API` build-time OpenAPI generation | Routes, verbs, response types, auth metadata, endpoint classification, schema shape. |
| `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | NSwag target in `Explore.Blazor.Client.csproj` | Method names, renamed/removed operations, optional API-version parameters, generated client ergonomics. |
| Container digest JSON | `_container-build.yml` | Image name, digest, commit SHA, tags, workflow run, scan evidence. |
| Deployment summaries | Coolify deploy jobs | Environment, commit SHA, webhook result, smoke-check result, rollback note. |

Never hand-edit OpenAPI or NSwag generated client artifacts. Regenerate them through the workflow-compatible commands in [TROUBLESHOOTING.md](TROUBLESHOOTING.md#openapi--nswag-drift).

## Artifact Retention Policy

| Evidence | Workflow(s) | Retention |
|---|---|---:|
| Fast/integration TRX | `Build & Test (Reusable)` | 14 days |
| OpenAPI drift artifacts | `OpenAPI Contract Guard` | 30 days |
| Security and Cerbos logs | `Security Integration Tests`, `Cerbos Policy Validation` | 30 days |
| E2E TRX, traces, screenshots, videos, Docker diagnostics | `E2E Runtime Tests` | 30 days |
| Container digest, Trivy output, SBOM/provenance evidence | `Container Build (Reusable)` | 90 days; preserve release evidence externally for release lifetime |
| Deployment summaries/logs | Coolify deploy workflows | 90 days minimum |

Release notes must copy or link long-lived evidence that GitHub artifact retention will eventually delete.

## Badge Policy

README badges must represent implemented gates. Do not show Codecov or SonarCloud badges until workflows upload coverage or publish SonarCloud analysis. Re-add those badges only in the same PR that introduces and verifies the corresponding workflow.

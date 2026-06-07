ABOUTME: Release readiness checklist for code, migrations, configuration, security, and documentation.
ABOUTME: Provides the release documentation contract for self-hostable operators and contributors.

# Release Checklist

> **Audience:** Contributors | Operators | AI agents
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/CONTRIBUTING.md`, `docs/TESTING.md`, `.github/workflows/test.yml`, `.github/workflows/agent-context.yml`

Use this checklist before tagging or publishing a release. A release is not ready until operators can understand what changed, how to upgrade, how to verify, and how to roll back.

## Release Model

The current release model is manual semantic-version tags plus manually authored GitHub Releases. Version scope and release history are tracked in [semantic_versioning/CHANGELOG.md](semantic_versioning/CHANGELOG.md); release readiness evidence is assembled from this checklist and the retained CI/CD artifacts referenced below.

Do not add or require `.github/workflows/release.yml`, Release Drafter, or automatic semantic-release behavior until the release evidence bundle format is stable and the automation can attach or link durable evidence without relying only on expiring GitHub Actions artifacts. Conventional Commits remain the preferred commit-message style, but they do not automatically publish or version releases today.

## Release Evidence Bundle

Before publishing a GitHub Release, download the retained CI/CD artifacts listed in this checklist into a local evidence directory and generate the durable bundle:

```bash
dotnet run .github/scripts/generate-release-evidence-bundle.cs -- artifacts release-evidence
```

Set `RELEASE_VERSION`, `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REPOSITORY`, `GITHUB_RUN_ID`, `GITHUB_RUN_ATTEMPT`, and `CLA_STATUS` when generating the bundle outside GitHub Actions so the manifest records the release metadata. The script writes:

- `release-evidence/release-evidence.json` — machine-readable evidence manifest;
- `release-evidence/release-evidence.md` — full human-readable evidence summary;
- `release-evidence/release-evidence-release-notes.md` — copy/paste GitHub Release evidence section;
- `release-evidence/release-evidence-checksums.sha256` — SHA-256 hashes for every retained evidence file.

Attach the generated bundle files to the GitHub Release or copy them to durable release storage before the source GitHub Actions artifacts expire. Paste the contents of `release-evidence-release-notes.md` into the GitHub Release body so release readers can find the durable evidence even after workflow artifacts expire.

## Pull Request Release Impact Gate

Pull requests that touch security/auth, migration/data/rollback, configuration/secrets/deployment, OpenAPI/client contract, or operator/self-hosting/release-note paths must satisfy the `Release Impact Check` before merge. The check validates the `## Release Impact` section in `.github/PULL_REQUEST_TEMPLATE.md` and requires the matching category checkbox plus non-empty `Details:`.

Use `Not applicable` only when the change has no release-impact category. If the check flags a category, update the PR body and link the relevant documentation or release-note evidence before requesting release approval.

## Release Metadata

- [ ] Version/tag is selected.
- [ ] Commit SHA is recorded.
- [ ] Image tags or deployment artifacts are recorded, including full-commit immutable `sha-*` / `dev-*` promotion tags when container images are published.
- [ ] Image digests, immutable promotion tag evidence, Docker base image digest pins, SBOM/provenance evidence, image scan artifacts, and attestation verification results are recorded when container images are published.
- [ ] Coolify-side consumption evidence is retained for container deployments: application configuration, API output, deployment logs, smoke summary, or deploy summary proves the running resource consumed either `image@sha256:<digest>` or the verified full-commit immutable tag.
- [ ] Deployment environment, approver, expected immutable image tag, expected image digest, webhook result, smoke-check result, whether smoke was required, deployment-freeze state, override reason if any, and rollback note are recorded for staging/production deployments.
- [ ] Supported deployment modes are stated: single-tenant, multi-tenant, optional storage, optional Cerbos.
- [ ] Known incompatible versions are stated.

## Code And Test Gates

- [ ] Release build succeeds:

  ```bash
  dotnet build --configuration Release --verbosity quiet
  ```

- [ ] Required per-project tests pass; do not run solution-level `dotnet test`.
- [ ] Architecture/docs quality tests pass:

  ```bash
  dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
  ```

- [ ] Infrastructure-dependent tests are either passed in the release lane or explicitly marked as deferred with the reason.
- [ ] E2E/manual visual checks are completed when the release changes auth, routing, onboarding, or core browser flows.
- [ ] Required GitHub checks match [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md): fast build/test, OpenAPI drift, CodeQL, dependency review, and any path-relevant security/Cerbos checks. If `CodeQL Advanced` owns uploads, confirm GitHub CodeQL default setup is `not-configured`.
- [ ] Advisory/nightly failures are triaged or explicitly deferred with owner and reason.

## Migration And Data Contract

- [ ] New EF migrations are named, reviewed, and tied to the feature/release.
- [ ] Migration impact is documented: additive, data backfill, destructive, or rollback-sensitive.
- [ ] Data-protection/key storage impact is documented if changed.
- [ ] Seed data or lookup table changes are documented.
- [ ] Rollback strategy is documented in release notes.

## Configuration And Secrets Contract

- [ ] New or changed environment keys are documented in `CONFIGURATION.md` or `SECRETS.md`.
- [ ] Removed keys are listed with replacements.
- [ ] Secret-provider paths and key names are documented.
- [ ] Keycloak realm/client/role changes are documented.
- [ ] Optional profiles (`storage`, `authz`) and dependencies are documented.

## Security And Operations Contract

- [ ] Authentication/authorization changes are documented in `SECURITY.md` or `AUTHORIZATION_PATTERNS.md`.
- [ ] Rate-limit, timeout, forwarded-header, CORS, or proxy changes are documented.
- [ ] Health-check, metrics, logging, or tracing changes are documented in `OPERATIONS.md`.
- [ ] Backup/restore impact is documented in `BACKUP_RESTORE_UPGRADE.md` when data shape changes.
- [ ] Known vulnerabilities or dependency warnings are triaged.
- [ ] Secret scanning, push protection, Dependabot security updates, dependency graph, and CodeQL alerts are enabled or explicitly waived at repository/organization level.

## CI/CD Evidence Contract

- [ ] OpenAPI drift artifacts are clean, or generated `openapi.json` / NSwag client changes are reviewed and committed.
- [ ] Intentional breaking API contract changes include a matching `docs/API_CHANGELOG.md` entry with affected route/schema/client method, old/new behavior, affected clients, migration guidance, release target, and retained OpenAPI / advisory `oasdiff` evidence links when available.
- [ ] Container image digest, immutable promotion tag evidence, Docker base image digest pins, SBOM/provenance, Trivy scan output, attestation verification JSON, checksum manifest, and image tag evidence are recorded when images are published.
- [ ] Deployment evidence includes environment, component, commit SHA, expected immutable image tag, expected image digest, promotion evidence path, webhook result, smoke-check result, whether smoke was required, deployment-freeze state, override reason if any, workflow run link, and rollback note.
- [ ] Production deployment approval and branch restrictions are configured in GitHub Environment settings.
- [ ] Long-lived release evidence is copied from expiring GitHub Actions artifacts into release notes or durable storage when required.
- [ ] Any failed gate rerun or emergency override follows [CI_CD_RUNBOOKS.md](CI_CD_RUNBOOKS.md) and records owner, reason, evidence, compensating control, and removal condition.

Expected artifact names:

- `test-results-fast`
- `test-results-integration`
- `openapi-contract-guard`
- `security-test-evidence`
- `cerbos-policy-evidence`
- `container-build-*`
- `deployment-production-evidence` / `deployment-staging-evidence`
- `e2e-runtime-evidence`

## Documentation Impact

Choose exactly one docs impact outcome for the release:

| Outcome | Required Evidence |
|---|---|
| Updated | Linked docs PR/commit updates affected docs. |
| Not needed | Explanation of why runtime/API/operator behavior did not change. |
| Deferred | Follow-up path, owner, and reason the release can proceed. |

Release-blocking docs usually include operator, security, API contract, onboarding, configuration, and migration changes.

## Release Notes Template

Use this structure for release notes:

```markdown
## Version X.Y.Z

### Highlights
- ...

### Upgrade Notes
- Backup requirements:
- Migration behavior:
- Config/secret changes:
- Rollback notes:

### Security Notes
- ...

### Operator Verification
- ...

### CI/CD Release Evidence
- Commit SHA:
- Attached evidence bundle files:
  - `release-evidence.json`
  - `release-evidence.md`
  - `release-evidence-checksums.sha256`
  - `release-evidence-release-notes.md`

### Documentation Impact
- Updated | Not needed | Deferred: ...
```

## Related

- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) — backup, restore, upgrade, rollback runbook.
- [CONTRIBUTING.md](CONTRIBUTING.md) — PR validation checklist.
- [TESTING.md](TESTING.md) — test project taxonomy and commands.
- [DOCUMENTATION_ARCHITECTURE.md](DOCUMENTATION_ARCHITECTURE.md) — docs impact contract.

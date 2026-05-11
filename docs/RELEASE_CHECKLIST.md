ABOUTME: Release readiness checklist for code, migrations, configuration, security, and documentation.
ABOUTME: Provides the release documentation contract for self-hostable operators and contributors.

# Release Checklist

> **Audience:** Contributors | Operators | AI agents
> **Status:** Implemented
> **Owner:** Platform/Ops
> **Last Verified:** 2026-05-06
> **Source Anchors:** `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/CONTRIBUTING.md`, `docs/TESTING.md`, `.github/workflows/test.yml`, `.github/workflows/agent-context.yml`

Use this checklist before tagging or publishing a release. A release is not ready until operators can understand what changed, how to upgrade, how to verify, and how to roll back.

## Release Metadata

- [ ] Version/tag is selected.
- [ ] Commit SHA is recorded.
- [ ] Image tags or deployment artifacts are recorded.
- [ ] Image digests, SBOM/provenance evidence, and image scan artifacts are recorded when container images are published.
- [ ] Deployment environment, approver, webhook result, smoke-check result, and rollback note are recorded for staging/production deployments.
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
- [ ] Required GitHub checks match [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md): fast build/test, OpenAPI drift, CodeQL, dependency review, and any path-relevant security/Cerbos checks.
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

- [ ] OpenAPI drift artifacts are clean, or generated `swagger.json` / NSwag client changes are reviewed and committed.
- [ ] Container image digest, SBOM/provenance, Trivy scan output, and image tag evidence are recorded when images are published.
- [ ] Deployment evidence includes environment, commit SHA, webhook result, smoke-check result, workflow run link, and rollback note.
- [ ] Production deployment approval and branch restrictions are configured in GitHub Environment settings.
- [ ] Long-lived release evidence is copied from expiring GitHub Actions artifacts into release notes or durable storage when required.

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

### Documentation Impact
- Updated | Not needed | Deferred: ...
```

## Related

- [BACKUP_RESTORE_UPGRADE.md](BACKUP_RESTORE_UPGRADE.md) — backup, restore, upgrade, rollback runbook.
- [CONTRIBUTING.md](CONTRIBUTING.md) — PR validation checklist.
- [TESTING.md](TESTING.md) — test project taxonomy and commands.
- [DOCUMENTATION_ARCHITECTURE.md](DOCUMENTATION_ARCHITECTURE.md) — docs impact contract.

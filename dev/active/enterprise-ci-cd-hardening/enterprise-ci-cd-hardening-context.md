ABOUTME: Current working context for the enterprise CI/CD hardening workstream.
ABOUTME: Preserves verified state, decisions, next steps, and blockers for future implementation agents.

# Enterprise CI/CD Hardening - Context

Last Updated: 2026-05-30 Europe/Brussels

## SESSION PROGRESS (2026-05-29 Europe/Brussels)

### COMPLETED

- Re-read `AGENTS.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `dev/active/README.md`, `.claude/commands/dev-docs.md`, and the senior CTO feedback skill/resources.
- Re-read the old CI/CD plan, context, and tasks files.
- Audited current workflow inventory under `.github/workflows/`.
- Verified no Tavily MCP tool is available in this session and no install candidate exists.
- Attempted Context7; it returned quota exhausted.
- Used official primary-source web docs for GitHub Actions, artifact attestations, Docker Buildx attestations, dependency review, zizmor, and OpenSSF Scorecard.
- Ran `dotnet build --configuration Release --verbosity quiet`; build passed with 3192 warnings.
- Rewrote the plan/context/tasks workstream around the current state and remaining enterprise gaps.
- Re-opened the workstream for deeper senior CTO feedback after the user requested a more ambitious CI/CD plan.
- Researched `contributoragreements.org`, `cla-assistant.io`, and `contributor-assistant/github-action`.
- Verified the CLA Assistant GitHub Action repository is archived/read-only and its sample workflow uses `pull_request_target`.
- Added CLA/DCO legal contribution governance, license policy scanning, release evidence bundling, SLSA/attestation verification, and repository-settings drift checks to the plan.
- Tightened OpenAPI wording so `schemas/openapi.json` is the canonical root schema artifact; `Explore.API/swagger.json` is only a legacy cleanup reference.
- Re-ran `dotnet build --configuration Release --verbosity quiet`; latest build failed outside this docs workstream in `Explore.Persistence/Repositories/ActorSubscriptionRepository.cs` with CS0266.
- Attempted Tavily MCP research in the current implementation session; Tavily returned plan-limit error `432` for GitHub Actions security and artifact-attestation searches.
- Attempted Context7 documentation lookup for GitHub Actions and Docker Buildx; Context7 returned monthly quota exhausted.
- Added `ci-cd-change` to `.claude/contract/intents.yaml` using the manifest shape currently enforced by `AgentContextIntentManifestTests`.
- Added `.github/CODEOWNERS` for workflow, Dockerfile, dependency manifest, release, operations, self-hosting, configuration, contributing, and legal-governance paths.
- Added `merge_group` to `.github/workflows/test.yml` so `Build & Test` can participate in merge queue before it becomes a required merge-queue gate.
- Removed the stale `Explore.API/swagger.json` drift check from `.github/workflows/_build-test.yml`; `openapi-contract.yml` remains the canonical drift guard for `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- Corrected `docs/CONTRIBUTING.md` to describe `schemas/openapi.json` as the NSwag input instead of `swagger.json`.
- Added repository-settings evidence and contributor legal-governance decision placeholders to `docs/CI_CD_GOVERNANCE.md`.
- Ran touched-file `git diff --check`; the CI/CD slice files passed whitespace checks.
- Ran `dotnet build --configuration Release --verbosity quiet`; build passed with warnings.
- Ran `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`; 179 succeeded, 1 skipped, and 1 failed because unrelated untracked AI integration code defines `Explore.Application.Contracts.Infrastructure.Ai.AiChatRequest`, which matches the architecture test's `*Request` CQRS query naming rule outside a `Queries` namespace.
- Checked local availability for `actionlint` and `zizmor`; neither command is installed in this environment.
- Reworked `.github/workflows/test.yml` from trigger-level `paths-ignore` to an always-present wrapper with internal build/test change detection and a merge-queue-safe fallback.
- Added `run-fast-tests` to `.github/workflows/_build-test.yml` so the reusable workflow can intentionally pass without restore/build/test when the caller detects only docs/schema/ops paths.
- Updated `docs/CI_CD_GOVERNANCE.md` to mark `Build & Test` as ready for required-check use after repository-side check-name verification, while keeping other path-filtered workflows conditional until separately wrapped.
- Ran touched-file `git diff --check` after the no-op wrapper slice; it passed.
- Parsed `.github/workflows/test.yml` and `.github/workflows/_build-test.yml` with local PyYAML; both parsed successfully. `actionlint`, `zizmor`, `ruby`, and `yq` are not installed locally.
- Resolved the active NuGet vulnerability finding by upgrading `MailKit` from `4.15.1` to patched `4.16.0` and regenerating lock files with `dotnet restore --force-evaluate`.
- Kept the `_build-test.yml` NuGet audit blocking for any direct or transitive vulnerable package, and documented the exception requirements in `docs/CI_CD_GOVERNANCE.md`.
- Fixed the audit parser to read both `advisoryUrl` and NuGet's current lowercase `advisoryurl` JSON field so CI failure output includes advisory links.
- Re-ran `dotnet list Explore.sln package --vulnerable --include-transitive --format json --output-version 1 --no-restore`; no vulnerable package entries were reported after the MailKit upgrade.
- Verified `dotnet restore --locked-mode` after lock-file regeneration.
- Added `.github/workflows/workflow-security.yml` as an always-present workflow security check that no-ops for unrelated changes and validates workflow-governance changes.
- Replaced repository-owned Python workflow policy scripts with file-based C# scripts run through `dotnet run <script>.cs -- <args>`.
- Added `.github/scripts/validate-action-pins.cs` to reject external `uses:` references unless they are pinned to full 40-character SHAs with same-line version comments, while allowing local reusable workflows such as `./.github/workflows/_build-test.yml`.
- Ran the action-pin validator locally against `.github/workflows`; all current external actions passed.
- Extended `Workflow Security` to install `actionlint` `1.7.12` from the release archive after SHA-256 verification, run it as a blocking workflow linter, install `zizmor` `1.25.2` in an isolated Python virtual environment, run `zizmor` offline as blocking SARIF/text evidence for medium-or-higher findings, and upload `workflow-security-evidence` for 30 days.
- Downloaded and ran `actionlint` `1.7.12` locally; `.github/workflows/*.yml` passed.
- Added `persist-credentials: false` to read-only checkout steps across workflows, removing the medium-severity checkout credential findings.
- Replaced direct GitHub expression interpolation inside shell commands with environment variables in `_build-test.yml` and `deploy-coolify.yml`, removing high-confidence template-injection findings.
- Ran `zizmor` `1.25.2` locally in a temporary virtual environment after remediation; it reports no medium-or-higher findings, so `Workflow Security` now fails on nonzero `zizmor` SARIF or text scan exits.
- Added `.github/scripts/validate-dependabot-policy.cs` and wired it into `Workflow Security` so the weekly grouped `github-actions` Dependabot lane remains enforced alongside immutable SHA pins.
- Updated `Workflow Security` to set up the pinned .NET SDK from `global.json` before running the C# policy validators.
- Added `.github/scripts/validate-nuget-vulnerabilities.cs` and updated `_build-test.yml` to use it instead of embedded Python for NuGet audit report parsing.
- Added `.github/scripts/write-container-digest-evidence.cs`, added .NET setup to `_container-build.yml`, and updated the container evidence step to use C# instead of embedded Python for digest JSON generation.
- Re-checked `.github/workflows` for embedded Python; only `workflow-security.yml` still invokes Python, and only to install/run the third-party `zizmor` tool from an isolated virtual environment.
- Renamed the new workflow security job display name to `Workflow Security` before it is made required in branch protection, so the required-check name matches the governance table.
- Added retained Trivy SARIF output to `_container-build.yml` before the blocking Trivy table scan, so container vulnerability evidence is available as both text and machine-readable SARIF artifacts.
- Replaced remaining direct GitHub expression interpolation inside staging deploy shell commands with environment variables in `deploy-coolify-develop.yml`, matching the production deploy hardening pattern.
- Added downloadable Buildx SBOM/provenance registry evidence to `_container-build.yml` through `docker buildx imagetools inspect` and `--raw` OCI index output for the pushed GHCR digest.
- Added `.github/scripts/write-artifact-checksums.cs` and wired `_container-build.yml` to generate SHA-256 checksum manifests for retained container evidence artifacts before upload.
- Added `_container-build.yml` `gh attestation verify` enforcement for the pushed GHCR digest before checksum/upload/deploy dependency completion. Verification is constrained to the repository, reusable signer workflow, source ref/digest, SLSA provenance predicate, and GitHub-hosted runner trust boundary.
- Added `.github/scripts/write-image-promotion-evidence.cs` and wired `_container-build.yml` to record primary-registry immutable `sha-*` / `dev-*` deployment tag promotion evidence, then inspect each tag and fail if it does not resolve to the built digest.
- Added `.github/actions/deploy-coolify` as the single local composite action for Coolify webhook invocation, redacted failure handling, bounded smoke checks, and deployment summaries across staging and production.
- Updated production and staging deploy workflows to publish full-commit immutable tags (`sha-${GITHUB_SHA}` and `dev-${GITHUB_SHA}`), checkout the local deploy action with read-only credentials, and record the expected immutable image tag in deploy evidence.
- Extended `Workflow Security` change detection to treat `.github/actions/**` changes as workflow-security-relevant because local composite actions can carry privileged deployment behavior.
- Reworked `.github/workflows/security-tests.yml`, `.github/workflows/cerbos-policy-check.yml`, and `.github/workflows/agent-context.yml` from trigger-level path filters to always-present detector/no-op checks with `merge_group` support.
- Verified the three required-check wrapper updates with PyYAML, `actionlint`, `zizmor`, and the C# action-pin validator.
- Reworked `.github/workflows/codeql.yml` from trigger-level `paths-ignore` to an always-present detector plus matrix-level no-op behavior for CodeQL-ignored changes. The matrix check names remain present for Actions, C#, and JavaScript/TypeScript, while schedule/manual runs still execute full analysis.
- Added `docs/legal/CONTRIBUTION_GOVERNANCE.md` as the Phase 2 legal contribution decision record. It summarizes ContributorAgreements.org, its legal questions and agreement chooser, hosted CLA Assistant, the archived `contributor-assistant/github-action`, and GitHub `pull_request_target` constraints for the implemented CLA-only workflow.
- Updated `docs/CI_CD_GOVERNANCE.md`, `docs/CONTRIBUTING.md`, `docs/index.md`, and `.github/PULL_REQUEST_TEMPLATE.md` to point contributors and maintainers at the active CLA requirement and metadata-only enforcement workflow.
- Added `docs/legal/CLA.md` as the active ISLAMU Event CLA. It grants ISLAMU nonprofit broad inbound copyright and patent rights so ISLAMU can provide, sell, sublicense, or relicense ISLAMU Event under alternative terms when social-impact or operational needs require it.
- Added `.github/workflows/cla.yml` and `.github/scripts/validate-cla-pr.cs` as the hardened CLA gate. The workflow uses `pull_request_target`, checks out only the trusted base commit, uses read-only permissions, validates PR metadata with C#, and does not execute pull-request head code.
- Added `.github/workflows/scorecard.yml` as an advisory scheduled/manual OpenSSF Scorecard lane that uploads SARIF to code scanning and retains `scorecard-evidence` for 30 days without publishing public Scorecard results.
- Added `.github/workflows/secret-scanning.yml` as a bounded `gitleaks` lane. PR, push, and merge-queue runs block on newly introduced leaks for the changed commit range; scheduled/manual history scans remain advisory evidence until legacy findings are triaged or baselined.

### IN PROGRESS

- Phase 0 is partially implemented and Phase 1 is locally complete. Phase 2 is now locally implemented with CLA-only posture, broad ISLAMU nonprofit inbound rights, `docs/legal/CLA.md`, metadata-only `pull_request_target` enforcement, explicit trusted-bot allowlist, PR-metadata signature storage, and a repository-owned C# validator; repository-side required-check configuration remains Phase 8. Phase 1 now has always-present detector/no-op wrappers for `Build & Test`, CodeQL, Security Integration, Cerbos Policy, and agent-context; repository-side required-check name verification remains in Phase 8. Phase 3 has repository-owned C# helper scripts, SHA-pin policy enforcement, Dependabot update-policy validation, blocking `actionlint`, blocking `zizmor`, checkout credential hardening, retained workflow security evidence, advisory scheduled OpenSSF Scorecard SARIF evidence, and bounded `gitleaks` feedback for newly introduced secrets. Phase 6 has digest JSON evidence, downloadable SBOM/provenance registry evidence, immutable primary-registry promotion evidence, retained Trivy text/SARIF scan artifacts, checksum manifests, pre-deploy GHCR attestation verification, and Docker base image digest policy. Phase 7 now has one local Coolify deploy execution action shared by staging and production, full-commit immutable-tag evidence, deploy-time expected digest resolution from retained promotion artifacts, deployment-freeze override evidence, and required production smoke checks for deployed components, but still needs Coolify-side digest or immutable-tag consumption proof. Remaining Phase 0 work is external settings evidence and CODEOWNERS owner/team verification. Remaining Phase 3 work is repository-side required-check verification and triage/baseline of legacy secret-scanning findings before any history-wide secret lane becomes blocking.

### NEXT

1. Verify `.github/CODEOWNERS` owner resolution for `@islamu-ngo/platform-ops` or replace it with the actual maintainer team/user.
2. Verify the `Contributor License Agreement` status check name in repository branch protection after the CLA workflow has run successfully on real PRs.
3. Verify repository-side required-check names before branch protection is updated; `Build & Test`, CodeQL, Security Integration, Cerbos, and agent-context now have always-running wrappers locally.
4. Keep legal review open for final wording of `docs/legal/CLA.md`, but do not weaken the active CLA-only enforcement without an explicit maintainer decision.
5. Continue Phase 7 by proving Coolify consumes either explicit digests or the full-commit immutable `sha-*` / `dev-*` tags whose registry digests are now verified before deploy jobs start.
6. Triage or baseline legacy history-wide `gitleaks` findings before considering scheduled/manual secret scanning blocking. PR/push/merge-queue ranges now block on newly introduced leaks.
7. Require `Workflow Security` after repository-side check-name verification so unpinned external action references, `actionlint` failures, and medium-or-higher `zizmor` findings cannot merge.

### BLOCKERS

- Tavily MCP is exposed but blocked by the current plan limit (`432`).
- Context7 quota is exhausted.
- Coolify digest deployment capability is still unknown.
- Final legal wording of `docs/legal/CLA.md` should still be reviewed by counsel before broad external contribution volume.
- `contributor-assistant/github-action` is archived as of 2026-03-23; this risk is avoided by using repository-owned C# validation instead.
- GitHub repository settings are not visible from local files and must be verified by a maintainer or GitHub API/connector with sufficient permissions.
- The worktree contains many unrelated user changes; only files in this workstream should be touched unless the user explicitly expands scope.

## Quick Resume

Read in this order:

1. `AGENTS.md`
2. `docs/QUICK_REFERENCE.md`
3. `docs/GOVERNANCE.md`
4. `docs/OPERATIONS.md`
5. `.claude/skills/senior-cto-feedback/SKILL.md`
6. `dev/active/enterprise-ci-cd-hardening/enterprise-ci-cd-hardening-plan.md`
7. `dev/active/enterprise-ci-cd-hardening/enterprise-ci-cd-hardening-tasks.md`

Then implement Phase 0 or Phase 1 from the task checklist.

## Key Files And Responsibilities

| File | Responsibility |
|---|---|
| `.github/workflows/test.yml` | Main fast CI wrapper. Always triggers for branch/PR/merge queue events, detects build/test-relevant paths internally, and calls `_build-test.yml` with either full fast tests or an intentional no-op. |
| `.github/workflows/_build-test.yml` | Reusable restore/audit/format/build/test workflow. Supports `run-fast-tests` no-op mode; NuGet vulnerability audit remains blocking; OpenAPI drift check removed because canonical drift belongs to `openapi-contract.yml`. |
| `.github/workflows/openapi-contract.yml` | Canonical `schemas/openapi.json` / API inventory / NSwag client drift and determinism guard. |
| `.github/workflows/cla.yml` | Active metadata-only CLA gate using `pull_request_target`; checks out trusted base code only and runs the C# PR metadata validator. |
| `.github/scripts/validate-cla-pr.cs` | File-based C# CLA validator that requires the checked ISLAMU CLA statement and `CLA Signature: @github-username` lines for GitHub-linked PR contributors. |
| `docs/legal/CLA.md` | Active ISLAMU Event CLA granting ISLAMU nonprofit broad inbound rights for alternative licensing, sublicensing, sale, and social-impact distribution. |
| `docs/legal/CONTRIBUTION_GOVERNANCE.md` | CLA decision record for legal posture, signature storage, automation token model, archived action risk, and `pull_request_target` threat model. |
| `.github/workflows/_container-build.yml` | Builds/pushes images, emits digest evidence, immutable primary-registry promotion evidence, downloadable OCI inspect/index evidence for Buildx SBOM/provenance attestations, Trivy text/SARIF scan artifacts, GHCR attestation verification JSON, checksum manifests, and GitHub artifact attestations. |
| `.github/workflows/deploy-coolify.yml` | Production deploy workflow; duplicated with staging workflow. |
| `.github/workflows/deploy-coolify-develop.yml` | Staging deploy workflow; duplicated with production workflow. |
| `.github/actions/deploy-coolify/action.yml` | Local composite action shared by staging and production for Coolify webhooks, smoke checks, required production smoke enforcement, deployment-freeze override enforcement, redacted summaries, expected immutable image tag evidence, and expected digest evidence. |
| `.github/workflows/codeql.yml` | Always-present CodeQL for Actions, C#, and JavaScript/TypeScript with internal CodeQL-relevant change detection, scheduled/manual full runs, merge queue support, and matrix-level no-op pass for ignored changes. |
| `.github/workflows/security-tests.yml` | Always-present security integration check with internal security-path detection, nightly/manual execution, merge queue support, and no-op pass for unrelated changes. |
| `.github/workflows/cerbos-policy-check.yml` | Always-present Cerbos policy validation with internal authz-path detection, nightly/manual execution, merge queue support, and no-op pass for unrelated changes. |
| `.github/workflows/agent-context.yml` | Always-present AI/context governance validation with internal context-path detection, merge queue/manual execution, and no-op pass for unrelated changes. |
| `.github/workflows/dependency-review.yml` | Dependency review and OpenSSF scorecard display. |
| `.github/workflows/workflow-security.yml` | Always-present workflow-governance security check; sets up .NET, runs C# policy validators, blocking actionlint, blocking zizmor evidence, and no-ops for unrelated changes. |
| `.github/workflows/scorecard.yml` | Advisory scheduled/manual OpenSSF Scorecard lane that uploads SARIF to code scanning and retains `scorecard-evidence` without publishing public results. |
| `.github/workflows/secret-scanning.yml` | Bounded gitleaks lane that blocks newly introduced leaks on PR/push/merge-queue ranges and retains advisory scheduled/manual history evidence. |
| `.github/scripts/validate-action-pins.cs` | File-based C# policy validator requiring external `uses:` references to use full SHAs plus same-line version comments while allowing local reusable workflows. |
| `.github/scripts/validate-dependabot-policy.cs` | File-based C# policy validator requiring a weekly grouped `github-actions` Dependabot lane so immutable action SHA pins remain updateable. |
| `.github/scripts/validate-nuget-vulnerabilities.cs` | File-based C# parser for `dotnet list package --vulnerable` JSON output; fails fast CI when direct or transitive advisory findings exist. |
| `.github/scripts/validate-dockerfile-base-images.cs` | File-based C# validator requiring deployable Dockerfiles to use tag-plus-digest base image references. |
| `.github/scripts/write-container-digest-evidence.cs` | File-based C# writer for normalized container image digest evidence generated by `_container-build.yml`. |
| `.github/scripts/write-image-promotion-evidence.cs` | File-based C# writer for primary-registry immutable deployment tag promotion evidence generated by `_container-build.yml`. |
| `.github/scripts/resolve-deploy-image-evidence.cs` | File-based C# resolver that reads retained promotion artifacts before deploy and exposes expected immutable image tag/digest outputs to Coolify deploy jobs. |
| `.github/scripts/write-artifact-checksums.cs` | File-based C# writer for SHA-256 checksum manifests covering retained CI/CD evidence artifacts. |
| `docs/CI_CD_GOVERNANCE.md` | Current CI/CD governance source of truth. |
| `docs/OPERATIONS.md` | Deployment protection, health endpoints, digest fallback guidance. |
| `docs/RELEASE_CHECKLIST.md` | Release evidence contract. |
| `docs/TESTING.md` | Test project taxonomy and per-project command policy. |
| `docs/CONTRIBUTING.md` | Contributor guidance including active CLA signing requirements and canonical `schemas/openapi.json` client regeneration guidance. |
| `docs/legal/CLA.md` | Active legal contribution document path for CLA-only inbound contribution posture. |

## Key Decisions

1. Treat this as a CI/CD platform hardening program, not one workflow cleanup PR.
2. Add a repository intent for CI/CD before more recurring workflow work.
3. Fix current incorrect/stale checks before adding stricter gates.
4. Make workflow YAML lint/security scanning required for workflow changes. Repository-owned C# helper scripts, SHA-pin enforcement, Dependabot maintenance-policy validation, blocking `actionlint`, blocking medium-or-higher `zizmor`, and retained evidence exist now.
5. Promote image digests, not mutable tags, into deployment.
6. Verify attestations before deploy; generating provenance alone is not enough.
7. Consolidate deploy logic into one reusable deployment path.
8. Preserve fast PR feedback, but do not call path-skipped workflows required without no-op wrappers. `Build & Test`, CodeQL, Security Integration, Cerbos Policy, and agent-context now satisfy this by moving path filtering inside each workflow and reporting an intentional no-op pass for unrelated changes.
9. Use repository settings evidence as a deliverable because branch/environment/security settings are outside local files.
10. Enforce CLA-only contribution governance before broad external contribution volume grows.
11. Treat `pull_request_target` as privileged. It is allowed only for the CLA metadata/status workflow and must never run PR head code.
12. Treat `schemas/openapi.json` as the canonical OpenAPI artifact. Any `Explore.API/swagger.json` references are legacy cleanup targets.
13. Keep the current `intents.yaml` format aligned with `AgentContextIntentManifestTests` until the separate JSON schema drift is intentionally reconciled.

## Constraints And Rules To Remember

- Use project-level test commands; no solution-level `dotnet test`.
- Keep `AGENTS.md` and `docs/QUICK_REFERENCE.md` rules in force.
- External actions stay full-SHA pinned with same-line version comments.
- Repository-owned CI helper scripts under `.github/scripts/` stay file-based C# scripts run with `dotnet run <script>.cs -- <args>` unless a documented exception is approved. Each script declares `#:property RestorePackagesWithLockFile=false` so file-based script runs do not leave transient `.github/scripts/packages.lock.json` files. Shell blocks stay orchestration-only; third-party tools may use their required runtime.
- Local reusable workflows stay path-based.
- Fork PRs get read-only validation only; no secrets, OIDC, package write, or deployment credentials.
- `pull_request_target` is banned outside the CLA metadata/status workflow unless a separate threat model approves the exact pattern.
- Generated OpenAPI/NSwag artifacts are never hand-edited.
- Canonical OpenAPI drift paths are `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- CLA automation may use `pull_request_target` only for trusted-base metadata validation and must not checkout/build/test untrusted PR code.
- CLA signatures are stored as PR body metadata and GitHub PR audit trail, not source branch changes.
- Production deploys require protected environment approval and retained evidence.
- Update all three dev-doc files as implementation progresses.

## Validation Baseline

Latest local build command:

```bash
dotnet build --configuration Release --verbosity quiet
```

Superseded result: failed earlier on 2026-05-29 with 1 error and 3806 warnings.

Earlier blocking error:

- `Explore.Persistence/Repositories/ActorSubscriptionRepository.cs`: `CS0266` cannot implicitly convert `IQueryable<ActorSubscription>` to `IOrderedQueryable<ActorSubscription>`.

Superseded result: passed on 2026-05-29 with 15 warnings.

Latest full-repo result: failed on 2026-05-29 after the NuGet remediation slice with unrelated application/analyzer errors in the current dirty worktree, including:

- `Explore.Application/Features/Actors/Handlers/Queries/GetActorDetailsRequestHandler.cs`: `CS8603` possible null reference return.
- `Explore.Application/Specifications/Events/EventQuerySpecification.cs`: `CS8629` nullable value type may be null.
- `Explore.Application/Features/Tenants/Handlers/Commands/ReorderTenantNavLinks/ReorderTenantNavLinksCommandHandler.cs`: `CS8602` dereference of a possibly null reference.
- `Explore.Application/DTOs/EventSessionTemplateSync/Validators/TemplateSyncPlanDtoValidator.cs`: `CA1305` culture-sensitive `int.Parse(string)`.
- `Explore.Application/Behaviors/AuthorizationBehavior.cs`: `CA1873` expensive logging argument.
- `Explore.Application/Telemetry/TranslationMetrics.cs`: `CA2000` disposable object not disposed.

Affected package compile check: `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet --no-restore` passed with warnings after `MailKit` `4.16.0`.

Notable warning classes from the latest full build before dependency remediation:

- package version constraint warning for `Microsoft.CodeAnalysis.Workspaces.MSBuild`.
- many analyzer/nullability/naming warnings from current dirty worktree.

NuGet vulnerability audit:

```bash
dotnet list Explore.sln package --vulnerable --include-transitive --format json --output-version 1 --no-restore
```

Latest result: passed on 2026-05-29 after `MailKit` `4.15.1` -> `4.16.0`; follow-up parser check reported `vulnerable-packages=0`.

Latest architecture test command:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Result: failed on 2026-05-29 with 181 total tests, 179 succeeded, 1 skipped, 1 failed.

Blocking failure:

- `CqrsPatternTests.Queries_ShouldResideIn_QueriesNamespace` rejects `Explore.Application.Contracts.Infrastructure.Ai.AiChatRequest` because it is an application contract record ending in `Request` outside a `Queries` namespace. This file is unrelated untracked AI integration work and was not changed by the CI/CD hardening slice.

Tooling checks:

- Touched-file `git diff --check` passed for `.claude/contract/intents.yaml`, `.github/CODEOWNERS`, edited workflows, edited docs, and this workstream directory.
- Latest touched-file `git diff --check` also passed for the `Build & Test` no-op wrapper and documentation updates.
- Local PyYAML parse passed for `.github/workflows/test.yml` and `.github/workflows/_build-test.yml`.
- Local PyYAML parse passed for `.github/workflows/workflow-security.yml`.
- Local action-pin validation passed: `dotnet run .github/scripts/validate-action-pins.cs -- .github/workflows`.
- Local Dependabot policy validation passed: `dotnet run .github/scripts/validate-dependabot-policy.cs -- .github/dependabot.yml`.
- Local NuGet audit parser validation passed: `dotnet run .github/scripts/validate-nuget-vulnerabilities.cs -- /tmp/nuget-vulnerabilities.json`.
- Local container digest evidence writer validation passed with representative environment input and created `artifacts/container/event-api-digest.json` in a temporary directory.
- Local workflow updates now retain Trivy SARIF evidence in `artifacts/container/<image>-trivy.sarif` before the blocking Trivy table scan.
- Local CodeQL workflow now keeps `Analyze (actions)`, `Analyze (csharp)`, and `Analyze (javascript-typescript)` checks present while no-oping for CodeQL-ignored paths.
- Legal contribution governance is implemented as CLA-only. `docs/legal/CLA.md`, `.github/workflows/cla.yml`, and `.github/scripts/validate-cla-pr.cs` exist; contributors sign through PR body metadata.
- Local container build workflow now exports Buildx SBOM/provenance registry evidence to `artifacts/container/<image>-oci-inspect.txt` and `artifacts/container/<image>-oci-index.json`.
- Local checksum manifest writer validation passed with representative artifact inputs and created a SHA-256 manifest for retained evidence files.
- Local container build workflow now writes `artifacts/container/<image>-attestation-verification.json` from `gh attestation verify` before dependent deploy jobs can start.
- Local container build workflow now writes `artifacts/container/<image>-promotion.json`, `artifacts/container/<image>-promotion-tags.txt`, and `artifacts/container/<image>-promotion-*.txt` by recording primary-registry `sha-*` / `dev-*` tags and verifying each resolves to the built digest before dependent deploy jobs can start.
- Coolify deploy workflows now share `.github/actions/deploy-coolify`, download retained container promotion evidence, resolve expected image digests through `.github/scripts/resolve-deploy-image-evidence.cs`, and record environment, component, commit, expected immutable image tag, expected digest, promotion evidence path, webhook result, smoke result, workflow run, and rollback note.
- Docker base image digests are pinned in `Explore.API/Dockerfile` and `Explore.Blazor/Dockerfile` using tag-plus-digest .NET base references. `Workflow Security` now runs `.github/scripts/validate-dockerfile-base-images.cs`, and `.github/dependabot.yml` contains weekly Docker update blocks for both deployable Dockerfile directories.
- `.github/scripts/packages.lock.json` was removed after adding script-local lock-file opt-out directives to every repository-owned C# helper script; `.github/scripts/` now contains only `*.cs` helper scripts, including `write-image-promotion-evidence.cs`.
- Local `actionlint` verification passed after downloading `actionlint` `1.7.12` and checking SHA-256 `8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8` for the Linux amd64 release archive.
- Local `zizmor` verification ran with `zizmor` `1.25.2` in a temporary Python virtual environment; after remediation it reports no medium-or-higher findings across `.github/workflows`.
- Full-repo `git diff --check` returned exit 2 without actionable output in the larger dirty worktree.
- `actionlint` and `zizmor` are not installed globally; local verification used downloaded/temporary tool installs.

## Current Known Risks / Unknowns

- Coolify digest deployment may require platform-side configuration not representable in YAML.
- ATCR may not support OIDC; if not, token scope/rotation must be documented.
- Repository settings must be verified externally.
- Final CLA wording should receive legal review before broad external contribution volume.
- NuGet vulnerability audit is currently clean after `MailKit` remediation; future advisory exceptions must include owner/date/advisory/removal-condition evidence before weakening CI.
- Workflow changes should be made carefully because many unrelated files are dirty in the worktree.

## Handoff Notes

The plan now intentionally raises the bar beyond the old historical PR sequence. Do not implement from the obsolete phase numbering in the old plan. Use the rewritten phase list and tasks file.

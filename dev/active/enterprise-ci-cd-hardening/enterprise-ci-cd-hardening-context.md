ABOUTME: Current working context for the enterprise CI/CD hardening workstream.
ABOUTME: Preserves verified state, decisions, next steps, and blockers for future implementation agents.

# Enterprise CI/CD Hardening - Context

Last Updated: 2026-05-29 Europe/Brussels

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
- Added `.github/scripts/validate-action-pins.py` to reject external `uses:` references unless they are pinned to full 40-character SHAs with same-line version comments, while allowing local reusable workflows such as `./.github/workflows/_build-test.yml`.
- Ran the action-pin validator locally against `.github/workflows`; all current external actions passed.

### IN PROGRESS

- Phase 0 and Phase 1 are partially implemented. Phase 3 has started with SHA-pin policy enforcement. Remaining Phase 0 work is external settings evidence and CODEOWNERS owner/team verification. Remaining Phase 1 work is no-op wrappers for any other workflows before they are made globally required. Remaining Phase 3 work is `actionlint`, `zizmor`, SARIF/evidence retention, and optional Scorecard/secret-scanning lanes.

### NEXT

1. Verify `.github/CODEOWNERS` owner resolution for `@islamu-ngo/platform-ops` or replace it with the actual maintainer team/user.
2. Create the contribution legal governance decision:
   - CLA vs DCO vs both;
   - inbound license and patent scope;
   - CLA document path;
   - signature storage location and privacy retention.
3. Review CodeQL, Security Integration, Cerbos, and agent-context check requirements before marking any path-filtered workflow globally required; add always-running wrappers first if needed.
4. Add a hardened CLA workflow only after the `pull_request_target` threat model is documented.
5. Add workflow security gate with actionlint and zizmor.
6. Require `Workflow Security` after repository-side check-name verification so unpinned external action references cannot merge.

### BLOCKERS

- Tavily MCP is exposed but blocked by the current plan limit (`432`).
- Context7 quota is exhausted.
- Coolify digest deployment capability is still unknown.
- CLA legal posture is undecided; this needs project owner/legal review before enabling enforcement.
- `contributor-assistant/github-action` is archived as of 2026-03-23; implementation must decide whether to accept, fork/vendor, or replace it.
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
| `.github/workflows/cla.yml` | Planned CLA/DCO contributor legal gate; does not exist yet. |
| `.github/workflows/_container-build.yml` | Builds/pushes images, emits digest evidence, SBOM/provenance, Trivy scan, and attestations. |
| `.github/workflows/deploy-coolify.yml` | Production deploy workflow; duplicated with staging workflow. |
| `.github/workflows/deploy-coolify-develop.yml` | Staging deploy workflow; duplicated with production workflow. |
| `.github/workflows/codeql.yml` | CodeQL for Actions, C#, and JavaScript/TypeScript. |
| `.github/workflows/dependency-review.yml` | Dependency review and OpenSSF scorecard display. |
| `.github/workflows/workflow-security.yml` | Always-present workflow-governance security check; runs action-pin validation for workflow/security-input changes and no-ops for unrelated changes. |
| `.github/scripts/validate-action-pins.py` | Local policy validator requiring external `uses:` references to use full SHAs plus same-line version comments while allowing local reusable workflows. |
| `docs/CI_CD_GOVERNANCE.md` | Current CI/CD governance source of truth. |
| `docs/OPERATIONS.md` | Deployment protection, health endpoints, digest fallback guidance. |
| `docs/RELEASE_CHECKLIST.md` | Release evidence contract. |
| `docs/TESTING.md` | Test project taxonomy and per-project command policy. |
| `docs/CONTRIBUTING.md` | Contributor guidance; contains stale `swagger.json` wording. |
| `docs/legal/CLA.md` or `docs/legal/DCO.md` | Planned legal contribution document path after owner/legal decision. |

## Key Decisions

1. Treat this as a CI/CD platform hardening program, not one workflow cleanup PR.
2. Add a repository intent for CI/CD before more recurring workflow work.
3. Fix current incorrect/stale checks before adding stricter gates.
4. Make workflow YAML lint/security scanning required for workflow changes. SHA-pin enforcement exists now; `actionlint` and `zizmor` are still pending.
5. Promote image digests, not mutable tags, into deployment.
6. Verify attestations before deploy; generating provenance alone is not enough.
7. Consolidate deploy logic into one reusable deployment path.
8. Preserve fast PR feedback, but do not call path-skipped workflows required without no-op wrappers. `Build & Test` now satisfies this by moving path filtering inside the workflow and reporting an intentional no-op pass for non-code changes.
9. Use repository settings evidence as a deliverable because branch/environment/security settings are outside local files.
10. Add CLA/DCO contribution governance before broad external contribution volume grows.
11. Treat `pull_request_target` as privileged. It is allowed only for the CLA metadata/status workflow and must never run PR head code.
12. Treat `schemas/openapi.json` as the canonical OpenAPI artifact. Any `Explore.API/swagger.json` references are legacy cleanup targets.
13. Keep the current `intents.yaml` format aligned with `AgentContextIntentManifestTests` until the separate JSON schema drift is intentionally reconciled.

## Constraints And Rules To Remember

- Use project-level test commands; no solution-level `dotnet test`.
- Keep `AGENTS.md` and `docs/QUICK_REFERENCE.md` rules in force.
- External actions stay full-SHA pinned with same-line version comments.
- Local reusable workflows stay path-based.
- Fork PRs get read-only validation only; no secrets, OIDC, package write, or deployment credentials.
- `pull_request_target` is banned outside the planned CLA metadata/status workflow unless a separate threat model approves the exact pattern.
- Generated OpenAPI/NSwag artifacts are never hand-edited.
- Canonical OpenAPI drift paths are `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- CLA automation may use `pull_request_target` only after threat-model review and must not checkout/build/test untrusted PR code.
- CLA signatures should not be written to protected source branches; prefer a remote private signatures repo or dedicated signatures branch.
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
- Local action-pin validation passed: `python3 .github/scripts/validate-action-pins.py .github/workflows`.
- Full-repo `git diff --check` returned exit 2 without actionable output in the larger dirty worktree.
- `actionlint` is not installed locally.
- `zizmor` is not installed locally.

## Current Known Risks / Unknowns

- Coolify digest deployment may require platform-side configuration not representable in YAML.
- ATCR may not support OIDC; if not, token scope/rotation must be documented.
- Repository settings must be verified externally.
- CLA signature storage, legal text, bot allowlist, and retention policy need owner/legal decisions.
- NuGet vulnerability audit is currently clean after `MailKit` remediation; future advisory exceptions must include owner/date/advisory/removal-condition evidence before weakening CI.
- Workflow changes should be made carefully because many unrelated files are dirty in the worktree.

## Handoff Notes

The plan now intentionally raises the bar beyond the old historical PR sequence. Do not implement from the obsolete phase numbering in the old plan. Use the rewritten phase list and tasks file.

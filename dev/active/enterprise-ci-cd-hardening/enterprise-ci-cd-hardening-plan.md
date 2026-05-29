ABOUTME: Re-baselined implementation plan for enterprise-grade GitHub Actions CI/CD hardening.
ABOUTME: Defines evidence, gaps, target architecture, rollout phases, and verification for the ISLAMU Event pipeline.

# Enterprise CI/CD Hardening - Implementation Plan

Last Updated: 2026-05-29 Europe/Brussels

## 0. Planning Metadata

- **Request:** Update the old `dev/active/enterprise-ci-cd-hardening/` plan into a stronger enterprise-grade CI/CD program for a pre-v1 self-hostable platform. Backward compatibility is not a goal.
- **Task directory:** `dev/active/enterprise-ci-cd-hardening/`
- **Planning status:** Re-baselined draft; old plan history preserved only as current-state evidence.
- **Matched intents:** `ci-cd-change` now exists in `.claude/contract/intents.yaml`; it routes workflow, deployment, dependency, release, and legal contribution governance changes through the Contribution Contract.
- **Relevant skills:** `.claude/skills/senior-cto-feedback/SKILL.md` and its resources.
- **Relevant rules:** No `.claude/rules/*.md` file targets `.github/**`; global repository and docs rules still apply.
- **Primary layers touched:** DevOps, Docs, Operations, Security, legal contribution governance, API contract governance, release governance.
- **Estimated complexity:** XL+. This crosses branch protection, GitHub repository settings, workflow security, CLA/DCO contribution intake, release promotion, container supply chain, OpenAPI governance, self-hosting, and operator runbooks.

### Research And Tooling Provenance

- **Tavily MCP:** Requested by the user and attempted in the implementation session; Tavily returned plan-limit error `432` for GitHub Actions security and artifact-attestation searches. Refresh external research when quota is available.
- **Context7 MCP:** Attempted for GitHub Actions and Docker Buildx documentation; Context7 returned monthly quota exhausted. Official primary-source docs were used through web research instead.
- **Primary sources used:** GitHub Actions secure-use, OIDC, environments, artifact attestations, dependency review, and `pull_request_target` warnings; Docker Buildx SBOM/provenance docs; zizmor docs; OpenSSF Scorecard docs; `contributor-assistant/github-action`; `cla-assistant.io`; `contributoragreements.org`.

## 1. Executive Summary

The existing CI/CD system has a credible foundation: Release builds, per-project TUnit lanes, OpenAPI drift detection against the canonical root artifact `schemas/openapi.json`, CodeQL, dependency review, pinned external actions, reusable container builds, SBOM/provenance generation, Trivy image scanning, GitHub Environments, Coolify deployment calls, smoke checks, nightly E2E, and CI/CD governance docs.

It is not yet "best CI/CD." The current design still has avoidable enterprise gaps:

- `Build & Test` now has an always-present detector/no-op wrapper, but required-check behavior is not fully safe while other important workflows still use path filters;
- path-skipped required-check behavior is still not fully safe for workflows other than `Build & Test` until no-op wrappers or path-filter removal are implemented;
- `test.yml` now includes `merge_group`, but required-check settings still need external repository evidence before it can be treated as merge-queue-ready;
- `_build-test.yml` no longer owns OpenAPI drift; `openapi-contract.yml` is the canonical guard for `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`;
- deploy workflows are duplicated and still deploy through webhook-side mutable configuration rather than a verified digest promotion contract;
- attestations are generated but not verified before deployment;
- workflow YAML now has a lightweight SHA-pin policy gate, but no dedicated `actionlint`/`zizmor` static-analysis gate yet;
- contribution legal provenance is not enforced by CI; there is no CLA/DCO workflow, CLA document, signature storage policy, or contributor allowlist governance;
- `.github/CODEOWNERS` now protects `.github/**`, Dockerfiles, dependency manifests, release docs, operations docs, and legal contribution docs, but the referenced GitHub owner/team still needs repository-side validation;
- branch protection, environment reviewers, secret scanning, push protection, and organization action policies remain out-of-repo settings that must be verified;
- the active `MailKit` package vulnerability has been remediated and the NuGet audit gate remains blocking; future temporary exceptions require explicit owner/date/advisory/removal-condition evidence.

The target state is a small, strict, auditable CI/CD control plane:

1. Every PR gets always-present required checks or explicit no-op pass jobs.
2. Contributor legal status is checked before merge through a hardened CLA/DCO gate.
3. Workflow definitions are linted and security-scanned before they can change; the first implemented step blocks unpinned external `uses:` references.
4. API contracts regenerate deterministically and block stale OpenAPI/NSwag diffs.
5. Images are built once, scanned, SBOM/provenance-attested, and promoted by digest.
6. Deployment verifies the attested digest, uses environment protection, runs bounded `/alive` and `/health` checks, and retains evidence.
7. Repository settings enforce the YAML contract instead of relying on documentation alone.

### Senior CTO Deep Feedback Applied

This plan deliberately raises the bar beyond the previous baseline:

- **Legal contribution intake is now in scope.** Add a CLA/DCO gate, but do it safely because CLA Assistant uses `pull_request_target`.
- **Canonical OpenAPI path is explicit.** `schemas/openapi.json` is the source-controlled schema artifact; any `Explore.API/swagger.json` language is legacy cleanup only.
- **Privileged workflows get threat models.** Anything with write tokens, `pull_request_target`, deployment secrets, package publication, or attestations must have a narrowly documented trust boundary.
- **Evidence beats aspiration.** Build/test/deploy artifacts, settings evidence, CLA signature storage evidence, and release evidence must be retained and reviewed.
- **No weak compatibility.** Stale workflow names, mutable tags, path-skipped required checks, unverified attestations, and obsolete generated-artifact paths should be removed after branch protection is migrated.

Out of scope for this plan: changing application runtime behavior, adding new API endpoints, or preserving old workflow names after branch protection migration is complete.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Existing workstream has plan/context/tasks but is stale and historical. | `dev/active/enterprise-ci-cd-hardening/*` | High | Old files mix completed PR history with remaining gaps. |
| CI/CD now has a dedicated intent entry. | `.claude/contract/intents.yaml` | High | `ci-cd-change` routes workflow, deployment, dependency, release, and legal contribution governance work. |
| Main fast CI is always present and includes `merge_group`. | `.github/workflows/test.yml` | High | Workflow-internal detection runs fast tests for code changes and intentional no-op pass for docs/schema/ops-only changes. Verify repository-side check names before requiring it. |
| Reusable build/test uses locked restore, NuGet audit, format, build, fast tests, optional integration tests. | `.github/workflows/_build-test.yml` | High | Supports `run-fast-tests` no-op mode. OpenAPI drift moved out; canonical drift remains in `openapi-contract.yml`. |
| `_build-test.yml` no longer checks `Explore.API/swagger.json`. | `.github/workflows/_build-test.yml` and `.github/workflows/openapi-contract.yml` | High | OpenAPI guard owns contract drift. |
| OpenAPI guard exists and includes inventory/client drift plus determinism checks. | `.github/workflows/openapi-contract.yml` | High | Keep, then enhance with `oasdiff` and Spectral after rules stabilize. |
| Canonical OpenAPI artifact is in the root `schemas` folder. | `schemas/openapi.json` | High | Plans and docs must use this path, not `Explore.API/swagger.json`. |
| External actions are SHA pinned with version comments. | `rg uses: .github/workflows` and `.github/scripts/validate-action-pins.py` | High | Workflow Security now enforces full-SHA pins and same-line version comments. |
| No workflow static analysis gate exists for actionlint or zizmor. | `rg actionlint|zizmor .github/workflows` | High | Add these to the dedicated workflow security lane after tool selection/pinning. |
| No CLA workflow exists today. | `find .github/workflows -name '*cla*'` / workflow inventory | High | Add legal contribution gate after legal/security decisions. |
| CLA Assistant GitHub Action repository is archived. | `https://github.com/contributor-assistant/github-action` | High | Use only after risk acceptance, fork/vendor decision, or replacement evaluation. |
| CLA Assistant action uses `pull_request_target`. | `contributor-assistant/github-action` README and GitHub docs | High | Must not checkout or run untrusted PR code in the CLA workflow. |
| Contributor agreements require legal scope decisions. | `https://contributoragreements.org/` | High | Choose CLA/DCO, inbound license model, patent scope, signature formalities, and privacy retention with counsel. |
| Root `.github/CODEOWNERS` exists. | `.github/CODEOWNERS` | Medium | Verify `@islamu-ngo/platform-ops` exists and has write access, or replace it with the actual maintainer owner. |
| Deploy workflows are duplicated for staging and production. | `.github/workflows/deploy-coolify.yml`, `.github/workflows/deploy-coolify-develop.yml` | High | Consolidate around one reusable deploy workflow. |
| Container builds emit digest evidence, Buildx SBOM/provenance, Trivy output, and GitHub artifact attestations. | `.github/workflows/_container-build.yml` | High | Missing deployment-time attestation verification and long-lived evidence export. |
| Coolify digest consumption remains unresolved. | `docs/OPERATIONS.md`, old context | High | This is now a blocker for "best CI/CD." |
| Build baseline passes with warnings. | `dotnet build --configuration Release --verbosity quiet` on 2026-05-29 | High | Latest run passed with package/analyzer warnings. Architecture tests are currently red because unrelated untracked AI integration code defines `AiChatRequest` outside the CQRS query namespace convention. |
| NuGet vulnerability audit is remediation-first and currently clean. | `dotnet list Explore.sln package --vulnerable --include-transitive --format json --output-version 1 --no-restore` on 2026-05-29 | High | `MailKit` was upgraded from `4.15.1` to `4.16.0` for `GHSA-9j88-vvj5-vhgr` / `CVE-2026-41319`; no vulnerable package entries remained after locked restore regeneration. |

### 2.2 Existing Implementation

Workflow inventory:

- `.github/workflows/test.yml` - fast PR/push CI wrapper around `_build-test.yml`.
- `.github/workflows/_build-test.yml` - reusable restore/audit/format/build/test workflow.
- `.github/workflows/openapi-contract.yml` - OpenAPI, contract inventory, NSwag client, and deterministic regeneration guard.
- `.github/workflows/codeql.yml` - CodeQL for Actions, C#, and JavaScript/TypeScript with manual C# build.
- `.github/workflows/dependency-review.yml` - PR dependency review and OpenSSF scorecard display.
- `.github/workflows/workflow-security.yml` - always-present workflow-governance security check for immutable action pins.
- `.github/scripts/validate-action-pins.py` - local validator for external action SHA pins and same-line version comments.
- `.github/workflows/security-tests.yml` - path and schedule driven security integration tests.
- `.github/workflows/cerbos-policy-check.yml` - Cerbos policy validation.
- `.github/workflows/e2e.yml` - manual/nightly Aspire-backed Playwright E2E lane.
- `.github/workflows/_container-build.yml` - reusable image build/push/scan/attestation workflow.
- `.github/workflows/deploy-coolify.yml` and `.github/workflows/deploy-coolify-develop.yml` - duplicated protected deploy workflows.
- `.github/workflows/agent-context.yml` - AI/context governance validation.
- No `.github/workflows/cla.yml` exists yet.
- No `docs/legal/CLA.md`, signature storage policy, or CLA status check is documented yet.

Documentation already exists in:

- `docs/CI_CD_GOVERNANCE.md`
- `docs/OPERATIONS.md`
- `docs/TESTING.md`
- `docs/RELEASE_CHECKLIST.md`
- `docs/TROUBLESHOOTING.md`
- `docs/GOVERNANCE.md`
- `docs/CONFIGURATION.md`

### 2.3 Existing Tests And Verification Coverage

Current verified baseline:

- `dotnet build --configuration Release --verbosity quiet` passed on 2026-05-29 with warnings.
- CI docs define per-project test execution; solution-level `dotnet test` remains forbidden.
- Architecture tests enforce repository rules and docs quality, but no architecture test currently validates GitHub workflow policy depth such as required `merge_group`, actionlint/zizmor presence, or stale OpenAPI paths.

Coverage gaps:

- No checked-in `actionlint`/`zizmor` gate for YAML semantics or injection-prone patterns. A checked-in SHA-pin policy gate now exists.
- No local test proving deploy jobs consume and verify the same digest produced by `_container-build.yml`.
- No CI assertion that docs and workflow artifact names stay aligned.
- No repo-visible proof that branch protection and GitHub Environment settings are actually configured.

### 2.4 Existing Documentation And Contracts

Existing docs already explain many rules, but some need correction:

- `docs/CI_CD_GOVERNANCE.md` correctly describes required/advisory gates and artifact retention.
- `docs/OPERATIONS.md` documents digest-preferred deployment but still allows the unresolved Coolify fallback.
- `docs/CONTRIBUTING.md` now points Blazor client regeneration at `schemas/openapi.json`; canonical contract artifacts are `schemas/openapi.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- `docs/TROUBLESHOOTING.md` explains OpenAPI drift recovery through `schemas/openapi.json` and the generated NSwag client.
- No current contributor legal intake doc explains whether the project uses CLA, DCO, or inbound=outbound under AGPL-3.0.

### 2.5 Current Pain Points / Improvement Areas

1. **Blocker - deployment is not yet digest-promoted.** Images are built and attested, but deploy jobs do not prove Coolify consumed the verified digest.
2. **Critical - attestations are generated but not verified before deploy.** Provenance is evidence, not enforcement, until deploy verifies it.
3. **Critical - workflow definitions lack full lint/security analysis.** SHA-pin enforcement now exists; CI still needs actionlint and zizmor.
4. **Critical - required-check design is still path-filter fragile outside `Build & Test`.** `Build & Test` now has an always-present no-op path; any other globally required workflow must get the same treatment before branch protection requires it.
5. **Major - deploy workflows are duplicated.** Staging/production drift will continue unless deployment logic is one reusable path.
6. **Resolved in first slice - stale OpenAPI checks were removed from the build workflow.** Keep one canonical OpenAPI guard in `openapi-contract.yml`; do not reintroduce dead `swagger.json` checks.
7. **Major - repository settings are documented but not verified.** Branch protection, environment approvals, push protection, and action policies must become an explicit evidence artifact.
8. **Resolved in first slices - dependency vulnerability posture is remediation-first.** Current `MailKit` advisory warnings were remediated by upgrading to the patched version, and the NuGet audit remains blocking for vulnerable direct or transitive packages.
9. **Major - no CODEOWNERS for high-risk operational files.** Workflow, Dockerfile, dependency, and release docs changes need owner review.
10. **Critical - contributor legal governance is missing.** A self-hostable open-source platform needs an explicit contribution agreement posture before broad external contributions scale.
11. **Critical - CLA automation can become a supply-chain/security risk if copied raw.** The sample workflow uses `pull_request_target` and write permissions; it must be isolated from untrusted PR code and pinned to an immutable SHA.

### 2.6 Unknowns After Investigation

- Whether Coolify can deploy `image@sha256:<digest>` through the current webhook/app model.
- Whether ATCR supports OIDC or equivalent short-lived credentials; current workflow still uses `ATCR_PASSWORD`.
- Whether GitHub repository/org settings enforce SHA-pinned actions, allowed actions, secret scanning, push protection, dependency graph, CodeQL, branch rulesets, and protected environments.
- Whether `docker/build-push-action@v7` and related Docker actions should be adopted immediately or through Dependabot after a controlled workflow-lint PR.
- Whether OpenAPI `oasdiff` should fail immediately for pre-v1 breaking changes or remain advisory until API versioning and skipped contract tests are stabilized.
- Whether the project wants CLA, DCO, or both; this requires legal/product governance, not only CI YAML.
- Whether CLA signatures should live in the same repository, a dedicated private signatures repository, or a dedicated unprotected branch. The action README says the write branch should not be protected; do not use protected source branches for signature writes.
- Whether using the archived `contributor-assistant/github-action` is acceptable, or whether the repository should fork/vendor it or use another maintained CLA/DCO solution.

## 3. Proposed Future State

The CI/CD architecture should become a promotion pipeline, not a collection of branch-triggered scripts:

```text
PR / merge queue
  -> CLA/DCO legal contribution gate
  -> workflow definition gate (actionlint + zizmor + pin policy)
  -> fast build/test + architecture + component + BFF tests
  -> OpenAPI deterministic drift + contract inventory + generated client
  -> dependency/license/security gates
  -> optional integration/runtime evidence

trusted main/develop/tag
  -> build container once
  -> scan image and dependencies
  -> attach/export SBOM + provenance
  -> generate GitHub artifact attestation
  -> verify attestation and digest
  -> deploy exact digest to staging/production
  -> smoke `/alive` and `/health`
  -> retain deploy evidence and release evidence
```

## 4. Non-Negotiable Constraints

- Required checks must be always-present; path-skipped required workflows are not acceptable.
- Deployment secrets must be environment-scoped and unavailable to untrusted PRs.
- `pull_request_target` is banned for build/test/generation/deployment unless a threat-model document approves the exact pattern.
- Workflows must use least-privilege `permissions`; jobs elevate only where needed.
- External actions must remain pinned to full-length SHAs with update automation.
- Generated OpenAPI and NSwag artifacts are never hand-edited; `schemas/openapi.json` is the canonical checked-in OpenAPI artifact.
- CLA automation may use `pull_request_target` only for metadata/comment/status work. It must not checkout, build, test, cache, or execute untrusted PR code.
- CLA signatures must not be written to protected source branches. Prefer a dedicated private signatures repository or a dedicated unprotected signatures branch with narrow write credentials.
- Container deployment must use an immutable digest or an explicitly documented temporary immutable-tag fallback with resolved digest evidence.
- Production deploys require GitHub Environment approval, branch restrictions, and retained evidence.
- CI must continue to run tests per project, not solution-level `dotnet test`.
- New or updated files must follow repository ABOUTME and docs metadata conventions where applicable.

## 5. Architecture And Design Decisions

### Decision 1 - Keep GitHub Actions, But Treat It As A Controlled Platform

**Why:** The repository already has a mature GitHub Actions base and docs. Replacing the platform would add complexity without solving the current gaps.

**Consequences:** Add linting, CODEOWNERS, settings verification, reusable workflows, and promotion evidence around the existing platform.

### Decision 2 - Promote Digests, Not Branches Or Tags

**Why:** Enterprise deployment must prove production runs the artifact that passed scan and attestation checks.

**Consequences:** `_container-build.yml` must publish digest outputs; deploy workflows must accept digest inputs and verify GitHub attestation before invoking Coolify.

### Decision 3 - Workflow Security Is A Required Gate

**Why:** Workflow YAML is privileged code. A compromised workflow can exfiltrate secrets or change deployments.

**Consequences:** Add actionlint for syntax/semantic checks, zizmor for GitHub Actions security issues, and CODEOWNERS for `.github/**`.

### Decision 4 - One Deploy Path

**Why:** Separate staging and production deploy scripts already duplicate several hundred lines. That is an operational drift risk.

**Consequences:** Create one reusable deploy workflow with environment, component, digest, webhook, smoke URL, and promotion inputs.

### Decision 5 - Repository Settings Are Deliverables

**Why:** Branch protection, environment approvals, action policies, and secret scanning cannot be fully encoded in YAML.

**Consequences:** Add a repository-settings evidence artifact and make release readiness require current screenshots/API output or a maintainer-checked settings manifest.

### Decision 6 - Add A Hardened CLA/DCO Contribution Gate

**Why:** ISLAMU Event is AGPL-3.0 and intended to be open-source and self-hostable. Before external contribution volume grows, the project needs a clear inbound contribution policy and CI evidence that every PR satisfies it.

**Alternatives considered:** No legal gate; DCO-only; hosted CLA Assistant; CLA Assistant GitHub Action; custom GitHub App. No gate is weak for enterprise governance. DCO-only is simpler but may not meet desired patent/assignment/license requirements. The requested CLA Assistant action is convenient but archived and privileged, so it needs a threat model.

**Consequences:** Add legal docs, PR template updates, CLA status check, contributor allowlist governance, signature storage policy, and privacy/retention rules. Implementation must pin the action by SHA, avoid wildcard allowlists such as `bot*`, and avoid running untrusted PR code.

## 6. Implementation Phases

### Phase 0 - Contract And Repository Settings Baseline

**Goal:** Make CI/CD a first-class repository change type before more YAML churn.

**Files:**

- `.claude/contract/intents.yaml`
- `docs/legal/CLA.md` or `docs/legal/DCO.md` if the legal decision is made in this phase
- `docs/CI_CD_GOVERNANCE.md`
- `docs/RELEASE_CHECKLIST.md`
- `.github/CODEOWNERS`
- `dev/active/enterprise-ci-cd-hardening/*`

**Tasks:**

- Add a `ci-cd-change` intent covering `.github/**`, `.github/dependabot.yml`, Dockerfiles, deployment docs, and release governance docs.
- Add `.github/CODEOWNERS` for `.github/**`, `Dockerfile`, `*.csproj`, `Directory.Packages.props`, `global.json`, `docs/CI_CD_GOVERNANCE.md`, `docs/RELEASE_CHECKLIST.md`, and `docs/OPERATIONS.md`.
- Create a repository settings checklist with required checks, merge queue, environments, action policy, secret scanning, push protection, dependency graph, CodeQL, and Dependabot security updates.
- Decide required check names before workflow renames.
- Add a legal contribution governance decision record: CLA vs DCO vs both, inbound license scope, patent language, signature storage, privacy retention, and approval owner.

**Exit criteria:**

- Future CI/CD changes have an explicit Contribution Contract entry.
- CODEOWNERS protects privileged files.
- Repository settings are no longer only tribal knowledge.

### Phase 1 - Fix Current Pipeline Defects Before Adding New Gates

**Goal:** Remove known stale checks and make required-check behavior safe.

**Files:**

- `.github/workflows/test.yml`
- `.github/workflows/_build-test.yml`
- `.github/workflows/openapi-contract.yml`
- `docs/CONTRIBUTING.md`
- `docs/CI_CD_GOVERNANCE.md`

**Tasks:**

- Remove or replace any legacy `_build-test.yml` OpenAPI drift check that targets `Explore.API/swagger.json`; only `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` belong in generated-contract drift enforcement.
- Ensure `test.yml` has `merge_group` before it can be required by merge queue.
- Replace path-skipped required checks with always-running wrappers or internal no-op jobs.
- Correct docs that still reference `swagger.json` as the Blazor generated-client source.
- Keep the NuGet vulnerability gate blocking. Current advisories were remediated; future temporary exceptions require owner/date/advisory/removal-condition evidence before weakening CI.

**Exit criteria:**

- No dead OpenAPI guard remains.
- Required checks cannot get stuck as skipped/pending.
- Docs name the same canonical artifacts as CI.

### Phase 2 - Contributor Legal Gate (CLA/DCO)

**Goal:** Add contribution legal provenance without creating a privileged `pull_request_target` security hole.

**Files:**

- `.github/workflows/cla.yml`
- `docs/legal/CLA.md` and/or `docs/legal/DCO.md`
- `docs/CONTRIBUTING.md`
- `.github/PULL_REQUEST_TEMPLATE.md`
- `docs/CI_CD_GOVERNANCE.md`
- `docs/RELEASE_CHECKLIST.md`

**Required research before implementation:**

- `https://contributoragreements.org/`
- `https://contributoragreements.org/legal.html`
- `https://contributoragreements.org/agreement-chooser.html`
- `https://cla-assistant.io/`
- `https://github.com/contributor-assistant/github-action`
- GitHub `pull_request_target` and secure-use documentation.

**Tasks:**

- Decide with the project owner/legal reviewer whether ISLAMU Event uses:
  - CLA only;
  - DCO only;
  - both CLA and DCO;
  - inbound=outbound with no separate contributor agreement.
- Draft or approve the actual agreement text before enabling the gate. Do not point production contributors at SAP's sample CLA.
- If using CLA Assistant GitHub Action:
  - add `.github/workflows/cla.yml`;
  - pin `contributor-assistant/github-action` to a full commit SHA, not `@v2.6.1`;
  - record that the upstream repository is archived and either accept, fork/vendor, or replace it;
  - use `pull_request_target` only for PR metadata, status, and comments;
  - do not checkout, build, test, cache, or execute PR head code in this workflow;
  - scope permissions to the smallest viable set;
  - prefer remote private signature storage or a dedicated signature branch over writing to protected source branches;
  - avoid broad allowlists such as `bot*`; explicitly allow trusted bots like `dependabot[bot]`, `github-actions[bot]`, and any Renovate bot if configured;
  - document whether a fine-grained PAT, GitHub App token, or `GITHUB_TOKEN` is used and what it can write.
- Add branch protection requirement for the CLA status check after the workflow is stable.
- Add PR template language explaining the CLA/DCO requirement and signing comment.
- Add a privacy/retention note for contributor signature metadata.

**Hardened workflow shape to design toward:**

```yaml
name: "CLA Assistant"

on:
  issue_comment:
    types: [created]
  pull_request_target:
    types: [opened, synchronize, reopened]

permissions:
  contents: read # raise only if same-repo signature writes are explicitly approved
  pull-requests: write
  statuses: write

jobs:
  cla-assistant:
    runs-on: ubuntu-latest
    steps:
      - name: "CLA Assistant"
        if: >
          github.event_name == 'pull_request_target' ||
          github.event.comment.body == 'recheck' ||
          github.event.comment.body == 'I have read the CLA Document and I hereby sign the CLA'
        uses: contributor-assistant/github-action@<full-commit-sha-or-approved-fork>
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          path-to-document: 'https://github.com/<owner>/<repo>/blob/main/docs/legal/CLA.md'
          path-to-signatures: 'signatures/version1/cla.json'
          branch: 'cla-signatures'
          allowlist: 'dependabot[bot],github-actions[bot]'
```

Do not copy the upstream sample blindly. The sample uses `@v2.6.1`, broad write permissions, `branch: main`, and `bot*` style allowlists. This repository should pin to an immutable SHA, avoid protected source branches for signature writes, and keep the workflow metadata-only.

**Exit criteria:**

- Contributor legal status is visible as a PR check.
- The workflow cannot run untrusted PR code with write credentials.
- Signature storage is auditable and not mixed into protected source branch changes.
- Legal docs, PR template, release checklist, and governance docs agree.

### Phase 3 - Workflow Quality And Supply-Chain Guard

**Goal:** Treat workflow changes like security-sensitive code changes.

**Files:**

- `.github/workflows/workflow-security.yml` (new)
- `.github/workflows/*.yml`
- `.github/dependabot.yml`
- `docs/CI_CD_GOVERNANCE.md`

**Tasks:**

- Add `actionlint` with a pinned installer or pinned container/action.
- Add `zizmor` in auditor mode initially as SARIF/advisory, then promote high-confidence findings to blocking.
- Add a pin-policy check that fails external non-SHA `uses:` references.
- Keep local reusable workflows path-based.
- Configure Dependabot grouping/review rules so SHA pin updates remain maintainable.
- Add OpenSSF Scorecard as a scheduled/SARIF evidence lane if repository visibility and permissions support it.
- Add `gitleaks` or an equivalent local secret-scanning lane for PR feedback, while keeping GitHub secret scanning and push protection as repository settings.
- Add `pinact` or a custom policy check if it gives cleaner enforcement for SHA-pinned actions than ad hoc shell parsing.

**Exit criteria:**

- Workflow YAML changes cannot merge with syntax errors, dangerous contexts, or unpinned third-party actions.
- Security findings are retained in code scanning or artifacts.

### Phase 4 - Build/Test Evidence, Coverage, License, And Dependency Integrity

**Goal:** Move from "tests ran" to actionable quality evidence.

**Files:**

- `.github/workflows/_build-test.yml`
- `.github/workflows/test.yml`
- `docs/TESTING.md`
- `docs/CI_CD_GOVERNANCE.md`

**Tasks:**

- Keep TRX artifacts and job summaries.
- Add coverage collection only after stable test lanes are confirmed; choose one provider or keep coverage as artifact-only.
- Add a warnings budget or analyzer report artifact before making warnings-as-errors broad.
- Keep the current all-findings NuGet audit blocking; only split by severity/dependency type later if documented false-positive or ecosystem-noise evidence justifies it.
- Ensure integration tests run for deploy callers and on a reliable schedule.
- Add license policy scanning for NuGet/npm/container dependencies and document AGPL-compatible allowed/denied licenses.
- Add cache-poisoning controls: fork PRs should not write privileged caches consumed by trusted deploy/publish workflows.

**Exit criteria:**

- Every failed gate gives maintainers enough evidence to triage from GitHub Actions.
- Dependency risk is policy-driven, not accidental.

### Phase 5 - OpenAPI Contract Guard V2

**Goal:** Keep contract drift deterministic and add controlled breaking-change intelligence.

**Files:**

- `.github/workflows/openapi-contract.yml`
- `docs/API_CHANGELOG.md`
- `docs/API_CONTRACT_INVENTORY.md`
- `docs/TROUBLESHOOTING.md`
- `docs/GOVERNANCE.md`

**Tasks:**

- Keep deterministic regeneration for the canonical contract set: `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- Add `oasdiff` against the base branch as advisory evidence first.
- Add Spectral only after the local OpenAPI rules are documented and low-noise.
- Promote breaking-change detection to blocking once skipped/stale API contract tests are resolved.
- Require API changelog evidence for intentional breaking changes.

**Exit criteria:**

- Stale generated artifacts block.
- Breaking-change reports are visible and can become blocking without redesign.

### Phase 6 - Container Build, SBOM, Provenance, And Attestation Verification

**Goal:** Build once, prove what was built, and verify before deployment.

**Files:**

- `.github/workflows/_container-build.yml`
- `.github/workflows/deploy-*.yml` or new reusable deploy workflow
- `Explore.API/Dockerfile`
- `Explore.Blazor/Dockerfile`
- `docs/OPERATIONS.md`
- `docs/RELEASE_CHECKLIST.md`

**Tasks:**

- Update Docker actions through Dependabot or a controlled PR, then pin resulting SHAs.
- Export SBOM/provenance evidence as downloadable artifacts, not only registry-attached metadata.
- Add Trivy SARIF/code-scanning output in addition to text artifacts where supported.
- Pin base images by digest or document a digest update policy.
- Verify GitHub artifact attestations with `gh attestation verify` or equivalent before deployment.
- Add SLSA provenance verification or document why GitHub artifact attestation is the chosen SLSA-compatible evidence path.
- Add release artifact integrity manifests with checksums for evidence bundles.
- Record final image digest per component as the release/deploy source of truth.

**Exit criteria:**

- Deployment cannot start unless image scan and attestation verification pass.
- Release evidence contains digests, SBOM/provenance references, vulnerability output, and build metadata.

### Phase 7 - Unified Digest-Based Deploy Promotion

**Goal:** Replace branch-triggered deployment scripts with one promotion path.

**Files:**

- `.github/workflows/deploy.yml` (new or replacement)
- `.github/workflows/_deploy-coolify.yml` (optional reusable workflow)
- `.github/workflows/deploy-coolify.yml`
- `.github/workflows/deploy-coolify-develop.yml`
- `docs/CONFIGURATION.md`
- `docs/OPERATIONS.md`

**Tasks:**

- Consolidate staging and production deploy logic into one reusable workflow.
- Pass environment, component, digest, smoke URLs, and webhook secret names as explicit inputs.
- Confirm Coolify digest support. If unsupported, configure immutable commit-SHA tag deployment and record resolved digest after deployment.
- Make production smoke checks mandatory when production URL variables exist.
- Keep staging auto-deploy optional; production must require environment approval.
- Prefer OIDC/short-lived credentials where the registry or deploy target supports it; otherwise document token scope and rotation.
- Add deployment freeze/manual override policy with audit notes for urgent security releases.

**Exit criteria:**

- One deploy implementation serves staging and production.
- Production deploy evidence proves which digest was deployed and whether health checks passed.

### Phase 8 - Repository And Organization Policy Enforcement

**Goal:** Make out-of-repo controls auditable.

**Files:**

- `docs/CI_CD_GOVERNANCE.md`
- `docs/RELEASE_CHECKLIST.md`
- `dev/active/enterprise-ci-cd-hardening/enterprise-ci-cd-hardening-context.md`

**Tasks:**

- Verify branch/ruleset protection for `main` and `develop`.
- Verify required checks match current workflow/job names.
- Verify GitHub Environments: `staging`, `production`, reviewers, branch restrictions, wait timers if used, and environment secrets.
- Verify GitHub Actions policy allows only GitHub-owned, verified, or SHA-pinned actions as appropriate.
- Verify secret scanning, push protection, dependency graph, Dependabot security updates, and CodeQL alerts.
- Store a redacted settings evidence note in this workstream or release checklist.
- Add a scheduled repository-settings drift check if GitHub API permissions allow it; otherwise require manual evidence before releases.

**Exit criteria:**

- Maintainers can prove YAML and GitHub settings agree.
- Release cannot claim enterprise CI/CD until settings evidence is current.

### Phase 9 - Runtime, E2E, Performance, And Release Evidence Maturity

**Goal:** Preserve fast PR feedback while measuring runtime reliability.

**Files:**

- `.github/workflows/e2e.yml`
- `.github/workflows/security-tests.yml`
- `.github/workflows/cerbos-policy-check.yml`
- `docs/TESTING.md`
- `docs/RELEASE_CHECKLIST.md`

**Tasks:**

- Keep E2E manual/nightly until reliability data supports promotion.
- Add trend summaries for E2E/security/runtime failures.
- Add scheduled OpenAPI breaking-change reports.
- Add scheduled performance/benchmark smoke lanes for endpoints and pages that represent real operator risk; keep benchmarks advisory until stable.
- Add flaky-test tracking with owner, first-seen date, and promotion/removal criteria.
- Ensure release notes link or copy long-lived evidence because GitHub artifacts expire.

**Exit criteria:**

- Nightly failures are actionable, not noise.
- Release evidence survives beyond short artifact retention windows.

### Phase 10 - Release Automation, Compliance, And Maintainer Experience

**Goal:** Turn CI/CD evidence into a repeatable release process instead of a manual artifact hunt.

**Files:**

- `.github/workflows/release.yml`
- `.github/workflows/release-drafter.yml` or equivalent if chosen
- `docs/RELEASE_CHECKLIST.md`
- `docs/CI_CD_GOVERNANCE.md`
- `docs/CONTRIBUTING.md`

**Tasks:**

- Decide release model: manual tags, GitHub Releases, Release Drafter, semantic versioning, or conventional commits.
- Generate a release evidence bundle containing commit SHA, image digests, SBOM/provenance references, attestations, scans, OpenAPI diff, test summary, CLA gate status, and deployment smoke results.
- Attach long-lived evidence or links to GitHub Releases because workflow artifacts expire.
- Add changelog/release-note checks for security, migrations, config, OpenAPI, and operator-impact changes.
- Add maintainer runbooks for re-running failed gates without bypassing controls.

**Exit criteria:**

- A release can be audited from GitHub Release notes and durable evidence without searching expired workflow artifacts.
- Maintainers have documented override and rerun paths with approval requirements.

## 7. Testing Strategy

Minimum verification for plan/document changes:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
git diff --check
```

Minimum verification for workflow implementation PRs:

- Run `actionlint` against `.github/workflows`.
- Run `zizmor` against `.github/workflows`.
- For `.github/workflows/cla.yml`, verify the workflow never checks out or executes untrusted PR code and that the action is SHA-pinned.
- Run the affected workflow manually or through a controlled PR.
- For OpenAPI changes, run `openapi-contract.yml` or local equivalent twice and confirm zero second-run diff.
- For deploy changes, use staging first and retain digest/smoke evidence.

## 8. Documentation, Configuration, And Operations Impact

Docs that must stay aligned:

- `docs/CI_CD_GOVERNANCE.md`
- `docs/OPERATIONS.md`
- `docs/RELEASE_CHECKLIST.md`
- `docs/TESTING.md`
- `docs/TROUBLESHOOTING.md`
- `docs/CONFIGURATION.md`
- `docs/CONTRIBUTING.md`
- `docs/legal/CLA.md` or `docs/legal/DCO.md` once selected
- `.github/PULL_REQUEST_TEMPLATE.md`

Configuration impact:

- GitHub Environment secrets remain deployment-only, not runtime app settings.
- Coolify webhook names may be normalized only in the same PR that updates docs and workflows.
- Any OIDC migration must document audience, subject claims, token scope, and fallback/rollback.
- CLA signature storage must document repository/branch, token permissions, retention, bot allowlist, and privacy impact.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Fork PRs remain untrusted and must not access secrets, OIDC tokens, package write scopes, deployment webhooks, or environment secrets.
- `pull_request_target` is allowed only for the CLA metadata/status workflow after a documented threat model. It remains banned for build/test/generation/deployment jobs.
- Inline scripts must treat PR/user-controlled values as untrusted and pass them through environment variables, not direct interpolation.
- Use short-lived credentials through OIDC where available.
- Do not log webhook URLs, bearer tokens, registry credentials, image signing material, or smoke-check response bodies containing secrets.
- Artifact names and summaries may include commit SHA, image digest, component name, environment, and health status, but not secrets.
- CLA signature artifacts may include contributor identity metadata; treat them as governance records with explicit privacy and retention rules.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

This workstream does not directly change tenant data paths. It still protects product integrity because CI gates are the enforcement path for:

- tenant isolation tests;
- authorization parity and Cerbos fail-closed behavior;
- OpenAPI and HAL contract drift;
- BFF trust-boundary tests;
- accessibility convention tests;
- release evidence for self-hosters.

## 11. Observability And Operations

CI/CD must produce operator-grade evidence:

- Job summaries for fast tests, OpenAPI drift, container builds, and deploys.
- Artifacts for TRX, OpenAPI diffs, CLA status, license scans, security scans, SBOM/provenance, Trivy outputs, E2E traces, and deployment smoke logs.
- Release notes must copy or link long-lived evidence outside expiring GitHub artifacts when required.
- Deploy summaries must include environment, component, commit SHA, digest, workflow run, smoke result, and rollback note.

## 12. Migration And Compatibility Plan

Backward compatibility with weak CI/CD behavior is intentionally not preserved.

Delete or replace:

- legacy `Explore.API/swagger.json` checks or docs references; `schemas/openapi.json` is canonical;
- duplicated deploy workflow bodies;
- path-skipped required check designs without no-op wrappers;
- mutable deployment source-of-truth tags;
- unmanaged action references;
- docs that describe obsolete generated-artifact paths.
- raw CLA sample workflow choices that write signatures to protected source branches or use broad bot allowlists.

Migration sequencing:

1. Add settings/CODEOWNERS/intent baseline.
2. Fix stale checks and required-check semantics.
3. Add CLA/DCO contribution governance.
4. Add workflow lint/security gates.
5. Consolidate deploy and enforce digest promotion.
6. Tighten advisory gates after reliability evidence exists.

## 13. Risk Register

| Risk | Severity | Mitigation |
|---|---|---|
| Coolify cannot deploy by digest | Blocker for final state | Use immutable commit-SHA tag fallback temporarily; record resolved digest; keep task open until digest deploy is solved. |
| Workflow security scanner false positives | Major | Start advisory/SARIF, baseline findings, then block only high-confidence rules. |
| Required checks stuck pending due skipped workflows | Critical | Always-running wrapper/no-op jobs before branch protection requires them. |
| Package vulnerability gate regresses | Critical | Keep remediation-first policy; any temporary advisory exception needs owner/date/advisory URL/affected version/compensating control/removal condition. |
| Duplicate deploy workflows drift | Major | Consolidate into one reusable deploy path. |
| Attestation generated but never verified | Critical | Verify attestation before deploy and include result in evidence. |
| Repository settings cannot be changed by code | Major | Add a settings evidence checklist and require maintainer verification. |
| CLA action uses privileged `pull_request_target` | Critical | Use only for metadata/status, no PR-code checkout/execution, SHA pin the action, and document the threat model. |
| CLA Assistant action is archived | Major | Accept risk explicitly, fork/vendor, or select a maintained alternative before enforcement. |
| License compliance is not checked | Major | Add dependency license policy scanning and AGPL-compatible allow/deny rules. |

## 14. Success Metrics And Definition Of Done

The workstream is done when:

- A `ci-cd-change` intent exists and names required docs/rules/tests.
- CODEOWNERS protects CI/CD and release-critical files.
- CLA/DCO contribution policy is decided, documented, and enforced by a safe required check.
- Every required check is always-present or has a no-op pass path.
- `test.yml` supports merge queue if merge queue is enabled.
- actionlint and zizmor run on workflow changes.
- OpenAPI drift guard remains deterministic and blocks stale generated artifacts.
- Container images have exported SBOM/provenance, vulnerability scan output, and GitHub attestations.
- Deploy jobs verify image attestation/digest before invoking Coolify.
- Production deploys are environment-protected and smoke-checked.
- Dependency license policy and vulnerability policy are explicit and enforced at the selected severity.
- Repository settings evidence confirms branch protection, environments, action policy, secret scanning, push protection, dependency graph, Dependabot security updates, and CodeQL.
- Release evidence bundles include CLA status, OpenAPI drift, test results, image digests, SBOM/provenance, attestations, scan output, and smoke checks.
- Docs explain how contributors fix every CI/CD failure class.

## 15. Implementation Agent Contract - Keep Dev Docs Current

Every implementation PR must update:

- this plan when architecture or sequencing changes;
- `enterprise-ci-cd-hardening-context.md` after major decisions, verification, blockers, or settings evidence;
- `enterprise-ci-cd-hardening-tasks.md` as tasks are completed or split.

Do not mark this workstream complete while Coolify digest deployment, attestation verification, repository settings evidence, and workflow security linting remain unresolved.

## 16. Progress Reporting Contract

Implementation agents should report:

- phase being implemented;
- files changed;
- gates added/removed/promoted;
- exact verification commands and results;
- any repository setting that must be changed manually;
- any advisory gate intentionally left non-blocking, with owner/date/removal condition.

## 17. External Research Sources

- GitHub Actions secure use: https://docs.github.com/en/actions/reference/security/secure-use
- GitHub `pull_request_target` warning: https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#pull_request_target
- GitHub OIDC reference: https://docs.github.com/en/actions/reference/security/oidc
- GitHub environments/deployments: https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments
- GitHub artifact attestations: https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations
- GitHub dependency review action: https://docs.github.com/en/code-security/how-tos/secure-your-supply-chain/manage-your-dependency-security/configuring-the-dependency-review-action
- Docker Buildx GitHub Actions and attestations: https://docs.docker.com/build/ci/github-actions/ and https://docs.docker.com/build/ci/github-actions/attestations/
- zizmor GitHub Actions security scanner: https://docs.zizmor.sh/
- OpenSSF Scorecard: https://scorecard.dev/
- Contributor Agreements: https://contributoragreements.org/
- Contributor Agreements legal questions: https://contributoragreements.org/legal.html
- Contributor Agreement chooser: https://contributoragreements.org/agreement-chooser.html
- CLA Assistant hosted service: https://cla-assistant.io/
- CLA Assistant GitHub Action: https://github.com/contributor-assistant/github-action

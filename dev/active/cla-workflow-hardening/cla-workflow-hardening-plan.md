<!-- ABOUTME: Implementation plan for CLA workflow hardening with pre-flight optimization, isolated signature branch, root CLA.md, and observability. -->
<!-- ABOUTME: Tier 3 (domain_state) CI/CD infrastructure change adopting best practices from oh-my-openagent comparison. -->

# CLA Workflow Hardening — Implementation Plan

Last Updated: 2026-08-25 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Adopt all CLA improvements identified in the oh-my-openagent comparative analysis: root `CLA.md`, dedicated `cla-signatures` branch, pre-flight `actions/github-script` check, expanded bot allowlist (`web-flow`, wildcard `*[bot]`), multiline YAML PR comments, and `$GITHUB_STEP_SUMMARY` observability step.
- **Task directory:** `dev/active/cla-workflow-hardening/`
- **Planning status:** Implemented — local commit complete; deployment prerequisite pending
- **Matched intents:** `ci-cd-change` (Tier 3 domain_state), `ip-clean-room-governance` (Tier 1 security — applicable only for legal governance docs, not for clean-room code provenance)
- **Relevant skills:** `implementation-plan`, `conventional-commit`, `i-vsd`
- **Relevant rules:** CI/CD governance, contribution governance, CLA legal document
- **Primary layers:** DevOps (GitHub Actions), Documentation, Legal
- **Complexity:** **S** — No application code changes. All edits are YAML workflow, Markdown documentation, and git branch operations. Six files modified, one file created.
- **I-VSD Document:** [`islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md`](../../../islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md)
- **Grill-Me Intake:** Not required (Tier 3 task). User explicitly specified all decisions: adopt all oh-my-openagent improvements, no backward compatibility constraints ("we are in development mode").

## 1. Executive Summary

The current CLA workflow runs `contributor-assistant/github-action` unconditionally on every PR event, stores signatures on the active `develop` branch (polluting the code commit log), lacks observability in GitHub Actions summaries, uses a narrow bot allowlist that misses `web-flow` and wildcard bot patterns, uses inline HTML `<br>` in PR comment strings, and has no root-level `CLA.md` for discoverability.

This plan upgrades the CLA infrastructure by adopting five improvements inspired by the oh-my-openagent repository while retaining ISLAMU Event's superior security posture (SHA-pinned actions, `zizmor` annotations, concurrency control, timeout, versioned signature paths, least-privilege permissions).

**Non-goals:**
- CLA legal text changes (the `legal/CLA.md` content is out of scope)
- Application code changes
- Branch protection rule changes (deferred until workflow is stable)

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context (Blast Radius)

```yaml
# Injected Structural Context (Pre-Flight Blast Radius)
Target: .github/workflows/cla.yml
Callers (Upstream):
  - GitHub Events: pull_request_target, issue_comment
Callees (Downstream):
  - contributor-assistant/github-action@ca4a40a7... (v2.6.1)
  - signatures/v1.0/cla.json on develop branch
Impacted Flows:
  - Flow: Contributor CLA Signing (Criticality: Tier 3)
Test Coverage:
  - None (workflow-only, no application tests)
Documentation Blast Radius:
  - README.md (2 references to signatures/v1.0/cla.json on develop)
  - CONTRIBUTING.md (1 reference to signatures/v1.0/cla.json on develop)
  - docs/CONTRIBUTING.md (1 reference to signatures/v1.0/cla.json on develop)
  - docs/CI_CD_GOVERNANCE.md (1 reference to signatures/v1.0/cla.json on develop)
  - docs/legal/CONTRIBUTION_GOVERNANCE.md (2 references to signatures/v1.0/cla.json on develop)
  - legal/CLA.md (2 references to signatures/v1.0/cla.json on develop)
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| CLA workflow exists | Verified: `.github/workflows/cla.yml` (44 lines) | High | |
| CLA document exists | Verified: `legal/CLA.md` (143 lines) | High | |
| No root `CLA.md` | Verified: `find` returned 0 results for `CLA.md` in repo root | High | Only `legal/CLA.md` exists |
| Signatures stored on `develop` | Verified: `cla.yml` line 34: `branch: "develop"` | High | |
| No `cla-signatures` branch exists locally | Verified by `find_by_name` and file listing | High | Will need to be created |
| No pre-flight script | Verified: `cla.yml` has single step only | High | |
| Bot allowlist is `dependabot[bot],github-actions[bot]` | Verified: `cla.yml` line 35 | High | Missing `web-flow`, wildcard `*[bot]` |
| No `$GITHUB_STEP_SUMMARY` | Verified: `cla.yml` has no summary step | High | |
| PR comment uses inline `<br>` HTML | Verified: `cla.yml` line 36 | High | |
| Signature path is versioned `v1.0` | Verified: `cla.yml` line 32 | High | Superior to oh-my-openagent; retain |
| Action is SHA-pinned | Verified: `cla.yml` line 25 | High | Superior to oh-my-openagent; retain |
| Concurrency & timeout present | Verified: `cla.yml` lines 14-16, 21 | High | Superior to oh-my-openagent; retain |
| Permissions are least-privilege | Verified: `cla.yml` lines 9-13, no `actions: write` | High | Superior to oh-my-openagent; retain |
| 7 files reference old storage model | Verified by `grep_search` for `signatures/v1.0` + `develop` | High | All listed in blast radius |

### 2.2 Existing Implementation

**Workflow (`.github/workflows/cla.yml`):**
- Single-step workflow using `contributor-assistant/github-action@ca4a40a7...` (v2.6.1)
- Triggers: `pull_request_target` (opened, edited, synchronize, reopened, closed, ready_for_review) and `issue_comment` (created)
- Permissions: `contents: write`, `issues: write`, `pull-requests: write`, `statuses: write`
- Concurrency group per PR/issue number with cancel-in-progress
- 10-minute timeout
- SHA-pinned with `zizmor` ignore annotations
- Stores signatures at `signatures/v1.0/cla.json` on `develop` branch
- Bot allowlist: `dependabot[bot],github-actions[bot]`

**Legal document (`legal/CLA.md`):**
- 143-line comprehensive CLA covering copyright, patent, moral rights, privacy, third-party materials, AGPL dual-licensing, trustee-to-ASBL transfer
- References workflow storage model in "How to Sign" section

**Governance (`docs/legal/CONTRIBUTION_GOVERNANCE.md`):**
- 121-line decision record covering research, threat model, bot allowlist policy, `pull_request_target` security requirements

### 2.3 Existing Tests And Verification Coverage

No application-level tests cover the CLA workflow (it is a metadata-only GitHub Actions workflow). Verification is manual: PR opening triggers the bot, contributor posts comment, bot records signature.

### 2.4 Existing Documentation And Contracts

Files referencing the current CLA storage model (`signatures/v1.0/cla.json` on `develop`):

1. [`README.md`](../../README.md) — Lines 182, 330
2. [`CONTRIBUTING.md`](../../CONTRIBUTING.md) — Section "Contributor Legal Status & CLA"
3. [`docs/CONTRIBUTING.md`](../../docs/CONTRIBUTING.md) — Line 71
4. [`docs/CI_CD_GOVERNANCE.md`](../../docs/CI_CD_GOVERNANCE.md) — Line 236
5. [`docs/legal/CONTRIBUTION_GOVERNANCE.md`](../../docs/legal/CONTRIBUTION_GOVERNANCE.md) — Lines 22, 107
6. [`legal/CLA.md`](../../legal/CLA.md) — Lines 132, 134
7. [`.github/PULL_REQUEST_TEMPLATE.md`](../../.github/PULL_REQUEST_TEMPLATE.md) — No direct storage reference (only links CLA document)

### 2.5 Current Pain Points / Improvement Areas

1. **No root `CLA.md`:** Contributors, GitHub community health tools, and compliance scanners cannot find the CLA at the standard root location.
2. **Develop branch pollution:** Every CLA signature creates a `chore(cla):` commit on `develop`, mixing legal metadata with code commits.
3. **Unconditional action execution:** Full `contributor-assistant/github-action` runs on every `synchronize` event even when all contributors have already signed, wasting runner minutes and API quota.
4. **Narrow bot allowlist:** Missing `web-flow` (GitHub Web UI commits) and wildcard `*[bot]` causes false CLA re-prompts for legitimate automated contributors.
5. **No job summary:** CLA check results are only visible in workflow logs, not in the GitHub Actions Summary tab.
6. **Inline HTML in PR comments:** Single-line string with `<br>` tags is hard to read and maintain in YAML.

### 2.6 Unknowns After Investigation

| Unknown | Searched | Resolution |
|---|---|---|
| Whether `cla-signatures` orphan branch exists on remote | Local branches only checked | Task 1.1 will create it |
| Whether existing signatures need migration | No existing `signatures/` directory in tree | No migration needed — no signatures have been recorded yet (development mode) |

## 3. Proposed Future State

After implementation:

1. **Root `CLA.md`** at repository root links to `legal/CLA.md` for discoverability.
2. **Dedicated `cla-signatures` orphan branch** isolates legal signature JSON from code commits on `develop`.
3. **Two-stage workflow** with a pre-flight `actions/github-script` step that short-circuits when all commit authors have already signed or are allowlisted, skipping the full `contributor-assistant/github-action` run.
4. **Expanded bot allowlist** including `web-flow` and wildcard `*[bot]` to prevent false CLA re-prompts.
5. **Multiline YAML block scalar** (`|`) for PR comment strings instead of inline HTML `<br>`.
6. **`$GITHUB_STEP_SUMMARY`** step providing observability in the GitHub Actions Summary tab.
7. **All existing security hardening retained**: SHA-pinned actions, `zizmor` annotations, concurrency, timeout, least-privilege permissions, versioned signature path (`signatures/v1.0/cla.json`).
8. **All documentation updated** to reference the new `cla-signatures` branch instead of `develop` for signature storage.

## 4. Non-Negotiable Constraints

From the matched `ci-cd-change` intent:

- `pull_request_target` is allowed only for metadata-only legal/status workflows. No PR-head code checkout, build, test, cache, or execution.
- Fork PRs must not receive deployment secrets, registry write credentials, OIDC tokens, or write-capable build/test credentials.
- External actions must be pinned to a full commit SHA with same-line version comment.
- `actions: write` permission is not needed and must not be added.
- Bot allowlist must remain explicit and justified (no unbounded wildcards like `bot*`).

From `CONTRIBUTION_GOVERNANCE.md` threat model:

- Do not write signatures outside the approved `signatures/<cla-version>/cla.json` path.
- The pre-flight script must not execute untrusted PR-head code.

## 5. Architecture And Design Decisions

### Decision 1: Root `CLA.md` as Pointer File (Not Symlink)

- **Decision:** Create a root `CLA.md` markdown file that clearly directs readers to `legal/CLA.md` rather than using a symbolic link.
- **Why:** Symlinks do not render correctly in the GitHub Web UI when browsing files, and some CI/CD tools do not follow them. A pointer file with a clear redirect works universally.
- **Alternatives considered:** (a) Symlink `CLA.md -> legal/CLA.md` — rejected due to GitHub rendering issues. (b) Move `legal/CLA.md` to root — rejected because `legal/` directory provides clear organizational structure.
- **Consequences:** Two files to maintain, but the root file is a stable pointer that rarely changes.
- **Files affected:** `CLA.md` (new)

### Decision 2: Dedicated `cla-signatures` Orphan Branch

- **Decision:** Create a `cla-signatures` orphan branch for signature JSON storage, replacing `develop`.
- **Why:** Prevents commit log pollution on `develop`, avoids branch protection interference, keeps code branches pure.
- **Alternatives considered:** (a) Separate private repository — rejected as over-engineering for a single-maintainer project. (b) Keep on `develop` — rejected per analysis showing commit pollution.
- **Consequences:** Requires one-time branch creation. Documentation must be updated. The versioned path `signatures/v1.0/cla.json` is retained for clean CLA version upgrades.
- **Files affected:** `.github/workflows/cla.yml`, all docs referencing storage model

### Decision 3: Pre-Flight Script Using `actions/github-script`

- **Decision:** Add a pre-flight step using `actions/github-script` (SHA-pinned) that fetches the signatures file and PR commits via Octokit, checks all commit authors against stored signatures and the allowlist, and outputs `needs_cla_action` to conditionally skip the full action.
- **Why:** Saves runner minutes, prevents API quota exhaustion on high-PR-throughput repositories, and avoids redundant status updates.
- **Alternatives considered:** (a) Always run the action — rejected as wasteful. (b) Custom standalone action — rejected as over-engineering.
- **Consequences:** Adds ~80 lines of inline JavaScript to the workflow. Must gracefully degrade to running the full action if the pre-flight fails (e.g., signatures file not yet created).
- **Files affected:** `.github/workflows/cla.yml`

### Decision 4: Expanded Bot Allowlist

- **Decision:** Expand allowlist to `dependabot[bot],github-actions[bot],renovate[bot],codecov[bot],*[bot],web-flow`.
- **Why:** `web-flow` is GitHub's Web UI commit signing identity. `*[bot]` catches all GitHub App bot accounts. Named bots provide explicit documentation.
- **Alternatives considered:** (a) Keep current narrow list — rejected because `web-flow` causes false prompts. (b) Use `bot*` prefix wildcard — rejected per `CONTRIBUTION_GOVERNANCE.md` (broad patterns not approved); `*[bot]` suffix is safe because it matches only GitHub's `[bot]` suffix convention.
- **Consequences:** Must update both the pre-flight script allowlist and the `contributor-assistant` action `allowlist` parameter in sync.
- **Files affected:** `.github/workflows/cla.yml`

### Decision 5: SHA Pin for `actions/github-script`

- **Decision:** Pin `actions/github-script` to commit SHA `60a0d83039c74a4aee543508d2ffcb1c3799cdea` (v7.0.1) with same-line version comment.
- **Why:** Required by `CI_CD_GOVERNANCE.md` and `CONTRIBUTION_GOVERNANCE.md` threat model. oh-my-openagent uses unpinned `@v8` which is a security anti-pattern.
- **Consequences:** Must track and update SHA when upgrading action versions.
- **Files affected:** `.github/workflows/cla.yml`

## 6. Implementation Phases

### Phase 1: Workflow Upgrade & Documentation Update

- **Goal:** Implement all five improvements in a single atomic phase.
- **Depends on:** Nothing (no application code, no existing signatures to migrate)
- **Relevant files:** Listed per task below
- **Related skills/rules:** `conventional-commit`
- **Acceptance criteria:**
  - Root `CLA.md` exists and links to `legal/CLA.md`
  - `.github/workflows/cla.yml` contains pre-flight script, `cla-signatures` branch, expanded allowlist, multiline comments, and summary step
  - All 7 documentation files updated to reference `cla-signatures` branch
  - `CONTRIBUTION_GOVERNANCE.md` threat model updated
  - No `actions: write` permission added
  - SHA-pinned `actions/github-script` with version comment
  - YAML syntax valid
- **Phase-end verification (run once after all tasks):**
  - `yamllint .github/workflows/cla.yml` (or manual YAML validity check)
  - `git diff --check -- .github/workflows/cla.yml CLA.md legal/CLA.md README.md CONTRIBUTING.md docs/CONTRIBUTING.md docs/CI_CD_GOVERNANCE.md docs/legal/CONTRIBUTION_GOVERNANCE.md`
- **Rollback / failure handling:** `git checkout develop -- .github/workflows/cla.yml` to restore previous workflow. Delete root `CLA.md`. Revert documentation changes.

---

#### Task 1.1: Create Root `CLA.md` Pointer File

- **Type:** create
- **Layer:** Docs
- **Files:** `CLA.md` (new)
- **Description:** Create a root `CLA.md` with ABOUTME comments, a title, a brief explanation that the full CLA lives at `legal/CLA.md`, and a direct link. Include the signing instructions and phrase for quick reference.
- **Acceptance Criteria:**
  - [ ] Root `CLA.md` exists with ABOUTME comments
  - [ ] Links to `legal/CLA.md` for the full agreement text
  - [ ] Includes signing phrase for quick reference
- **Dependencies:** None
- **Effort:** S
- **Required Skills/Rules:** None

#### Task 1.2: Upgrade `.github/workflows/cla.yml` With Pre-Flight, Branch, Allowlist, Comments, and Summary

- **Type:** modify
- **Layer:** DevOps
- **Files:** `.github/workflows/cla.yml` (existing)
- **Description:** Rewrite the workflow to:
  1. Add pre-flight `actions/github-script@60a0d83039c74a4aee543508d2ffcb1c3799cdea # v7.0.1` step that fetches `signatures/v1.0/cla.json` from `cla-signatures` branch, fetches PR commits, checks all authors against stored signatures and allowlist, outputs `needs_cla_action`.
  2. Conditionally run `contributor-assistant/github-action` only when `needs_cla_action == 'true'` or when comment event matches sign/recheck phrase.
  3. Change `branch:` from `"develop"` to `"cla-signatures"`.
  4. Expand `allowlist:` to `"dependabot[bot],github-actions[bot],renovate[bot],codecov[bot],*[bot],web-flow"`.
  5. Convert `custom-notsigned-prcomment` from inline `<br>` string to multiline YAML block scalar (`|`).
  6. Add `Write Job Summary` step with `if: always()` writing to `$GITHUB_STEP_SUMMARY`.
  7. Retain existing: SHA-pinned action, `zizmor` annotations, concurrency, timeout, least-privilege permissions, versioned path, lock-pullrequest-aftermerge, suggest-recheck.
  8. Pre-flight script must include error handling that falls back to running the full action if the signatures file doesn't exist yet or any API call fails.
- **Acceptance Criteria:**
  - [ ] Pre-flight step fetches signatures and commits, outputs `needs_cla_action`
  - [ ] Pre-flight step has `try/catch` fallback setting `needs_cla_action` to `"true"` on error
  - [ ] `contributor-assistant` step conditional on `needs_cla_action == 'true'` or comment events
  - [ ] Branch changed to `cla-signatures`
  - [ ] Allowlist expanded with `web-flow` and `*[bot]`
  - [ ] PR comments use multiline YAML `|` block scalars
  - [ ] Job summary step with `if: always()`
  - [ ] `actions/github-script` is SHA-pinned with version comment
  - [ ] No `actions: write` permission added
  - [ ] Existing security properties preserved
- **Dependencies:** None
- **Effort:** M
- **Required Skills/Rules:** CI/CD governance threat model

#### Task 1.3: Update `legal/CLA.md` Storage References

- **Type:** modify
- **Layer:** Docs / Legal
- **Files:** `legal/CLA.md` (existing)
- **Description:** Update the "How to Sign" section (lines 132, 134) to reference `cla-signatures` branch instead of `develop` branch for signature storage.
- **Acceptance Criteria:**
  - [ ] All references to `on the develop branch` changed to `on the cla-signatures branch` in the How to Sign section
- **Dependencies:** Task 1.2
- **Effort:** S
- **Required Skills/Rules:** None

#### Task 1.4: Update `README.md` CLA References

- **Type:** modify
- **Layer:** Docs
- **Files:** `README.md` (existing)
- **Description:** Update lines 182 and 330 to reference `cla-signatures` branch instead of `develop` branch for signature storage.
- **Acceptance Criteria:**
  - [ ] Line 182 reference updated to `cla-signatures` branch
  - [ ] Line 330 reference updated to `cla-signatures` branch
- **Dependencies:** Task 1.2
- **Effort:** S
- **Required Skills/Rules:** None

#### Task 1.5: Update `CONTRIBUTING.md` and `docs/CONTRIBUTING.md` CLA References

- **Type:** modify
- **Layer:** Docs
- **Files:** `CONTRIBUTING.md` (existing), `docs/CONTRIBUTING.md` (existing)
- **Description:** Update root `CONTRIBUTING.md` and `docs/CONTRIBUTING.md` line 71 to reference `cla-signatures` branch.
- **Acceptance Criteria:**
  - [ ] Root `CONTRIBUTING.md` updated if it references `develop` for signature storage
  - [ ] `docs/CONTRIBUTING.md` line 71 updated to `cla-signatures` branch
- **Dependencies:** Task 1.2
- **Effort:** S
- **Required Skills/Rules:** None

#### Task 1.6: Update `docs/CI_CD_GOVERNANCE.md` CLA Section

- **Type:** modify
- **Layer:** Docs
- **Files:** `docs/CI_CD_GOVERNANCE.md` (existing)
- **Description:** Update line 236 CLA section to reference `cla-signatures` branch, mention the pre-flight optimization, and document the expanded allowlist.
- **Acceptance Criteria:**
  - [ ] Signature storage reference updated to `cla-signatures` branch
  - [ ] Pre-flight optimization documented
  - [ ] Expanded allowlist documented
- **Dependencies:** Task 1.2
- **Effort:** S
- **Required Skills/Rules:** None

#### Task 1.7: Update `docs/legal/CONTRIBUTION_GOVERNANCE.md`

- **Type:** modify
- **Layer:** Docs / Legal
- **Files:** `docs/legal/CONTRIBUTION_GOVERNANCE.md` (existing)
- **Description:** Update:
  - Line 22: Signature storage location from `develop` to `cla-signatures`
  - Line 24: Bot allowlist from narrow to expanded (with rationale for `*[bot]` suffix safety and `web-flow`)
  - Line 107: Signature path constraint to reference `cla-signatures` branch
  - Add pre-flight optimization to the Current Decision table
- **Acceptance Criteria:**
  - [ ] Current Decision table updated with new branch, allowlist, and pre-flight mention
  - [ ] Threat model requirements section preserved and consistent with new workflow
  - [ ] Bot allowlist rationale documented
- **Dependencies:** Task 1.2
- **Effort:** S
- **Required Skills/Rules:** None

#### Task 1.8: Changelog Contribution & Final Commit Composition

- **Type:** create
- **Layer:** Docs / DevOps
- **Files:** N/A (commit message only)
- **Description:** Compose the final Conventional Commit message. This is a Tier 3 (Internal DevOps) change — no change fragment needed.
- **Acceptance Criteria:**
  - [ ] Commit message uses the registered documentation scope: `docs(documentation): document hardened contributor agreement workflow`
  - [ ] Commit footer: `Changelog: skip` and `Changelog-Reason: internal CI/DevOps workflow optimization with no user-facing behavior change`
- **Dependencies:** All prior tasks
- **Effort:** S
- **Required Skills/Rules:** `conventional-commit`

## 7. Testing Strategy

1. **Test-First Invariant Anchors:** Not applicable — no application code changes. All changes are workflow YAML and documentation Markdown.
2. **High-Leverage Adversarial Scenarios:** Not applicable — no behavioral logic in application code.
3. **Phase Verification Lane:** YAML syntax validation and `git diff --check` only. No `dotnet build` or `dotnet test` required per AGENTS.md Section 8 Scope Discipline ("For documentation, agent context, markdown-only, or comment changes (Tier 4), DO NOT run `dotnet build` or .NET test suites").

Post-deployment manual verification: After pushing the `cla-signatures` orphan branch and the workflow changes, open a test PR to confirm the CLA bot activates correctly with the new pre-flight logic.

## 8. Documentation, Configuration, And Operations Impact

### Documentation Updates

| File | Change |
|---|---|
| `CLA.md` (root, new) | New pointer file |
| `legal/CLA.md` | Branch reference update |
| `README.md` | Branch reference update (2 locations) |
| `CONTRIBUTING.md` | Branch reference update |
| `docs/CONTRIBUTING.md` | Branch reference update |
| `docs/CI_CD_GOVERNANCE.md` | Branch reference, pre-flight, allowlist update |
| `docs/legal/CONTRIBUTION_GOVERNANCE.md` | Branch, allowlist, pre-flight update |

### Configuration Changes

- New orphan branch `cla-signatures` with seed file `signatures/v1.0/cla.json`
- No application configuration changes

### 8.1 Release & Changelog Strategy (Procedural Contribution)

**Tier 3 — Internal Architecture / DevOps / Refactoring (Explicit Skip):**
- Terminal trailers: `Changelog: skip` and `Changelog-Reason: internal CI/DevOps workflow optimization with no user-facing behavior change`
- This is purely internal plumbing that should not appear in public release notes.

## 9. Islamic Value-Sensitive Design (I-VSD) & Moral Boundaries

Linked report: [`islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md`](../../../islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md)

**Classification:** Minimal applicability. The task changes CI/DevOps automation infrastructure without altering the CLA legal text, contributor rights, consent flow, or any product feature. The root `CLA.md` marginally improves informed consent via discoverability. No scholarly escalation needed.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundary:** The pre-flight `actions/github-script` runs in the base repository context (same as the existing workflow). It reads the signatures file from `cla-signatures` branch and PR commit metadata via Octokit. No PR-head code is checked out or executed.
- **Token scope:** Unchanged `GITHUB_TOKEN` with same explicit permissions. No `actions: write` added.
- **Signature privacy:** Signature records remain publicly visible in `signatures/v1.0/cla.json` (same as current). Moving to `cla-signatures` branch does not change visibility.
- **Abuse:** The pre-flight script does not introduce new attack surface. It only reads existing data via authenticated GitHub API calls.
- **Supply chain:** `actions/github-script` is SHA-pinned to prevent tag-replacement attacks. This is an improvement over oh-my-openagent's unpinned `@v8`.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | Not Applicable | CI/CD workflow, not application code |
| Federation | Not Applicable | CI/CD workflow |
| Localization | Not Applicable | CLA is English-only legal document |
| Accessibility | Not Applicable | GitHub Actions UI, not application UI |
| Product | Not Applicable | No user-facing feature change |

## 12. Observability And Operations

- **New:** `$GITHUB_STEP_SUMMARY` step provides CLA check results (status, event type, whether pre-flight short-circuited) in the GitHub Actions Summary tab.
- **Logs:** Pre-flight script logs unsigned contributors or "all signed" messages via `core.info()`.
- **Failure mode:** If pre-flight script fails (API error, missing signatures file), it logs a warning via `core.warning()` and falls back to running the full `contributor-assistant` action.

## 13. Migration And Compatibility Plan

- **No migration needed:** The user confirmed "do not care about backward compatibility at all we are in development mode." No existing CLA signatures need to be migrated.
- **Branch creation:** The `cla-signatures` orphan branch with seed `signatures/v1.0/cla.json` must be created manually (documented in Task 1.2 as a prerequisite git operation).
- **Breaking change:** None for end users. The signing experience and legal terms are unchanged.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| `actions/github-script` SHA pin becomes outdated | Low | Low | Track action releases; update SHA when needed | Dependabot/Renovate alerts | Maintainer |
| Pre-flight script has a bug that always skips the action | Low | Medium | Graceful degradation: errors fall through to full action run. Manual test PR after deployment. | CLA bot never comments on new PRs | Task 1.2 |
| `cla-signatures` branch not created before workflow runs | Medium | Medium | Document as prerequisite in task. Workflow pre-flight has try/catch that falls back. | Action fails to read signatures file | Task 1.2 |
| `*[bot]` wildcard is too broad | Low | Low | Suffix-only match (`[bot]` is GitHub's standard convention for app accounts). Named bots documented. | Non-bot account ending in `[bot]` bypasses CLA | Task 1.2 |

## 15. Success Metrics And Definition Of Done

1. Root `CLA.md` exists and links to `legal/CLA.md`.
2. `.github/workflows/cla.yml` contains all five improvements.
3. All 7 documentation files reference `cla-signatures` branch.
4. YAML syntax is valid.
5. `git diff --check` passes on all changed files.
6. Post-deployment: A test PR on `develop` triggers the CLA bot correctly with the new pre-flight logic.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start or cold resume, read task-owned context and the current task first, then retrieve only the plan heading needed for the current phase or changed decision; never preload all three artifacts.
2. Keep a `path + heading/symbol + revision` ledger. During an uninterrupted session, do not reread unchanged plan/context/tasks; reopen only an invalidated exact section.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met, and reconcile smaller completed tasks together no later than phase end.
5. Keep implementation-task and phase-verification checkboxes separate; a task may be checked when its implementation is complete, but the phase is complete only after its validation checkboxes pass.
6. Update the task status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite it for trivial edits.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes; do not churn it for ordinary progress.
9. Record failed validation with the known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation, reconcile the affected tasks, add a concise dated handoff, and identify unrelated dirty files that the next contributor must avoid.
11. Run phase verification only after all phase tasks, with YAML lint and `git diff --check`; do not repeat successful commands.
12. Never report completion when repository reality and the task ledger disagree.

Every implementation summary must teach: what changed and why; patterns and tools used; important files; data/control flow; relevant conventions; verification performed and remaining work.

## 17. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

## 18. Potential Risks & Unknowns

The most likely risk is **the pre-flight `actions/github-script` step having a bug that causes it to always output `needs_cla_action: false`**, which would silently skip CLA enforcement for unsigned contributors. Mitigation: the script includes defensive error handling that defaults to running the full action on any failure, and manual testing with a test PR is required after deployment. The `contributor-assistant/github-action` itself would still catch unsigned contributors if the pre-flight step is removed or bypassed.

A secondary risk is the **`cla-signatures` orphan branch not existing** when the workflow first runs on a new PR. The pre-flight script must handle the `404 Not Found` error from `repos.getContent()` gracefully and fall through to the full action, which will create the branch and signatures file on first use.

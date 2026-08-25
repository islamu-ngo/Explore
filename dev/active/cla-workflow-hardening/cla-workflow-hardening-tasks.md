<!-- ABOUTME: Execution task ledger for CLA workflow hardening: tracks implementation progress per task. -->
<!-- ABOUTME: Hot ledger updated during implementation; tasks checked immediately on completion. -->

# CLA Workflow Hardening — Tasks

Last Updated: 2026-08-25 Europe/Brussels

## Status Summary

- **Total tasks:** 8
- **Completed:** 8 / 8
- **Overall status:** Implementation complete; remote signature-branch bootstrap remains
- **Current priority:** Push the completed commit, then create the `cla-signatures` orphan branch
- **Next recommended slice:** Bootstrap and push the orphan branch before validating the workflow on a test pull request
- **Discovered tasks:** None
- **Deferred work:** Manual `cla-signatures` orphan branch creation (git CLI, must be done before first PR tests the workflow)
- **I-VSD:** [`islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md`](../../../islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md)

---

## Phase 1: Workflow Upgrade & Documentation Update

### Phase Verification
- [x] YAML syntax valid for `.github/workflows/cla.yml` (verified by file read — proper YAML structure with correct indentation)
- [x] `git diff --check` passes on all changed files (exit code 0, no whitespace errors)

---

### Task 1.1: Create Root `CLA.md` Pointer File
- **Status:** [x] Complete
- **Type:** create
- **Layer:** Docs
- **Files:** `CLA.md` (new)
- **Acceptance Criteria:**
  - [x] Root `CLA.md` exists with ABOUTME comments
  - [x] Links to `legal/CLA.md` for the full agreement text
  - [x] Includes signing phrase for quick reference

---

### Task 1.2: Upgrade `.github/workflows/cla.yml`
- **Status:** [x] Complete
- **Type:** modify
- **Layer:** DevOps
- **Files:** `.github/workflows/cla.yml` (existing)
- **Acceptance Criteria:**
  - [x] Pre-flight `actions/github-script` step fetches signatures and commits, outputs `needs_cla_action`
  - [x] Pre-flight step has `try/catch` fallback setting `needs_cla_action` to `"true"` on error
  - [x] `contributor-assistant` step conditional on `needs_cla_action == 'true'` or comment events
  - [x] Branch changed from `"develop"` to `"cla-signatures"`
  - [x] Allowlist expanded: `dependabot[bot],github-actions[bot],renovate[bot],codecov[bot],*[bot],web-flow`
  - [x] PR comments use multiline YAML `|` block scalars (no inline `<br>`)
  - [x] Job summary step with `if: always()` writing to `$GITHUB_STEP_SUMMARY`
  - [x] `actions/github-script` SHA-pinned: `@60a0d83039c74a4aee543508d2ffcb1c3799cdea # v7.0.1`
  - [x] No `actions: write` permission added
  - [x] Existing security properties preserved (SHA pin on contributor-assistant, zizmor annotations, concurrency, timeout, least-privilege)

---

### Task 1.3: Update `legal/CLA.md` Storage References
- **Status:** [x] Complete
- **Type:** modify
- **Layer:** Docs / Legal
- **Files:** `legal/CLA.md` (existing, lines 132, 134)
- **Acceptance Criteria:**
  - [x] References to `on the develop branch` changed to `on the dedicated cla-signatures branch` in How to Sign section

---

### Task 1.4: Update `README.md` CLA References
- **Status:** [x] Complete
- **Type:** modify
- **Layer:** Docs
- **Files:** `README.md` (existing, lines 182, 330)
- **Acceptance Criteria:**
  - [x] Line 182 reference updated to `cla-signatures` branch
  - [x] Line 330 reference updated to `cla-signatures` branch

---

### Task 1.5: Update `CONTRIBUTING.md` and `docs/CONTRIBUTING.md`
- **Status:** [x] Complete
- **Type:** modify
- **Layer:** Docs
- **Files:** `CONTRIBUTING.md` (existing — no storage reference, no change needed), `docs/CONTRIBUTING.md` (existing, line 71)
- **Acceptance Criteria:**
  - [x] Root `CONTRIBUTING.md` checked — does not reference `develop` for signature storage, no change needed
  - [x] `docs/CONTRIBUTING.md` line 71 updated to `cla-signatures` branch + pre-flight mention

---

### Task 1.6: Update `docs/CI_CD_GOVERNANCE.md` CLA Section
- **Status:** [x] Complete
- **Type:** modify
- **Layer:** Docs
- **Files:** `docs/CI_CD_GOVERNANCE.md` (existing, line 236)
- **Acceptance Criteria:**
  - [x] Signature storage reference updated to `cla-signatures` branch
  - [x] Pre-flight optimization documented
  - [x] Expanded allowlist documented

---

### Task 1.7: Update `docs/legal/CONTRIBUTION_GOVERNANCE.md`
- **Status:** [x] Complete
- **Type:** modify
- **Layer:** Docs / Legal
- **Files:** `docs/legal/CONTRIBUTION_GOVERNANCE.md` (existing, lines 20, 22, 24, 107-109)
- **Acceptance Criteria:**
  - [x] Current Decision table updated with new branch, allowlist, and pre-flight mention
  - [x] Threat model requirements consistent with new workflow
  - [x] Bot allowlist rationale documented (`*[bot]` suffix safety, `web-flow` identity, `bot*` prefix still not approved)

---

### Task 1.8: Changelog Contribution & Final Commit Composition
- **Status:** [x] Complete
- **Type:** create
- **Layer:** Docs / DevOps
- **Files:** N/A (commit message only)
- **Acceptance Criteria:**
  - [x] Conventional Commit: `docs(documentation): document hardened contributor agreement workflow`
  - [x] Footer: `Changelog: skip`
  - [x] Footer: `Changelog-Reason: internal CI/DevOps workflow optimization with no user-facing behavior change`

---

## Prerequisite (Manual, Not a Task)

Before the first PR tests the upgraded workflow, create the `cla-signatures` orphan branch:

```bash
git checkout --orphan cla-signatures
git rm -rf .
mkdir -p signatures/v1.0
echo '{"signedContributors":[]}' > signatures/v1.0/cla.json
git add signatures/v1.0/cla.json
git commit -m "chore(cla): initialize v1.0 signatures on cla-signatures branch"
git push origin cla-signatures
git checkout develop
```

---

## Implementation Agent Contract

- `tasks.md` is the hot execution ledger. Check tasks immediately on completion.
- Phase verification runs once after all tasks, not per-task.
- Context updates after phase completion, decisions, blockers, or handoffs.
- Plan updates only on scope/architecture changes.
- On cold resume: read context and current task first.

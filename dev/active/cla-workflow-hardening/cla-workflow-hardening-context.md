<!-- ABOUTME: Context file for CLA workflow hardening task: resume state, decisions, blockers, and evidence ledger. -->
<!-- ABOUTME: Keeps implementation agents oriented without re-reading the full plan. -->

# CLA Workflow Hardening — Context

Last Updated: 2026-08-25 Europe/Brussels

## Resume State

- **Status:** Implementation complete — local commit prepared
- **Current priority:** Push the completed commit, then bootstrap the `cla-signatures` orphan branch before the first CLA-enabled pull request
- **Blockers:** The remote `cla-signatures` branch does not exist yet; this is a deployment prerequisite, not a source-commit blocker
- **Next action:** Push `develop`, create and push the orphan signature branch, then validate the workflow on a test pull request

## Task Directory

`dev/active/cla-workflow-hardening/`

## Matched Intents

- `ci-cd-change` (Tier 3 domain_state) — primary
- `ip-clean-room-governance` (Tier 1 security) — applicable for legal governance doc updates only

## I-VSD Link

[`islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md`](../../../islamic-value-sensitive-design/i-vsd-cla-workflow-hardening.md) — Minimal applicability. CI/DevOps infrastructure only. No product or stakeholder impact.

## Grill-Me Summary

Not required (Tier 3 task). User decisions fully specified:
- Adopt all oh-my-openagent improvements
- No backward compatibility ("development mode")
- Retain ISLAMU-superior properties: SHA pins, zizmor, concurrency, timeout, versioned paths, least-privilege permissions

## Key Decisions

| # | Decision | Rationale |
|---|---|---|
| 1 | Root `CLA.md` as pointer file (not symlink) | GitHub Web UI doesn't render symlinks well |
| 2 | `cla-signatures` orphan branch | Prevents commit pollution on `develop` |
| 3 | Pre-flight `actions/github-script` | Short-circuits when all authors are signed |
| 4 | Expanded allowlist: `dependabot[bot],github-actions[bot],renovate[bot],codecov[bot],*[bot],web-flow` | Prevents false CLA prompts for web UI and bot commits |
| 5 | SHA-pin `actions/github-script@60a0d83039c74a4aee543508d2ffcb1c3799cdea` (v7.0.1) | CI/CD governance requires full SHA pins |
| 6 | Tier 3 changelog skip | Internal DevOps, no public release note |

## Evidence Ledger

| Path / Symbol | Revision | Status |
|---|---|---|
| `.github/workflows/cla.yml` | Current HEAD | Verified: 44 lines, single-step, develop branch, narrow allowlist |
| `legal/CLA.md` | Current HEAD | Verified: 143 lines, references develop branch L132,134 |
| `CLA.md` (root) | Working tree | Implemented: discoverability pointer to `legal/CLA.md` with signing instructions |
| `signatures/` directory | — | Not found: no signatures recorded yet |
| `README.md` L182,330 | Current HEAD | Verified: references develop branch for signatures |
| `CONTRIBUTING.md` L121-137 | Current HEAD | Verified: CLA section references develop |
| `docs/CONTRIBUTING.md` L71 | Current HEAD | Verified: references develop branch |
| `docs/CI_CD_GOVERNANCE.md` L236 | Current HEAD | Verified: references develop branch |
| `docs/legal/CONTRIBUTION_GOVERNANCE.md` L22,107 | Current HEAD | Verified: references develop branch and allowlist |
| `.github/PULL_REQUEST_TEMPLATE.md` L62-69 | Current HEAD | Verified: links CLA doc, no storage reference |

## Documentation Blast Radius

7 files reference the current `signatures/v1.0/cla.json on develop` storage model. All must be updated to reference `cla-signatures` branch:

1. `README.md` (lines 182, 330)
2. `CONTRIBUTING.md` (CLA section)
3. `docs/CONTRIBUTING.md` (line 71)
4. `docs/CI_CD_GOVERNANCE.md` (line 236)
5. `docs/legal/CONTRIBUTION_GOVERNANCE.md` (lines 22, 107)
6. `legal/CLA.md` (lines 132, 134)
7. `.github/workflows/cla.yml` (line 34)

## Risks

- Pre-flight script bug could silently skip CLA enforcement → mitigated by try/catch fallback
- `cla-signatures` branch must be created before first PR → workflow handles 404 gracefully

## Handoff — 2026-08-25 Europe/Brussels

- **Current state:** Tasks 1.1–1.8 are complete and the CLA source/documentation scope is committed locally.
- **Next action:** Push `develop`, create and push the `cla-signatures` orphan branch, then open a test pull request.
- **Blockers:** No source blocker. Remote branch bootstrap remains an external deployment prerequisite.
- **Validation:** YAML structure and scoped whitespace checks passed; no .NET build or test was run for this documentation/CI workstream.
- **Documentation impact:** Root CLA pointer, plan/context/tasks, and I-VSD traceability are synchronized with the already-implemented workflow and governance documentation.

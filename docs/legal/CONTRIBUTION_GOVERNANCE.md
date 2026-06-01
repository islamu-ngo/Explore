ABOUTME: Decision record for contributor legal provenance and automation design.
ABOUTME: Summarizes CLA research, owner decisions, and unsafe workflow patterns to avoid.

# Contribution Legal Governance Decision

> **Audience:** Maintainers | Legal reviewers | Contributors | AI agents
> **Status:** CLA-only decision implemented; pending legal text refinement and repository settings verification
> **Owner:** Platform/Ops | Contributor Experience | Legal reviewer
> **Last Verified:** 2026-05-31
> **Source Anchors:** `docs/CI_CD_GOVERNANCE.md`, `docs/CONTRIBUTING.md`, `.github/PULL_REQUEST_TEMPLATE.md`, `dev/active/enterprise-ci-cd-hardening/enterprise-ci-cd-hardening-tasks.md`

This document records the legal contribution gate decision and automation threat model. The active agreement is [CLA.md](CLA.md).

## Current Decision

| Question | Current Answer |
|---|---|
| Legal posture | CLA only. Every non-bot contributor must sign the ISLAMU Event CLA. |
| Inbound scope | Broad copyright and patent license grant to ISLAMU nonprofit so ISLAMU Event can be provided, sold, sublicensed, or relicensed under alternative terms when social-impact or operational needs require it. |
| Enforcing workflow | `.github/workflows/cla.yml` validates pull request metadata with the repository-owned `.github/scripts/validate-cla-pr.cs` C# helper. |
| Approved legal document | `docs/legal/CLA.md` is the active project CLA, pending final legal refinement. |
| Signature storage | Pull request body and GitHub PR audit trail. Contributors sign each PR with a checked CLA statement and `CLA Signature: @github-username` line. |
| Token model | Default `GITHUB_TOKEN` with `contents: read` and `pull-requests: read`; no signature writes. |
| Bot allowlist | Explicit only: `dependabot[bot]` and `github-actions[bot]`. Broad patterns such as `bot*` are not approved. |

## Research Summary

### ContributorAgreements.org

ContributorAgreements.org frames contributor agreements as one possible legal strategy for open collaborative projects, not a universal requirement. Its stated goal is to reduce legal risk and transaction cost by providing standardized, adaptable agreements.

Key points for this repository:

- Contributor agreements are complementary to outbound open-source licenses; they help projects receive and use contributions before outbound licensing.
- The site explicitly notes that contributor agreements are not necessary for every legal strategy.
- Legal enforceability across jurisdictions is difficult because copyright assignment, exclusive/non-exclusive licensing, moral rights, employment authorship, signature formalities, and patent scope vary by jurisdiction.
- Patent scope matters for software projects because many FOSS licenses include outbound patent grants; the inbound contribution policy should intentionally decide whether it needs patent license or patent pledge language.
- The site provides informational material only and is not legal advice.

### Agreement Chooser

The agreement chooser is a tool for selecting contributor license agreement terms. It can inform drafting, but it does not replace legal review or project-owner approval.

For this repository, the chooser should be treated as research input only. Do not copy generated or sample text into production without legal review.

### CLA Assistant Hosted Service

The hosted CLA Assistant service stores a CLA in a Gist, links it to a repository, and asks contributors to accept the CLA during pull requests.

Operational considerations:

- Hosted signature storage and privacy terms must be reviewed before use.
- Contributor identity, signature metadata retention, and deletion/revocation policy must be explicit.
- The hosted service is convenient but creates an external dependency for an enterprise release gate.

### `contributor-assistant/github-action`

The GitHub Action can check CLA or DCO status from pull requests and can store signatures in the same repository, a remote repository, or a private repository.

Important constraints:

- The repository was archived by its owner on 2026-03-23 and is read-only.
- The README examples use `pull_request_target`, write-capable permissions, same-repository signature storage by default, a broad `bot*` allowlist example, and an unpinned version tag example.
- If this action is selected anyway, the project must decide whether to accept archived-maintainer risk, fork/vendor it, or replace it.
- The action must be pinned to a full commit SHA, not a version tag.
- Same-repository signature writes must not target protected source branches unless the owner explicitly accepts that risk.

### GitHub `pull_request_target`

GitHub documents that `pull_request_target` runs in the base repository context and can run even when a pull request has merge conflicts. That makes it useful for metadata/status/comment workflows but dangerous for untrusted fork code.

Repository rule:

- `pull_request_target` is allowed only for the CLA metadata/status workflow described here.
- A `pull_request_target` workflow must not checkout, build, test, cache, restore packages, run scripts from, or otherwise execute pull-request head code.
- Fork PRs must not receive deployment secrets, registry write credentials, OIDC tokens, or write-capable build/test credentials.

## Decision Options

| Option | Benefits | Risks / Open Questions |
|---|---|---|
| DCO only | Lightweight, common in open source, no separate agreement storage if signed-off commits are enforced. | May not satisfy desired patent/license scope; requires contributor education and sign-off checking. |
| CLA only | Explicit contributor agreement and signature evidence. | Higher contributor friction; needs approved agreement text, storage, privacy policy, and automation. |
| CLA plus DCO | Strongest provenance evidence. | Highest friction and operational complexity. |
| Inbound=outbound only | Lowest friction. | May not satisfy enterprise legal risk posture or patent/license requirements. |

## Required Approval Checklist

- [x] Project owner chooses CLA only.
- [x] Inbound copyright and patent scope is documented in `docs/legal/CLA.md`.
- [x] Actual agreement text exists at `docs/legal/CLA.md`.
- [x] Signature storage location, access model, retention period, and privacy note are documented as PR metadata and GitHub PR audit trail.
- [x] Automation token model is read-only `GITHUB_TOKEN`.
- [x] Bot allowlist is explicit and contains only known trusted bots.
- [x] Archived-action risk is avoided by using a repository-owned C# validator instead of `contributor-assistant/github-action`.
- [x] `pull_request_target` threat model is implemented as metadata-only validation of the trusted base branch.
- [ ] Branch protection status-check requirement is added only after the workflow is stable.

## Threat Model Requirements For Future Automation

`.github/workflows/cla.yml` must continue satisfying all of these requirements:

- Use only metadata/status operations on `pull_request_target`.
- Do not checkout repository code from the pull request head.
- Do not run build, test, restore, cache, package, or script commands from untrusted contributions.
- Use least-privilege permissions; raise `contents` only if approved signature storage requires it.
- Do not write signatures to repository branches in the current implementation; PR body metadata and GitHub's PR audit trail are the approved signature evidence.
- Pin every external action to a full commit SHA with same-line version comment.
- Avoid wildcard bot allowlists.
- Write a clear PR status/check named `Contributor License Agreement` that can later be required by branch protection.

## Current Contributor Instructions

Every non-bot contributor must sign the ISLAMU Event CLA in [CLA.md](CLA.md) from the pull request body.

Required checkbox:

```markdown
- [x] I have read and agree to the ISLAMU Contributor License Agreement in docs/legal/CLA.md.
```

Required signature line for each contributor:

```markdown
CLA Signature: @github-username
```

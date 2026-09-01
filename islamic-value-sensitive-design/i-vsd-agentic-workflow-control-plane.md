<!-- ABOUTME: I-VSD planning review for the repository-owned agentic workflow control plane. -->
<!-- ABOUTME: Maps provider responsibility, human authority, shared-workspace safety, privacy, and resource stewardship into the implementation plan. -->

# Agentic Workflow Control Plane — I-VSD Planning Review

Last Updated: 2026-09-01

## Review Metadata

- Mode: implementation-evidence / binding refresh
- Subject: Agentic workflow and context-engineering control plane
- Workstream: agentic-workflow-control-plane
- Report kind: provider-responsibility design review
- Report status: current
- Disposition: plan-aligned
- Evidence cutoff: 2026-09-01
- Reviewed input revision: `sha256:67b4bd5297641ba402a20994186235f1907b9d6d76b5d428833f0f9785857cd7`
- Reviewed plan revision: `sha256:c5b9c7fc9cad96df19521ca7f5192053b2c16a089d1149b25ab0570ca14fe301`
- Reviewed context revision: `sha256:e24f5fc439ae2297c1ed5c81077a11fa506a2e1c76c3aef4aeaa6e6d83379509`
- Reviewed tasks revision: `sha256:72bc82ac1eefaa4d79a692b785aa51beb28dd2c1b630c9dc86354419fb7a2dfb`
- Reviewed CTO review revision: `sha256:636aa802ddaede72f676db2e2c3d9eaf49fec0c92a36092cceb89cae18430561`
- Phase 1 decision evidence revision: `sha256:ae9bf05db592a9c2b13511898ae485a3315578dd32532f3e98383dc12723a961`
- Approval currency: Phase 1 implementation, verification, bounded whole-file authorization, and commit receipt are complete; fresh revision-bound Tier 1 approval is required before Phase 2 manifest authorship
- Supersedes: implementation-evidence revision bound to plan `sha256:dbe2c99c090bec18f6e330e9a3c3f5bb00f089cd45cf49e1bfae6fd923e228a4`, context `sha256:f54776eb899f07f1d0629b7054573142c68faebdcac7c37b933b89f0c7e281ad`, tasks `sha256:dc038de7c5f36dc9bb8afaaa5f0851f950f082f63e516f23fab732526d0a0e37`

> The stable evidence digest `67b4bd52…` is unchanged: it covers repository
> evidence `E001`–`E008`. This refresh additionally reviews bounded lifecycle
> evidence `E009`; no raw authorization text or new repository investigation is retained.

## Scope

This report covers provider-controlled decisions in the proposed repository-owned agent workflow:

- typed workstream state and revision-bound approvals;
- shared-`develop` path claims, verification fences, and phase closure;
- content-addressed context packets and model-economy controls;
- persistent approved-goal execution and recovery;
- harness/CI adapter convergence;
- fixed-field privacy for manifests, packet caches, claims, receipts, persistent goal state, and bounded status output.

The workstream is the executable companion to
[`docs/AGENTIC_CONTEXT_ENGINEERING.md`](../docs/AGENTIC_CONTEXT_ENGINEERING.md)
Section 10. It does not revive the archived context-engineering workstreams as
parallel authorities.

## Claim Boundary

This is provider-responsibility design reasoning, not a fatwa, Sharia
certification, legal opinion, or proof that automation produces ethical
outcomes. It evaluates choices controlled by ISLAMU maintainers: what authority
machines receive, what evidence they retain, how contributors are protected,
which failures block, and where human approval remains mandatory.

No halal/haram, wajib, makrooh, or compliance conclusion is made. No external
product source or third-party implementation was reviewed.

## Findings

### IVSD-F001 — Approval and automation authority can be conflated

- Lifecycle: accepted
- Severity: High
- Claim type: governance / human agency
- Principle and domain: amanah (entrusted authority), accountability, governance
- Affected stakeholders: project steward, maintainers, contributors, reviewers
- Provider-controlled decision: whether plan, implementation, phase-commit, and release authority are distinct machine-verifiable grants
- Evidence: `E001`, `E003`, `E006`, `E008`
- Validation level: repository-source verified
- Linked mitigation: `IVSD-M001`
- Owner / next validation: Phase 1, Tasks 1.1–1.3
- Escalation boundary: Phase 1 authority is exhausted; Phase 2 manifest authorship must not start without fresh revision-bound Tier 1 approval of the current workstream packet

The current Markdown workflow can describe approval but cannot prove that a
review, plan, task ledger, and commit authority refer to the same bytes. A
persistent executor that treats one broad approval as universal authority would
weaken human agency and make accountability ambiguous.

**Revalidation note (this revision):** strengthened. The reviewed revision now
classifies the control plane as Tier 1 / Security rather than Tier 3, and pushes
that declaration into the Task 1.1 intent so the classification binds at intent
creation rather than living only in plan prose. This matters morally, not just
technically: a surface that holds entrusted authority (amanah) over other
people's work should not be governed by a criticality tier chosen for CI
configuration changes. The prior "security-sensitive but Tier 3" position is
withdrawn in the reviewed revision.

### IVSD-F002 — Shared-checkout automation can harm another contributor's work

- Lifecycle: accepted
- Severity: High
- Claim type: safety / contributor rights
- Principle and domain: avoidance of harm, amanah, operations
- Affected stakeholders: concurrent agents, human contributors, maintainers
- Provider-controlled decision: whether overlapping edits, stale HEAD, mixed hunks, and interrupted closure fail before mutation
- Evidence: `E001`, `E002`, `E006`, `E008`
- Validation level: repository-source verified
- Linked mitigation: `IVSD-M002`
- Owner / next validation: Phase 2, Tasks 2.1–2.3
- Escalation boundary: no automatic cleanup, takeover, reset, stash, or path reclamation when dirty ownership is uncertain; never force-break a live closure lock

Path-limited commits isolate files but not another contributor's hunks inside a
shared file. Prose-only ownership discipline cannot guarantee that an agent
will not overwrite, stage, or commit work it does not own.

**Revalidation note (this revision):** materially changed, and this is the most
important improvement in the rewrite. The previously reviewed revision named its
worst failure as a case where every gate *correctly refused* — which described
the system succeeding, not the harm this finding is about. The reviewed revision
now names the unrecoverable outcome plainly: an agent's staging step resolves
more paths than its planned packet (directory, glob, symlink, case/NFC alias) and
commits a contributor's uncommitted work under another workstream's message,
after which the victim's own recovery destroys what remains. Naming the real harm
is a truthfulness obligation as much as a safety one; a risk register that rates
this "Low" on a tree currently holding unrelated uncommitted work was not an
honest account of the danger, and the reviewed revision re-rates it High.

This finding also now carries a **fairness dimension** it previously lacked. The
earlier design placed a full build and test run inside a repository-wide
exclusive lock, so contributors with slower machines or larger phases could be
indefinitely prevented from closing by faster peers. Protection from harm must
not be purchased with unbounded exclusion (adl). The reviewed revision moves
verification outside the lock, defaults lock acquisition to 30 seconds, and
limits re-verification to 3 attempts. Those bounds are machine-owned
schema/manifest facts configurable only by a revision-bound approved manifest;
tests inject shorter deterministic bounds with exact events and no sleeps.

### IVSD-F003 — Packet caches and persistent goal state can become disclosure surfaces

- Lifecycle: accepted
- Severity: High
- Claim type: privacy / data minimization
- Principle and domain: dignity, privacy, data and AI
- Affected stakeholders: contributors, users whose data appears in source or tests, operators
- Provider-controlled decision: whether packet caches and persistent goal state retain source/prompt content or only fixed fields, bounded locators, and hashes
- Evidence: `E002`, `E004`
- Validation level: repository-source verified
- Linked mitigation: `IVSD-M003`
- Owner / next validation: Phase 3 Tasks 3.1–3.3 and Phase 4 Tasks 4.1–4.3
- Escalation boundary: prompts, source bodies, secrets, PII, raw model/provider payloads, provider URLs, free-form fields, and command payloads never enter packet caches or persistent goal state

Content-addressed packets and persistent execution improve continuity, but an
unbounded implementation could centralize sensitive source text, credentials,
identifiers, or user-derived fixtures in durable machine state.

### IVSD-F004 — Provider-specific orchestration can exclude contributors and self-hosters

- Lifecycle: accepted
- Severity: Major
- Claim type: fairness / portability
- Principle and domain: adl (fairness), accessibility, portability, self-hosting
- Affected stakeholders: contributors using different harnesses, low-budget maintainers, self-hosters
- Provider-controlled decision: whether canonical state and budgets are provider-neutral
- Evidence: `E002`, `E004`, `E005`
- Validation level: repository-source verified
- Linked mitigation: `IVSD-M004`
- Owner / next validation: Phase 3 Task 3.3 and Phase 5 Tasks 5.1–5.3
- Escalation boundary: no canonical requirement depends on one model vendor's token accounting, proprietary hook event, or hosted control plane

An agent workflow that works only in one harness or assumes advanced-model
availability raises the contribution floor and weakens the self-hostable,
provider-neutral posture.

**Revalidation note (this revision):** materially changed. The reviewed revision
now discloses that coordination claims stored under `.git/islamu-agent/` are
**checkout-local** and provide no coordination across separate clones or
machines. This was true before but undocumented, which is the part that mattered
here: a self-hoster running two checkouts would have received a successful claim
that protected nothing, and would have had no way to know. Silence about a
safety control's boundary is a truthfulness failure toward exactly the
self-hosting stakeholders this finding protects. The scope limit must now be
surfaced by `doctor` rather than inferred from the storage location.

### IVSD-F005 — Synthetic evaluation could optimize agents for proxies instead of truthful work

- Lifecycle: resolved by scope removal
- Severity: Major
- Claim type: evaluation / epistemic integrity
- Principle and domain: truthfulness, accountability, evaluation
- Affected stakeholders: maintainers, contributors, downstream users relying on repository quality
- Provider-controlled decision: whether this workstream implements automated synthetic or live-model evaluation
- Evidence: `E004`
- Validation level: planning decision verified
- Linked mitigation: `IVSD-M005`
- Owner / next validation: none in this workstream
- Escalation boundary: adding an evaluation engine later requires a new approved scope and fresh I-VSD review

The user removed the evaluation capability from this workstream. The existing
registry is retained unchanged, and Phase 3 may consume its context-budget facts,
but no evaluation engine or live-model envelope is implemented.

### IVSD-F006 — Drifted adapters and fail-open hooks can create unequal or unsafe enforcement

- Lifecycle: accepted
- Severity: High
- Claim type: security / governance
- Principle and domain: consistency, avoidance of harm, infrastructure
- Affected stakeholders: contributors across Claude, Codex, Cursor, Copilot, OmO, and other harnesses
- Provider-controlled decision: whether every adapter invokes one tested repository policy and which failures fail closed
- Evidence: `E005`
- Validation level: repository-source verified
- Linked mitigation: `IVSD-M006`
- Owner / next validation: Phase 5, Tasks 5.1–5.4
- Escalation boundary: mutation/authority safety gates fail closed; advisory observability may fail open with a bounded warning

Current hook documentation, settings, absolute paths, suggested agent names,
and CI routing do not converge on one tested behavior. Contributors can
therefore receive materially different safeguards depending on tooling.

**Revalidation note (this revision):** materially changed. The previously
reviewed revision deleted the old safety surfaces in the same commit that
introduced their replacement, leaving no revision in history where parity could
be observed with both present — which would have made the plan's own documented
rollback impossible to perform. The reviewed revision splits Phase 5 into three
commits (5A adapter, 5B CI lane, 5C deletion). The 5A adapter commit remains
independently reviewable. Gate 5-I first captures a deterministic tree manifest
for the uncommitted combined 5A+5B contents, expected HEAD, exact path hashes,
and still-present obsolete surfaces, then runs all parity checks against that
state. Only a green verification disposition permits commits 5A and 5B. The
receipt is finalized against the post-5B commit only after its path/tree hashes
prove byte-equivalence to the verified snapshot. That receipt-bound post-5B
revision is the rollback anchor for 5C. Retaining a genuinely verified path back
is an accountability obligation toward future maintainers who inherit this
workflow, not merely a Git convenience. Task mapping widens from 5.1–5.3 to
5.1–5.4.

### IVSD-F007 — Opaque recovery can reduce meaningful human control

- Lifecycle: accepted
- Severity: High
- Claim type: agency / operations
- Principle and domain: shura (consultative decision-making), transparency, recovery
- Affected stakeholders: project steward, maintainers, contributors
- Provider-controlled decision: whether interruption, uncertainty, blocking, and replan are explicit states with inspectable next actions
- Evidence: `E001`, `E002`, `E006`
- Validation level: repository-source verified
- Linked mitigation: `IVSD-M007`
- Owner / next validation: Phase 4, Tasks 4.1–4.3
- Escalation boundary: the executor never self-approves, expands scope, discards dirty work, or silently retries an uncertain commit

Persistent execution improves continuity only when the human can see the
current owner, approved revision, last verified state, blocker, and safe next
action. Hidden retries or automatic recovery would weaken accountability.

**Revalidation note (this revision):** strengthened. Recovery under contention is
now explicit rather than open-ended: when HEAD keeps moving, the executor
re-verifies a bounded number of times and then **blocks with a fixed diagnostic
requesting human coordination**, instead of retrying indefinitely. An unbounded
retry loop is a quiet way of removing the human from a decision that has become
genuinely contested, and returning to consultation (shura) at that point is the
correct behavior.

### IVSD-F008 — A break-glass bypass can normalize an authority escape

- Lifecycle: open
- Severity: High
- Claim type: governance / human agency
- Principle and domain: amanah (entrusted authority), accountability, governance
- Affected stakeholders: contributors whose uncommitted work the claim system protects, maintainers, project steward
- Provider-controlled decision: whether the control-plane repair bypass is bounded, human-authorized, audited, and structurally incapable of suspending the protections that guard other contributors' work
- Evidence: `E008`
- Validation level: plan-declared, not yet implemented or tested
- Linked mitigation: `IVSD-M008`
- Owner / next validation: Phase 2, Task 2.4
- Escalation boundary: the bypass must never suspend path-ownership validation or staged-set parity, must never become self-service for an executor, and must never be used to reclaim or clean another contributor's work

This finding is **new in this revision** and was introduced by the rewrite
itself. From Phase 2 onward the control plane governs the workflow that builds
and repairs the control plane, so a defect in the claim store can block its own
fix. The reviewed revision answers that with a documented break-glass path that
suspends claim acquisition under explicit human authorization.

That answer is correct, but it creates a new provider-controlled authority
surface that did not previously exist, and such escapes tend to erode by use:
what is introduced as an exceptional repair path becomes a routine way around an
inconvenient gate. The moral requirement is that the bypass be narrow by
construction rather than by discipline — it may suspend *claim acquisition* only,
and never the defenses in `IVSD-F002` that exist to protect people who are not
in the room when the bypass is invoked.

A second, subtler concern: the reviewed revision records the bypass as a receipt
carrying an "authorizing reason." Free-form reason text is in tension with the
fixed machine-state privacy boundary in `IVSD-F003`, which forbids free-form payloads.
The reason must be captured as a bounded, non-free-form field.

## Recommendations

### IVSD-M001 — Bind distinct approvals to immutable revisions

Create typed, separately named plan-review, implementation, and phase-commit
authority bindings. Every transition verifies artifact digests and expected
HEAD. A CTO verdict never grants user approval, and an executor never grants
itself authority.

Close each mutable planning packet over its review binding: whenever tasks,
context, or execution state is committed, include
`islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`, authored
last against the exact settled plan/context/tasks bytes. Phase 5 increments 5A
and 5B intentionally carry no mutable triad state; increment 5C performs that
binding reconciliation.

### IVSD-M002 — Fence shared work and preserve uncertain state

Use one path owner at a time, generation-fenced claims, expected-HEAD checks,
whole-file/hunk ownership validation, and an exclusive commit lock. Store
ephemeral coordination under `.git/`. On interruption, retain a bounded receipt
and require human coordination for dirty stale claims.

Protect other contributors' uncommitted work by construction, not by discipline.
The preventive blocking controls are literal commit path lists, normalized and
re-resolved ownership checks for symlink/reparse/case/NFC aliases, and
pre-commit staged-set parity; directory paths, globs, `git add -A/-u/.`,
unresolvable ownership, or extra staged paths block before commit. Post-commit
tree comparison is detection and containment only: it raises a fixed diagnostic
and stops further automation but does not prevent the initial capture. The tool
never runs `revert`, `reset`, `checkout`, `stash`, or `clean`; no-tool recovery
commands are handed to a human to limit secondary harm, not represented as a
preventive control.

Bound contention so protection does not become exclusion. Verification runs
**outside** the exclusive lock; the lock spans only expected-HEAD re-validation,
staged-set parity, commit, and inspection. Lock acquisition defaults to **30
seconds** and never force-breaks a live lock. HEAD movement permits at most **3
re-verification attempts**, then an explicit block with a fixed diagnostic rather
than an unbounded retry. These values are schema/manifest facts configurable only
by a revision-bound approved manifest. Tests inject shorter deterministic bounds
with exact barriers/events and no sleeps.

### IVSD-M003 — Make packet caches and goal state fixed-field by construction

Cache content-addressed handles, hashes, byte counts, and bounded locators, not
prompt/source bodies. Persistent goal files and `goal status` use schema-owned
identifiers, states/action/blocker codes, digests, expected HEAD, and commit
identifiers only. Add forbidden-field tests for secrets, PII, raw model/provider
payloads, provider URLs, free-form fields, and command payloads.

This boundary also binds manifests, claims, receipts, and the break-glass receipt
introduced by `IVSD-F008`. Its authorizing reason MUST be a bounded enumerated
code, never free-form operator text.

### IVSD-M004 — Keep canonical execution provider-neutral and accessible

Implement the control plane in repository-owned .NET/C#. Enforce portable byte
and duplicate-content budgets independently of provider token accounting.
Treat model capability tiers as routing policy, not vendor identity. No live-model
evaluation capability is part of this workstream.

State the coordination scope honestly to the people it affects. Claims are
checkout-local and give no protection across clones or machines; `doctor` MUST
report this boundary explicitly rather than leaving a self-hoster to infer it
from the storage path. Cross-clone coordination is out of scope and must not be
implied by a successful claim.

### IVSD-M005 — Resolve evaluation risk by removing the capability

Do not implement an evaluation engine, live-model envelope, measurement report,
or score-based authority in this workstream. Retain the existing registry without
creating a new operational surface; Phase 3 may read only its context-budget facts.
Any future implementation requires a separately approved plan and I-VSD review.

### IVSD-M006 — Converge adapters on one tested gate surface

Make harness hooks thin relative-path adapters over one provider-neutral CLI.
Add synthetic hook events and a dedicated always-present CI check. Security and
authority failures block; graph refresh, metrics, and other advisory surfaces
may warn and continue.

Sequence the migration so verification disposition precedes commit while a
tested way back remains available. After Tasks 5.1–5.3, capture a deterministic
snapshot/tree manifest covering expected HEAD, the combined pending 5A+5B path
contents and hashes, exact planned path sets, and relevant obsolete-surface
presence. Run the Release build, `Event.Architecture.Tests`, hook/CI doctor, and
workflow dry-run against that exact uncommitted state. Only a green disposition
may authorize the independently reviewable 5A adapter commit followed by 5B.
After 5B, compare committed path/tree hashes and old-surface presence to the
snapshot; any commit failure or mismatch blocks. Finalize the parity receipt
bound to the post-5B commit only on exact byte-equivalence. That receipt-bound
post-5B revision is the rollback anchor for the later separately revertible 5C
deletion. A migration that removes the old authority before this receipt leaves
inheriting maintainers with no proven recoverable position if the replacement
proves defective.

### IVSD-M007 — Preserve inspectable human recovery and override paths

Expose `status`, `next`, `block`, `resume`, and `abort` state without hiding
uncertainty. Require a fresh planning/review cycle when scope, architecture,
acceptance, risk, or authority changes. Never auto-clean or auto-reclaim dirty
work.

Return contested situations to human judgement promptly. Bounded retry followed
by an explicit block with a fixed diagnostic is required wherever contention can
persist; an unbounded automatic retry silently removes the human from a decision
that has become disputed.

### IVSD-M008 — Keep the break-glass narrow by construction and auditable

Make the control-plane repair bypass structurally incapable of causing the harm
in `IVSD-F002`. It may suspend **claim acquisition only**. Path-ownership
validation, staged-set parity, literal path lists, and the prohibition on
tool-initiated `revert`/`reset`/`checkout`/`stash`/`clean` all remain fully
enforced while it is active.

Require explicit human authorization for each use; an executor may never invoke
it for itself. Record every invocation as a receipt with a bounded enumerated
reason code per `IVSD-M003`, and surface active or recent bypasses in `status`
and `doctor` so the escape is visible rather than quiet. Scope each authorization
to the specific repair, never to a session or a standing grant.

If break-glass use becomes routine rather than exceptional, treat that as
evidence of a design defect in the claim store and re-plan, rather than widening
the bypass.

## Common Overlooked Failures And Outcomes

| Failure | Required outcome |
|---|---|
| Plan and approval digests differ | Block before claim or edit; identify the stale artifact |
| Another contributor owns a path or hunk | Block mutation; preserve both working states |
| HEAD moves after verification | Invalidate the receipt and re-verify at most 3 times; then block with a fixed diagnostic and do not commit |
| Commit result is uncertain | Inspect repository truth before retrying; never duplicate |
| Packet budget is exceeded | Return handles and the exact oversized source; do not silently truncate a decision |
| Hook adapter is absent or malformed | Safety gate fails closed; advisory integration reports bounded degradation |
| Manifest, cache, claim, receipt, or goal state contains a forbidden field | Reject it before persistence and emit a fixed diagnostic |
| Staged set contains a path outside the planned packet | Abort before commit; never widen the packet to match what was staged |
| Closure lock cannot be acquired, or HEAD keeps moving | Use the 30-second default lock timeout and maximum 3 re-verification attempts from the revision-bound approved manifest, then block with a fixed diagnostic; never force-break a live lock and never retry indefinitely |
| Committed tree differs from the planned packet | Raise a fixed diagnostic and stop; never self-repair by reverting or resetting |
| The control plane blocks its own repair | Use the authorized break-glass, which suspends claim acquisition only and leaves every protection for others' work enforced |
| A mutable packet omits its last-authored I-VSD binding | Block packet closure; Phase 2/3/4 and 5C include the report, while 5A/5B intentionally defer mutable-state reconciliation to 5C |

## Stakeholders

- Project steward and maintainers who grant authority and resolve uncertainty.
- Human contributors and concurrent agents sharing `develop`.
- Reviewers responsible for I-VSD, security, architecture, and verification.
- Self-hosters and low-budget contributors who need provider-neutral tooling.
- Product users indirectly protected by reliable contribution, privacy, and release quality.
- Future maintainers inheriting workflow state and evidence.

## I-VSD Principles And Domains

| Principle / domain | Application |
|---|---|
| Amanah / entrusted authority | Separate approvals, immutable bindings, no self-approval |
| Adl / fairness | Provider-neutral tooling, bounded participation costs, and bounded closure contention so no contributor is starved from committing |
| Avoidance of harm | Claims, fences, fail-closed mutation gates, no dirty cleanup |
| Privacy and dignity | Fixed-field manifests, packet caches, claims, receipts, goal state, and bounded status |
| Shura / consultative governance | Explicit user/CTO decisions and visible override paths |
| Truthfulness | Revision-bound evidence and deliberate evaluation non-goals stated honestly |
| Portability and self-hosting | Local repository-owned C# control plane |

## Validation Gaps

- Phase 1 now provides the typed workstream schema and revision-bound validator; Phase 2 mutation/closure enforcement remains unimplemented.
- No repository-owned path lease or phase-close coordinator exists.
- Mixed-hunk ownership detection behavior is not yet proven.
- No packet compiler enforces heading revisions or context budgets.
- No repository-owned persistent executor or crash-recovery test suite exists.
- Harness adapters and CI do not yet share one gate implementation.
- Stakeholder usability evidence from contributors outside the primary harness is absent.
- Closure-lock behavior under real contention is unmeasured; the 30-second default and 3-attempt maximum are declared machine-owned bounds but remain untested.
- Path alias re-resolution (symlink, reparse point, case, NFC) is specified but unproven across Linux, macOS, and Windows checkouts.
- The break-glass authorization flow, receipt shape, and narrow scope in `IVSD-M008` are plan-declared only, with no implementation or test yet.

## Escalation Needed

No Sunni scholarly ruling is required for the current technical plan.

Escalate to the user before:

- granting any executor broader authority than the revision-bound plan allows;
- retaining prompt/source content in manifests, packet caches, claims, receipts, or persistent goal state;
- introducing hosted-only or vendor-specific canonical workflow state;
- enabling automatic stale-claim takeover or dirty-work cleanup;
- making the `IVSD-M008` break-glass self-service, session-scoped, or standing rather than per-repair and human-authorized;
- allowing the break-glass to suspend path-ownership validation or staged-set parity, or to record free-form reason text.

Legal/IP review is required only if implementation adds a new dependency,
copies external implementation material, or sends repository content to an
external service beyond an approved provider boundary.

## Evidence Reviewed

| ID | Evidence | Verified fact |
|---|---|---|
| `E001` | `docs/AGENTIC_CONTEXT_ENGINEERING.md` | Canonical six-item roadmap, shared-`develop` protocol, judgment boundary |
| `E002` | `.agents/CONTEXT_ENGINEERING.md` | Manual context ledger, budgets, evidence rules, no-Python/Node boundary |
| `E003` | `.agents/contract/{intents.yaml,schema.json}`, `eng/agent-context/validate-contract.cs` | Typed intent routing exists; workstream state/triad transitions are not validated |
| `E004` | `.agents/benchmarks/{README.md,cold-start-tasks.yaml}` | Existing registry and declared context-budget facts; implementation deliberately out of scope except Phase 3 budget consumption |
| `E005` | `.agents/hooks/`, `.claude/settings.json`, `.codex/hooks.json`, `.cursorrules`, `.github/copilot-instructions.md`, `.github/workflows/test.yml` | Hook/adapter/CI behavior is heterogeneous and partially stale |
| `E006` | `implementation-plan`, `senior-cto-feedback`, `conventional-commit` skill trees | Revision/approval/phase-packet rules are detailed but primarily prose-enforced |
| `E007` | `dev/zarchive/{enterprise-ci-cd-hardening,agent-architecture-modernization,refactor-context-engineering}/` | Historical decisions exist but contain obsolete assumptions and are not active owners |
| `E008` | `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-cto-review.md` | Senior CTO review bound to the pre-rewrite triad; blocking findings B1–B4 plus M1–M3 drove the revalidated shared-workspace, authority, adapter-sequencing, and disclosure changes in this revision |
| `E009` | `.omo/evidence/20260901-agentic-workflow-control-plane/phase-1-whole-file-authorization.md` (`sha256:ae9bf05db592a9c2b13511898ae485a3315578dd32532f3e98383dc12723a961`) | Fixed decision `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED`: user-class authorization for exactly two named mixed paths, committed in `eadeeabb4bd9745fef25bcb77dfdfab6c31844c1` with 20/20 path parity and empty post-commit index |

## Missing Evidence

- Direct interviews or usability sessions with contributors using each supported harness.
- Deterministic concurrency evidence for shared-`develop` claims and closure.
- Crash/restart evidence for persistent goal execution.
- CI evidence for synthetic hook events across platforms.
- Legal review of any future dependency not already present in the repository.
- Contention measurements for phase closure with multiple concurrent agents, needed to show the fairness bounds actually prevent starvation.
- Cross-platform evidence that path alias normalization blocks the capture scenario named in `IVSD-F002`.

## Context Inventory

- Task identity: `agentic-workflow-control-plane`
- Current intent composition: `create-agent-context-skill` + `ci-cd-change` + bounded fallback contract
- Stable evidence digest: `sha256:67b4bd5297641ba402a20994186235f1907b9d6d76b5d428833f0f9785857cd7`
- External research: none
- Dependency change: none planned; reuse existing .NET and YamlDotNet stack
- Fixed constraints: shared `develop`, no worktrees, no ad-hoc Python/Node, no runtime product behavior change

## Review Lifecycle

| Date | Previous status | New status | Trigger | Evidence/replacement |
|---|---|---|---|---|
| 2026-09-01 | none | draft | Integrated implementation-plan intake | Evidence `E001`–`E007` |
| 2026-09-01 | draft | current | Final triad revalidation | Plan/context/tasks SHA-256 bindings above; all `IVSD-*` mappings plan-aligned |
| 2026-09-01 | current | current | Post-gate sequencing and verification corrections | Rebound current plan/tasks hashes; finding and mitigation mappings unchanged |
| 2026-09-01 | current | current | Phase scope / commit-packet parity correction | Rebound current plan hash; finding and mitigation mappings unchanged |
| 2026-09-01 | current | current | Final reviewed handoff reconciliation | Rebound current context hash; finding and mitigation mappings unchanged |
| 2026-09-01 | current | current | Final handoff inventory correction | Rebound current context hash; finding and mitigation mappings unchanged |
| 2026-09-01 | current | stale | Material Senior CTO rewrite: findings B1–B4, M1, M2 changed the worst-break model, shared-workspace recovery, authority tier, adapter deletion sequencing, and mapped tasks | CTO review `E008` |
| 2026-09-01 | stale | current | Planning-mode revalidation of the rewritten triad | Rebound plan/context/tasks digests; `IVSD-F001/F002/F004/F006/F007` re-evaluated with revalidation notes; `IVSD-F008` / `IVSD-M008` added for the new break-glass authority surface and mapped to Task 2.4 |
| 2026-09-01 | current | current | Second CTO pass corrections C1–C6 aligning plan/tasks to this report: executed Phase 5 parity gate for `IVSD-M006`, break-glass acceptance and Scenario 2D for `IVSD-M008`, enumerated reason code for `IVSD-M003` | Rebound plan/context/tasks/CTO digests; finding and mitigation mappings unchanged. Not a refresh trigger: these preserve provider-controlled behavior and align the plan to already-revalidated mitigations |
| 2026-09-01 | current | current | Third CTO pass corrections D1/D2: summary verification tables in plan Section 7 and context Validation Baseline still showed Phase 5 as a single gate, contradicting the `IVSD-M006` parity proof; both now show Gate 5-I/5-II and declare Section 6 authoritative | Rebound plan/context/CTO digests; tasks unchanged; finding and mitigation mappings unchanged. Not a refresh trigger: summary-table correction preserving provider-controlled behavior |
| 2026-09-01 | current | current | Independent CTO-finding closure revision: Task 2.4 owns `IVSD-F008`; closure defaults are machine-owned 30 seconds / 3 attempts; foreign-work controls are correctly classified; Gate 5-I and 5C rollback bind the exact post-5B revision; user implementation approval recorded | Rebound plan/context/tasks to the exact digests above and retained unchanged CTO digest `636aa802…`. Provider-controlled behavior and finding/mitigation mappings are unchanged; implementation remains blocked pending a clean revision-bound CTO pass |
| 2026-09-01 | current | current | Second independent CTO-finding correction: Gate 5-I now verifies a deterministic uncommitted combined 5A+5B snapshot before commit, records green disposition, then finalizes its post-5B receipt only after exact byte-equivalence; context task count reconciled to 20 | Rebound plan/context/tasks to the exact digests above and retained unchanged CTO digest `636aa802…`. This restores implementation → verification disposition → commit ordering while preserving the Phase 5 split, two-gate exception, commit packets, and provider-controlled behavior |
| 2026-09-01 | current | current | Alignment-only Task 1.2/1.3 bootstrap correction: Task 1.2 now creates the standalone test project/lock/tests and executes a nonzero black-box red test run before Task 1.3 creates production | Rebound plan/context/tasks to the exact digests above and retained unchanged CTO digest `636aa802…`. Provider-controlled behavior, findings/mitigations, task IDs/count, phase count, later design, and commit packet paths/order are unchanged. Fresh clean revision-bound CTO approval is required for these bytes |
| 2026-09-01 | current | current | Planning revalidation for explicit user scope removal | Rebound the five-phase plan/context/tasks and unchanged CTO review; `IVSD-F005` / `IVSD-M005` resolved by scope removal, `F003/M003` narrowed to packet-cache and fixed persistent-goal state/status privacy, and `F004/M004` narrowed to Phases 3/5. The execution manifest remains intentionally stale/fail-closed pending clean CTO approval. |
| 2026-09-01 | current | current | Task 1.3 implementation-evidence and binding refresh after independent security review | Rebound unchanged plan, current context/tasks, and unchanged CTO review after the final targeted contract passed 8/8 and independent current-bytes review reported CLEAR with no blockers. No provider-controlled behavior, stakeholder, persisted-field, recovery, finding, mitigation, phase, task, or commit-packet mapping changed. `IVSD-F005/M005` remain resolved by scope removal; `IVSD-F008/M008` remain open and owned by Task 2.4. |
| 2026-09-01 | current | current | Phase 1 receipt and packet-binding correction | Recorded bounded `E009` decision evidence for `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED`, commit `eadeeabb4bd9745fef25bcb77dfdfab6c31844c1`, exact 20/20 paths, and empty index. Corrected Phase 2/3/4/5C packet path mappings so mutable state commits with the last-authored I-VSD report; 5A/5B remain intentionally excluded. Rebound exact plan/context/tasks hashes and unchanged CTO hash. Finding IDs, mappings, lifecycles, and dispositions remain unchanged; `IVSD-F008` stays open for Task 2.4. |

## Planning Handoff

- Workstream: agentic-workflow-control-plane
- Status: current / plan-aligned
- Reviewed input revision: `sha256:67b4bd5297641ba402a20994186235f1907b9d6d76b5d428833f0f9785857cd7` (repository evidence packet `E001`–`E008`; not an artifact hash; bounded lifecycle evidence `E009` is separately hashed)
- Reviewed plan revision: `sha256:c5b9c7fc9cad96df19521ca7f5192053b2c16a089d1149b25ab0570ca14fe301`
- Reviewed context revision: `sha256:e24f5fc439ae2297c1ed5c81077a11fa506a2e1c76c3aef4aeaa6e6d83379509`
- Reviewed tasks revision: `sha256:72bc82ac1eefaa4d79a692b785aa51beb28dd2c1b630c9dc86354419fb7a2dfb`
- Reviewed CTO review revision: `sha256:636aa802ddaede72f676db2e2c3d9eaf49fec0c92a36092cceb89cae18430561`
- Phase 1 decision evidence revision: `sha256:ae9bf05db592a9c2b13511898ae485a3315578dd32532f3e98383dc12723a961`
- Findings and mitigations: `IVSD-F001→IVSD-M001`, `IVSD-F002→IVSD-M002`, `IVSD-F003→IVSD-M003`, `IVSD-F004→IVSD-M004`, `IVSD-F005→IVSD-M005`, `IVSD-F006→IVSD-M006`, `IVSD-F007→IVSD-M007`, `IVSD-F008→IVSD-M008`
- Required plan mappings: Phase 1 / Tasks 1.1–1.3; Phase 2 / Tasks 2.1–2.4; Phase 3 / Tasks 3.1–3.3; Phase 4 / Tasks 4.1–4.3; Phase 5 / Tasks 5.1–5.4
- Open findings: `IVSD-F008` is lifecycle `open` and plan-declared only; it is mapped to Task 2.4 but has no implementation or test evidence yet
- Alignment disposition: 17 tasks, five phases, and seven planned commits; `IVSD-F005/M005` resolved by scope removal; Phase 1 verified and committed; `IVSD-F008/M008` remains open for Task 2.4
- Implementation evidence: `.omo/evidence/20260901-agentic-workflow-control-plane/task-1.3-green.md`; independent current-bytes review: `.omo/evidence/20260901-agentic-workflow-control-plane/task-1.3-code-review.md` (`CLEAR`, no blockers); bounded Phase 1 authorization/receipt: `E009`
- Escalations required before: fresh revision-bound Tier 1 approval and Phase 2 manifest authorship; any persisted-field expansion; any vendor-specific canonical state; any widening of the `IVSD-M008` break-glass beyond per-repair human authorization
- Refresh triggers: authority model, affected stakeholders, persisted machine-state fields, provider/model routing, shared-workspace recovery, adapter fail-closed behavior, or any mapped mitigation/task changes

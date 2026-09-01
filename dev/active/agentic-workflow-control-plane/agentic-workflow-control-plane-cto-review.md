<!-- ABOUTME: Senior CTO technical review of the agentic workflow control plane workstream. -->
<!-- ABOUTME: Binds the verdict to exact plan/context/tasks/I-VSD revisions and records blocking findings and required changes. -->

# Agentic Workflow Control Plane — Senior CTO Review

Last Updated: 2026-09-01 Europe/Brussels

## 1. Revision Binding

This verdict is bound to the following exact bytes. A change to any artifact
below invalidates this review.

| Artifact | Reviewed digest | Disposition |
|---|---|---|
| `agentic-workflow-control-plane-plan.md` | `sha256:42cb838253611bea9ecf929467e220ccb465573a7d7c29d4ffaf5b934f9957fd` | Reviewed |
| `agentic-workflow-control-plane-context.md` | `sha256:ce982a6d603870ba58c4c9507a919e0e13180f8884bc417393c8f608d66ffd29` | Reviewed |
| `agentic-workflow-control-plane-tasks.md` | `sha256:7bcfd08b1d7612b14925b1a40dada5bb1e0e8d197f70cfd4f3b502e61c649851` | Reviewed |
| `i-vsd-agentic-workflow-control-plane.md` | `sha256:9d770af9076e07b183ba4465ce68a2552eef26657b42f74eb5836f9d365605b3` | Reviewed |
| I-VSD stable evidence digest | `sha256:67b4bd5297641ba402a20994186235f1907b9d6d76b5d428833f0f9785857cd7` | Reviewed |

**I-VSD currency: VERIFIED CURRENT at review time.** The I-VSD report's
`Reviewed plan/context/tasks revision` bindings (report lines 17–20) match the
current artifact bytes exactly. Modification-time ordering is consistent
(plan 11:12:54 → context 11:17:12 → I-VSD 11:17:32); the report was authored
last against the final triad. All seven `IVSD-F001`–`IVSD-F007` findings and
`IVSD-M001`–`IVSD-M007` mitigations exist in the report and are mapped in plan
Section 9. Invariant 1 is satisfied for this revision.

## 2. Verdict

**APPROVE WITH REQUIRED CHANGES — implementation blocked until B1–B4 are resolved.**

The architecture is sound and I would fund this program. It is not approved to
start implementation in the reviewed revision. Four blocking findings must be
resolved first; three of them are safety or reviewability defects rather than
documentation gaps.

This is **not** a rejection and **not** a "split before approval" verdict. The
plan already right-sizes itself correctly (see Section 5). The blocking work is
bounded and does not require re-architecture.

### Approval boundary

- This verdict grants **technical readiness only** after B1–B4 close.
- It does **not** grant user approval, scholarly approval, or legal approval.
- It does **not** approve Phases 2–6 to start. Each phase retains its own
  readiness decision as the plan already requires.

## 3. What I Verified Versus What I Did Not

### Verified from repository bytes

- All four revision digests above, computed directly.
- I-VSD finding/mitigation ID existence and the plan Section 9 mapping.
- I-VSD refresh-trigger list (report line 343).
- Red-phase-first task ordering in all six phases.
- Commit packet completeness in all six phases.
- Absence of `- [ ]` task pollution and handoff prose in `plan.md`.

### Not verified — accepted as plan aspiration

- That `net10.0`, TUnit, and YamlDotNet are pinned as the plan claims
  (`Directory.Packages.props` not read this pass).
- That the current validator passes 23 intents / 14 scenarios.
- That `.agents/benchmarks/README.md` says 13 while the validator reports 14.
- That `.codex/hooks.json` contains workstation-absolute paths.
- That `.github/workflows/agent-context.yml` is absent.

None of these unverified claims change the verdict. All are Phase 1/5/6
implementation-time checks the plan already schedules.

## 4. Findings

Severity: **Blocking** prevents implementation start. **Moderate** must be fixed
within the owning phase. **Minor** is a correctness nit.

### B1 — Blocking — The declared "Worst Break Scenario" describes correct behavior

Plan Section 3's Worst Break is *"stale approval plus overlapping commit → both
gates fail closed, nothing is staged, both blockers reported."*

That is the safety system **succeeding**. A fail-closed refusal with no mutation
and a precise diagnostic is the happy path. Invariant 4 requires the single most
catastrophic failure mode, and the plan has not named it.

The real worst break is the inverse: **the Git adapter stages a path set that
captures another contributor's uncommitted work on shared `develop` and commits
it under this workstream's message.** The victim then reverts or resets to
recover, and unrecoverable dirty state is destroyed. Unlike a blocked commit,
this has no clean recovery.

This is not hypothetical. `context.md` states the tree *currently* holds
"extensive unrelated Setup Assistant and agent-context changes."

Compounding it: risk register row "Git adapter commits wrong paths" is rated
**Likelihood: Low**. On a tree that is presently dirty with unrelated multi-file
work, and with `git add` preceding every commit packet, Low is not defensible.

**Required:** Replace the Worst Break scenario with a true catastrophic-failure
scenario and re-rate that risk row upward.

### B2 — Blocking — Phase closure serializes behind a full build and test run

Two design elements combine into an unaddressed throughput and starvation cliff:

- Scenario 2C invalidates a verification receipt whenever HEAD moves.
- Phase 2 acceptance requires "an exclusive closure lock spans final
  verification receipt through commit inspection."

Read together, the global closure lock contains a Release solution build plus a
full test-project run — minutes of wall time — and every other agent's commit
invalidates any receipt produced outside it. With N concurrent agents this
degrades to serialized closure at best and livelock at worst.

**No task, acceptance criterion, or risk-register row addresses lock acquisition
fairness, wait bounds, timeout, queueing, or starvation.** This is the primary
operational failure mode of the design and it is currently invisible.

**Required:** Restructure the critical section so verification runs *outside*
the lock, and the lock holds only re-validation of expected HEAD, staging, and
commit. Add a bounded acquisition timeout, a declared maximum re-verification
attempt count, and a fixed diagnostic that hands off to human coordination on
exhaustion. Add a deterministic concurrent-closer test (barriers, no sleeps)
proving no indefinite starvation.

### B3 — Blocking — Phase 5 is oversized and destroys rollback granularity

Task 5.3 is self-rated **Effort: XL**. The Phase 5 commit packet carries **26
paths** and mixes five unrelated concerns in one commit:

1. New adapter (`AgentWorkflowHook.cs`)
2. Four harness config migrations (`.claude`, `.codex`, `.cursorrules`, Copilot)
3. New CI workflow plus `test.yml` change
4. Deletion of five hook files, the bootstrap validator, and two lock files
5. Four documentation updates plus `AGENTS.md`

Two separate defects follow:

- **Reviewability.** This matches right-sizing symptoms 1 and 3 (multi-intent
  scope; migration + contract churn + enablement in one big-bang phase). It also
  contradicts the project's atomic-commit standard: smallest independently
  reviewable behavior.
- **Rollback granularity — the more serious one.** The plan states obsolete
  surfaces are "deleted only after command/reference parity is green," but the
  packet stages the deletions in the **same commit** as the adapter that proves
  parity. No revision in history therefore contains both the new adapter and the
  old surfaces. Reverting the deletion necessarily reverts the replacement, so
  Phase 5's own rollback instruction ("restore the last known-good hook
  entrypoints from the previous commit") cannot be executed cleanly.

**Required:** Split Phase 5 into three commits — 5A adapter + harness configs +
architecture test, 5B CI lane, 5C deletion + reference migration — so a history
point exists where parity was proven with both surfaces present.

### B4 — Blocking — Criticality is under-rated at Tier 3

Plan Section 0 sets "Tier 3 / Domain State" from `ci-cd-change`, then immediately
concedes: *"Treat approval authority, Git mutation, path ownership, hook
enforcement, and telemetry as security-sensitive even though the current matched
intents are not Tier 1."*

A component that owns approval authority, mutates Git on a shared branch, and
enforces fail-closed safety gates **is** a security boundary. Declaring it
sensitive while classifying it Tier 3 selects a weaker review protocol, testing
strategy, and exploration budget than `criticality-guardrail` mandates — and
does so for precisely the surfaces that can destroy contributor work (B1).

Phase 1 Task 1.1 creates the `agent-workflow-control-plane` intent. That intent
is where the true tier must be declared.

**Required:** Declare Tier 1 (Security) in the new intent and in plan Section 0,
or document an explicit, reasoned exemption. "Sensitive but Tier 3" is not a
tenable position.

### M1 — Moderate — Self-hosting bootstrap hazard is unregistered

From Phase 2 onward the control plane governs the workflow that is building the
control plane. Plan Section 16 rule 4 states *"Never edit without the declared
path claim once Phase 2 is active."* A defect in the Phase 2 claim store
therefore blocks its own repair.

No risk row, escape hatch, or bootstrap-ordering rule covers this.

**Required:** Add a risk row and a documented, auditable break-glass procedure
for repairing the control plane when the control plane is the thing that is
broken.

### M2 — Moderate — Claim scope is checkout-local and never stated

Decision 3 stores claims under `.git/islamu-agent/`. This is coherent for the
single shared `develop` checkout the plan assumes, but the consequence is never
written down: **claims provide zero coordination across clones.** A self-hoster
or contributor running two checkouts gets silent non-coordination with no
warning — the tool will report a successful claim that protects nothing.

**Required:** State the checkout-local scope boundary explicitly in plan
Section 10 and Section 13, and surface it in `doctor` output.

### M3 — Moderate — The triad does not surface its own verifiable bindings

`plan.md`, `context.md`, and `tasks.md` each surface only:

```
I-VSD reviewed input revision: sha256:67b4bd52...
```

That value is the I-VSD **stable evidence digest**, not an artifact digest, and
the triad never defines its preimage. The verifiable plan/context/tasks bindings
exist only inside the I-VSD report.

Consequence: a reviewer reading `plan.md` alone cannot verify currency, and is
actively misled into thinking `67b4…` should hash one of the triad files. I made
exactly that error on my first pass through this workstream.

**Required:** Surface all four bindings in each triad Review State block, and
label `67b4…` as the stable evidence digest rather than an unqualified "input
revision."

### m1 — Minor — Task 6.2 title hardcodes the count its own acceptance forbids

Task 6.2 is titled "…verify **14-scenario** parity" while its acceptance
criterion states "Count derives from YAML." The plan's own evidence log flags
README=13 versus validator=14 as the exact drift being repaired. Pinning 14 in
the title reintroduces the defect.

### m2 — Minor — Task 1.3 edits the I-VSD report during schema implementation

Task 1.3's file list includes
`islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`.
Implementing a YAML schema and transition core should not modify the I-VSD
report. If the intent is a review-state refresh, that belongs in an explicit
step, not bundled into schema implementation.

## 5. Right-Sizing Decision (Invariant 8)

Symptom check against the reviewed revision:

| # | Symptom | Present |
|---|---|---|
| 1 | Multi-intent "and also" scope | Yes — six distinct capabilities |
| 2 | Exceeds reviewable task capacity | Yes — 18 tasks |
| 3 | Migration + contract churn + enablement in one phase | Yes — **Phase 5 only** |
| 4 | Backend slice ships independently of UI | N/A — no UI in scope |

Three symptoms match, which would normally force "split before approval."

**I am not issuing that verdict, because the plan already applied the remedy.**
Section 0 self-declares the XL trigger and commits to six independently approved
and committed increments, each with its own readiness decision, verification
gate, and commit packet. Six sequenced phase commits with independent approval
is functionally equivalent to six PRs, and the phase dependency chain is honest.

The right-sizing failure is **localized to Phase 5**, and B3 fixes it.

## 6. Grill-Me Stress Test

Questions put to the plan, with its answers and my disposition.

| # | Question | Plan's answer | Disposition |
|---|---|---|---|
| 1 | What happens when two agents close a phase simultaneously? | Exclusive closure lock | **Unresolved — B2.** No fairness, timeout, or starvation bound. |
| 2 | What is the unrecoverable failure? | Stale approval + overlap, fails closed | **Unresolved — B1.** That is success, not failure. |
| 3 | Can Phase 5 be rolled back? | "Restore last known-good hook entrypoints" | **Unresolved — B3.** Same commit deletes them. |
| 4 | Who repairs the control plane when it is broken? | Not addressed | **Unresolved — M1.** |
| 5 | What if HEAD moves during verification? | Receipt invalidated, re-verify | Resolved (Scenario 2C), but see B2 for cost. |
| 6 | What stops telemetry leaking source or PII? | Forbidden-field rejection before persistence, atomic journal | Resolved. Tests specified in 6.1. |
| 7 | What stops the executor self-approving? | Immutable approvals, no model invocation, `NeedsReplan` | Resolved. Strong. |
| 8 | What stops YAML becoming a fourth prose authority? | Machine-facts-only schema, parity validator, explicit stop-and-replan rule | Resolved. Section 18 is candid and correct. |
| 9 | Does the tool ever touch another contributor's work? | Blocks on mixed hunks; never stages broadly | Policy resolved; **residual risk under-rated — B1.** |
| 10 | Are claims safe across clones? | Not stated | **Unresolved — M2.** |

## 7. Credits — Verified Strengths

These are confirmed from the bytes, not assumed:

- **Red-phase-first ordering is correct in all six phases** (1.2→1.3, 2.1→2.2/2.3,
  3.1→3.2/3.3, 4.1→4.2/4.3, 5.1→5.2/5.3, 6.1→6.2/6.3). Invariant 6 satisfied.
- **All six commit packets are complete and self-sufficient** per Invariant 10:
  title, description, changelog treatment, trailers, explicit path list,
  pre-commit inspection commands, staging command, path-limited commit command,
  post-commit verification, and an override field.
- **`git commit --only -m … -- <paths>` is the correct primitive** for shared
  `develop`: it commits exactly the named paths and preserves unrelated staged
  state. This is careful, correct work.
- **Greenfield posture is right.** Decision 7 and non-goal 7 delete obsolete
  surfaces after parity instead of keeping deprecated aliases.
- **Test discipline is right.** Barriers and temporary Git repositories, no
  sleeps, "public CLI/domain seams only; no raw source/prose assertions" — this
  directly avoids mock-mirroring and source-scraping bloat.
- **Artifact separation is clean.** No `- [ ]` checklists or session handoffs in
  `plan.md`; handoffs correctly isolated in `context.md`.
- **Evidence discipline is high.** The Section 2.1 evidence log carries per-claim
  confidence ratings, and Section 18 names the real risk (a second source of
  truth) rather than a comfortable one.

## 8. Required Changes Before Implementation

| ID | Change | Owning artifact |
|---|---|---|
| B1 | Rewrite Worst Break as a true catastrophic scenario; re-rate wrong-path commit risk | `plan.md` §3, §14 |
| B2 | Move verification outside the closure lock; add timeout, bounded retry, starvation test | `plan.md` §6 Phase 2, §14; `tasks.md` Phase 2 |
| B3 | Split Phase 5 into 5A/5B/5C with separate commit packets | `plan.md` §6; `tasks.md` Phase 5 |
| B4 | Declare Tier 1 criticality in the new intent and §0 | `plan.md` §0; `tasks.md` Task 1.1 |
| M1 | Add self-hosting bootstrap risk and break-glass procedure | `plan.md` §14, §12 |
| M2 | State checkout-local claim scope; surface in `doctor` | `plan.md` §10, §13 |
| M3 | Surface all four revision bindings in each Review State block | all three triad files |
| m1 | Remove hardcoded "14" from Task 6.2 title | `tasks.md` |
| m2 | Remove the I-VSD report from Task 1.3's file list | `tasks.md` |

## 9. I-VSD Consequence Of This Review

The I-VSD refresh triggers (report line 343) include **shared-workspace
recovery**, **authority model**, **adapter fail-closed behavior**, and **any
mapped mitigation/task changes**.

Changes B1, B2, B3, B4, M1, and M2 each touch at least one of those triggers.

Therefore, per the reviewer invariant on rewrites:

1. Applying these changes **marks the I-VSD report stale**. Its
   `Reviewed plan/context/tasks revision` digests will no longer match.
2. **This review cannot approve the rewritten revision.** A fresh I-VSD pass and
   a second CTO pass bound to the new digests are required before implementation.
3. The correct sequence is: apply required changes → refresh I-VSD → second CTO
   review → user approval → Task 1.1.

This is expected and correct, not a regression. The alternative — leaving B1–B4
unresolved to preserve a green binding — would be approving a plan whose worst
failure mode is undocumented and whose closure protocol can starve.

## 10. Review Metadata

- **Reviewer:** Senior CTO review pass (`senior-cto-feedback`)
- **Mode:** Review plus explicitly requested rewrite
- **Verdict:** Approve with required changes; implementation blocked on B1–B4
- **Approval granted:** Technical readiness only, contingent and revision-bound
- **Not granted:** User approval, scholarly approval, legal approval, Phase 2–6 start

---

# Second Pass — Post-Rewrite Review

Last Updated: 2026-09-01 Europe/Brussels

## 11. Second-Pass Revision Binding

| Artifact | Reviewed digest | Disposition |
|---|---|---|
| `…-plan.md` | `sha256:b1b19475cd0c0497c53e8b5e3a0175c65cb43935584fabba020175a7b205e1d5` | Reviewed |
| `…-context.md` | `sha256:dae5d3e2ed7156cf4d0b12f648428ce54639511956d867320e28beb2b064a6c1` | Reviewed |
| `…-tasks.md` | `sha256:4d8db86384f7326a65a126a2614d96de5e9c65fbd6557d07337d44dd99d23be7` | Reviewed |
| `i-vsd-…-control-plane.md` | `sha256:694e4b46c116506f3b1b8a894e5bc934f714ef328a771c4c721bee1bba1afc7e` | Reviewed |

**I-VSD currency: VERIFIED CURRENT.** The report's bound plan/context/tasks
digests match the reviewed bytes exactly. Status `current`, disposition
`plan-aligned`, lifecycle records the full `current → stale → current`
transition, and `IVSD-F008` / `IVSD-M008` are mapped to Task 2.4 in plan
Section 9. Invariant 1 is satisfied.

## 12. Second-Pass Verdict

**CHANGES REQUIRED — not approved. One blocking internal contradiction.**

B1, B2, B3, and B4 from the first pass are **closed**, verified against the
current bytes. However, the rewrite that closed B3 introduced a new defect that
makes Phase 5's own parity guarantee unachievable as sequenced. That defect
originated in the first-pass rewrite, which is precisely why a second pass
bound to fresh digests exists.

### First-pass findings — closure status

| ID | Status | Evidence in reviewed bytes |
|---|---|---|
| B1 worst-break | **Closed** | §3 names silent capture of foreign uncommitted work with five defenses; risk re-rated to High |
| B2 closure livelock | **Closed** | Phase 2 acceptance moves verification outside the lock, adds timeout, bounded re-verification, starvation test in Task 2.1 |
| B3 Phase 5 oversized | **Closed with a new defect — see C1** | Split into increments 5A/5B/5C; path parity 7+3+16 = original 26 |
| B4 criticality | **Closed** | §0 declares Tier 1 / Security; Task 1.1 acceptance requires `criticality.tier: 1` |
| M1 self-hosting | **Closed** | §12 break-glass; risk row added |
| M2 claim scope | **Closed** | §10 states checkout-local scope; `doctor` must surface it |
| M3 bindings | **Closed by an accepted deviation — see note** | Triad points at the I-VSD as authoritative and warns `67b4…` is not an artifact hash |
| m1 hardcoded count | **Closed** | Task 6.2 title now says "registry-derived scenario parity" |
| m2 I-VSD in Task 1.3 | **Closed** | Removed from Task 1.3 file list |

**Note on M3.** The literal instruction ("surface all four bindings in each triad
Review State block") is unsatisfiable: a file cannot contain its own hash, and
every triad edit invalidates the copy. The applied resolution — triad points to
the I-VSD `Review Metadata` as the single authoritative source, and explicitly
warns that `67b4…` is an evidence-packet digest that will never match a triad
file — addresses the actual defect M3 identified. Accepted as closed.

## 13. Second-Pass Findings

### C1 — Blocking — Phase 5 parity is claimed but never executed in the state that proves it

Plan Phase 5 asserts: *"A revision exists in history where the new adapter and
the old surfaces are both present and parity is proven, so 5C is revertible
without reverting 5A."*

The first half holds — `git commit --only -- <paths>` limits increment 5A's tree
to its seven paths, so commit 5A does contain the adapter with the old surfaces
still present. **The second half does not.** Task 5.4 deletes the obsolete hook
scripts and validator from the working tree, and Phase 5 verification runs
**once after tasks 5.1–5.4**. The Release build and `Event.Architecture.Tests`
run therefore execute *after* deletion. Parity is asserted by commit-tree
construction, never by an executed test in the both-present state.

The contradiction is explicit in the ledger's own text. The Phase 5 verification
checkbox reads:

> "Confirm hook/CI doctor and new workflow dry-run pass **before obsolete files
> are absent**."

Task 5.4 has already removed them by the time that checkbox is reached. The
instruction cannot be satisfied as sequenced.

This also weakens `IVSD-M006`, which requires proving parity *while the old
surfaces are still present*.

**Required:** Give Phase 5 two verification gates — 5-I after tasks 5.1–5.3 with
the old surfaces present (gating commits 5A and 5B), and 5-II after task 5.4
(gating commit 5C). This needs an explicit, documented exception to the
one-build-one-test-per-phase rule, because Phase 5 is structurally two phases.

### C2 — Moderate — Break-glass has a task but no phase acceptance criterion

`grep -c "break-glass"` across plan Section 6's entire Phase 2 block returns
**0**. Task 2.4 exists and `IVSD-F008` maps to it, but the phase's own definition
of done never mentions it. A Tier 1 authority surface must not depend on a task
line alone for its acceptance gate.

**Required:** Add break-glass criteria to Phase 2 acceptance in plan Section 6.

### C3 — Moderate — No behavioral scenario covers the break-glass

Requirement 2 carries Scenarios 2A, 2B, and 2C only. Plan Section 3 is the
behavioral contract, and Invariant 6 requires invariant tests bound to *named*
scenarios. The break-glass — a fail-closed authority bypass on a Tier 1 surface —
has acceptance prose in Task 2.4 but no GIVEN/WHEN/THEN anchor.

**Required:** Add Scenario 2D covering authorized bypass, unauthorized/self-service
refusal, and the invariant that ownership and staged-set parity stay enforced.

### C5 — Moderate — Plan and I-VSD disagree on the break-glass receipt field

Plan Section 12 still specifies a receipt *"carrying the authorizing reason"* —
free-form text. `IVSD-M003` and `IVSD-M008` now require a **bounded enumerated
reason code**, precisely because a free-form audit field would reopen the
free-form-payload hole the telemetry boundary forbids everywhere else.

**Required:** Align plan Section 12 to the enumerated reason code.

### C6 — Minor — Risk-register owner predates Task 2.4

The self-repair risk row lists owner `2.2, all later phases`. The break-glass is
now owned by Task 2.4.

### Verified clean

Task 2.4's four files (`ClaimCommands.cs`, `FileClaimStore.cs`,
`SharedDevelopCoordinatorTests.cs`, `docs/AGENTIC_CONTEXT_ENGINEERING.md`) are
**all** already present in the Phase 2 commit packet. No packet change is needed
and no path was orphaned by adding the task.

## 14. I-VSD Consequence Of The Second Pass

C1, C2, C3, C5, and C6 are **alignment of plan/tasks to the already-revalidated
I-VSD**, not changes to provider-controlled behavior:

- C1 makes the plan actually perform the parity proof `IVSD-M006` already demands.
- C2 and C3 document behavior `IVSD-M008` already requires.
- C5 *removes* a telemetry-field discrepancy rather than introducing one.

The integration contract states that "architecture details that preserve
provider-controlled behavior do not invalidate the report." These corrections
preserve it. **Therefore these changes do not trigger an I-VSD content refresh.**
Only the bound digests change, which is handled by a rebind lifecycle row of the
kind this report already uses ("mappings unchanged").

## 15. Second-Pass Review Metadata

- **Reviewer:** Senior CTO review, second pass
- **Mode:** Review bound to refreshed digests, plus correction of C1–C6
- **Verdict:** Changes required — C1 blocking
- **Approval granted:** None. Approval requires a clean third pass that makes no edits
- **Not granted:** User approval, scholarly approval, legal approval, implementation start

---

# Third Pass — Consistency Audit

Last Updated: 2026-09-01 Europe/Brussels

## 16. Third-Pass Findings

C1–C6 verified closed against the current bytes: Phase 5 carries both gates, the
maintenance-rule exception is recorded and scoped, Phase 2 acceptance covers the
break-glass, Scenario 2D exists, the enumerated reason code appears in three
places, two risk rows are owned by Task 2.4, and Task 5.4 carries its stop-guard.

Two defects remained, both caused by the C1 fix not propagating to summary tables.

### D1 / D2 — Moderate — Summary verification tables contradicted the two-gate structure

Plan Section 7's phase verification matrix still listed Phase 5 as a single gate.
Context's Validation Baseline did the same and added *"Each command runs once
after all tasks in its phase"* — false for Phase 5 after C1.

An executor consulting either summary rather than Section 6 would have run one
gate after deletion, silently voiding the executed parity proof `IVSD-M006`
requires. That is the exact failure C1 was raised to prevent, reintroduced
through a stale summary.

**Root cause, and why the fix is not just "update both tables":** these tables
restate what Section 6 already declares. Duplicated authority is precisely the
failure mode the plan warns about in Section 18 — a second source of truth that
drifts. Both tables are now marked explicitly as summaries with Section 6 (and
the task ledger) authoritative, so a future divergence is defined as a defect in
the table rather than an ambiguity between two equal claims.

**Applied.** Both tables now show Gate 5-I and Gate 5-II as separate rows and
carry an explicit precedence statement.

## 17. Convergence And Reviewer-Independence Limitation

This is the third consecutive pass in which the reviewer also authored the
corrections. Each pass has found real defects — C1 was a genuine blocking error,
D1/D2 were genuine propagation failures — so the passes have not been theatre.
But the loop has a structural weakness that should be stated plainly rather than
hidden behind a clean verdict:

**The author and the reviewer are the same agent.** Independence is the main
thing that makes a CTO review worth having, and this review does not have it. The
defects found were mechanical and self-inflicted; a genuinely independent
reviewer is more likely to challenge the *architecture* — whether a bespoke
control plane is the right answer at all, whether six phases is the right
decomposition, whether `.git/`-local coordination is wise.

Recommendation: before implementation begins on a Tier 1 surface, obtain one
independent review pass from a different agent or human. That is a stronger
control than a fourth self-review.

## 18. Third-Pass Metadata

- **Reviewer:** Senior CTO review, third pass
- **Mode:** Consistency audit plus correction of D1/D2
- **Verdict:** Changes required — D1/D2 applied; no design defect found
- **Residual risk:** Low and mechanical. No unresolved design or safety finding remains across all three passes
- **Approval granted:** None from this pass, because it made edits
- **Not granted:** User approval, scholarly approval, legal approval, implementation start

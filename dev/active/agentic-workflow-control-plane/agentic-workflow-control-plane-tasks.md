<!-- ABOUTME: Executable five-phase task ledger for the agentic workflow control plane. -->
<!-- ABOUTME: Keeps implementation, verification, shared-workspace ownership, exact commit packets, and review state synchronized. -->

# Agentic Workflow Control Plane — Task Checklist

Last Updated: 2026-09-01 Europe/Brussels

## Status Summary

- **Overall status:** Phase 1 is verified complete and committed as `eadeeabb4bd9745fef25bcb77dfdfab6c31844c1`; no Phase 2 task has started.
- **Completed:** 3/17 implementation tasks and 1/7 planned commits across five phases.
- **Current priority:** Obtain fresh revision-bound Tier 1 approval for the corrected plan/context/tasks/I-VSD packet.
- **Next recommended slice:** After that approval, author the Phase 2 execution manifest binding and begin Task 2.1; do not mutate the existing manifest before approval.
- **Review state:**
  - I-VSD report: `../../../islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
  - I-VSD stable evidence digest: `sha256:67b4bd5297641ba402a20994186235f1907b9d6d76b5d428833f0f9785857cd7` — digest of the evidence packet `E001`–`E008`, **not** a hash of any triad file
  - Authoritative artifact bindings: see the I-VSD report's `Review Metadata`, authored last and the single source of truth for currency
  - I-VSD status / disposition: current / plan-aligned, revalidated after the CTO rewrite; `IVSD-F008` / `IVSD-M008` added and mapped to Task 2.4
  - CTO review: Historical review retained unchanged; all recorded findings are applied. Independent Phase 1 current-bytes technical review is CLEAR at `.omo/evidence/20260901-agentic-workflow-control-plane/task-1.3-code-review.md`; the corrected planning packet needs fresh revision-bound Tier 1 approval before Phase 2.
  - User approval: Phase 1 implementation/commit authority and bounded whole-file capture authorization were exercised. Decision `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED` applies only to `.agents/contract/intents.yaml` and `docs/AGENTIC_CONTEXT_ENGINEERING.md`; later-phase readiness remains separate.

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on cold resume, read context and the current task, then only referenced plan headings.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `IN PROGRESS` only when it will span meaningful work or a handoff.
- Check a substantial task immediately after its acceptance criteria are met; reconcile small related tasks before phase exit.
- Update completed count, current priority, next slice, deferred work, and date whenever task state changes.
- Check a phase complete only after implementation tasks, verification disposition, commit packet execution, and receipt reconciliation succeed.
- Each phase requires a fresh readiness decision; later phases are not implicitly approved with Phase 1.
- Do not run the phase-end Release build or full-project test until every implementation task in that phase is complete.
- Targeted TUnit `--treenode-filter` slices are allowed only where a Red/Green task explicitly requires its failing anchor and green confirmation.
- Run one Release build and at most one selected project test per phase. **Phase 5 is the single documented exception** and runs two gates (5-I before deletion, 5-II after), because its parity proof must execute while the obsolete surfaces are still present. No other phase may claim this exception.
- Never start product hosts, browsers, Docker, Aspire, Playwright, or external services for a phase gate.
- Work only on shared `develop`; no worktrees, checkout, stash, reset, cleanup, force, history rewrite, or broad staging.
- Never modify, unstage, stage, commit, or reclaim another contributor's work.
- Record phase-attributable failures separately from proven unrelated failures and never call the repository green when an external failure remains.
- Use the planned self-sufficient commit packet without loading `conventional-commit` when truthful.
- Load `conventional-commit` only for a permitted material divergence and record complete `Actual commit contracts` before committing.
- Update context after a phase, blocker, decision, failed validation, material discovery, interruption, or handoff.
- Update the plan only for scope, architecture, phase order, acceptance, risk, or validation changes.
- Revalidate I-VSD after provider authority, stakeholder, persisted machine-state fields, recovery, or mapped-task changes.
- Any future packet that commits this tasks ledger, workstream context, or execution state MUST also commit `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`, authored last against the exact settled plan/context/tasks bytes. Increments 5A/5B intentionally defer mutable-state reconciliation to 5C.

## Program Approval Boundary

- This is an XL program split into five delivery phases closing as seven commits (Phase 5 closes as increments 5A, 5B, and 5C per CTO finding B3).
- User approval of the program does not waive a fresh Phase 2–5 readiness decision.
- Senior CTO review MUST bind to exact plan, tasks, and I-VSD revisions.
- Phase 1 MUST establish the dedicated intent before any path outside the current composite allow-lists is edited.

## Phase 1: Typed Workstream Contract And Tool Foundation — COMPLETE

**Phase-owned paths:** exactly the paths in the planned commit contract below.

- [x] **1.1 Add the dedicated agent-workflow-control-plane intent and verify the current contract validator accepts its exact surface**
  - **Files:** `.agents/contract/intents.yaml`; `.agents/contract/README.md`
  - **Acceptance:** The new recurring intent **declares `criticality.tier: security` (Tier 1)** per CTO finding B4, authorizes every planned Phase 1–5 `.agents`, `eng/agent-workflow`, test, harness, CI, documentation, solution, I-VSD, and active-manifest path, names exact build/test/validator commands, carries the highest required review/docs/forbidden constraints, and passes the current contract validator before any new tool/test path is edited. Tier 1 is required because this surface owns approval authority, mutates Git on a shared branch, and can destroy another contributor's uncommitted work; it selects adversarial Invariant-Breaker testing and the Tier 1 review protocol.
  - **Execution evidence:** Verified complete before Task 1.2 bootstrap; the dedicated intent exists and the bounded contract validator accepted it.
  - **Effort:** M
  - **Dependencies:** Approved workstream revision.
  - **Guidance:** This is the bounded bootstrap gate, not implementation of the control plane.

- [x] **1.2 Bootstrap the standalone test project, author failing workstream contract and transition invariants, and verify an executable red failure before production implementation**
  - **Files:** `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj` (new); `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/packages.lock.json` (generated); `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/WorkstreamContractTests.cs` (new)
  - **Acceptance:** The standalone test project references no product project and no future control-plane source project. Tests cover valid current approval, stale digest, missing commit authority, illegal transition, incomplete packet, unknown field, unsafe path, and expected-HEAD mismatch through the future control-plane CLI/schema's public black-box process/file seams. They use deterministic temporary inputs, launch processes with `ProcessStartInfo.ArgumentList`, and await the exact process-exit completion signal with a bounded timeout; they use no sleeps or polling. Assertions target machine-consumed exit codes, diagnostic codes, and schema/CLI outputs, never raw source or prose. Run exactly `dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/WorkstreamContractTests/*"` once and record a failing disposition with a nonzero executed-test count. The tests must compile and execute now, failing because the production project/schema/CLI/commands/diagnostics are absent or nonconforming — never because the test project, lock file, test discovery, or other test infrastructure is absent.
  - **Effort:** L
  - **Dependencies:** 1.1.
  - **Guidance:** Reuse centrally pinned TUnit; no new dependency and no source-project reference.
  - **Execution evidence:** Verified complete: the standalone black-box test project and executable failing-first public-contract slice were established before production implementation.

- [x] **1.3 Implement the typed schema, transition core, YAML store, standalone CLI/project integration, and first execution manifest and verify the same Task 1.2 tests turn green**
  - **Files:** `.agents/contract/workstream.schema.json`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/ISLAMU.AgentWorkflow.csproj`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Program.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkstreamExecution.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ValidateWorkstreamCommand.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/YamlWorkstreamStore.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/packages.lock.json`; `Explore.slnx`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`; `docs/AGENTIC_CONTEXT_ENGINEERING.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-plan.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`
  - **Acceptance:** Machine-only schema owns digests, approvals, phase DAG/state, paths, packets, expected HEAD, and receipts. Illegal transitions return typed errors; malformed input fails closed. The production tool references no product project; the already-created test project remains standalone and black-box with no source-project reference. `validate-workstream` binds all artifacts and authority. Rerun the exact targeted Task 1.2 command and record the same nonzero tests green without weakening their behavioral assertions. The old validator remains bootstrap-only until Phase 5.
  - **Effort:** L
  - **Dependencies:** 1.2.
  - **Guidance:** Reuse centrally pinned YamlDotNet; no new dependency.
  - **Execution evidence:** 2026-09-01 — complete. The final exact targeted public-contract run passed 8/8 after test-first Windows-drive, transition-scope, canonical-schema, bounded YAML/artifact, symlink/reparse, oversized-input, and non-regular-artifact hardening. Final LSP diagnostics were clean; real CLI adversarial QA returned the expected typed failures without hanging. Executor evidence: `.omo/evidence/20260901-agentic-workflow-control-plane/task-1.3-green.md`. Independent current-bytes review: `.omo/evidence/20260901-agentic-workflow-control-plane/task-1.3-code-review.md` (`codeQualityStatus: CLEAR`, no blockers). The remaining same-user pathname/content TOCTOU fence is owned by Phase 2 and does not block this read-only Phase 1 validator.

### Phase 1 Verification — RUN ONCE AFTER TASKS 1.1–1.3

- [x] Run `dotnet build --configuration Release --verbosity quiet` and record exit code 0 or exact unrelated failure evidence. **Receipt:** exit 0, 8,185 solution warnings and 0 errors; only one warning record was retained, so no unsupported per-path attribution or wholly-green repository claim is made.
- [x] Run `dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet` and record passing nonzero tests. **Receipt:** 8/8 passed.
- [x] Confirm no phase-attributable failure remains and the Phase 1 packet still matches the authorized path disposition. **Receipt:** exact 20/20 committed paths; whole-file capture of the two fixed mixed paths was explicitly authorized under `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED`.

### Phase 1 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `build(architecture): define executable agent workstream contracts`
- **Default description:** Add revision-bound workflow state, approval bindings, phase packets, and a standalone validator foundation without changing product runtime behavior.
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:**
  - `Changelog: skip`
  - `Changelog-Reason: internal agent workflow contract and tooling foundation`
- **Commit paths:**
  - `.agents/contract/intents.yaml`
  - `.agents/contract/workstream.schema.json`
  - `.agents/contract/README.md`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/ISLAMU.AgentWorkflow.csproj`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Program.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkstreamExecution.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ValidateWorkstreamCommand.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/YamlWorkstreamStore.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/packages.lock.json`
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj`
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/WorkstreamContractTests.cs`
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/packages.lock.json`
  - `Explore.slnx`
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md`
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-plan.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-cto-review.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:**
  ```bash
  git add -- .agents/contract/intents.yaml .agents/contract/workstream.schema.json .agents/contract/README.md eng/agent-workflow/src/ISLAMU.AgentWorkflow/ISLAMU.AgentWorkflow.csproj eng/agent-workflow/src/ISLAMU.AgentWorkflow/Program.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkstreamExecution.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ValidateWorkstreamCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/YamlWorkstreamStore.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/packages.lock.json eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/WorkstreamContractTests.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/packages.lock.json Explore.slnx docs/AGENTIC_CONTEXT_ENGINEERING.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-plan.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-cto-review.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml
  ```
- **Commit command:**
  ```bash
  git commit --only -m "build(architecture): define executable agent workstream contracts" -m "Add revision-bound workflow state, approval bindings, phase packets, and a standalone validator foundation without changing product runtime behavior." -m "Changelog: skip" -m "Changelog-Reason: internal agent workflow contract and tooling foundation" -- .agents/contract/intents.yaml .agents/contract/workstream.schema.json .agents/contract/README.md eng/agent-workflow/src/ISLAMU.AgentWorkflow/ISLAMU.AgentWorkflow.csproj eng/agent-workflow/src/ISLAMU.AgentWorkflow/Program.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkstreamExecution.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ValidateWorkstreamCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/YamlWorkstreamStore.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/packages.lock.json eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/WorkstreamContractTests.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/packages.lock.json Explore.slnx docs/AGENTIC_CONTEXT_ENGINEERING.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-plan.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-cto-review.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml
  ```
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [x] Execute the packet without loading `conventional-commit` if truthful; otherwise record complete actual packets before commit. **Receipt:** default packet committed as `eadeeabb4bd9745fef25bcb77dfdfab6c31844c1` from parent `1e2a4d20fae97857e10bacdb24802b66e287cf80`.
- [x] Verify committed paths equal `Commit paths` and record the hash/receipt. **Receipt:** exact 20/20 parity and empty post-commit index. Bounded authorization evidence: `.omo/evidence/20260901-agentic-workflow-control-plane/phase-1-whole-file-authorization.md` (`sha256:ae9bf05db592a9c2b13511898ae485a3315578dd32532f3e98383dc12723a961`).
- **Phase 2 entry gate:** Fresh revision-bound Tier 1 approval for the corrected packet is still required before authoring the Phase 2 execution manifest.

## Phase 2: Shared-Develop Claims And Fenced Phase Closure — NOT STARTED

**Phase-owned paths:** exactly the paths in the planned commit contract below.

- [ ] **2.1 Author deterministic claim, overlap, mixed-hunk, HEAD-race, foreign-work-capture, starvation, and interrupted-close tests and verify red failure**
  - **Files:** `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/SharedDevelopCoordinatorTests.cs` (new)
  - **Acceptance:** Tests use barriers/events and temporary Git repositories; no sleeps. They prove disjoint success, overlap refusal, generation fencing, mixed-hunk blocking, moved-HEAD invalidation, unrelated staged preservation, and uncertain interruption. **Worst-break coverage (CTO finding B1):** a temporary repository seeded with a foreign contributor's uncommitted file proves the preventive controls: directory paths and globs fail as non-literal, symlink and case/NFC aliases fail normalized ownership, and a staged set exceeding the planned packet aborts before commit. A separate post-commit divergence case proves detection/containment stops further automation without claiming prevention, while recovery output contains no tool-initiated destructive command. **Starvation coverage (CTO finding B2):** N concurrent closers each either close or block with a fixed diagnostic within the maximum **3 re-verification attempts**; lock acquisition defaults to **30 seconds**. Tests inject shorter deterministic bounds through an approved test manifest and exact barriers/events, never sleeps.
  - **Effort:** L
  - **Dependencies:** Verified Phase 1 commit and readiness.

- [ ] **2.2 Implement generation-fenced claims and local claim storage and verify ownership tests**
  - **Files:** `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkspaceClaim.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClaimCommands.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/FileClaimStore.cs`
  - **Acceptance:** One mutator per normalized path, heartbeat/release, no automatic dirty takeover, bounded diagnostics, and `.git/islamu-agent` storage.
  - **Effort:** L
  - **Dependencies:** 2.1.

- [ ] **2.3 Implement verify-and-close Git coordination and recovery receipts and verify exact commit-tree behavior**
  - **Files:** `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/PhaseClosure.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClosePhaseCommand.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/GitWorkspaceCoordinator.cs`; `docs/AGENTIC_CONTEXT_ENGINEERING.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`; `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
  - **Acceptance:** `ProcessStartInfo.ArgumentList`, expected-HEAD/hash checks, packet parity, exact commit tree, uncertain-result inspection, no worktree/stash/reset/cleanup. **Lock structure (CTO finding B2):** verification executes outside the exclusive lock; the lock spans only expected-HEAD re-validation, staged-set parity, commit, and post-commit inspection — never a build or test run. Lock acquisition defaults to **30 seconds** and never force-breaks a live lock. HEAD movement permits at most **3 re-verification attempts**, then blocks with a fixed diagnostic requesting human coordination. Both values are machine-owned schema/manifest facts configurable only by a revision-bound approved manifest; tests inject shorter deterministic values without sleeps. **Path safety (CTO finding B1):** literal file lists, normalized ownership resolution, and pre-commit staged-set parity are preventive blockers. Post-commit tree comparison is detection/containment only and stops further automation; it does not prevent initial capture. The tool never runs `revert`, `reset`, `checkout`, `stash`, or `clean`; human recovery commands limit secondary harm only.
  - **Effort:** L
  - **Dependencies:** 2.2.

- [ ] **2.4 Implement the authorized break-glass repair path and verify it cannot suspend protections for others' work**
  - **Files:** `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClaimCommands.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/FileClaimStore.cs`; `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/SharedDevelopCoordinatorTests.cs`; `docs/AGENTIC_CONTEXT_ENGINEERING.md`
  - **Acceptance:** Implements `IVSD-M008`. The bypass suspends **claim acquisition only**; tests prove path-ownership validation, staged-set parity, literal path lists, and the prohibition on tool-initiated `revert`/`reset`/`checkout`/`stash`/`clean` all remain enforced while it is active. Each use requires explicit human authorization and can never be self-invoked by an executor. Every invocation writes a receipt carrying a **bounded enumerated reason code** — never free-form text, per `IVSD-M003`. Authorization is scoped to one repair, never to a session or standing grant. `status` and `doctor` surface active and recent bypasses.
  - **Effort:** M
  - **Dependencies:** 2.2.
  - **Guidance:** Closes `IVSD-F008`, which exists because the control plane governs its own repair from Phase 2 onward. Routine break-glass use is evidence of a claim-store design defect and must trigger re-planning, not a wider bypass.

### Phase 2 Verification — RUN ONCE AFTER TASKS 2.1–2.4

- [ ] Run the Release solution build once and record the disposition.
- [ ] Run the full `ISLAMU.AgentWorkflow.Tests` project once and record passing nonzero tests.
- [ ] Confirm the shared-tree packet is wholly owned and no unrelated failure was repaired.

### Phase 2 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `build(architecture): fence shared develop phase closure`
- **Default description:** Add generation-fenced path claims, expected-HEAD verification receipts, and exact path-limited phase closure while preserving unrelated dirty state.
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: internal shared-workspace safety control`
- **Commit paths:**
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkspaceClaim.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/PhaseClosure.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClaimCommands.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClosePhaseCommand.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/GitWorkspaceCoordinator.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/FileClaimStore.cs`
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/SharedDevelopCoordinatorTests.cs`
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:**
  ```bash
  git add -- eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkspaceClaim.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/PhaseClosure.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClaimCommands.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClosePhaseCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/GitWorkspaceCoordinator.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/FileClaimStore.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/SharedDevelopCoordinatorTests.cs docs/AGENTIC_CONTEXT_ENGINEERING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Commit command:**
  ```bash
  git commit --only -m "build(architecture): fence shared develop phase closure" -m "Add generation-fenced path claims, expected-HEAD verification receipts, and exact path-limited phase closure while preserving unrelated dirty state." -m "Changelog: skip" -m "Changelog-Reason: internal shared-workspace safety control" -- eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkspaceClaim.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/PhaseClosure.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClaimCommands.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClosePhaseCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/GitWorkspaceCoordinator.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/FileClaimStore.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/SharedDevelopCoordinatorTests.cs docs/AGENTIC_CONTEXT_ENGINEERING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Execute or replace the packet under the override rules.
- [ ] Verify exact paths/receipt and obtain Phase 3 readiness approval.

## Phase 3: Content-Addressed Decision And Execution Packets — NOT STARTED

**Phase-owned paths:** exactly the paths in the planned commit contract below.

- [ ] **3.1 Author packet freshness, exact-heading, duplicate-byte, and budget invariant tests and verify red failure**
  - **Files:** `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ContextPacketCompilerTests.cs` (new)
  - **Acceptance:** Tests cover current packet, stale digest, missing heading, duplicate unchanged content, registry overread, oversized source, bounded handle fallback, and forbidden packet-cache fields.
  - **Effort:** L
  - **Dependencies:** Verified Phase 2 commit and readiness.

- [ ] **3.2 Implement content-addressed packet domain, bounded heading reader, and local cache and verify Task 3.1**
  - **Files:** `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/ContextPacket.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/BuildContextPacketCommand.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/ContentAddressedPacketStore.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/MarkdownSectionReader.cs`
  - **Acceptance:** Cache key includes path/heading/hash, stale handles reject, outputs are deterministic, exact source retrieval remains through normal tools, and no new parser dependency is added.
  - **Effort:** L
  - **Dependencies:** 3.1.

- [ ] **3.3 Bind packet budgets and planning/review/execution consumption and verify cold-resume packet**
  - **Files:** `.agents/CONTEXT_ENGINEERING.md`; `.agents/benchmarks/cold-start-tasks.yaml`; `docs/AGENTIC_CONTEXT_ENGINEERING.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`; `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
  - **Acceptance:** Existing byte, duplicate, scout, and registry limits are enforced; implementation receives current task/decisions/rules/paths/tests/hashes only; provider token fields remain optional metadata.
  - **Effort:** M
  - **Dependencies:** 3.2.

### Phase 3 Verification — RUN ONCE AFTER TASKS 3.1–3.3

- [ ] Run the Release solution build once and record the disposition.
- [ ] Run `ISLAMU.AgentWorkflow.Tests` once and record passing nonzero tests.
- [ ] Confirm packet bytes/duplicates meet declared limits and the packet cache persists no sensitive content.

### Phase 3 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `build(architecture): compile bounded agent execution packets`
- **Default description:** Compile revision-valid task packets from exact headings and content hashes while enforcing repository context budgets and zero duplicate unchanged bytes.
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: internal agent context packet optimization`
- **Commit paths:**
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/ContextPacket.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/BuildContextPacketCommand.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/ContentAddressedPacketStore.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/MarkdownSectionReader.cs`
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ContextPacketCompilerTests.cs`
  - `.agents/CONTEXT_ENGINEERING.md`
  - `.agents/benchmarks/cold-start-tasks.yaml`
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:**
  ```bash
  git add -- eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/ContextPacket.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/BuildContextPacketCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/ContentAddressedPacketStore.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/MarkdownSectionReader.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ContextPacketCompilerTests.cs .agents/CONTEXT_ENGINEERING.md .agents/benchmarks/cold-start-tasks.yaml docs/AGENTIC_CONTEXT_ENGINEERING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Commit command:**
  ```bash
  git commit --only -m "build(architecture): compile bounded agent execution packets" -m "Compile revision-valid task packets from exact headings and content hashes while enforcing repository context budgets and zero duplicate unchanged bytes." -m "Changelog: skip" -m "Changelog-Reason: internal agent context packet optimization" -- eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/ContextPacket.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/BuildContextPacketCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/ContentAddressedPacketStore.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/MarkdownSectionReader.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ContextPacketCompilerTests.cs .agents/CONTEXT_ENGINEERING.md .agents/benchmarks/cold-start-tasks.yaml docs/AGENTIC_CONTEXT_ENGINEERING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Execute or replace the packet under the override rules.
- [ ] Verify exact paths/receipt and obtain Phase 4 readiness approval.

## Phase 4: Persistent Approved-Goal Execution — NOT STARTED

**Phase-owned paths:** exactly the paths in the planned commit contract below.

- [ ] **4.1 Author transition, crash, uncertain-commit, stale-approval, and needs-replan tests and verify red failure**
  - **Files:** `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/PersistentGoalExecutionTests.cs` (new)
  - **Acceptance:** Crash injection at every state uses deterministic receipts. Tests prove no skipped approval, duplicate phase commit, blind retry, hidden cleanup, or scope expansion. Forbidden-field cases prove manifests, receipts, goal files, and `goal status` reject source/prompt text, secrets, PII, raw provider/model payloads, provider URLs, free-form fields, and command payloads before persistence or display.
  - **Effort:** L
  - **Dependencies:** Verified Phase 3 commit and readiness.

- [ ] **4.2 Implement persistent goal state, commands, and run store and verify idempotent resume**
  - **Files:** `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/GoalExecution.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalCommands.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalStatusCommand.cs`; `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/WorkflowRunStore.cs`
  - **Acceptance:** `goal start|next|record|resume|block|abort|status` returns one legal action or one bounded fixed-field view, never invokes models, and routes material drift to `NeedsReplan`. Goal files and status reject source/prompt text, secrets, PII, raw provider/model payloads, free-form exceptions/reasons, and command payloads before persistence or display.
  - **Effort:** L
  - **Dependencies:** 4.1.

- [ ] **4.3 Add persistent-goal skill and intent integration and verify bounded status/resume experience**
  - **Files:** `.agents/skills/persistent-goal-execution/SKILL.md`; `.agents/contract/intents.yaml`; `.agents/CONTEXT_ENGINEERING.md`; `docs/AGENTIC_CONTEXT_ENGINEERING.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`; `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
  - **Acceptance:** Skill description routes only approved execution/resume. `goal status` shows current owner/state/next-action code/blocker code/expected HEAD/last good commit from fixed machine fields only. Phase 2 break-glass state remains visible through `status`; Phase 5 hook `doctor` remains owned by Phase 5. Existing harness `/goal` is not canonical state.
  - **Effort:** M
  - **Dependencies:** 4.2.

### Phase 4 Verification — RUN ONCE AFTER TASKS 4.1–4.3

- [ ] Run the Release solution build once and record the disposition.
- [ ] Run `ISLAMU.AgentWorkflow.Tests` once and record passing nonzero tests.
- [ ] Confirm crash/recovery evidence covers every transition without sleeps and fixed goal-state/status privacy tests are green.

### Phase 4 Commit — RUN IMMEDIATELY AFTER VERIFICATION

#### Planned Commit Contract

- **Default title:** `build(architecture): resume approved agent goals deterministically`
- **Default description:** Persist revision-bound goal transitions and recovery receipts so approved work resumes at one safe next action without model-owned authority.
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: internal persistent agent execution control`
- **Commit paths:**
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/GoalExecution.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalCommands.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalStatusCommand.cs`
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/WorkflowRunStore.cs`
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/PersistentGoalExecutionTests.cs`
  - `.agents/skills/persistent-goal-execution/SKILL.md`
  - `.agents/contract/intents.yaml`
  - `.agents/CONTEXT_ENGINEERING.md`
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:**
  ```bash
  git add -- eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/GoalExecution.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalCommands.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalStatusCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/WorkflowRunStore.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/PersistentGoalExecutionTests.cs .agents/skills/persistent-goal-execution/SKILL.md .agents/contract/intents.yaml .agents/CONTEXT_ENGINEERING.md docs/AGENTIC_CONTEXT_ENGINEERING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Commit command:**
  ```bash
  git commit --only -m "build(architecture): resume approved agent goals deterministically" -m "Persist revision-bound goal transitions and recovery receipts so approved work resumes at one safe next action without model-owned authority." -m "Changelog: skip" -m "Changelog-Reason: internal persistent agent execution control" -- eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/GoalExecution.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalCommands.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalStatusCommand.cs eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/WorkflowRunStore.cs eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/PersistentGoalExecutionTests.cs .agents/skills/persistent-goal-execution/SKILL.md .agents/contract/intents.yaml .agents/CONTEXT_ENGINEERING.md docs/AGENTIC_CONTEXT_ENGINEERING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Execute or replace the packet under the override rules.
- [ ] Verify exact paths/receipt and obtain Phase 5 readiness approval.

## Phase 5: Harness Adapter And CI Gate Convergence / Final Delivery — NOT STARTED

**Phase-owned paths:** exactly the paths in the planned commit contract below.

- [ ] **5.1 Author synthetic adapter, doctor, fail-closed safety, and reference-parity tests and verify red failure**
  - **Files:** `tests/Event.Architecture.Tests/AgentWorkflowHarnessArchitectureTests.cs` (new)
  - **Acceptance:** Equivalent events normalize identically; missing safety gate blocks; advisory graph/status failure warns; absolute/missing hook paths, stale agent names, duplicate authorities, and absent CI routes fail.
  - **Effort:** L
  - **Dependencies:** Verified Phase 4 commit and readiness.

- [ ] **5.2 Implement one relative-path hook adapter and migrate Claude, Codex, Cursor, and Copilot configuration and verify doctor parity**
  - **Files:** `.agents/hooks/AgentWorkflowHook.cs`; `.agents/hooks/README.md`; `.claude/settings.json`; `.codex/hooks.json`; `.cursorrules`; `.github/copilot-instructions.md`
  - **Acceptance:** Adapters invoke one CLI surface, preserve root authority, declare unsupported events, and contain no workstation-absolute paths. Safety decisions are equivalent and bounded.
  - **Effort:** L
  - **Dependencies:** 5.1.

- [ ] **5.3 Add the always-present agent-context CI lane and verify workflow integration**
  - **Files:** `.github/workflows/agent-context.yml`; `.github/workflows/test.yml`; `docs/CI_CD_GOVERNANCE.md`
  - **Acceptance:** Dedicated check covers agent/context paths, pull request, push, merge queue, and schedule/manual where appropriate; action pins comply; no secret, write, or OIDC authority is added; the `test.yml` no-op is documented as intentional because the dedicated lane now exists.
  - **Effort:** L
  - **Dependencies:** 5.2.
  - **Guidance:** Closes as commit increment 5B. Does not delete any obsolete surface.

- [ ] **5.4 Migrate remaining command references and delete obsolete hook/bootstrap code after proven parity**
  - **Files:** `AGENTS.md`; `.agents/contract/intents.yaml`; `.agents/contract/README.md`; `.agents/hooks/SecurityCheck.cs`; `.agents/hooks/SkillTrigger.cs`; `.agents/hooks/ContextTracker.cs`; `.agents/hooks/FormatCode.cs`; `.agents/hooks/BuildCheck.cs`; `eng/agent-context/validate-contract.cs`; `eng/agent-context/packages.lock.json`; `docs/AGENTIC_CONTEXT_ENGINEERING.md`; `docs/OPERATIONS.md`; `docs/TESTING.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`; `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`; `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
  - **Acceptance:** Deletion runs only after Gate 5-I has a green pre-commit verification disposition and a finalized receipt bound to the exact post-5B revision whose path/tree hashes equal the verified combined pending 5A+5B snapshot. All old command references are migrated, exactly one active safety authority remains, and that byte-equivalent post-5B revision remains available as the 5C rollback anchor containing commits 5A and 5B plus the old surfaces.
  - **Effort:** L
  - **Dependencies:** 5.3, **plus a green Gate 5-I verification disposition, successful commits 5A and 5B, exact post-5B snapshot equivalence, and a finalized revision-bound parity receipt.**
  - **Guidance:** Closes as commit increment 5C. Never combine this deletion with the independently reviewable 5A adapter commit (CTO finding B3). **STOP — do not start this task while reading the ledger top to bottom.** It appears above the verification gates, but it must run only after the pending combined state is verified, 5A and 5B commit successfully, and Gate 5-I finalizes its post-5B equivalence receipt. Any earlier deletion voids `IVSD-M006` and destroys the proven rollback anchor.

### Phase 5 Verification — TWO GATES (CTO finding C1)

> **Documented exception to the one-build-one-test-per-phase rule.** Phase 5 is
> structurally two phases: it replaces an authority surface, then removes the one
> it replaced. A single gate after Task 5.4 would execute every test *after* the
> obsolete surfaces were already deleted, so the parity that `IVSD-M006` requires
> — proven while the old surfaces are still present — would never actually run.
> Phase 5 therefore gets two gates. No other phase may claim this exception.

#### Gate 5-I — VERIFY AFTER TASKS 5.1–5.3, BEFORE COMMITS 5A/5B; FINALIZE AFTER 5B

- [ ] Before either commit, capture a deterministic verified snapshot/tree manifest binding expected HEAD, the exact combined pending contents and path hashes for the 5A and 5B commit sets, their planned path lists, and the presence/hashes of the relevant obsolete hook scripts and `eng/agent-context/validate-contract.cs`. If any planned path is missing, extra, unowned, or unresolved, stop.
- [ ] Run `dotnet build --configuration Release --verbosity quiet` against that exact uncommitted combined state and record the disposition.
- [ ] Run `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` once against that same state and record passing nonzero tests.
- [ ] Run hook/CI doctor and the workflow dry-run against the same snapshot while obsolete surfaces remain. If any check fails or the snapshot changes, block without committing.
- [ ] Record a green Gate 5-I verification disposition authorizing only the exact 5A then 5B packets below. Verification disposition MUST precede both commits.
- [ ] After 5A and 5B commit successfully, compare the exact post-5B committed path/tree hashes and obsolete-surface presence to the verified snapshot. On exact equivalence, finalize the parity receipt bound to the post-5B commit; on mismatch or either commit failure, block before Task 5.4.

#### Gate 5-II — RUN AFTER TASK 5.4, AFTER DELETION

- [ ] Run `dotnet build --configuration Release --verbosity quiet` and record the disposition.
- [ ] Run `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` once and record passing nonzero tests.
- [ ] Confirm exactly one active safety authority remains and no reference to a deleted command survives.
- [ ] Execute commit increment 5C.

### Phase 5 Commits — THREE SEPARATE COMMITS; 5A/5B AFTER GATE 5-I GREEN, 5C AFTER GATE 5-II

> **CTO finding B3 — mandatory split.** Phase 5 MUST NOT close as one 27-path
> commit. Two defects forced this split: a single commit mixing adapter, CI, and
> deletion is not independently reviewable, and committing the deletions
> alongside the replacement adapter leaves no revision in history containing
> both — which makes this phase's own documented rollback impossible. Verify the
> combined pending 5A+5B snapshot first, record Gate 5-I green, then execute 5A
> and 5B in order. Finalize Gate 5-I only after the post-5B tree proves exact
> snapshot equivalence. Execute 5C only after Task 5.4 and Gate 5-II.

#### Increment 5A — Planned Commit Contract (adapter unification)

- **Default title:** `ci(ci): route harness adapters through one agent workflow gate`
- **Default description:** Add one relative-path hook adapter and migrate Claude, Codex, Cursor, and Copilot configuration to a single provider-neutral policy surface while the obsolete hook scripts remain present and inactive.
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: internal agent harness adapter convergence`
- **Commit paths:**
  - `.agents/hooks/AgentWorkflowHook.cs`
  - `.agents/hooks/README.md`
  - `.claude/settings.json`
  - `.codex/hooks.json`
  - `.cursorrules`
  - `.github/copilot-instructions.md`
  - `tests/Event.Architecture.Tests/AgentWorkflowHarnessArchitectureTests.cs`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:**
  ```bash
  git add -- .agents/hooks/AgentWorkflowHook.cs .agents/hooks/README.md .claude/settings.json .codex/hooks.json .cursorrules .github/copilot-instructions.md tests/Event.Architecture.Tests/AgentWorkflowHarnessArchitectureTests.cs
  ```
- **Commit command:**
  ```bash
  git commit --only -m "ci(ci): route harness adapters through one agent workflow gate" -m "Add one relative-path hook adapter and migrate Claude, Codex, Cursor, and Copilot configuration to a single provider-neutral policy surface while the obsolete hook scripts remain present and inactive." -m "Changelog: skip" -m "Changelog-Reason: internal agent harness adapter convergence" -- .agents/hooks/AgentWorkflowHook.cs .agents/hooks/README.md .claude/settings.json .codex/hooks.json .cursorrules .github/copilot-instructions.md tests/Event.Architecture.Tests/AgentWorkflowHarnessArchitectureTests.cs
  ```
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Execute 5A only after the green Gate 5-I verification disposition; verify committed paths equal `Commit paths`, record the hash, and block on commit failure.
- [ ] Confirm the obsolete surfaces remain present and the 5A path hashes equal their entries in the verified combined snapshot. Treat 5A as independently reviewable adapter evidence; the final parity receipt is not issued until 5B completes and combined equivalence passes.

#### Increment 5B — Planned Commit Contract (dedicated CI lane)

- **Default title:** `ci(ci): add always-present agent context CI lane`
- **Default description:** Add the dedicated agent-context workflow covering pull request, push, merge queue, and scheduled runs, and document why the existing test workflow no-op remains intentional.
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: internal agent context CI gate`
- **Commit paths:**
  - `.github/workflows/agent-context.yml`
  - `.github/workflows/test.yml`
  - `docs/CI_CD_GOVERNANCE.md`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:**
  ```bash
  git add -- .github/workflows/agent-context.yml .github/workflows/test.yml docs/CI_CD_GOVERNANCE.md
  ```
- **Commit command:**
  ```bash
  git commit --only -m "ci(ci): add always-present agent context CI lane" -m "Add the dedicated agent-context workflow covering pull request, push, merge queue, and scheduled runs, and document why the existing test workflow no-op remains intentional." -m "Changelog: skip" -m "Changelog-Reason: internal agent context CI gate" -- .github/workflows/agent-context.yml .github/workflows/test.yml docs/CI_CD_GOVERNANCE.md
  ```
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Execute 5B only after successful 5A and the green Gate 5-I verification disposition; verify committed paths equal `Commit paths`, record the hash, and block on commit failure.
- [ ] Confirm no secret, write, or OIDC authority was added and action pins comply. Compare the exact post-5B combined path/tree hashes and obsolete-surface presence to the verified snapshot; only exact equivalence finalizes the Gate 5-I receipt bound to this post-5B commit and establishes the 5C rollback anchor.

#### Increment 5C — Planned Commit Contract (obsolete surface deletion)

- **Default title:** `ci(ci): delete obsolete agent hook and validator surfaces`
- **Default description:** Remove the superseded hook scripts and bootstrap contract validator after adapter parity is proven, and migrate every remaining command reference so exactly one active safety authority remains.
- **Changelog treatment:** `Changelog: skip`
- **Required trailers:** `Changelog: skip`; `Changelog-Reason: internal obsolete agent surface removal after parity`
- **Commit paths:**
  - `.agents/hooks/SecurityCheck.cs`
  - `.agents/hooks/SkillTrigger.cs`
  - `.agents/hooks/ContextTracker.cs`
  - `.agents/hooks/FormatCode.cs`
  - `.agents/hooks/BuildCheck.cs`
  - `eng/agent-context/validate-contract.cs`
  - `eng/agent-context/packages.lock.json`
  - `AGENTS.md`
  - `.agents/contract/intents.yaml`
  - `.agents/contract/README.md`
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md`
  - `docs/OPERATIONS.md`
  - `docs/TESTING.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md`
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md`
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:**
  ```bash
  git add -- .agents/hooks/SecurityCheck.cs .agents/hooks/SkillTrigger.cs .agents/hooks/ContextTracker.cs .agents/hooks/FormatCode.cs .agents/hooks/BuildCheck.cs eng/agent-context/validate-contract.cs eng/agent-context/packages.lock.json AGENTS.md .agents/contract/intents.yaml .agents/contract/README.md docs/AGENTIC_CONTEXT_ENGINEERING.md docs/OPERATIONS.md docs/TESTING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Commit command:**
  ```bash
  git commit --only -m "ci(ci): delete obsolete agent hook and validator surfaces" -m "Remove the superseded hook scripts and bootstrap contract validator after adapter parity is proven, and migrate every remaining command reference so exactly one active safety authority remains." -m "Changelog: skip" -m "Changelog-Reason: internal obsolete agent surface removal after parity" -- .agents/hooks/SecurityCheck.cs .agents/hooks/SkillTrigger.cs .agents/hooks/ContextTracker.cs .agents/hooks/FormatCode.cs .agents/hooks/BuildCheck.cs eng/agent-context/validate-contract.cs eng/agent-context/packages.lock.json AGENTS.md .agents/contract/intents.yaml .agents/contract/README.md docs/AGENTIC_CONTEXT_ENGINEERING.md docs/OPERATIONS.md docs/TESTING.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-tasks.md dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-context.md islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md
  ```
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

- [ ] Execute 5C only after Gate 5-I has a green pre-commit disposition and a finalized receipt binding the byte-equivalent post-5B rollback anchor; verify committed paths equal `Commit paths` and record the hash/receipt.
- [ ] Confirm exactly one active safety authority remains.
- [ ] Reconcile final plan/context/tasks/I-VSD/intent/canonical-strategy bytes, all seven commit receipts, and the intentionally stale execution manifest; then obtain independent final review before marking the workstream complete.

## Remaining / Deferred Work

- Cross-platform lock primitive selection is deferred to Task 2.2 within the fixed fencing contract.
- Benchmark replay, live-model evaluation, workflow telemetry/cost reporting, and a run journal are deliberate non-goals, not deferred work.
- Bulk migration of historical/active workstreams is explicitly deferred; only current-slice opt-in is allowed.

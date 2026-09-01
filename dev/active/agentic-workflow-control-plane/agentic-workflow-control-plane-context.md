<!-- ABOUTME: Active planning and handoff context for the agentic workflow control-plane workstream. -->
<!-- ABOUTME: Records current review state, evidence, decisions, blockers, shared-tree constraints, and the next safe action. -->

# Agentic Workflow Control Plane — Context

Last Updated: 2026-09-01 Europe/Brussels

## Review State

- I-VSD report: `../../../islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`
- I-VSD stable evidence digest: `sha256:67b4bd5297641ba402a20994186235f1907b9d6d76b5d428833f0f9785857cd7` — digest of evidence packet `E001`–`E008`, not a triad artifact hash.
- Authoritative artifact bindings: the I-VSD report's `Review Metadata`, authored last after the five-phase triad settles.
- I-VSD status / disposition: current / plan-aligned after the Phase 1 receipt and packet-binding correction; authoritative exact bindings are refreshed in its Review Metadata.
- CTO review: historical review retained unchanged at `sha256:636aa802ddaede72f676db2e2c3d9eaf49fec0c92a36092cceb89cae18430561`. Its findings remain applied, but this corrected packet needs fresh revision-bound Tier 1 approval before Phase 2.
- User approval: Phase 1 implementation/commit authority was exercised. Whole-file capture for the two fixed mixed paths was separately authorized under `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED`; this does not grant Phase 2 revision approval.
- Execution manifest: intentionally remains at its committed Phase 1 state. It is not current Phase 2 authority and MUST NOT be edited until fresh revision-bound Tier 1 approval is received.

## Current Progress

### Verified complete

- Tasks 1.1–1.3 and the Phase 1 command gate are complete. The intent/license checks passed, the Release build exited 0 with 8,185 warnings and 0 errors, and `ISLAMU.AgentWorkflow.Tests` passed 8/8.
- Phase 1 commit `eadeeabb4bd9745fef25bcb77dfdfab6c31844c1` has parent `1e2a4d20fae97857e10bacdb24802b66e287cf80`, exact 20/20 planned paths, and an empty post-commit index.
- Decision `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED` records user-class whole-file authorization for only `.agents/contract/intents.yaml` and `docs/AGENTIC_CONTEXT_ENGINEERING.md`, disposition `authorized_and_committed`.
- Bounded decision evidence: `.omo/evidence/20260901-agentic-workflow-control-plane/phase-1-whole-file-authorization.md` (`sha256:ae9bf05db592a9c2b13511898ae485a3315578dd32532f3e98383dc12723a961`).
- Task 1.3 executor and independent review evidence remain `.omo/evidence/20260901-agentic-workflow-control-plane/task-1.3-green.md` and `.omo/evidence/20260901-agentic-workflow-control-plane/task-1.3-code-review.md` (`CLEAR`, no blockers).

### In progress

- Planning-artifact correction only. No Phase 2 implementation or execution-manifest mutation has started.

### Next safe action

1. Obtain fresh revision-bound Tier 1 approval for the exact corrected plan/context/tasks/I-VSD packet.
2. Only after approval, author the Phase 2 execution manifest binding.
3. Begin Task 2.1 under that bound authority.

## Binding Scope Decision

The user removed the former benchmark/measurement phase completely. The authoritative program is now:

- 17 implementation tasks;
- 5 delivery phases;
- 7 planned commits, with Phase 5 retaining increments 5A, 5B, and 5C;
- final delivery in Phase 5 through artifact reconciliation and independent review.

There is no benchmark replay engine, live-model envelope, workflow telemetry or cost surface, run journal, or measurement-phase implementation claim. The existing benchmark registry is not deleted; Phase 3 may read `.agents/benchmarks/cold-start-tasks.yaml` only for existing context-budget facts.

## Key Files And Responsibilities

| Path | Responsibility |
|---|---|
| `docs/AGENTIC_CONTEXT_ENGINEERING.md` | Canonical strategy and explicit five-capability roadmap/non-goal boundary |
| `.agents/CONTEXT_ENGINEERING.md` | Context budgets and retrieval constraints |
| `.agents/contract/intents.yaml` | Dedicated five-phase intent routing and scope |
| `.agents/contract/workstream.schema.json` | Machine schema for execution facts and bindings |
| `eng/agent-context/validate-contract.cs` | Bounded bootstrap validator, removed only after Phase 5 parity |
| `eng/agent-workflow/src/ISLAMU.AgentWorkflow/` | Repository-owned control-plane implementation |
| `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/` | Deterministic behavioral and concurrency tests |
| `dev/active/agentic-workflow-control-plane/*-execution.yaml` | Machine-only workstream state; currently bound to the approved Phase 1 foundation packet |
| `.git/islamu-agent/` | Untracked claims, locks, and packet cache only |
| `.agents/benchmarks/cold-start-tasks.yaml` | Existing registry consumed only for Phase 3 context-budget facts |
| `.agents/hooks/` and harness settings | Thin adapter surface after Phase 5 |
| `.github/workflows/agent-context.yml` | Always-present dedicated agent workflow gate |

## Key Decisions

1. One standalone repository-owned .NET control plane owns typed workflow behavior and references no product project.
2. YAML owns fixed machine facts only; Markdown owns architecture, findings, tasks, and handoffs.
3. Shared `develop` remains authoritative: no worktrees, branch switching, stash/reset, automatic cleanup, or dirty takeover.
4. Claims are checkout-local and generation-fenced; cross-clone coordination is explicitly out of scope.
5. The deterministic executor emits one safe action and never invokes a model, self-approves, or expands scope.
6. Packet cache entries contain content-addressed handles, hashes, byte counts, and bounded locators, not copied source bodies.
7. Persistent goal state and `goal status` use fixed schema-owned identifiers, state/action/blocker codes, digests, expected HEAD, and commit identifiers only.
8. Manifests, packet caches, claims, receipts, and goal state reject prompts, source bodies, secrets, PII, raw provider/model payloads, provider URLs, free-form exceptions/reasons, and command payloads.
9. Phase 2 retains ownership of break-glass authorization and its visibility through `status` and `doctor`.
10. Phase 5 retains ownership of hook/adapter `doctor` and the two-gate 5A/5B/5C convergence sequence.
11. Existing active workstreams migrate only by explicit current-slice opt-in.
12. The removed measurement capability is a deliberate non-goal, not deferred work.
13. Revision binding is packet-closed: every future packet committing tasks, context, or execution state also commits the I-VSD report, authored last against exact settled plan/context/tasks bytes. Phase 5 increments 5A/5B intentionally exclude mutable state and are finalized by 5C.

## Validation Baseline

Phase 1's focused paths are green: the validator and dependency audit passed, the Release build exited 0, and the selected AgentWorkflow project passed 8/8. The solution build emitted 8,185 warnings, but only one warning record plus the aggregate was retained; the other 8,184 records cannot be attributed without a prohibited rerun. This non-pristine external warning state prevents a wholly-green repository claim while leaving the Phase 1 focused disposition green. The separately captured unrelated Setup Assistant `SA518-GRAPH-RATCHET` baseline remains at `.omo/evidence/20260901-agentic-workflow-control-plane/test-results.txt` and `.omo/evidence/20260901-agentic-workflow-control-plane/st_01a05cbf-manual-qa.md`.

| Phase | Release build | Selected test |
|---|---|---|
| 1 | `dotnet build --configuration Release --verbosity quiet` | `ISLAMU.AgentWorkflow.Tests` |
| 2 | same | `ISLAMU.AgentWorkflow.Tests` |
| 3 | same | `ISLAMU.AgentWorkflow.Tests` |
| 4 | same | `ISLAMU.AgentWorkflow.Tests` |
| 5 — Gate 5-I | same | `Event.Architecture.Tests` |
| 5 — Gate 5-II | same | `Event.Architecture.Tests` |

The plan Section 6 and task ledger gates are authoritative. Phase 5 alone has two gates because parity must be proven while old and replacement surfaces coexist before deletion.

## Current Risks / Unknowns

| Risk / unknown | Current disposition | Owner |
|---|---|---|
| Same-user pathname/content replacement can race the final artifact check | Task 1.3 hashes the opened handle and fails unstable size observations closed; atomic ownership/fencing remains Phase 2 scope | 2.1–2.3 |
| Fresh corrected-revision approval is absent | Keep the Phase 1 execution manifest unchanged; obtain revision-bound Tier 1 approval before authoring Phase 2 state | approval gate |
| External Setup Assistant `SA518-GRAPH-RATCHET` baseline remains red | Treat as pre-existing/unrelated; do not repair or claim the repository wholly green | external workstream |
| Cross-platform lock primitive | Preserve generation fencing and no dirty takeover | 2.2 |
| Mixed hunk ownership cannot be inferred safely | Block and require human coordination | 2.1–2.3 |
| Packet cache or goal state leaks sensitive content | Fixed fields and forbidden-field tests at both boundaries | 3.1–3.3, 4.1–4.3 |
| Executor amplifies a stale or bad plan | Immutable approvals and `NeedsReplan` | 4.1–4.3 |
| Adapter removal occurs before parity | Gate 5-I snapshot, post-5B equivalence receipt, then Gate 5-II and 5C | 5.1–5.4 |
| Shared dirty tree captures another contributor's work | Literal paths, normalized ownership, staged parity, no destructive recovery | 2.1–2.4 |
| Mutable packet omits its last-authored I-VSD binding | Corrected Phase 2/3/4/5C packet lists and commands include I-VSD; 5A/5B remain intentionally immutable-state-only | planning artifacts |

## Shared-Tree Constraint

The checkout contains extensive unrelated Setup Assistant work. This correction touches only the four authorized planning/report documents plus the bounded decision-evidence file. Do not stage, commit, clean, repair, or absorb any unrelated path or hunk.

## Handoff — 2026-09-01 Europe/Brussels (Phase 1 receipt and packet-binding correction)

- **Current state:** Tasks 1.1–1.3 and commit 1/7 are complete; all 14 later implementation tasks remain open across the unchanged five phases.
- **Phase 1 receipt:** `eadeeabb4bd9745fef25bcb77dfdfab6c31844c1`, parent `1e2a4d20fae97857e10bacdb24802b66e287cf80`, exact 20/20 paths, post-commit index empty.
- **Bounded authority:** `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED`, authorizer class `user`, fixed two-path list, disposition `authorized_and_committed`; no free-form authorization content is retained.
- **Validation limitation:** Phase 1 focused paths are green, but the solution produced 8,185 warnings with incomplete attribution and the external SA518 baseline remains unrelated; no wholly-green repository claim is made.
- **Binding defect and correction:** Future mutable-state packets previously omitted the last-authored I-VSD report. Phase 2, Phase 3, Phase 4, and increment 5C now include it in phase/task path ownership, staging, and path-limited commit contracts; 5A/5B remain intentionally excluded and finalized by 5C.
- **Preserved:** Five phases, 17 tasks, seven commits, all finding IDs/mappings/dispositions, and all behavior/security controls. `IVSD-F008` remains open for Task 2.4.
- **Next action:** fresh revision-bound Tier 1 approval, then Phase 2 manifest authorship.

## DoneClaim — 2026-09-01 Europe/Brussels

- **Claim:** The bounded Phase 1 planning receipt and future packet-binding defect are reconciled.
- **Bound outcomes:** Phase 1 is verified/committed with exact receipt evidence; future mutable packets are revision-complete; no Phase 2 execution state was authored.
- **Truth state:** Exact commit and index facts are verified. The 8,185-warning solution state is disclosed as non-pristine while Phase 1 focused paths remain green.
- **Authority state:** Whole-file authorization is exhausted by the fixed Phase 1 decision and commit; fresh revision-bound Tier 1 approval is the sole next gate.
- **Scope:** Plan/tasks/context/I-VSD plus bounded decision evidence only; no execution manifest, CTO review, intent, strategy source, schema, source, test, stage, commit, build, or test mutation.

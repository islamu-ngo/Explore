<!-- ABOUTME: Multi-phase implementation plan for the repository-owned agentic workflow control plane. -->
<!-- ABOUTME: Turns the canonical roadmap into typed state, shared-workspace fencing, bounded packets, persistent execution, and converged gates. -->

# Agentic Workflow Control Plane — Implementation Plan

Last Updated: 2026-09-01 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Write a high-stakes, multi-phase implementation plan for the highest-leverage agentic/context-engineering improvements.
- **Task directory:** `dev/active/agentic-workflow-control-plane/`
- **Canonical strategy source:** [`docs/AGENTIC_CONTEXT_ENGINEERING.md`](../../../docs/AGENTIC_CONTEXT_ENGINEERING.md) Section 10.
- **Planning status:** Phase 1 is receipt-verified and committed; the packet-binding correction is complete, pending fresh revision-bound Tier 1 approval before Phase 2.
- **Change classification:** Behavioral Delta — contributor-visible CLI behavior, approval/state transitions, shared-workspace failure behavior, packet outputs, goal status, and hook decisions change.
- **Current governing intents:**
  - Primary: `create-agent-context-skill`.
  - Secondary: `ci-cd-change`.
  - Bounded fallback: agent workflow tooling, tests, adapters, and active execution manifests that are not yet authorized by either current path allow-list.
  - Phase 1 MUST add the recurring `agent-workflow-control-plane` intent before broader implementation paths are touched.
- **Highest current criticality:** **Tier 1 / Security.** The new `agent-workflow-control-plane` intent created by Task 1.1 MUST declare Tier 1. The composite fallback intents (`create-agent-context-skill`, `ci-cd-change`) are Tier 3/4, but they do not bound this surface.
- **Criticality rationale (CTO finding B4):** This control plane owns approval authority, mutates Git on a shared branch, and enforces fail-closed safety gates. A component that can destroy another contributor's uncommitted work is a security boundary regardless of which legacy intent routes it. Tier 1 selects the required adversarial Invariant-Breaker testing, exhaustive blast-radius exploration, and review protocol. The previous "security-sensitive but Tier 3" classification is withdrawn as untenable.
- **Risk posture:** Approval authority, Git mutation, path ownership, persisted goal state, and hook enforcement are treated as Tier 1 security surfaces with fail-closed defaults and adversarial test coverage.
- **Relevant skills:** `implementation-plan`, `i-vsd`, `grill-me`, `conventional-commit`, `clean-architecture-rules`, `skill-authoring`, `ip-clean-room`, `senior-cto-feedback`.
- **Relevant rules:** `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `.agents/rules/ip-clean-room.md`, no-Python/Node tooling boundary, shared-`develop` no-worktree contract.
- **Primary layers:** Tool Domain, Tool Application, Tool Infrastructure, agent contracts, harness adapters, CI, documentation.
- **Complexity:** XL program. The CTO right-sizing gate is triggered by task count, mixed adapter/CI concerns, and independent delivery value. This plan therefore defines five independently approved delivery phases closing as seven planned commits; it MUST NOT ship as one mega-change.
- **I-VSD document:** [`islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md`](../../../islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md)
- **I-VSD stable evidence digest:** `sha256:67b4bd5297641ba402a20994186235f1907b9d6d76b5d428833f0f9785857cd7` — this is the digest of the *evidence packet* (`E001`–`E008`), **not** a hash of any plan/context/tasks file. Do not attempt to verify a triad file against it.
- **Authoritative artifact bindings:** the current `sha256` digests of this plan, the context, and the tasks live in the I-VSD report's `Review Metadata`. The I-VSD report is authored last, after the triad settles, and is the single source of truth for currency. Verify against that block, not against a copy here.
- **I-VSD status / disposition:** Current / plan-aligned, revalidated after the CTO rewrite. Findings `IVSD-F001`, `F002`, `F004`, `F006`, and `F007` were re-evaluated against the rewritten revision, and `IVSD-F008` / `IVSD-M008` were added to govern the new break-glass authority surface.
- **CTO review:** Historical review retained unchanged — [`agentic-workflow-control-plane-cto-review.md`](agentic-workflow-control-plane-cto-review.md). All recorded findings remain applied; this corrected revision requires fresh revision-bound Tier 1 approval before Phase 2.
- **User approval:** Implementation and Phase 1 authority were exercised. The user separately authorized whole-file capture of the two fixed mixed paths under decision `PH1_WHOLE_FILE_CAPTURE_AUTHORIZED`; that bounded disposition does not grant Phase 2 revision approval.
- **Grill-Me intake:** Resolved from repository evidence. Fixed decisions are repository-owned C#, shared `develop`, no worktrees, machine enforcement of facts/state, human/agent semantic judgment, fixed privacy-safe machine state, and opt-in migration of active workstreams.
- **Provenance:** Not externally informed; no dependency change planned. Reuse the existing .NET SDK, TUnit, and centrally pinned YamlDotNet.

## 1. Executive Summary

ISLAMU Event already has sophisticated agent governance, but the execution
surface remains mostly prose-driven. Intents are typed, while workstream state,
approvals, path ownership, context revisions, phase commits, evidence, and
recovery are manually reconciled across Markdown, hooks, and harness-specific
configuration.

This program introduces one provider-neutral .NET control plane that:

1. validates typed workstream execution state and immutable approval bindings;
2. fences shared-`develop` path ownership and verification-to-commit closure;
3. compiles bounded, content-addressed decision/execution packets;
4. resumes approved goals through an idempotent state machine;
5. converges harnesses and CI on the same executable gates.

The target is not “autonomous agents with more authority.” The target is
smaller context, stronger human authority, safer concurrency, cheaper semantic
review, deterministic recovery, and evidence that can be trusted.

### Explicit non-goals

- No product/API/domain behavior changes.
- No worktrees, branch switching, automatic stashing, reset, or dirty-work cleanup.
- No hosted workflow database or provider-owned canonical state.
- No model invocation from the deterministic executor.
- No prompts, source bodies, secrets, PII, raw model responses, free-form payloads, or command payloads in manifests, packet caches, claims, receipts, or persistent goal state.
- No benchmark replay engine, live-model envelope, workflow telemetry/cost surface, or redacted run journal. The existing benchmark registry remains unchanged and may supply Phase 3 context-budget facts only.
- No automatic user/CTO/I-VSD approval.
- No backward-compatibility aliases for obsolete hook or command surfaces after migration parity is proven.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context

The code-review graph is product-code oriented and no graph tool was available
in this planning session. The bounded tooling flow was established from current
machine artifacts:

```yaml
Target: Repository agent workflow control plane (new)
Upstream callers:
  - AGENTS.md contribution lifecycle
  - implementation-plan and senior-cto-feedback skills
  - Claude/Codex/Cursor/Copilot/OmO adapters
  - GitHub Actions agent-context gate (missing today)
Downstream adapters:
  - YAML workstream storage
  - local Git process adapter
  - .git-local claim and packet-cache stores
  - Markdown section retrieval
  - fixed persistent-goal state and status
Impacted flows:
  - plan -> review -> approval -> claim -> implement -> verify -> commit
  - cold start and cold resume
  - shared-develop concurrent contribution
  - hook and CI policy enforcement
Existing dedicated tests:
  - none
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Intent routing is typed, execution state is not | `.agents/contract/schema.json`, `eng/agent-context/validate-contract.cs` | High | Validator models intents and special cases, not active workstream transitions |
| Triad agreement and approvals are prose-enforced | implementation-plan quality gates and operational artifacts | High | No digest/parity validator exists |
| Shared-`develop` closure is precise but manual | `docs/AGENTIC_CONTEXT_ENGINEERING.md` Shared Develop Phase-Close Protocol | High | No lease, expected-HEAD fence, or phase-close coordinator |
| Context budgets exist but are not enforced by packet compilation | `.agents/CONTEXT_ENGINEERING.md`, `.agents/benchmarks/cold-start-tasks.yaml` | High | Phase 3 may consume registry budget facts; replay and live measurement are deliberate non-goals |
| Hook behavior diverges from documentation/config | `.agents/hooks/README.md`, `.claude/settings.json`, `.codex/hooks.json`, `.cursorrules` | High | Codex uses absolute paths; Claude registers graph hooks only |
| Agent-context CI is described but absent | `docs/CI_CD_GOVERNANCE.md`, missing `.github/workflows/agent-context.yml`, `test.yml` ignore rules | High | Current agent-only changes can take a dedicated/no-op route without the described gate |
| Historical workstreams are not current authorities | `dev/zarchive/enterprise-ci-cd-hardening`, `agent-architecture-modernization`, `refactor-context-engineering` | High | Reuse decisions, not stale paths/status |
| Existing dependencies are sufficient | `Directory.Packages.props` | High | YamlDotNet and TUnit are already centrally pinned |

### 2.2 Existing Implementation

#### Contract and validation

- `.agents/contract/intents.yaml` and `schema.json` model contribution intent.
- `eng/agent-context/validate-contract.cs` validates schema, references, selected paths, and a limited benchmark parity subset.
- No typed workstream manifest, transition model, approval digest, or active-triad validator exists.

#### Planning and review

- `implementation-plan` produces plan/context/tasks plus an I-VSD report.
- `senior-cto-feedback` binds technical review to revisions.
- Exact phase commit packets are authored during planning.
- Parity, freshness, and execution authority are still verified through prose and agent review.

#### Shared workspace

- Agents work on one shared `develop` checkout without worktrees.
- Exact staging and path-limited commit packets preserve unrelated staged paths.
- Mixed ownership blocks by policy, but no repository coordinator owns path claims or expected-HEAD fences.

#### Harnesses and CI

- `.agents/rules` and `.omo/rules` are the only reciprocal rule pair.
- Root `CLAUDE.md` and Copilot instructions point to `AGENTS.md`.
- `.cursorrules` currently carries graph guidance only.
- `.codex/hooks.json` contains workstation-absolute paths.
- `.claude/settings.json` registers graph hooks, not the documented C# hook suite.
- `.github/workflows/test.yml` ignores agent-context paths, while the dedicated workflow described by CI governance is absent.

### 2.3 Existing Tests And Verification Coverage

- `Event.Architecture.Tests` protects product/repository conventions but does not execute agent workflow state.
- The contract validator is an executable check, not a behavioral TUnit suite.
- No deterministic tests cover transition legality, approval freshness, concurrent claims, HEAD races, interrupted closure, packet budgets, persistent-goal state privacy, or adapter parity.
- Existing phase-close Git behavior has reviewer evidence, not a maintained repository test suite.

### 2.4 Existing Documentation And Contracts

Primary sources:

- `AGENTS.md`
- `docs/AGENTIC_CONTEXT_ENGINEERING.md`
- `.agents/CONTEXT_ENGINEERING.md`
- `.agents/contract/*`
- `.agents/benchmarks/cold-start-tasks.yaml` (Phase 3 context-budget facts only)
- `.agents/skills/implementation-plan/*`
- `.agents/skills/senior-cto-feedback/*`
- `.agents/skills/conventional-commit/SKILL.md`
- `docs/CI_CD_GOVERNANCE.md`
- `.agents/hooks/*` and harness settings

### 2.5 Current Pain Points / Improvement Areas

1. Mechanical defects consume advanced-model review context.
2. One workflow fact is copied across many prose authorities.
3. Shared-checkout ownership is inferred rather than claimed.
4. Workstream resume requires rereading and reconciling Markdown.
5. Approval and evidence freshness are asserted rather than cryptographically bound.
6. Harnesses can apply different safeguards.
7. Agent-context CI can report green while adapter/documentation behavior drifts.
8. Manual evidence discovery increases developer friction.

### 2.6 Unknowns After Investigation

No unknown changes scope, architecture, or phase order.

Deferrable implementation details:

| Detail | Bound | Owning task |
|---|---|---|
| Exact cross-platform advisory lock primitive | Must preserve generation fencing and never auto-reclaim dirty state | Task 2.2 |
| Markdown heading extraction edge cases | No new parser dependency; bounded exact headings with content hashes | Task 3.2 |

## 3. Proposed Future State: Behavioral Contract & Scenarios

### Requirement 1 — Revision-bound executable workstreams

The system SHALL accept execution only for a schema-valid workstream whose
plan, tasks, I-VSD, CTO decision, user approval, current phase, and expected HEAD
bindings agree.

#### Scenario 1A — Approved current revision

- **GIVEN** a valid workstream with matching artifact digests and explicit implementation/commit authority
- **WHEN** an executor requests the next action
- **THEN** exactly one legal transition and one bounded execution packet are returned

#### Scenario 1B — Stale or contradictory approval

- **GIVEN** any reviewed artifact changed after approval or commit authority is absent
- **WHEN** claim or execution is requested
- **THEN** the request fails closed with the exact stale binding and no repository mutation

### Requirement 2 — Fenced shared-workspace ownership

The system SHALL prevent overlapping mutation ownership and SHALL bind phase
verification to the exact HEAD and owned file state committed.

#### Scenario 2A — Disjoint concurrent claims

- **GIVEN** two agents request non-overlapping normalized paths
- **WHEN** both acquire claims
- **THEN** both claims succeed with independent fencing generations

#### Scenario 2B — Overlap or mixed ownership

- **GIVEN** an existing claim or a file containing unowned hunks
- **WHEN** another agent requests mutation or phase closure
- **THEN** the operation fails before edit/stage/commit and preserves all dirty state

#### Scenario 2C — HEAD moves after verification

- **GIVEN** phase verification succeeded at HEAD A
- **WHEN** repository HEAD becomes B before closure
- **THEN** the receipt is invalidated and commit is refused until re-verification, for at most 3 total re-verification attempts before a fixed diagnostic blocks for human coordination

The default closure-lock acquisition timeout is 30 seconds and the maximum
re-verification attempt count is 3. These are machine-owned schema/manifest
facts, configurable only by a revision-bound approved manifest. Tests inject
shorter deterministic bounds and coordinate with exact barriers/events; they do
not sleep or wait for wall-clock luck.

#### Scenario 2D — Break-glass control-plane repair

The control plane governs its own repair from Phase 2 onward, so this bypass
exists to prevent a claim-store defect from blocking its own fix. It is a Tier 1
authority surface and is bounded by construction, not by discipline.

- **GIVEN** an explicit per-repair human authorization for control-plane repair
- **WHEN** the operator invokes the break-glass
- **THEN** claim acquisition is suspended, a receipt is written with a bounded enumerated reason code, and `status` and `doctor` surface the active bypass

- **GIVEN** an active break-glass
- **WHEN** any mutation is attempted
- **THEN** path-ownership validation, staged-set parity, and the literal-path-list rule remain fully enforced, and no `revert`, `reset`, `checkout`, `stash`, or `clean` becomes available

- **GIVEN** an executor with no human authorization, or an authorization scoped to a prior repair
- **WHEN** it requests the break-glass
- **THEN** the request fails closed; the bypass is never self-service, session-scoped, or standing

### Requirement 3 — Bounded content-addressed packets

The system SHALL emit only the current task's decision-complete evidence and
SHALL reject stale or over-budget packets.

#### Scenario 3A — Cold resume

- **GIVEN** a valid workstream and current task
- **WHEN** a packet is built
- **THEN** it contains only current state, named plan headings, matched rules/skills, owned paths, tests, and content hashes within configured byte limits

#### Scenario 3B — Stale handle or duplicate content

- **GIVEN** a source hash changed or unchanged content would be repeated
- **WHEN** the packet is reused
- **THEN** stale handles are rejected and unchanged bytes are referenced rather than duplicated

### Requirement 4 — Idempotent persistent goal execution

The system SHALL resume from durable machine state without self-approval,
scope expansion, duplicate side effects, or hidden cleanup.

#### Scenario 4A — Interrupted verification

- **GIVEN** verification started without a terminal receipt
- **WHEN** execution resumes
- **THEN** verification reruns against the current expected state before any commit

#### Scenario 4B — Uncertain commit result

- **GIVEN** the commit process result is unknown
- **WHEN** execution resumes
- **THEN** repository truth and the planned packet are inspected before retrying or advancing

#### Scenario 4C — Material scope change

- **GIVEN** implementation no longer matches approved scope or architecture
- **WHEN** the executor records divergence
- **THEN** state becomes `NeedsReplan` and fresh I-VSD/CTO/user bindings are required

### Requirement 5 — Converged harness and CI gates

The system SHALL expose one provider-neutral policy surface to every supported
harness and CI.

#### Scenario 5A — Equivalent safety event

- **GIVEN** semantically equivalent mutation events from two harnesses
- **WHEN** thin adapters invoke the control plane
- **THEN** the normalized decision and diagnostic code are equivalent

#### Scenario 5B — Missing or broken safety adapter

- **GIVEN** a required mutation/authority safety gate cannot execute
- **WHEN** a mutating action is requested
- **THEN** the operation fails closed; advisory graph/metrics failures remain bounded warnings

### Worst Break Scenario — Silent capture and commit of another contributor's uncommitted work

This is the single most catastrophic failure mode of this program. It is the
only failure with no clean recovery: a blocked commit costs minutes, whereas
destroyed uncommitted work is unrecoverable from repository state.

- **GIVEN** shared `develop` holds another contributor's uncommitted work (the tree currently holds extensive unrelated Setup Assistant and agent-context changes), and the executor's staging step resolves a broader path set than the planned packet — through a directory path, a glob, a normalization mismatch, a symlink, or a case/NFC alias
- **WHEN** the executor stages and commits under this workstream's message
- **THEN** the foreign work is captured into this workstream's commit, the victim observes an unexpected tree, and recovery by `revert`/`reset` destroys their remaining uncommitted state

**Preventive blocking controls:**

1. The commit path set is a literal file list. Directory paths, globs, and `git add -A/-u/.` are forbidden at every layer.
2. Every path is normalized and re-resolved (symlink, reparse point, case, NFC) before ownership comparison, and unresolvable or unowned paths block.
3. The staged set is diffed against the planned packet path list before commit; any extra path aborts before commit.

**Detection and containment after commit:** Post-commit tree comparison detects a
committed-tree divergence and raises a fixed diagnostic that stops further
automation. It does not prevent the initial capture.

**Secondary-harm limitation:** The tool never runs `revert`, `reset`, `checkout`,
`stash`, or `clean` as recovery. It reports no-tool recovery commands to a human,
limiting additional damage without claiming to undo the initial capture.

**Detection signal:** committed tree differs from the planned packet path list.

**Blast radius if undefended:** permanent loss of an unrelated contributor's
uncommitted work — the only outcome in this program that cannot be forward-fixed.

### Secondary Fail-Closed Scenario — Stale approval plus overlapping commit

This scenario documents correct refusal behavior. It is a success path, not a
break, and is retained as a positive safety assertion.

- **GIVEN** an executor holds a stale approval and another contributor modified or owns one planned commit path
- **WHEN** the executor attempts verify-and-close
- **THEN** approval freshness and ownership independently fail closed, no file is staged or committed, and the status output identifies both blockers without exposing source content

## 4. Non-Negotiable Constraints

1. Root `AGENTS.md` and the contribution contract retain authority.
2. One shared `develop` checkout; no worktrees, checkout, stash, reset, destructive cleanup, force, or history rewrite.
3. Repository-owned .NET/C# only; no ad-hoc Python or Node tooling.
4. Machine state contains facts and receipts, not architecture prose or moral/technical approval reasoning.
5. No model invocation from the deterministic control plane.
6. Manifests, packet caches, claims, receipts, and persistent goal state contain only fixed machine fields; no source, prompt, secret, PII, raw response, provider payload, free-form field, or command payload is persisted.
7. Safety gates fail closed; advisory observability may fail open with bounded diagnostics.
8. Existing active workstreams migrate only by explicit current-slice opt-in.
9. No new dependency unless IP/dependency review proves every outbound licensing path remains available.
10. Tests use exact state/event signals and temporary repositories; no sleeps or timing luck.
11. Git invocation uses `ProcessStartInfo.ArgumentList`, never shell text from Markdown.
12. Product runtime projects and EF migrations are out of scope.

## 5. Architecture And Design Decisions

### Decision 1 — Typed execution sidecar, not Markdown-as-state

- **Decision:** Add a workstream execution YAML validated by a dedicated schema. Keep plan/context/tasks/I-VSD as human/agent artifacts with digest references from the sidecar.
- **Why:** Machine facts need schema, transition, and freshness enforcement; architecture and teaching prose remain better in Markdown.
- **Alternatives considered:**
  - Markdown-only parsing: rejected because it preserves ambiguity and prose tests.
  - Generate all Markdown from YAML: rejected because it collapses architecture and handoff reasoning into a machine schema.
- **Consequences:** A fourth artifact exists, but it owns only machine state and fails on parity drift.
- **Files/layers affected:** `.agents/contract`, `dev/active`, Tool Domain/Application/Infrastructure.

### Decision 2 — One standalone Clean Architecture console project

- **Decision:** Create `ISLAMU.AgentWorkflow`, a standalone `net10.0` console project with internal Domain, Application, and Infrastructure folders plus a focused TUnit project.
- **Why:** The workflow needs durable domain transitions and Git/YAML adapters without referencing product projects.
- **Alternatives considered:**
  - Extend the file-based validator indefinitely: rejected because state, concurrency, and recovery exceed a script's maintainable scope.
  - Put the control plane in product source: rejected because contribution tooling is not product runtime behavior.
  - Multiple micro-tools: rejected because they would duplicate state and policy.
- **Consequences:** Solution/build and lock-file governance include one new tooling project and one test project.
- **Files/layers affected:** `eng/agent-workflow`, `Explore.slnx`.

### Decision 3 — Ephemeral coordination under `.git/`

- **Decision:** Store leases, locks, caches, and run receipts under `.git/islamu-agent/`; keep durable approved execution manifests under `dev/active`.
- **Why:** Coordination is checkout-local and must not pollute commits or require worktrees/cloud infrastructure.
- **Alternatives considered:**
  - Tracked lease files: rejected because normal Git operations create false conflicts.
  - Worktrees: rejected by explicit workflow constraint.
  - Hosted coordination service: rejected for portability, privacy, and offline use.
- **Consequences:** Commands must detect repository root and refuse unsupported/noncanonical checkout state.
- **Files/layers affected:** Tool Infrastructure/Git.

### Decision 4 — Deterministic executor emits actions, never model calls

- **Decision:** Persistent execution validates state and emits one bounded next-action packet. It does not choose architecture, invoke models, approve, or expand scope.
- **Why:** Determinism and human authority are lost if the control plane becomes another autonomous agent.
- **Alternatives considered:** Embedded model orchestration rejected; harnesses remain responsible for model invocation.
- **Consequences:** Harness adapters consume packets and report receipts.
- **Files/layers affected:** Tool Domain/Application, harness adapters.

### Decision 5 — Thin adapters over one gate

- **Decision:** Replace stale per-harness hook logic with relative-path adapters that normalize events and invoke `ISLAMU.AgentWorkflow`.
- **Why:** Safety decisions must not vary by harness.
- **Alternatives considered:** Maintaining equivalent native logic per harness rejected as drift-prone.
- **Consequences:** Adapter-specific capabilities remain visible; unsupported required safety events fail closed.
- **Files/layers affected:** `.agents/hooks`, `.claude`, `.codex`, `.cursorrules`, `.github`.

### Decision 6 — Revision-binding packets close over their mutable review state

- **Decision:** Every future commit packet that includes this workstream's tasks artifact, context artifact, or execution state also includes the I-VSD report. The I-VSD report is authored last against the exact settled plan/context/tasks bytes before that packet closes.
- **Why:** Omitting the last-authored review artifact lets a packet commit mutable workflow state while leaving its authoritative review bindings stale.
- **Alternatives considered:** Rebinding I-VSD in a later packet was rejected because the intervening revision would claim approval currency it cannot prove.
- **Consequences:** Future mutable-state packets remain revision-complete; Phase 5 increments 5A and 5B stay excluded because 5C performs their final mutable-state reconciliation.
- **Files/layers affected:** Workstream plan/context/tasks/execution packets and the I-VSD report.

### Decision 7 — Incremental migration with hard deletion after parity

- **Decision:** Bootstrap the new project beside `eng/agent-context`, migrate callers only after parity, then delete obsolete validator/hook surfaces in Phase 5.
- **Why:** This is staged replacement, not a compatibility shim; each intermediate commit remains buildable and truthful.
- **Alternatives considered:** Big-bang deletion rejected because current intents depend on the existing command.
- **Consequences:** Temporary dual implementation is explicitly bounded to Phases 1–5.
- **Files/layers affected:** validator, intents, hooks, docs, CI.

## 6. Implementation Phases

Each phase is an independently reviewed delivery increment. The program closes
as seven planned commits across five phases. Starting a later phase requires all
commit outcomes for the previous phase to be verified and recorded, plus a fresh
CTO/user readiness decision for the next phase state.

### Phase 1 — Typed Workstream Contract And Tool Foundation

- **Goal:** Introduce the dedicated intent, schema, standalone tool/test projects, typed transition core, and validator command.
- **Depends on:** Approved plan and current I-VSD report.
- **Relevant / phase-owned files:**
  - `.agents/contract/intents.yaml` (existing)
  - `.agents/contract/workstream.schema.json` (new)
  - `.agents/contract/README.md` (existing)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/ISLAMU.AgentWorkflow.csproj` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Program.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkstreamExecution.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ValidateWorkstreamCommand.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/YamlWorkstreamStore.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/packages.lock.json` (generated)
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj` (new)
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/WorkstreamContractTests.cs` (new)
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/packages.lock.json` (generated)
  - `Explore.slnx` (existing)
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml` (new)
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md` (existing)
  - the four planning artifacts for this workstream
- **Related skills/rules:** `clean-architecture-rules`, `skill-authoring`, `ip-clean-room`.
- **Acceptance criteria:**
  - Task 1.1 adds and validates the new intent before any new tool/test/adapter path is edited.
  - New intent authorizes the full planned control-plane/harness/test/doc surface and names exact verification.
  - Task 1.2 creates the standalone test project, its lock file, and `WorkstreamContractTests.cs` before any production project, schema, CLI, command, or diagnostic is implemented. The test project references no product or future control-plane source project.
  - Task 1.2 tests the future control-plane CLI and schema only through public black-box process/file seams. They compile and execute immediately, then fail because the production project/schema/CLI/commands/diagnostics are absent or nonconforming — never because test infrastructure is absent.
  - The Task 1.2 red run uses deterministic temporary inputs, `ProcessStartInfo.ArgumentList`, and an exact process-exit completion signal with a bounded timeout; it uses no sleeps, polling, raw-source assertions, or prose assertions. One targeted red command must report a nonzero executed-test count and a failing disposition.
  - Task 1.3 creates the production source project, schema, CLI, commands, diagnostics, and first execution manifest, then turns the same Task 1.2 tests green without changing their behavioral contract.
  - Schema rejects unknown fields, illegal states, missing digests/approvals, path traversal, and incomplete phase packets.
  - Domain transition API makes illegal approval/phase transitions unrepresentable or returns typed errors.
  - Validator binds plan/tasks/I-VSD/CTO/user digests and expected HEAD.
  - Both standalone projects reference no product project; the test project remains black-box and has no source-project reference.
  - Existing validator remains only as the bounded bootstrap path until Phase 5.
- **Red/Green ordering:** After Task 1.1, Task 1.2 bootstraps all test infrastructure and runs exactly `dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/WorkstreamContractTests/*"`. The command must compile the tests, execute a nonzero test count, and fail only at the future public CLI/schema contract. Task 1.3 then implements production and reruns that same targeted command to green before the phase-end gate.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Define revision-bound executable agent workstreams.
- **Rollback / failure handling:** Revert the new tool/intent/schema as one slice. Existing agent workflow remains authoritative until the new validator passes parity.

### Phase 2 — Shared-Develop Claims And Fenced Phase Closure

- **Goal:** Prevent overlapping mutation and bind verification to exact HEAD, owned bytes, commit packet, and result.
- **Depends on:** Phase 1 typed manifest and transition model.
- **Relevant / phase-owned files:**
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/WorkspaceClaim.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/PhaseClosure.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClaimCommands.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/ClosePhaseCommand.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/GitWorkspaceCoordinator.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/FileClaimStore.cs` (new)
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/SharedDevelopCoordinatorTests.cs` (new)
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md` (existing)
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml` (existing)
  - workstream tasks/context artifacts (existing)
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md` (existing)
- **Related skills/rules:** `conventional-commit` for planned packet authorship only; shared-`develop` constraints.
- **Acceptance criteria:**
  - Disjoint claims can coexist; overlapping normalized paths fail before mutation.
  - Generation fencing prevents stale claim holders from closing.
  - Verification receipt binds expected HEAD and file hashes.
  - HEAD movement or mixed/unowned hunks invalidates closure.
  - **Verification runs outside the closure lock (CTO finding B2).** The exclusive lock is acquired only after verification produces a receipt, and it spans exactly: expected-HEAD re-validation, staged-set parity check, commit, and post-commit inspection. A Release build or test run MUST NOT execute while the lock is held.
  - **Lock acquisition is bounded.** The default timeout is **30 seconds**; expiry returns a fixed diagnostic and never force-breaks a live lock.
  - **Re-verification is bounded and non-starving.** If HEAD moves between verification and lock acquisition, the receipt is invalidated and re-verification is limited to **3 attempts**. On exhaustion the phase blocks with a fixed diagnostic instructing human coordination rather than retrying indefinitely.
  - The 30-second default and 3-attempt maximum are machine-owned schema/manifest facts, configurable only by a revision-bound approved manifest. Tests inject shorter deterministic bounds with exact barriers/events and no sleeps.
  - Concurrent closers make progress: with N contending agents every agent either closes or blocks with a fixed diagnostic; none waits unboundedly.
  - Unrelated staged state is preserved.
  - Staged-set parity is asserted against the literal planned packet path list before commit; any extra path aborts before commit.
  - Interruption retains bounded state and never auto-cleans or reclaims dirty files.
  - **Break-glass repair is bounded by construction (CTO finding C2; `IVSD-M008`).** The control-plane repair bypass suspends claim acquisition only. Path-ownership validation, staged-set parity, literal path lists, and the prohibition on tool-initiated `revert`/`reset`/`checkout`/`stash`/`clean` remain enforced while it is active. Each use requires explicit per-repair human authorization, is never self-invocable by an executor, is never session-scoped or standing, records a receipt with a bounded enumerated reason code, and is surfaced by the Phase 2 `status` and `doctor` behavior.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Fence shared-develop ownership and phase closure.
- **Rollback / failure handling:** Disable claim/close commands without modifying working files; retain read-only validation and receipts for diagnosis.

### Phase 3 — Content-Addressed Decision And Execution Packets

- **Goal:** Compile the smallest revision-valid packet for planning, review, implementation, and cold resume.
- **Depends on:** Phase 1 manifest; Phase 2 claims for mutating packet consumers.
- **Relevant / phase-owned files:**
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/ContextPacket.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/BuildContextPacketCommand.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/ContentAddressedPacketStore.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/MarkdownSectionReader.cs` (new)
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ContextPacketCompilerTests.cs` (new)
  - `.agents/CONTEXT_ENGINEERING.md` (existing)
  - `.agents/benchmarks/cold-start-tasks.yaml` (existing)
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md` (existing)
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml` (existing)
  - workstream tasks/context artifacts (existing)
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md` (existing)
- **Related skills/rules:** context engineering, implementation-plan, senior-cto-feedback.
- **Acceptance criteria:**
  - Packet contains current task, exact selected headings, matched rules/skills, scope, tests, approvals, paths, and source hashes only.
  - Cache key is `path + heading/symbol + Git blob/content hash`.
  - Stale handles fail; unchanged bytes are referenced, not duplicated.
  - Existing byte, duplicate, registry-read, and scout-output limits are executable gates.
  - Packet cache persists only content-addressed handles, hashes, byte counts, and bounded locators; requested source is retrieved through normal repository tools and is not copied into the cache.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Compile bounded revision-valid agent execution packets.
- **Rollback / failure handling:** Fall back to existing bounded manual retrieval; do not serve a stale or truncated packet as complete.

### Phase 4 — Persistent Approved-Goal Execution

- **Goal:** Resume approved work idempotently through explicit legal states, one safe next action, and a privacy-bounded goal-status view.
- **Depends on:** Phases 1–3.
- **Relevant / phase-owned files:**
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Domain/GoalExecution.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalCommands.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Application/GoalStatusCommand.cs` (new)
  - `eng/agent-workflow/src/ISLAMU.AgentWorkflow/Infrastructure/WorkflowRunStore.cs` (new)
  - `eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/PersistentGoalExecutionTests.cs` (new)
  - `.agents/skills/persistent-goal-execution/SKILL.md` (new)
  - `.agents/contract/intents.yaml` (existing)
  - `.agents/CONTEXT_ENGINEERING.md` (existing)
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md` (existing)
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml` (existing)
  - workstream tasks/context artifacts (existing)
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md` (existing)
- **Related skills/rules:** `skill-authoring`, `implementation-plan`, `senior-cto-feedback`.
- **Acceptance criteria:**
  - States cover validated, CTO-approved, user-approved, claimed, implementing, verifying, commit-ready, committed, complete, blocked, interrupted, and needs-replan.
  - `goal start|next|record|resume|block|abort` never calls a model or mutates outside a claim.
  - Crash/restart tests at each transition prove no skipped approval or duplicate commit.
  - Uncertain commit inspects repository truth before retry.
  - Scope/architecture/acceptance/risk change routes to `NeedsReplan`.
  - `goal status` reads a bounded fixed-field view showing owner, state, next action code, blocker code, expected HEAD, and last verified commit.
  - Persistent goal files and status output reject source/prompt text, secrets, PII, raw provider/model payloads, free-form exceptions/reasons, and command payloads before persistence or display.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet`
- **Phase-close commit outcome:** Resume approved agent goals through deterministic state.
- **Rollback / failure handling:** Stop execution, preserve manifest/run receipts, release OS lock, and leave dirty state untouched for human recovery.

### Phase 5 — Harness Adapter And CI Gate Convergence / Final Delivery

- **Goal:** Route every supported harness and CI through one tested provider-neutral policy surface, remove obsolete hook/validator code, then reconcile the final artifacts and obtain independent review.
- **Depends on:** Phases 1–4 command and receipt surfaces.
- **Mandatory delivery split (CTO finding B3):** Phase 5 MUST close as three separate commits, not one. A single 27-path commit is not independently reviewable, and — more seriously — committing the deletions together with the replacement adapter leaves no revision in history containing both, which makes this phase's own documented rollback ("restore the last known-good hook entrypoints from the previous commit") impossible to execute.
  - **5A — Adapter unification.** `.agents/hooks/AgentWorkflowHook.cs`, `.agents/hooks/README.md`, `.claude/settings.json`, `.codex/hooks.json`, `.cursorrules`, `.github/copilot-instructions.md`, `tests/Event.Architecture.Tests/AgentWorkflowHarnessArchitectureTests.cs`. Old surfaces remain present but inactive. This commit remains independently reviewable.
  - **5B — Dedicated CI lane.** `.github/workflows/agent-context.yml`, `.github/workflows/test.yml`, `docs/CI_CD_GOVERNANCE.md`. Its pending contents are verified together with 5A before either commit executes.
  - **5C — Obsolete surface deletion and mutable-state reconciliation.** Deletes the five hook scripts, `eng/agent-context/validate-contract.cs`, and its lock file; migrates references in `AGENTS.md`, `.agents/contract/*`, and remaining docs; and commits the final tasks/context/execution/I-VSD state. This commit runs only after Gate 5-I finalizes a parity receipt bound to the byte-equivalent post-5B revision.
- **Relevant / phase-owned files:**
  - `.agents/hooks/AgentWorkflowHook.cs` (new)
  - `.agents/hooks/README.md` (existing)
  - `.agents/hooks/SecurityCheck.cs` (delete after parity)
  - `.agents/hooks/SkillTrigger.cs` (delete after parity)
  - `.agents/hooks/ContextTracker.cs` (delete after parity)
  - `.agents/hooks/FormatCode.cs` (delete after parity)
  - `.agents/hooks/BuildCheck.cs` (delete after parity)
  - `.claude/settings.json` (existing)
  - `.codex/hooks.json` (existing)
  - `.cursorrules` (existing)
  - `.github/copilot-instructions.md` (existing)
  - `.github/workflows/agent-context.yml` (new)
  - `.github/workflows/test.yml` (existing)
  - `AGENTS.md` (existing)
  - `.agents/contract/intents.yaml` (existing)
  - `.agents/contract/README.md` (existing)
  - `eng/agent-context/validate-contract.cs` (delete after parity)
  - `eng/agent-context/packages.lock.json` (delete with obsolete script)
  - `tests/Event.Architecture.Tests/AgentWorkflowHarnessArchitectureTests.cs` (new)
  - `docs/CI_CD_GOVERNANCE.md` (existing)
  - `docs/AGENTIC_CONTEXT_ENGINEERING.md` (existing)
  - `docs/OPERATIONS.md` (existing)
  - `docs/TESTING.md` (existing)
  - `dev/active/agentic-workflow-control-plane/agentic-workflow-control-plane-execution.yaml` (existing)
  - workstream tasks/context artifacts (existing)
  - `islamic-value-sensitive-design/i-vsd-agentic-workflow-control-plane.md` (existing; included in increment 5C only)
- **Related skills/rules:** `ci-cd-change`, `ip-clean-room`, `skill-authoring`.
- **Acceptance criteria:**
  - All adapter commands are repository-relative and invoke one normalized CLI surface.
  - Synthetic equivalent events yield equivalent safety decisions.
  - Required mutation/authority gates fail closed; graph/status metrics can fail open with bounded diagnostics.
  - Hook doctor reports missing files, unsupported events, stale agent/skill names, and configuration drift.
  - Always-present `agent-context` workflow runs for agent/context changes and supports PR, push, merge queue, schedule/manual where appropriate.
  - Existing `test.yml` no-op remains intentional because the dedicated lane now exists.
  - Old hook scripts and bootstrap validator are deleted only after command/reference parity is green, and **only in increment 5C — never in the same commit as the replacement adapter.**
  - Before either 5A or 5B commits, Gate 5-I captures a deterministic verified snapshot/tree manifest binding expected HEAD, the combined pending 5A+5B path contents and hashes, exact planned path sets, and relevant obsolete-surface presence. Release build, `Event.Architecture.Tests`, hook/CI doctor, and workflow dry-run execute against that uncommitted combined state.
  - A green Gate 5-I verification disposition authorizes commits 5A then 5B. After 5B, exact path/tree hashes and obsolete-surface presence MUST equal the verified snapshot. Only then is the parity receipt finalized and bound to the exact post-5B commit. Any verification mismatch, commit failure, or post-5B equivalence mismatch blocks before Task 5.4.
  - The byte-equivalent post-5B revision, not 5A alone, is the rollback anchor for 5C.
  - No published history, release policy, deployment authority, or secret permissions change.
  - After 5C, reconcile the plan/context/tasks/I-VSD/intent/canonical-strategy artifacts and all seven commit receipts, explicitly account for the intentionally stale execution manifest, and obtain independent final review before completion.
- **Phase-end verification (two gates — CTO finding C1):** Phase 5 is the one
  documented exception to the single phase-end gate, because it replaces an
  authority surface and then deletes the one it replaced. A single gate after the
  deletion task would run every test *after* the obsolete surfaces were gone, so
  the parity `IVSD-M006` requires — proven while both are present — would never
  execute.
  - **Gate 5-I**, after Tasks 5.1–5.3 and before commits 5A/5B, with the obsolete surfaces still present: capture the deterministic combined pending-tree snapshot described above, then run
    `dotnet build --configuration Release --verbosity quiet`,
    `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`, hook/CI doctor, and workflow dry-run against that exact uncommitted state.
    Record the green verification disposition before committing. Execute 5A then 5B only after green; compare the exact post-5B path/tree hashes and obsolete-surface presence to the snapshot, then finalize the parity receipt bound to the post-5B commit. Any mismatch or commit failure blocks Task 5.4 / commit 5C.
  - **Gate 5-II**, after Task 5.4 with the obsolete surfaces deleted: the same two
    commands, proving the repository is green without them. It gates commit 5C.
- **Phase-close commit outcomes (three commits — CTO finding B3), followed by final artifact reconciliation and independent review:**
  - 5A: Route harness adapters through one agent workflow gate.
  - 5B: Add the always-present agent-context CI lane.
  - 5C: Delete obsolete hook and bootstrap validator surfaces.
- **Rollback / failure handling:** Verification precedes commits: Gate 5-I first proves the deterministic uncommitted combined 5A+5B snapshot. The final receipt names the exact post-5B revision only after its path/tree hashes and obsolete-surface presence are byte-equivalent to that snapshot. That post-5B revision is the rollback anchor for 5C because it contains the independently reviewable 5A adapter, the 5B CI lane, and the still-present obsolete surfaces. Any failed commit or equivalence mismatch blocks before 5C. Revert 5C back to the receipt-bound post-5B revision first; only then may 5B and 5A be reverted in reverse order if separately required. Never keep two active safety authorities after rollback.

## 7. Testing Strategy

### Invariant anchors

- `ISLAMU.AgentWorkflow.Tests` owns state, schema, Git coordination, packet, recovery, fixed goal-state privacy, and adapter normalization behavior.
- `Event.Architecture.Tests` owns repository integration, path/rule/hook conventions, and solution dependency boundaries.

### High-leverage adversarial coverage

- Two claimants for the same path with deterministic barriers.
- HEAD movement between verification and close.
- Mixed owned/unowned hunks in one file.
- Stale approval and changed artifact digests.
- Crash injection before/after each state receipt.
- Uncertain commit result and idempotent resume.
- Packet hash drift, duplicate bytes, and budget overflow.
- Hook safety failure versus advisory failure.
- Forbidden persistent-goal fields and partial-write prevention.

### Phase verification matrix

| Phase | Release build | Selected project test | Repetition reason |
|---|---|---|---|
| 1 | Solution Release build | `ISLAMU.AgentWorkflow.Tests` | Establish tool behavior |
| 2 | Solution Release build | `ISLAMU.AgentWorkflow.Tests` | Extends same state/Git boundary |
| 3 | Solution Release build | `ISLAMU.AgentWorkflow.Tests` | Extends same packet boundary |
| 4 | Solution Release build | `ISLAMU.AgentWorkflow.Tests` | Extends same persistent state machine |
| 5 — Gate 5-I (pending 5A+5B state; receipt finalized post-5B) | Solution Release build | `Event.Architecture.Tests` | Verify combined uncommitted snapshot first; commit 5A/5B only on green; bind receipt after post-5B byte-equivalence; gates 5C |
| 5 — Gate 5-II (after deletion) | Solution Release build | `Event.Architecture.Tests` | Proves the repository is green without the obsolete surfaces; gates commit 5C |

This matrix is a summary. **Section 6 is authoritative** for every phase's gates;
if the two ever disagree, Section 6 wins and this table is the defect. Phase 5 is
the only phase with two gates, for the reason stated in its Section 6 entry.

No browser, Aspire, Docker, external service, manual runtime walkthrough, or
E2E project is part of a phase gate.

Targeted TUnit `--treenode-filter` slices are allowed inside Red/Green tasks to
prove the intended failure and turn it green. The one Release build plus one
full selected-project test remains the only phase-end gate.

## 8. Documentation, Configuration, And Operations Impact

### Documentation

Update:

- `AGENTS.md`
- `.agents/CONTEXT_ENGINEERING.md`
- `.agents/contract/README.md`
- `.agents/hooks/README.md`
- `docs/AGENTIC_CONTEXT_ENGINEERING.md`
- `docs/CI_CD_GOVERNANCE.md`
- `docs/OPERATIONS.md`
- `docs/TESTING.md`

### Configuration

- Durable workstream execution state lives in `dev/active/<task>/*-execution.yaml`.
- Ephemeral claims, locks, and packet cache live under `.git/islamu-agent/`; persistent goal state uses fixed schema-owned fields only.
- No secret or environment configuration is required.
- Harness adapters use repository-relative commands only.

### Operations

- Add contributor commands for validate, claim, packet, goal, doctor, status, and close.
- Document interruption, uncertain commit, stale claim, HEAD movement, and replan recovery.
- Safety gates fail closed; advisory integrations degrade visibly.

### Release, Changelog, And Phase Commit Strategy

All seven commits are internal architecture/CI changes using explicit `Changelog: skip` trailers. Exact path lists and commands remain solely in the task ledger; packet composition follows Decision 6.

## 9. Islamic Value-Sensitive Design Mapping

| I-VSD ID | Finding / mitigation status | Scenario and task mapping | Disposition |
|---|---|---|---|
| `IVSD-F001` / `IVSD-M001` | Accepted | Requirements 1/4; Tasks 1.1–1.3, 4.1–4.3 | Implement |
| `IVSD-F002` / `IVSD-M002` | Accepted | Requirement 2; Tasks 2.1–2.3 | Implement |
| `IVSD-F003` / `IVSD-M003` | Accepted | Requirements 3/4; Tasks 3.1–3.3, 4.1–4.3 | Implement |
| `IVSD-F004` / `IVSD-M004` | Accepted | Requirements 3/5; Tasks 3.3, 5.1–5.3 | Implement |
| `IVSD-F005` / `IVSD-M005` | Resolved by scope removal | Deliberate non-goal: no replay/live-model evaluation implementation | No implementation |
| `IVSD-F006` / `IVSD-M006` | Accepted | Requirement 5; Tasks 5.1–5.4 | Implement |
| `IVSD-F007` / `IVSD-M007` | Accepted | Requirement 4; Tasks 4.1–4.3 | Implement |
| `IVSD-F008` / `IVSD-M008` | Open — new in the post-CTO revision | Requirement 2; Task 2.4 | Implement |

No scholarly/legal escalation blocks planning. Phase 1 is complete. Phase 2 is
blocked pending fresh revision-bound Tier 1 approval of the corrected planning
packet. Fresh user approval also remains required for any future expansion of
executor authority, persisted machine-state fields, vendor-specific canonical
state, or dirty-state takeover.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundaries:** CLI inputs, YAML manifests, Git paths, hook events, and receipts are untrusted until parsed and validated.
- **Authorization:** Separate CTO, user implementation, and phase-commit authority. No executor self-approval.
- **Path safety:** Reject absolute paths, traversal, backslashes where unsupported, aliases, symlinks/reparse points, and case/NFC collisions. Commit path sets are literal file lists; directory paths, globs, and `git add -A/-u/.` are forbidden at every layer.
- **Claim scope boundary (CTO finding M2):** Claims live in `.git/islamu-agent/` and are therefore **checkout-local**. They coordinate agents sharing one working tree and provide **zero** coordination across separate clones or machines. A contributor or self-hoster running two checkouts of the same repository receives a successful claim that protects nothing. `doctor` MUST surface this scope explicitly, and the operator documentation MUST state it. Cross-clone coordination is out of scope and is not silently implied.
- **Git safety:** No shell-interpreted commands, broad staging, worktrees, stash/reset/checkout, automatic cleanup, or history rewrite.
- **Privacy:** Manifests, packet caches, claims, receipts, and persistent goal state use fixed fields only; no source/prompt/secret/PII/raw response/provider payload/free-form field/command payload.
- **Abuse:** Bound manifest size, packet bytes, file counts, process output, and command duration. The schema defaults closure-lock acquisition to 30 seconds and limits re-verification to 3 attempts; only a revision-bound approved manifest may configure those machine-owned facts.
- **Fail posture:** Mutation/authority failures fail closed; advisory graph/metrics failures warn.
- **Audit:** Receipts bind action, state transition, expected HEAD, path hashes, command result, and resulting commit object without actor PII.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | Not applicable to tool state | Repository workflow is checkout-scoped; no product tenant data belongs in state |
| Federation | Not applicable | No product federation behavior |
| Localization | Not applicable initially | CLI diagnostics use stable English codes; future localization must not change canonical codes |
| Accessibility | Applicable | Status/errors must be plain-text, deterministic, screen-reader/terminal friendly, with no color-only meaning |
| Product behavior | Not applicable | No runtime/API/UI product change |
| Contributor fairness | Applicable | Provider-neutral bytes/capability tiers and local execution keep participation accessible |
| Self-hosting | Applicable | Entire control plane works offline from repository-local .NET/Git state |

## 12. Observability And Operations

### Fixed machine-state privacy

Manifests, packet-cache entries, claims, receipts, and persistent-goal files may
store only schema-owned identifiers, states/codes, digests, byte counts, bounded
locators, expected HEAD, and commit identifiers. They reject prompt/source text,
secrets, PII, raw model/provider output, provider URLs, free-form exceptions or
reasons, and command payloads before persistence. `goal status` renders only the
same fixed fields.

### Recovery surfaces

- `status`: current owner/state/next/blocker/last good commit.
- `doctor`: adapter/tool/schema/claim/cache health.
- `goal resume`: one safe state transition or explicit block.
- `phase close --dry-run`: ownership, HEAD, packet, and verification disposition without commit.
- **Break-glass control-plane repair (CTO finding M1):** From Phase 2 onward the control plane governs the workflow that builds and repairs the control plane, so a defect in the claim store can block its own fix. A documented break-glass path allows claim enforcement to be bypassed for control-plane repair only, under explicit per-repair human authorization, with the bypass recorded as a receipt carrying a **bounded enumerated reason code** — never free-form operator text, which would reopen the free-form-payload hole the fixed machine-state privacy boundary forbids everywhere else (`IVSD-M003`, `IVSD-M008`, CTO finding C5). The bypass never suppresses path-ownership or staged-set parity checks — it suspends only claim acquisition, never the defenses that protect another contributor's work. Routine use is evidence of a claim-store design defect and triggers re-planning rather than a wider bypass.

## 13. Migration And Compatibility Plan

This is a clean replacement program:

1. Phase 1 introduces typed state beside the active bootstrap validator.
2. Each later phase migrates one owned behavior and proves parity.
3. Phase 5 updates every repository reference and deletes obsolete hook/validator code.
4. No deprecated command aliases or long-lived adapters remain.
5. Existing active workstreams are not bulk rewritten. A maintainer opts one current slice into execution YAML, binds fresh approvals, and preserves historical Markdown.
6. Archived workstreams remain historical evidence and are never reactivated as state.

Rollback is phase-local and forward-fix oriented. Ephemeral `.git` state may be
abandoned but never used to delete working files. Durable execution manifests
remain inspectable and can be marked blocked/superseded without fabricating
success.

## 14. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection signal | Owner / task |
|---|---:|---:|---|---|---|
| Execution YAML becomes another stale source | Medium | High | Strict machine-only ownership and digest parity | `validate-workstream` mismatch | 1.1–1.3 |
| Lease expiry enables unsafe takeover | Medium | Critical | Generation fence; dirty stale claims require human coordination | claim generation/dirty mismatch | 2.1–2.3 |
| Git adapter captures/commits another contributor's uncommitted work | **High** | Critical | Prevention: literal path lists, normalized ownership resolution, and pre-commit staged-set parity block. Detection/containment: post-commit tree comparison stops further automation. No-tool human recovery commands limit secondary harm only. | committed tree differs from planned packet path list | 2.3 |
| Phase-closure serialization causes livelock or starvation | Medium | High | Verification executes outside the closure lock; 30-second default acquisition timeout; maximum 3 re-verification attempts; machine-owned bounds configurable only by a revision-bound approved manifest; block with fixed diagnostic on exhaustion | lock wait or retry count exceeds approved manifest bound | 2.1–2.3 |
| Control plane breaks the workflow that repairs the control plane | Medium | High | Break-glass bounded by construction: suspends claim acquisition only, per-repair human authorization, enumerated reason code, never self-service | claim store unreadable or claim command fails | 2.4 |
| Break-glass normalizes into a routine authority escape | Medium | High | Never session-scoped or standing; surfaced by `status`/`doctor`; routine use triggers re-planning rather than a wider bypass | repeated bypass receipts across unrelated repairs | 2.4 |
| Context packet or persistent goal state leaks source/PII | Medium | Critical | handle-based cache, fixed goal schema/status, forbidden-field tests | fixed privacy rejection code | 3.1–3.3, 4.1–4.3 |
| Persistent executor amplifies bad plan | Medium | Critical | immutable approvals, no scope expansion, `NeedsReplan` | digest/authority mismatch | 4.1–4.3 |
| Adapter migration removes a working gate too early | Medium | High | synthetic parity before deletion; one active authority | doctor/parity failure | 5.1–5.3 |
| Control-plane project becomes shallow wrapper sprawl | Medium | Major | deep domain/application modules and deletion test | excessive command/pass-through classes | all phases |
| Shared dirty tree causes unrelated build failure | High | Major | ownership classification and scoped evidence; do not repair others | external-path failure receipt | every phase |

## 15. Success Metrics And Definition Of Done

The program is complete only when:

- all seven planned commits across the five phases are verified and recorded;
- illegal transition and stale approval tests fail closed;
- overlapping path/HEAD/mixed-hunk races are deterministic and green;
- a cold resume packet stays within declared byte/duplication budgets;
- interrupted/uncertain execution resumes without skipped approval or duplicate commit;
- every supported adapter passes synthetic parity or is explicitly unsupported;
- agent-context changes run an always-present dedicated CI gate;
- fixed-field privacy tests prove forbidden content never enters manifests, packet caches, claims, receipts, or persistent goal state;
- current docs describe implemented behavior without retaining proposal language;
- obsolete bootstrap/hook surfaces are deleted after parity.

## 16. Implementation Agent Contract — Keep Dev Docs Current

1. On first start or cold resume, read context and the current task, then only the referenced plan headings.
2. Treat tasks as the sole execution ledger; update substantial tasks immediately and small related tasks before phase exit.
3. Treat the execution YAML as machine state only; never copy architecture, findings, or handoff prose into it.
4. Never edit without the declared path claim once Phase 2 is active.
5. Run one Release build and the selected project test only after all phase implementation tasks.
6. Record phase-attributable failures separately from proven unrelated shared-tree failures; never claim repository-wide green.
7. Execute the planned commit packet directly when truthful; load `conventional-commit` only for a permitted material override.
8. Update context after each phase, blocker, decision, validation failure, material discovery, interruption, or handoff.
9. Update this plan only for scope, architecture, phase order, acceptance, risk, or validation changes.
10. A scope, authority, persisted-field, or provider-routing change triggers I-VSD revalidation and fresh CTO/user approval.
11. Never modify, unstage, stage, commit, clean, or reclaim another contributor's work.
12. Before pause/transfer, reconcile task status, execution state, evidence, modified paths, and next safe action.

Every implementation summary teaches the architecture, state transition, files,
control flow, safeguards, exact verification, remaining work, and whether
plan/context/tasks/I-VSD changed.

## 17. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact build/test/receipt evidence
Remaining: incomplete or deferred work
Next: one safe next task or approval gate
Docs updated: tasks/context/plan/I-VSD state and reason
```

## 18. Potential Risks & Unknowns

The hardest risk is not building the state machine; it is avoiding a second,
equally complex source of truth. The execution manifest must remain strictly
machine factual, while Markdown retains design and human meaning. If future
implementation begins parsing prose or duplicating architecture into YAML,
stop and re-plan rather than expanding the schema.

The second risk is concurrent dirty state. No lease timeout or automation can
prove ownership of ambiguous hunks. The correct behavior is to block and ask
for human coordination, not to make the tool more aggressive.

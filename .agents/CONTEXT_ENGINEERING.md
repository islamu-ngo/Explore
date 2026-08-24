<!-- ABOUTME: Canonical context-budget and retrieval policy for repository agents and subagents. -->
<!-- ABOUTME: Prevents duplicate reads, preserves the main agent's working set, and routes broad discovery to economical scouts. -->

# Context Engineering Contract

## Objective

Give the main agent the smallest decision-complete working set. Repository context is retrieved once, summarized once, and reused until the underlying file or decision changes.

## Context Ledger

The main agent keeps a short in-session ledger of loaded evidence using `path + heading/symbol + revision`. Before retrieving anything, check the ledger and current conversation.

- Do not reread unchanged content already present in the session.
- A summary does not require reopening its sources unless the source changed, the summary is insufficient for a concrete decision, or contradictory evidence appears.
- After an edit, reread only the changed symbol/section and invalidated callers or tests.
- A handoff names evidence locations; it does not paste source, logs, plans, or previous conversation text.

## Retrieval Order

1. Resolve only the matching intent entry; never load the full intent registry as task context.
2. Use the knowledge graph or structural outline to locate owners, symbols, callers, and tests.
3. Retrieve only the required symbol, Markdown heading, or bounded line range.
4. Load one sibling pattern and the directly relevant test when implementation needs precedent.
5. Expand only when the first retrieval leaves a named decision unresolved.

Raw full-file reads are a fallback for small files with no structural retrieval path. Never use a broad read when an outline, symbol, heading, diff, or graph query can answer the question.

## Automated Blast Radius & Pre-Flight Graph Injection

To achieve zero-turn dependency discovery and eliminate multi-turn manual exploration, agents utilize pre-flight blast radius analysis on Turn 1. Using the repository's `code-review-graph` MCP tools (`get_impact_radius_tool`, `get_affected_flows_tool`), the agent injects a compact, bounded ~1 KB structural context slice into the intake notes or implementation plan:

```yaml
# Injected Structural Context (Pre-Flight Blast Radius)
Target: <Namespace.ClassName.MethodName>
Callers (Upstream):
  - <Controller.Action> (Route: <route_template>)
  - <Blazor.Component.Handler>
Callees (Downstream):
  - <Repository.Method>
  - <Outbox.EnqueueAsync> (Event: <DomainEventName>)
Impacted Flows:
  - Flow: <BusinessFlowName> (Criticality: <Tier>)
Test Coverage:
  - <PathToUnitTests>
  - <PathToIntegrationTests>
```

### Protocol Invariants:
1. **Zero-Turn Context**: Map the full vertical dependency chain (API Route $\rightarrow$ MediatR Handler $\rightarrow$ Outbox Event $\rightarrow$ DB Repository $\rightarrow$ Tests) on Turn 1 before authoring changes.
2. **Side-Effect Prevention**: Ensure downstream side effects (Outbox events, cache invalidation, UI links) are captured upfront.
3. **Context Budget Economy**: Replace multi-step manual file traversal with a single structured graph query.

## Default Budgets

The machine-readable limits live once in [`benchmarks/cold-start-tasks.yaml`](benchmarks/cold-start-tasks.yaml) under `context_budget`. They cover additional repository bootstrap after already-injected `AGENTS.md`, one retrieval result, the initial discovery result, scout output, duplicate unchanged bytes, and full-registry reads.

When a tool would exceed a budget, return a summary plus artifact/path handles and retrieve the next section only if needed. Build and test tools retain full logs outside the conversation and return exit code, counts, first actionable failures, and the artifact path.

## Model Economy And Delegation

Use model capability for decisions, not for bulk reading.

| Work | Default model tier |
|---|---|
| File discovery, symbol inventory, documentation routing, codebase search, mechanical evidence collection | `economical` |
| Focused implementation and deterministic verification | `balanced` |
| Architecture synthesis, security/privacy judgement, adversarial review, unresolved multi-system debugging | `advanced` |

The main agent retains goals, constraints, decisions, and synthesis. Economical read-only scouts receive one narrow question, exact scope, and the machine-readable result cap. They return findings and locations only, never raw files, command logs, or copied documentation. Escalate a scout to a stronger model only after a concrete ambiguity or failed evidence pass, not because the repository is large.

Do not delegate an atomic lookup: each subagent pays its own bootstrap cost. Delegate only when the scout keeps a larger body of discovery out of the main context or when independent lanes can run in parallel.

## Dynamic Exploration Budget & Criticality Matrix

Context budgets dynamically adapt based on the task's criticality tier resolved from [`.agents/contract/intents.yaml`](contract/intents.yaml):

| Criticality Tier | Exploration Protocol | Intake & Inquiry Mode | Test Strategy | Multi-Agent Review |
|---|---|---|---|---|
| **Tier 0: Sovereign** | Exhaustive Knowledge Graph (callers, callees, outbox, DB locks, ADRs) | Mandatory `/grill-me` (money flows, hold expiration, refund authority) | Invariant-Breaker concurrency tests + Postgres + Stryker (>85%) | Anonymized Epistemic MAD (Weighted Voting) |
| **Tier 1: Security** | Exhaustive Graph + Policy (Cerbos, BFF, global filters, tokens) | Mandatory `/grill-me` (threat modeling, fail-closed auth, tenant spoofing) | Invariant-Breakers + multi-provider DB tests + Stryker (>85%) | Anonymized Epistemic MAD (Weighted Voting) |
| **Tier 2: Privacy** | Exhaustive Data Flow (all `*Pii` fields, `IAiContextGateway`, log sinks) | Mandatory `/grill-me` (erasure authority, anti-resurrection, receipt tokens) | Invariant-Breakers + log sink PII scans + purge tests | Anonymized Epistemic MAD (Weighted Voting) |
| **Tier 3: Domain State** | Bounded caller/callee tracing of target aggregate/handler | Standard Q&A (only if requirements are ambiguous) | Behavioral CQRS unit and integration tests | Peer Review (`backend-engineer-agent`) |
| **Tier 4: Standard** | Local surface reading (target razor/css/doc file only) | Autonomous defaults (zero unnecessary interruptions) | Affordance & component render tests | Lightweight Self-Check (`presentation-engineer-agent`) |

## Research Boundary

Repository research stays local first. Official documentation or external research is loaded only for a named unresolved framework, protocol, dependency, standard, or advisory question. Search results are summarized into repository-relevant facts and source handles; raw external content never enters implementation context.

## Tooling & Execution Environment Boundary

Agents and subagents must operate strictly within the repository's native environment and toolset:

- **Prohibited Runtimes**: Never run or generate ad-hoc Python (`python`, `python3`, `pip`, `python -c`) or JavaScript/Node.js (`node`, `npm`, `npx`, `node -e`) scripts for file inspection, text extraction, data transformation, or scratch automation.
- **Allowed Edit Tools**: Use native agent file manipulation tools (`apply_patch`, `replace_file_content`, `write_to_file`) or built-in IDE diff mechanisms.
- **Allowed Shell Utilities**: Use standard POSIX Bash commands (`grep`, `sed`, `awk`, `find`, `jq`, `git`, `dotnet`).
- **Scripting is a Last Resort (High-ROI Only)**: Do not create temporary or scratch scripts for one-off tasks. If a persistent developer automation tool provides indispensable, lasting architectural value, it MUST reside under `eng/` (e.g. `eng/scripts/` or `eng/tools/`) as a file-based C# script (`*.cs` runnable via `dotnet run`) or Bash script.
- **CI/CD Isolation**: Scripts under `.ci/scripts/` are strictly dedicated to CI/CD pipeline automation (e.g. release bundle verification, license policy enforcement) and must never be repurposed for agent/dev scratch tasks.
- **Secrets Source of Truth**: Secrets, connection strings, API tokens, passwords, and encryption keys must strictly reside in **Infisical** or **`.env`** (with schema/keys documented in **`.env.example`**). NEVER hard-code, define, or embed secrets in `AppHost.cs`, test fixtures, classes, or configuration files.
- **Verification Scoping Discipline**: Documentation, agent contract, and markdown-only changes (Tier 4) must NEVER run full `dotnet build` or .NET test suites. Verification for documentation tasks is strictly scoped to file format, schema validation, and link integrity.

## Workstream And Handoff State

For a substantial active workstream, resume from its task-owned `*-context.md`. Read the current state and current task first, then zoom only the referenced plan heading. Do not automatically load plan, context, and task files together.

A handoff is written directly into the task-owned context and contains only:

- current outcome and next action;
- decisions and unresolved risks;
- modified paths;
- verification commands and results;
- evidence locations;
- unrelated dirty-worktree notes.

Generic dev-directory README and handoff templates are intentionally absent because directory-context systems may inject them into every nested read.

## Measurement

Cold-start benchmarks record first-turn input tokens, maximum live context, cumulative input/cache-read tokens, tool-result bytes, duplicate bytes by content hash, full-file reads, and scout result size. Correctness is required, but a scenario that exceeds its context budget is not a pass.

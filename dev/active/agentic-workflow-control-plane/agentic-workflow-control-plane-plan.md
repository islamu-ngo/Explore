<!-- ABOUTME: Cancellation tombstone for the former agentic workflow control-plane plan. -->
<!-- ABOUTME: Preserves the lightweight guard boundary and prevents revival of the Agent OS roadmap. -->

# Agentic Workflow Control Plane — Cancelled Implementation Plan

Last Updated: 2026-09-01 Europe/Brussels

## Status And Authority

**Status: CANCELLED — DO NOT IMPLEMENT OR RESUME.**

This decision supersedes every earlier revision, approval, digest, task, phase,
review, execution manifest, and persistent goal associated with this workstream.
Historical Phases 2 through 6 have no remaining implementation authority. Git
history is provenance only; it is not permission to reconstruct the roadmap.

This file remains at the former active-plan path solely as a tombstone so stale
goals and historical references resolve to the cancellation instead of reviving
the deleted design. It is intentionally not accompanied by context, tasks,
execution-state, CTO-review, or I-VSD artifacts.

## Retained Boundary

The only retained agent-workflow tool is the dependency-light C# guard under
`eng/agent-workflow`. It may do exactly two things:

1. Validate that `.agents/contract/intents.yaml` is one bounded, valid UTF-8 YAML
   document.
2. Validate that a described `git commit` command names distinct literal files
   after `--`, never `.`, directories, globs, traversal, rooted paths, Git
   pathspec magic, control characters, or duplicates.

The guard is read-only. It never executes Git or owns workflow state.

## Explicit Non-Goals

Do not add or restore:

- workstream manifests, approval receipts, or plan/task/context digest chains;
- file claims, leases, heartbeats, lock daemons, or shared-checkout coordination;
- persistent goal state machines or autonomous resume behavior;
- custom context packet compilers, caches, or content-addressed execution packets;
- harness adapters, hooks, CI orchestration, or Git mutation for this tool;
- compatibility shims for any deleted control-plane command or schema.

Parallel contributors use ordinary Git branches or worktrees. Git commit SHAs
provide provenance.

## Implementation Phases

There are no implementation phases or follow-up tasks. Do not derive work from
the deleted roadmap. Reopening any non-goal above requires a new, explicit user
request that names this cancellation and replaces it with a concrete product
need; stale goals, historical commits, and archived text are insufficient.

## Product Priority

Engineering effort returns to Explore (ISLAMU Event): event discovery,
registration, portable multi-database persistence, and the Blazor experience.

## Verification Boundary

```bash
dotnet run --project eng/agent-workflow/src/ISLAMU.AgentWorkflow/ISLAMU.AgentWorkflow.csproj -- validate-intents .agents/contract/intents.yaml
dotnet run --project eng/agent-workflow/src/ISLAMU.AgentWorkflow/ISLAMU.AgentWorkflow.csproj -- validate-commit -- git commit --only -m "message" -- src/ExactFile.cs docs/ExactFile.md
```

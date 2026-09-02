<!-- ABOUTME: Active working memory for the CTO audit remediation workstream. -->
<!-- ABOUTME: Tracks session progress, decisions, blockers, and handoff state across P0/P1/P2 remediation. -->

# CTO Audit Remediation — Context

Last Updated: 2026-09-02 Europe/Brussels

## Review State

- I-VSD report: `islamic-value-sensitive-design/i-vsd-cto-audit-remediation.md`
- I-VSD status / disposition: `current` + `plan-aligned`
- CTO review: Not reviewed
- User approval: Approved by the explicit implementation request on 2026-09-02

## SESSION PROGRESS (2026-09-02 Europe/Brussels)

### ✅ COMPLETED
- CTO audit identified P0/P1/P2 problems
- Full codebase research via 3 subagents (migration + API client + controller/warning/tenant)
- I-VSD assessment completed
- Planning created with evidence-grounded current state for all 6 phases

### 🟡 IN PROGRESS
- Phase 1, Task 1.3 — migration ownership documentation parity

### ⏭️ NEXT
1. Complete Phase 1, Task 1.3
2. Continue through the remaining Phase 1 consolidation tasks

### ⚠️ BLOCKERS
- None known

## Quick Resume

1. Read this context and `cto-audit-remediation-tasks.md`
2. Read only the current phase from `cto-audit-remediation-plan.md`
3. Start from the first unchecked task unless the user overrides

## Key Decisions

1. MariaDb routes to MySql assembly — not removed from runtime enum
2. Squash by delete+regenerate — no migration history preservation needed
3. NSwag `{controller}Client` naming — per-tag classes
4. Roslyn transformer enumerates all generated client interfaces
5. 17 TryParseConcurrencyStamp duplicators → inherit EventControllerBase (renaming ExploreControllerBase)
6. Generic CrudControllerBase and LookupControllerBase formally rejected (concrete controllers & composition preserved)
7. TreatWarningsAsErrors incremental ratchet (suppress → fix → unsuppress)
8. PostgreSQL RLS as defense-in-depth (uses existing `app.current_tenant_id` infrastructure)

## Constraints And Rules

- Never hand-edit migrations or model-snapshots (AGENTS.md #7)
- Greenfield breaking changes are first-class (AGENTS.md #11)
- Never hand-edit `EventApiClient.g.cs` (blazor-client.md)
- Generated artifacts travel with triggering commit
- Phase 5 (warnings) sequenced AFTER Phases 1-4 to avoid fixing soon-to-be-deleted code
- Phase 6 (RLS) is Tier 1 Security — requires invariant-breaker tests first
- Phase 1 Task 1.1 includes the existing composition and migration-ownership tests so the routing change is test-first and the MariaDb runtime dialect remains distinct.
- Phase 1 Task 1.2 owns every live build/test/container/lockfile/agent-contract reference to the deleted MariaDb migration projects; historical workstream evidence remains immutable.
- Phase 1 Task 1.3 uses canonical internal documentation paths plus the public backup/restore/upgrade twin; the stale `docs/CONFIGURATION.md` path does not exist.
- Phase 2 tasks 2.1-2.4 execute atomically across eight migration projects and eleven provider/context catalogs; `InitialCreate` is the canonical generated migration name.

## Phase Dependency Graph

```
Phase 1 (MariaDb merge) ─→ Phase 2 (Squash) ─→ Phase 6 (RLS needs clean baseline)
Phase 3 (NSwag split) ─→ independent
Phase 4 (Controllers) ─→ independent
Phases 1-4 ─→ Phase 5 (Warnings — last, clean codebase)
Phase 5 ─→ Phase 6 (RLS — warnings clean before security work)
```

## Handoff Notes

### Handoff — 2026-09-02
- **Current state:** Plan approved and implementation active.
- **Next action:** Complete Phase 1, Task 1.1 after the baseline gate.
- **Old workstream:** `dev/active/p0-consolidation/` removed.

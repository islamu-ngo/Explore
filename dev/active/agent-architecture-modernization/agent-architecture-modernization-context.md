# Agent Architecture Modernization Context

> **Task:** Modernize, refactor, and govern the subagent portfolio in `.agents/agents/`.
> **Status:** Planning / In Review
> **Last Updated:** 2026-08-12

## Current State & Problem Statement

1. **Path Drift**: The codebase migrated from `.claude/` to `.agents/`, but `_AGENT_SCHEMA.md` and agent files contained legacy `.claude/` path references.
2. **Schema Inconsistency**: `_AGENT_SCHEMA.md` Section 7 contained a stale 13-agent v1 draft list that contradicted the active 5-subagent architecture.
3. **Invalid Frontmatter Enums**: Frontmatter `type` fields used invalid values such as `implement`.
4. **Missing Invariants**: Domain, Clean Architecture, HAL UI link gating, Keycloak/Cerbos authz, and IP Clean Room rules were missing from agent instructions.

## Key Decisions

1. **Pre-v1 Development Stance**: Zero compatibility preservation for legacy prompt files or broken paths. Deprecate and purge all references to non-existent v1 agents.
2. **5 Canonical Role Subagents**: Maintain strictly 5 role-scoped subagents (`architect-agent`, `backend-engineer-agent`, `presentation-engineer-agent`, `quality-verifier-agent`, `librarian-agent`).
3. **Strict Schema Enforcement**: Every agent file must adhere to all 10 required sections in `_AGENT_SCHEMA.md` and stay under 160 lines.

## Mandatory Rules & References
- `AGENTS.md`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/legal/IP_GOVERNANCE.md`
- `docs/OPERATIONS.md`

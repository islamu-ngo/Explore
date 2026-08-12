---
name: Agents Documentation
description: Documentation index for repository subagent operational contracts
disabled: true
---

<!-- ABOUTME: Index and selection guide for repository role subagents in .agents/agents/. -->
<!-- ABOUTME: Documents agent selection, invocation rules, schema boundaries, and skill vs agent usage. -->

# Repository Subagent Registry

These agent files are **small, rereadable operational contracts** (50–120 lines target, 160 max). Every subagent file in this directory MUST conform to [`_AGENT_SCHEMA.md`](_AGENT_SCHEMA.md).

## Agent Selection Guide

| Role | Subagent File | Core Domain & Responsibility |
|------|---------------|------------------------------|
| **Architect** | [`architect-agent.md`](architect-agent.md) | High-level system design, dev-docs plan creation, ADRs, Aspire orchestration, IP clean-room verification. |
| **Backend Engineer** | [`backend-engineer-agent.md`](backend-engineer-agent.md) | Domain, Application, Persistence, CQRS (MediatR), EF Core, specification builders, transactional outbox. |
| **Presentation Engineer** | [`presentation-engineer-agent.md`](presentation-engineer-agent.md) | API controllers, HATEOAS link policies, Blazor UI, HAL link presence UI affordance gating, BFF proxy. |
| **Quality Verifier** | [`quality-verifier-agent.md`](quality-verifier-agent.md) | TUnit test suite execution, `Event.Architecture.Tests` validation, Release build profile checks, CI failure diagnosis. |
| **Librarian** | [`librarian-agent.md`](librarian-agent.md) | Documentation maintenance, clean-room research, AI tool contract inventory, durable journal synthesis (`dev/_journal/`). |

## Usage & Execution Rules

1. **Open the File Before Invoking**: Subagent files are small and contain authoritative constraints. Always read the target `.agents/agents/<name>.md` file first.
2. **Strict Schema Compliance**: Every subagent file must contain all 10 required sections in order as defined in [`_AGENT_SCHEMA.md`](_AGENT_SCHEMA.md).
3. **Single Mutating Agent**: Do not invoke multiple subagents concurrently if they modify the same file paths.
4. **Layered Context**: Subagents must link to [`AGENTS.md`](../../AGENTS.md), [`docs/QUICK_REFERENCE.md`](../../docs/QUICK_REFERENCE.md), and relevant `.agents/rules/*.md` rather than duplicating invariant text.
5. **Clean Room IP Protection**: Subagents must never ingest or copy third-party copyleft/proprietary source code. Follow [`docs/legal/IP_GOVERNANCE.md`](../../docs/legal/IP_GOVERNANCE.md).

## When to Use Agents vs Skills

- **Use Subagents** for multi-step or autonomous tasks requiring role-scoped investigation, planning, TDD verification, or architectural governance.
- **Use Skills** for inline patterns, specific code templates, and contextual cheatsheets during active editing.

## When NOT to Use Subagents

- Single-file edits with known file paths — use direct tools (`replace_file_content`).
- Simple grep or file search — use direct search tools (`grep_search`).
- Trivial questions answered directly in [`docs/QUICK_REFERENCE.md`](../../docs/QUICK_REFERENCE.md).


---
name: Agents Documentation
description: Documentation file - not an executable agent
disabled: true
---

# Project Agents

These agent files are **small** and meant to be re-read every time they are invoked. Do **not** rely on memory.

## Agent Selection Guide

| Role | Agent | Responsibility |
|------|-------|----------------|
| **Architect** | `architect-agent.md` | System design, refactor plans, ADRs, sequencing. |
| **Backend Engineer** | `backend-engineer-agent.md` | Domain, Application, Persistence, CQRS, EF Core. |
| **Presentation Engineer** | `presentation-engineer-agent.md` | API, HATEOAS, Blazor UI, BFF, Accessibility. |
| **Quality Verifier** | `quality-verifier-agent.md` | Build/test failures, architecture tests, CI validation. |
| **Librarian** | `librarian-agent.md` | Docs, research (Context7/Tavily), journal synthesis. |

## Usage Rules

1. **Open the file before invoking**: These files are short and include the precise constraints for that role.
2. **One agent at a time**: Do not run multiple agents in parallel if they mutate the same files.
3. **Layered Context**: Agents must link to `QUICK_REFERENCE.md` rather than duplicating its invariants.

## When to Use Agents vs Skills

- **Use agents** for multi-step or autonomous tasks (planning, complex implementation, verification).
- **Use skills** for inline patterns and implementation recipes while coding.

## When NOT to Use Agents

- Single-file edits with known location — use direct tools.
- Simple search/grep — use direct tools.
- Trivial questions answered in `QUICK_REFERENCE.md`.

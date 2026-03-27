---
name: Agents Documentation
description: Documentation file - not an executable agent
disabled: true
---
ABOUTME: Overview of available project agents, selection guide, and usage rules.
ABOUTME: Emphasizes required reads, agent selection criteria, and lean file expectations.

# Project Agents

These agent files are **small** and meant to be re-read. Do **not** rely on memory.

## Agent Selection Guide

| Need | Agent | Type |
|------|-------|------|
| Build/test verification | `codebase-verifier` | diagnostic |
| 401/403 auth debugging | `auth-route-debugger` | diagnostic |
| Auth endpoint testing | `auth-route-tester` | diagnostic |
| Build error resolution | `auto-error-resolver` | diagnostic |
| Blazor frontend errors | `frontend-error-fixer` | diagnostic |
| Architecture rule review | `code-architecture-reviewer` | review |
| Code quality review | `clean-code-architect` | review |
| Plan/PRD review | `plan-reviewer` | review |
| Refactor planning (read-only) | `refactor-planner` | review |
| Blazor component compliance | `blazor-component-architect` | domain |
| Refactoring execution | `code-refactor-master` | implementation |
| Documentation writing | `documentation-architect` | implementation |
| External research | `web-research-specialist` | research |

## When to Use Agents vs Skills

- **Use agents** for multi-step or autonomous tasks (review, refactor planning, verification, debugging).
- **Use skills** for inline patterns while coding (architecture rules, CQRS patterns, CSS conventions).

## When NOT to Use Agents

- Single-file edits with known location — use direct tools.
- Simple search/grep — use explore or direct tools.
- Answers already in project docs — read the doc instead.

## Required Reading

Open the agent file **before** invoking it. These files are short and include the precise constraints for that role.

## Agent Types

| Type | Purpose | Typical Tools |
|------|---------|---------------|
| **diagnostic** | Find and fix problems | Read, Bash, Grep |
| **review** | Read-only analysis and recommendations | Read, Glob, Grep |
| **implementation** | Create or modify files | Read, Write, Edit, Bash |
| **domain** | Domain-specific compliance checks | Read, Write, Edit, Glob |
| **research** | External information gathering | Read, Bash, WebFetch |

## Multi-Agent Attribution

This session may involve multiple agents. To determine which agent produced each response, call the `agent_attribution` tool.

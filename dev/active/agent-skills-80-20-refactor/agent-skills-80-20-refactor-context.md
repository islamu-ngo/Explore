# Agent & Skills 80/20 Refactor - Context

Last Updated: 2026-02-24

## SESSION PROGRESS (2026-02-24)

### ✅ COMPLETED
- Gathered current repo context (CLAUDE.md, docs, skills, agents, hooks)
- Collected Tavily research on AGENTS.md, skills, and OpenCode config
- Identified that Cloud.md is not an OpenCode config file; use opencode.json instead
- Refactored `CLAUDE.md` to be lean and to **explicitly require fetching relevant docs/skills/agents**
- Ran build + all test projects (warnings only; see below)
- Recorded baseline line counts for CLAUDE.md, agents, and skills (see baseline metrics)
- Compressed all `.claude/agents/*.md` files to lean format (see post-refactor metrics)
- Added minimal `opencode.json` (schema + model + instructions + permissions)
- Added ABOUTME headers to all agent files.
- Compressed and recreated oversized skill resources (Blazor UI conventions, CSS isolation, CQRS/MediatR, EF Core, error tracking).
- Restored missing `component-design.md` with lean guidance.

### 🟡 IN PROGRESS
- Updating dev docs and verification after resource compression.

### ⚠️ BLOCKERS
- None

## Key Files (Verified)

**Root instructions**
- `AGENTS.md` — entrypoint, points to `CLAUDE.md`
- `CLAUDE.md` — master rules and workflow guidance

**Docs**
- `docs/ARCHITECTURE.md` — Clean Architecture + CQRS + stack
- `docs/DOMAIN.md` — domain entities and invariants
- `docs/SECURITY.md` — BFF, Keycloak, Cerbos patterns
- `docs/CONFIGURATION.md` — runtime config and secret management
- `docs/GOVERNANCE.md` — conventions and rules
- `docs/OPERATIONS.md` — deployment modes and observability
- `docs/TROUBLESHOOTING.md` — build/test workflow and common issues
- `docs/PROJECT.md`, `docs/FEDERATION.md`, `docs/API.md`

**Skills**
- `.claude/skills/skill-rules.json` — triggers and architecture layers
- `.claude/skills/*/SKILL.md` — 9 skills (auth, cqrs, blazor, etc.)
- `.claude/skills/*/resources/*.md` — 44 resource files

**Agents**
- `.claude/agents/*.md` — 12 agents + README

**Hooks/Config**
- `.claude/hooks/*.cs`, `.claude/hooks/CONFIG.md`, `.claude/settings.json`

**OpenCode**
- `.opencode/` exists (plugin), but no `opencode.json` project config

## Research Sources (Tavily MCP)
- AGENTS.md best practices: https://docs.factory.ai/cli/configuration/agents-md
- AGENTS.md open format: https://agents.md/
- Skills best practices: https://cursor.com/docs/context/skills
- Claude skills best practices: https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices
- AGENTS.md analysis: https://github.blog/ai-and-ml/github-copilot/how-to-write-a-great-agents-md-lessons-from-over-2500-repositories/
- AGENTS.md impact study: https://arxiv.org/abs/2602.11988
- ACON context compression: https://arxiv.org/abs/2510.00615
- OpenCode config: https://opencode.ai/docs/config/
- OpenCode skills: https://opencode.ai/docs/skills/
- OpenCode agents: https://opencode.ai/docs/agents/
- OpenCode permissions: https://opencode.ai/docs/permissions/

## Decisions & Constraints
- Keep instructions minimal; avoid duplication across docs/skills/agents.
- Use progressive disclosure: SKILL.md stays lean; references hold details.
- Reduce every agent file and SKILL.md to ~20% size.
- Agent compression template: **Role + Required Reads + Must Do + Output** (≤30 lines)
- **CLAUDE.md must explicitly instruct agents to fetch relevant docs/skills/agents**.
- Mention docs/skills/resources are small to encourage re-reading.
- Remove only inferable content; preserve non‑inferable constraints.

## Build/Test Verification (2026-02-24)
- `dotnet build --configuration Release --verbosity quiet`: ✅ success
  - Warnings: NU1510 in Explore.Infrastructure/Explore.Blazor, ASPDEPR001 ApiDescription.Client
- All test projects: ✅ passed
  - Warnings: existing nullability/analyzer warnings in Explore.Blazor.Client (CS860x/CS8618/CA18xx/MUD0002)
- No database/schema changes required.

## Build/Test Verification (2026-02-24, Post-Refactor)
- `dotnet build --configuration Release --verbosity quiet`: ✅ success
  - Warnings: NU1510 (Explore.Infrastructure, Explore.Blazor), ASPDEPR001 (ApiDescription.Client)
- All test projects: ✅ passed

## Build/Test Verification (2026-02-24, Current Session)
- `dotnet build --configuration Release --verbosity quiet`: ✅ success
  - Warnings: NU1510 (Explore.Infrastructure, Explore.Blazor), ASPDEPR001 (ApiDescription.Client)
- All test projects: ✅ passed
  - Warnings: CS86xx/CS04xx/CS016x, CA17xx/CA18xx/CA1000/CA1873, MUD0002 (Explore.Blazor.Client)

## Baseline Metrics (Line Counts)

**CLAUDE.md**
- `CLAUDE.md`: 236 lines

**Skills**
- `.claude/skills/auth-patterns/SKILL.md`: 35 lines
- `.claude/skills/blazor-bff-patterns/SKILL.md`: 39 lines
- `.claude/skills/blazor-css-isolation/SKILL.md`: 37 lines
- `.claude/skills/blazor-ui-conventions/SKILL.md`: 43 lines
- `.claude/skills/clean-architecture-rules/SKILL.md`: 36 lines
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`: 38 lines
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`: 40 lines
- `.claude/skills/error-tracking/SKILL.md`: 38 lines
- `.claude/skills/prd/SKILL.md`: 38 lines

**Agents**
- `.claude/agents/README.md`: 359 lines
- `.claude/agents/auth-route-debugger.md`: 151 lines
- `.claude/agents/auth-route-tester.md`: 767 lines
- `.claude/agents/auto-error-resolver.md`: 341 lines
- `.claude/agents/blazor-component-architect.md`: 148 lines
- `.claude/agents/clean-code-architect.md`: 161 lines
- `.claude/agents/code-architecture-reviewer.md`: 154 lines
- `.claude/agents/code-refactor-master.md`: 158 lines
- `.claude/agents/codebase-verifier.md`: 48 lines
- `.claude/agents/documentation-architect.md`: 85 lines
- `.claude/agents/frontend-error-fixer.md`: 164 lines
- `.claude/agents/plan-reviewer.md`: 189 lines
- `.claude/agents/refactor-planner.md`: 280 lines
- `.claude/agents/web-research-specialist.md`: 246 lines

## Post-Refactor Metrics (Line Counts)

**Agents (lean)**
- `.claude/agents/README.md`: 34 lines
- `.claude/agents/auth-route-debugger.md`: 27 lines
- `.claude/agents/auth-route-tester.md`: 25 lines
- `.claude/agents/auto-error-resolver.md`: 28 lines
- `.claude/agents/blazor-component-architect.md`: 30 lines
- `.claude/agents/clean-code-architect.md`: 26 lines
- `.claude/agents/code-architecture-reviewer.md`: 30 lines
- `.claude/agents/code-refactor-master.md`: 28 lines
- `.claude/agents/codebase-verifier.md`: 24 lines
- `.claude/agents/documentation-architect.md`: 26 lines
- `.claude/agents/frontend-error-fixer.md`: 26 lines
- `.claude/agents/plan-reviewer.md`: 26 lines
- `.claude/agents/refactor-planner.md`: 26 lines
- `.claude/agents/web-research-specialist.md`: 26 lines

## Quick Resume
1. Read the plan file for phases and acceptance criteria.
2. Review current sizes of each SKILL.md and agent file.
3. Implement 80/20 compression per phase.
4. Update tasks checklist as each phase completes.

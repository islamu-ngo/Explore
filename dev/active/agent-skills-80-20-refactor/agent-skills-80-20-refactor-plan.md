# Plan: Agent & Skills 80/20 Refactor (AGENTS.md + Skills + Resources)

Last Updated: 2026-02-23

## Executive Summary
The current agent/skills documentation is large and context-heavy, which research shows can hurt agent performance and increase cost. This plan reduces the core instruction footprint to ~20% of current size while retaining the essential constraints and high-leverage guidance. It does so by aggressively removing what can be inferred, enforcing progressive disclosure, and restructuring skills/resources into minimal, task-triggered references.

## Current State Analysis (Verified Paths)
The following files and directories exist and currently drive agent/skills behavior:

- **Root agent entrypoints:**
  - `AGENTS.md` (points to `CLAUDE.md`)
  - `CLAUDE.md` (724 lines, comprehensive rules and workflow guidance)
- **Project documentation:**
  - `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/SECURITY.md`, `docs/CONFIGURATION.md`, `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `docs/TROUBLESHOOTING.md`, `docs/PROJECT.md`, `docs/FEDERATION.md`, `docs/API.md`
- **Skills system:**
  - `.claude/skills/skill-rules.json` (skill trigger rules)
  - `.claude/skills/*/SKILL.md` (9 skills)
  - `.claude/skills/*/resources/*.md` (44 resource files)
- **Agent definitions:**
  - `.claude/agents/*.md` (12 agents + README)
- **Hooks & config:**
  - `.claude/hooks/*.cs`, `.claude/hooks/CONFIG.md`, `.claude/settings.json`
- **OpenCode integration:**
  - `.opencode/` exists (node_modules present), but no project-level `opencode.json` found

### Key Problem Signals
Research indicates verbose context files reduce agent success rates and increase cost. Minimal, human-authored context improves effectiveness when focused on hard constraints only (e.g., AGENTS.md/CLAUDE.md) and defers detail to on-demand references. The current structure violates this by mixing exhaustive guidance with always-on instructions.

## Research Summary (Tavily MCP)
**AGENTS.md best practices** emphasize brevity, exact commands, and single-source-of-truth linkage rather than duplication:
- Factory AGENTS.md guidance recommends ≤150 lines and concrete commands, with updates in the same PR as build/process changes. [https://docs.factory.ai/cli/configuration/agents-md]
- GitHub’s AGENTS.md analysis similarly stresses clear role definition, concise commands, and minimal rules. [https://github.blog/ai-and-ml/github-copilot/how-to-write-a-great-agents-md-lessons-from-over-2500-repositories/]

**Open format AGENTS.md** and progressive disclosure guidance:
- Official AGENTS.md format: minimal markdown, hierarchical files for monorepos. [https://agents.md/]
- Progressive disclosure is the recommended pattern—keep root instructions tiny and link out for deeper docs. [https://www.aihero.dev/a-complete-guide-to-agents-md]

**Skill files** should keep SKILL.md lean and move detailed content to references:
- Cursor Skills format: SKILL.md with frontmatter + minimal instructions; references for detail. [https://cursor.com/docs/context/skills]
- Anthropic Skills best practices: short SKILL.md, strong descriptions, move detail to references. [https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices]

**Context compression research**:
- AGENTS.md study shows excessive context can reduce success and increase cost; minimal context is superior. [https://arxiv.org/abs/2602.11988]
- ACON and related compression work advocate structured, task-triggered compression strategies. [https://arxiv.org/abs/2510.00615]

**OpenCode configuration** (no Cloud.md exists):
- OpenCode uses `opencode.json` and instructions list, not Cloud.md. [https://opencode.ai/docs/config/]
- Skills are discovered from `.claude/skills/` and loaded on-demand. [https://opencode.ai/docs/skills/]
- Agent and permissions configuration lives in `opencode.json`. [https://opencode.ai/docs/agents/] [https://opencode.ai/docs/permissions/]

## Proposed Future State
1. **Ultra-minimal agent entrypoint**:
   - Keep a short `AGENTS.md` (or short `CLAUDE.md` if we choose not to add AGENTS.md) that only lists non-negotiable rules and links to docs.
2. **Progressive disclosure for skills/resources**:
   - Each SKILL.md reduced to a 3-part structure: WHEN / RULES / EXAMPLE. All detail moves to at most 1–2 reference files per skill.
3. **Skill resources consolidated**:
   - Reduce 44 resource files to a smaller curated set (~20%). Merge overlapping topics, remove redundancy.
4. **Minimal OpenCode project config**:
   - Add `opencode.json` with small model config, instructions list, and permissions. No duplication of skills/agents.
5. **Agent files slimmed to role + constraints**:
   - Each agent reduced to core role, tool constraints, and 3–7 bullet duties.

## Implementation Phases (Broken Into Clean Architecture Layers)

### Phase 0: Cross-Cutting Baseline (Foundation)
**Goal:** Establish minimal global instructions and inventory.

**Task 0.1: Inventory and baseline metrics**
- **Acceptance Criteria:**
  - [ ] Line counts recorded for `CLAUDE.md`, each `.claude/skills/*/SKILL.md`, and `.claude/agents/*.md`
  - [ ] Resource file list captured with sizes
- **Effort:** S
- **Dependencies:** None
- **Related Skills:** `clean-architecture-rules`

**Task 0.2: Create minimal OpenCode config**
- **Acceptance Criteria:**
  - [ ] `opencode.json` exists with `$schema`, model, instructions list, and permissions
  - [ ] Instructions include only `CLAUDE.md` + `docs/QUICK_REFERENCE.md`
- **Effort:** S
- **Dependencies:** Task 0.1
- **Related Skills:** `clean-architecture-rules`

### Phase 1: Domain Layer Skills (Domain)
**Focus Skills:** `clean-architecture-rules`, domain-specific rules from `docs/DOMAIN.md`.

**Task 1.1: Compress `clean-architecture-rules` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] WHEN/RULES/EXAMPLE structure applied
  - [ ] Only one reference file retained for detailed examples
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `clean-architecture-rules`

### Phase 2: Application Layer Skills (Application)
**Focus Skills:** `cqrs-mediatr-guidelines`, `prd`.

**Task 2.1: Compress `cqrs-mediatr-guidelines` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Commands/queries rules retained; detailed patterns moved to 1 reference file
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `cqrs-mediatr-guidelines`

**Task 2.2: Compress `prd` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Redundant examples moved to reference, or removed if inferable
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `prd`

### Phase 3: Infrastructure Layer Skills (Infrastructure)
**Focus Skills:** `dotnet-efcore-guidelines`, `error-tracking`.

**Task 3.1: Compress `dotnet-efcore-guidelines` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Keep only hard constraints (named query filters, repository returns entities, etc.)
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `dotnet-efcore-guidelines`

**Task 3.2: Compress `error-tracking` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Consolidate observability resources into a single reference file
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `error-tracking`

### Phase 4: Presentation Layer Skills (API/Blazor)
**Focus Skills:** `blazor-ui-conventions`, `blazor-bff-patterns`, `blazor-css-isolation`, `auth-patterns`.

**Task 4.1: Compress `blazor-ui-conventions` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Keep only MudBlazor/BEM must-haves; move other patterns to 1 reference
- **Effort:** L
- **Dependencies:** Task 0.1
- **Related Skills:** `blazor-ui-conventions`, `blazor-css-isolation`

**Task 4.2: Compress `blazor-bff-patterns` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Preserve token forwarding + YARP rules; consolidate resources
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `blazor-bff-patterns`, `auth-patterns`

**Task 4.3: Compress `blazor-css-isolation` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Only BEM + ::deep rules retained; one reference file
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `blazor-css-isolation`

**Task 4.4: Compress `auth-patterns` SKILL.md**
- **Acceptance Criteria:**
  - [ ] SKILL.md reduced to ≤20% of current lines
  - [ ] Keep claim extraction and BFF boundaries; move details to reference
- **Effort:** M
- **Dependencies:** Task 0.1
- **Related Skills:** `auth-patterns`

### Phase 5: Agents & Commands (Cross-Cutting)
**Goal:** Reduce each `.claude/agents/*.md` file to ~20% size with minimal role + constraints.

**Task 5.1: Define agent compression template**
- **Acceptance Criteria:**
  - [ ] Single template created with: role, tools/permissions, 3–7 bullet responsibilities
- **Effort:** S
- **Dependencies:** Task 0.1

**Task 5.2: Apply template to all agents**
- **Acceptance Criteria:**
  - [ ] Each agent file reduced to ≤20% size
  - [ ] All cross-references moved to skills/docs instead of inline text
- **Effort:** L
- **Dependencies:** Task 5.1

### Phase 6: Validation & Metrics
**Task 6.1: Build & test verification**
- **Acceptance Criteria:**
  - [ ] `dotnet build --configuration Release --verbosity quiet` passes (warnings noted)
  - [ ] All test projects pass (warnings noted)
- **Effort:** S
- **Dependencies:** All phases

**Task 6.2: Effectiveness review**
- **Acceptance Criteria:**
  - [ ] New sizes recorded (target ≤20% of baseline)
  - [ ] Sample prompts show correct skill triggering (manual spot-check)
- **Effort:** M
- **Dependencies:** Phase 1–5

## Detailed Tasks (Actionable + Acceptance Criteria)
Tasks are already embedded per phase above with explicit acceptance criteria and dependencies. No database or authorization policy changes are required for this documentation-only refactor. No EF Core migrations are needed.

## Risk Assessment & Mitigation
| Risk | Impact | Mitigation |
|------|--------|-----------|
| Over-pruning loses critical constraints | Medium | Preserve “must-not-break” rules in SKILL.md and link to references for detail. Validate with 3–5 sample prompts. |
| Skill triggers become too generic | Medium | Keep high-signal descriptions and keywords per skill; validate against `skill-rules.json`. |
| Agent drift due to reduced instructions | Medium | Add a minimal “golden rules” section and link to docs. |
| OpenCode config conflicts with existing hooks | Low | Keep `opencode.json` minimal and avoid duplicate agent/skill definitions. |

## Success Metrics
- **Size reduction:** ≥80% line reduction for each SKILL.md and agent file.
- **Signal quality:** 0 regressions on 5 representative prompts.
- **Performance:** Reduced context length per session (measured by instruction size).

## Required Resources & Dependencies
- Tavily MCP research sources (AGENTS.md and skills best practices) [https://docs.factory.ai/cli/configuration/agents-md] [https://agents.md/] [https://cursor.com/docs/context/skills] [https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices]
- OpenCode configuration docs [https://opencode.ai/docs/config/] [https://opencode.ai/docs/skills/] [https://opencode.ai/docs/agents/] [https://opencode.ai/docs/permissions/]
- Existing local docs and skills listed in Current State (verified paths)

## Effort Estimates
- Phase 0: S
- Phase 1: M
- Phase 2: M
- Phase 3: M
- Phase 4: L
- Phase 5: L
- Phase 6: M

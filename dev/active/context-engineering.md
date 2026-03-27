ABOUTME: Context engineering audit and implementation plan for the .claude/ configuration layer.
ABOUTME: Senior-level review of skills, agents, hooks, and CLAUDE.md covering gaps, antipatterns, and prioritized improvements.

# Context Engineering Audit & Implementation Plan

> **Prepared by:** Context engineering audit (senior tech lead perspective)
> **Date:** 2026-03-27
> **Scope:** Everything under `.claude/` — skills, agents, hooks, context state — excluding commands
> **Research sources:** Anthropic official docs, Claude Code source, Everything-Claude-Code, 12-Factor Agents, JetBrains research, LangChain blog, Simon Willison, enterprise AI governance literature

---

## Part 1 — The Discipline: What Context Engineering Is

Context engineering emerged in 2025 as a recognized discipline distinct from prompt engineering. Where prompt engineering focuses on the text of a single request, **context engineering addresses the entire information ecosystem available to a model during inference** — the art of filling the context window with exactly the right information at exactly the right moment.

The foundational insight: **intelligence is not the bottleneck; context is.** Frontier models are already capable of performing most coding tasks. What determines success is whether they have the right information at the right moment, formatted in the right way.

### The Attention Budget Problem

The transformer architecture creates an n-squared relationship between token count and computational cost. Every token in the context window competes for the model's "attention budget." As context fills, performance degrades through **context rot** — four failure modes:

| Mode | Description |
|------|-------------|
| **Context Poisoning** | Incorrect or outdated information corrupts reasoning |
| **Context Distraction** | Irrelevant content dilutes attention from what matters |
| **Context Confusion** | Similar but distinct pieces cause uncertainty |
| **Context Clash** | Contradictory instructions force unpredictable arbitration |

The practical implication: a 500-line CLAUDE.md is not five times better than a 100-line one. Beyond a threshold, adding instructions actively degrades compliance with existing ones.

### The Four Foundational Context Actions

LangChain's research crystallized context engineering into four action categories:

| Action | What It Does | Claude Code Mechanism |
|--------|--------------|----------------------|
| **Write** | Persist context outside the window | NOTES.md, memory files, to-do lists |
| **Select** | Pull relevant context into the window | Skills, file reads, Glob/Grep/Read JIT |
| **Compress** | Reduce tokens while retaining signal | `/compact`, observation masking, sub-agent summaries |
| **Isolate** | Split context across separate agents | Sub-agents, worktrees, sandboxed execution |

### The Decision Framework

```
"Should this always be in context?"         → CLAUDE.md
"Is this domain knowledge needed sometimes?" → Skill
"Is this a self-contained task?"             → Sub-agent
"Does this need to happen unconditionally?"  → Hook
```

---

## Part 2 — Audit: Current Configuration State

### 2.1 CLAUDE.md (Project Root)

**Overall Assessment: 8/10 — Strong governance, approaching token-budget risk**

CLAUDE.md is the single most important context engineering artifact. It loads every session and costs tokens unconditionally.

**Strengths:**
- Hard technical rules are non-inferable (auditing fields, manual validators, `int` not `long`, file-scoped namespaces)
- "Absolute Fetch Rule" enforces JIT skill loading — architecture-aware context management
- Build-first workflow is concrete and executable
- Collaboration section is unusually strong (colleagues, not hierarchical)
- TDD section is concise and behaviorally specific
- `@dev/active/README.md` import pattern correctly uses progressive disclosure

**Risks:**
- The full file is estimated at ~250-300 lines — approaching the industry-recommended 300-line threshold
- Documentation Index section (30+ lines) enumerates every doc file — this is borderline context distraction. Consider replacing with a summary rule + link
- Start-of-Work Verification section with full bash commands (~20 lines) duplicates TESTING.md content that could be imported via `@`
- Blazor UI Development Workflow section (~25 lines) includes PowerShell commands — should be extracted to a skill or referenced doc
- Some coding rules are already enforced by analyzers/linters — documenting those wastes tokens

**The test for every line:** *"Would removing this cause Claude to make mistakes?"* Apply ruthlessly on next review.

---

### 2.2 Agents — Full Audit

**Total:** 13 agents + 1 README | **Combined lines:** ~520

All agents correctly follow the ABOUTME convention. Structure is lean and re-readable by design — a deliberate architecture decision aligned with best practice (agents are meant to be re-read before each use, not referenced from memory).

#### Agent Quality Matrix

| Agent | Lines | Type | Enforcement | Priority | Quality | Key Issue |
|-------|-------|------|-------------|----------|---------|-----------|
| `auth-route-debugger` | 34 | — | — | — | 7/10 | Missing frontmatter metadata |
| `auth-route-tester` | 33 | — | — | — | 7/10 | Missing frontmatter metadata |
| `auto-error-resolver` | 36 | — | — | — | 8/10 | Missing frontmatter metadata |
| `blazor-component-architect` | 61 | domain | suggest | high | **9/10** | Best in set — v9 migration, aesthetic rules |
| `clean-code-architect` | 33 | — | — | — | 7/10 | Missing frontmatter metadata |
| `code-architecture-reviewer` | 38 | domain | enforce | high | **9/10** | Strong — BLOCK-level architectural checks |
| `code-refactor-master` | 35 | — | — | — | 7/10 | Missing frontmatter metadata |
| `codebase-verifier` | 28 | — | — | — | 5/10 | Too minimal — just a build runner |
| `documentation-architect` | 31 | — | — | — | 6/10 | Missing frontmatter; no output format defined |
| `frontend-error-fixer` | 49 | — | — | — | **9/10** | Best in set — specific v9 error patterns |
| `plan-reviewer` | 32 | — | — | — | 7/10 | Missing frontmatter metadata |
| `refactor-planner` | 31 | — | — | — | 7/10 | Missing frontmatter metadata |
| `web-research-specialist` | 30 | — | — | — | 6/10 | Missing frontmatter; minimal constraints |

**Cross-cutting issues:**

1. **Metadata gap**: 10 of 13 agents lack `type`, `enforcement`, and `priority` frontmatter. These fields inform both tool-level routing and the skill-rules activation system. Without them, the system cannot programmatically understand agent capabilities.

2. **No example outputs**: Every agent specifies an output format in prose but provides zero concrete examples. "Root cause (file + line), fix steps, verification command" is far weaker than showing one actual example. Industry research consistently shows that concrete input/output pairs in instructions outperform abstract descriptions.

3. **Missing tool restrictions**: No agent specifies a `tools` allowlist. Read-only agents (code-architecture-reviewer, plan-reviewer, codebase-verifier, documentation-architect) should be restricted to `Read, Grep, Glob` to prevent accidental file mutation and to signal their scope.

4. **`codebase-verifier` is too thin**: 28 lines, no meaningful constraints beyond "run build and tests." Should either be expanded with clear verification criteria and failure reporting format, or merged into a `PostToolUse` hook.

5. **Dangling reference**: Several agents reference `dev/active/mudblazor-migration-v9/` — this directory may no longer be active. All referenced paths should be verified before each use.

---

### 2.3 Skills — Full Audit

**Total:** 11 skill domains | **Combined SKILL.md lines:** ~631 | **Resource files:** ~48 files, ~1,400 lines

All skill SKILL.md files correctly follow the ABOUTME convention.

#### Skill Quality Matrix

| Skill | SKILL.md | Resources | Type | Enforcement | Priority | Quality | Key Issue |
|-------|----------|-----------|------|-------------|----------|---------|-----------|
| `accessibility` | 119 | 0 | inferred | suggest | high | **9/10** | No activation triggers in skill-rules.json |
| `agentic-research` | 49 | 3 sparse | domain | suggest | high | 7/10 | No activation triggers; resource files thin |
| `auth-patterns` | 45 | 2 | domain | suggest | critical | **9/10** | Token refresh / logout not covered |
| `blazor-bff-patterns` | 44 | 5 | domain | suggest | high | 8/10 | Resource files too short (14-20 lines each) |
| `blazor-css-isolation` | 96 | 4 | ui | suggest | high | 8/10 | Uneven resource depth |
| `blazor-ui-conventions` | 54 | 8 | ui | suggest | high | **9/10** | Some resources oversized (144-152 lines) |
| `clean-architecture-rules` | 41 | 4 | guardrail | **block** | critical | **9/10** | `violation-examples.md` = 16 lines (stub) |
| `cqrs-mediatr-guidelines` | 45 | 6 | domain | suggest | high | 8/10 | `complete-examples.md` = 7 lines (stub) |
| `dotnet-efcore-guidelines` | 47 | 6 | domain | suggest | high | 8/10 | Missing performance tuning, value converters |
| `error-tracking` | 50 | 9 | guardrail | suggest | high | 8/10 | Resource depth varies wildly (12-56 lines) |
| `prd` | 41 | 0 | workflow | none | — | 6/10 | No activation triggers; no example PRD |

#### Critical Finding: 3 Skills Have No Activation Triggers

`skill-rules.json` is the central dispatch table that auto-suggests skills based on keywords, intent patterns, and file patterns. **Three high-value skills are completely absent from it:**

| Missing Skill | What Would Trigger It | Impact |
|--------------|----------------------|--------|
| `accessibility` | "a11y", "wcag", "aria", "screen reader", "focus", "tabindex" in prompt or `.razor`/`.razor.css` files touched | WCAG violations go uncorrected because skill is never surfaced |
| `agentic-research` | "official docs", "migration", "breaking change", "library version" | Research without guardrails — no source hierarchy enforcement |
| `prd` | "create prd", "requirements", "spec out", "feature request" | PRD workflow never auto-suggested for planning tasks |

These skills exist but are effectively invisible to the activation system.

#### Resource File Depth Distribution Problem

The 48 resource files across all skills have a severe depth imbalance:

| Depth tier | Files | Examples |
|-----------|-------|---------|
| **Oversized** (>130 lines) | 4 | `theming.md` (152), `mudblazor-usage.md` (144), `common-patterns.md` (142), `mudblazor-styling.md` (143) |
| **Well-sized** (40-130 lines) | 22 | Most technical references |
| **Stub** (<30 lines) | 22 | `complete-examples.md` (7!), `token-forwarding.md` (14), `service-layer-patterns.md` (14), `bem-with-isolation.md` (13), `migrations.md` (13), `query-patterns.md` (13) |

**The stub problem is critical.** A 7-line `complete-examples.md` is worse than no file — it signals incompleteness and consumes a file read without delivering value. Claude loads it and finds near-nothing. Either expand these or remove them.

**The oversized problem is real too.** Files over 130 lines may exceed Claude's effective reading threshold for a resource file. The progressive disclosure model works best when reference files are focused and bounded. Files over ~80 lines should be split by concern.

#### Missing Skills for Current Codebase

Based on the documentation audit and new features introduced on the develop branch, the following skills have no coverage anywhere in `.claude/skills/`:

| Missing Skill | New Feature It Covers | Why Needed |
|--------------|----------------------|------------|
| `outbox-pattern` | `OutboxMessage`, `OutboxProcessor`, `IOutboxMessageDispatcher` | Patterns for adding messages, implementing dispatchers, retry semantics |
| `design-system` | CSS tokens, `@layer`, wrapper components, `DialogOptionsFactory` | Token tiers, layer ordering, `mudblazor-overrides.css` whitelist rules |
| `footer-management` | `FooterAdminService`, footer templates, governance locks | Admin CRUD patterns, template dispatch, tenant vs. instance governance |

Without these skills, Claude will not have the project-specific context needed to correctly extend these systems.

---

### 2.4 skill-rules.json — Activation System Audit

**Status: Stale and incomplete**

| Property | Value | Assessment |
|----------|-------|------------|
| Version | 2.0 | Structured, versioned — good |
| Last updated | 2026-02-24 | **33 days stale** as of today |
| Skills with triggers | 8 of 11 | 3 skills have zero triggers |
| Schema enforcement | Present | JSON Schema reference — good |
| Architecture layers | Defined | Domain/Application/Infrastructure/Presentation — good |

**Missing trigger entries** (concrete additions needed):

```json
// accessibility — currently absent
"accessibility": {
  "type": "domain",
  "enforcement": "suggest",
  "priority": "high",
  "description": "WCAG 2.2 Level AA accessibility rules for Blazor components.",
  "promptTriggers": {
    "keywords": ["a11y", "accessibility", "wcag", "aria", "screen reader", "focus", "tabindex", "keyboard nav"],
    "intentPatterns": ["(make|ensure|add|improve).*?(accessible|a11y)", "wcag.*?(aa|aaa)", "aria.*?label"]
  },
  "fileTriggers": {
    "pathPatterns": ["**/*.razor", "**/*.razor.css", "**/accessibility.js"],
    "contentPatterns": ["IAccessibilityAnnouncerService", "aria-", "tabindex", "@inject.*IAccessibility"]
  }
}

// agentic-research — currently absent
"agentic-research": {
  "type": "workflow",
  "enforcement": "suggest",
  "priority": "high",
  "description": "Local-first research methodology — repo before external sources.",
  "promptTriggers": {
    "keywords": ["breaking change", "migration guide", "official docs", "library version", "upgrade", "changelog"],
    "intentPatterns": ["how.*?(library|package|nuget).*?works", "what.*?changed.*?(version|release)"]
  }
}

// prd — currently absent
"prd": {
  "type": "workflow",
  "enforcement": "suggest",
  "priority": "high",
  "description": "Product Requirements Document generation workflow.",
  "promptTriggers": {
    "keywords": ["create prd", "write prd", "spec out", "requirements doc", "feature request", "plan this feature"],
    "intentPatterns": ["(create|write|generate).*?(prd|requirements|spec)", "plan.*?feature"]
  }
}
```

---

### 2.5 Hooks — Audit

**Architecture:** C# scripts (`SkillTrigger.cs`, `ContextTracker.cs`, `SecurityCheck.cs`, `FormatCode.cs`, `BuildCheck.cs`) invoked from `settings.json` hook configuration.

**Strengths:**
- C# implementation stays in the project's own ecosystem — eliminates Node.js/Bash dependency
- 4 lifecycle points covered: `UserPromptSubmit` (SkillTrigger), `PreToolUse` (SecurityCheck), `PostToolUse` (ContextTracker), `Stop` (FormatCode + BuildCheck)
- Hook documentation exists: `README.md` (158 lines) + `CONFIG.md` (109 lines)

**Gaps:**

1. **No `SessionStart` hook** — The most valuable hook for context engineering. Fires when a session begins or resumes after compaction. Should re-inject project state, current branch, active sprint focus into context. Without it, critical context can be lost after auto-compaction.

2. **No `PreCompact`/`PostCompact` hooks** — These allow preserving critical information across the compaction boundary. The project currently has no compaction strategy documentation.

3. **No HTTP audit hook** — `PostToolUse` with HTTP type could centralize an audit log of all tool invocations. Currently there is no audit trail beyond what logs the running process emits.

4. **Missing hook timeout documentation** — `README.md` does not document timeout behavior. In Claude Code, hooks have a hard timeout (default 60s, configurable via `timeout`). Long-running C# compilation in hooks risks silent timeout failures.

5. **SecurityCheck.cs scope unknown** — File is present but content not audited. Critical for understanding what is being blocked at `PreToolUse`. If it is under-specified, dangerous commands may not be caught.

6. **`settings.local.json` permission sprawl:**
   - `Bash(dotnet:*)` permits ALL dotnet commands — `dotnet ef database drop`, `dotnet publish`, `dotnet tool install` — all allowed
   - `Bash(Out-File -FilePath C:/ISLAMU/GitHub/Event/test-output.txt...)` hardcodes a machine-specific path
   - `mcp__acp__Write` and `mcp__acp__Edit` suggest an unofficial ACP client — unclear governance implication
   - No timeout configuration on any hook

---

### 2.6 context-state.json — Audit

**Status: Minor issues**

```json
{
  "Project": "ISLAMU Event",
  "Stack": ".NET 10 + Blazor + Aspire",
  "RootPath": "C:\\ISLAMU\\GitHub\\Event",
  "ActiveLayers": { ... },
  "RecentFocus": "Application (UnlockSettingCommandHandler.cs)",
  "LastUpdate": "2026-03-27 00:18:53"   ← Not ISO 8601
}
```

Issues:
- `LastUpdate` is not ISO 8601 (`2026-03-27T00:18:53Z` is the correct format for machine-parseability)
- `RecentFocus` is a static string manually maintained — it will drift and become misleading
- `ActiveLayers` is coarse-grained (Frontend/Backend/Domain/Infra) — no tracking of current feature branch or active sprint

---

## Part 3 — Gap Analysis by Concern

### Context Engineering Layer Coverage

| Layer | Current Status | Gap |
|-------|---------------|-----|
| **Always-present context** (CLAUDE.md) | Strong, near token-budget ceiling | Needs pruning pass; Blazor UI workflow section should move to skill |
| **On-demand knowledge** (Skills) | 11 skills, good depth in 6, stub problems in 5 | 3 missing activation triggers; 3 missing skills for new features; 22 stub resource files |
| **Context isolation** (Sub-agents) | 13 agents, good quality in 5 | 10 missing frontmatter; no tool restrictions; no example outputs |
| **Deterministic enforcement** (Hooks) | 4 lifecycle points covered | No SessionStart; no compaction hooks; no audit trail; settings.local.json sprawl |
| **Context snapshot** (context-state.json) | Present and mostly current | Non-ISO timestamp; RecentFocus drifts |
| **Activation dispatch** (skill-rules.json) | 8 of 11 skills wired | 3 missing; 33 days stale; no sync mechanism |

### Antipattern Inventory

Based on industry research and the audit findings above, the following antipatterns are present:

| Antipattern | Where | Severity |
|-------------|-------|----------|
| **Stub files** — resource files <10 lines with placeholder content | `cqrs-mediatr-guidelines/complete-examples.md` (7 lines), `blazor-bff-patterns/token-forwarding.md` (14 lines) | HIGH |
| **Missing activation triggers** — skills exist but are never surfaced | `accessibility`, `agentic-research`, `prd` in skill-rules.json | HIGH |
| **No tool restrictions** — agents can use all tools regardless of role | All 13 agents in `.claude/agents/` | MEDIUM |
| **Inconsistent frontmatter** — metadata fields absent from 77% of agents | 10 of 13 agents | MEDIUM |
| **No example outputs** — abstract output descriptions without concrete samples | All 13 agents | MEDIUM |
| **Permission sprawl** — `Bash(dotnet:*)` allows dangerous dotnet commands locally | `settings.local.json` | MEDIUM |
| **No compaction strategy** — no `SessionStart`/`PreCompact` hooks to preserve context | `settings.json` | MEDIUM |
| **Oversized resource files** — 4 files over 130 lines dilute progressive disclosure | `theming.md`, `mudblazor-usage.md`, `common-patterns.md`, `mudblazor-styling.md` | LOW |
| **Dangling references** — paths to possibly-archived active directories | Multiple agents referencing `dev/active/mudblazor-migration-v9/` | LOW |
| **Non-ISO timestamp** — `context-state.json` uses non-standard date format | `context-state.json` | LOW |

---

## Part 4 — Implementation Plan

Organized by priority tier. Each item has: file path, content scope, rationale, and dependencies.

---

### Tier 1 — Critical (Activation and enforcement gaps)

---

#### T1-1: Update `skill-rules.json` — Add 3 missing activation triggers

**File:** `.claude/skills/skill-rules.json`
**Change:** Add `accessibility`, `agentic-research`, and `prd` entries with full trigger specifications
**Why critical:** These skills exist and are high-value but are **never surfaced** to the model because the activation system doesn't know they exist
**Scope:** Add 3 trigger blocks (~40 lines), update `lastUpdated` to 2026-03-27
**Dependencies:** None

---

#### T1-2: Add `SessionStart` hook for context re-injection

**File:** `.claude/hooks/SessionStartInjector.cs` (new) + wire in `settings.json`
**Purpose:** Re-inject critical project context when a session starts or resumes after compaction
**Why critical:** Auto-compaction at ~95% context usage discards recent context. Without a `SessionStart` hook, the model loses track of current sprint focus, branch state, and active tasks after compaction
**What to inject:**
```
- Current branch name (git rev-parse --abbrev-ref HEAD)
- Active dev folder items (ls dev/active/)
- Last 5 commits (git log --oneline -5)
- context-state.json RecentFocus value
```
**Scope:** ~60-line C# script + settings.json hook entry
**Dependencies:** None

**Hook configuration to add to settings.json:**
```json
"SessionStart": [
  {
    "matcher": "*",
    "hooks": [
      {
        "type": "command",
        "command": "dotnet script .claude/hooks/SessionStartInjector.cs",
        "timeout": 10000
      }
    ]
  }
]
```

---

#### T1-3: Expand 5 stub resource files — Eliminate 7-line placeholder content

**Files (highest priority stubs):**
1. `.claude/skills/cqrs-mediatr-guidelines/resources/complete-examples.md` — 7 lines → ~80 lines
   - Add a complete command example (Create), complete query example (GetList), complete delete example
2. `.claude/skills/blazor-bff-patterns/resources/token-forwarding.md` — 14 lines → ~50 lines
   - Add full YARP transform configuration, header forwarding code, BFF client pattern
3. `.claude/skills/blazor-bff-patterns/resources/service-layer-patterns.md` — 14 lines → ~50 lines
   - Add typed HttpClient pattern, error mapping, response deserialization pattern
4. `.claude/skills/clean-architecture-rules/resources/violation-examples.md` — 16 lines → ~60 lines
   - Add 3 concrete violation examples with before/after code snippets
5. `.claude/skills/dotnet-efcore-guidelines/resources/migrations.md` — 13 lines → ~45 lines
   - Add migration creation, rollback, environment-specific execution, naming conventions

**Why critical:** Stub files are worse than no files — they consume a file read operation and return near nothing. Progressive disclosure depends on loaded resources delivering value.

---

#### T1-4: Add `tools` restriction to read-only agents

**Files:** 4 agent files need explicit tool allowlists
**Changes:**

```yaml
# code-architecture-reviewer.md, plan-reviewer.md, documentation-architect.md
---
tools: Read, Grep, Glob
---

# codebase-verifier.md
---
tools: Read, Grep, Glob, Bash
---

# auth-route-tester.md (needs Bash to run tests)
---
tools: Read, Grep, Glob, Bash
---
```

**Why critical:** Read-only agents with full tool access can accidentally mutate files. Tool restrictions enforce the agent's stated role deterministically rather than relying on instruction compliance.

---

### Tier 2 — High (Completeness and consistency)

---

#### T2-1: Standardize agent frontmatter — Add metadata to all 10 incomplete agents

**Files:** 10 agent .md files
**Change:** Add `type`, `enforcement`, `priority`, and `tools` fields to frontmatter

| Agent | Recommended Type | Enforcement | Priority | Tools |
|-------|-----------------|-------------|----------|-------|
| `auth-route-debugger` | domain | suggest | high | Read, Grep, Glob, Bash |
| `auth-route-tester` | domain | suggest | high | Read, Grep, Glob, Bash |
| `auto-error-resolver` | domain | suggest | high | Read, Grep, Glob, Bash, Edit |
| `clean-code-architect` | domain | suggest | high | Read, Grep, Glob, Write, Edit, Bash |
| `code-refactor-master` | domain | suggest | high | Read, Grep, Glob, Write, Edit, Bash |
| `codebase-verifier` | workflow | suggest | high | Read, Grep, Glob, Bash |
| `documentation-architect` | workflow | suggest | medium | Read, Grep, Glob, Write, Edit |
| `plan-reviewer` | workflow | suggest | high | Read, Grep, Glob |
| `refactor-planner` | workflow | suggest | high | Read, Grep, Glob |
| `web-research-specialist` | workflow | suggest | medium | Read, WebFetch, WebSearch |

---

#### T2-2: Add concrete output examples to 4 key agents

**Files:** `auth-route-debugger.md`, `auto-error-resolver.md`, `blazor-component-architect.md`, `code-architecture-reviewer.md`
**Change:** Add `## Example Output` section with one realistic, minimal example per agent
**Why:** Industry research consistently shows concrete input/output pairs in instructions outperform abstract format descriptions. These 4 agents are the most frequently invoked.

**Pattern for each:**
```markdown
## Example Output

**Input context**: [one-sentence scenario]

**Output**:
```
[Minimal but realistic example matching the stated format]
```
```

---

#### T2-3: New skill — `outbox-pattern`

**File:** `.claude/skills/outbox-pattern/SKILL.md` (new)
**Resources:** `.claude/skills/outbox-pattern/resources/`
- `entity-lifecycle.md` — `OutboxMessageStatus` enum, entity fields, retry formula
- `writing-messages.md` — How to add a message inside a UoW transaction
- `implementing-dispatcher.md` — `IOutboxMessageDispatcher` contract, routing by `EventType`
- `configuration.md` — `OutboxProcessorSettings` all 7 options

**SKILL.md scope (~60 lines):**
- What the outbox pattern is and why
- Key components (entity → repository → processor → dispatcher)
- At-least-once semantics, idempotency requirement
- When to trigger activation (mention `IOutboxRepository`, `OutboxMessage`, `OutboxProcessor`, `EventType`)

**skill-rules.json trigger entry:**
```json
"outbox-pattern": {
  "type": "domain",
  "enforcement": "suggest",
  "priority": "high",
  "promptTriggers": {
    "keywords": ["outbox", "side effect", "reliable delivery", "at-least-once", "background delivery"],
    "intentPatterns": ["(add|create|dispatch).*?outbox.*?message", "reliable.*?(email|webhook|notification)"]
  },
  "fileTriggers": {
    "pathPatterns": ["**/OutboxMessage.cs", "**/IOutboxMessageDispatcher.cs", "**/OutboxProcessor.cs", "**/IOutboxRepository.cs"],
    "contentPatterns": ["OutboxMessage", "IOutboxMessageDispatcher", "OutboxMessageStatus"]
  }
}
```

---

#### T2-4: New skill — `design-system`

**File:** `.claude/skills/design-system/SKILL.md` (new)
**Resources:**
- `token-tiers.md` — Primitive → Semantic → Component tier definitions, naming conventions, how to add
- `layer-ordering.md` — `@layer` cascade, unlayered beats layers, dark mode strategy
- `wrapper-components.md` — AppButton/AppCard/AppTextField/AppIconButton/AppDialogShell parameter tables, DialogOptionsFactory presets
- `mudblazor-overrides.md` — Override whitelist, JUSTIFICATION comment requirement, approved exceptions

**SKILL.md scope (~70 lines):**
- CSS architecture overview (3-tier tokens + layer cascade)
- When to use tokens vs. inline values
- Wrapper component defaults and when to deviate
- Override policy (whitelist model)
- RTL readiness (logical properties only)

**skill-rules.json trigger entry:**
```json
"design-system": {
  "type": "ui",
  "enforcement": "suggest",
  "priority": "high",
  "promptTriggers": {
    "keywords": ["design token", "css variable", "isl-", "mudblazor override", "wrapper component", "AppButton", "AppCard", "DialogOptionsFactory"],
    "intentPatterns": ["(add|change|update).*?css.*?(token|variable|layer)", "style.*?(component|button|card)"]
  },
  "fileTriggers": {
    "pathPatterns": ["**/tokens.css", "**/layers.css", "**/mudblazor-overrides.css", "**/AppButton.razor", "**/AppCard.razor", "**/DialogOptionsFactory.cs"],
    "contentPatterns": ["--isl-", "@layer", "AppButton", "AppCard", "DialogOptionsFactory"]
  }
}
```

---

#### T2-5: New skill — `footer-management`

**File:** `.claude/skills/footer-management/SKILL.md` (new)
**Resources:**
- `service-contract.md` — Full `IFooterAdminService` contract with all 11 methods
- `template-dispatch.md` — Template names, how Footer.razor dispatches, how to add a new template
- `governance.md` — Lock toggles, tenant vs. instance permissions, single-tenant behavior

**skill-rules.json trigger entry:**
```json
"footer-management": {
  "type": "domain",
  "enforcement": "suggest",
  "priority": "medium",
  "promptTriggers": {
    "keywords": ["footer", "link group", "footer template", "footer settings", "social link"],
    "intentPatterns": ["(add|edit|manage).*?footer", "footer.*?(link|template|setting)"]
  },
  "fileTriggers": {
    "pathPatterns": ["**/Footer.razor", "**/FooterAdminService.cs", "**/IFooterAdminService.cs", "**/FooterSettings.razor", "**/FooterTemplates/**"],
    "contentPatterns": ["IFooterAdminService", "FooterAdminService", "_template", "FooterLinkGroup"]
  }
}
```

---

#### T2-6: Expand `codebase-verifier` agent

**File:** `.claude/agents/codebase-verifier.md`
**Current:** 28 lines — just runs build and tests
**Target:** ~60 lines
**Additions:**
- Structured verification report format (pass/fail table per test project)
- Failure escalation instructions (generate TRX, read error output, report root cause)
- Which test projects exist and what they verify
- What "pristine" output means (no unexpected warnings/stack traces)
- When to stop vs. report and continue
- Explicit tool allowlist: `Read, Grep, Glob, Bash`

---

### Tier 3 — Medium (Quality and maintainability)

---

#### T3-1: Split 4 oversized resource files

**Files** (each over 130 lines):

| File | Current Lines | Split Strategy |
|------|--------------|----------------|
| `blazor-ui-conventions/resources/theming.md` | 152 | Split: `theming-light.md` + `theming-dark.md` |
| `blazor-ui-conventions/resources/mudblazor-usage.md` | 144 | Split: `mudblazor-forms.md` + `mudblazor-layout.md` |
| `blazor-ui-conventions/resources/common-patterns.md` | 142 | Split: `dialog-patterns.md` + `state-patterns.md` |
| `blazor-css-isolation/resources/mudblazor-styling.md` | 143 | Split: `mudblazor-override-policy.md` + `mudblazor-component-patterns.md` |

**Target:** Each split file ~70-80 lines. Progressive disclosure works best with focused, bounded files. Update parent SKILL.md references accordingly.

---

#### T3-2: Tighten `settings.local.json` permissions

**File:** `.claude/settings.local.json`
**Changes:**

| Current | Change | Reason |
|---------|--------|--------|
| `Bash(dotnet:*)` | Replace with explicit allowlist: `dotnet build:*`, `dotnet test:*`, `dotnet run:*`, `dotnet ef:*` | Prevents `dotnet publish --self-contained`, `dotnet tool install` etc. from running without approval |
| `Bash(Out-File -FilePath C:/ISLAMU/GitHub/Event/test-output.txt...)` | Remove or replace with relative path or parameterized form | Hardcoded path breaks on any other machine |
| `mcp__acp__Write`, `mcp__acp__Edit` | Audit and document — or remove | Unclear what ACP client is; governance concern |

**Add timeout to all hooks in settings.json:**
```json
{ "type": "command", "command": "...", "timeout": 30000 }
```

---

#### T3-3: Update `context-state.json` — ISO 8601 + structured tracking

**File:** `.claude/context-state.json`
**Changes:**
- `LastUpdate` → ISO 8601 format: `"2026-03-27T00:18:53Z"`
- Add `ActiveBranch` field: `"develop"`
- Add `ActiveSprint` field: `"accessibility-improvements, css-modernization, background-customization"`
- Remove `RecentFocus` (drifts and becomes misleading) — use `ActiveSprint` instead
- Document in hooks/README.md which hook updates this file and when

---

#### T3-4: Update `agents/README.md` — Agent selection guide

**File:** `.claude/agents/README.md`
**Current:** 45 lines — lists agents by name, explains concept, no selection criteria
**Additions (~20 lines):**
- Decision table: what scenario → which agent
- When NOT to use an agent (quick single-file changes, prefer inline)
- Model selection guidance (inherit default, when to override)
- How to verify an agent ran correctly (check output format, look for required output sections)

---

#### T3-5: Fix `agentic-research` resource files

**Files:** 3 resource files under `.claude/skills/agentic-research/resources/`
**All are sparse (16-29 lines each)**
**Target:** Expand each to 40-60 lines:
- `source-hierarchy.md` — Detailed decision tree: when repo is authoritative vs. when to escalate to official docs vs. external research. Add concrete examples for each level.
- `verification-matrix.md` — Table of change types (EF Core migration, API endpoint, domain entity, CSS token) vs. required verification steps. Currently too abstract.
- `research-guardrails.md` — Specific data minimization rules (no API keys, no PII, no proprietary code in external tool queries). Add examples of what to redact.

---

#### T3-6: Add `PreCompact` hook for context preservation

**File:** `.claude/hooks/PreCompactPreserver.cs` (new) + wire in `settings.json`
**Purpose:** Before auto-compaction, persist the current task summary, modified files list, and active dev item to a `dev/active/.claude-session-state.md` file so context can be re-injected by the `SessionStart` hook after compaction
**Why:** JetBrains research shows LLM summarization elongates agent trajectories by ~15% due to obscured stopping signals. Observation masking + targeted state persistence outperforms aggressive LLM summarization.

**Scope:** ~50-line C# script that reads current git status and writes a structured markdown state file

---

### Tier 4 — Maintenance (Polish and hygiene)

| Item | File | Action |
|------|------|--------|
| `skill-rules.json` freshness | `.claude/skills/skill-rules.json` | Update `lastUpdated` monthly; document review cadence in CONTRIBUTING.md |
| `hooks/CONFIG.md` formatting | `.claude/hooks/CONFIG.md` | Normalize list vs. table inconsistency; add execution order documentation |
| `hooks/README.md` timeout docs | `.claude/hooks/README.md` | Add timeout behavior section (default 60s, `timeout` configuration key) |
| Dangling agent references | Multiple agents | Verify `dev/active/mudblazor-migration-v9/` still exists; remove reference if archived |
| `prd/SKILL.md` example | `.claude/skills/prd/SKILL.md` | Add one example PRD (even abbreviated) to make the workflow concrete |
| `CLAUDE.md` pruning | `CLAUDE.md` | Move Blazor UI Workflow section to a skill reference; review Documentation Index for pruning |
| `clean-architecture-rules` violation examples | `.claude/skills/clean-architecture-rules/resources/violation-examples.md` | Expand from 16 → ~60 lines with 3 real before/after examples |

---

## Part 5 — New Files Summary

| File | Status | Priority | Est. Lines |
|------|--------|----------|-----------|
| `.claude/hooks/SessionStartInjector.cs` | NEW | T1 — Critical | ~60 |
| `.claude/skills/outbox-pattern/SKILL.md` | NEW | T2 — High | ~60 |
| `.claude/skills/outbox-pattern/resources/entity-lifecycle.md` | NEW | T2 — High | ~50 |
| `.claude/skills/outbox-pattern/resources/writing-messages.md` | NEW | T2 — High | ~40 |
| `.claude/skills/outbox-pattern/resources/implementing-dispatcher.md` | NEW | T2 — High | ~50 |
| `.claude/skills/outbox-pattern/resources/configuration.md` | NEW | T2 — High | ~40 |
| `.claude/skills/design-system/SKILL.md` | NEW | T2 — High | ~70 |
| `.claude/skills/design-system/resources/token-tiers.md` | NEW | T2 — High | ~70 |
| `.claude/skills/design-system/resources/layer-ordering.md` | NEW | T2 — High | ~50 |
| `.claude/skills/design-system/resources/wrapper-components.md` | NEW | T2 — High | ~70 |
| `.claude/skills/design-system/resources/mudblazor-overrides.md` | NEW | T2 — High | ~50 |
| `.claude/skills/footer-management/SKILL.md` | NEW | T2 — High | ~55 |
| `.claude/skills/footer-management/resources/service-contract.md` | NEW | T2 — High | ~60 |
| `.claude/skills/footer-management/resources/template-dispatch.md` | NEW | T2 — High | ~40 |
| `.claude/skills/footer-management/resources/governance.md` | NEW | T2 — High | ~40 |
| `.claude/hooks/PreCompactPreserver.cs` | NEW | T3 — Medium | ~50 |

**Total new files: 16 | Est. ~855 lines**

---

## Part 6 — Files to Update Summary

| File | Priority | Change Type |
|------|----------|-------------|
| `.claude/skills/skill-rules.json` | T1 — Critical | Add 3 missing skill triggers + 3 new skill triggers |
| `.claude/skills/cqrs-mediatr-guidelines/resources/complete-examples.md` | T1 — Critical | Expand 7 → ~80 lines |
| `.claude/skills/blazor-bff-patterns/resources/token-forwarding.md` | T1 — Critical | Expand 14 → ~50 lines |
| `.claude/skills/blazor-bff-patterns/resources/service-layer-patterns.md` | T1 — Critical | Expand 14 → ~50 lines |
| `.claude/skills/clean-architecture-rules/resources/violation-examples.md` | T1 — Critical | Expand 16 → ~60 lines |
| `.claude/skills/dotnet-efcore-guidelines/resources/migrations.md` | T1 — Critical | Expand 13 → ~45 lines |
| All 10 agent `.md` files (missing frontmatter) | T2 — High | Add `type`, `enforcement`, `priority`, `tools` |
| 4 read-only agent `.md` files | T2 — High | Add `tools: Read, Grep, Glob` |
| `.claude/agents/codebase-verifier.md` | T2 — High | Expand 28 → ~60 lines |
| 4 key agents (output examples) | T2 — High | Add `## Example Output` section |
| `.claude/settings.local.json` | T3 — Medium | Tighten Bash permissions, remove hardcoded path |
| `.claude/settings.json` | T3 — Medium | Add `timeout` to all hooks; add SessionStart + PreCompact hooks |
| `.claude/context-state.json` | T3 — Medium | ISO 8601 timestamps; add ActiveBranch, ActiveSprint |
| `.claude/agents/README.md` | T3 — Medium | Add agent selection decision table |
| `.claude/hooks/README.md` | T4 — Maintenance | Add timeout documentation |
| `.claude/hooks/CONFIG.md` | T4 — Maintenance | Normalize formatting, add execution order |
| `CLAUDE.md` | T4 — Maintenance | Pruning pass; move Blazor UI workflow section |

---

## Part 7 — Recommended Execution Order

```
1. skill-rules.json (add 3 missing triggers)     [T1 — unblocks 3 inactive skills immediately]
2. Expand 5 stub resource files                   [T1 — fix worst quality issues]
3. Add tools restrictions to read-only agents     [T1 — deterministic safety improvement]
4. SessionStart hook (SessionStartInjector.cs)    [T1 — context preservation for long sessions]
5. Standardize agent frontmatter (10 agents)      [T2 — consistency pass]
6. Add output examples to 4 key agents            [T2 — usability improvement]
7. outbox-pattern skill (SKILL.md + 4 resources)  [T2 — covers active feature]
8. design-system skill (SKILL.md + 4 resources)   [T2 — covers active CSS modernization]
9. footer-management skill (SKILL.md + 3 resources) [T2 — covers active feature]
10. skill-rules.json (add 3 new skill triggers)   [T2 — wire new skills into activation system]
11. Expand codebase-verifier agent                [T2 — quality improvement]
12. Split 4 oversized resource files              [T3 — progressive disclosure improvement]
13. Tighten settings.local.json permissions       [T3 — security hygiene]
14. PreCompact hook                               [T3 — context preservation at scale]
15. context-state.json ISO 8601 + fields          [T3 — maintainability]
16. agents/README.md selection guide              [T3 — onboarding quality]
17. CLAUDE.md pruning pass                        [T4 — token budget management]
18. hooks/README.md + CONFIG.md polish            [T4 — documentation completeness]
```

---

## Appendix A — Context Engineering Principles Reference

Apply these principles when writing any new skill, agent, or hook:

### For CLAUDE.md

1. **Under 300 lines** — every line must pass the "would removal cause mistakes?" test
2. **No linter-enforceable rules** — if ESLint/Prettier/Roslyn analyzers catch it, don't document it
3. **Import via `@`** — detailed reference material belongs in skills, loaded via `@path`
4. **Emphasis is real** — `IMPORTANT:` and `YOU MUST` prefixes are legitimately effective signals, not theater; reserve for high-stakes rules

### For Skills

1. **Descriptions are activation mechanisms** — write for precision, include both what and when
2. **Progressive disclosure** — SKILL.md as table of contents; details in one-level-deep resources
3. **70-80 lines per resource** — focused enough to be read in one pass, deep enough to be useful
4. **Concrete over abstract** — input/output pairs outperform abstract descriptions every time
5. **One default, one escape hatch** — avoid listing five valid approaches; provide a preferred one

### For Agents

1. **Tool allowlist, not denylist** — always specify `tools`; grant minimum required
2. **Description triggers delegation** — include "use proactively when..." language in descriptions
3. **One example output** — even one realistic minimal example makes the format concrete
4. **Three to ten steps max** — agents doing more become unmaintainable and undebuggable
5. **Read-only roles** — if an agent only needs to analyze, give it only `Read, Grep, Glob`

### For Hooks

1. **Hooks are deterministic; instructions are probabilistic** — safety-critical rules belong in hooks, not CLAUDE.md
2. **Exit code protocol**: `0` = proceed (stdout injected into context), `2` = block (stderr becomes Claude's feedback), other = proceed + log error
3. **Keep hook output focused** — stdout for SessionStart/UserPromptSubmit hooks is injected into context; verbose output pollutes attention
4. **Always set `timeout`** — default 60s is often too long for compilation hooks; set explicit timeouts
5. **HTTP hooks for audit** — for enterprise audit trails, use `type: "http"` PostToolUse hooks rather than local log files

---

## Appendix B — The Four Context Engineering Failure Modes (Reference)

| Mode | Symptom | Remedy |
|------|---------|--------|
| **Context Poisoning** | Claude follows outdated patterns despite corrections | Clear CLAUDE.md of stale rules; refresh skill files; use `/clear` |
| **Context Distraction** | Claude ignores important instructions in favor of tangential content | Prune CLAUDE.md; move context to skills (only loaded when relevant) |
| **Context Confusion** | Claude alternates between two approaches inconsistently | Eliminate duplicate rules; single source of truth per concern |
| **Context Clash** | Claude ignores your instruction in favor of its own pattern | Move the rule to a hook (deterministic) or use `IMPORTANT:` prefix |

---

## Appendix C — Open Questions (Require investigation before acting)

1. **SecurityCheck.cs content** — What operations does it currently block at `PreToolUse`? If it's under-specified, dangerous commands may be executing without review.
2. **`mcp__acp__*` tools in settings.local.json** — What is the ACP client? Is it an official Anthropic tool or a third-party? Governance implications unclear.
3. **mudblazor-migration-v9 reference** — Does `dev/active/mudblazor-migration-v9/` still exist? Multiple agent files reference it. If archived, all references should be updated.
4. **C# hooks execution model** — Do the hooks compile and run via `dotnet script`? Or are they pre-compiled? What happens if the build fails in a hook? Timeout behavior needs documentation.
5. **`codebase-verifier` vs. `Stop` hook** — Is `BuildCheck.cs` in the hooks folder redundant with the `codebase-verifier` agent? If both run after task completion, they may be duplicating work.

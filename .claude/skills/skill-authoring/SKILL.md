---
name: skill-authoring
description: Create, update, and verify schema-compliant project skills with strong context engineering, resource depth, and evidence boundaries.
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Workflow skill for authoring durable project skills and resource libraries. -->
<!-- ABOUTME: Converts implementation-plan knowledge into schema-compliant, verifiable, context-efficient agent guidance. -->

## Purpose
Use this skill when creating or changing `.claude/skills/*` so the result is a concise router plus a durable resource library. It protects context quality, schema compliance, evidence boundaries, and future-agent usability.

## When to Load
- Keywords: create a skill, write a skill, skill resources, agentic engineering, context engineering, agent-context skill.
- Intent IDs: `create-agent-context-skill`.
- File patterns: `.claude/skills/**`, `.claude/contract/intents.yaml`, `Event.Architecture.Tests/AgentContext*Tests.cs`, `dev/active/**` workstreams that plan a skill.
- A plan or handoff says implementation knowledge should become reusable agent guidance.
- A skill is becoming long, vague, under-resourced, or hard to verify.

## When NOT to Load
- Not for ordinary application-code work unless the task changes agent instructions or skill files.
- Not for generic documentation edits that do not affect `.claude/skills`, `.claude/contract`, or agent-context tests.
- Not for creating external opencode configuration outside this repository.
- Not for adding a runtime feature where a domain, CQRS, auth, Blazor, or persistence skill is the primary rule source.
- Not for bypassing schema failures by adding skip-list exceptions.

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)
- [../../../docs/DOCUMENTATION_STYLE_GUIDE.md](../../../docs/DOCUMENTATION_STYLE_GUIDE.md)
- [../../../docs/OPERATIONS.md](../../../docs/OPERATIONS.md)
- [../../../docs/TESTING.md](../../../docs/TESTING.md)
- [../_SKILL_SCHEMA.md](../_SKILL_SCHEMA.md)
- [resources/index.md](resources/index.md)
- [resources/authoring-workflow.md](resources/authoring-workflow.md)
- [resources/context-engineering.md](resources/context-engineering.md)
- [resources/schema-checklist.md](resources/schema-checklist.md)
- [resources/resource-library-patterns.md](resources/resource-library-patterns.md)
- [resources/verification-and-tests.md](resources/verification-and-tests.md)

## Top 5 Invariants
1. `SKILL.md` is a compact routing and invariant file, while deep explanations, templates, checklists, and domain material live in `resources/*.md`.
2. Every new skill folder name must match frontmatter `name`, satisfy the required section order, and avoid `AgentContextSchemaTests.SkipSchemaMigration` exceptions.
3. The skill must preserve evidence boundaries by separating verified repository facts, source-derived framework claims, assumptions, and future validation needs.
4. Must-read links, resource indexes, and verification hooks must let a future agent reproduce the workflow without relying on prior chat context.
5. Context updates belong in `dev/active/*` when the skill work is substantial, especially before handoff, compaction, or switching tasks.

## Top 5 Anti-Patterns
1. Monolithic skill file, which exceeds the line cap and hides reusable depth outside resource files.
2. Vague activation boundary, which loads the skill too often and wastes context on unrelated work.
3. Unverified authority claim, which presents advice, plans, or framework interpretation as proof, certification, or implemented behavior.
4. Broken resource graph, which leaves future agents with dead links, missing prerequisites, or no clear reading order.
5. Schema-test bypass, which adds skip exceptions instead of making the skill conform to the project contract.

## Minimal Examples
```text
Skill authoring loop:
1. Classify the change with create-agent-context-skill.
2. Read AGENTS.md, _SKILL_SCHEMA.md, active plan/context/tasks, and source material.
3. Draft SKILL.md as a short router with exactly five invariants and five anti-patterns.
4. Move depth into resources/index.md and focused resource files.
5. Run architecture schema/link tests and update dev docs before handoff.
```

```text
Minimum output boundary:
This skill provides workflow guidance grounded in the listed sources. It does not prove the product is correct, replace source review, or certify claims that require external validation.
```

## Verification Hooks
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `git diff --check -- .claude/contract/intents.yaml .claude/skills Event.Architecture.Tests/AgentContextLinkTests.cs`

## Related Skills
- [../agentic-research/SKILL.md](../agentic-research/SKILL.md)
- [../senior-cto-feedback/SKILL.md](../senior-cto-feedback/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)

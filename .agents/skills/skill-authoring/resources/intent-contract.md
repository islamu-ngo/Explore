<!-- ABOUTME: Intent-manifest guidance for skill creation and agent-context infrastructure changes. -->
<!-- ABOUTME: Explains how create-agent-context-skill answers the repository Contribution Contract. -->

# Intent Contract

## Intent ID

Use `create-agent-context-skill` when a task creates or materially updates project skills, skill resources, agent-context tests, or the intent routing that loads those skills.

## Files In Scope

Primary files are:

- `.claude/skills/<skill-name>/SKILL.md`
- `.claude/skills/<skill-name>/resources/*.md`
- `.claude/contract/intents.yaml`
- `Event.Architecture.Tests/AgentContext*Tests.cs`
- `dev/active/<task>/*` when an active workstream is driving the skill work

Do not use this intent as permission to edit application code. If a skill references application behavior, link to source docs or code and keep the application change under its own intent.

## Required Reads

Always read the schema and the active source material before editing. For domain-heavy skills, read the upstream research, plan, or implementation notes that justify each invariant and resource.

## Acceptance Criteria

- The new skill is schema-compliant without skip-list changes.
- The skill has a precise activation boundary and clear non-goals.
- The resource library is reachable through `resources/index.md`.
- Claims are evidence-bounded and do not overstate validation.
- Architecture tests prove the schema, links, and intent manifest still work.

## Forbidden Moves

- Do not add the new skill to `SkipSchemaMigration`.
- Do not create a skill that relies on prior chat context for essential instructions.
- Do not claim certification, production behavior, or external validation unless the source evidence proves it.
- Do not bury verification instructions in prose; keep commands copy-pasteable.

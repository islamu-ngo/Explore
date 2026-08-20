<!-- ABOUTME: Output templates for skill-authoring handoffs and final summaries. -->
<!-- ABOUTME: Helps agents teach what changed without relying on vague status messages. -->

# Output Template

## Final Summary

Use this shape after implementation:

```text
Implemented <skill-name> as a schema-compliant workflow skill.

Changed:
- .agents/skills/<skill-name>/SKILL.md: compact router with activation boundaries, invariants, anti-patterns, examples, verification hooks, and related skills.
- .agents/skills/<skill-name>/resources/index.md: resource reading map.
- .agents/skills/<skill-name>/resources/*.md: durable checklists/templates/frameworks.
- .agents/contract/intents.yaml: intent routing if added or updated.
- Event.Architecture.Tests/...: enforcement updates if any.

Architecture:
The skill follows the repo pattern of short SKILL.md plus resource library. It preserves source evidence boundaries and uses architecture tests as enforcement.

Verified:
- <exact command> passed.

Remaining:
- <anything not run or deferred>.
```

## Handoff Note

For context compaction or task pause, record:

- Current state.
- Files changed.
- Commands run and results.
- What remains.
- Known risks and unrelated dirty worktree areas.

## Review Response

If asked to review a skill, lead with findings. Name file paths and line references, then list open questions and verification gaps.

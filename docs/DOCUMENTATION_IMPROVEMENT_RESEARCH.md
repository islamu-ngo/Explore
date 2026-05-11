ABOUTME: Captures the high-value documentation research outcomes for this repository.
ABOUTME: Converts broad research into a short, prioritized improvement backlog.

# Documentation Improvement Research

## Scope

This summary distills external documentation best practices into decisions that fit this codebase.

## Key Findings

1. Structure by intent, not by technology.
2. Keep reference docs factual and close to implementation.
3. Keep tutorials short and task-first.
4. Use one consistent terminology set across all docs.
5. Treat documentation updates as part of feature completion.

## Recommended Documentation Model

Use four doc intents (Diataxis-style):

- Tutorial: learn by doing.
- How-to: complete a specific task.
- Reference: exact behavior, keys, contracts.
- Explanation: architectural rationale and tradeoffs.

For this repository:

- `docs/API.md`, `docs/CONFIGURATION.md`, `docs/QUICK_REFERENCE.md` -> reference.
- `docs/docs-website/tutorials/*` -> tutorials.
- `docs/ARCHITECTURE.md`, `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md` -> explanation.

## Writing Standards That Matter Most

- Prefer short sections with explicit headings.
- Prioritize non-inferable facts (fallback orders, key names, guardrails, defaults).
- Avoid large visual ASCII diagrams in markdown.
- Keep code examples minimal and only where ambiguity is likely.
- Link to one authoritative file/path when describing runtime behavior.

## Current Gaps (Observed)

- Several docs mixed conceptual guidance and runbook steps.
- Some docs included outdated architecture assumptions.
- Some long files repeated information that already exists elsewhere.

## Priority Backlog

1. Keep authoritative reference docs synchronized with code:
   - `API.md`
   - `CONFIGURATION.md`
   - `SECURITY.md`
   - `MULTI_TENANCY.md`
   - `RENDER_POLICIES.md`
2. Keep troubleshooting and getting-started task-oriented.
3. Keep changelog entries short and behavior-focused.
4. Keep architecture docs focused on actual implemented patterns.

## Quality Gate for Future Doc Changes

Before merging doc updates:

1. Verify key claims directly in source files.
2. Remove speculative or roadmap statements unless explicitly labeled.
3. Ensure each section answers one concrete user question.
4. Ensure each page has links to related authoritative docs.

## Success Criteria

- New contributors can identify where a behavior is defined in under 2 minutes.
- Operational incidents can be triaged using docs without reading large files.
- Fewer contradictions between docs and runtime behavior.

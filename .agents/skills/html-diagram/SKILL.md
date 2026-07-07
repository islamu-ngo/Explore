---
name: html-diagram
description: Create a self-contained HTML artifact focused on a high-quality SVG architecture or system diagram with minimal supporting prose.
type: workflow
enforcement: suggest
priority: low
disable-model-invocation: true
---

# HTML Diagram

## Purpose

Create one self-contained HTML file that makes an architecture, stack, or system behavior understandable through a high-quality SVG diagram. Favor clarity, spatial hierarchy, and interaction over prose volume.

## When to Load

- The user asks for an HTML diagram, architecture visualization, stack map, or system flow view.
- The artifact should be full-screen or diagram-first with only supporting labels and notes.
- The user needs a browser-openable visual explanation rather than production UI code.

## When NOT to Load

- Not for prose-heavy reports or explainers where `html` is the better fit.
- Not for implementation plans where `html-plan` is the better fit.
- Not for source-code architecture changes, Mermaid-only diagrams, or repository documentation updates.

## Must-Read Docs

- `references/html-effectiveness/`
- `references/architecture-example.html`

## Top 5 Invariants

1. The diagram must be built as inline SVG in a self-contained HTML file.
2. The page must include dark mode using CSS variables on `:root` and `html.dark`.
3. SVG colors must be styled through CSS classes and theme variables rather than hard-coded hex values inside the SVG.
4. The diagram should simplify the system enough to make the architecture click quickly.
5. Interactions or animations should clarify system behavior rather than decorate it.

## Top 5 Anti-Patterns

1. Prose-heavy layouts bury the diagram and defeat the skill's purpose.
2. Hard-coded SVG colors make the diagram fail dark mode.
3. Overloaded node graphs create visual noise instead of understanding.
4. Unlabeled animated paths look impressive but do not explain behavior.
5. External diagram libraries add dependency risk for a shareable one-file artifact.

## Minimal Examples

```text
Use for: "Create an HTML diagram of the request flow."
Avoid for: "Write a detailed Markdown architecture doc."
```

## Verification Hooks

- Open the generated file in a browser and confirm the SVG remains legible in light and dark mode.
- Click or trigger any diagram interactions and verify they explain the intended system behavior.

## Related Skills

- [../html/SKILL.md](../html/SKILL.md)
- [../html-plan/SKILL.md](../html-plan/SKILL.md)

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

Create one self-contained HTML file that makes an architecture, stack, or system behavior understandable through a high-quality SVG diagram. Favor clarity, spatial hierarchy, and interaction over prose volume. Always save generated HTML files into the `dev/report/visuals/` folder (e.g. `dev/report/visuals/<diagram-name>.html`). NEVER write HTML files to the repository root.

## When to Load

- The user asks for an HTML diagram, architecture visualization, stack map, or system flow view.
- The artifact should be full-screen or diagram-first with only supporting labels and notes, saved in `dev/report/visuals/`.
- The user needs a browser-openable visual explanation rather than production UI code.

## When NOT to Load

- Not for prose-heavy reports or explainers where `html` is the better fit.
- Not for implementation plans where `html-plan` is the better fit.
- Not for source-code architecture changes, Mermaid-only diagrams, or repository documentation updates.

## Must-Read Docs

- `references/html-effectiveness/`
- `references/architecture-example.html`

## Top 5 Invariants

1. All generated HTML files MUST be created inside `dev/report/visuals/` (e.g. `dev/report/visuals/diagram-name.html`). Never save HTML files in the repository root.
2. The diagram must be built as inline SVG in a self-contained HTML file.
3. The page must include dark mode using CSS variables on `:root` and `html.dark`.
4. SVG colors must be styled through CSS classes and theme variables rather than hard-coded hex values inside the SVG.
5. The diagram should simplify the system enough to make the architecture click quickly.

## Top 5 Anti-Patterns

1. Creating generated HTML files in the repository root or outside `dev/report/visuals/` pollutes the repository.
2. Prose-heavy layouts bury the diagram and defeat the skill's purpose.
3. Hard-coded SVG colors make the diagram fail dark mode.
4. Overloaded node graphs create visual noise instead of understanding.
5. External diagram libraries add dependency risk for a shareable one-file artifact.

## Minimal Examples

```text
Use for: "Create an HTML diagram of the request flow." -> Saves to dev/report/visuals/request-flow.html
Avoid for: "Write a detailed Markdown architecture doc."
```

## Verification Hooks

- Confirm the HTML diagram file is created inside `dev/report/visuals/` and not in the repository root.
- Open the generated file in a browser and confirm the SVG remains legible in light and dark mode.
- Click or trigger any diagram interactions and verify they explain the intended system behavior.

## Related Skills

- [../html/SKILL.md](../html/SKILL.md)
- [../html-plan/SKILL.md](../html-plan/SKILL.md)

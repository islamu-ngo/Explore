---
name: html
description: Create a self-contained HTML artifact for reports, explainers, comparisons, prototypes, or decks when no narrower HTML diagram or plan skill applies.
type: workflow
enforcement: suggest
priority: low
disable-model-invocation: true
---

# HTML

## Purpose

Create one self-contained HTML file that communicates the user's requested artifact clearly and visually. Use the effective HTML references for style, density, tone, and interaction choices.

## When to Load

- The user asks for a self-contained HTML report, explainer, comparison, prototype, deck, or visual document.
- The request is best delivered as one browser-openable HTML file rather than Markdown or application code.
- The request is not specifically a diagram-first or plan-first artifact.

## When NOT to Load

- Not for architecture diagrams or stack visualizations where `html-diagram` is the better fit.
- Not for pragmatic implementation plans where `html-plan` is the better fit.
- Not for editing production Blazor, Razor, CSS, or application UI code.

## Must-Read Docs

- `references/html-effectiveness/`

## Top 5 Invariants

1. The output is a single self-contained HTML file with embedded CSS and JavaScript unless the user explicitly asks otherwise.
2. The page must include dark mode using CSS variables on `:root` and `html.dark`.
3. A small theme toggle must persist the user choice in `localStorage` and apply before paint from the document head.
4. The artifact must match the user's requested purpose without inflating it into a larger product or app.
5. Visual density, spacing, and tone should be guided by `references/html-effectiveness/`.

## Top 5 Anti-Patterns

1. Multi-file output fragments the artifact and makes it harder to share.
2. Hard-coded single-theme colors break the required light/dark presentation.
3. Generic dashboard styling ignores the effective HTML reference style.
4. Turning an explainer into a framework app adds unnecessary build and hosting complexity.
5. Rewriting the user's content into a bigger concept changes the requested artifact.

## Minimal Examples

```text
Use for: "Make this comparison as a single HTML page."
Avoid for: "Build this into the Blazor app."
```

## Verification Hooks

- Open the generated HTML file in a browser and toggle light/dark mode.
- Verify the file works without external assets, build steps, or network access unless explicitly requested.

## Related Skills

- [../html-diagram/SKILL.md](../html-diagram/SKILL.md)
- [../html-plan/SKILL.md](../html-plan/SKILL.md)

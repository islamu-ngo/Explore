---
name: html-plan
description: Create a pragmatic self-contained HTML plan that organizes the user's plan content visually without expanding the scope unnecessarily.
type: workflow
enforcement: suggest
priority: low
disable-model-invocation: true
---

# HTML Plan

## Purpose

Create one self-contained HTML file that turns a plan into a clear, practical, visually organized artifact. Keep the writing close to the user's intent while improving grammar, structure, and scanability. Always save generated HTML files into the `dev/report/visuals/` folder (e.g. `dev/report/visuals/<plan-name>.html`). NEVER write HTML files to the repository root.

## When to Load

- The user asks for a plan page, implementation plan, roadmap, checklist, or timeline as HTML.
- The user wants the plan polished visually without turning it into a larger proposal.
- The output should be one browser-openable HTML file saved in `dev/report/visuals/`.

## When NOT to Load

- Not for diagram-first architecture visualizations where `html-diagram` is the better fit.
- Not for general HTML reports or explainers where `html` is the better fit.
- Not for creating repository implementation plans under `dev/active/`; use the project planning workflow instead.

## Must-Read Docs

- `references/html-effectiveness/`

## Top 5 Invariants

1. All generated HTML files MUST be created inside `dev/report/visuals/` (e.g. `dev/report/visuals/plan-name.html`). Never save HTML files in the repository root.
2. The output is a single self-contained HTML file with embedded CSS and JavaScript unless the user explicitly asks otherwise.
3. The plan must stay pragmatic and close to the user's supplied scope.
4. The page must include dark mode using CSS variables on `:root` and `html.dark`.
5. A small theme toggle must persist the user choice in `localStorage` and apply before paint from the document head.

## Top 5 Anti-Patterns

1. Creating generated HTML files in the repository root or outside `dev/report/visuals/` pollutes the repository.
2. Expanding the plan into a larger strategy changes the user's requested scope.
3. Dense wall-of-text sections defeat the purpose of an HTML plan artifact.
4. Hard-coded single-theme styling breaks the required light/dark presentation.
5. Decorative complexity makes the plan harder to execute.

## Minimal Examples

```text
Use for: "Turn this launch plan into a clean HTML page." -> Saves to dev/report/visuals/launch-plan.html
Avoid for: "Create repository dev docs for this feature."
```

## Verification Hooks

- Confirm the HTML plan file is created inside `dev/report/visuals/` and not in the repository root.
- Open the generated HTML file and confirm phases, priorities, risks, and next steps are easy to scan.
- Toggle light/dark mode and verify the plan remains readable without external assets.

## Related Skills

- [../html/SKILL.md](../html/SKILL.md)
- [../html-diagram/SKILL.md](../html-diagram/SKILL.md)

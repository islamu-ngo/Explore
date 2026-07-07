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

Create one self-contained HTML file that turns a plan into a clear, practical, visually organized artifact. Keep the writing close to the user's intent while improving grammar, structure, and scanability.

## When to Load

- The user asks for a plan page, implementation plan, roadmap, checklist, or timeline as HTML.
- The user wants the plan polished visually without turning it into a larger proposal.
- The output should be one browser-openable HTML file.

## When NOT to Load

- Not for diagram-first architecture visualizations where `html-diagram` is the better fit.
- Not for general HTML reports or explainers where `html` is the better fit.
- Not for creating repository implementation plans under `dev/active/`; use the project planning workflow instead.

## Must-Read Docs

- `references/html-effectiveness/`

## Top 5 Invariants

1. The output is a single self-contained HTML file with embedded CSS and JavaScript unless the user explicitly asks otherwise.
2. The plan must stay pragmatic and close to the user's supplied scope.
3. The page must include dark mode using CSS variables on `:root` and `html.dark`.
4. A small theme toggle must persist the user choice in `localStorage` and apply before paint from the document head.
5. Visual organization should make phases, priorities, risks, and next steps easier to scan.

## Top 5 Anti-Patterns

1. Expanding the plan into a larger strategy changes the user's requested scope.
2. Dense wall-of-text sections defeat the purpose of an HTML plan artifact.
3. Hard-coded single-theme styling breaks the required light/dark presentation.
4. Decorative complexity makes the plan harder to execute.
5. External dependencies make the one-file plan less portable.

## Minimal Examples

```text
Use for: "Turn this launch plan into a clean HTML page."
Avoid for: "Create repository dev docs for this feature."
```

## Verification Hooks

- Open the generated HTML file and confirm phases, priorities, risks, and next steps are easy to scan.
- Toggle light/dark mode and verify the plan remains readable without external assets.

## Related Skills

- [../html/SKILL.md](../html/SKILL.md)
- [../html-diagram/SKILL.md](../html-diagram/SKILL.md)

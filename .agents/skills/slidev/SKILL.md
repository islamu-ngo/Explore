---
name: slidev
description: "Load when creating or editing a Slidev `slides.md` developer talk/workshop with Markdown slides, Vue components, live code, Mermaid/LaTeX, presenter notes, or Slidev PDF/PPTX export; not for native PowerPoint decks or general docs."
type: workflow
enforcement: suggest
priority: medium
---
<!-- ABOUTME: Slidev authoring workflow for Markdown/Vue developer presentations, exports, and live demos. -->
<!-- ABOUTME: Keeps technical decks practical, accessible, and easy to verify without duplicating Slidev reference material inline. -->

## Must-Read Docs
- [../../../docs/DESIGN_SYSTEM.md](../../../docs/DESIGN_SYSTEM.md)
- [../../../docs/ACCESSIBILITY.md](../../../docs/ACCESSIBILITY.md)
- [../../../docs/DOCUMENTATION_STYLE_GUIDE.md](../../../docs/DOCUMENTATION_STYLE_GUIDE.md)

## Top 5 Invariants
1. A Slidev deck starts with headmatter and uses `---` separators for slide boundaries.
2. Technical decks keep one primary idea per slide and move long explanations into presenter notes.
3. Local static assets belong in the deck `public/` folder and should be referenced with stable root-relative paths.
4. Code examples must be short, readable, and syntax-highlighted with line focus only where it improves comprehension.
5. Export workflows must be verified by running the deck locally or generating the requested PDF/PPTX artifact.

## Top 5 Anti-Patterns
1. Overloading a slide with full documentation text makes the deck unreadable during presentation.
2. Depending on remote images without bundling or fallback behavior breaks offline delivery.
3. Using complex animations for every transition distracts from the technical message.
4. Omitting presenter notes for demo-heavy talks leaves the delivery flow unreproducible.
5. Shipping an exported deck without opening the output can hide broken fonts, clipping, or missing assets.

## Minimal Examples
```md
---
theme: default
title: Event Platform Walkthrough
---

# First Slide

One idea with one supporting example.

<!--
Presenter note: explain the context before showing the demo.
-->
```

```bash
pnpm create slidev
pnpm run dev
pnpm run export
```

## Verification Hooks
- `pnpm run dev`
- `pnpm run build`
- `pnpm run export`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Related Skills
- [../design-system/SKILL.md](../design-system/SKILL.md)
- [../accessibility/SKILL.md](../accessibility/SKILL.md)
- [../skill-authoring/SKILL.md](../skill-authoring/SKILL.md)

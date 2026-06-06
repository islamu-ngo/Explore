---
name: slidev
description: Create and maintain developer-focused Slidev presentations with Markdown slides, Vue components, code demos, diagrams, presenter notes, and export workflows.
type: workflow
enforcement: suggest
priority: medium
---
<!-- ABOUTME: Slidev authoring workflow for Markdown/Vue developer presentations, exports, and live demos. -->
<!-- ABOUTME: Keeps technical decks practical, accessible, and easy to verify without duplicating Slidev reference material inline. -->

## Purpose
Use this skill when creating or editing Slidev decks, developer talks, workshop material, or presentation exports. It keeps slides concise, verifiable, and aligned with project accessibility and design expectations.

## When to Load
- Keywords: Slidev, slides, presentation, deck, talk, workshop, presenter notes, export PDF, export PPTX.
- File patterns: `**/slides.md`, `**/*.slides.md`, `**/slidev.config.*`, `**/pages/*.md`.
- Tasks that need Markdown slides with live code, Mermaid diagrams, LaTeX, Monaco editor examples, or custom Vue components.

## When NOT to Load
- Not for `.pptx` or PowerPoint artifact generation; use the presentations plugin instead.
- Not for general Blazor, API, or domain implementation work that does not touch a Slidev deck.
- Not for repository documentation pages unless the requested output is explicitly a presentation deck.

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
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Related Skills
- [../design-system/SKILL.md](../design-system/SKILL.md)
- [../accessibility/SKILL.md](../accessibility/SKILL.md)
- [../skill-authoring/SKILL.md](../skill-authoring/SKILL.md)

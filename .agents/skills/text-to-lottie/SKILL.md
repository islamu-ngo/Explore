---
name: text-to-lottie
description: "Load when creating, editing, or debugging Lottie/Bodymovin JSON for the local Skia Skottie player: logo/type/SVG animation, loaders, icons, UI microinteractions, lower thirds, charts, diagrams, scene motion, effects, slots, or controls; not for static raster/SVG, CSS-only motion, or video editing."
type: workflow
enforcement: suggest
priority: medium
---
<!-- ABOUTME: Thin control plane for authoring and verifying Lottie scenes in Skia Skottie. -->
<!-- ABOUTME: Routes each animation type to one recipe plus only necessary supporting references. -->

# Text To Lottie

## Rules

- Read [player contract](references/player-contract.md) for every create/edit/fix task and verify with the official Skia Skottie player.
- Resolve the target by explicit file path, then route, then known task scene; never overwrite a non-placeholder default scene.
- Scene output lives at `public/projects/<project>/<scene-N>/lottie.json`; re-read it immediately before overwrite because the UI can persist slot edits.
- Include valid top-level Lottie metadata and treat `op` as exclusive.
- Default logos, icons, loaders, overlays, lower thirds, and SVG-derived assets to transparent; full-frame scenes get one deliberate background.
- Prefer native text with a shipped matching font; use vector text only for path-specific effects.
- Use purposeful staging/easing and verify real frames; JSON validity alone is insufficient.
- Premium/minimal means restraint: whitespace, scale, weight, brightness, and timing before cards, borders, glow, dividers, or stacked tints.

## Reference routing

| Deliverable | Load in addition to player contract |
|---|---|
| JSON/keyframes/shapes/assets/slots | [spec map](references/lottie-spec-map.md) |
| Logo | [logo recipe](references/recipe-logo.md) + [motion](references/motion-taste.md) |
| Typography or lower third | [typography](references/recipe-typography.md) or [lower thirds](references/recipe-lower-thirds.md) + [design](references/design-taste.md) |
| Loader/icon/state feedback | [loaders/icons](references/recipe-loaders-icons.md) + [motion](references/motion-taste.md) |
| UI microinteraction | [UI recipe](references/recipe-ui-microinteractions.md) + [motion](references/motion-taste.md) |
| SVG source | Primary recipe + [SVG compatibility](references/svg-compatibility.md) |
| Camera/scene motion | [camera recipe](references/recipe-camera-scene-motion.md) |
| Diagram or data/chart | [diagram](references/recipe-diagram-technical.md) or [data](references/recipe-data-stats.md) |
| Promo or visual effect | [promo](references/recipe-product-promo.md) or [effects](references/recipe-visual-effects.md) + [design](references/design-taste.md) |
| Multi-beat/long-form scene | [chapterization](references/chapterization-transition-grammar.md) |

Choose one primary recipe, then add only source-format or treatment references required by the prompt.

## Workflow

1. Resolve the scene and read its current JSON plus the player contract.
2. Choose background policy and the smallest matching reference set.
3. Edit `lottie.json`; add `controls.json` only for useful editable slots.
4. Validate JSON and load the scene in the official player.
5. Inspect frame 0, a representative midpoint, and `op - 1`; fix missing assets, crop, text overflow, layer order, timing, easing, or SVG artifacts.

## Verification

```bash
node -e "JSON.parse(require('fs').readFileSync('public/projects/<project>/<scene-N>/lottie.json','utf8'))"
```

Confirm the scene appears in `GET /__context`, inspect pinned browser frames, and verify the intended background policy.

Use `evals/*` only when changing this skill's routing or quality behavior, never during normal animation work.

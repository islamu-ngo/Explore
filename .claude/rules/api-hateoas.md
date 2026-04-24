---
name: api-hateoas
description: Apply when editing Explore.API HAL assemblers, link policies, or related HATEOAS infrastructure.
paths:
  - "Explore.API/Hateoas/**/*.cs"
related_skills: [clean-architecture-rules, auth-patterns]
related_docs: [docs/API.md, docs/AUTHORIZATION.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.API.IntegrationTests, Explore.Blazor.Client.Tests, Event.Architecture.Tests]
related_intents: [add-hal-link, add-get-endpoint, add-write-endpoint, blazor-component-affordance]
---
<!-- ABOUTME: Path-scoped rules for API HAL and HATEOAS components. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# API HATEOAS Rules

> **Applies to:** `Explore.API/Hateoas/**/*.cs`.
> **Authority:** HAL affordance rules are canonical in `docs/API.md`, `docs/AUTHORIZATION.md`, and `docs/QUICK_REFERENCE.md`.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Emit links with `yield return` | Keep per-link emission explicit and lazy | Build ad hoc mutable lists everywhere |
| 2 | Separate collection and detail affordances | Maintain distinct policies when list/detail capabilities differ | Reuse one policy and hope the DTO state sorts it out |
| 3 | Bind links to named routes | Use `RouteNames` values that match controller attribute names exactly | Hardcode route strings or drift from controller names |
| 4 | Keep HAL as client truth | Expose or hide mutation links here so clients can gate affordances | Push per-resource action gating into UI role checks |
| 5 | Preserve batch authorization flow | Feed candidate links into the documented 4-phase capability pipeline | Reintroduce per-item authorization chatter |
| 6 | Fail closed on uncertainty | Omit links when permission or route resolution is not sound | Emit optimistic links that may 403 later |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/API.md`
- `docs/AUTHORIZATION.md`
- `.claude/skills/clean-architecture-rules/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- UI-only role checks used as the real affordance source.
- Route-name drift between controllers and link policies.
- Cross-layer HTTP concerns moved into Application handlers.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests`, `Event.Architecture.Tests`

## Related

- Intents: `add-hal-link`, `add-get-endpoint`, `add-write-endpoint`, `blazor-component-affordance`
- Agents: `.claude/agents/code-architecture-reviewer.md`, `.claude/agents/blazor-component-architect.md`
- Rules: `api-controllers.md`, `blazor-client.md`

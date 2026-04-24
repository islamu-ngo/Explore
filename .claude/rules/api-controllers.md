---
name: api-controllers
description: Apply when editing Explore.API controller actions and route contracts.
paths:
  - "Explore.API/Controllers/**/*.cs"
related_skills: [cqrs-mediatr-guidelines, auth-patterns]
related_docs: [docs/API.md, docs/GOVERNANCE.md, docs/AUTHORIZATION.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.API.IntegrationTests, Event.Architecture.Tests]
related_intents: [add-get-endpoint, add-write-endpoint, openapi-contract-change, add-hal-link]
---
<!-- ABOUTME: Path-scoped rules for API controllers. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# API Controller Rules

> **Applies to:** `Explore.API/Controllers/**/*.cs`.
> **Authority:** The controller-authoring standard is canonical in `docs/QUICK_REFERENCE.md` and `docs/API.md`.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Make routes explicit | Use explicit templates and `Name = RouteNames.Xxx` on every action | Rely on bare `[HttpGet]` or unnamed routes |
| 2 | Classify endpoints explicitly | Apply `[EndpointClassification(...)]` at controller or action level | Leave endpoint class implicit |
| 3 | Type responses for OpenAPI | Add `[ProducesResponseType]` for success and expected errors | Depend on inferred response metadata |
| 4 | Keep controller logic thin | Dispatch MediatR, assemble HAL, return result | Put business or persistence logic in the controller |
| 5 | Follow auth conventions | Public reads are typically `[AllowAnonymous]`; writes require `[Authorize]` | Make write endpoints anonymous by omission |
| 6 | Avoid overloaded semantics | One verb + route template maps to one action contract | Stack ambiguous actions on the same route shape |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/API.md`
- `docs/AUTHORIZATION.md`
- `docs/GOVERNANCE.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- Changing route names without updating the HAL path that depends on them.
- Letting controller code become a second application layer.
- Skipping explicit error response metadata on new public endpoints.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.API.IntegrationTests`, `Event.Architecture.Tests`

## Related

- Intents: `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`
- Agents: `.claude/agents/clean-code-architect.md`, `.claude/agents/code-architecture-reviewer.md`
- Rules: `application-layer.md`, `api-hateoas.md`

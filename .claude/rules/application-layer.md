---
name: application-layer
description: Apply when editing Explore.Application CQRS handlers, requests, DTOs, and validators.
paths:
  - "Explore.Application/**/*.cs"
related_skills: [cqrs-mediatr-guidelines, clean-architecture-rules]
related_docs: [docs/ARCHITECTURE.md, docs/GOVERNANCE.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Application.UnitTests, Event.Architecture.Tests]
related_intents: [add-cqrs-handler, add-get-endpoint, add-write-endpoint, update-repository-query]
---
<!-- ABOUTME: Path-scoped rules for the Application layer. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# Application Layer Rules

> **Applies to:** `Explore.Application/**/*.cs`.
> **Authority:** Canonical invariants live in `docs/QUICK_REFERENCE.md`; this file narrows how they apply here.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Keep CQRS separated | Commands mutate, queries read, handlers stay single-purpose | Mix reads and writes in one handler |
| 2 | Enforce manual validation | Instantiate validators inside handlers/services as documented | Inject `IValidator<T>` through DI |
| 3 | Keep repos entity-first | Fetch entities from repositories, then map in the handler | Return DTOs from repositories |
| 4 | Respect response contracts | Use `BaseCommandResponse<Guid>` for create/update patterns unless local feature rules differ | Invent one-off mutation envelopes casually |
| 5 | Preserve pipeline behavior contracts | Pass `CancellationToken`, respect authorization/idempotency/caching patterns | Bypass MediatR conventions in controllers or services |
| 6 | Keep dependencies inward | Reference Domain-only contracts and abstractions from Application | Reach into API, Blazor, or persistence implementation details |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/ARCHITECTURE.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/clean-architecture-rules/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- Repository DTO shaping.
- Controller logic leaking into handlers or handlers depending on presentation types.
- Hidden business defaults added to domain objects from Application without an explicit source.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Application.UnitTests`, `Event.Architecture.Tests`

## Related

- Intents: `add-cqrs-handler`, `add-get-endpoint`, `add-write-endpoint`, `update-repository-query`
- Agents: `.claude/agents/clean-code-architect.md`, `.claude/agents/code-architecture-reviewer.md`
- Rules: `api-controllers.md`, `efcore-persistence.md`, `domain.md`

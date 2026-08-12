---
name: application-layer
description: Apply when editing Explore.Application CQRS handlers, requests, DTOs, and validators.
paths:
  - "src/Explore.Application/**/*.cs"
related_skills: [cqrs-mediatr-guidelines, clean-architecture-rules]
related_docs: [docs/ARCHITECTURE.md, docs/GOVERNANCE.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Application.UnitTests, Event.Architecture.Tests]
related_intents: [add-cqrs-handler, add-get-endpoint, add-write-endpoint, update-repository-query]
---

# Application Layer Rules

## Applies To
- `src/Explore.Application/**/*.cs`

## Path-Specific Constraints
- **Handler Purity**: Handlers must stay single-purpose (one Command/Query per handler). No mixing of read/write concerns.
- **Contract Adherence**: Queries return DTOs shaped for the consumer; Commands return `BaseCommandResponse<TId>`.
- **Dependency Direction**: Reference Domain-only contracts and abstractions; never reach "outward" into API, Blazor, or persistence implementation details.
- **Cancellation**: Always pass `CancellationToken` through to all async calls (repository, cache, etc.).

## Must Read
- [docs/QUICK_REFERENCE.md#critical-rules](../../docs/QUICK_REFERENCE.md#critical-rules) (Rules #1, #2, #5, #11)
- [docs/ARCHITECTURE.md](../../docs/ARCHITECTURE.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.Application.UnitTests`, `Event.Architecture.Tests`

## Related
- Intents: `add-cqrs-handler`, `add-get-endpoint`, `add-write-endpoint`, `update-repository-query`
- Agents: `architect-agent.md`, `backend-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `api-controllers.md`, `efcore-persistence.md`, `domain.md`

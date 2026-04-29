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

# API HATEOAS Rules

## Applies To
- `Explore.API/Hateoas/**/*.cs`

## Path-Specific Constraints
- **Yield Return Pattern**: Always use the `yield return` pattern for link emission. Do not build mutable lists.
- **Policy Separation**: Maintain strictly separate policies for Resource-Detail and Resource-Collection (do not reuse).
- **Capability Pipeline**: Feed candidate links through the documented 4-phase capability pipeline (Auth → Resource State → Module Toggle → Visibility).
- **Fail Closed**: If route resolution or permission check is uncertain, omit the link entirely.

## Must Read
- [docs/QUICK_REFERENCE.md#critical-rules](../../docs/QUICK_REFERENCE.md#critical-rules) (Rules #16, #20, #21)
- [docs/API.md](../../docs/API.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests`

## Related
- Intents: `add-hal-link`, `add-get-endpoint`, `add-write-endpoint`, `blazor-component-affordance`
- Agents: `presentation-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `api-controllers.md`, `blazor-client.md`

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

# API Controller Rules

## Applies To
- `Explore.API/Controllers/**/*.cs`

## Path-Specific Constraints
- **Logic Thinning**: Controller actions must be limited to: dispatching MediatR, assembling HAL links (via policies), and returning the `ActionResult`. No business logic.
- **Contract Integrity**: Every action MUST use a `RouteNames.Xxx` constant that matches the HATEOAS policy reference.
- **Error Metadata**: Explicitly include `[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]` (and other error codes) for all public endpoints.

## Must Read
- [docs/QUICK_REFERENCE.md#controller-authoring-standard-forward-policy](../../docs/QUICK_REFERENCE.md#controller-authoring-standard-forward-policy)
- [docs/API.md](../../docs/API.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.API.IntegrationTests`, `Event.Architecture.Tests`

## Related
- Intents: `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`
- Agents: `architect-agent.md`, `presentation-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `application-layer.md`, `api-hateoas.md`

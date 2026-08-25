---
name: api-controllers
description: Apply when editing Explore.API controller actions and route contracts.
paths:
  - "src/Explore.API/Controllers/**/*.cs"
related_skills: [cqrs-mediatr-guidelines, auth-patterns]
related_docs: [docs/API.md, docs/GOVERNANCE.md, docs/AUTHORIZATION.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.API.IntegrationTests, Event.Architecture.Tests]
related_intents: [add-get-endpoint, add-write-endpoint, openapi-contract-change, add-hal-link]
---

# API Controller Rules

## Applies To
- `src/Explore.API/Controllers/**/*.cs`

## Path-Specific Constraints
- **Logic Thinning**: Controller actions must be limited to: dispatching MediatR, assembling HAL links (via policies), and returning the `ActionResult`. No business logic. A rule that validates or normalizes *domain* input belongs in the command handler, so every caller — HTTP, MCP, internal — is held to it.
- **Contract Integrity**: Every action MUST use a `RouteNames.Xxx` constant that matches the HATEOAS policy reference. The route name is what pins the generated `operationId`, and therefore the generated client method — never rename or drop one while moving an action.
- **Error Metadata**: Explicitly include `[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]` (and other error codes) for all public endpoints. Error bodies are always ProblemDetails; never declare `BaseCommandResponse<T>` for a 4xx/5xx status.
- **No Service Location**: `HttpContext.RequestServices` is banned here. Take a constructor dependency, or read the request principal directly.
- **Identity**: use `CurrentUserId` / `RequiredUserId` from `ExploreControllerBase`, which project `PlatformIdentityPrincipalExtensions` over `ControllerBase.User`. When the provider subject is not itself a platform user id (ATProto DID, Google subject), use `mediator.ResolveCurrentUserIdAsync(User, cancellationToken)`. Do not parse claims in a controller; purpose-bound schemes (API key, setup secret, managed control plane, ATProto session, erasure receipt) are the only exceptions and are allowlisted by name.
- **Tenant Authority**: current tenant/user facts come from `ITenantContext`, the established principal helpers, an authoritative route/persisted resource, or a trusted adapter; never accept them from a body. Body IDs may name legitimate targets only when the server independently authorizes and tenant-checks that target. Removed authority members get no alias or compatibility reader.
- **Failure Mapping**: declare a `CommandFailurePolicy` per capability (`ValidatedBy(...).NotFound(...).Conflict(...)`) and call `Policy.Map(this, response)`. Policies are immutable and compose, so a variant is the base policy plus one rule. Do not write a private `switch` over `FailureCode`.
- **Capability Partition**: keep a controller to one route capability. When it accumulates several, split it — preserving every route template, verb, `Name = RouteNames.*`, authorization attribute, and endpoint classification — and lift genuinely shared behavior into an explicit family base class (see `InstanceSettingsControllerBase`, `WebhooksControllerBase`, `RegistrationOrderControllerBase`).

## Must Read
- [docs/QUICK_REFERENCE.md#controller-authoring-standard-forward-policy](../../docs/QUICK_REFERENCE.md#controller-authoring-standard-forward-policy)
- [docs/API.md](../../docs/API.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.API.IntegrationTests`, `Event.Architecture.Tests`
- `ApiLiabilityRatchetTests` holds service location, controller claim parsing, private failure switches, and controller size to exact allowlists. The allowlists may only shrink: adding an occurrence fails, and removing one without deleting its entry also fails.
- A controller split must leave `schemas/openapi_islamu-event.json` semantically unchanged apart from `tags`. Verify by diffing operationIds and the `components` section, then regenerating the NSwag client and confirming no method name moved.

## Related
- Intents: `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`
- Agents: `architect-agent.md`, `presentation-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `application-layer.md`, `api-hateoas.md`

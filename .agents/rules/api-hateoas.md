---
name: api-hateoas
description: Apply when editing Explore.API HAL assemblers, link policies, or related HATEOAS infrastructure.
paths:
  - "src/Explore.API/Hateoas/**/*.cs"
related_skills: [clean-architecture-rules, auth-patterns]
related_docs: [docs/API.md, docs/AUTHORIZATION.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.API.IntegrationTests, Explore.Blazor.Client.Tests, Event.Architecture.Tests]
related_intents: [add-hal-link, add-get-endpoint, add-write-endpoint, blazor-component-affordance]
---

# API HATEOAS Rules

## Applies To
- `src/Explore.API/Hateoas/**/*.cs`

## Path-Specific Constraints
- **Yield Return Pattern**: Always use the `yield return` pattern for link emission. Do not build mutable lists.
- **Policy Separation**: Maintain strictly separate policies for Resource-Detail and Resource-Collection (do not reuse).
- **Capability Pipeline**: Feed candidate links through the documented 4-phase capability pipeline (Auth → Resource State → Module Toggle → Visibility).
- **Fail Closed**: If route resolution or permission check is uncertain, omit the link entirely.
- **Registration**: wire a family with `AddHalResource<TDetail, TList, TDetailPolicy, TCollectionPolicy>()` (or the same-type arity). It supplies the default `HalResourceAssembler<TDto, TListDto>`. Use `AddHalResourceWithAssembler<...>` **only** when the family genuinely assembles differently; an assembler subclass whose entire body forwards constructor arguments adds a type without adding behavior and must not be created. `AddHalResourceWithSharedPolicy<...>` is for policies also resolved by concrete type elsewhere, so both paths share one per-request instance.
- **No Reflection**: registration stays compile-time and explicit. Do not scan assemblies or infer registrations from naming — a missing policy must be a build error, and every closed contract must stay greppable by its concrete type.
- **Typed Facts Over Attribute Bags**: when a `ResourceDescriptor` publishes `IAuthorizationFacts`, `RequirePermission` deliberately omits the stringly-typed `PermissionResourceAttributes`, and `HateoasAuthorizationEvaluator` applies the same precedence. Assert and reason against the typed facts record (`EventAuthorizationFacts` and siblings), not the attribute dictionary.

## Must Read
- [docs/QUICK_REFERENCE.md#critical-rules](../../docs/QUICK_REFERENCE.md#critical-rules) (Rules #16, #20, #21)
- [docs/API.md](../../docs/API.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests`
- `HateoasRegistrationGraphTests` pins lifetime uniformity, duplicate-free registration, and assembler-to-detail-policy pairing, and prints the full descriptor inventory. When changing registration **style**, capture that inventory before and after and diff it — a dropped policy silently removes an affordance, and because affordances are the client's authorization signal that is a security regression, not a cosmetic one.
- Public HAL detail wrappers must be listed in `Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`. An omitted `HalResourceOf*Dto` emits an **empty** wrapper schema and generated clients silently lose the DTO's fields; nested DTOs also need registering or they inline as anonymous shapes.

## Related
- Intents: `add-hal-link`, `add-get-endpoint`, `add-write-endpoint`, `blazor-component-affordance`
- Agents: `presentation-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `api-controllers.md`, `blazor-client.md`

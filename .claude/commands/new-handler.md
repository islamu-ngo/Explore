---
name: new-handler
description: Scaffold a new MediatR command or query handler (request + handler + validator + DTO + tests) following project CQRS conventions.
type: scaffold
priority: high
---
<!-- ABOUTME: Scaffolding guidance for a new MediatR command or query handler. -->
<!-- ABOUTME: Encodes file layout, naming, validator instantiation, and HATEOAS wiring per CQRS invariants. -->

# /new-handler — Scaffold a CQRS Handler

> **Primary references:**
> - [`.claude/rules/application-layer.md`](../rules/application-layer.md)
> - [`.claude/skills/cqrs-mediatr-guidelines/SKILL.md`](../skills/cqrs-mediatr-guidelines/SKILL.md)
> - [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md)
> - [`docs/QUICK_REFERENCE.md`](../../docs/QUICK_REFERENCE.md)

## Step 1 — Classify the Handler

| Question | Command | Query |
|---|---|---|
| Mutates state? | ✔ | ✘ |
| Returns data? | May return an ID or minimal result | Always returns a DTO |
| Auth default | `[Authorize]` | `[AllowAnonymous]` by default |
| Response type | `BaseCommandResponse<Guid>` (or `bool` for some deletes) | DTO or `PaginatedResult<TDto>` |
| Cache interaction | `RemoveAsync()` | `GetOrCreateAsync()` |

If the change mixes read and write, split into two handlers.

## Step 2 — Files to Create

All paths are relative to `Explore.Application/Features/{FeatureArea}/`:

### Command

```
Commands/
  Create{Resource}/
    Create{Resource}Command.cs            # IRequest<BaseCommandResponse<Guid>>
    Create{Resource}CommandHandler.cs     # : IRequestHandler<...>
    Create{Resource}CommandValidator.cs   # : AbstractValidator<...> — manual ctor call
    Create{Resource}Dto.cs                # input DTO (wrapped by Command)
```

### Query

```
Queries/
  Get{Resource}/
    Get{Resource}Request.cs               # IRequest<{Resource}Dto> (or PaginatedResult<...>)
    Get{Resource}RequestHandler.cs        # : IRequestHandler<...>
    Get{Resource}Dto.cs                   # projection
```

### Controller wiring (in `Explore.API/Controllers/{Resource}Controller.cs`)

```
[HttpGet("{id:guid}", Name = RouteNames.{Resource}Get)]
[AllowAnonymous]
[EndpointClassification(EndpointClass.Public)]
[ProducesResponseType<{Resource}Dto>(StatusCodes.Status200OK)]
public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    => this.AsActionResult(await mediator.Send(new Get{Resource}Request(id), ct));
```

## Step 3 — Handler Rules (from CQRS skill)

1. Repository returns **entity**. Handler maps to DTO (AutoMapper profile in `Explore.Application`).
2. Validator is **manually instantiated** in the handler: `new Create{Resource}CommandValidator().ValidateAsync(request, ct)`.
3. Pass `CancellationToken` through every async call.
4. Atomic writes go through `IUnitOfWork.ExecuteInTransactionAsync`.
5. Validation failures populate `BaseCommandResponse.Errors`, do not throw.
6. For Queries: use `AsNoTracking()` via repository / Specification Pattern.
7. For cacheable queries: wrap in `HybridCache.GetOrCreateAsync(key, factory, ct)`.
8. For commands that invalidate cache: call `HybridCache.RemoveAsync(key, ct)` after commit.

## Step 4 — Tests

Required minimum:

- Unit test in `Event.Application.UnitTests/Features/{FeatureArea}/` covering:
  - Happy path.
  - Validation failure population.
  - CancellationToken propagation (where meaningful).
- Integration test in `Event.API.IntegrationTests/Controllers/{Resource}ControllerTests.cs` covering:
  - HTTP status codes (200/201/400/401/403/404).
  - HAL link presence (if applicable — see [`add-hal-link`](../contract/intents.yaml) intent).

## Step 5 — HATEOAS (for write endpoints producing visible resources)

If the resource is exposed with HAL links, add / update:

- `Explore.API/Hateoas/Policies/{Resource}LinkPolicy.cs` — separate policies for detail and collection.
- Use `yield return` in policies. Do not inline conditions in the controller.

See [`.claude/rules/api-hateoas.md`](../rules/api-hateoas.md).

## Step 6 — Authorization

- Write endpoints: apply `[Authorize]` and implement `IAuthorizedRequest` OR decorate the request with `[AuthorizeResource]`.
- For dynamic resource context, use `ISecureRequest`.
- Denied → `AuthorizationException` → 403 via chained `IExceptionHandler`.

See [`.claude/skills/auth-patterns/SKILL.md`](../skills/auth-patterns/SKILL.md).

## Anti-Patterns (fail fast if you see these)

- ❌ Validator injected via constructor.
- ❌ `DbContext` directly in the handler (use repository).
- ❌ Returning a domain entity from the handler.
- ❌ Mixing command and query logic in one handler.
- ❌ Skipping `CancellationToken`.
- ❌ Missing `BaseCommandResponse<>` on writes.
- ❌ Duplicate route in `@page` (Blazor) and controller without reconciliation.

## Verification

After scaffolding, run:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## Related

- [`/bootstrap`](bootstrap.md)
- [`/check`](check.md)
- [`/review-pr`](review-pr.md)
- Intent: [`add-cqrs-handler`](../contract/intents.yaml)
- Intent: [`add-write-endpoint`](../contract/intents.yaml)
- Intent: [`add-get-endpoint`](../contract/intents.yaml)

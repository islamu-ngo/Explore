# API Endpoint Design for CQRS

Use this guide when wiring HTTP endpoints to MediatR commands/queries.

## Core Rules

- Keep controllers transport-only: parse HTTP input, send request to MediatR, map result to HTTP status code.
- Keep handlers application-only: business logic, validation, orchestration, repository usage.
- Never return domain entities from API endpoints. Return DTOs or response envelopes.

## Route and Method Conventions

- Collection read: `GET /api/v1/{entities}` -> query request -> paginated DTO result.
- Item read: `GET /api/v1/{entities}/{id}` -> query request -> item DTO or `404`.
- Create: `POST /api/v1/{entities}` -> command -> `BaseCommandResponse<Guid>` and `201 Created`.
- Update: `PUT /api/v1/{entities}/{id}` -> command -> `BaseCommandResponse<Guid>` and `200 OK`.
- Delete: `DELETE /api/v1/{entities}/{id}` -> command -> `bool` and `204`/`404`.

Use route constraints for clarity (`{id:guid}`) where applicable.

## HTTP Status Mapping

- Successful list/detail query: `200 OK`
- Successful create: `201 Created` with `Location` header
- Successful update: `200 OK`
- Successful delete: `204 NoContent`
- Validation failures: `400 BadRequest`
- Unauthenticated: `401 Unauthorized`
- Unauthorized: `403 Forbidden`
- Missing resource: `404 NotFound`

## Controller Pattern

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<BaseCommandResponse<Guid>>> Create(
    [FromBody] CreateEntityDto dto,
    CancellationToken cancellationToken)
{
    var response = await _mediator.Send(new CreateEntityCommand { EntityDto = dto }, cancellationToken);

    if (!response.Success)
        return BadRequest(response);

    return CreatedAtRoute(
        RouteNames.GetEntityById,
        new { id = response.Id },
        response);
}
```

## Handler Pattern Boundary

- Handler receives typed request and `CancellationToken`.
- Handler manually instantiates validator (if required by project pattern).
- Handler maps DTO -> Entity, saves through repository, maps result to response envelope.
- Handler does not set HTTP status codes.

## Pagination Pattern

- Query contracts should include at minimum `PageNumber` and `PageSize`.
- Return `PaginatedResult<TDto>` from query handlers.
- Controller adds any representation concerns (for example HATEOAS wrapping).

## Security Placement

- Endpoint gate in controller with `[AllowAnonymous]`/`[Authorize]`/`[Authorize(Roles = "...")]`.
- Resource ownership and business permission checks in handlers or authorization behaviors.

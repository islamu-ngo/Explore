ABOUTME: API conventions and non-obvious runtime behavior for the current codebase.
ABOUTME: Keeps examples minimal and focuses on contracts, auth, tenancy, caching, and HAL behavior.

# API Architecture

## Scope
This document describes practical API behavior in `Explore.API`: endpoint conventions, response contracts, auth model, tenancy resolution, and client-generation flow.

## Runtime Endpoints
### Development
- API: `https://localhost:7039`
- Swagger UI: `https://localhost:7039/swagger`
- Scalar: `https://localhost:7039/scalar/v1`
- OpenAPI document: `https://localhost:7039/openapi/event-api.json`

### Docker Compose
- API: `http://localhost:7039`

## API Versioning
1. Controllers are currently annotated with API version `0.1`.
2. Versioning is media-type based (`Accept` parameter `v`), not URL-segment based.
3. If unspecified, default API version is `0.1`.

## Controller Conventions
1. Controllers are thin: receive request, dispatch MediatR request, map to HTTP response.
2. Business logic belongs in handlers/services, not controllers.
3. Endpoints should include summary/description and response metadata for OpenAPI quality.

## Auth And Authorization
1. JWT bearer auth is configured against Keycloak metadata/authority.
2. Common endpoint pattern:
   - `GET`: usually `[AllowAnonymous]`
   - `POST/PUT/DELETE`: `[Authorize]`
   - privileged operations: role/policy constrained
3. User ID extraction fallback order: `sub` -> `nameidentifier` -> `sid`.

## Response Contracts
1. Create/update flows usually return `BaseCommandResponse<TId>`.
2. Many delete flows return `bool` and map to `204 NoContent` or `404 NotFound`.
3. Query flows return DTOs or paginated DTO wrappers.

## HAL / HATEOAS Behavior
1. HAL wrappers are used for discoverable responses.
2. Link generation is policy-aware (authorization can remove links).
3. `Prefer: return=minimal` can be used to suppress link-heavy payloads for lightweight clients.
4. Event API supports both `application/json` and `application/hal+json` media types.

## Pagination And Filters
1. Standard pagination query params: `pageNumber`, `pageSize`.
2. Event listing supports broad filter families:
   - core filters (search/date/status/type/format)
   - lookup filters (audience/madhab/language)
   - relationship filters (categories/tags/locations/sessions)
   - aspect filters (Islamic/Tech) where module settings allow them
3. Sort behavior is configurable by query parameters.
4. Event list supports JSONB metadata filters (`metadataJsonContains`, `metadataJsonKeyExists`).
5. Aspect filters are ignored when related modules are disabled for the current tenant.

## Key Endpoint Groups
1. Core events:
   - `GET /api/event`
   - `GET /api/event/{id}`
   - `POST /api/event`
   - `POST /api/event/with-sessions`
2. Aspect endpoints:
   - `.../aspects/islamic` (`GET/PUT/DELETE`)
   - `.../aspects/tech` (`GET/PUT/DELETE`)
3. Module governance:
   - `/api/module/*` (`available`, `enabled`, `enable`, `disable`, `schema`)
4. Public experience:
   - `GET /api/publicexperience/settings`

## Error Handling
1. Unhandled exceptions are normalized through API exception handling middleware.
2. Expected validation/business failures return structured command responses.
3. Standard status patterns include `200`, `204`, `400`, `401`, `403`, `404`, and `500`.
4. Timeout middleware can emit `504 Gateway Timeout`.
5. Rate-limiter middleware can emit `429 Too Many Requests`.

## Caching
1. Output caching policies are configured in API startup:
   - `LookupData` (longer-lived lookup responses)
   - `ListData` (short-lived list responses)
   - `DetailData` (short-lived detail responses)
2. Hybrid cache is configured for application-level caching scenarios.

## Multi-Tenancy In API
1. Tenant context is resolved per request.
2. Resolution behavior:
   - `SingleTenant`: default tenant
   - `MultiTenant`: `X-Tenant-Id` -> custom domain -> subdomain -> default tenant
3. EF query filters enforce tenant scoping in persistence.

## OpenAPI Export And Client Generation
1. In Development, API startup exports OpenAPI to `Explore.API/swagger.json`.
2. Blazor client build uses this file as NSwag input and regenerates `Clients/EventApiClient.g.cs` before compile.
3. DTO changes should follow API-first regeneration workflow (see `docs/CONTRIBUTING.md`).

## Related Docs
- `docs/SECURITY.md`
- `docs/MULTI_TENANCY.md`
- `docs/CONTRIBUTING.md`
- `docs/TROUBLESHOOTING.md`

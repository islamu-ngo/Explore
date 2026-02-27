ABOUTME: System architecture summary for the current codebase, not a theoretical template.
ABOUTME: Captures key runtime patterns and boundaries that are not obvious from one file.

# Technical Architecture

## System Profile
- Style: Clean Architecture + CQRS + BFF.
- Runtime: .NET 10 (`net10.0`, preview SDK pinned in `global.json`).
- API host: `Explore.API`.
- BFF host: `Explore.Blazor`.
- Interactive UI client: `Explore.Blazor.Client`.
- Data: PostgreSQL via EF Core.

## Layer Boundaries
1. `Explore.Domain`: entities, enums, domain rules, no infrastructure concerns.
2. `Explore.Application`: requests/handlers, DTOs, validators, contracts.
3. `Explore.Persistence` + `Explore.Infrastructure`: data + external service implementations.
4. `Explore.API` and `Explore.Blazor`: presentation and composition roots.

Dependency direction is inward: presentation -> application -> domain.

## Request Flow
1. HTTP request hits API controller.
2. Controller forwards command/query through MediatR.
3. Handler orchestrates validation, authorization behavior, repository calls, mapping.
4. Persistence layer returns entities; handlers map to DTO/response contracts.

## BFF Model (Blazor)
1. Browser authenticates via OIDC through BFF endpoints.
2. Session/cookie state remains in server-controlled flow.
3. BFF forwards API calls to backend (token forwarding + tenant header propagation where needed).
4. `Explore.Blazor.Client` focuses on UI and typed service calls; it is not a token authority.

## Multi-Tenancy Model
1. Runtime mode is resolved from governance settings (`SingleTenant` / `MultiTenant`).
2. In `SingleTenant`, default tenant is used for all requests.
3. In `MultiTenant`, tenant is resolved from header/domain/subdomain fallback chain.
4. EF query filters enforce tenant isolation centrally in `ExploreDbContext`.

## Authorization Architecture
1. Endpoint-level auth is handled via ASP.NET attributes/policies.
2. Resource-level auth is handled in application pipeline behaviors.
3. Runtime authorization provider can route checks to Cerbos PDP or local authorization logic depending on settings.
4. BYO Cerbos is supported per tenant through configuration resolver logic.

## API Representation
1. HAL/HATEOAS wrappers are used for discoverable responses.
2. `Prefer: return=minimal` can reduce link payload where clients do not need hypermedia.
3. OpenAPI is exposed and exported in development for client generation.

## Federation Status
Implemented today:
- ATProto-related entities and API resources (e.g., indexed DID and ATProto records).
- Outbox-based PDS sync background worker.

Not fully implemented today:
- Complete ActivityPub gateway endpoint surface.
- Full federation protocol exposure expected by third-party federated servers.

## Local Runtime Endpoints
- API dev: `https://localhost:7039`
- Blazor dev: `https://localhost:7177`
- Docker API: `http://localhost:7039`
- Docker Blazor: `http://localhost:7002`

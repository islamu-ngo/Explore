# API Architecture

> **Project-Agnostic REST API Design Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md).

**Last Updated**: January 2026

---

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.API` | API project | `Explore.API` |
| `{Project}.Blazor` | Blazor Server project | `Explore.Blazor` |
| `{DbContext}` | EF Core DbContext class | `ExploreDbContext` |
| `{Entity}` | Your main entity (singular) | `Event` |
| `{Entities}` | Your main entity (plural) | `Events` |
| `{entity}` | camelCase entity | `event` |
| `{IdType}` | Primary key type | `Guid` |

---

## Table of Contents

1. [Overview](#1-overview)
2. [Base URL & Versioning](#2-base-url--versioning)
3. [Controller Conventions](#3-controller-conventions)
4. [Authentication & Authorization](#4-authentication--authorization)
5. [Request/Response Patterns](#5-requestresponse-patterns)
6. [Pagination](#6-pagination)
7. [Error Handling](#7-error-handling)
8. [OpenAPI Documentation](#8-openapi-documentation)
9. [Endpoint Reference](#9-endpoint-reference)
10. [Multi-Tenancy](#10-multi-tenancy)

---

## 1. Overview

The `{Project}.API` is a stateless REST API built on ASP.NET Core, following Clean Architecture and CQRS patterns.

### Implementation Example: ISLAMU Event

- **API Project**: `Explore.API`
- **Technology**: ASP.NET Core 10.0
- **Architecture**: Clean Architecture + CQRS + MediatR

### Key Characteristics

| Aspect | Implementation |
|--------|----------------|
| Framework | ASP.NET Core 10.0 |
| Architecture | Clean Architecture + CQRS |
| Authentication | JWT Bearer (Keycloak) |
| Documentation | OpenAPI 3.0 (Scalar + Swagger) |
| Versioning | URL path (`/api/`) |
| Serialization | System.Text.Json |

### Request Flow

```
HTTP Request → Controller → MediatR → Handler → Repository → Entity → DTO → Response
```

---

## 2. Base URL & Versioning

### Development URLs

| Service | URL |
|---------|-----|
| API | `https://localhost:7001` |
| Scalar Docs | `https://localhost:7001/scalar/v1` |
| Swagger UI | `https://localhost:7001/swagger` |
| OpenAPI JSON | `https://localhost:7001/openapi/v1.json` |

### URL Structure

**Generic Pattern:**
```
/api/{resource}
/api/{resource}/{id}
/api/{resource}/{id}/{subresource}
```

**Generic Examples:**
- `GET /api/{entity}` - List entities
- `GET /api/{entity}/{id}` - Get entity details
- `GET /api/{childEntity}/by-{parentEntity}/{parentId}` - Get children
- `POST /api/{entity}` - Create entity

### Implementation Examples: ISLAMU Event
- `GET /api/event` - List events
- `GET /api/event/{id}` - Get event details
- `GET /api/eventsession/by-event/{eventId}` - Get sessions for event
- `POST /api/organization` - Create organization

---

## 3. Controller Conventions

### Standard Controller Structure

**Generic Template:**

```csharp
[Route("api/[controller]")]
[ApiController]
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<{Entity}Controller> _logger;

    public {Entity}Controller(
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<{Entity}Controller> logger)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
}
```

### Implementation Example: ISLAMU Event

```csharp
[Route("api/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EventController> _logger;

    public EventController(
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EventController> logger)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
}
```

### CRUD Operations

| Operation | HTTP Method | Route | Auth | Returns |
|-----------|-------------|-------|------|---------|
| List | `GET` | `/` | `[AllowAnonymous]` | `PaginatedResult<{Entity}ListDto>` |
| Get | `GET` | `/{id}` | `[AllowAnonymous]` | `{Entity}Dto` |
| Create | `POST` | `/` | `[Authorize]` | `BaseCommandResponse<{IdType}>` |
| Update | `PUT` | `/{id}` | `[Authorize]` | `BaseCommandResponse<{IdType}>` |
| Delete | `DELETE` | `/{id}` | `[Authorize]` | `NoContent` or `NotFound` |

### Thin Controllers

Controllers should be thin - they only:
1. Extract data from HTTP request
2. Send command/query to MediatR
3. Return appropriate HTTP response

**Generic Template:**

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create([FromBody] Create{Entity}Dto dto)
{
    var command = new Create{Entity}Command { {Entity}Dto = dto };
    var response = await _mediator.Send(command);

    if (!response.Success)
    {
        return BadRequest(response);
    }

    return Ok(response);
}
```

### Implementation Example: ISLAMU Event

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
{
    var command = new CreateEventCommand { EventDto = dto };
    var response = await _mediator.Send(command);

    if (!response.Success)
    {
        return BadRequest(response);
    }

    return Ok(response);
}
```

---

## 4. Authentication & Authorization

### JWT Bearer Authentication

The API uses JWT Bearer tokens issued by Keycloak:

```csharp
// In Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false; // Dev only
    });
```

### Authorization Patterns

**Read Operations** - Public access:
```csharp
[HttpGet]
[AllowAnonymous]
[EndpointSummary("Get all {Entities}")]
public async Task<ActionResult<PaginatedResult<{Entity}ListDto>>> GetAll(...)
```

**Write Operations** - Authenticated users:
```csharp
[HttpPost]
[Authorize]
[EndpointSummary("Create a new {Entity}")]
public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create(...)
```

### User ID Extraction

Use the fallback pattern for extracting user ID from JWT claims:

```csharp
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(new { error = "User ID not found in token" });
}
```

---

## 5. Request/Response Patterns

### HATEOAS / HAL+JSON Implementation

The API implements **HATEOAS (Hypermedia as the Engine of Application State)** using the HAL+JSON format. This allows clients to discover available actions dynamically based on their permissions and the resource state.

#### Core Components

1.  **`IResourceAssembler<TDto, TListDto>`**:
    The central service that converts DTOs into `HalResource<T>` or `HalCollectionResource<T>`.
    - Automatically handles `Prefer: return=minimal` header to strip links.
    - Evaluates authorization policies before adding links.
    - Uses `ResourceAssemblerBase` for shared logic.

2.  **`ILinkPolicy<T>`**:
    Defines *which* links apply to a specific resource.
    - `GetLinks(dto, user)`: Returns links for a single resource.
    - `GetCollectionLinks(user)`: Returns links for the collection root (e.g., "create").

3.  **`IHateoasAuthorizationEvaluator`**:
    Integrates with **Cerbos** to filter links. If a user lacks permission for an action (e.g., `update`), the corresponding link is automatically omitted from the response.

#### Defining Links

Links are defined using a fluent API in Policy classes:

```csharp
public class EventLinkPolicy : ILinkPolicy<EventDto>
{
    public IEnumerable<LinkDefinition> GetLinks(EventDto dto, ClaimsPrincipal user)
    {
        // Self link (always visible)
        yield return new LinkDefinition(
            rel: LinkRelations.Self,
            routeName: RouteNames.GetEvent,
            routeValues: new { id = dto.Id });

        // Update link (only if user has permission)
        yield return new LinkDefinition(
            rel: LinkRelations.Update,
            routeName: RouteNames.UpdateEvent,
            routeValues: new { id = dto.Id })
            .RequirePermission(CerbosPermissionAction.Update, dto); // Checks Cerbos policy
    }
}
```

#### Response Wrapper Types

**For Single Resources:**
```csharp
public class HalResource<T>
{
    public T Data { get; set; }  // The actual DTO payload
    public Dictionary<string, HalLink> _links { get; set; }  // Hypermedia links
}
```

**For Collections:**
```csharp
public class HalCollectionResource<T>
{
    public Dictionary<string, object> _embedded { get; set; }  // Contains "items" array
    public Dictionary<string, HalLink> _links { get; set; }   // Collection navigation links
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

#### Example Response

**Request:**
```http
GET /api/events/123e4567-e89b-12d3-a456-426614174000
Accept: application/hal+json
```

**Response:**
```json
{
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "title": "Community Iftar 2026",
    "status": "Published"
  },
  "_links": {
    "self": {
      "href": "/api/events/123e4567-e89b-12d3-a456-426614174000",
      "method": "GET"
    },
    "update": {
      "href": "/api/events/123e4567-e89b-12d3-a456-426614174000",
      "method": "PUT"
    }
  }
}
```

#### Optimization: Minimal Response

Clients can request payloads without hypermedia overhead using the standard `Prefer` header. This is useful for mobile apps or internal services that don't need discovery.

**Request:**
```http
GET /api/events/123...
Prefer: return=minimal
```

**Response (Pure JSON):**
```json
{
  "data": {
    "id": "123...",
    "title": "Community Iftar 2026"
  }
}
```

### Command Response


All write operations return `BaseCommandResponse<T>`:

```csharp
public class BaseCommandResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Id { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

**Success Response**:
```json
{
    "success": true,
    "message": "{Entity} created successfully.",
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "errors": []
}
```

**Failure Response**:
```json
{
    "success": false,
    "message": "{Entity} creation failed.",
    "id": null,
    "errors": [
        "Title is required",
        "{RelatedEntity}Id not found"
    ]
}
```

### Implementation Example: ISLAMU Event

**Success Response**:
```json
{
    "success": true,
    "message": "Event created successfully.",
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "errors": []
}
```

**Failure Response**:
```json
{
    "success": false,
    "message": "Event creation failed.",
    "id": null,
    "errors": [
        "Title is required",
        "EventTypeId not found"
    ]
}
```

### Query Response

Read operations return DTOs directly or wrapped in `PaginatedResult<T>`:

**Generic Pattern:**

```csharp
// Single entity
public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id)

// List with pagination
public async Task<ActionResult<PaginatedResult<{Entity}ListDto>>> GetAll(...)
```

### HATEOAS / HAL+JSON Response Format

All API responses are wrapped in **HAL (Hypertext Application Language)** format, providing hypermedia links for discoverability and navigation.

#### Response Wrapper Types

**For Single Resources:**
```csharp
public class HalResource<T>
{
    public T Data { get; set; }  // The actual DTO payload
    public Dictionary<string, HalLink> _links { get; set; }  // Hypermedia links
}

public class HalLink
{
    public string Href { get; set; }       // URL
    public string? Method { get; set; }    // HTTP method (GET, POST, etc.)
    public string? Rel { get; set; }       // Link relation type
}
```

**For Collections:**
```csharp
public class HalCollectionResource<T>
{
    public Dictionary<string, object> _embedded { get; set; }  // Contains "items" array
    public Dictionary<string, HalLink> _links { get; set; }   // Collection navigation links
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

#### Example Single Resource Response

**Request:**
```http
GET /api/events/123e4567-e89b-12d3-a456-426614174000
Accept: application/hal+json
```

**Response:**
```json
{
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "title": "Community Iftar 2026",
    "startDate": "2026-03-15T18:00:00Z",
    "location": "Amsterdam Mosque",
    "status": "Published"
  },
  "_links": {
    "self": {
      "href": "/api/events/123e4567-e89b-12d3-a456-426614174000",
      "method": "GET"
    },
    "update": {
      "href": "/api/events/123e4567-e89b-12d3-a456-426614174000",
      "method": "PUT"
    },
    "delete": {
      "href": "/api/events/123e4567-e89b-12d3-a456-426614174000",
      "method": "DELETE"
    },
    "sessions": {
      "href": "/api/events/123e4567-e89b-12d3-a456-426614174000/sessions",
      "method": "GET"
    }
  }
}
```

#### Example Collection Response

**Request:**
```http
GET /api/events?pageNumber=1&pageSize=10
Accept: application/hal+json
```

**Response:**
```json
{
  "_embedded": {
    "items": [
      {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "title": "Community Iftar 2026",
        "startDate": "2026-03-15T18:00:00Z"
      },
      {
        "id": "987e6543-e21c-45d6-b789-123456789abc",
        "title": "Tech Workshop: Clean Architecture",
        "startDate": "2026-04-01T14:00:00Z"
      }
    ]
  },
  "_links": {
    "self": {
      "href": "/api/events?pageNumber=1&pageSize=10",
      "method": "GET"
    },
    "first": {
      "href": "/api/events?pageNumber=1&pageSize=10",
      "method": "GET"
    },
    "next": {
      "href": "/api/events?pageNumber=2&pageSize=10",
      "method": "GET"
    },
    "last": {
      "href": "/api/events?pageNumber=5&pageSize=10",
      "method": "GET"
    }
  },
  "totalCount": 47,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

#### Prefer Header Support

Clients can request minimal responses without hypermedia links using the `Prefer` header:

**Request:**
```http
GET /api/events/123e4567-e89b-12d3-a456-426614174000
Prefer: return=minimal
```

**Response (without _links):**
```json
{
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "title": "Community Iftar 2026",
    "startDate": "2026-03-15T18:00:00Z",
    "location": "Amsterdam Mosque",
    "status": "Published"
  }
}
```

#### Resource Assembler Pattern

Controllers use `IResourceAssembler<TDto, TListDto>` to build HAL responses:

**Generic Pattern:**
```csharp
public class {Entity}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<{Entity}Dto, {Entity}ListDto> _resourceAssembler;

    [HttpGet("{id}")]
    public async Task<ActionResult<HalResource<{Entity}Dto>>> GetById({IdType} id)
    {
        var dto = await _mediator.Send(new Get{Entity}DetailsRequest { Id = id });
        if (dto == null) return NotFound();

        var resource = _resourceAssembler.ToResource(dto, RouteNames.Get{Entity});
        return Ok(resource);
    }

    [HttpGet]
    public async Task<ActionResult<HalCollectionResource<{Entity}ListDto>>> GetAll(...)
    {
        var dtos = await _mediator.Send(new Get{Entity}ListRequest { ... });
        var resource = _resourceAssembler.ToCollectionResource(
            dtos,
            RouteNames.Get{Entities},
            totalCount,
            pageNumber,
            pageSize);
        return Ok(resource);
    }
}
```

**RouteNames Constants:**
Route names are defined in `RouteNames.cs` for consistency:
```csharp
public static class RouteNames
{
    public const string GetEvent = "GetEvent";
    public const string GetEvents = "GetEvents";
    public const string UpdateEvent = "UpdateEvent";
    public const string DeleteEvent = "DeleteEvent";
    // ...
}
```

#### Benefits

- **Discoverability**: Clients can navigate the API without hardcoding URLs
- **Evolvability**: Server can change URLs without breaking clients
- **Self-Documenting**: Links show available actions based on current state
- **Bandwidth Control**: Use `Prefer: return=minimal` to reduce payload size

**See Also**: [CODEBASE_INSIGHTS.md](CODEBASE_INSIGHTS.md) Section 14 for HATEOAS implementation details

---

## 6. Pagination

### Paginated Response Structure

```csharp
public class PaginatedResult<T>
{
    public ICollection<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

### Query Parameters

**Generic Pattern:**
```
GET /api/{entity}?pageNumber=1&pageSize=20
```

**Example (ISLAMU Event):**
```
GET /api/event?pageNumber=1&pageSize=20
```

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `pageNumber` | int | 1 | - | Page number (1-indexed) |
| `pageSize` | int | 20 | 100 | Items per page |

### Example Response

```json
{
    "items": [
        { "id": "...", "title": "Event 1", ... },
        { "id": "...", "title": "Event 2", ... }
    ],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 8,
    "hasPreviousPage": false,
    "hasNextPage": true
}
```

---

## 7. Error Handling

### Standardization

The API follows **RFC 7807 (Problem Details for HTTP APIs)** for all unhandled exceptions, while using structured `BaseCommandResponse` for domain logic failures.

### 7.1. Global Exception Handler

Any unhandled exception (System.Exception) is caught by the global `ExceptionHandler` middleware and converted into a standardized JSON response.

**Response Format (ProblemDetails):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Database connection timeout",
  "instance": "/api/events"
}
```

### 7.2. Domain Logic Errors (Command Response)

For expected business rule violations (e.g., validation failures, logic conflicts), endpoints return a `200 OK` or `400 Bad Request` with a structured `BaseCommandResponse`.

**Response Format:**
```json
{
    "success": false,
    "message": "Validation failed",
    "id": null,
    "errors": [
        "Event date cannot be in the past.",
        "Title is required."
    ]
}
```

### 7.3. HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| `200 OK` | Success | GET, PUT, POST success |
| `201 Created` | Resource created | POST (alternative) |
| `204 No Content` | Success, no body | DELETE success |
| `400 Bad Request` | Validation failed | Business rule violation |
| `401 Unauthorized` | Not authenticated | Missing/invalid token |
| `403 Forbidden` | Not authorized | Insufficient permissions (Cerbos denial) |
| `404 Not Found` | Resource not found | Invalid ID |
| `500 Internal Error` | Server error | Unexpected exception (Unhandled) |

### 7.4. Exception Handling Pattern in Controllers

Controllers should use `try-catch` blocks only when specific recovery or logging context is needed. Otherwise, let the global handler manage 500s.

**Recommended Pattern:**

```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<ActionResult> Delete(Guid id)
{
    try
    {
        // ... logic ...
    }
    catch (Exception ex)
    {
        // Only catch if you need to add context before re-throwing
        // or if you want to return a specific status code manually
        _logger.LogError(ex, "Error deleting event {EventId}", id);
        return StatusCode(500, new { error = "An unexpected error occurred." });
    }
}
```

---

## 8. OpenAPI Documentation

### Required Attributes

All controller actions must include:

**Generic Template:**

```csharp
[HttpGet("{id}")]
[EndpointSummary("Get {Entity} Details")]
[EndpointDescription("Returns detailed information about a specific {entity} including related data.")]
[AllowAnonymous]
[ProducesResponseType(typeof({Entity}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<{Entity}Dto>> GetById({IdType} id)
```

### Implementation Example: ISLAMU Event

```csharp
[HttpGet("{id}")]
[EndpointSummary("Get Event Details")]
[EndpointDescription("Returns detailed information about a specific event including sessions.")]
[AllowAnonymous]
[ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<EventDto>> GetById(Guid id)
```

### Attribute Guidelines

| Attribute | Required | Purpose |
|-----------|----------|---------|
| `[EndpointSummary]` | Yes | Short title in API docs |
| `[EndpointDescription]` | Yes | Detailed description |
| `[ProducesResponseType]` | Yes | Document response types |
| `[Consumes]` | For POST/PUT | Request content type |

### Example with Full Documentation

**Generic Template:**

```csharp
[HttpPost]
[EndpointSummary("Create a new {Entity}")]
[EndpointDescription("Creates a new {entity} under the specified {relatedEntity}. User must have appropriate permissions.")]
[Authorize]
[Consumes("application/json")]
[ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(BaseCommandResponse<{IdType}>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<BaseCommandResponse<{IdType}>>> Create([FromBody] Create{Entity}Dto dto)
```

### Implementation Example: ISLAMU Event

```csharp
[HttpPost]
[EndpointSummary("Create a new Event")]
[EndpointDescription("Creates a new event under the specified organization. User must be an admin of the organization.")]
[Authorize]
[Consumes("application/json")]
[ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
```

---

## 9. Caching Strategy

The API implements a **Dual-Layer Caching Strategy** to maximize performance while ensuring data consistency.

### 9.1. Layer 1: Output Caching (HTTP Level)

**Purpose**: Caches the entire HTTP response body.
**Use Case**: Public, read-only endpoints (lists, details) that don't vary per user.
**Mechanism**: ASP.NET Core Output Caching Middleware.

**Configuration**:
Policies are defined in `Program.cs`:
- `ListData`: Expire 5m, Vary by Query keys (page, size, search).
- `DetailData`: Expire 10m, Vary by Route Value (id).

**Usage in Controllers**:
```csharp
[HttpGet("{id}")]
[AllowAnonymous]
[OutputCache(PolicyName = "DetailData")]
public async Task<ActionResult> GetById(Guid id) { ... }
```

### 9.2. Layer 2: Hybrid Caching (Application Level)

**Purpose**: Caches domain entities or calculation results within the application logic.
**Use Case**:
- Data needed by multiple handlers.
- Data that is expensive to compute/fetch but used in authenticated endpoints (where OutputCache isn't suitable).
- Scenarios requiring **stampede protection**.
**Mechanism**: .NET 9+ `HybridCache` (L1 In-Memory + L2 Redis).

**Usage in Handlers (Query Side)**:
Use `GetOrCreateAsync` to fetch-or-cache:

```csharp
public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken ct)
{
    var cacheKey = $"event:{request.Id}";
    
    return await _hybridCache.GetOrCreateAsync(
        cacheKey,
        async cancel => await _repository.GetByIdAsync(request.Id, cancel),
        options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
        cancellationToken: ct
    );
}
```

**Usage in Handlers (Command Side)**:
Explicitly invalidate cache after updates:

```csharp
public async Task<BaseCommandResponse> Handle(UpdateEventCommand request, CancellationToken ct)
{
    // ... update logic ...
    
    // Invalidate the specific entity cache
    await _hybridCache.RemoveAsync($"event:{request.Id}", ct);
    
    return response;
}
```

### 9.3. Caching Guidelines

| Feature | Use **Output Cache** | Use **Hybrid Cache** |
| :--- | :---: | :---: |
| **Public Public Data** (e.g., Event List) | ✅ Yes | ❌ No |
| **Authenticated Data** (e.g., "My Events") | ❌ No | ✅ Yes |
| **Lookup Tables** (e.g., Categories) | ✅ Yes | ✅ Yes (if reused internally) |
| **User-Specific Content** | ❌ No | ✅ Yes (Keyed by UserID) |
| **Write Operations** | ❌ Never | ❌ Use to Invalidate |

---

## 10. Endpoint Reference

### Core Resources

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Events | `/api/event` | Event management |
| Event Sessions | `/api/eventsession` | Session management |
| Organizations | `/api/organization` | Organization management |
| Registrations | `/api/eventregistration` | Event registration |
| Locations | `/api/location` | Venue management |
| Categories | `/api/category` | Event categories |
| Tags | `/api/tag` | Event tags |

### Lookup Tables (Read-Only)

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Event Types | `/api/eventtype` | Conference, Webinar, etc. |
| Event Formats | `/api/eventformat` | In-person, Online, Hybrid |
| Event Statuses | `/api/eventstatus` | Draft, Published, etc. |
| Audience Ages | `/api/audienceage` | Children, Youth, Adults |
| Audience Genders | `/api/audiencegender` | Men, Women, Mixed |
| Madhabs | `/api/madhab` | Islamic jurisprudence schools |
| Languages | `/api/language` | Event languages |
| Registration Modes | `/api/registrationmode` | Registration options |

### User & Identity

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Users | `/api/user` | User management |
| Actors | `/api/actor` | Identity (user/org) actors |
| Organization Members | `/api/organizationmember` | Org membership |

### Storage

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Storage Objects | `/api/storageobject` | File/image storage |

---

## 11. Multi-Tenancy

### Tenant Header

All requests must include the tenant ID header:

```
X-Tenant-Id: 018e4e5c-7f00-7000-8000-000000000001
```

### Default Tenant

For single-instance deployments, use the default tenant:

```
018e4e5c-7f00-7000-8000-000000000001
```

This MUST match across:
- `{Project}.API/Services/TenantContext.cs`
- `{Project}.Blazor/Program.cs` (YARP transform)
- `{Project}.Blazor/Services/CircuitAccessTokenService.cs`
- `{Project}.Persistence/SeedIds.cs`

### Implementation Example: ISLAMU Event
- `Explore.API/Services/TenantContext.cs`
- `Explore.Blazor/Program.cs` (YARP transform)
- `Explore.Blazor/Services/CircuitAccessTokenService.cs`
- `Explore.Persistence/SeedIds.cs`

### Tenant Context Service

```csharp
public interface ITenantContext
{
    Guid TenantId { get; }
}

public class TenantContext : ITenantContext
{
    public const string DefaultTenantIdString = "018e4e5c-7f00-7000-8000-000000000001";
    public static readonly Guid DefaultTenantId = Guid.Parse(DefaultTenantIdString);

    public Guid TenantId { get; }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        TenantId = Guid.TryParse(header, out var tenantId) ? tenantId : DefaultTenantId;
    }
}
```

### Global Query Filters

EF Core automatically filters all queries by tenant:

**Generic Template:**

```csharp
// In {DbContext}
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply tenant filter to all entities with TenantId
    modelBuilder.Entity<{Entity}>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    // ... etc for all tenant-scoped entities
}
```

### Implementation Example: ISLAMU Event

```csharp
// In ExploreDbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply tenant filter to all entities with TenantId
    modelBuilder.Entity<Event>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    // ... etc for all tenant-scoped entities
}
```

---

## Related Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Overall system architecture
- **[SECURITY.md](SECURITY.md)** - Authentication and authorization
- **[GOVERNANCE.md](GOVERNANCE.md)** - Coding conventions and patterns
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Critical coding rules

## Skills

- **`cqrs-mediatr-guidelines`** - CQRS patterns with MediatR
- **`auth-patterns`** - Authentication and Keycloak integration
- **`dotnet-efcore-guidelines`** - EF Core patterns

# API Architecture

> REST API design patterns and conventions for ISLAMU Event.

**Last Updated**: January 2026

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

The `Explore.API` is a stateless REST API built on ASP.NET Core, following Clean Architecture and CQRS patterns.

### Key Characteristics

| Aspect | Implementation |
|--------|----------------|
| Framework | ASP.NET Core 10.0 |
| Architecture | Clean Architecture + CQRS |
| Authentication | JWT Bearer (Keycloak) |
| Documentation | OpenAPI 3.0 (Scalar + Swagger) |
| Versioning | URL path (`/api/v1/`) |
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

```
/api/v1/{resource}
/api/v1/{resource}/{id}
/api/v1/{resource}/{id}/{subresource}
```

**Examples**:
- `GET /api/v1/event` - List events
- `GET /api/v1/event/{id}` - Get event details
- `GET /api/v1/eventsession/by-event/{eventId}` - Get sessions for event
- `POST /api/v1/organization` - Create organization

---

## 3. Controller Conventions

### Standard Controller Structure

```csharp
[Route("api/v1/[controller]")]
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
| List | `GET` | `/` | `[AllowAnonymous]` | `PaginatedResult<TListDto>` |
| Get | `GET` | `/{id}` | `[AllowAnonymous]` | `TDto` |
| Create | `POST` | `/` | `[Authorize]` | `BaseCommandResponse<Guid>` |
| Update | `PUT` | `/{id}` | `[Authorize]` | `BaseCommandResponse<Guid>` |
| Delete | `DELETE` | `/{id}` | `[Authorize]` | `NoContent` or `NotFound` |

### Thin Controllers

Controllers should be thin - they only:
1. Extract data from HTTP request
2. Send command/query to MediatR
3. Return appropriate HTTP response

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
[EndpointSummary("Get all Events")]
public async Task<ActionResult<PaginatedResult<EventListDto>>> GetAll(...)
```

**Write Operations** - Authenticated users:
```csharp
[HttpPost]
[Authorize]
[EndpointSummary("Create a new Event")]
public async Task<ActionResult<BaseCommandResponse<Guid>>> Create(...)
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

```csharp
// Single entity
public async Task<ActionResult<EventDto>> GetById(Guid id)

// List with pagination
public async Task<ActionResult<PaginatedResult<EventListDto>>> GetAll(...)
```

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

```
GET /api/v1/event?pageNumber=1&pageSize=20
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

### HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| `200 OK` | Success | GET, PUT, POST success |
| `201 Created` | Resource created | POST (alternative) |
| `204 No Content` | Success, no body | DELETE success |
| `400 Bad Request` | Validation failed | Invalid input |
| `401 Unauthorized` | Not authenticated | Missing/invalid token |
| `403 Forbidden` | Not authorized | Insufficient permissions |
| `404 Not Found` | Resource not found | Invalid ID |
| `500 Internal Error` | Server error | Unexpected exception |

### Error Response Format

```json
{
    "error": "User ID not found in token"
}
```

Or with validation errors:

```json
{
    "success": false,
    "message": "Validation failed",
    "errors": [
        "Title is required",
        "StartDate must be in the future"
    ]
}
```

### Exception Handling

```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<ActionResult> Delete(Guid id)
{
    try
    {
        var userId = ExtractUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User ID not found in token" });
        }

        var result = await _mediator.Send(new DeleteEventCommand { Id = id, UserId = userId });

        if (!result)
        {
            return NotFound(new { error = "Event not found or permission denied" });
        }

        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting event {EventId}", id);
        return StatusCode(500, new { error = ex.Message });
    }
}
```

---

## 8. OpenAPI Documentation

### Required Attributes

All controller actions must include:

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

## 9. Endpoint Reference

### Core Resources

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Events | `/api/v1/event` | Event management |
| Event Sessions | `/api/v1/eventsession` | Session management |
| Organizations | `/api/v1/organization` | Organization management |
| Registrations | `/api/v1/eventregistration` | Event registration |
| Locations | `/api/v1/location` | Venue management |
| Categories | `/api/v1/category` | Event categories |
| Tags | `/api/v1/tag` | Event tags |

### Lookup Tables (Read-Only)

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Event Types | `/api/v1/eventtype` | Conference, Webinar, etc. |
| Event Formats | `/api/v1/eventformat` | In-person, Online, Hybrid |
| Event Statuses | `/api/v1/eventstatus` | Draft, Published, etc. |
| Audience Ages | `/api/v1/audienceage` | Children, Youth, Adults |
| Audience Genders | `/api/v1/audiencegender` | Men, Women, Mixed |
| Madhabs | `/api/v1/madhab` | Islamic jurisprudence schools |
| Languages | `/api/v1/language` | Event languages |
| Registration Modes | `/api/v1/registrationmode` | Registration options |

### User & Identity

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Users | `/api/v1/user` | User management |
| Actors | `/api/v1/actor` | Identity (user/org) actors |
| Organization Members | `/api/v1/organizationmember` | Org membership |

### Storage

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Storage Objects | `/api/v1/storageobject` | File/image storage |

---

## 10. Multi-Tenancy

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

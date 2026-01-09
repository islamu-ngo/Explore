# Security Architecture

## Authentication (Keycloak)

**Protocol**: OpenID Connect (OIDC) / OAuth 2.0

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Authentication Flow                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Blazor (OIDC)                      API (JWT Bearer)                │
│  ─────────────                      ───────────────                 │
│  1. User clicks login               1. Client sends JWT in header   │
│  2. Redirect to Keycloak            2. API validates with Keycloak  │
│  3. User authenticates              3. Extract claims from token    │
│  4. Redirect back with code         4. Process request              │
│  5. Exchange code for tokens                                        │
│  6. Store in secure cookie                                          │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**Keycloak Configuration**:

| Setting | Value |
|---------|-------|
| Realm | `islamu-dev` (dev), `islamu` (prod) |
| Client ID (API) | `explore-api` |
| Client ID (Blazor) | `explore-blazor` |
| Grant Types | Authorization Code (Blazor), Client Credentials (service) |

### User ID Extraction from JWT

**Critical Pattern**: Extract userId from JWT claims with fallback order:

```csharp
// In controllers requiring user ID extraction
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

if (string.IsNullOrEmpty(userId))
{
    return Unauthorized(new { error = "User ID not found in token" });
}
```

**Fallback Order**:
1. `sub` - Standard OIDC subject claim
2. `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` - Legacy JWT claim
3. `sid` - Session ID (fallback for certain auth flows)

### JWT Claims Structure

Typical JWT token payload:

```json
{
  "sub": "user-guid-here",
  "name": "John Doe",
  "email": "john@example.com",
  "preferred_username": "johndoe",
  "email_verified": true,
  "realm_access": {
    "roles": ["user", "organization_admin"]
  },
  "resource_access": {
    "explore-api": {
      "roles": ["read", "write"]
    }
  }
}
```

### Authorization Patterns

**Public Read Access**:
```csharp
[HttpGet]
[AllowAnonymous]
public async Task<ActionResult<List<EventListDto>>> GetAll()
{
    // No authentication required
    var events = await _mediator.Send(new GetEventListRequest());
    return Ok(events);
}
```

**Authenticated Write Access**:
```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
{
    // Requires valid JWT token
    var command = new CreateEventCommand { EventDto = dto };
    var response = await _mediator.Send(command);
    return Ok(response);
}
```

**User-Specific Operations**:
```csharp
[HttpGet("my")]
[Authorize]
public async Task<ActionResult<List<EventListDto>>> GetMyEvents()
{
    // Extract userId from JWT claims
    var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;

    if (string.IsNullOrEmpty(userId))
    {
        return Unauthorized(new { error = "User ID not found in token" });
    }

    var events = await _mediator.Send(new GetMyEventsRequest { UserId = userId });
    return Ok(events);
}
```

## Authorization (Cerbos)

**Pattern**: Policy Decision Point (PDP) with attribute-based access control (ABAC)

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Authorization Flow                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. Request arrives at API                                          │
│  2. Extract user claims from JWT                                    │
│  3. Build Cerbos request:                                           │
│     - Principal (user ID, roles, attributes)                        │
│     - Resource (event ID, owner, visibility)                        │
│     - Action (create, read, update, delete)                         │
│  4. Send to Cerbos PDP                                              │
│  5. Cerbos evaluates policies                                       │
│  6. Return allow/deny decision                                      │
│  7. API enforces decision                                           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

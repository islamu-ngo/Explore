name: auth-patterns
description: Guidelines for authentication and authorization patterns in the ISLAMU Event project, covering OIDC, JWT, and BFF security.
type: domain
enforcement: suggest
priority: critical
---

# Authentication & Authorization Patterns

## 🎯 Purpose

This skill provides the standard patterns for implementing security in the ISLAMU Event platform. It covers the OIDC/JWT authentication flow, the Backend-for-Frontend (BFF) security model, and authorization conventions.

## ⚡ When This Skill Activates

**Triggered by**:
- Keywords: "auth", "security", "jwt", "oidc", "keycloak", "authorize", "claim"
- File patterns: `*Controller.cs`, `*Program.cs` (for auth setup)

## 📚 Resources

| Resource | Description |
|----------|-------------|
| [user-id-extraction.md](resources/user-id-extraction.md) | The critical fallback pattern for extracting user ID from JWT claims. |

## 1. Authentication Architecture: BFF with OIDC & JWT

The project uses a **Backend-for-Frontend (BFF)** pattern to handle authentication, which provides a strong security posture by never exposing tokens to the browser.

```mermaid
sequenceDiagram
    participant Browser (Blazor WASM)
    participant BFF (Blazor Server)
    participant Keycloak
    participant API (Backend)

    Browser->>+BFF: User clicks "Login"
    BFF->>+Keycloak: Initiates OIDC Authorization Code Flow
    Keycloak-->>-BFF: Redirects with Authorization Code
    BFF->>+Keycloak: Exchanges Code for Tokens (Access + Refresh)
    Keycloak-->>-BFF: Returns JWTs
    BFF-->>-Browser: Stores tokens in secure, HttpOnly cookie & redirects

    Browser->>+BFF: Makes API call (/api/v1/...)
    BFF->>+API: YARP proxy reads token from cookie, attaches as "Authorization: Bearer" header
    API->>API: Validates JWT signature & claims
    API-->>-BFF: Returns data
    BFF-->>-Browser: Returns data to client
```

*   **`Explore.Blazor` (BFF)**: Is the OIDC client. It handles the redirect to Keycloak and manages the user's session via a cookie.
*   **`Explore.Blazor.Client` (WASM)**: Is not OIDC-aware. It simply includes credentials (the cookie) with every request to its backend (the BFF).
*   **`Explore.API`**: Is a stateless service that only trusts JWT Bearer tokens. It has no knowledge of the user's session cookie.

## 2. Authorization Conventions

### Controller Endpoint Authorization

A simple and strict convention is followed for API endpoints:
*   **`GET` requests are public**: Decorated with `[AllowAnonymous]`.
*   **`POST`, `PUT`, `DELETE` requests are protected**: Decorated with `[Authorize]`.

```csharp
// Example from EventController.cs

// ✅ Public read access
[HttpGet]
[AllowAnonymous]
public async Task<ActionResult<List<EventListDto>>> GetAll()
{
    var events = await _mediator.Send(new GetEventListRequest());
    return Ok(events);
}

// ✅ Authenticated write access
[HttpPost]
[Authorize]
public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto @event)
{
    var command = new CreateEventCommand { EventDto = @event };
    var response = await _mediator.Send(command);
    return Ok(response);
}
```

### Resource-Level Authorization

Resource-level authorization (e.g., "can this user edit *this specific* event?") is the responsibility of the **MediatR handler** in the Application layer, not the controller.

```csharp
// Example: DeleteEventCommandHandler
public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
{
    // The handler receives the UserId from the controller
    var @event = await _eventRepository.GetById(request.Id);

    if (@event == null)
        return false; // Not found

    // ✅ Handler enforces ownership logic
    var actor = await _actorRepository.GetById(@event.ActorId);
    if (actor == null || actor.UserId.ToString() != request.UserId)
    {
        // User does not own the event, deny deletion
        return false;
    }

    await _eventRepository.Delete(@event);
    return true;
}
```

## 3. User ID Extraction from JWT Claims

For the **CRITICAL PATTERN** for safely and consistently extracting the user ID from JWT claims, including the fallback mechanism for `sub`, `nameidentifier`, and `sid`, refer to [user-id-extraction.md](resources/user-id-extraction.md).

---
**Related Skills**:
- `clean-architecture-rules`
- `blazor-bff-patterns`

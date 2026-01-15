# User ID Extraction Pattern

This document details the **CRITICAL** pattern for safely and consistently extracting the user ID from JWT claims within the ISLAMU Event platform. This pattern accounts for various claim types and provides a robust fallback mechanism.

---

## 1. The Fallback Pattern (ASP.NET Core)

Always use this specific fallback logic when attempting to retrieve the authenticated user's ID from a `ClaimsPrincipal` (e.g., `HttpContext.User`).

```csharp
using System.Security.Claims; // Required for ClaimTypes

// Access the ClaimsPrincipal from HttpContext or another source
// Example from an ASP.NET Core Controller:
var claimsPrincipal = HttpContext.User;

var userId = claimsPrincipal.FindFirst("sub")?.Value // 1. Standard OIDC subject claim (preferred)
    ?? claimsPrincipal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value // 2. Legacy .NET nameidentifier claim (fallback)
    ?? claimsPrincipal.FindFirst("sid")?.Value; // 3. Session ID (last resort, less common for user ID)

if (string.IsNullOrEmpty(userId))
{
    // Handle the case where no user ID could be extracted.
    // This typically indicates a misconfigured token or an unauthenticated request
    // that reached an [Authorize] endpoint without a valid user context.
    // Depending on context (e.g., API controller), you might return Unauthorized.
    // In a Blazor component, you might redirect to login.
    throw new UnauthorizedAccessException("User ID claim could not be found in the token.");
}

// At this point, userId contains the extracted user ID
Console.WriteLine($"Extracted User ID: {userId}");
```

### Claim Priority Explained:

1.  **`sub` (Subject)**: This is the **standard and preferred** claim for a unique identifier of the user (or "subject") in OpenID Connect (OIDC) and OAuth 2.0. Keycloak typically populates this.
2.  **`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`**: This is the .NET Framework's default claim type for `NameIdentifier` (User ID). It's a common fallback, especially when dealing with older identity providers or configurations.
3.  **`sid` (Session ID)**: This is the session ID claim. While it's a unique identifier for a session, it's generally **less preferred** for identifying the user directly across different sessions or systems. It's included as a last resort fallback if `sub` and `nameidentifier` are absent, but its usage as a primary user ID should be carefully evaluated based on the specific requirements.

---

## 2. Using the Pattern in MediatR Handlers

When performing authorization checks or data filtering based on the current user in MediatR handlers, the user ID typically needs to be passed from the controller (where `HttpContext` is available) to the handler.

```csharp
// File: Explore.API/Controllers/EventController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // For ClaimsPrincipal and ClaimTypes

[Authorize]
[HttpPost]
public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateEvent([FromBody] CreateEventDto dto)
{
    // ✅ Extract user ID using the fallback pattern
    var userId = HttpContext.User.FindFirst("sub")?.Value
        ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value // Using ClaimTypes.NameIdentifier constant
        ?? HttpContext.User.FindFirst("sid")?.Value;

    if (string.IsNullOrEmpty(userId))
    {
        return Unauthorized(new { error = "User ID not found in token" });
    }

    // Pass the extracted userId to the command
    var command = new CreateEventCommand { EventDto = dto, UserId = userId }; // Assume CreateEventCommand has a UserId property
    var response = await _mediator.Send(command);
    return Ok(response);
}

// File: Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateEventDto EventDto { get; set; }
    public string UserId { get; set; } = string.Empty; // ✅ Property to receive the userId
}

// File: Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    // ... dependencies ...

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // ... validation ...
        
        // ✅ Use request.UserId for ownership checks or data filtering
        var isAuthorized = await _permissionService.HasPermission(request.UserId, "create_event");
        if (!isAuthorized)
        {
            // Handle unauthorized access within the application layer
            // For example, return a specific error in BaseCommandResponse
        }

        // ... create event logic ...
    }
}
```

---

## 3. Key Considerations

*   **Security**: Always perform server-side validation of user claims. Do not trust client-side assertions.
*   **Consistency**: Adhere strictly to this pattern across all services that require user ID extraction.
*   **Error Handling**: Ensure that `string.IsNullOrEmpty(userId)` is handled appropriately (e.g., returning `401 Unauthorized` from an API controller).
*   **Blazor vs. API**: In Blazor Server-side, `HttpContext.User` is directly available. In Blazor WebAssembly, you typically rely on the BFF pattern, where the server-side Blazor app handles authentication and potentially passes claims to the WASM client.

---

**Related Skills**:
- [`auth-patterns`](../SKILL.md) - General authentication and authorization rules.
- [`blazor-bff-patterns`](../../blazor-bff-patterns/SKILL.md) - Blazor-specific authentication context.
- [`cqrs-mediatr-guidelines`](../../cqrs-mediatr-guidelines/SKILL.md) - How to pass user context to MediatR handlers.

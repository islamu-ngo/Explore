# Service Layer Patterns

> **Project-Agnostic Service Layer Patterns for Blazor**
>
> Placeholders use `{Placeholder}` syntax - see [../../../../docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).
>
> **Note**: Prefer generic templates first. Project-specific examples are optional references only.

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.Blazor.Client` | Blazor WASM project | `Explore.Blazor.Client` |
| `{Entity}` | Main entity (singular) | `Event` |
| `{Entities}` | Entity plural | `Events` |
| `{entity}` | camelCase entity | `event` |
| `{IdType}` | Primary key type | `Guid` |

---

This document describes the recommended pattern for creating a service layer that wraps the NSwag-generated API clients in the Blazor frontend.

---

## 1. Purpose of the Service Layer

The primary goal of this service layer is to provide an abstraction over the raw API client, offering several benefits:

*   **Error Handling**: Centralized and consistent error handling, preventing exceptions from bubbling up directly to UI components.
*   **Logging**: Standardized logging for API calls and their outcomes.
*   **Safe Defaults**: Returning empty collections or `null`s instead of throwing exceptions on expected API failures (e.g., 404 Not Found).
*   **Decoupling**: Isolating UI components from direct knowledge of the API client's methods or specific HTTP status codes.
*   **Reusability**: Encapsulating common API call logic.

---

## 2. Service Layer Structure

### Service Interface (`I{Entity}Service.cs`)

Define an interface for each entity or domain area that exposes methods for common operations.

**File**: `Explore.Blazor.Client/Services/IEventService.cs`

```csharp
namespace Explore.Blazor.Client.Services;

using Explore.Blazor.Client.Clients; // NSwag generated client
using Explore.Application.DTOs.Event; // Shared DTOs
using Explore.Application.Responses; // Shared command responses

public interface IEventService
{
    Task<ICollection<EventListDto>> GetAllEventsAsync();
    Task<EventDto?> GetEventByIdAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid id, UpdateEventDto dto);
    Task<bool> DeleteEventAsync(Guid eventId);
}
```

### Service Implementation (`{Entity}Service.cs`)

Implement the interface, injecting the NSwag-generated API client and `ILogger`. Each method should include `try-catch` blocks to handle `ApiException` (from NSwag) and general exceptions.

**File**: `Explore.Blazor.Client/Services/EventService.cs`

```csharp
namespace Explore.Blazor.Client.Services;

using Explore.Blazor.Client.Clients; // NSwag generated client
using Explore.Application.DTOs.Event; // Shared DTOs
using Explore.Application.Responses; // Shared command responses
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class EventService : IEventService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventService> _logger;

    public EventService(IEventApiClient apiClient, ILogger<EventService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<EventListDto>> GetAllEventsAsync()
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Fetching all events...");
            var response = await _apiClient.EventAllAsync();
            _logger.LogInformation("[EVENT SERVICE] Received {Count} events", response?.Count ?? 0);
            return response ?? new List<EventListDto>(); // ✅ Return safe default
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>(); // ✅ Return safe default on API error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching events");
            return new List<EventListDto>(); // ✅ Return safe default on unexpected error
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
    {
        try
        {
            return await _apiClient.EventGETAsync(eventId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404) // ✅ Specific handling for 404
        {
            _logger.LogWarning("[EVENT SERVICE] Event not found: {EventId}", eventId);
            return null; // ✅ Return null for not found
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching event {EventId}", eventId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto dto)
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Creating event: {Title}", dto.Title);
            return await _apiClient.EventPOSTAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error creating event: {StatusCode}", ex.StatusCode);
            // ✅ Return error response object for command failures
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
    }

    public async Task<bool> DeleteEventAsync(Guid eventId)
    {
        try
        {
            await _apiClient.EventDELETEAsync(eventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error deleting event {EventId}", eventId);
            return false;
        }
    }
}
```

### Key Practices

*   **Error Handling**: Use `try-catch` blocks to specifically handle `ApiException` (which NSwag clients throw for HTTP errors) and general `Exception`s.
*   **Safe Defaults**: Always return an empty collection (`new List<T>()`) for list methods or `null` for single-item methods on failure, rather than re-throwing exceptions.
*   **Logging**: Log API call status, errors, and any specific warnings.
*   **Command Responses**: For command-type operations (Create, Update, Delete), return the `BaseCommandResponse<Guid>` object, which explicitly contains `Success`, `Message`, `Id`, and `Errors` properties.

---

## 3. Service Registration

Register service interfaces and implementations in the Dependency Injection container. Services that manage state for a user's session should be `Scoped`.

**File**: `Explore.Blazor.Client/Program.cs`

```csharp
// Register the NSwag generated API client
// This uses HttpClient, configured with message handlers for credentials and 401 redirects
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<BrowserCredentialsMessageHandler>()
.AddHttpMessageHandler<BffUnauthorizedHandler>();

// Register custom services as scoped
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
// ... other services
```

---

**Related Documentation**:
- [bff-configuration.md](bff-configuration.md) - How NSwag clients are configured for BFF.
- [auth-state-management.md](auth-state-management.md) - Use of `BrowserCredentialsMessageHandler` and `BffUnauthorizedHandler`.

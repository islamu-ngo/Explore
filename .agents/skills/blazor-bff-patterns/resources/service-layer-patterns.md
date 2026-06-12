ABOUTME: Service-layer wrapper patterns for NSwag API clients in Blazor.
ABOUTME: Defines interface contracts, error handling, and registration rules.

# Service Layer Patterns

## Architecture

```
Razor Component → IFeatureService → NSwag ApiClient → BFF Proxy → API
```

Components never call `ApiClient` directly. All API interaction flows through a service interface.

## Required Rules

1. **Wrap NSwag clients** behind service interfaces (contract in `Contracts/Services/`, implementation in `Services/`).
2. **Catch `ApiException`** and return safe defaults — empty collections (`[]`), `null`, or typed error responses.
3. **Log errors** at the service layer — do not throw to UI components by default.
4. **Map API responses** to view models when the API DTO shape does not match UI needs.

## Service Structure

```csharp
// Contract — Contracts/Services/IEventService.cs
public interface IEventService
{
    Task<IReadOnlyList<EventListItemViewModel>> GetEventsAsync(int page = 1, int pageSize = 20);
    Task<EventDetailViewModel?> GetEventByIdAsync(Guid id);
    Task<ServiceResult<Guid>> CreateEventAsync(CreateEventRequest request);
}

// Implementation — Services/EventService.cs
public class EventService(IApiClient apiClient, ILogger<EventService> logger) : IEventService
{
    public async Task<IReadOnlyList<EventListItemViewModel>> GetEventsAsync(int page, int pageSize)
    {
        try
        {
            var response = await apiClient.GetEventsAsync(page, pageSize);
            return response.Items.Select(MapToViewModel).ToList();
        }
        catch (ApiException ex)
        {
            logger.LogWarning(ex, "Failed to fetch events (page {Page})", page);
            return [];
        }
    }
}
```

## Error Handling Strategy

| API Status | Service Behavior |
|------------|-----------------|
| 200-299 | Map and return result |
| 400 | Return `ServiceResult` with validation errors |
| 401/403 | Log warning, return empty/null (auth layer handles redirect) |
| 404 | Return `null` for single items, empty for collections |
| 429 | Log warning, return safe default (UI shows retry message) |
| 500+ | Log error, return safe default |

## Registration

Register all services as **Scoped** in `ServiceCollectionExtensions`:

```csharp
services.AddScoped<IEventService, EventService>();
```

## Related

- [bff-configuration.md](bff-configuration.md)
- [auth-state-management.md](auth-state-management.md)

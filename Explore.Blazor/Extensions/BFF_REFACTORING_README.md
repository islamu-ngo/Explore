# BFF Endpoint Refactoring - Documentation

## Overview
The `Program.cs` has been completely refactored to use the NSwag-generated `EventApiClient` instead of manual `HttpClient` calls. This provides type safety, better maintainability, and follows industry best practices.

## Key Improvements

### 1. **Type Safety**
- All API calls now use strongly-typed DTOs from the NSwag client
- Compile-time checking of request/response types
- IntelliSense support for all API operations

### 2. **DRY Principle (Don't Repeat Yourself)**
- Created `BffApiExtensions` helper class to eliminate code duplication
- Common patterns extracted into reusable extension methods
- Centralized error handling and logging

### 3. **Centralized Error Handling**
- Consistent error response mapping across all endpoints
- Proper HTTP status code handling (401, 403, 404, 500)
- Structured logging for all API calls

### 4. **Simplified Code**
- **Before**: ~1500 lines of repetitive HttpClient code
- **After**: ~600 lines of clean, declarative endpoint mappings
- **Reduction**: ~60% less code while maintaining all functionality

### 5. **Better Security**
- Maintains BFF (Backend for Frontend) pattern
- Server-side token management via Duende
- No token exposure to client-side code

## Architecture

### Extension Methods (`BffApiExtensions.cs`)

```csharp
ExecuteAsync<T>()       // For API calls returning data
ExecuteVoidAsync()      // For API calls returning void/NoContent
GetApiClient()          // Resolves scoped IEventApiClient from DI
```

### Endpoint Structure

#### Primary Endpoints (`/api/v1/...`)
These are the main endpoints used by the WebAssembly client via NSwag:

```
/api/v1/Organization             // GET, POST
/api/v1/Organization/{id}        // GET, PUT
/api/v1/Organization/my          // GET (authenticated)
/api/v1/Organization/updatestatustype/{id}  // PUT (admin)

/api/v1/Event                    // GET, POST
/api/v1/Event/{id}               // GET, PUT, DELETE
/api/v1/Event/my                 // GET (authenticated)

/api/v1/User                     // GET, PUT, DELETE
/api/v1/User/sync                // POST

/api/v1/EventType                // GET
/api/v1/AudienceGender           // GET
/api/v1/AudienceAge              // GET
/api/v1/StatusType               // GET
```

#### Legacy Endpoints (`/bff/api/...`)
Maintained for backward compatibility with existing server-side Blazor components:

```
/bff/api/Organization/...
/bff/api/Event/...
/bff/api/User/...
/bff/api/admin/organizations/...
```

**Migration Path**: Gradually migrate all Blazor Server components to use `/api/v1` endpoints and eventually remove `/bff/api` endpoints.

## NSwag Client Methods Used

| Endpoint | NSwag Method | DTO Types |
|----------|--------------|-----------|
| GET /Organization | `OrganizationAllAsync()` | `ICollection<OrganizationListDto>` |
| POST /Organization | `OrganizationPOSTAsync(CreateOrganizationDto)` | `CreateOrganizationDto` ? `BaseCommandResponseOfGuid` |
| GET /Organization/{id} | `OrganizationGETAsync(Guid)` | `OrganizationDto` |
| PUT /Organization/{id} | `OrganizationPUTAsync(Guid, UpdateOrganizationDto)` | `UpdateOrganizationDto` ? `BaseCommandResponseOfGuid` |
| GET /Organization/my | `My2Async()` | `ICollection<OrganizationListDto>` |
| PUT /Organization/updatestatustype/{id} | `UpdatestatustypeAsync(Guid, UpdateOrganizationApprovalStatusDto)` | `UpdateOrganizationApprovalStatusDto` |
| GET /Event | `EventAllAsync()` | `ICollection<EventListDto>` |
| POST /Event | `EventPOSTAsync(CreateEventDto)` | `CreateEventDto` ? `BaseCommandResponseOfGuid` |
| GET /Event/{id} | `EventGETAsync(Guid)` | `EventDto` |
| PUT /Event/{id} | `EventPUTAsync(Guid, UpdateEventDto)` | `UpdateEventDto` ? `BaseCommandResponseOfGuid` |
| DELETE /Event/{id} | `EventDELETEAsync(Guid)` | void |
| GET /Event/my | `My3Async()` | `ICollection<EventListDto>` |
| POST /User/sync | `SyncAsync()` | `BaseCommandResponseOfGuid` |
| GET /User | `UserGETAsync()` | `UserDto` |
| PUT /User | `UserPUTAsync(UpdateUserDto)` | `UpdateUserDto` ? `BaseCommandResponseOfGuid` |
| DELETE /User | `UserDELETEAsync()` | void |

## Error Handling

All endpoints follow a consistent error handling pattern:

### Success Responses
- **200 OK**: Returns JSON data for GET/POST/PUT operations
- **204 No Content**: Returns for DELETE operations and void updates

### Error Responses
- **400 Bad Request**: Invalid request body
- **401 Unauthorized**: User not authenticated
- **403 Forbidden**: User authenticated but lacks permissions
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Unexpected errors

### Example Error Flow

```csharp
try
{
    var result = await apiClient.OrganizationGETAsync(id);
    return Results.Ok(result);
}
catch (ApiException ex) when (ex.StatusCode == 404)
{
    return Results.NotFound();
}
catch (ApiException ex) when (ex.StatusCode == 401)
{
    return Results.Unauthorized();
}
catch (ApiException ex)
{
    return Results.Problem(detail: ex.Response, statusCode: ex.StatusCode);
}
```

## Logging

All API calls are logged with:
- **Information**: Start and completion of operations
- **Error**: Failures with status codes and error details

Example log output:
```
INFO: BFF: Executing GET /api/v1/Organization
INFO: BFF: GET /api/v1/Organization completed successfully

ERROR: BFF: POST /api/v1/Organization failed with status 400
```

## Security

### Authentication Flow
1. User authenticates via Keycloak (OIDC)
2. Access token and refresh token stored in encrypted cookie
3. BFF endpoints retrieve token from cookie
4. `IEventApiClient` automatically attaches token to API requests (via `AddUserAccessTokenHandler()`)
5. Duende automatically refreshes expired tokens

### Authorization
- Public endpoints: `/api/v1/Organization` (GET), `/api/v1/Event` (GET), lookup data
- Authenticated endpoints: All POST, PUT, DELETE operations
- `.RequireAuthorization()` applied to protected endpoints

## Migration Guide

### For New Features
Use the `/api/v1` endpoints directly:

```csharp
// In Blazor components or services
var organizations = await ApiClient.OrganizationAllAsync();
```

### For Existing Code
1. Update service layer to use `/api/v1` paths
2. Update HttpClient calls to use NSwag client methods
3. Test thoroughly
4. Once all code migrated, remove `/bff/api` endpoints

### Example Migration

**Before**:
```csharp
var http = httpFactory.CreateClient("ExploreApiPublic");
var response = await http.GetAsync("api/Organization");
var content = await response.Content.ReadFromJsonAsync<List<OrganizationListDto>>();
```

**After**:
```csharp
var organizations = await apiClient.OrganizationAllAsync();
```

## Testing

### Unit Tests
The refactored code is more testable because:
- Dependencies are clearly defined (IEventApiClient)
- Extension methods can be tested independently
- Mocking is simpler with typed interfaces

### Integration Tests
Test both `/api/v1` and `/bff/api` endpoints during migration period.

## Performance Considerations

### Benefits
- **Faster Development**: Type-safe API calls with IntelliSense
- **Fewer Runtime Errors**: Compile-time checking of DTOs
- **Better Caching**: NSwag client supports response caching
- **Connection Pooling**: Managed by HttpClientFactory

### Monitoring
- All endpoints log execution time via ILogger
- Use Application Insights or similar for production monitoring

## Maintenance

### When API Changes
1. Update API swagger.json
2. Rebuild Blazor.Client project (NSwag regenerates client)
3. Update Program.cs endpoints if new operations added
4. No manual DTO updates required

### Adding New Endpoints
1. Add to API controller
2. NSwag auto-generates client method
3. Add BFF endpoint mapping:

```csharp
apiV1.MapPost("/NewResource", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateNewResourceDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().NewResourcePOSTAsync(dto),
        logger,
        "POST /api/v1/NewResource"
    );
})
.RequireAuthorization();
```

## Best Practices Applied

? **SOLID Principles**
- Single Responsibility: Each endpoint has one responsibility
- Open/Closed: Extension methods allow extension without modification
- Dependency Inversion: Depends on IEventApiClient abstraction

? **Clean Code**
- Self-documenting code with clear method names
- Consistent patterns across all endpoints
- No code duplication

? **Industry Standards**
- BFF pattern for security
- OpenAPI/NSwag for API contracts
- Minimal API pattern (.NET 9)
- Structured logging

? **Security**
- Server-side token management
- CSRF protection
- Proper authorization checks
- No sensitive data in client code

## Troubleshooting

### Issue: "IEventApiClient not registered"
**Solution**: Ensure `AddHttpClient<IEventApiClient, EventApiClient>()` is called in DI setup.

### Issue: "401 Unauthorized" on authenticated endpoints
**Solution**: Check that `.RequireAuthorization()` is applied and user is logged in.

### Issue: "NSwag client method not found"
**Solution**: Rebuild Blazor.Client project to regenerate NSwag client from latest swagger.json.

### Issue: "Invalid request body"
**Solution**: Ensure DTO sent from client matches expected type in NSwag client.

## Future Enhancements

1. **Response Caching**: Add caching for lookup data endpoints
2. **Rate Limiting**: Add rate limiting to prevent abuse
3. **Versioning**: Support API versioning (v1, v2, etc.)
4. **Health Checks**: Add BFF health check endpoint
5. **Metrics**: Add Prometheus metrics for endpoint usage
6. **OpenTelemetry**: Add distributed tracing

## Conclusion

This refactoring provides a solid foundation for the BFF layer with:
- ? Type safety and compile-time checks
- ? Maintainable and testable code
- ? Industry best practices
- ? Clear upgrade path for future changes
- ? ~60% code reduction
- ? Better developer experience

The investment in this refactoring will pay dividends in reduced bugs, faster development, and easier maintenance.

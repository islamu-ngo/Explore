# BFF Endpoints - Quick Reference

## How to Add a New Endpoint

### Step 1: Ensure API endpoint exists
The API endpoint should already be defined in your API controller and exposed via Swagger.

### Step 2: Rebuild Blazor.Client  
NSwag will auto-generate the client method from swagger.json:
```bash
dotnet build Explore.Blazor.Client
```

### Step 3: Add BFF endpoint mapping
In `Program.cs`, add the endpoint after the existing ones:

#### For GET endpoints (returning data):
```csharp
apiV1.MapGet("/YourResource", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().YourResourceAllAsync(),
        logger,
        "GET /api/v1/YourResource"
    ));
```

#### For GET with ID:
```csharp
apiV1.MapGet("/YourResource/{id}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().YourResourceGETAsync(id),
        logger,
        $"GET /api/v1/YourResource/{id}"
    ));
```

#### For POST (creating data):
```csharp
apiV1.MapPost("/YourResource", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateYourResourceDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().YourResourcePOSTAsync(dto),
        logger,
        "POST /api/v1/YourResource"
    );
})
.RequireAuthorization(); // Add if authentication required
```

#### For PUT (updating data):
```csharp
apiV1.MapPut("/YourResource/{id}", async (Guid id, HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<UpdateYourResourceDto>();
    if (dto == null)
        return Results.BadRequest("Invalid request body");

    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().YourResourcePUTAsync(id, dto),
        logger,
        $"PUT /api/v1/YourResource/{id}"
    );
})
.RequireAuthorization();
```

#### For DELETE:
```csharp
apiV1.MapDelete("/YourResource/{id}", async (Guid id, HttpContext ctx) =>
    await BffApiExtensions.ExecuteVoidAsync(
        () => ctx.GetApiClient().YourResourceDELETEAsync(id),
        logger,
        $"DELETE /api/v1/YourResource/{id}"
    ))
    .RequireAuthorization();
```

## Common Patterns

### Public endpoint (no auth):
```csharp
apiV1.MapGet("/Public", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().PublicAsync(),
        logger,
        "GET /api/v1/Public"
    ));
```

### Protected endpoint (requires auth):
```csharp
apiV1.MapGet("/Protected", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().ProtectedAsync(),
        logger,
        "GET /api/v1/Protected"
    ))
    .RequireAuthorization(); // <-- Add this
```

### Endpoint with query parameters:
```csharp
apiV1.MapGet("/Search", async (string query, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().SearchAsync(query),
        logger,
        $"GET /api/v1/Search?query={query}"
    ));
```

### Endpoint with multiple parameters:
```csharp
apiV1.MapGet("/Filter", async (string category, int page, HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().FilterAsync(category, page),
        logger,
        $"GET /api/v1/Filter?category={category}&page={page}"
    ));
```

## Finding the Correct NSwag Method Name

1. Open `Explore.Blazor.Client\Clients\EventApiClient.g.cs`
2. Search for your controller name (e.g., "YourResource")
3. Look for methods like:
   - `YourResourceAllAsync()` - GET all
   - `YourResourceGETAsync(Guid id)` - GET by ID
   - `YourResourcePOSTAsync(CreateDto dto)` - POST
   - `YourResourcePUTAsync(Guid id, UpdateDto dto)` - PUT
   - `YourResourceDELETEAsync(Guid id)` - DELETE

**Note**: Some methods have non-standard names like:
- `MyAsync()` for `/Event/my`
- `My2Async()` for `/Organization/my`
- `ApprovalStatusAllAsync()` for `/StatusType`

Check the XML comments or method parameters to verify the correct one.

## Error Handling

All endpoints automatically handle:
- ? 400 Bad Request (invalid DTO)
- ? 401 Unauthorized (not authenticated)
- ? 403 Forbidden (no permission)
- ? 404 Not Found (resource not found)
- ? 500 Internal Server Error (unexpected errors)

No additional error handling code needed!

## Logging

All endpoints automatically log:
- **INFO**: Start and completion of operation
- **ERROR**: Failures with status code and details

Example log output:
```
INFO: BFF: Executing GET /api/v1/YourResource
INFO: BFF: GET /api/v1/YourResource completed successfully

// or on error:
ERROR: BFF: POST /api/v1/YourResource failed with status 400
```

## Testing Your Endpoint

### 1. Manual Testing (Browser/Postman)
```bash
# Public endpoint
GET https://localhost:7071/api/v1/YourResource

# Authenticated endpoint (requires login first)
POST https://localhost:7071/api/v1/YourResource
Content-Type: application/json

{
  "name": "Test",
  "description": "Test description"
}
```

### 2. Check Logs
Look for your endpoint log messages in the console/output window.

### 3. Test Error Cases
- Try invalid JSON ? Should get 400
- Try without auth (if protected) ? Should get 401
- Try with wrong ID ? Should get 404

## Troubleshooting

### "IEventApiClient does not contain a definition for XAsync"
1. Check `EventApiClient.g.cs` for the exact method name
2. Rebuild `Explore.Blazor.Client` to regenerate NSwag client
3. Verify the API endpoint exists in swagger.json

### "Invalid request body" on POST/PUT
Ensure the DTO type matches exactly:
```csharp
await ctx.Request.ReadFromJsonAsync<CreateYourResourceDto>()
//                                  ^^^^^^^^^^^^^^^^^^^^^^^^
//                                  Must match NSwag client method parameter
```

### Endpoint returns 401 even when logged in
Add `.RequireAuthorization()` at the end:
```csharp
})
.RequireAuthorization(); // <-- Don't forget this!
```

### Changes not reflected after rebuild
1. Stop debugging
2. Clean solution
3. Rebuild
4. Restart application

## Best Practices

? **DO**:
- Use `ExecuteAsync<T>()` for endpoints returning data
- Use `ExecuteVoidAsync()` for endpoints returning void/NoContent
- Always validate DTO is not null before calling API
- Add `.RequireAuthorization()` for protected endpoints
- Use descriptive operation names in logs

? **DON'T**:
- Don't manually create HttpClient instances
- Don't manually attach tokens
- Don't write custom error handling (use BffApiExtensions)
- Don't duplicate endpoint definitions
- Don't forget to rebuild Blazor.Client after API changes

## Quick Copy-Paste Templates

### Minimal GET endpoint:
```csharp
apiV1.MapGet("/Resource", async (HttpContext ctx) =>
    await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().ResourceAllAsync(),
        logger,
        "GET /api/v1/Resource"
    ));
```

### Minimal POST endpoint with auth:
```csharp
apiV1.MapPost("/Resource", async (HttpContext ctx) =>
{
    var dto = await ctx.Request.ReadFromJsonAsync<CreateResourceDto>();
    if (dto == null) return Results.BadRequest("Invalid request body");
    
    return await BffApiExtensions.ExecuteAsync(
        () => ctx.GetApiClient().ResourcePOSTAsync(dto),
        logger,
        "POST /api/v1/Resource"
    );
})
.RequireAuthorization();
```

---

**Remember**: The BFF layer is just a thin proxy. All business logic stays in the API!

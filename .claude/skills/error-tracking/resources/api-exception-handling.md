# Centralized API Exception Handling

> **Project-Agnostic Exception Handling Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

This pattern ensures that all unhandled exceptions occurring within the API are caught centrally, logged, and presented to the client in a standardized, RFC 7807 `ProblemDetails` format. This avoids repetitive `try-catch` blocks in every controller action and provides a consistent error experience.

---

## 1. Configuration in `Program.cs`

The primary setup for centralized exception handling in ASP.NET Core involves `UseExceptionHandler` and `AddProblemDetails` in the `Program.cs` file of the `{Project}.API` project.

**File**: `{Project}.API/Program.cs`

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

// Add ProblemDetails services to the DI container
builder.Services.AddProblemDetails();

// Configure the exception handling middleware
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        // Set the HTTP status code to 500 Internal Server Error
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        // Set the Content-Type header to indicate a ProblemDetails response
        context.Response.ContentType = "application/problem+json";

        // Retrieve the IExceptionHandlerFeature, which contains the unhandled exception
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        // Retrieve the ProblemDetails service from the request services
        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();

        // Log the exception (ILogger would be used here)
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exceptionHandlerFeature?.Error, "An unhandled exception occurred during request processing.");

        // Construct and write the ProblemDetails response to the client
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Detail = "Please try again later. If the problem persists, contact support.",
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path
            }
        });
    });
});
```

---

## 2. Benefits of Centralized Handling

*   **Consistency**: All unhandled API errors return a standard `ProblemDetails` response, making client-side error handling predictable.
*   **Reduced Boilerplate**: Eliminates the need for `try-catch` blocks in every controller action, keeping controllers clean and focused on business logic.
*   **Security**: Prevents sensitive exception details from being directly exposed to API consumers by default, while still allowing for internal logging.
*   **Observability**: Provides a single point where all unhandled exceptions can be logged, monitored, and potentially sent to external error tracking services like Sentry.

---

## 3. `ProblemDetails` Format (RFC 7807)

The `ProblemDetails` object conforms to RFC 7807, providing a structured way to convey API errors.

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An unexpected error occurred.",
  "status": 500,
  "detail": "Please try again later. If the problem persists, contact support.",
  "instance": "/api/{entity}/some-failing-endpoint"
}
```

---

## 4. Interaction with Other Error Handling

*   **Explicit Error Returns**: For business validation errors (e.g., FluentValidation failing), use `BadRequest(response)` to return a specific `BaseCommandResponse` or `ProblemDetails` with status `400 Bad Request`. These are *handled* errors and won't be caught by `UseExceptionHandler`.
*   **Sentry Integration**: If Sentry is enabled, the centralized exception handler is the ideal place to capture the `exceptionHandlerFeature.Error` using `SentrySdk.CaptureException()`, ensuring all crashes are reported.

---

## 5. Middleware Order

The order of middleware in `Program.cs` is crucial:
*   `app.UseExceptionHandler()` should typically be placed early in the pipeline to catch exceptions from subsequent middleware.
*   `app.UseDeveloperExceptionPage()` (if used for development) must be placed *before* `UseExceptionHandler`.

```csharp
// Example middleware pipeline order
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Sentry tracing middleware (if integrated) should be here
// app.UseSentryTracing();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

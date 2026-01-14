---
name: error-tracking
description: Add Sentry error tracking and performance monitoring to ISLAMU Event .NET services. Use this skill when adding error handling, creating new controllers, or tracking performance. ALL ERRORS MUST BE CAPTURED TO SENTRY - no exceptions.
type: guardrail
enforcement: suggest
priority: high
---

# ISLAMU Event Sentry Integration Skill

## Purpose
This skill enforces comprehensive Sentry error tracking and performance monitoring across ISLAMU Event .NET services (API, Blazor).

## When to Use This Skill
- Adding error handling to controllers or pages
- Creating new API endpoints
- Tracking performance of database operations
- Handling exceptions in command/query handlers
- Monitoring Blazor component errors

## 🚨 CRITICAL RULE

**Do not swallow exceptions.** Use structured logging (`ILogger`) and centralized exception handling that returns RFC 7807 `ProblemDetails`. 

> Note: Sentry isn't currently integrated in this repo (as of this skill update). If/when Sentry is added, capture exceptions *in addition to* logging.

## Current Integration Status

### Explore.API
- ✅ Centralized error responses should be implemented via `UseExceptionHandler` + `AddProblemDetails`.
- 🟡 Sentry integration: **planned** (not currently present in codebase).

### Explore.Blazor
- 🟡 UI error boundary patterns are optional.
- 🟡 Sentry integration: **planned** (not currently present in codebase).

## Error Handling & Observability Patterns

### 1. Centralized API Exception Handling (preferred)

**Pattern**: Keep controllers/handlers free of repetitive try/catch. Use `UseExceptionHandler` + `AddProblemDetails` to return RFC 7807 responses and log exceptions once.

```csharp
// Explore.API/Program.cs
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

builder.Services.AddProblemDetails();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Detail = feature?.Error.Message,
                Status = StatusCodes.Status500InternalServerError
            }
        });
    });
});
```

### 2. MediatR Handler Error Handling

**Pattern**: Prefer *pipeline behaviors* for cross-cutting concerns (logging, timing, tracing). Let unexpected exceptions bubble to the centralized exception handler.

```csharp
// Explore.Application/Behaviors/LoggingBehavior.cs
using MediatR;
using Microsoft.Extensions.Logging;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

### 3. Database Performance Monitoring

**Pattern**: Prefer OpenTelemetry tracing (Aspire-friendly). If you need manual spans, use `ActivitySource` (works with OTEL exporters and Aspire dashboard).

```csharp
// Explore.Persistence/Repositories/EventRepository.cs
using System.Diagnostics;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    public async Task<List<Event>> GetEventsWithDetails()
    {
        using var activity = new ActivitySource("Explore.Persistence")
            .StartActivity("EventRepository.GetEventsWithDetails");

        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
            .Include(e => e.EventStatus)
            .ToListAsync();
    }
}
```

### 4. Blazor Error Boundary

**Pattern**: Use ErrorBoundary component for UI errors.

```razor
<!-- Explore.Blazor/Components/Pages/Events.razor -->
<ErrorBoundary>
    <ChildContent>
        @if (_events == null)
        {
            <MudProgressCircular Indeterminate="true" />
        }
        else
        {
            <!-- Event list -->
        }
    </ChildContent>
    <ErrorContent Context="ex">
        <MudAlert Severity="Severity.Error">
            An error occurred while loading events.
        </MudAlert>
        @code {
            // Capture/log via your configured provider (ILogger, OpenTelemetry, or Sentry when integrated).
        }
    </ErrorContent>
</ErrorBoundary>

@code {
    private List<EventListDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _events = await Http.GetFromJsonAsync<List<EventListDto>>("api/v1/event");
        }
        catch (Exception ex)
        {
            // Capture/log via your configured provider (ILogger, OpenTelemetry, or Sentry when integrated).
            throw; // Let ErrorBoundary handle it
        }
    }
}
```

### 5. ASP.NET Core Middleware Integration

> **Optional (planned)**: only apply this section after adding the Sentry packages and DSN configuration.

**Pattern**: Use Sentry middleware for automatic request tracking.

```csharp
// Explore.API/Program.cs or Explore.AppHost/Program.cs
using Sentry;
using Sentry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Sentry
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 0.1; // 10% of transactions
    options.AutoSessionTracking = true;
    options.IsGlobalModeEnabled = true;
    
    // Performance monitoring
    options.EnableTracing = true;
    options.ProfilesSampleRate = 0.1;
    
    // Filter out sensitive data
    options.BeforeSend = (sentryEvent) =>
    {
        // Remove sensitive headers
        if (sentryEvent.Request?.Headers != null)
        {
            sentryEvent.Request.Headers.Remove("Authorization");
            sentryEvent.Request.Headers.Remove("Cookie");
        }
        return sentryEvent;
    };
});

var app = builder.Build();

// Use Sentry middleware (must be FIRST)
app.UseSentryTracing();

// ... other middleware
app.UseAuthentication();
app.UseAuthorization();
```

## Optional: Sentry-specific Guidance (planned)

### Error Levels

Use appropriate severity levels:

- **Fatal**: System is unusable (database down, critical service failure)
- **Error**: Operation failed, needs immediate attention
- **Warning**: Recoverable issues, degraded performance
- **Info**: Informational messages, successful operations
- **Debug**: Detailed debugging information (dev only)

```csharp
// Example usage
SentrySdk.CaptureMessage("Event created successfully", SentryLevel.Info);
SentrySdk.CaptureMessage("Database query slow", SentryLevel.Warning);
SentrySdk.CaptureException(ex, scope => { scope.Level = SentryLevel.Fatal; });
```

### Required Context

```csharp
SentrySdk.CaptureException(ex, scope =>
{
    // User context
    scope.User = new User
    {
        Id = userId,
        Email = userEmail,
        Username = username
    };

    // Tags for filtering
    scope.SetTag("service", "explore-api");
    scope.SetTag("environment", "production");
    scope.SetTag("tenant", tenantId);
    scope.SetTag("feature", "event-management");

    // Additional context
    scope.SetContext("operation", new
    {
        Type = "event.create",
        EventId = eventId,
        OrganizationId = orgId
    });

    // Extra data
    scope.SetExtra("request", requestDto);
});
```

### Configuration (appsettings.json)

```json
{
  "Sentry": {
    "Dsn": "https://your-sentry-dsn@sentry.io/project-id",
    "Environment": "Production",
    "TracesSampleRate": 0.1,
    "ProfilesSampleRate": 0.1,
    "Debug": false,
    "DiagnosticLevel": "Error"
  }
}
```

### Performance Monitoring

### Requirements

1. **All API endpoints** must have automatic transaction tracking (via middleware)
2. **Database queries > 100ms** should be flagged
3. **Command/Query handlers** should track execution time
4. **External API calls** must be tracked

### Transaction Tracking

```csharp
// Automatic via middleware for HTTP requests
// Manual for background jobs or custom operations

var transaction = SentrySdk.StartTransaction("job.sync-events", "background");
try
{
    // Your operation
    transaction.Status = SpanStatus.Ok;
}
catch (Exception ex)
{
    transaction.Status = SpanStatus.InternalError;
    SentrySdk.CaptureException(ex);
    throw;
}
finally
{
    transaction.Finish();
}
```

## Common Mistakes to Avoid

❌ **NEVER** use Console.WriteLine for errors in production
❌ **NEVER** swallow exceptions silently
❌ **NEVER** expose sensitive data (passwords, tokens, PII) in error context
❌ **NEVER** use generic error messages without context
❌ **NEVER** skip error handling in async operations
❌ **NEVER** forget to configure Sentry DSN in appsettings

## Implementation Checklist (when integrating Sentry)

When adding Sentry to this repo:

- [ ] Added Sentry NuGet package reference
- [ ] Configured Sentry in Program.cs
- [ ] Prefer centralized exception handling; avoid duplicating try/catch in every controller
- [ ] Added meaningful context to errors
- [ ] Used appropriate error level
- [ ] No sensitive data in error messages
- [ ] Added performance tracking for slow operations
- [ ] Tested error handling paths
- [ ] Verified Sentry dashboard receives events

## NuGet Packages

### Explore.API
```xml
<PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
<PackageReference Include="Sentry.Serilog" Version="4.0.0" />
```

### Explore.Blazor
```xml
<PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
```

### Explore.Application (Optional)
```xml
<PackageReference Include="Sentry" Version="4.0.0" />
```

## Testing Sentry Integration (optional)

### API Test Endpoint

```csharp
[HttpGet("sentry/test-error")]
[AllowAnonymous]
public IActionResult TestSentryError()
{
    try
    {
        throw new InvalidOperationException("Test Sentry exception from API");
    }
    catch (Exception ex)
    {
        SentrySdk.CaptureException(ex, scope =>
        {
            scope.SetTag("test", "true");
            scope.SetTag("endpoint", "test-error");
        });
        throw;
    }
}

[HttpGet("sentry/test-performance")]
[AllowAnonymous]
public async Task<IActionResult> TestPerformance()
{
    var transaction = SentrySdk.StartTransaction("test.performance", "test");
    try
    {
        await Task.Delay(1000); // Simulate slow operation
        transaction.Status = SpanStatus.Ok;
        return Ok(new { message = "Performance test completed" });
    }
    finally
    {
        transaction.Finish();
    }
}
```

### Test Commands

```bash
# Test error capture
curl https://localhost:7001/api/v1/sentry/test-error

# Test performance tracking
curl https://localhost:7001/api/v1/sentry/test-performance
```

## Related Skills

- Use **clean-architecture-rules** for proper error handling layer placement
- Use **cqrs-mediatr-guidelines** for handler error patterns
- Use **dotnet-efcore-guidelines** for database error handling

---

**Enforcement Level**: 💡 SUGGEST (Provides guidance, encourages adoption)

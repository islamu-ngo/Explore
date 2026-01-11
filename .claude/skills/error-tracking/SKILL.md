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

**ALL ERRORS MUST BE CAPTURED TO SENTRY** - No exceptions. Never use `Console.WriteLine` alone for errors.

## Current Integration Status

### Explore.API ✅ (To be implemented)
- Sentry SDK integration
- Controller error handling
- MediatR pipeline instrumentation
- Database performance monitoring

### Explore.Blazor 🟡 (To be implemented)
- Blazor error boundary
- Component lifecycle errors
- SignalR connection errors

## Sentry Integration Patterns

### 1. API Controller Error Handling

**Pattern**: Use try-catch with Sentry capture in all controller actions.

```csharp
// Explore.API/Controllers/EventController.cs
using Sentry;

[Route("api/v1/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventController> _logger;

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateEventDto dto)
    {
        try
        {
            var command = new CreateEventCommand { EventDto = dto };
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                // Business validation failures
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Capture exception to Sentry
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("controller", "EventController");
                scope.SetTag("action", "Create");
                scope.SetTag("userId", User.FindFirst("sub")?.Value ?? "anonymous");
                scope.SetExtra("dto", dto);
            });

            _logger.LogError(ex, "Error creating event");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
```

### 2. MediatR Handler Error Handling

**Pattern**: Wrap handler logic in try-catch, capture to Sentry.

```csharp
// Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs
using Sentry;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        try
        {
            // Validation
            var validator = new CreateEventDtoValidator(...);
            var validationResult = await validator.ValidateAsync(request.EventDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            // Business logic
            var @event = _mapper.Map<Event>(request.EventDto);
            @event.TotalViews = 0;
            @event = await _eventRepository.Create(@event);

            response.Success = true;
            response.Id = @event.Id;
            response.Message = "Event created successfully.";

            return response;
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "CreateEventCommandHandler");
                scope.SetTag("command", "CreateEventCommand");
                scope.SetExtra("eventDto", request.EventDto);
            });

            response.Success = false;
            response.Message = "An error occurred while creating the event.";
            response.Errors = new List<string> { ex.Message };
            return response;
        }
    }
}
```

### 3. Database Performance Monitoring

**Pattern**: Wrap database operations with Sentry spans.

```csharp
// Explore.Persistence/Repositories/EventRepository.cs
using Sentry;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    public async Task<List<Event>> GetEventsWithDetails()
    {
        var transaction = SentrySdk.StartTransaction("repository.get-events-with-details", "db.query");
        var span = transaction.StartChild("db.query", "GetEventsWithDetails");

        try
        {
            var events = await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Actor)
                .Include(e => e.EventStatus)
                .ToListAsync();

            span.Finish(SpanStatus.Ok);
            return events;
        }
        catch (Exception ex)
        {
            span.Finish(SpanStatus.InternalError);
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("repository", "EventRepository");
                scope.SetTag("method", "GetEventsWithDetails");
            });
            throw;
        }
        finally
        {
            transaction.Finish();
        }
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
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("component", "Events");
                scope.SetTag("error-boundary", "true");
            });
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
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("component", "Events");
                scope.SetTag("lifecycle", "OnInitializedAsync");
            });
            throw; // Let ErrorBoundary handle it
        }
    }
}
```

### 5. ASP.NET Core Middleware Integration

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

## Error Levels

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

## Required Context

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

## Configuration (appsettings.json)

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

## Performance Monitoring

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

## Implementation Checklist

When adding Sentry to new code:

- [ ] Added Sentry NuGet package reference
- [ ] Configured Sentry in Program.cs
- [ ] All try/catch blocks capture to Sentry
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

## Testing Sentry Integration

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

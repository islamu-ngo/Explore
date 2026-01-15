# Database Performance Monitoring

Effective database performance monitoring is crucial for identifying and resolving bottlenecks in your application. This document outlines patterns for tracking database query performance within the ISLAMU Event project, leveraging .NET's `ActivitySource` for OpenTelemetry compatibility.

---

## 1. Using `ActivitySource` for Tracing

`ActivitySource` is part of `System.Diagnostics` and is the recommended way to create custom traces that can be integrated with OpenTelemetry. These traces are visible in tools like the Aspire dashboard, making it easy to visualize performance.

### `ActivitySource` Setup

You typically define a static `ActivitySource` instance per logical component (e.g., your Persistence layer).

**File**: `Explore.Persistence/ActivitySourceProvider.cs` (New File)

```csharp
using System.Diagnostics;

namespace Explore.Persistence;

/// <summary>
/// Provides a centralized ActivitySource for tracing operations within the Persistence layer.
/// This enables custom spans for database interactions to be captured by OpenTelemetry.
/// </summary>
public static class ActivitySourceProvider
{
    // Define an ActivitySource instance for the Persistence layer.
    // The name should be unique and descriptive.
    public static readonly ActivitySource PersistenceActivitySource = new("Explore.Persistence");
}
```

---

## 2. Manual Span Creation in Repositories

Wrap database operations in `using (var activity = ...)` blocks to create spans that track their duration and context.

**File**: `Explore.Persistence/Repositories/EventRepository.cs`

```csharp
using System.Diagnostics; // Required for ActivitySource
using Explore.Application.Contracts.Persistence; // Assuming this defines IEventRepository
using Explore.Domain; // Assuming this contains the Event entity
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Persistence.Repositories;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Event>> GetEventsWithDetails()
    {
        // ✅ Create a new Activity (span) for this specific database operation.
        // The span name should be descriptive.
        using var activity = ActivitySourceProvider.PersistenceActivitySource.StartActivity(
            "EventRepository.GetEventsWithDetails",
            ActivityKind.Internal // Indicates an internal operation within the service
        );

        // Optional: Add tags to the activity for more context.
        activity?.AddTag("db.operation", "SELECT");
        activity?.AddTag("db.collection", "Events");
        // activity?.AddTag("tenant.id", _tenantContext.TenantId); // Example: if tenant context is available

        try
        {
            // Execute the actual database query
            var events = await _dbContext.Events
                .Include(e => e.EventType)
                .Include(e => e.AudienceGender)
                .Include(e => e.AudienceAge)
                .Include(e => e.Actor)
                .Include(e => e.EventStatus)
                .ToListAsync();

            // Optional: Set the status of the activity on success.
            activity?.SetStatus(ActivityStatusCode.Ok);

            return events;
        }
        catch (System.Exception ex)
        {
            // Optional: Set the status of the activity on error.
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex); // Record the exception with the activity
            throw; // Re-throw the exception for higher-level handling
        }
    }
    
    // ... other repository methods would follow a similar pattern ...
}
```

---

## 3. Integrating with OpenTelemetry (Conceptual)

To make these custom activities visible, your application's `Program.cs` needs to configure an OpenTelemetry `TracerProvider`. This involves adding instrumentation for ASP.NET Core, Entity Framework Core, and your custom `ActivitySource`.

**File**: `Explore.API/Program.cs` (Conceptual, requires OpenTelemetry packages)

```csharp
// Example: Add OpenTelemetry packages
// <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="..." />
// <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="..." />
// <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="..." />
// <PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="..." />

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Explore.Persistence; // For ActivitySourceProvider

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource(ActivitySourceProvider.PersistenceActivitySource.Name) // ✅ Add your custom ActivitySource
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(builder.Environment.ApplicationName))
            .AddAspNetCoreInstrumentation() // Automatically instrument ASP.NET Core requests
            .AddEntityFrameworkCoreInstrumentation() // Automatically instrument EF Core database calls
            .AddOtlpExporter(otlpOptions =>
            {
                // Configure OTLP exporter to send traces to an OpenTelemetry collector (e.g., Jaeger, Prometheus)
                // otlpOptions.Endpoint = new Uri("http://localhost:4317");
            });
    });

// ... rest of Program.cs ...
```

---

## 4. Benefits of this Pattern

*   **Granular Performance Insight**: Provides detailed timing information for specific database operations beyond what automatic EF Core instrumentation might offer.
*   **Correlation**: Custom spans are automatically correlated with parent HTTP requests, making it easy to trace a user request from the API gateway down to individual database calls.
*   **Observability Platform Agnostic**: `ActivitySource` is a standard .NET mechanism that works with any OpenTelemetry-compatible collector and dashboard (e.g., Jaeger, Zipkin, Prometheus, Aspire dashboard).
*   **Troubleshooting**: Helps identify slow queries, N+1 problems, or other database-related performance issues.

---

## 5. Key Considerations

*   **Automatic EF Core Instrumentation**: OpenTelemetry already provides instrumentation for Entity Framework Core that captures basic database operations. Manual `ActivitySource` spans are useful for grouping multiple EF Core operations or adding custom context.
*   **Performance Overhead**: While generally low, adding excessive manual spans can introduce some overhead. Use judiciously for critical paths.
*   **Context Propagation**: `ActivitySource` automatically handles context propagation (e.g., `TraceId`, `SpanId`), ensuring that your custom spans are correctly nested within the overall trace.

---

**Related Resources**:
- [api-exception-handling.md](api-exception-handling.md) - Context for overall API observability.
- [`dotnet-efcore-guidelines`](../../dotnet-efcore-guidelines/SKILL.md) - General EF Core best practices.

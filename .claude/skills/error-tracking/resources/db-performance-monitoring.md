# Database Performance Monitoring

> **Project-Agnostic Performance Monitoring Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

Effective database performance monitoring is crucial for identifying and resolving bottlenecks in your application. This document outlines patterns for tracking database query performance, leveraging .NET's `ActivitySource` for OpenTelemetry compatibility.

---

## 1. Using `ActivitySource` for Tracing

`ActivitySource` is part of `System.Diagnostics` and is the recommended way to create custom traces that can be integrated with OpenTelemetry. These traces are visible in tools like the Aspire dashboard, making it easy to visualize performance.

### `ActivitySource` Setup

You typically define a static `ActivitySource` instance per logical component (e.g., your Persistence layer).

**File**: `{Project}.Persistence/ActivitySourceProvider.cs` (New File)

```csharp
using System.Diagnostics;

namespace {Project}.Persistence;

/// <summary>
/// Provides a centralized ActivitySource for tracing operations within the Persistence layer.
/// This enables custom spans for database interactions to be captured by OpenTelemetry.
/// </summary>
public static class ActivitySourceProvider
{
    // Define an ActivitySource instance for the Persistence layer.
    // The name should be unique and descriptive.
    public static readonly ActivitySource PersistenceActivitySource = new("{Project}.Persistence");
}
```

---

## 2. Manual Span Creation in Repositories

Wrap database operations in `using (var activity = ...)` blocks to create spans that track their duration and context.

**File**: `{Project}.Persistence/Repositories/{Entity}Repository.cs`

```csharp
using System.Diagnostics;
using {Project}.Application.Contracts.Persistence;
using {Project}.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace {Project}.Persistence.Repositories;

public class {Entity}Repository : GenericRepository<{Entity}, {IdType}>, I{Entity}Repository
{
    private readonly {DbContext} _dbContext;

    public {Entity}Repository({DbContext} dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        // ✅ Create a new Activity (span) for this specific database operation.
        using var activity = ActivitySourceProvider.PersistenceActivitySource.StartActivity(
            "{Entity}Repository.Get{Entities}WithDetails",
            ActivityKind.Internal
        );

        // Optional: Add tags to the activity for more context.
        activity?.AddTag("db.operation", "SELECT");
        activity?.AddTag("db.collection", "{Entities}");
        // activity?.AddTag("tenant.id", _tenantContext.TenantId);

        try
        {
            var {entities} = await _dbContext.{Entities}
                .Include(e => e.{LookupEntity})
                .Include(e => e.{RelatedEntity1})
                .Include(e => e.{ParentEntity})
                .Include(e => e.Status)
                .ToListAsync();

            // Optional: Set the status of the activity on success.
            activity?.SetStatus(ActivityStatusCode.Ok);

            return {entities};
        }
        catch (System.Exception ex)
        {
            // Optional: Set the status of the activity on error.
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    // ... other repository methods would follow a similar pattern ...
}
```

---

## 3. Integrating with OpenTelemetry (Conceptual)

To make these custom activities visible, your application's `Program.cs` needs to configure an OpenTelemetry `TracerProvider`. This involves adding instrumentation for ASP.NET Core, Entity Framework Core, and your custom `ActivitySource`.

**File**: `{Project}.API/Program.cs` (Conceptual, requires OpenTelemetry packages)

```csharp
// Example: Add OpenTelemetry packages
// <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="..." />
// <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="..." />
// <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="..." />
// <PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="..." />

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using {Project}.Persistence;

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
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(otlpOptions =>
            {
                // Configure OTLP exporter to send traces to an OpenTelemetry collector
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

# Sentry Middleware Configuration (Conceptual)

> **Project-Agnostic Sentry Integration Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

This document provides conceptual guidance on integrating Sentry into ASP.NET Core applications. This integration aims to automatically capture exceptions, log messages, and collect performance data.

---

## 1. Sentry Integration in `Program.cs`

Sentry integration typically involves configuring the Sentry SDK early in your application's `Program.cs` file. This setup enables automatic error capturing and performance monitoring for your ASP.NET Core application.

**File**: `{Project}.API/Program.cs` (or `{Project}.Blazor/Program.cs` for Blazor Server)

```csharp
// Example: Add Sentry NuGet packages
// <PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
// <PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" />
// <PackageReference Include="Sentry.Serilog" Version="4.0.0" /> // If using Serilog

using Sentry;
using Sentry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Sentry for the Web Host
builder.WebHost.UseSentry(options =>
{
    // Configure your DSN (Data Source Name) - this is crucial!
    options.Dsn = builder.Configuration["Sentry:Dsn"];

    // Set the application environment
    options.Environment = builder.Environment.EnvironmentName;

    // Set the release version for better tracking in Sentry
    // options.Release = "my-app@1.0.0";

    // Performance Monitoring configuration
    options.TracesSampleRate = 0.1;
    options.ProfilesSampleRate = 0.1;

    // Optional: Enable automatic session tracking
    options.AutoSessionTracking = true;

    // Optional: Enables Sentry to capture more global context
    options.IsGlobalModeEnabled = true;

    // Optional: Custom filtering or data modification before sending to Sentry
    options.BeforeSend = (sentryEvent) =>
    {
        // ✅ Remove sensitive information from HTTP request headers
        if (sentryEvent.Request?.Headers != null)
        {
            sentryEvent.Request.Headers.Remove("Authorization");
            sentryEvent.Request.Headers.Remove("Cookie");
        }

        return sentryEvent;
    };
});

var app = builder.Build();

// ✅ Add Sentry middleware EARLY in the pipeline for comprehensive tracking
app.UseSentryTracing();
app.UseSentryTracing().UseSentryRequestErrorCatching();

// ... rest of your application's middleware pipeline ...
```

---

## 2. Using `SentrySdk` Directly

For manual error capturing, adding custom context, or performance monitoring outside of standard HTTP requests, you can use the `SentrySdk` static class directly.

### Capturing Exceptions

```csharp
using Sentry;

try
{
    // ... code that might throw an exception ...
    throw new InvalidOperationException("Something bad happened!");
}
catch (Exception ex)
{
    // ✅ Capture the exception to Sentry
    SentrySdk.CaptureException(ex, scope =>
    {
        // ✅ Add additional context to the error report
        scope.User = new User { Id = "user-123", Email = "user@example.com" };
        scope.SetTag("feature", "{entity}-creation");
        scope.SetTag("tenant_id", "tenant-abc");
        scope.SetContext("request_data", new { Title = "My {Entity}", UserAgent = "..." });
        scope.Level = SentryLevel.Error;
    });
    throw;
}
```

### Capturing Messages

```csharp
using Sentry;

// ✅ Capture an informational message
SentrySdk.CaptureMessage("User logged in successfully.", SentryLevel.Info);

// ✅ Capture a warning message
SentrySdk.CaptureMessage("Low disk space warning on server.", SentryLevel.Warning);
```

### Manual Performance Transactions and Spans

For background jobs or specific code blocks where you want to track performance, you can create manual Sentry transactions and spans.

```csharp
using Sentry;

public class BackgroundSyncService
{
    public async Task SyncDataAsync()
    {
        // ✅ Start a new Sentry transaction for the background operation
        var transaction = SentrySdk.StartTransaction("background.job.sync_{entities}", "sync");
        transaction.Description = "Synchronizing {entities} from external API";

        try
        {
            // ✅ Start a child span for a specific part of the operation
            var fetchSpan = transaction.StartChild("http.client", "fetch_external_{entities}");
            try
            {
                await Task.Delay(500);
                fetchSpan.SetTag("http.url", "https://external.api/{entities}");
                fetchSpan.SetTag("http.method", "GET");
            }
            finally
            {
                fetchSpan.Finish(SpanStatus.Ok);
            }

            var dbSpan = transaction.StartChild("db.query", "save_{entities}_to_db");
            try
            {
                await Task.Delay(800);
            }
            finally
            {
                dbSpan.Finish(SpanStatus.Ok);
            }

            transaction.Status = SpanStatus.Ok;
        }
        catch (Exception ex)
        {
            transaction.Status = SpanStatus.InternalError;
            transaction.SetTag("error", "true");
            SentrySdk.CaptureException(ex, scope => scope.Transaction = transaction);
            throw;
        }
        finally
        {
            transaction.Finish();
        }
    }
}
```

---

## 3. Configuration (from `appsettings.json`)

Sentry settings are typically managed through `appsettings.json`, allowing easy modification across environments.

```json
{
  "Sentry": {
    "Dsn": "https://your-public-key@o000000.ingest.sentry.io/0000000",
    "Environment": "Development",
    "Release": "1.0.0",
    "Debug": false,
    "DiagnosticLevel": "Error",
    "TracesSampleRate": 1.0,
    "ProfilesSampleRate": 1.0,
    "SendDefaultPii": false,
    "AttachStackTraces": true
  }
}
```

---

## 4. NuGet Packages

Ensure the necessary Sentry NuGet packages are installed in the relevant projects.

### `{Project}.API`
```xml
<PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
<PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" />
```

### `{Project}.Blazor` (for Blazor Server)
```xml
<PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
<PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" />
```

### `{Project}.Blazor.Client` (for Blazor WebAssembly)
```xml
<PackageReference Include="Sentry.Blazor.WebAssembly" Version="4.0.0" />
<PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" />
```

### `{Project}.Application` (Optional, if using `SentrySdk` for business logic exceptions)
```xml
<PackageReference Include="Sentry" Version="4.0.0" />
```

---

## 5. Key Considerations

*   **Sensitive Data**: Always be mindful of sensitive data (PII, secrets, tokens) and configure `BeforeSend` to strip it from payloads.
*   **Performance Overhead**: While Sentry is optimized, capturing too many transactions or very large payloads can introduce overhead. Adjust `TracesSampleRate` and `ProfilesSampleRate` based on your performance needs.
*   **Error Reporting Discipline**: Ensure that exceptions are logged and reported consistently, preferably through the centralized exception handler. Avoid swallowing errors silently.
*   **Sentry DSN**: The DSN is your project's unique identifier in Sentry. It must be kept secret and should not be committed to source control directly (use environment variables or secret management).

---

**Related Resources**:
- [api-exception-handling.md](api-exception-handling.md) - Context for how Sentry can integrate with centralized API error handling.
- [db-performance-monitoring.md](db-performance-monitoring.md) - How Sentry can complement OpenTelemetry for database performance.
- [`blazor-error-boundary.md`](blazor-error-boundary.md) - How Sentry can capture errors caught by Blazor's `ErrorBoundary`.

# Sentry Middleware Configuration (Conceptual)

This document provides conceptual guidance on integrating Sentry into ASP.NET Core applications within the ISLAMU Event project. This integration aims to automatically capture exceptions, log messages, and collect performance data.

---

## 1. Sentry Integration in `Program.cs`

Sentry integration typically involves configuring the Sentry SDK early in your application's `Program.cs` file. This setup enables automatic error capturing and performance monitoring for your ASP.NET Core application.

**File**: `Explore.API/Program.cs` (or `Explore.Blazor/Program.cs` for Blazor Server)

```csharp
// Example: Add Sentry NuGet packages
// <PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
// <PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" /> // If using ILogger
// <PackageReference Include="Sentry.Serilog" Version="4.0.0" /> // If using Serilog

using Sentry; // For SentrySdk, SentryLevel, SpanStatus
using Sentry.AspNetCore; // For UseSentry

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Sentry for the Web Host
builder.WebHost.UseSentry(options =>
{
    // Configure your DSN (Data Source Name) - this is crucial!
    // Get this from your Sentry project settings.
    options.Dsn = builder.Configuration["Sentry:Dsn"];

    // Set the application environment (e.g., "production", "development", "staging")
    options.Environment = builder.Environment.EnvironmentName;

    // Set the release version for better tracking in Sentry
    // options.Release = "my-app@1.0.0"; // Consider using Assembly.GetEntryAssembly().GetName().Version.ToString()

    // Performance Monitoring configuration
    // Sample rate for transactions (0.0 to 1.0) - e.g., 0.1 means 10% of requests are sampled
    options.TracesSampleRate = 0.1;
    // Sample rate for profiling (0.0 to 1.0)
    options.ProfilesSampleRate = 0.1;

    // Optional: Enable automatic session tracking
    options.AutoSessionTracking = true;

    // Optional: Enables Sentry to capture more global context (e.g., AppDomain exceptions)
    options.IsGlobalModeEnabled = true;

    // Optional: Set a maximum number of breadcrumbs to record. Default is 100.
    // options.MaxBreadcrumbs = 50;

    // Optional: Custom filtering or data modification before sending to Sentry
    options.BeforeSend = (sentryEvent) =>
    {
        // ✅ Remove sensitive information from HTTP request headers
        if (sentryEvent.Request?.Headers != null)
        {
            sentryEvent.Request.Headers.Remove("Authorization");
            sentryEvent.Request.Headers.Remove("Cookie");
            // Add any other sensitive headers to remove
        }
        // ✅ Add custom tags or context based on the event
        // sentryEvent.SetTag("custom_tag", "value");

        return sentryEvent;
    };

    // Optional: Custom filtering or modification for transactions
    // options.BeforeSendTransaction = (transaction) => { /* ... */ return transaction; };

    // Optional: If you want to filter out certain types of events or messages
    // options.SetBeforeBreadcrumb((breadcrumb) => { /* ... */ return breadcrumb; });

    // Optional: Set the minimum event level to send to Sentry
    // options.MinimumEventLevel = LogLevel.Warning; // e.g., only send Warning and above
});

var app = builder.Build();

// ✅ Add Sentry middleware EARLY in the pipeline for comprehensive tracking
// This middleware captures unhandled exceptions and HTTP request information.
app.UseSentryTracing(); // For performance monitoring (must be before UseRouting)
app.UseSentryTracing().UseSentryRequestErrorCatching(); // Combines both

// ... rest of your application's middleware pipeline ...
// app.UseHttpsRedirection();
// app.UseStaticFiles();
// app.UseRouting();
// app.UseAuthentication();
// app.UseAuthorization();
// app.MapControllers();
```

---

## 2. Using `SentrySdk` Directly

For manual error capturing, adding custom context, or performance monitoring outside of standard HTTP requests, you can use the `SentrySdk` static class directly.

### Capturing Exceptions

```csharp
using Sentry; // For SentrySdk, SentryLevel

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
        scope.SetTag("feature", "event-creation");
        scope.SetTag("tenant_id", "tenant-abc");
        scope.SetContext("request_data", new { EventTitle = "My Event", UserAgent = "..." });
        scope.Level = SentryLevel.Error; // Set custom severity level
    });
    throw; // Re-throw to maintain the application's normal error flow
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
using Sentry; // For SentrySdk, SpanStatus

public class BackgroundSyncService
{
    public async Task SyncDataAsync()
    {
        // ✅ Start a new Sentry transaction for the background operation
        var transaction = SentrySdk.StartTransaction("background.job.sync_events", "sync");
        transaction.Description = "Synchronizing events from external API";

        try
        {
            // ✅ Start a child span for a specific part of the operation
            var fetchSpan = transaction.StartChild("http.client", "fetch_external_events");
            try
            {
                // Simulate external API call
                await Task.Delay(500);
                fetchSpan.SetTag("http.url", "https://external.api/events");
                fetchSpan.SetTag("http.method", "GET");
            }
            finally
            {
                fetchSpan.Finish(SpanStatus.Ok); // Finish the child span
            }

            var dbSpan = transaction.StartChild("db.query", "save_events_to_db");
            try
            {
                // Simulate database operation
                await Task.Delay(800);
            }
            finally
            {
                dbSpan.Finish(SpanStatus.Ok); // Finish the child span
            }

            transaction.Status = SpanStatus.Ok; // Set transaction status to OK on success
        }
        catch (Exception ex)
        {
            transaction.Status = SpanStatus.InternalError; // Set transaction status to error
            transaction.SetTag("error", "true");
            SentrySdk.CaptureException(ex, scope => scope.Transaction = transaction); // Link exception to transaction
            throw;
        }
        finally
        {
            transaction.Finish(); // ✅ Finish the transaction
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
    "Environment": "Development", // Automatically overridden by builder.Environment.EnvironmentName
    "Release": "1.0.0", // Optional: specify app version
    "Debug": false, // Enables internal Sentry SDK logging
    "DiagnosticLevel": "Error", // Minimum log level for Sentry's internal logging
    "TracesSampleRate": 1.0, // Sample 100% of transactions in development/testing
    "ProfilesSampleRate": 1.0, // Sample 100% of profiles in development/testing
    "SendDefaultPii": false, // Do not send personally identifiable information by default
    "AttachStackTraces": true // Attach stack traces to log messages
  }
}
```

---

## 4. Nuget Packages

Ensure the necessary Sentry NuGet packages are installed in the relevant projects.

### `Explore.API`
```xml
<PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
<PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" />
```

### `Explore.Blazor` (for Blazor Server)
```xml
<PackageReference Include="Sentry.AspNetCore" Version="4.0.0" />
<PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" />
```

### `Explore.Blazor.Client` (for Blazor WebAssembly)
```xml
<PackageReference Include="Sentry.Blazor.WebAssembly" Version="4.0.0" />
<PackageReference Include="Sentry.Extensions.Logging" Version="4.0.0" />
```

### `Explore.Application` (Optional, if using `SentrySdk` for business logic exceptions)
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

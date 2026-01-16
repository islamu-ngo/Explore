# Testing Sentry Integration

This document provides example endpoints for testing the Sentry integration in the `Explore.API` project, allowing you to verify that errors and performance metrics are correctly being captured by Sentry.

---

## 1. API Test Endpoints

These endpoints can be added temporarily to your `EventController` or a dedicated test controller to trigger specific Sentry events. **Remember to remove these endpoints before deploying to production.**

**File**: `Explore.API/Controllers/EventController.cs` (Temporary addition)

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentry; // Required for SentrySdk, SpanStatus, SentryLevel

namespace Explore.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    // ... existing constructor and methods ...

    /// <summary>
    /// TEST ENDPOINT: Triggers an unhandled exception to test Sentry error capture.
    /// Access: Anonymous (for easy testing)
    /// </summary>
    [HttpGet("sentry/test-error")]
    [AllowAnonymous]
    public IActionResult TestSentryError()
    {
        try
        {
            // Simulate an operation that causes an exception
            throw new InvalidOperationException("This is a test Sentry exception from the API!");
        }
        catch (Exception ex)
        {
            // ✅ Manually capture the exception with additional context for verification
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("test", "true");
                scope.SetTag("endpoint", "sentry/test-error");
                scope.User = new User { Id = "test-user-123", Email = "test@example.com", Username = "sentry-tester" };
                scope.SetContext("request_info", new { RequestPath = HttpContext.Request.Path });
                scope.Level = SentryLevel.Error; // Explicitly set level
            });
            throw; // Re-throw to ensure it's caught by the UseExceptionHandler middleware (if configured)
        }
    }

    /// <summary>
    /// TEST ENDPOINT: Simulates a slow operation to test Sentry performance monitoring.
    /// Access: Anonymous (for easy testing)
    /// </summary>
    [HttpGet("sentry/test-performance")]
    [AllowAnonymous]
    public async Task<IActionResult> TestPerformance()
    {
        // ✅ Start a new Sentry transaction for this test operation
        var transaction = SentrySdk.StartTransaction("test.performance", "manual-api-endpoint");
        transaction.Description = "Simulates a slow API endpoint for performance testing";

        try
        {
            // Simulate some work with a delay
            await Task.Delay(1500); // 1.5 second delay

            // ✅ Add a child span for a specific part of the operation
            var dbSpan = transaction.StartChild("db.query", "simulate_db_call");
            await Task.Delay(500); // Simulate a database call
            dbSpan.Finish(SpanStatus.Ok); // Mark child span as successful

            transaction.Status = SpanStatus.Ok; // Mark transaction as successful
            return Ok(new { message = "Sentry performance test completed successfully." });
        }
        catch (Exception ex)
        {
            transaction.Status = SpanStatus.InternalError; // Mark transaction as an error
            SentrySdk.CaptureException(ex, scope => scope.Transaction = transaction); // Link exception to transaction
            return StatusCode(500, new { message = "Sentry performance test failed.", error = ex.Message });
        }
        finally
        {
            transaction.Finish(); // ✅ Always finish the transaction
        }
    }
}
```

---

## 2. Test Commands

Use `curl` or your preferred HTTP client to hit these endpoints. Replace `https://localhost:7001` with your API's base URL.

### Test Error Capture

```bash
curl -v https://localhost:7001/api/v1/event/sentry/test-error
```

*Expected Sentry Outcome*: An error event should appear in your Sentry dashboard, with tags like `test:true` and `endpoint:sentry/test-error`, and the message "This is a test Sentry exception from the API!". If you have `UseExceptionHandler` configured, the client will receive a `ProblemDetails` response.

### Test Performance Tracking

```bash
curl -v https://localhost:7001/api/v1/event/sentry/test-performance
```

*Expected Sentry Outcome*: A transaction named `test.performance` with operation `manual-api-endpoint` should appear in your Sentry dashboard's "Performance" section. The duration should be around 1.5 seconds, and you should see a child span named `db.query` (simulate_db_call) within it.

---

## 3. Verifying Sentry Dashboard

After executing the test commands, log in to your Sentry dashboard for the configured project and:

*   **Check "Issues"**: Look for the test exception triggered by `/sentry/test-error`. Verify the message, stack trace, and any custom tags/context you added.
*   **Check "Performance"**: Look for the `test.performance` transaction triggered by `/sentry/test-performance`. Verify its duration and that it contains the `db.query` child span.

---

**Important Notes**:
*   Always ensure your Sentry DSN is correctly configured in `appsettings.json` or environment variables for the environment you are testing.
*   These test endpoints should *never* be deployed to a production environment. Remove them before release.
*   This documentation assumes Sentry SDK and middleware are already integrated as described in [sentry-middleware-config.md](sentry-middleware-config.md).

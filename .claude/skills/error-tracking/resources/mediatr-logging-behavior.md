# MediatR Handler Logging Behavior

> **Project-Agnostic Logging Behavior Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

This document describes how to implement a logging behavior for MediatR requests using a pipeline behavior. This pattern allows for centralized logging of request processing, providing insight into the application's flow without cluttering individual handlers.

---

## 1. `LoggingBehavior` Implementation

Create a class that implements `IPipelineBehavior<TRequest, TResponse>`. This class will intercept all MediatR requests before they are handled and after they complete.

**File**: `{Project}.Application/Behaviors/LoggingBehavior.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace {Project}.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior for logging requests and responses.
/// </summary>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the MediatR response.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Log information BEFORE the request is handled
        logger.LogInformation("Handling MediatR Request: {RequestName} ({RequestType})",
            typeof(TRequest).Name, typeof(TRequest).FullName);

        TResponse response;
        try
        {
            // Call the next delegate in the pipeline, which eventually invokes the handler
            response = await next();
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "MediatR Request: {RequestName} ({RequestType}) failed with exception: {ErrorMessage}",
                typeof(TRequest).Name, typeof(TRequest).FullName, ex.Message);
            throw; // Re-throw for higher-level exception handlers
        }

        // Log information AFTER the request has been successfully handled
        logger.LogInformation("Handled MediatR Request: {RequestName} ({RequestType}) - Response: {ResponseType}",
            typeof(TRequest).Name, typeof(TRequest).FullName, typeof(TResponse).Name);

        return response;
    }
}
```

---

## 2. Registering the Behavior

The `LoggingBehavior` needs to be registered with MediatR in the application's `Program.cs` file.

**File**: `{Project}.API/Program.cs` (or `{Project}.Blazor/Program.cs` if using MediatR in Blazor)

```csharp
using MediatR;
using {Project}.Application.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// Add MediatR services to the DI container
builder.Services.AddMediatR(cfg => {
    // Register the assembly containing your handlers and requests
    cfg.RegisterServicesFromAssembly(typeof({Project}.Application.ApplicationServicesRegistration).Assembly);

    // Register the LoggingBehavior as a pipeline behavior
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

---

## 3. Benefits of this Pattern

*   **Centralized Logging**: All request processing logs are consolidated in one place, making it easier to track the flow of operations.
*   **Decoupled Concerns**: Handlers remain clean, focusing solely on business logic, while logging is handled by the pipeline.
*   **Debugging and Monitoring**: Provides a clear audit trail of what requests are being processed and their outcomes, aiding in debugging and monitoring.
*   **Error Visibility**: Catches and logs exceptions at the pipeline level, ensuring that any unhandled errors during request processing are recorded.

---

## 4. Key Considerations

*   **Performance**: While generally efficient, be mindful of what you log within the behavior. Logging large request/response objects can impact performance and log storage.
*   **Sensitive Data**: Be cautious about logging sensitive data from requests or responses. Implement redaction or filtering if necessary.
*   **Error Handling Flow**: Exceptions caught in the behavior are re-thrown (`throw;`). This is crucial to ensure they propagate to higher-level exception handlers (like `UseExceptionHandler` in the API) for a consistent error response to the client.

---

**Related Resources**:
- [api-exception-handling.md](api-exception-handling.md) - How exceptions caught by this behavior are eventually handled by the API.
- [`cqrs-mediatr-guidelines`](../../cqrs-mediatr-guidelines/SKILL.md) - General MediatR guidelines.

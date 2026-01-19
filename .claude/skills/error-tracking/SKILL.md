---
name: error-tracking
description: Add Sentry error tracking and performance monitoring to .NET services. Use this skill when adding error handling, creating new controllers, or tracking performance. ALL ERRORS MUST BE CAPTURED - no exceptions.
type: guardrail
enforcement: suggest
priority: high
---

# Error Tracking & Observability Guidelines

> **Project-Agnostic Error Tracking Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose

This skill provides guidelines for implementing robust error tracking and performance monitoring across .NET services (API, Blazor). It outlines patterns for centralized exception handling, logging, tracing, and Sentry integration.

## When This Skill Activates

**Triggered by**:
- Keywords: "error handling", "exception", "sentry", "logging", "performance", "tracing", "problem details", "observability"
- Intent patterns: "add error logging", "implement try-catch", "monitor API performance", "handle UI errors"
- File patterns: `**/Program.cs`, `**/*Controller.cs`, `**/*Handler.cs`, `**/*Repository.cs`, `**/*.razor`

## CRITICAL RULE: Do Not Swallow Exceptions!

All errors **MUST** be handled gracefully. Use structured logging (`ILogger`) and centralized exception handling. When Sentry is integrated, capture all exceptions.

## Resources

*For detailed implementation examples, refer to the `resources/` folder within this skill.*

| Resource | Description |
|----------|-------------|
| [api-exception-handling.md](resources/api-exception-handling.md) | Centralized API exception handling using `UseExceptionHandler` and `ProblemDetails` (RFC 7807). |
| [mediatr-logging-behavior.md](resources/mediatr-logging-behavior.md) | MediatR pipeline behavior for centralized logging and error capturing in handlers. |
| [db-performance-monitoring.md](resources/db-performance-monitoring.md) | Database performance tracing using `ActivitySource` for OpenTelemetry compatibility. |
| [blazor-error-boundary.md](resources/blazor-error-boundary.md) | Implementing graceful UI error handling in Blazor with the `ErrorBoundary` component. |
| [sentry-middleware-config.md](resources/sentry-middleware-config.md) | Conceptual guidance for Sentry SDK and middleware integration in ASP.NET Core. |
| [sentry-testing-endpoints.md](resources/sentry-testing-endpoints.md) | Example API endpoints for testing Sentry integration (error capture, performance). |

## Quick Reference

### 1. Centralized API Exception Handling

API unhandled exceptions are caught and transformed into RFC 7807 `ProblemDetails` responses.

```csharp
// {Project}.API/Program.cs (Simplified)
app.UseExceptionHandler(exceptionHandlerApp => { /* ... */ });
```
*For complete code, see [api-exception-handling.md](resources/api-exception-handling.md).*

### 2. MediatR Handler Logging & Error Capturing

A `LoggingBehavior` in the MediatR pipeline ensures all requests are logged and exceptions are captured.

```csharp
// {Project}.Application/Behaviors/LoggingBehavior.cs (Simplified)
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{ /* ... */ }
```
*For complete code, see [mediatr-logging-behavior.md](resources/mediatr-logging-behavior.md).*

### 3. Database Performance Monitoring

Use `ActivitySource` for custom spans to trace database operations, integrating with OpenTelemetry.

```csharp
// {Project}.Persistence/Repositories/{Entity}Repository.cs (Simplified)
using System.Diagnostics;
public class {Entity}Repository : I{Entity}Repository
{
    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        using var activity = ActivitySourceProvider.PersistenceActivitySource.StartActivity("...");
        // ... EF Core query ...
    }
}
```
*For complete code, see [db-performance-monitoring.md](resources/db-performance-monitoring.md).*

### 4. Blazor UI Error Boundary

Gracefully handle unhandled UI errors in Blazor components, displaying a fallback UI and logging the error.

```razor
<!-- {Project}.Blazor/Components/Pages/{Entities}.razor (Simplified) -->
<ErrorBoundary>
    <ChildContent> <!-- Potentially error-prone content --> </ChildContent>
    <ErrorContent Context="ex"> <!-- Fallback UI --> </ErrorContent>
</ErrorBoundary>
```
*For complete code, see [blazor-error-boundary.md](resources/blazor-error-boundary.md).*

### 5. Sentry Integration (Conceptual)

When Sentry is integrated, `UseSentry` and `app.UseSentryTracing()` provide automatic error and performance tracking.

```csharp
// {Project}.API/Program.cs (Simplified)
builder.WebHost.UseSentry(options => { /* ... */ });
app.UseSentryTracing();
```
*For complete conceptual configuration and usage, see [sentry-middleware-config.md](resources/sentry-middleware-config.md).*

### 6. Testing Sentry Integration (Conceptual)

Dedicated endpoints can be used to verify Sentry's error and performance capturing capabilities.

```csharp
// {Project}.API/Controllers/{Entity}Controller.cs (Simplified)
[HttpGet("sentry/test-error")]
public IActionResult TestSentryError() { /* ... */ }
[HttpGet("sentry/test-performance")]
public async Task<IActionResult> TestPerformance() { /* ... */ }
```
*For complete test endpoint examples, see [sentry-testing-endpoints.md](resources/sentry-testing-endpoints.md).*

## Key Principles

*   **No Uncaught Exceptions**: All exceptions, whether from API, MediatR handlers, or Blazor UI, should be caught and logged.
*   **Structured Logging**: Use `ILogger` for all logging, leveraging structured logging capabilities.
*   **Contextual Information**: When reporting errors, always include relevant context (user ID, tenant ID, request data, tags) to aid in debugging.
*   **Performance First**: Monitor critical paths (database, external API calls) for performance bottlenecks.
*   **RFC 7807 Compliance**: Ensure API error responses are standardized using `ProblemDetails`.

---

**Related Skills**:
- [`clean-architecture-rules`](../clean-architecture-rules/SKILL.md) - For proper error handling layer placement.
- [`cqrs-mediatr-guidelines`](../cqrs-mediatr-guidelines/SKILL.md) - For error handling patterns within MediatR handlers.
- [`dotnet-efcore-guidelines`](../dotnet-efcore-guidelines/SKILL.md) - For database error handling and performance considerations.
- [`blazor-ui-conventions`](../blazor-ui-conventions/SKILL.md) - For UI error handling patterns.

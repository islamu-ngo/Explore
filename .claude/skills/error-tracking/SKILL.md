---
name: error-tracking
description: Implement error tracking and observability using Prometheus (metrics) and Loki (logs). Covers centralized exception handling, structured logging, and performance monitoring for .NET services.
type: guardrail
enforcement: suggest
priority: high
---

# Error Tracking & Observability Guidelines

> **Project-Agnostic Observability Patterns for .NET**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose

Guidelines for implementing **robust error tracking** and **performance monitoring** across .NET services using:
- **Prometheus** - Metrics collection and monitoring
- **Loki** - Centralized log aggregation
- **Structured Logging** - Consistent log formatting via Serilog
- **OpenTelemetry** - Distributed tracing and telemetry

## When This Skill Activates

**Triggered by**:
- Keywords: "error handling", "logging", "prometheus", "loki", "observability", "metrics", "tracing"
- Intent patterns: "add error logging", "monitor performance", "track exceptions", "implement telemetry"
- File patterns: `**/Program.cs`, `**/*Controller.cs`, `**/*Handler.cs`, `**/*Repository.cs`, `**/*.razor`

## CRITICAL RULE: Do Not Swallow Exceptions!

All errors **MUST** be handled gracefully with structured logging and centralized exception handling.

## Resources

*Detailed patterns are available in `resources/` (to be created) and [docs/CONFIGURATION.md](../../../docs/CONFIGURATION.md).*

| Resource | Description |
|----------|-------------|
| [api-exception-handling.md](resources/api-exception-handling.md) | Centralized API exception handling with RFC 7807 ProblemDetails |
| [mediatr-logging-behavior.md](resources/mediatr-logging-behavior.md) | MediatR pipeline behavior for request/response logging |
| [prometheus-metrics.md](resources/prometheus-metrics.md) | Exposing custom metrics via `/metrics` endpoint |
| [loki-logging.md](resources/loki-logging.md) | Structured logging to Loki via Serilog |
| [blazor-error-boundary.md](resources/blazor-error-boundary.md) | Graceful UI error handling with ErrorBoundary component |
| [opentelemetry-tracing.md](resources/opentelemetry-tracing.md) | Distributed tracing with ActivitySource |

## Quick Reference

### 1. Centralized API Exception Handling
- Use `app.UseExceptionHandler()` to catch unhandled API exceptions
- Transform exceptions into RFC 7807 `ProblemDetails` format
- Log exceptions with structured logging before returning response
- Return appropriate HTTP status codes (400, 404, 500)

*Details: [api-exception-handling.md](resources/api-exception-handling.md)*

### 2. MediatR Pipeline Logging
- Implement `IPipelineBehavior<TRequest, TResponse>` for centralized logging
- Log request start/completion with structured data (request name, duration)
- Capture and log exceptions from handlers
- Attach correlation IDs and tenant context to logs

*Details: [mediatr-logging-behavior.md](resources/mediatr-logging-behavior.md)*

### 3. Prometheus Metrics
- Expose `/metrics` endpoint using `app.MapPrometheusScrapingEndpoint()`
- Configure OpenTelemetry with `AddPrometheusExporter()`
- Track: HTTP request durations, database query times, command/query execution
- Use histograms for latency metrics (P50, P95, P99)

*Configuration: [docs/CONFIGURATION.md](../../../docs/CONFIGURATION.md) Prometheus section*

### 4. Loki Centralized Logging
- Configure Serilog with `GrafanaLoki` sink
- Add structured labels: `app`, `environment`, `tenant`
- Include correlation IDs for request tracing
- Ship logs asynchronously to avoid performance impact

*Configuration: [docs/CONFIGURATION.md](../../../docs/CONFIGURATION.md) Loki section*

### 5. Blazor Error Boundary
- Wrap components in `<ErrorBoundary>` to catch UI exceptions
- Provide `<ErrorContent>` fallback UI for graceful degradation
- Log caught exceptions with component context
- Display user-friendly error messages

*Implementation: [blazor-error-boundary.md](resources/blazor-error-boundary.md)*

### 6. OpenTelemetry Tracing
- Use `ActivitySource` for distributed tracing
- Create spans for database operations, external API calls, long-running processes
- Attach custom tags (entity ID, operation type)
- Integrate with Prometheus for performance monitoring

*Details: [opentelemetry-tracing.md](resources/opentelemetry-tracing.md)*

## Key Principles

*   **No Uncaught Exceptions**: All exceptions must be caught and logged with structured context
*   **Structured Logging**: Use Serilog with `ILogger<T>` for consistent log formatting
*   **Contextual Information**: Include user ID, tenant ID, correlation IDs, and request data in logs
*   **Metrics Over Logs**: Use Prometheus metrics for performance tracking; logs for debugging
*   **RFC 7807 Compliance**: API error responses must use `ProblemDetails` format
*   **Label-Based Filtering**: Add labels to Loki logs for efficient querying (app, env, tenant)
*   **Performance Monitoring**: Track P50/P95/P99 latencies for critical operations

---

**Related Skills**:
- [`clean-architecture-rules`](../clean-architecture-rules/SKILL.md) - For proper error handling layer placement.
- [`cqrs-mediatr-guidelines`](../cqrs-mediatr-guidelines/SKILL.md) - For error handling patterns within MediatR handlers.
- [`dotnet-efcore-guidelines`](../dotnet-efcore-guidelines/SKILL.md) - For database error handling and performance considerations.
- [`blazor-ui-conventions`](../blazor-ui-conventions/SKILL.md) - For UI error handling patterns.

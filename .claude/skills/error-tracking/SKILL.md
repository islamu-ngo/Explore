---
name: error-tracking
description: Implement error tracking and observability using Prometheus (metrics) and Loki (logs). Covers centralized exception handling, structured logging, and performance monitoring for .NET services.
type: guardrail
enforcement: suggest
priority: high
---

ABOUTME: Error handling + observability rules for .NET services.
ABOUTME: Read referenced resources before applying.

# Error Tracking & Observability Guidelines

> **Project-Agnostic Observability Patterns for .NET**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
Standardize error handling + observability with OpenTelemetry, Prometheus, Loki.

## When This Skill Activates
- Keywords: error handling, logging, observability, metrics, tracing
- File patterns: `**/Program.cs`, `**/*Controller.cs`, `**/*Handler.cs`, `**/*.razor`

## Non‑Inferable Rules (Must Follow)
- **Do not swallow exceptions.**
- Use RFC 7807 ProblemDetails for API errors.
- **Sentry is not used** — use OTEL + Prometheus + Loki.
- **Chained IExceptionHandler** (not middleware): `ValidationExceptionHandler` (handles `ValidationException` → 422) → `GlobalExceptionHandler` (catch‑all → 500 in prod, full detail in dev). Each returns `true` to stop the chain once handled.
- **RFC 7807 extensions**: All ProblemDetails include `traceId` (Activity.Current) and `timestamp` (ISO 8601). 500 errors hide `detail` in production.
- **CorrelationIdMiddleware**: Reads `X-Correlation-ID` or `X-Request-ID` from inbound requests, generates GUID if absent, pushes to `Serilog.LogContext`. Added to response headers.
- **RequestLoggingMiddleware**: Structured logging with method, path, status, duration, userId, tenantId, correlationId.
- **BusinessMetrics** (OpenTelemetry): Meter `"Explore.Business"` with counters: `events.created`, `events.published`, `registrations.created`, `organizations.created`, `authorization.decisions`. All tagged with `tenant_id`, `resource_type`.
- **PerformanceBehavior** (MediatR pipeline): Warning for >500ms, error for >3000ms. Includes handler name and elapsed time.
- **Rate limiting 429**: Rejection response is RFC 6585 ProblemDetails with `Retry-After` and `X-RateLimit-*` headers.

## Resources (Read Before Applying)
- [api-exception-handling.md](resources/api-exception-handling.md)
- [api-error-responses.md](resources/api-error-responses.md)
- [mediatr-logging-behavior.md](resources/mediatr-logging-behavior.md)
- [prometheus-metrics.md](resources/prometheus-metrics.md)
- [loki-logging.md](resources/loki-logging.md)
- [blazor-error-boundary.md](resources/blazor-error-boundary.md)
- [opentelemetry-tracing.md](resources/opentelemetry-tracing.md)
- [observability-stack.md](resources/observability-stack.md)

## Related Documentation
- [`docs/CONFIGURATION.md`](../../../docs/CONFIGURATION.md)
- [`docs/API.md`](../../../docs/API.md) — Middleware pipeline, exception handling chain, rate limiting
- [`docs/OPERATIONS.md`](../../../docs/OPERATIONS.md) — Business metrics, correlation ID, security headers

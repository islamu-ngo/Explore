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

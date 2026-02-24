ABOUTME: Database performance monitoring guidance via tracing.
ABOUTME: Keeps ActivitySource usage minimal and targeted.

# DB Performance Monitoring (Lean)

## Rules
- Use `ActivitySource` spans for critical repository operations.
- Tag spans with operation name and duration.
- Avoid excessive custom spans; rely on EF Core instrumentation when possible.

## Related
- [opentelemetry-tracing.md](opentelemetry-tracing.md)

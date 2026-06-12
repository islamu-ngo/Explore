ABOUTME: Observability stack summary (OpenTelemetry + Prometheus + Loki).
ABOUTME: Notes project-specific rules and integration constraints.

# Observability Stack (Lean)

## Stack
- OpenTelemetry traces + metrics
- Prometheus scraping `/metrics`
- Loki for structured logs (Grafana for dashboards)

## Project Rules
- This project does **not** use Sentry.
- Keep `UseSerilog(..., writeToProviders: true)` enabled.
- Configure OTLP once via `UseOtlpExporter()`; do not mix exporters.

## Related
- [loki-logging.md](loki-logging.md)
- [prometheus-metrics.md](prometheus-metrics.md)

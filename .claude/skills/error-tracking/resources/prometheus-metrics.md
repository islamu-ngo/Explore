# Prometheus Metrics for API Services

Expose metrics for API behavior and latency.

## Baseline Setup

- Register OpenTelemetry metrics.
- Add Prometheus exporter.
- Map scraping endpoint (commonly `/metrics`).

## What to Measure

- HTTP request duration histogram
- Request count by route, method, status
- Active request gauge
- Database operation duration histogram
- MediatR request duration by request type

## Cardinality Guardrails

- Do not label metrics with unbounded user input.
- Prefer route templates over raw URLs.
- Keep label sets small and stable.

## Alerting-Ready Dimensions

- Service name
- Environment
- Tenant (only when bounded)
- Operation/request type

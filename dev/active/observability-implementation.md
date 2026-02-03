Role
  You are a Senior .NET Cloud Architect + DevOps Engineer.

  Objective
  Re-architect observability for my .NET solution using the PLG stack
  (Prometheus, Loki, Grafana) with Serilog + OpenTelemetry, supporting
  dual-mode deployment:
  - Local Dev: .NET Aspire AppHost orchestrates containers
  - Production: fully decoupled Docker containers (no AppHost, no Compose)

  Context (Source Files)
  Pay special attention to:
  - Explore.ServiceDefaults/Extensions.cs (current OTel setup)
  - Explore.ServiceDefaults/Explore.ServiceDefaults.csproj (current
  packages)
  - Explore.API and Explore.Blazor startup code (logging/hosting patterns)
  - Explore.AppHost (if present) for dev orchestration patterns

  Non‑Negotiable Requirements
  1) Logging (Serilog + OTel + Loki)
  - Use Serilog for structured JSON logging with enrichers.
  - Do NOT use Serilog.Sinks.Loki.
  - Serilog must emit into OpenTelemetry (OpenTelemetry sink/provider).
  - OpenTelemetry must export logs via OTLP to Loki.

  2) Metrics (Prometheus)
  - Use OpenTelemetry to collect .NET runtime, ASP.NET Core, and HttpClient
  metrics.
  - Expose metrics for Prometheus to scrape (pull) OR route via OTLP in a
  standard, Prometheus-compatible way.
  - Prefer pull with a /metrics endpoint unless you strongly justify
  otherwise.

  3) Visualization (Grafana)
  - Ensure Grafana can query both Prometheus (metrics) and Loki (logs).

  4) Dual‑Mode Deployment
  - Local (Aspire): show AppHost configuration that spins up Prometheus,
  Loki, Grafana containers and wires endpoints/env vars.
  - Production (No Aspire): 12‑Factor compliant. All observability config
  must be driven by environment variables (e.g.,
  OTEL_EXPORTER_OTLP_ENDPOINT).
  - The app must NOT crash or block if observability backends are
  unavailable (fail‑open, fire‑and‑forget).

  5) Keep Packages and Patterns Aligned
  - Use .NET 9 / current package versions in the repo where possible.
  - Only add packages that are strictly required and justify each addition.

  Deliverables
  A) Packages
  - List NuGets to add per project: ServiceDefaults, API, Blazor (if
  different).

  B) ServiceDefaults Code (C#)
  - Provide concrete code changes (Extensions.cs or Program.cs) to:
    - Configure Serilog with enrichers and JSON output.
    - Pipe Serilog into OpenTelemetry.
    - Configure OpenTelemetry for logs + metrics (+ traces if already in
  place).
    - Configure OTLP exporter and resource attributes.
    - Add Prometheus scraping endpoint if using pull model.
    - Ensure non‑fatal exporter failure behavior.

  C) AppHost (C#)
  - Show the AppHost code to run Loki, Prometheus, Grafana containers.
  - Include ports, volumes, config mounts, and any environment variables
  for local wiring.

  D) Production Env Vars
  - Exact environment variables required for API and Blazor containers.
  - Include OTEL_* values and any service names/resource attributes.
  - No code changes required per env.

  E) Config Files
  - Provide production‑ready:
    - prometheus.yml
    - loki-config.yaml
  Tailor them to this architecture and OTLP flow.

  Output Format
  Use the following sections in your response:
  1) Assumptions
  2) Packages
  3) ServiceDefaults Changes
  4) AppHost Changes
  5) Production Environment Variables
  6) Config Files
  7) Notes / Validation Steps

  Edge Cases / Quality Bar
  - Do not break existing health checks.
  - Do not hardcode endpoints in code.
  - Use stable, explicit service names.
  - Keep code minimal, production‑safe, and composable.
  - If you need to introduce an OpenTelemetry Collector, explain why and
  keep it optional.
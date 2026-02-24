ABOUTME: Minimal MediatR logging behavior rules.
ABOUTME: Centralizes request logging and error capture.

# MediatR Logging Behavior (Lean)

## Rules
- Log start/end for each request with elapsed time.
- Log exceptions with request type and traceId.
- Keep request/response payloads out of logs unless sanitized.

## Related
- [loki-logging.md](loki-logging.md)

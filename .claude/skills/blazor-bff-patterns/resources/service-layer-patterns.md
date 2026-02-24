ABOUTME: Service-layer wrapper for NSwag clients in Blazor.
ABOUTME: Keep UI free of raw client error handling.

# Service Layer Patterns

## Required Rules
- Wrap NSwag clients behind services (interface + implementation).
- Catch `ApiException` and return safe defaults (`[]`, `null`, or error response).
- Log errors; do not throw to UI by default.

## Registration
- Register services as **Scoped** in Blazor Client DI.

**Related**: `bff-configuration.md`, `auth-state-management.md`.

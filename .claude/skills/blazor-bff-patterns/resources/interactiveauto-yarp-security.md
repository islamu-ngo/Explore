# InteractiveAuto + YARP Security Patterns

Production guidance for Blazor Hybrid apps using InteractiveAuto and YARP/BFF.

## Middleware and Endpoint Order

- Configure forwarded headers before auth middleware when behind reverse proxy.
- Use routing, then authentication, then authorization, then antiforgery and endpoints.
- Map Blazor routes before proxy catch-all routes when both coexist.

## Token Forwarding

- Keep tokens server-side (cookie-backed session in BFF).
- Forward access tokens to downstream API via YARP request transforms or delegating handlers.
- Never expose raw access tokens to browser storage.

## Anti-Forgery

- Enable antiforgery and validate on state-changing endpoints.
- For SPA requests, send antiforgery token via header (for example `X-CSRF-TOKEN`).
- Keep cookie flags secure (`HttpOnly`, `Secure`, `SameSite` based on deployment needs).

## InteractiveAuto Constraints

- Components may execute in server or WASM contexts.
- Avoid direct `HttpContext` assumptions in components intended for InteractiveAuto.
- Register required services in both server and client projects when shared by InteractiveAuto components.

## Operational Hardening

- Add health checks and graceful shutdown for rolling deployments.
- Use structured auth/proxy logs without leaking tokens.
- Validate proxy destination configuration through environment-specific settings, not hardcoded values.

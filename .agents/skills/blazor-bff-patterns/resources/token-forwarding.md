ABOUTME: Token forwarding rules for BFF to API proxy requests.
ABOUTME: Covers YARP configuration, header propagation, and InteractiveServer fallback.

# Token Forwarding

## Request Flow

```
Browser → Blazor BFF (cookie auth) → YARP Proxy → API (Bearer JWT)
```

The BFF extracts the access token from the server-side session and attaches it as a Bearer token on proxied API requests. Tokens never reach the browser.

## Required Rules

1. **Extract access token** from server-side session/cookie store — never from client-side storage.
2. **Attach `Authorization: Bearer`** on all proxied API requests via `AccessTokenForwardingHandler`.
3. **Forward tenant headers** (`X-Tenant-Slug`) when the BFF has an authoritative tenant hint from route, host, or session context.
4. **Forward setup headers** (`X-Setup-Secret`) during initial bootstrap flow.
5. **Use the repo handler chain** for API-facing clients: `AccessTokenForwardingHandler` → `TenantHeaderForwardingHandler` → `SetupSecretForwardingHandler`.
6. **Disable cookie forwarding on outbound API clients** with `HttpClientHandler.UseCookies = false` so the BFF remains the trust boundary.

## YARP Proxy Configuration

The BFF registers YARP reverse proxy transforms that:
- Copy the access token from the authenticated session
- Add the `Authorization` header to outbound requests
- Propagate `X-Tenant-Slug` and `X-Correlation-Id` headers

Tenant identity is still resolved authoritatively by the API: trusted `X-Tenant-Slug` first, then normalized `Request.Host.Host` after forwarded-header processing. API-key requests can defer final tenant binding until post-auth middleware runs.

## InteractiveServer Fallback

When running in `InteractiveServer` render mode, `HttpContext` is unavailable during SignalR circuit execution.

**Pattern**: Use a scoped token cache service that:
1. Captures the token during the initial HTTP request (where `HttpContext` exists)
2. Stores it in a scoped service for the circuit lifetime
3. Provides it to `HttpClient` handlers for API calls within the circuit

## Security Constraints

| Rule | Rationale |
|------|-----------|
| Never expose tokens to WebAssembly | BFF security model — tokens stay server-side |
| Validate token expiry before forwarding | Avoid 401 cascades from expired tokens |
| Use `HttpOnly` + `Secure` + `SameSite=Lax` cookies | Prevent XSS token theft |
| Rotate refresh tokens on use | Detect token replay attacks |

## Related

- [bff-configuration.md](bff-configuration.md)
- [auth-state-management.md](auth-state-management.md)

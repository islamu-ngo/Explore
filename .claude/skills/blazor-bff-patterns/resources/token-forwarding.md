ABOUTME: Token forwarding rules for BFF → API.
ABOUTME: Keep tokens server-side; attach as Bearer on proxy requests.

# Token Forwarding

## Required Rules
- Extract access token from server-side session/cookie.
- Attach `Authorization: Bearer` on proxied API requests.
- Forward tenant/setup headers when present.

## InteractiveServer Note
- If `HttpContext` unavailable, use a scoped token cache/service.

**Related**: `bff-configuration.md`, `auth-state-management.md`.

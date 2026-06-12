ABOUTME: JWT validation and middleware order for API.
ABOUTME: Minimal guardrails for Keycloak multi-client tokens.

# API JWT Validation Patterns

## Required Validation Rules
- Validate **issuer** and **audience**.
- For Keycloak multi-client tokens, accept if **aud** OR **azp** matches allowed clients.
- Validate lifetime; apply small clock skew.

## Middleware Order (Required)
1. Exception handling
2. Forwarded headers
3. Security/correlation/logging middleware
4. Routing
5. Tenant resolution middleware (pre-auth)
6. Request timeouts
7. Authentication
8. Tenant resolution middleware (post-auth for API-key flows)
9. Authorization
10. Endpoint mapping

## Claim Extraction
- Centralize user-id parsing; use fallback order: `sub → nameidentifier → sid`.

## Tenant Resolution (If Multi-Tenant)
Priority order for normal API requests: trusted `X-Tenant-Slug` → normalized `Request.Host.Host` after forwarded-header processing → fail-closed `404` when unresolved in multi-tenant mode.

For API-key flows, pre-auth middleware can store a requested tenant hint and post-auth middleware finalizes tenant binding or returns `404 Tenant mismatch` / `401 API key authentication failed`.

## Logging Guardrails
- Log auth failures with context.
- **Never** log raw JWTs.

**Related**: `auth-patterns` skill.

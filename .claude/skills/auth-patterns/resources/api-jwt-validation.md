ABOUTME: JWT validation and middleware order for API.
ABOUTME: Minimal guardrails for Keycloak multi-client tokens.

# API JWT Validation Patterns

## Required Validation Rules
- Validate **issuer** and **audience**.
- For Keycloak multi-client tokens, accept if **aud** OR **azp** matches allowed clients.
- Validate lifetime; apply small clock skew.

## Middleware Order (Required)
1. Exception handling
2. Routing + CORS
3. Authentication
4. Authorization
5. Endpoint mapping

## Claim Extraction
- Centralize user-id parsing; use fallback order: `sub → nameidentifier → sid`.

## Tenant Resolution (If Multi-Tenant)
Priority order: `X-Tenant-Id` → custom domain → subdomain → default tenant.

## Logging Guardrails
- Log auth failures with context.
- **Never** log raw JWTs.

**Related**: `auth-patterns` skill.

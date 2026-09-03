---
description: Operate Keycloak, BFF browser sessions, API credentials, and optional linked AT Protocol sign-in.
---

# Authentication Architecture

Keycloak is the authoritative identity provider for user authentication. The Blazor Backend-for-Frontend (BFF) owns secure, encrypted cookie sessions, obtains and refreshes OIDC tokens, and proxies authenticated requests to `Explore.API` (see [Architecture & Request Flows](../getting-started/architecture-and-request-flows.md#1-browser-request-flow)).

---

## Browser Authentication Flow

The browser communicates strictly with `Explore.Blazor` over HTTPS:
* The client never stores raw JWT access tokens in browser `localStorage` or `sessionStorage` (mitigating XSS token theft).
* Authentication state is tracked via an encrypted `SameSite=Lax` session cookie managed by the BFF.
* Production operators must ensure:
  * Proper TLS termination and reverse-proxy header forwarding (`X-Forwarded-Proto: https`).
  * Explicit registration of valid redirect URIs in the Keycloak Admin Console (see [Troubleshooting Redirect Errors](../configuration-and-operations/troubleshooting-and-health.md#recipe-1-keycloak-invalid-parameter-redirect_uri-or-infinite-login-loop)).
  * Secure client secret storage via [Secrets Management](../configuration-and-operations/secrets.md).

---

## Direct API Authentication

External programmatic clients and integration workers authenticate using either:

* **Bearer Token**: `Authorization: Bearer <jwt_access_token>` (issued by Keycloak).
* **API Key**: `X-API-Key: <key>` (hashed with SHA-256 in the database).

> [!NOTE]
> Do not supply both headers simultaneously. Authentication establishes *who* the caller is; [Authorization](authorization.md) and [Multi-Tenancy](multi-tenancy.md) boundaries still evaluate independently on every request.

---

## Linked AT Protocol Sign-In

Optional [AT Protocol Authentication](../federation-and-open-protocols/at-protocol-and-bluesky-jetstream.md) enables users to sign in using their Bluesky handle (`@handle.bsky.social`) or Decentralized Identifier (DID):
* Links strictly to an existing, verified local account.
* Does not automatically create accounts via opportunistic email matching.
* Does not implicitly grant event publication or federation consent.
* Operates under the same unified [Authorization](authorization.md) rules as standard Keycloak users.

---

## Acceptance Testing Checklist

1. Verify successful login through Keycloak redirects back to the Blazor application.
2. Confirm that expired tokens refresh automatically without user disruption.
3. Verify that logging out invalidates both the local cookie and the Keycloak SSO session.
4. Verify that an invalid API key returns `401 Unauthorized` with ProblemDetails.

---

## Related Guides & Next Steps

* **[Authorization & Access Control](authorization.md)** — Learn how MediatR handlers evaluate Local RBAC or Cerbos policies.
* **[Docker Compose Runbook](../self-hosting/docker-compose.md)** — Deploy Keycloak and configure the `event-blazor` client.
* **[Troubleshooting Keycloak Errors](../configuration-and-operations/troubleshooting-and-health.md#recipe-1-keycloak-invalid-parameter-redirect_uri-or-infinite-login-loop)** — Resolve redirect URI mismatches and login loops.
* **[Admin Hierarchy & Roles](../administration-and-branding/admin-hierarchy.md)** — Map Keycloak users to Instance, Tenant, and Event roles.

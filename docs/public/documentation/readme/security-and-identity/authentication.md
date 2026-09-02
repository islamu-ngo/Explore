---
description: >-
  Operate Keycloak, BFF browser sessions, API credentials, and optional linked
  AT Protocol sign-in.
---

# Authentication

Keycloak is the required browser identity authority. The Blazor BFF owns cookie sessions, obtains or refreshes access tokens, and forwards bearer authentication to the API.

## Browser authentication

The browser talks to the BFF rather than storing API tokens. Production operators own:

* TLS and trusted reverse-proxy configuration;
* public Keycloak and application URLs;
* redirect URI correctness;
* Keycloak realm/client hardening;
* browser client-secret delivery and rotation;
* administrative access and recovery.

Re-test a complete login and token refresh after any DNS, proxy, callback, or client-secret change.

## Direct API authentication

Integrations use either:

* `Authorization: Bearer <token>`; or
* `X-API-Key: <key>`.

Do not send both. Authentication identifies the caller; authorization and tenant binding still apply independently. API keys can carry bounded scopes and may finalize tenant context, but they do not bypass resource policy.

## AT Protocol sign-in

Optional AT Protocol authentication links only to an existing local user. It does not:

* create users through email matching;
* enable federation or Jetstream ingestion;
* grant publication consent;
* create a second authorization model.

Treat account linking and federation enablement as separate governed actions.

## Acceptance

Verify one successful and one rejected browser login, token refresh, logout/session invalidation, one valid and invalid API key, tenant binding, and safe failure responses. Keep tokens, cookies, client secrets, and provider diagnostics out of logs and support artifacts.

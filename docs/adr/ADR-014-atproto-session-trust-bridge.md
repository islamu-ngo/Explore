<!-- ABOUTME: Architectural decision record for the AT Protocol OAuth session trust bridge and key purposes. -->
<!-- ABOUTME: Defines server-private verification, first-party JWT issuance, CarpaNet ownership, and rotation boundaries. -->

# ADR-014: AT Protocol Session Trust Bridge

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-18 |
| **Deciders** | ISLAMU Event Platform — Architecture, Security, Authentication workstreams |
| **Supersedes** | FishyFlip intent in the ATProto authentication stubs |
| **Superseded by** | — |

## Context

An AT Protocol authorization produces PDS-bound OAuth/DPoP credentials, not an application access token. The browser-facing BFF must establish a local cookie while the API independently verifies the PDS identity and continues to enforce its authorized-write invariant. Multi-node callbacks, custom tenant hosts, persisted OAuth sessions, and signing-key rotation must not put credentials or identity assertions in browser-visible channels.

## Decision

### Protocol and trust ownership

1. CarpaNet and CarpaNet.OAuth are the sole OAuth implementation. They own identity resolution, authorization-server discovery, PAR, PKCE, DPoP, callback validation, refresh, restore, and signout. FishyFlip, hand-written OAuth, Keycloak brokering, and CarpaNet.AspNetCore are excluded.
2. The BFF cookie is a browser session boundary only. It never contains PDS access or refresh tokens. PDS credentials and the complete CarpaNet session remain server-side.
3. The BFF calls the server-private ATProto session bridge with a short-lived, audience-, route-, method-, tenant-, and `jti`-bound ES256 bootstrap assertion. The assertion authenticates the BFF client but carries no authoritative user identity. The bridge is an authorized write; it is never anonymous, exposed through HAL, or generated into the public API client.
4. The API consumes the assertion `jti` once, restores the submitted CarpaNet session in a temporary store, calls `com.atproto.server.getSession`, and requires the expected DID, token subject, authenticated CarpaNet DID, and PDS response DID to agree. No BFF header or unverified JWT selector result establishes the user.
5. Only after independent verification may the API reuse the existing linked-account synchronization rule, persist the OAuth session, and issue a short-lived first-party ES256 session JWT. YARP forwards that platform JWT; the API selects its bounded issuer branch and then performs full issuer, audience, signature, lifetime, algorithm, and key-id validation.
6. ATProto sign-in is linked-account-only. The platform does not auto-match email, synthesize email, or create an incomplete user from a DID. A separate approved linking/onboarding design is required to relax this limitation.

### Session persistence and transient state

CarpaNet's `IOAuthSessionStore` has two adapters and one durable table: a BFF server-private API adapter and an Infrastructure repository adapter over `UserAuthenticationToken`. The complete versioned `OAuthSessionData` is encrypted as one AES-GCM envelope with subject DID, encryption key ID, safe PDS host, and expiry metadata; plaintext and partial-column fallbacks are forbidden.

OAuth state and cross-host handoff are atomic and single-use. Multi-node deployments use Redis `GETDEL`; process-local atomic storage is limited to explicit single-node development. Handoff URLs contain only an opaque random code, never a platform JWT, PDS credential, DID, or session payload.

### Cryptographic purpose separation and rotation

Three instance-scoped, rotation-capable secret purposes are mandatory:

- `auth.atproto.oauth_client_private_jwks` signs OAuth `private_key_jwt` assertions and the server-private bootstrap assertion.
- `auth.atproto.session_encryption_keyring` encrypts persisted CarpaNet OAuth-session envelopes.
- `auth.atproto.session_jwt_private_jwks` signs first-party API session JWTs.

Keys are never reused across these purposes. ES256 signing rings have unique nonblank `kid` values and exactly one active key. Retired public OAuth client keys remain in `/oauth/jwks.json` for the overlap window required by in-flight assertions and bound sessions. Private `d` values remain server-only and are excluded from public JSON, logs, health output, exceptions, traces, and diagnostics. Malformed rings, unknown fields/statuses, duplicate key IDs, or an invalid active-key count fail readiness closed.

The BFF's URL-form client ID is its exact canonical HTTPS `/oauth/client-metadata.json` URL. The anonymous metadata and JWKS endpoints serve exact JSON without redirects only on that configured host, use bounded public caching and document size, advertise `private_key_jwt` with ES256 and DPoP-bound access tokens, and publish only EC P-256 public parameters. Loopback helpers may be used only in Development where CarpaNet explicitly supports them; they cannot weaken production canonical-host, HTTPS, key, or egress policy.

## Rejected alternatives

1. Anonymous bridge writes or trusted BFF identity headers — they bypass independent PDS verification and the API write boundary.
2. Sending PDS tokens to the browser or accepting them as platform bearer tokens — their audience and proof binding are not the application's authorization contract.
3. One key for OAuth assertions, envelope encryption, and API JWTs — it expands compromise impact and confuses verifier trust.
4. BFF database access or a second durable OAuth-session table — both violate the BFF boundary or duplicate session authority.
5. Non-atomic cache get/remove or JWT query-string handoff — both permit replay or credential disclosure.
6. Implicit DID-only user creation — it conflicts with existing required identity fields and explicit-linking policy.

## Consequences

- The browser receives only an HttpOnly BFF cookie; the API receives only a short-lived first-party bearer token after PDS verification.
- BFF, API, and Infrastructure need separate adapters and key consumers while Domain remains unaware of CarpaNet types.
- Key rotation requires overlap publication, `kid`-based verification/decryption, multi-node consistency, and operator readiness checks.
- CarpaNet outbound traffic must pass the separately implemented constrained transport/readiness gate before ATProto login is advertised.
- Existing development sessions may be invalidated; backward-compatible plaintext or FishyFlip paths are intentionally not retained.

## Related

- `dev/report/atproto-report.md`
- `dev/active/atproto-auth/atproto-auth-plan.md`
- `dev/active/atproto-auth/atproto-auth-context.md`
- ADR-001: Authorization Provider Architecture
- ADR-015: AT Protocol Event Federation Ownership

<!-- ABOUTME: Backlog for Local Identity account-security capabilities deferred from the initial JWT implementation. -->
<!-- ABOUTME: Defines the trust-boundary work required before recovery, verification, MFA, or passkeys ship. -->

# Local Identity Account Security

## Problem Statement

The initial Local Identity implementation provides registration, password login,
bounded lockout, short-lived JWTs, normalized platform-user synchronization, and
HttpOnly BFF sessions. It intentionally does not pretend that registration
verifies email ownership or that a login endpoint is a recovery system.

Password recovery, verified-email elevation, MFA, and passkeys require durable
purpose-bound tokens, notification delivery, replay prevention, audit, and
administrator-support flows. Adding isolated controller methods without that
system would weaken the authentication trust boundary.

## Required Capabilities

### Email verification

- Issue single-purpose, expiring, one-time verification tokens.
- Deliver links through the transactional notification/outbox path.
- Bind confirmation to the exact Local Identity credential and current email.
- Promote `email_verified` only after successful confirmation.
- Invalidate outstanding verification tokens when the email changes.

### Password change and recovery

- Require the current password for authenticated change.
- Use non-enumerating recovery responses and purpose-bound, expiring reset
  tokens.
- Revoke or rotate existing Local sessions after a successful reset.
- Apply rate limits and security audit events without logging credential values.

### MFA and passkeys

- Add TOTP recovery-code lifecycle with encrypted-at-rest secrets.
- Add WebAuthn/passkey registration and assertion with origin, RP ID, challenge,
  sign-count, and replay validation.
- Define step-up requirements for administrator and security-sensitive actions.
- Preserve provider-account normalization and tenant-independent credential
  ownership.

### Session governance

- Add server-authoritative Local session inventory and revocation.
- Define refresh-token rotation or reauthentication instead of extending the
  initial bearer lifetime.
- Provide administrator recovery without allowing email-only account takeover.

## Acceptance Criteria

- [ ] Invariant-breaker tests cover token replay, expiry, cross-account use, and
      concurrent consumption.
- [ ] Email verification is the only path that changes Local
      `email_verified=false` to `true`.
- [ ] Recovery responses do not reveal whether an account exists.
- [ ] Password reset revokes or rotates all affected sessions atomically.
- [ ] MFA/passkey challenges are origin-bound, one-time, and auditable.
- [ ] No secret, token, password, recovery code, or PII appears in telemetry.
- [ ] Public operator and user documentation describes recovery and rollback.

## References

- `docs/internal/AUTHENTICATION.md`
- `src/Explore.Persistence/Identity/LocalIdentityAuthService.cs`
- `src/Explore.Infrastructure/Authentication/LocalJwtTokenGenerator.cs`
- `src/Explore.Blazor/Extensions/BffAuthEndpoints.cs`


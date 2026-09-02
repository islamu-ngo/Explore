---
description: >-
  Diagnose startup, identity, authorization, tenancy, and provider failures
  without leaking sensitive data.
---

# Troubleshooting & Health

Diagnose from configuration and authority boundaries outward. Avoid restarting everything blindly or bypassing fail-closed checks.

## Diagnostic order

1. Validate rendered configuration.
2. Confirm DNS, TLS, ports, reverse-proxy headers, and routing.
3. Verify application database and privacy-erasure authority reachability.
4. Prove migrations completed before API/UI startup.
5. Check Keycloak client-secret synchronization and external bootstrap URL safety.
6. Verify the selected authorization provider and policy readiness.
7. Confirm tenant resolution for the exact host/request.
8. Inspect `/alive`, `/health`, and `/metrics`.
9. Correlate bounded logs by trace or correlation ID.

## Common signals

* **Missing HAL action:** the current principal/resource/tenant/state is not authorized, or policy evaluation failed closed. Do not add a client-side role override.
* **Cerbos denial during outage:** expected fail-closed behavior; restore PDP/policy health or perform an explicit provider change.
* **Tenant `404`:** verify trusted BFF context, admin-host exclusion, custom domain, then subdomain. Unknown hosts must not choose an arbitrary tenant.
* **Keycloak loop or callback failure:** verify public URL, proxy forwarding, redirect URIs, client type, and synchronized client secret.
* **Setup provider status:** preserve distinctions between unconfigured, unavailable, unauthorized, and invalid. Do not expose the credential while debugging.
* **Migration block:** inspect the one-shot migration resource and database roles; do not let API/UI bypass it.

## Safe health and support evidence

Health responses must not disclose filesystem paths, object keys, bucket names, provider endpoints, access keys, connection strings, tokens, or PII. Support bundles should contain version, topology, bounded health codes, timestamps, affected resource, correlation IDs, and redacted configuration shape.

After a fix, repeat the exact failed operation and one adjacent control case. Verify both the user-visible result and the relevant durable state rather than accepting a healthy aggregate status alone.

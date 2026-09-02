---
description: Deploy the documented Cerbos PDP profile behind Traefik on Coolify.
---

# Coolify with Cerbos & Traefik

The repository documents how to operate Cerbos on Coolify. It does not ship a one-click template for the entire ISLAMU Event platform.

## Cerbos profile

Use a pinned Cerbos image, durable policy/configuration storage, and the documented PostgreSQL-backed administration store. Keep runtime authorization and policy administration as separate operational paths.

Cerbos runtime decisions use the gRPC PDP. Expose that service through Traefik with `h2c` to container port `3593`. Protect the Cerbos admin API independently and do not expose it as a public convenience endpoint.

## Verify the route

```bash
grpcurl cerbos-grpc.example.org:443 list
```

A protocol failure commonly means Traefik is not using `h2c` or traffic is reaching the wrong port. Also verify:

* `/_cerbos/health`;
* `/_cerbos/metrics`;
* policy availability and distribution;
* application authorization readiness;
* TLS and DNS for the external gRPC endpoint.

## Fail-closed behavior

When Cerbos is the selected authorization provider, PDP outage, invalid policy state, or tenant BYO-PDP failure denies access. ISLAMU Event does not silently switch to local RBAC. A provider change is an explicit operator action with its own recovery plan.

## Whole-platform responsibilities

Complete the application deployment separately: database, Keycloak, migration job, API, BFF/UI, storage, secrets, mail, webhooks, DNS, TLS, backup, restore, and rollback. Pin all images and document which service owns each durable volume and health signal.

The Coolify deployment is ready only after an authenticated resource decision succeeds through the public gRPC route and a deliberate PDP outage produces the expected fail-closed result.

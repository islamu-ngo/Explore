---
description: >-
  Select deployment mode, resolve tenants, and preserve fail-closed data
  isolation.
---

# Multi-Tenancy

Single-tenant is the default. Set `DEPLOYMENT_MODE=multi_tenant` before first-run onboarding to operate multiple tenant experiences. Normal administration does not casually convert deployment mode later.

## Tenant resolution

Multi-tenant requests resolve in this order:

1. trusted BFF tenant context;
2. admin-host exclusion;
3. custom domain;
4. subdomain;
5. fail closed with `404`.

An unknown host must never select an arbitrary/default tenant. The API does not trust tenant or caller identity submitted in request bodies.

## Persistence isolation

EF Core named tenant filters fail closed when ambient tenant context is absent. Explicit system or cross-tenant work must opt into a bypass and apply bounded tenant predicates.

PostgreSQL row-level security is not enabled on current production tables. Do not describe RLS as the platform's present isolation mechanism.

## Identity and participation

Global User, Actor, Organization, and Group identities are distinct from tenant participation. Tenant administrators can govern local participation and delegated settings without authority to mutate global identities.

Settings cascade through instance, tenant, organization, group, and user scopes, with locks and delegation controlling who may override each value.

## Custom domains

Before binding a tenant domain, prove ownership, DNS, TLS, reverse-proxy host forwarding, canonical host policy, and isolation from the instance administration host. Test the exact host against public, authenticated, and rejected tenant requests.

## Acceptance

Use at least two tenants and an unknown host. Verify that reads, writes, API keys, HAL links, settings, storage, callbacks, and background work never cross the resolved boundary, and that missing ambient context denies access rather than widening a query.

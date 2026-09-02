---
description: >-
  Delegate instance, tenant, organization, and group administration without
  widening authority.
---

# Admin Hierarchy

Administration is scoped. An administrator at one level does not automatically receive authority over every identity, tenant, or platform concern.

## Scopes

| Scope        | Typical responsibilities                                                                                                       |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------ |
| Instance     | Deployment-wide governance, tenants, provider posture, platform settings, and monetization settings                            |
| Tenant       | Policies, public experience, lookups, navigation, footer, templates, custom properties, tenant API keys, and delegated storage |
| Organization | Profile, membership, branding, and scoped integrations/API keys                                                                |
| Group        | Profile, membership, branding, and scoped delegation                                                                           |

Instance console routes are primarily meaningful in multi-tenant deployments. Tenant administration governs tenant participation and settings; it does not grant authority to mutate global User, Actor, Organization, or Group identities.

## Action authority

Every mutable action follows current HAL `_links`. Do not show purge, revoke, edit, restore, or provider-switch controls based only on a cached role or claim. Concurrency/version conflicts are safety signals and should be resolved by reloading current state, not bypassed.

## High-impact operations

Record ownership, preconditions, recovery, and audit expectations before:

* tenant purge scheduling or restoration;
* tenant/API-key revocation;
* identity or authorization provider changes;
* render-policy changes;
* bulk template or custom-property changes;
* payment or monetization governance changes.

Instance monetization and organizer earnings are separate concerns. Do not describe platform contributions as organizer payouts.

## Delegation checklist

Verify the principal, scope, target tenant/organization/group, available links, concurrency behavior, audit record, recovery path, and absence of PII/secrets in logs. Test one action that should be allowed and one that should be absent or denied.

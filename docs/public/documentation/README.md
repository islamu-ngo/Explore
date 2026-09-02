---
description: >-
  Adopter documentation for evaluation, self-hosting, operations, security, and
  integration.
---

# Documentation

This space is the operating manual for organizations evaluating or self-hosting ISLAMU Event. It follows the lifecycle of an adoption decision: understand the system, select a topology, configure authoritative state, secure it, operate it, then add product capabilities and integrations.

{% hint style="warning" %}
ISLAMU Event is pre-1.0 and the current API version is `0.1`. Pin exact releases, review change notes before upgrades, and prove backup and restore procedures before production use.
{% endhint %}

## Recommended reading paths

### Evaluator

1. [Getting Started](readme/getting-started/)
2. [Self-Hosting](readme/self-hosting/)
3. [Security & Identity](readme/security-and-identity/)
4. [Events & Ticketing](readme/events-and-ticketing/)
5. [Integrations & AI](readme/integrations-and-ai/)

### Self-hoster

1. [Self-Hosting](readme/self-hosting/)
2. [Configuration & Operations](readme/configuration-and-operations/)
3. [Security & Identity](readme/security-and-identity/)
4. [Administration & Branding](readme/administration-and-branding/)
5. [Communications & Notifications](readme/communications-and-notifications/)

### Integrator or contributor

1. [Integrations & AI](readme/integrations-and-ai/)
2. [Federation & Open Protocols](readme/federation-and-open-protocols/)
3. [API Reference](https://islamu.gitbook.io/islamu-event/api-reference/)
4. [Contributing](readme/contributing/)

## Documentation contract

Repository behavior is authoritative. Pages distinguish implemented behavior from deferred or adopter-owned work. They do not claim turnkey cloud deployment, legal or regulatory compliance, religious certification, guaranteed provider behavior, or support for infrastructure the repository does not ship.

The platform uses fail-closed authority boundaries. When identity, authorization, tenant resolution, secrets, privacy erasure, payment evidence, or mandatory public disclosure cannot be established, the system rejects or withholds the operation rather than silently weakening the contract.

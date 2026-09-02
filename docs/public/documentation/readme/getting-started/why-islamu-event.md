---
description: Evaluate the platform’s purpose, scope, maturity, and operating model.
---

# Why ISLAMU Event?

ISLAMU Event is a self-hostable event platform for communities and organizations that need explicit control over deployment, durable data, identity, authorization, payments, admission, moderation, and federation.

## What the platform brings together

The product covers public event discovery, organizer administration, registration and waitlists, admission and online check-in, organizer-direct payments and refunds, durable notifications, SMTP, webhooks, forms, MCP, storage, and selective AT Protocol federation.

These capabilities remain bounded. Enabling an integration does not silently grant it identity, authorization, payment, or moderation authority. Browser returns are not payment truth, realtime transports are not notification truth, and external moderation signals do not replace local decisions.

## Why self-hosters adopt it

* **Deployment control:** choose the documented standalone or split topology and own its infrastructure.
* **Fail-closed authority:** Keycloak, the selected authorization provider, tenant resolution, and secret providers do not silently fall back to weaker paths.
* **Durable workflows:** outboxes and idempotent intake protect external effects such as email, webhooks, payments, and federation.
* **Discoverable permissions:** HAL links tell clients which actions are currently allowed for a resource.
* **Explicit limits:** unsupported deployment modes, protocol surfaces, and provider responsibilities are documented rather than implied.

## Maturity and responsibility

The API is version `0.1` and the project is in major version zero. Pin application and image versions, review release and API changes before upgrades, and keep tested restore and rollback procedures.

Self-hosting transfers operational responsibility to the adopter. You must assess TLS, DNS, identity-provider hardening, authorization policy, secrets, backups, incident response, retention, data residency, accessibility, and provider agreements for your deployment. Product documentation describes implemented behavior; it is not a compliance, security, accessibility, or religious certification.

## Continue

Use [5-Minute Quickstart](5-minute-quickstart.md) to evaluate locally, then read [Architecture & Request Flows](architecture-and-request-flows.md) before selecting a production topology.

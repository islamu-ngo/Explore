---
description: Environment-specific OpenAPI, Swagger, Scalar, health, and metrics guidance.
---

# Interactive Endpoints

## OpenAPI, Swagger, and Scalar

Development and Testing environments can expose:

* Swagger UI at `/swagger`;
* Scalar at its mapped development/testing route;
* generated OpenAPI JSON at `/openapi/islamu-event.json`.

The local development API commonly uses `https://localhost:7039`. Docker Compose runs Production at `http://localhost:7039`, where interactive descriptions are not exposed by default.

{% hint style="warning" %}
Do not advertise an unfiltered development document as a public production integrator contract. Production exposure is an explicit operator decision and must pass authentication/authorization, tenant, TLS, rate-limit, metadata, and information-disclosure review.
{% endhint %}

The governed build artifact is `schemas/openapi-islamu-event.json`. Server code is the source; OpenAPI inventory and generated NSwag clients are outputs and must not be hand-edited.

## Operational endpoints

### `/alive`

Use for process liveness. It should answer whether the process can serve, not whether every dependency is ready.

### `/health`

Use for readiness and dependency-specific health. Relevant checks may cover databases, privacy-erasure authority, Keycloak, authorization, SMTP, webhooks, Listmonk, and other enabled providers.

A safe health payload does not reveal credentials, connection strings, filesystem paths, object keys, bucket names, private endpoints, access keys, provider payloads, or PII.

### `/metrics`

Use for monitoring with bounded identity-free dimensions. Never attach tenant/user identifiers, secrets, provider IDs, admission material, or unbounded error text as labels.

## External provider health

Cerbos also exposes `/_cerbos/health` and `/_cerbos/metrics`. Outgoing webhooks expose mode-specific application readiness. Provider health proves reachability/readiness, not that every tenant policy, credential, template, or business workflow is correct.

## Production exposure checklist

* terminate and verify TLS;
* restrict interactive descriptions to intended operators/integrators;
* enforce normal authentication, authorization, tenant, and rate-limit policy;
* redact operational details;
* test from outside the trusted network;
* record exact API version and generated artifact revision;
* monitor access and remove temporary exposure after diagnosis.

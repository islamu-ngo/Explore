---
description: Select, bind, rotate, and recover secrets through fail-closed authorities.
---

# Secrets

Secrets are external authority bindings, not application configuration values to copy between files.

## Approved authorities

* **Infisical** for centrally managed secret delivery.
* **Explicit environment injection** documented by `.env.example`.
* **Shared .NET User Secrets** only when deliberately selected in Development/Testing.

User Secrets are not a production fallback. Azure Key Vault and AWS Secrets Manager are not shipped as native supported adapters.

## Prohibited locations

Never store credentials, tokens, connection strings, encryption keys, or setup secrets in:

* source code or AppHost code;
* committed settings or `.env` files;
* configuration manifests;
* tests and fixtures;
* container images;
* logs, screenshots, traces, health payloads, or support tickets.

## Fail-closed status

A selected secret provider reports distinct states such as unconfigured, unavailable, unauthorized, or invalid. It does not silently downgrade one of those failures to “missing” and resolve the value from a weaker source.

Generated setup secrets use controlled files and are locked after onboarding. Retrieve them without printing, apply restrictive permissions, use them once, and remove unnecessary host copies.

## Rotation

Plan rotation as restart or redeploy based. The current platform does not promise universal live in-process credential refresh.

1. Create the replacement in the selected authority.
2. Coordinate provider-side and consumer-side validity windows.
3. Restart/redeploy the affected resources.
4. verify readiness and one bounded real operation;
5. revoke the old credential;
6. record only identifiers, timestamps, and outcome, never values.

## Recovery

Encrypt secret-provider backups, restrict access, audit retrieval, and test break-glass recovery. Recovery evidence should prove that the authority can be restored without exporting raw values into tickets or shared documents.

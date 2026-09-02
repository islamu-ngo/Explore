---
description: >-
  Render topology-specific configuration safely from the repository environment
  schema.
---

# Environment Variables

Use `.env.example` as the canonical inventory and schema for environment-bound configuration. It is not a production value set and is not interchangeable across deployment topologies.

## Validate before starting

For split deployments:

```bash
cp .env.example .env
# Replace samples and bind secrets through an approved authority.
docker compose config --quiet
```

The rendered Compose model should be reviewed before any migration or service starts. Standalone operators must remove or adapt split-only settings rather than passing the example unchanged.

## Configuration groups

Review at least:

* database provider, server flavor/version, runtime role, and migrator role;
* Keycloak authority, client, external URLs, and browser BFF settings;
* Cerbos or local RBAC intent;
* single-tenant or `DEPLOYMENT_MODE=multi_tenant` selection before onboarding;
* privacy-erasure authority topology;
* local or S3-compatible storage;
* SMTP and optional Listmonk;
* incoming callbacks and outgoing webhook mode;
* payment provider settings;
* optional MCP, forms, moderation, and federation capabilities.

Use structured provider settings. Do not invent an undocumented raw database connection-string shortcut or hard-code credentials in AppHost, Compose, settings files, or images.

## Authority rules

Secret values come only from Infisical, explicit environment injection, or deliberately selected shared .NET User Secrets in Development/Testing. If a selected provider is unavailable, unauthorized, or invalid, startup/readiness fails closed instead of reading a weaker fallback.

## Change procedure

1. Record the current non-secret rendered configuration.
2. Change one bounded configuration group.
3. Restart or redeploy; universal live credential/config refresh is not promised.
4. Verify migration, identity, authorization, tenant resolution, health, and the affected provider.
5. Keep values out of logs, screenshots, shell history, and support artifacts.

Treat deployment-mode, database-provider, erasure-topology, identity-provider, and authorization-provider changes as topology changes requiring backup and recovery planning.

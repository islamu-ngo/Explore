---
description: >-
  Use Aspire locally and adapt declared resources to adopter-owned cloud
  infrastructure.
---

# .NET Aspire & Cloud

.NET Aspire is the repository's development orchestrator and a foundation adopters can adapt. ISLAMU Event does not ship turnkey supported Azure or AWS production templates.

## Local orchestration

```bash
aspire run --apphost Explore.AppHost/Explore.AppHost.csproj
```

The AppHost declares resources, dependency ordering, service discovery, configuration injection, health checks, and restart relationships. Use the Aspire dashboard and resource logs to diagnose startup. Endpoints are dynamic; do not substitute Docker Compose port assumptions.

## Cloud adaptation

Aspire can target environments such as Azure Container Apps when an adopter supplies the deployment target and configuration. The repository does not provide a universal cloud responsibility model or native managed-secret adapters for Azure Key Vault or AWS Secrets Manager.

A production adaptation must define:

1. durable application and privacy-erasure databases;
2. object storage and metadata recovery;
3. Keycloak and authorization endpoints;
4. one-shot migration execution before serving traffic;
5. approved secret delivery through Environment or Infisical;
6. DNS, TLS, reverse-proxy, and trusted-forwarder policy;
7. health, logs, metrics, and alert ownership;
8. scaling constraints for SQLite/standalone versus server databases;
9. backup, restore rehearsal, upgrade, and rollback procedures.

## Acceptance

Do not call a cloud deployment supported merely because its containers start. Prove migrations, identity, authorization, tenant resolution, durable storage, privacy-erasure replay, representative reads/writes, configured provider delivery, and recovery from a restored backup.

Document every cloud service substitution and its owner. Framework capability is not the same as repository-supported infrastructure.

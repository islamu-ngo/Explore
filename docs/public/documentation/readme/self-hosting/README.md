---
description: >-
  Choose and operate the supported standalone, split, Coolify, or Aspire/cloud
  path.
---

# Self-Hosting

Select a topology by operational needs, then follow its dedicated runbook. The project is pre-1.0: pin versions and image digests, review release/API changes, and test backup restoration before upgrades.

## Deployment paths

| Path                                                       | Best fit                                                | Primary constraint                                                     |
| ---------------------------------------------------------- | ------------------------------------------------------- | ---------------------------------------------------------------------- |
| [Docker Standalone](docker-standalone.md)                  | Smallest deployment and lowest operating load           | One replica, durable SQLite/local volume, initial `linux/amd64` target |
| [Docker Compose](docker-compose.md)                        | Split services and a server database                    | One-shot migration service must complete before API/UI                 |
| [Coolify with Cerbos & Traefik](coolify-cerbos-traefik.md) | Existing Coolify/Traefik operators using Cerbos         | Cerbos runbook only, not a whole-platform one-click template           |
| [.NET Aspire & Cloud](dotnet-aspire-and-cloud.md)          | Development orchestration or adopter-owned cloud design | No turnkey Azure/AWS template or universal responsibility model        |

Kubernetes, Helm, ActivityPub infrastructure, first-party PDS/AppView hosting, and initial `linux/arm64` packaging are not implemented deployment options.

## Shared production gate

Every path must define durable state, migrations, identity, authorization, tenant binding, secrets, TLS/DNS, health, backups, restore rehearsal, upgrade, and rollback. Continue with [Configuration & Operations](../configuration-and-operations/) after choosing a topology.

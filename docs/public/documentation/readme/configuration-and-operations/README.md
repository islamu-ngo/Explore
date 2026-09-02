---
description: Configuration, secrets, backup, upgrade, health, and recovery runbooks.
---

# Configuration & Operations

This section covers the operating contract after you select a deployment topology. Keep configuration, secret values, application data, and privacy-erasure authority distinct; each has a different backup and trust boundary.

## In this section

* [Environment Variables](environment-variables.md) — render topology-specific settings from the repository schema.
* [Configuration Manifests](configuration-manifests.md) — move governed settings without secrets, PII, or application data.
* [Secrets](secrets.md) — select fail-closed authorities and rotate credentials safely.
* [Backup, Restore & Upgrade](backup-restore-upgrade.md) — protect every durable authority and rehearse recovery.
* [Troubleshooting & Health](troubleshooting-and-health.md) — diagnose startup, identity, policy, tenancy, and provider failures safely.

Start with environment rendering before migrations. Complete backup and restore rehearsal before every topology change or pre-1.0 upgrade.

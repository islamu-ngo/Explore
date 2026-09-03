---
description: Contribute under the AGPL, CLA, provenance, and dependency-review boundaries.
---

# Clean-Room IP & Licensing

ISLAMU Event is publicly licensed under **GNU AGPL-3.0-or-later**. Non-bot contributors sign the project Contributor License Agreement (CLA), which grants the project steward rights to distribute and preserve the platform.

> [!NOTE]
> This page summarizes engineering policy, not formal legal advice.

---

## Clean-Room Contribution Invariants

To safeguard the sovereign open-source nature of the project and protect outbound licensing paths:

* **No Ingestion of Copyleft / Proprietary Source**: Never copy code, SQL, migrations, tests, comments, documentation prose, or assets from third-party proprietary software or incompatible copyleft platforms.
* **Independent Creation**: When studying external features or public protocols:
  1. Record source URLs, access dates, and factual protocol behavior.
  2. Produce a source-free functional specification containing inputs, outputs, and wire contracts.
  3. Author the implementation independently using repository-native Clean Architecture conventions (see [Clean Architecture Conventions](clean-architecture.md)).
  4. Ensure all public standard identifiers (e.g. AT Protocol [Lexicons](../federation-and-open-protocols/lexicons.md)) match wire requirements while surrounding expression remains original.

---

## Dependency & License Review

Before introducing a new NuGet package or external library:
* Verify the package uses an approved permissive open-source license (MIT, Apache 2.0, BSD).
* Incompatible licenses (GPLv2-only, SSPL, BSL) are strictly forbidden.

---

## Related Guides & Next Steps

* **[Contributing Overview](README.md)** — Community guidelines and contribution channels.
* **[Local Development Guide](local-development.md)** — Setting up your workstation with .NET 10.
* **[Clean Architecture Conventions](clean-architecture.md)** — Architectural patterns and code structure.
* **[Lexicons Reference](../federation-and-open-protocols/lexicons.md)** — Open standard protocol schemas and identifiers.

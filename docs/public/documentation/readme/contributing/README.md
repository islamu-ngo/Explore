---
description: How to contribute to ISLAMU Event through issues, documentation, translations, and code.
---

# Contributing to ISLAMU Event

Welcome! We are building a free, ethical, and community-sovereign event management platform. Whether you are reporting bugs, improving documentation, translating into your language, or contributing code, your help is deeply valued.

---

## In this Section

* **[Local Development Guide](local-development.md)** — Set up your developer workstation with .NET 10, Docker, and .NET Aspire.
* **[Clean Architecture Conventions](clean-architecture.md)** — Understand domain invariants, MediatR CQRS slices, and HAL link assembly.
* **[TUnit Testing Conventions](tunit.md)** — Run fast, targeted unit and integration test slices with TUnit.
* **[Clean-Room IP & Licensing](clean-room-ip-and-licensing.md)** — AGPLv3 guidelines, CLA requirements, and clean-room provenance rules.

---

## Ways to Contribute

### 1. Report Bugs & Request Features
If you discover a bug or have an idea for an enhancement:
- Search existing [GitHub Issues](https://github.com/islamu-ngo/Event/issues) to avoid duplicates.
- Open an issue with detailed reproduction steps, environment details (Docker version, browser, OS), and logs.

### 2. Localization & Translations (Weblate)
ISLAMU Event is an internationalized, multi-lingual platform:
- We use [Weblate](https://hosted.weblate.org/) for crowdsourced translations.
- Help translate UI strings, emails, and documentation into Arabic, French, Urdu, Bahasa, and other community languages without writing code.

### 3. Improve Public Documentation
Documentation improvements are first-class contributions:
- If you find a typo, broken link, or unclear explanation, submit a PR directly against the `docs/public/` directory in our [GitHub Repository](https://github.com/islamu-ngo/Event).

### 4. Code Contributions
- Review our [Local Development Guide](local-development.md) and [Clean Architecture Conventions](clean-architecture.md).
- Ensure all pull requests include invariant-oriented [TUnit Tests](tunit.md).

---

## Related Guides & Next Steps

* **[Local Development Guide](local-development.md)** — Step-by-step developer workstation setup.
* **[Architecture & Request Flows](../getting-started/architecture-and-request-flows.md)** — Overview of CQRS commands and transactional outbox.
* **[Clean-Room IP & Licensing](clean-room-ip-and-licensing.md)** — Provenance and outbound licensing protection.
* **[Docker Compose Runbook](../self-hosting/docker-compose.md)** — Run the split service topology locally.

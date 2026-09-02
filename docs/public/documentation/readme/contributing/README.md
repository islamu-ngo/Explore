---
description: >-
  Local development, architecture, testing, release governance, and clean-room
  contribution rules.
---

# Contributing

Contributions should preserve the platform's Clean Architecture boundaries, fail-closed security model, generated-contract workflow, and clean-room provenance.

## In this section

* [Local Development](local-development.md) — prerequisites, Aspire startup, and the local verification loop.
* [Clean Architecture](clean-architecture.md) — layer ownership, CQRS, repositories, and HAL assembly.
* [TUnit](tunit.md) — focused project-level tests and risk-based evidence.
* [Clean-Room IP & Licensing](clean-room-ip-and-licensing.md) — AGPL, CLA, provenance, and dependency review.

## Public contract discipline

The API is currently version `0.1` and the platform is pre-1.0. Breaking changes are allowed when they simplify the architecture, but public contract changes must update the canonical API changelog and regenerate governed OpenAPI and client artifacts.

## Release governance

Current releases use manually created SemVer tags and GitHub Releases under the release checklist. The proposed signed release engine and promoted-artifact authority are future governance, not active automation. Release notes must tell adopters what changed, how to upgrade, how to verify, and how to recover or roll back.

Start with [Local Development](local-development.md), then read the architecture and testing pages before changing product code.

<!-- ABOUTME: Tracks intentionally skipped API contract tests with owners and removal criteria. -->
<!-- ABOUTME: Keeps deferred contract enforcement visible until the owning stabilization plan removes the skips. -->

# API Contract Test Debt

> **Audience:** API maintainers | Release reviewers | AI agents
> **Status:** Active debt inventory
> **Owner:** Platform/Ops
> **Last Verified:** 2026-06-01
> **Source Anchors:** `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs`, `.github/workflows/openapi-contract.yml`

This inventory exists so skipped API contract tests cannot become invisible release debt. `OpenAPI Contract Guard` validates that every skipped test whose skip reason includes `Category: API contract` is listed here with an owner and a removal condition.

Do not add new skipped API contract tests without updating this file in the same change. Prefer enabling the test or narrowing the assertion before adding a skip.

## Active Skipped API Contract Tests

| Test | File | Category | Owner | Removal condition | Why still skipped | Promotion path |
|---|---|---|---|---|---|---|
## Removal Rules

- A skipped API contract test must have `Category: API contract` in the code skip reason.
- The code skip reason must include a concrete `Removal:` clause.
- This inventory must include the test method name, file path, owner, and removal condition.
- Removing a skip requires updating this file in the same PR so no resolved debt remains listed.

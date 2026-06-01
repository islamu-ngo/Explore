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
| `RouteNames_EveryConstantResolvesToExactlyOneEndpoint` | `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs` | API contract / HATEOAS route-name coverage | Platform/Ops + API contract stabilization owner | Enable after write actions are decorated with `[HttpXxx(Name = RouteNames.X)]` in `dev/active/api-contract-stabilization` Phase 3. | The current generated inventory still marks many `RouteName` values as `(Phase 1.4)`, so enabling this now would mix route-name stabilization work into CI/CD hardening. | Finish route-name decoration, remove the `Skip` attribute, and delete this row in the same PR. |
| `EndpointRouteNames_EveryNamedEndpointHasMatchingConstant` | `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs` | API contract / HATEOAS route-name coverage | Platform/Ops + API contract stabilization owner | Enable after `RouteNames` constants are added for all endpoint route names in `dev/active/api-contract-stabilization` Phase 3. | The route-name registry is not yet the single source of truth for every named endpoint. Enabling this before the stabilization pass would create broad API/HATEOAS churn outside this CI/CD workstream. | Finish route-name constant coverage, remove the `Skip` attribute, and delete this row in the same PR. |

## Removal Rules

- A skipped API contract test must have `Category: API contract` in the code skip reason.
- The code skip reason must include a concrete `Removal:` clause.
- This inventory must include the test method name, file path, owner, and removal condition.
- Removing a skip requires updating this file in the same PR so no resolved debt remains listed.

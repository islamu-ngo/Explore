---
name: tests
description: Apply when editing unit, integration, architecture, or end-to-end test source files.
paths:
  - "**/*Tests/*.cs"
  - "**/*UnitTests/*.cs"
  - "**/*IntegrationTests/*.cs"
  - "**/*.Tests/*.cs"
related_skills:
  - criticality-guardrail
  - implementation-plan
  - senior-cto-feedback
related_docs: [docs/TESTING.md, AGENTS.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Event.Architecture.Tests]
related_intents: [add-get-endpoint, add-write-endpoint, add-cqrs-handler, add-ef-migration, update-repository-query, blazor-component-affordance, bff-auth-bug, openapi-contract-change]
---

<!-- ABOUTME: Apply when editing unit, integration, architecture, or end-to-end test source files. -->
<!-- ABOUTME: Twin copy at .agents/rules/tests.md. When modifying this file, update both paths. -->

# Test Rules

## Applies To
- All test projects and source files (`**/*Tests/*.cs`).

## Path-Specific Constraints
- **Clean-Architecture Sliced Execution**: Never run solution-wide tests or irrelevant downstream layer suites (e.g. no database integration tests when modifying Blazor UI or Application unit tests). During active subtask iteration, use TUnit tree-node filtering (`--treenode-filter "/*/*/*<TestClassName>/*"`) for fast ~1.5s feedback.
- **Test-First Invariant Specification**: Author failing contract/invariant tests *before* implementing production code (Red Phase). Never write code first and generate post-hoc tests that merely mirror implementation assumptions ("The Ugly Mirror").
- **High-Leverage Behavioral Assertions**: Assert against public contracts (MediatR requests, HTTP routes, ProblemDetails RFC 7807, database state invariants) rather than private implementation details. Prioritize concurrency races, state transitions, and real DB integration tests over shallow getter/setter mocks.
- **Pre-Agreed Public Seams**: Tests verify behavior through public interfaces (MediatR commands/queries, API endpoints, aggregate root methods), never by reaching into private internals or mocking internal collaborators.
- **No Tautological Assertions**: Assertions must never recompute expected values the same way the production code does (e.g. `Assert.Equal(items.Sum(x => x.Price), result.Total)`). Expected values must come from an independent, known-good source of truth (a literal constant, worked domain scenario, or specification).
- **No Interface Bypassing**: Tests must verify operations through the public contract. Prohibit verifying a command's success by querying the raw database table directly if the aggregate or query handler exposes that state.
- **Mock Boundary Rule**: Mock ONLY external systems (third-party payment, email delivery, system clock, randomness). Never mock internal entities, repositories, or application handlers when test database fixtures or real handlers can be executed.
- **Suite Integrity**: Failing tests must be fixed or investigated; never deleted to bypass failures.
- **Pristine Output**: Test runs must have zero stray warnings, stack traces, or noisy logs.
- **Runtime Realism**: Keep in-process integration tests deterministic; use explicit runtime lanes when real provider infrastructure is the behavior under test.
- **Project Role Balance**: Assertions must live in the project matching the host profile (e.g., Domain logic in `Domain.UnitTests`, not API tests).
- **Forward-Only Ratchets**: `ApiLiabilityRatchetTests` freezes each liability class as an **exact** allowlist, not a ceiling. Introducing an occurrence fails, and removing one without deleting its entry fails too — that second direction is what keeps the list shrinking. Every entry carries the reason it still exists. Never relax a ratchet to make a change pass; delist the entry the change actually fixed.
- **Substitute the Principal, Not the Identity Service**: controller tests must set real claims on `ControllerContext.HttpContext.User` rather than mocking `IUserContext` through the container. Mocking the service means the test never exercises the claim chain it claims to cover.
- **Assert Across a Split Family**: after a controller is partitioned, contract tests that look actions up by name must search the whole family (see `EventFamilyAction`, `WebhookFamilyAction`) rather than one hardcoded class — that is what the assertion always meant.
- **Container Runtime**: Testcontainers-backed tests need a Docker-compatible endpoint. Under Podman, export `DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock`, `TESTCONTAINERS_RYUK_DISABLED=true`, and `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE`. Without them the suite reports hundreds of `DockerUnavailableException` failures that look like a mass regression — check the endpoint before the code.
- **Zero Hard-Coded Test Secrets**: Tests must never define hard-coded passwords, tokens, API keys, or connection strings in test files or fixtures. Bind secrets via environment variables or mock secret providers (`ISecretResolver`) using keys documented in `.env.example`.
- **Record Contract Specifications**: Follow the [canonical record-selection policy](../../docs/GOVERNANCE.md#canonical-record-selection-policy). Start Red with consumed equality, one-fact `with` variants, caller-mutation isolation, JSON construction, PATCH omitted/clear/replacement behavior, and trust-boundary attacks. Keep record/body baselines exact, and keep the published-collection exceptional-disposition baseline exact: missing new debt and stale resolved entries must both fail.

## Must Read
- [docs/QUICK_REFERENCE.md#build-and-test-baseline](../../docs/QUICK_REFERENCE.md#build-and-test-baseline)
- [docs/TESTING.md](../../docs/TESTING.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: Run the specific test project using `--project` with `--configuration Release`.

## Related
- Intents: `add-get-endpoint`, `add-write-endpoint`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `blazor-component-affordance`, `bff-auth-bug`, `openapi-contract-change`
- Agents: `quality-verifier-agent.md`
- Rules: `application-layer.md`, `api-controllers.md`, `blazor-client.md`

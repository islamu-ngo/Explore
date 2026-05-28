ABOUTME: Working context for the enterprise CI/CD hardening implementation plan.
ABOUTME: Preserves repository evidence, decisions, constraints, and quick-resume notes for future sessions.

# Enterprise CI/CD Hardening Context

Last Updated: 2026-05-07

## Session Progress

- Completed workflow audit across all 8 GitHub Actions workflows.
- Completed repository exploration for planning conventions, OpenAPI generation, test taxonomy, governance, security, configuration, and operations constraints.
- Used Tavily research for current OpenAPI contract guard and GitHub Actions best-practice evidence.
- Used Context7 documentation for official ASP.NET Core OpenAPI behavior.
- Consulted Oracle for implementation sequencing and hidden risks.
- Created implementation plan, context, and tasks files under `dev/active/enterprise-ci-cd-hardening/`.
- Incorporated CTO feedback requiring smaller PR boundaries, exact OpenAPI guard commands, required/advisory check policy, fork PR security rules, artifact retention, and a Coolify digest decision gate.
- Implemented PR 1 CI correctness baseline: `_build-test.yml` now runs `Explore.Infrastructure.Tests`, publishes TRX artifacts, avoids starting Postgres for fast PR validation, and splits PostgreSQL-backed integration tests into a conditional job; `agent-context.yml` now uses `global.json`; `docs/TESTING.md` now documents 10 test projects.
- Implemented PR 2 OpenAPI contract guard: `.github/workflows/openapi-contract.yml` now runs as an always-present required-check candidate with internal no-op detection, uses the verified TUnit/MTP exporter command, rebuilds the Blazor generated client, fails on generated drift, uploads OpenAPI/client/TRX artifacts, and proves deterministic second-run behavior.
- Incorporated read-only review feedback: replaced unsupported `dotnet list package --vulnerable --count 0` with JSON-based NuGet vulnerability parsing, and widened the OpenAPI guard detector to include API project/startup/config files and API integration test project metadata.
- Verified the corrected NuGet audit command shape locally; it currently surfaces existing package vulnerabilities (8 including transitive dependencies, 1 top-level-only), so the CI gate is now technically valid but package remediation is required before the audit can pass.
- Implemented the Phase 3 workflow-hygiene slice: added explicit read-only permissions to remaining validation workflows, scoped deploy token permissions so only image-push jobs keep `packages: write`, added missing concurrency and timeouts, expanded auth/Cerbos path filters, and pinned the Cerbos binary version away from mutable `latest`.
- Implemented the Phase 4 security-gates slice: added Dependabot automation for GitHub Actions/NuGet updates, moved C# CodeQL to manual Release build with the pinned SDK, added `develop` and merge-queue coverage to CodeQL, added a PR-only dependency-review workflow, and retained security/Cerbos failure evidence as artifacts.
- Implemented the Phase 5 container-provenance slice: extracted reusable `_container-build.yml`, routed production/develop Coolify workflows through it, added commit-SHA image tags, Buildx SBOM/provenance, GHCR artifact attestations after the Trivy gate, image vulnerability scanning, digest JSON evidence, job summaries, and 90-day container evidence artifacts while preserving the existing Coolify webhook deployment contract.
- Implemented the Phase 6 protected-deployment slice: deploy jobs now bind to `staging`/`production` GitHub Environments, serialize by environment rather than ref, harden Coolify webhook calls with retry/timeout/status validation and transport-failure summaries, run bounded optional `/alive` + `/health` smoke checks when URL variables exist, redact/truncate smoke failure output, and upload 90-day deployment evidence.
- Implemented the Phase 7 runtime-lane slice: added `.github/workflows/e2e.yml` for manual/nightly Aspire-backed Playwright E2E execution, retained TRX/browser/Docker diagnostics, and scheduled the existing security and Cerbos policy workflows so full auth/policy checks run even without matching path changes.
- Incorporated Phase 7 review feedback: moved the scheduled `security-tests.yml` TUnit `--treenode-filter` arguments after the Microsoft.Testing.Platform `--` separator so nightly Security and PolicyContract filters are applied consistently with `docs/TESTING.md`.
- Implemented the Phase 8 governance-documentation slice: added `docs/CI_CD_GOVERNANCE.md`, linked it from governance/operations/testing/release docs, documented branch-protection rulesets, required vs advisory gates, GitHub Environment settings, fork PR policy, generated-artifact review rules, and artifact retention, and removed README Codecov/SonarCloud badges until those gates are actually implemented.
- Implemented the Phase 9 action-pinning slice: replaced external GitHub Actions `uses:` tag references with full-length commit SHAs plus same-line version comments, while preserving local reusable workflow calls as path references; Dependabot `github-actions` automation remains responsible for keeping SHA pins maintainable.
- Implemented the Phase 1B evidence-summary follow-up: `_build-test.yml` now assigns stable IDs to each fast/integration test step and writes always-running GitHub job summaries that map project names to step outcomes and the `test-results-fast` / `test-results-integration` artifacts.
- Implemented the Phase 1C locked-restore follow-up for GitHub Actions and deployable Dockerfiles: all tracked project directories have matching `packages.lock.json` files; workflows now call `dotnet restore --locked-mode`; `Explore.API/Dockerfile` and `Explore.Blazor/Dockerfile` copy root restore inputs, referenced project files, and relevant lock files before locked restore; and `docs/CI_CD_GOVERNANCE.md` documents how package updates must include regenerated lock files.
- Implemented the OpenAPI stable-invariant follow-up: `.github/workflows/openapi-contract.yml` now builds `Event.API.IntegrationTests` and runs the proven `OpenApiDocument_*` invariant subset with TUnit/MTP before generated-artifact drift is accepted.

## User Request

Create an implementation plan based on the GitHub workflow audit report. The plan must:

- be enterprise-grade and maintainable;
- follow repository conventions and industry best practices;
- include a dedicated CI OpenAPI contract guard;
- ensure OpenAPI schema/client artifacts are regenerated when API-surface changes require regeneration;
- avoid forcing regeneration for unrelated changes;
- use Tavily MCP for research and Context7 MCP for documentation;
- produce `dev/active/[task-name]/[task-name]-plan.md`, `-context.md`, and `-tasks.md`.

## Key Repository Conventions

- `dev/active/README.md` defines the planning structure:
  - `dev/active/[task-name]/[task-name]-plan.md`
  - `dev/active/[task-name]/[task-name]-context.md`
  - `dev/active/[task-name]/[task-name]-tasks.md`
- `AGENTS.md` requires:
  - Clean Architecture boundaries;
  - repositories return entities, not DTOs;
  - validators are manually instantiated in handlers;
  - GET endpoints are `[AllowAnonymous]`, write endpoints `[Authorize]`, admin endpoints `[Authorize(Roles="Admin")]`;
  - HAL links are the source of truth for UI affordances;
  - every file starts with two `ABOUTME` comments;
  - canonical build baseline is `dotnet build --configuration Release --verbosity quiet`.
- `docs/TESTING.md` says tests must run per project; do not use solution-level `dotnet test`.
- `docs/GOVERNANCE.md` treats OpenAPI as a governed artifact and says `schemas/openapi.json` is generated, reviewed, and never hand-edited.
- `docs/OPERATIONS.md` defines `/alive`, `/health`, and `/metrics` operational semantics.
- `docs/SECURITY-MODEL.md` defines BFF token boundaries and authorization trust boundaries.
- `docs/CONFIGURATION.md` defines runtime configuration layers and secret-handling expectations.

## Current Workflow Files

- `.github/workflows/_build-test.yml`
- `.github/workflows/agent-context.yml`
- `.github/workflows/cerbos-policy-check.yml`
- `.github/workflows/codeql.yml`
- `.github/workflows/_container-build.yml`
- `.github/workflows/dependency-review.yml`
- `.github/workflows/deploy-coolify-develop.yml`
- `.github/workflows/deploy-coolify.yml`
- `.github/workflows/e2e.yml`
- `.github/workflows/openapi-contract.yml`
- `.github/workflows/security-tests.yml`
- `.github/workflows/test.yml`

## Workflow Audit Findings

### Cross-Cutting

- PR3 now gives workflows explicit least-privilege `permissions`; deploy jobs that do not need `GITHUB_TOKEN` use `permissions: {}`, while image-push jobs keep `contents: read` and `packages: write`.
- External GitHub Actions are pinned to full-length commit SHAs with same-line version comments; local reusable workflows remain path-based.
- PR1/PR2/PR4/PR5/PR7 now add TRX/OpenAPI/security/Cerbos/container/E2E artifact uploads; coverage remains future work.
- Container build jobs now use `id-token`, `attestations`, `artifact-metadata`, `provenance`, and `sbom`; GitHub Environments, Codecov, and SonarCloud remain future work.
- PR6 now adds optional bounded deploy smoke checks when environment URL variables are configured.
- Deploy workflows are duplicated between production and develop.
- Phase 8 now centralizes repository-settings governance in `docs/CI_CD_GOVERNANCE.md`; branch protection, production reviewers, secret scanning, push protection, and environment restrictions remain GitHub settings to verify outside the local repo.

### Build/Test

- PR1 moved Postgres into the conditional integration job so fast PR validation no longer starts database infrastructure.
- PR1 added `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` to the fast test lane.
- PR7 adds a manual/nightly workflow for `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj`.
- PR1 now uploads TRX artifacts for fast and integration test lanes, and the Phase 1B follow-up writes job summaries that point maintainers to the relevant project outcomes and artifact names; coverage artifacts remain future work.
- GitHub Actions restore and deployable Docker build restore now use `dotnet restore --locked-mode`; Docker image build validation still needs a clean CI run because the local workspace has unrelated compile/package issues.

### Security

- `codeql.yml` now uses manual build mode for C# so analysis sees the repository's pinned SDK and canonical Release build graph.
- `security-tests.yml` path filters now cover known auth/BFF/trust-boundary files from PR3; future cleanup should keep these filters aligned with `docs/SECURITY-MODEL.md`.
- `cerbos-policy-check.yml` now pins the Cerbos binary to a concrete release instead of `latest`.

### Deploy

- `deploy-coolify.yml` and `deploy-coolify-develop.yml` now call `_container-build.yml` for image build evidence, but Coolify still deploys according to its existing webhook-configured image/tag source.
- GHCR is the Phase 5 evidence registry for digest scans and GitHub artifact attestations; ATCR is still pushed for the existing Coolify deployment contract until Phase 6 decides digest versus immutable-tag consumption.
- Phase 6 adds GitHub Environment bindings on normal deploy jobs (`staging`, `production`), hardened Coolify webhook calls, deployment summaries/artifacts, and optional `/alive` + `/health` smoke checks when environment URL variables are configured.
- Deploy concurrency is keyed by environment (`deploy-staging`, `deploy-production`) so tag/manual/branch deployments cannot overlap inside the same environment.
- Deploy jobs have a 20-minute timeout; optional smoke checks are bounded to ten attempts per endpoint with 10-second request timeouts and 5-second sleeps so failure handling can write summaries before the job timeout.
- Deploy secrets should live in GitHub Environment secrets. Production reviewer/branch restrictions are repository settings and must be configured outside YAML before the production gate is considered fully enforced.
- Coolify still deploys according to its existing webhook-configured image/tag source. `docs/OPERATIONS.md` now documents digest-preferred deployment and immutable SHA tag fallback as the Phase 6 decision path.

## Runtime Lane Findings

- `.github/workflows/e2e.yml` is advisory only: it has `workflow_dispatch` and nightly `schedule` triggers, but no PR trigger, so it is not a required merge gate until reliability is proven.
- `Explore.Blazor.Client.E2ETests` starts Aspire internally through `DistributedApplicationTestingBuilder.CreateAsync<Projects.Explore_AppHost>()`; the workflow must not start a second external AppHost with `aspire start` for this suite.
- E2E tests require Docker because fixtures start Testcontainers PostgreSQL (`postgres:18-alpine`) and Keycloak (`quay.io/keycloak/keycloak:26.1.2`).
- The workflow installs Chromium with OS dependencies using the generated Playwright PowerShell script after building the E2E project.
- The E2E command runs the whole project without category filtering because only `SmokeTests` currently has `[Category("E2E")]`; untagged critical flows would be skipped by a category-only filter.
- Browser artifacts are emitted under `Explore.Blazor.Client.E2ETests/bin/Release/net10.0/TestResults/playwright-artifacts`; the workflow also captures TRX, the test log, and Docker container diagnostics.
- OIDC should be preferred where platform/registry supports it; Coolify webhook token auth remains the current deployment contract.

## OpenAPI Evidence

### Source Files

- `Explore.API/Program.cs`
  - `builder.Services.AddOpenApi("event-api", ...)`
  - `app.MapOpenApi()` in Development/Testing
- `Explore.API/OpenApi/OperationIdInvariantTransformer.cs`
- `Explore.API/OpenApi/EndpointClassificationTransformer.cs`
- `Explore.API/OpenApi/HalSchemaTransformer.cs`
- `Explore.API/OpenApi/HalDtoSchemaTransformer.cs`
- `Explore.API/Explore.API.csproj`
- `Event.API.IntegrationTests/Features/ApiContractInventoryGeneratorTests.cs`
- `Event.API.IntegrationTests/Features/ContractInvariantsTests.cs`
- `Explore.Blazor.Client/Explore.Blazor.Client.csproj`
- `Explore.Blazor.Client/nswag.json`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `schemas/openapi.json`

### OpenAPI Flow

1. Runtime exposes `/openapi/event-api.json` in Development/Testing for inspection and assertion-style tests.
2. Building `Explore.API/Explore.API.csproj` refreshes `schemas/openapi.json` through ASP.NET Core build-time OpenAPI generation.
3. `Explore.Blazor.Client.csproj` runs NSwag before compile when `schemas/openapi.json` is present/changed.
4. NSwag writes `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
5. Contract tests and architecture tests validate operation IDs, endpoint classification, URL-versioning bans, and generated-client naming.

### Guard Design Decision

Use the API build-time OpenAPI target as the canonical CI regeneration path. CI should fail on drift by comparing committed generated artifacts after regeneration and should reject the SDK suffix artifact (`openapi_event-api.json`) if it appears.

The guard also runs stable OpenAPI invariant tests with:

```bash
dotnet run --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build -- --treenode-filter "/*/*/*/OpenApiDocument_*" --minimum-expected-tests 5 --no-progress --report-trx --report-trx-filename openapi-invariants.trx
```

This intentionally excludes timestamped inventory generation and skipped route-name coverage tests until those outputs/tests are deterministic and stable.

Files to compare:

- `schemas/openapi.json`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- `docs/API_CONTRACT_INVENTORY.md` when inventory generation is in scope

## Test Inventory

Earlier docs mentioned 9 projects; PR1 updated `docs/TESTING.md` after repo inventory found 10 relevant test projects:

- `Event.Domain.UnitTests/Event.Domain.UnitTests.csproj`
- `Event.Application.UnitTests/Event.Application.UnitTests.csproj`
- `Event.Architecture.Tests/Event.Architecture.Tests.csproj`
- `Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj`
- `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj`
- `Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj`
- `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`
- `Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`
- `Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`
- `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj`

## External Research Summary

### Context7 / ASP.NET Core OpenAPI

- Official ASP.NET Core docs confirm `AddOpenApi` and `MapOpenApi` runtime generation.
- `MapOpenApi` is commonly restricted to Development to avoid exposing sensitive metadata.
- .NET 10 supports programmatic OpenAPI document access outside HTTP contexts.
- Build-time generation is suitable when generated OpenAPI files are committed to source control, used for spec-based testing, or served statically.

### Tavily / OpenAPI Guard

- Microsoft .NET Blog recommends integrating OpenAPI breaking-change tools such as `oasdiff` into CI/CD to fail PRs with breaking changes and guide contributors toward API versioning.
- OpenAPI linting/diff tools:
  - Spectral for quality/linting;
  - oasdiff for breaking-change detection;
  - OpenAPI Diff as an alternative.
- GitHub required workflows with path filters can create pending-check problems; prefer always-running required workflow with internal no-op detection.

### GitHub Actions Blueprint Research

- Use reusable workflows with explicit input/secret boundaries.
- Use least-privilege permissions per job.
- Use protected environments for production deployments.
- Use concurrency for deploy jobs.
- Prefer OIDC and short-lived credentials.
- Emit SBOM/provenance for container builds.
- Upload TRX/coverage/build/deploy artifacts.
- Pin actions to SHAs and use Dependabot/Renovate for updates.

## Oracle Guidance

- Treat this as a phased hardening program, not a single rewrite.
- Start with deterministic build/test correctness, then contract/security gates, then deployment protection and provenance.
- OpenAPI guard should use a contract snapshot model: generate, normalize, compare to committed baseline, fail if drift is not committed.
- Avoid advanced gates before core lanes are stable; otherwise developers will distrust CI.
- Full rollout is large; first useful milestone is baseline CI + least privilege + OpenAPI drift guard + artifacts.

## CTO Feedback Incorporated

The CTO verdict approved the planning direction but blocked a single giant implementation PR. The updated plan now treats these as execution constraints:

- Keep current workflow names in the first milestones; required-check renames require branch-protection coordination.
- Split implementation into focused PRs:
  1. CI correctness baseline;
  2. OpenAPI drift guard;
  3. workflow hygiene/permissions/timeouts/concurrency/Cerbos fixed version;
  4. action SHA pinning plus dependency/security review;
  5. protected deployments;
  6. container provenance;
  7. nightly E2E/runtime lanes.
- Split Phase 1 into:
  - Phase 1A test inventory correctness;
  - Phase 1B TRX/evidence artifacts;
  - Phase 1C CI efficiency and restore policy.
- Keep `oasdiff` advisory until operation IDs, versioning policy, HAL schema behavior, client generation, and skipped contract tests are stable enough for breaking-change enforcement.
- Add deterministic OpenAPI acceptance criterion: running the guard twice on the same commit must produce zero diff on the second run.
- Add an explicit required/advisory matrix so enterprise-grade does not mean every signal blocks every PR.
- Add a fork PR security policy: external fork PRs must not receive deployment secrets, registry write credentials, environment secrets, or privileged tokens; avoid `pull_request_target` for untrusted build/test/generation jobs.
- Add generated-artifact review rules for operation IDs, routes, auth metadata, DTO shapes, HAL changes, generated client method names, and removed/renamed endpoints.
- Add artifact retention expectations for TRX/logs, OpenAPI drift artifacts, security outputs, SBOM/provenance, and deploy logs.
- Add a Coolify deployment artifact decision gate: deploy explicit digest if supported; otherwise deploy immutable commit-SHA tag and record resolved digest; never use `latest` as production source of truth.

## Decisions Made

1. Task folder: `dev/active/enterprise-ci-cd-hardening/`.
2. Keep implementation incremental; do not collapse this hardening program into one giant PR.
3. Treat OpenAPI contract guard as the first signature business-value CI improvement after baseline CI correctness.
4. Use the `Explore.API` build-time OpenAPI target and the existing Blazor NSwag target rather than reintroducing a runtime/test exporter path.
5. Keep E2E/manual visual checks out of fast PR gate until nightly/manual reliability is proven.
6. Collapse deploy YAML later, after image build artifacts/digests are available.
7. Do not rename or consolidate workflows in the first implementation PRs unless branch protection has already been migrated.
8. Separate least-privilege permissions from full action SHA pinning to keep review diffs manageable.
9. Prefer manual-build CodeQL for this repo if moving away from `build-mode: none`, because the repo has a canonical Release build and pinned preview SDK.
10. Exclude `docs/API_CONTRACT_INVENTORY.md` from the PR2 strict OpenAPI guard until the inventory generator is included in that job.
11. Keep the OpenAPI relevance detector conservative: broad API project/startup/config and API integration test project changes should run the guard rather than silently no-op.

## Quick Resume

If continuing implementation:

1. Continue with final verification and repository-settings follow-up from `enterprise-ci-cd-hardening-tasks.md` unless Phase 8 review reveals a concrete defect.
2. Do not rename required checks until branch protection impacts are known.
3. Preserve the PR1 through PR8 baselines unless a verification failure proves they need correction.
4. For OpenAPI contract generation, use the build-time generation path:
   - `dotnet build Explore.API/Explore.API.csproj --configuration Release --no-restore --verbosity minimal`
   - `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet`
   - `git diff --exit-code -- schemas/openapi.json Explore.Blazor.Client/Clients/EventApiClient.g.cs`
5. Upload artifacts before enforcing stricter gates.
6. Prove OpenAPI determinism by running the guard twice on the same commit and verifying the second run produces zero diff.
7. Do not commit unless explicitly asked.

## Potential Risks & Unknowns

- Exact current branch-protection and environment settings are not visible from local repo files.
- Whether Coolify supports digest-based deploys through the current webhook contract needs verification.
- If Coolify cannot deploy a digest directly, the fallback must be immutable commit-SHA tags plus post-deploy digest recording; mutable `latest` must not remain production source of truth.
- Docker image build validation still needs a clean CI run because the local workspace has unrelated compile/package issues; local syntax validation cannot prove full container publish success.
- Some OpenAPI/ApiClient tests are currently skipped; enabling them may require separate stabilization work.
- OIDC availability depends on registry/deployment platform support; static secrets may remain temporarily.
- Required-check names must be coordinated with repository settings before workflow renames are merged.
- The old TUnit exporter was retired by OpenAPI modernization Phase 5; do not restore the `SwaggerJson_Export_WritesPrettyPrintedDocToExploreApi` command. Use the API build-time OpenAPI target for the guard.
- The NuGet vulnerability audit now fails on detected vulnerabilities instead of using the unsupported `--count 0` option; remediate current vulnerable packages or explicitly decide whether transitive vulnerabilities are advisory before making this a required branch-protection gate.
- Cerbos binary version is pinned to the latest observed release tag during implementation; keep it maintained through the later action-update automation task.

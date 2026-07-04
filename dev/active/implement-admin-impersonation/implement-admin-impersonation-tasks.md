<!-- ABOUTME: Implementation task checklist for admin support access and break-glass impersonation. -->
<!-- ABOUTME: Breaks the work into scoped, verifiable phases across Domain, Application, API, BFF, UI, ops, and tests. -->

# Admin Support Access / Break-Glass Impersonation Tasks

Last Updated: 2026-07-04 Europe/Brussels

## Status Summary

Planning status: re-baselined and accepted for implementation.

Implementation status: core support-access architecture implemented across Domain, Application, Persistence, Infrastructure, API, BFF, and Blazor Client. API contract hardening covers the support-access controller lifecycle and audit routes, local/Cerbos parity hardening covers support-access lifecycle/evidence decisions, provider-layer support-access enforcement filters handler and HAL authorization batches for inactive/read-only/cross-tenant forwarded sessions, backend observability covers lifecycle logs, bounded metrics, trace tags, and alert-worthy denial paths, the operator console exposes start/history/audit/force-stop workflows through HAL-backed BFF calls, and tenant administration exposes a read-only support evidence view. Remaining verification work is authenticated E2E coverage and OpenAPI/generated-client clean-state verification; broad API/Persistence integration suites were run and are not green because of unrelated failures documented below.

Completed in planning:

- [x] Reviewed old impersonation plan.
- [x] Loaded repo contract, dev-docs command, docs, rules, and skills.
- [x] Inspected current admin authority, BFF claims, YARP, sanitizer, audit, and unit-of-work source.
- [x] Completed Tavily and Context7 research.
- [x] Rewrote plan/context/tasks around support-access sessions.
- [x] Implemented support-access Domain/Application/Persistence foundation.
- [x] Added focused lifecycle and persistence tests for the foundation slice.
- [x] Implemented runtime CQRS/API, local/Cerbos authorization vocabulary, trusted BFF forwarding, global Blazor active-session banner, and per-request audit middleware.
- [x] Hardened BFF support-access header stripping/forwarding and added focused trust-boundary tests.
- [x] Added focused API integration/contract tests for support-access fail-closed settings, lifecycle, HAL links, and audit history.
- [x] Added local fallback, support-session context, and Cerbos PDP contract tests for support-access lifecycle/evidence authority and write-mode session validation.
- [x] Added provider-layer support-access boundary enforcement for stale/disabled/stopped/expired/revoked forwarded sessions, read-only write denial, cross-tenant denial, and HAL-style authorization batches.
- [x] Added backend observability for support-access lifecycle, request-audit persistence, session-validation denials, provider-boundary denials, active-session trace tags, and alert-worthy structured logs.
- [x] Added operator support-access console UX for tenant selection, mode/duration/reason/ticket capture, session history, audit event drill-in, and force-stop.
- [x] Added BFF history, audit, and force-stop endpoints with antiforgery on unsafe operations and focused BFF integration tests.
- [x] Added Blazor client support-access resource models and tests proving HAL-only affordance gating for start/audit/force-stop.
- [x] Added visual smoke evidence for unauthenticated/setup shell states.
- [x] Recorded current verification status and unrelated repository blockers.

## Phase 0: Pre-Implementation Gate

- [x] User approves the support-access direction and default product decisions by requesting implementation.
- [x] Implementation agent re-reads `AGENTS.md`, `.claude/contract/intents.yaml`, matching `.claude/rules/*.md`, and the three workstream files.
- [x] Create or select implementation branch. Current branch: `develop`.
- [x] Record starting build/test status in `implement-admin-impersonation-context.md`.

## Phase 1: Foundation, Contracts, And Authorization Vocabulary

- [x] Add support-access constants and trusted header names.
- [x] Add fail-closed support-access settings/governance keys.
- [x] Add `ISupportAccessContext`.
- [x] Add `ISupportAccessSessionService`.
- [x] Add `ISupportAccessSessionRepository`.
- [x] Add `ISupportAccessAuditEventRepository`.
- [x] Add support-access resource kinds/actions.
- [x] Update local authorization policy vocabulary.
- [x] Update Cerbos policy vocabulary.
- [x] Add local/Cerbos parity tests for start/stop/read/write/force-stop decisions. Coverage now includes local fallback action matrices, support-session read-only/write-mode context validation, provider-layer support boundary tests, and Cerbos PDP policy contract/loadability tests.
- [x] Update `docs/AUTHORIZATION.md` for the new resource/action model.

## Phase 2: Domain And Persistence

- [x] Add `Explore.Domain/SupportAccessSession.cs`.
- [x] Add `Explore.Domain/SupportAccessAuditEvent.cs`.
- [x] Add lookup enums/entities for session status, mode, event type, and end reason.
- [x] Add domain lifecycle tests for start, stop, expire, revoke, and invalid transitions.
- [x] Add EF configurations for support-access entities.
- [x] Add DbSets to `ExploreDbContext`.
- [x] Add repository implementations.
- [x] Register repositories in persistence DI.
- [x] Generate EF migration and model snapshot updates.
- [x] Add indexes for active session lookup and audit queries.
- [x] Add persistence integration tests for tenant scoping, active-session constraints, and audit queries.
- [x] Update `schemas/islamu-event.md`.

## Phase 3: Application, API, Authorization Runtime, And HAL

- [x] Add DTOs for start/stop/status/list/audit responses.
- [x] Add `StartSupportAccessSessionCommand` and validator.
- [x] Add `StopSupportAccessSessionCommand` and validator.
- [x] Add `ForceStopSupportAccessSessionCommand` and validator.
- [x] Add `GetCurrentSupportAccessSessionRequest`.
- [x] Add `ListSupportAccessSessionsRequest`.
- [x] Add `GetSupportAccessAuditEventsRequest`.
- [x] Implement handlers with manual validators and `IUnitOfWork` transaction boundaries.
- [x] Ensure handlers do not mutate `TenantUserRoleGrant`.
- [x] Add API middleware or scoped binder for trusted support-access session validation.
- [x] Extend authorization behavior/provider input with `ISupportAccessContext`.
- [x] Add `SupportAccessController`.
- [x] Add API integration/contract tests for support-access controller policy, lifecycle, HAL, and audit behavior.
- [x] Add `RouteNames` constants.
- [x] Add `ProblemDetails` metadata and idempotency/rate-limit behavior for write endpoints.
- [x] Add HAL policies/links for start, stop, status, and audit.
- [x] Ensure existing tenant resource links honor support-access context only through the provider pipeline.
- [x] Regenerate OpenAPI/NSwag client if the public API contract changes.
- [x] Update `docs/API.md` and `docs/API_CHANGELOG.md`.

## Phase 4: BFF Boundary

- [x] Add `BffSupportAccessEndpoints.cs`.
- [x] Map support-access endpoints from `BffEndpointExtensions`.
- [x] Add BFF service for resolving and storing opaque active session references server-side.
- [x] Add `GET /bff/support-access/current`.
- [x] Add `POST /bff/support-access/sessions` with `.RequireAuthorization()` and `.ValidateAntiforgery()`.
- [x] Add `POST /bff/support-access/sessions/current/stop` with `.RequireAuthorization()` and `.ValidateAntiforgery()`.
- [x] Add `GET /bff/support-access/tenants/{targetTenantId}/sessions`.
- [x] Add `GET /bff/support-access/tenants/{targetTenantId}/sessions/{sessionId}/audit-events`.
- [x] Add `POST /bff/support-access/sessions/{sessionId}/force-stop` with `.RequireAuthorization()` and `.ValidateAntiforgery()`.
- [x] Extend `BffProxyHeaderSanitizer` to remove all support-access headers.
- [x] Extend YARP transforms to add only trusted server-owned support-access session header.
- [x] Add server-side HttpClient forwarding handler for support-access context where needed.
- [x] Add BFF integration tests proving browser-supplied support headers are ignored.
- [x] Add BFF integration tests proving trusted support headers are added only for active owned sessions.
- [x] Add BFF integration tests for session history forwarding, antiforgery fail-closed force-stop, and current-session store clearing after force-stop.
- [x] Update `docs/BLAZOR.md` and `docs/SECURITY-MODEL.md`.

## Phase 5: Blazor Client UX

- [x] Add `ISupportAccessClientService` client contract and separate `SupportAccessCommandResult` model.
- [x] Add `SupportAccessClientService` that calls BFF endpoints.
- [x] Add admin support-access start console under instance admin settings.
- [x] Add tenant selector and optional target-user id field without replacing actor identity.
- [x] Add mode/duration controls with policy caps.
- [x] Add reason and ticket/reference capture.
- [x] Add persistent active session banner.
- [x] Add stop action wired through service and BFF-confirmed current-session state.
- [x] Add instance/operator support-access session history and audit viewer.
- [x] Add tenant-facing support-access evidence view if required for the first release.
- [x] Add CSS isolation files using project BEM conventions.
- [x] Add focused component/service assertions for HAL-only affordance gating in the client service layer.
- [x] Add Blazor client tests for HAL-only affordance gating.
- [x] Run visual QA smoke for desktop and mobile shell layouts. Authenticated operator-console interaction still needs browser E2E coverage.

## Phase 6: Observability, Operations, And Docs

- [x] Add structured logs for start, stop, expire, revoke, deny, and force-stop.
- [x] Add Prometheus metrics with bounded labels.
- [x] Add trace/activity tags for active support sessions.
- [x] Add alert-worthy events for write-mode, forced stop, kill-switch denial, cross-tenant mismatch, and audit persistence failure.
- [x] Add per-request support-access audit middleware for active API sessions with bounded metadata and safe failure logging.
- [x] Add operator kill-switch documentation.
- [x] Add active-session inspection and force-stop runbook.
- [x] Add retention/backup notes for support-access audit evidence.
- [x] Update `docs/CONFIGURATION.md`.
- [x] Update `docs/SELF_HOSTING.md`.
- [x] Update `docs/OPERATIONS.md`.
- [x] Update `docs/MULTI_TENANCY.md`.
- [x] Update `docs/TESTING.md`.

## Phase 7: Final Verification And Hardening

- [x] Run `dotnet build --configuration Release --verbosity quiet`.
- [x] Run `Event.Domain.UnitTests`.
- [x] Run `Event.Application.UnitTests`.
- [x] Run `Event.Persistence.IntegrationTests`. Current full suite is not green; see latest full-suite verification.
- [x] Run `Event.API.IntegrationTests`. Current full suite is not green; see latest full-suite verification.
- [x] Run `Explore.Blazor.IntegrationTests`.
- [x] Run `Explore.Blazor.Client.Tests`.
- [x] Run `Event.Architecture.Tests`.
- [ ] Run E2E/manual Aspire support-access flow if full browser coverage is not automated.
- [ ] Verify OpenAPI/NSwag generated client state is clean.
- [x] Review all docs changed in the slice.
- [x] Update this task file with completed commands and outcomes.
- [x] Update context file with final implementation handoff notes.

Latest foundational-slice verification, 2026-07-04 Europe/Brussels:

- `Explore.Domain`, `Explore.Application`, `Explore.Persistence`, and `Explore.API` Release builds passed.
- `Event.Domain.UnitTests` passed: 313 tests.
- `Event.Application.UnitTests` passed: 1857 tests.
- Direct `Event.Persistence.IntegrationTests` binary run passed the new support-access repository tests; the project still has one unrelated failing `EventQuerySpecificationTests.PublicDiscoveryScheduleFilter_ExcludesEventsWhosePublishedSessionsHaveEnded` test.
- Full solution build is blocked by unrelated dirty webhook integration-test work: `WebhookEndpointDto.ProviderModeName` is now required in `Event.API.IntegrationTests/Features/WebhooksControllerTests.cs`.

Latest runtime/BFF/UI/audit verification, 2026-07-04 Europe/Brussels:

- `Explore.Application`, `Explore.Infrastructure`, `Explore.API`, `Explore.Blazor`, and `Explore.Blazor.Client` Release builds passed.
- `Event.Domain.UnitTests` passed: 313 tests.
- `Event.Application.UnitTests` passed: 1860 tests.
- `Event.Architecture.Tests` passed: 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
- `Explore.Blazor.Client.Tests` did not build because unrelated dirty moderation test code references missing `Signals.CorrelationId`.
- Headless Chrome screenshots were captured at 375, 768, and 1280 pixel widths under `dev/active/implement-admin-impersonation/evidence/visual-qa/`. The unauthenticated setup shell rendered coherently and the support banner stayed hidden without an active session.
- Full active-session browser verification remains pending because the local API/authenticated support session was not available during the smoke run.

Latest BFF trust-boundary hardening verification, 2026-07-04 Europe/Brussels:

- `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 9 projects, 0 errors, 979 pre-existing warnings.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/SupportAccessForwardingHandlerTests/*" --minimum-expected-tests 1` passed: 4 tests.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffProxyHeaderSanitizerTests/*" --minimum-expected-tests 1` passed: 2 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
- `git diff --check` passed for the touched support-access BFF and workstream files.

Latest operator console/BFF verification, 2026-07-04 Europe/Brussels:

- `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed after a transient concurrent worktree mismatch in `ExternalApiKeyRepository` cleared: 4 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 8 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 5 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessClientServiceTests/*" --minimum-expected-tests 1` passed: 3 tests.
- `dotnet build Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 9 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/BffSupportAccessEndpointsTests/*" --minimum-expected-tests 1` passed: 3 tests.
- `git diff --check` passed for the touched support-access client, BFF, test, and workstream files.

Latest tenant-facing evidence verification, 2026-07-04 Europe/Brussels:

- Added `TenantSupportAccessEvidenceSection` under tenant administration. It resolves the current tenant through `TenantOnboardingService.GetStatusAsync()`, loads bounded support-access sessions through `ISupportAccessClientService`, and renders audit drill-in only when the session resource contains the `audit-events` HAL link.
- Updated `TenantAdminSettingsLayout` with a read-only `Support Evidence` tab and excluded that tab from the generic `Save Tenant Settings` footer.
- Updated `schemas/islamu-event.md` with support-access lookup, session, audit-event, index, and relationship DBML entries.
- `dotnet build Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 5 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/TenantSupportAccessEvidenceSectionTests/*"` passed: 3 tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/SupportAccessClientServiceTests/*" --minimum-expected-tests 1` passed: 3 tests.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet` passed: 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
- `git diff --check` passed for the touched tenant evidence, docs, and workstream files.
- `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 8 projects, 0 errors, pre-existing warnings.
- Standalone Blazor host started on `http://127.0.0.1:5107`, but app routes including `/admin/tenant/settings` returned 404 while the API/onboarding/auth runtime was unavailable. Authenticated visual QA for the tenant evidence tab remains pending.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --verbosity quiet` passed: 1459 total, 1458 succeeded, 1 skipped pre-existing accessibility test.
- `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` passed: 186 total, 186 succeeded.

Latest full-suite verification, 2026-07-04 Europe/Brussels:

- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` ran to completion and failed outside the focused support-access API tests: 1583 total, 1555 succeeded, 25 failed, 3 skipped. Representative failing areas include `McpAuthorizationTests` returning `504 GatewayTimeout`, AI assistant disabled-flow timeout, storage-object HATEOAS anonymous reads returning `401 Unauthorized`, endpoint authorization matrix authenticated requests returning `401 Unauthorized`, and security JWT audience tests still returning `401 Unauthorized`. Report artifact: `Event.API.IntegrationTests/bin/Release/net10.0/TestResults/Event.API.IntegrationTests-linux-net10.0-report.html`.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` ran to completion and failed outside the support-access repository tests: 215 total, 214 succeeded, 1 failed, 0 skipped. Failing test: `EventRegistrationRepositoryTests.GetRegistrationsByEventWithDetailsPaged_ReturnsOnlyRequestedEventRows`, where `items[0].User?.Pii` was unexpectedly null. Report artifact: `Event.Persistence.IntegrationTests/bin/Release/net10.0/TestResults/Event.Persistence.IntegrationTests-linux-net10.0-report.html`.
- OpenAPI/NSwag clean-state could not be verified honestly because `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, and `docs/API_CONTRACT_INVENTORY.md` were already dirty in the worktree before this verification step. No generated contract files were reverted or normalized in this support-access slice.
- Authenticated E2E/manual Aspire support-access flow remains pending because the standalone Blazor host could not reach a fully running API/onboarding/auth runtime for an authenticated support-access browser scenario.

Latest API contract hardening verification, 2026-07-04 Europe/Brussels:

- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 8 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessApiTests/*"` passed: 8 total, 8 succeeded.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
- `dotnet build --configuration Release --verbosity quiet` passed: 25 projects, 0 errors, pre-existing warnings.

Latest local/Cerbos parity hardening verification, 2026-07-04 Europe/Brussels:

- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 4 projects, 0 errors, pre-existing warnings.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 8 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessSessionServiceTests/*|/*/*/FallbackAuthorizationServiceTests/*SupportAccessSession*" --minimum-expected-tests 1` passed: 7 total, 7 succeeded.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/CerbosPolicyContractTests/*" --minimum-expected-tests 1` passed: 103 total, 103 succeeded.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/CerbosPolicyCompilationTests/*" --minimum-expected-tests 1` passed: 40 total, 40 succeeded.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 240 total, 239 succeeded, 1 skipped pre-existing API metadata test.
- `git diff --check` passed.
- `dotnet build --configuration Release --verbosity quiet` passed: 25 projects, 0 errors, pre-existing warnings.

Latest provider-layer support-access boundary verification, 2026-07-04 Europe/Brussels:

- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 4 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-restore --no-dependencies -clp:ErrorsOnly` passed after focused terminal-session test additions: changed test project compiled, 0 errors, pre-existing warnings.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/RuntimeAuthorizationProviderTests/*" --minimum-expected-tests 1` passed: 21 total, 21 succeeded.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessSessionServiceTests/*" --minimum-expected-tests 1` passed: 10 total, 10 succeeded.
- A transient dependency rebuild blocker from unrelated dirty UserAuthenticationToken edits cleared after the worktree's concurrent alignment; this support-access slice did not modify those files.

Latest support-access observability verification, 2026-07-04 Europe/Brussels:

- `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 2 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 3 projects, 0 errors, pre-existing warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 7 projects, 0 errors, pre-existing warnings.
- `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 3 projects, 0 errors, pre-existing warnings.
- `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore -clp:ErrorsOnly` passed: 8 projects, 0 errors, pre-existing warnings.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/BusinessMetricsSupportAccessTests/*" --minimum-expected-tests 1` passed: 2 total, 2 succeeded.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessApiTests/*" --minimum-expected-tests 1` passed: 8 total, 8 succeeded.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/SupportAccessSessionServiceTests/*" --minimum-expected-tests 1` passed: 10 total, 10 succeeded.
- `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet --treenode-filter "/*/*/RuntimeAuthorizationProviderTests/*" --minimum-expected-tests 1` passed: 21 total, 21 succeeded.

## Required Scenario Checklist

- [x] Non-admin cannot start support access.
- [x] Instance admin cannot start when feature is disabled.
- [x] Missing ticket is rejected when required. Missing reason remains covered by validator-level checks, not the new API class.
- [x] Duration cannot exceed policy.
- [x] Second active session is blocked or idempotently handled according to final contract.
- [x] Read-only session cannot write.
- [x] Write session cannot cross tenants.
- [x] Stopped session cannot authorize requests.
- [x] Expired session cannot authorize requests.
- [x] Revoked session cannot authorize requests.
- [x] Browser-supplied support-access headers are stripped.
- [x] BFF forwards support context only for active owned session.
- [x] HAL links drive all UI action affordances.
- [x] Tenant admin sees only own tenant support-audit evidence.
- [x] Mutating support action records audit evidence.
- [x] Kill switch denies new and existing support-access use.

## Canonical Verification Commands

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

OpenAPI/client generation when API contracts change:

```bash
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1
dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal
```

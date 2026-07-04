<!-- ABOUTME: Tactical checklist for the multi-tenant control-plane implementation workstream. -->
<!-- ABOUTME: Tracks planning, implementation slices, validation, deferred work, and doc-maintenance obligations. -->

# Multi-Tenant Control Plane - Task Checklist

Last Updated: 2026-07-04 Europe/Brussels

## Status Summary

- **Overall status:** In implementation. Phase 1 shared BFF hosting foundation is accepted for the current scope; Phase 2 shared control-plane client library has not started.
- **Completed:** 14/70
- **Current priority:** Start Phase 2 Task 2.1 by creating `Event.ControlPlane.Client` as a host-neutral Razor class library.
- **Next recommended slice:** Phase 2.1 shared control-plane client library scaffold and architecture guard.

## Implementation Maintenance Rules

- [ ] Before starting work, read plan/context/tasks.
- [ ] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [ ] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.
- [ ] Do not report completion unless all three dev docs reflect the actual current state.

## Phase 0: Plan Review And Baseline - In Progress

- [x] Create `multi-tenant-control-plane-plan.md`.
  - **Acceptance:** Plan contains Sections 0-17 required by `.claude/commands/dev-docs.md`.
  - **Validation:** Manual structure check.
  - **Effort:** M
  - **Dependencies:** None
- [x] Create `multi-tenant-control-plane-context.md`.
  - **Acceptance:** Context includes session progress, quick resume, key files, decisions, constraints, validation, risks, and handoff.
  - **Validation:** Manual structure check.
  - **Effort:** S
  - **Dependencies:** None
- [x] Create `multi-tenant-control-plane-tasks.md`.
  - **Acceptance:** Checklist tracks implementation maintenance rules, phases, validation, and deferred work.
  - **Validation:** Manual structure check.
  - **Effort:** S
  - **Dependencies:** None
- [x] Run baseline build before planning edits.
  - **Acceptance:** `dotnet build --configuration Release --verbosity quiet` passes or failure is documented.
  - **Validation:** Build passed with existing warnings.
  - **Effort:** M
  - **Dependencies:** None
- [x] Apply Senior CTO feedback to the dev-docs workstream.
  - **Acceptance:** New planned projects use `Event.ControlPlane.*`; separate control-plane app security requires Keycloak OIDC confidential-client BFF auth; plan/context/tasks agree.
  - **Validation:** Manual workstream review and targeted search for stale `Explore.ControlPlane.*` project creation tasks.
  - **Effort:** M
  - **Dependencies:** User review feedback.
- [x] Apply CTO feedback making shared BFF hosting a required foundation.
  - **Acceptance:** Plan/context/tasks require `Event.Web.BffHosting` before `Event.ControlPlane.Blazor`; future app projects stay out of scope; Instance Console language is refined; current dirty-worktree `Event.Web.BffHosting` files are treated as a candidate to audit, not as completed work.
  - **Validation:** Manual workstream review and targeted search for stale "no third project" BFF guidance plus current dirty-worktree status check.
  - **Effort:** M
  - **Dependencies:** User CTO feedback.
- [x] Re-run architecture/context tests after the CTO update.
  - **Acceptance:** `Event.Architecture.Tests` reaches test execution and passes or produces actionable context-rule failures.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`
  - **Effort:** S
  - **Dependencies:** Senior CTO feedback applied.
- [x] User reviews the plan and approves or corrects scope.
  - **Acceptance:** Plan status changes from Draft to User-reviewed or Approved.
  - **Validation:** Active goal continuation explicitly requested full implementation of the plan.
  - **Effort:** S
  - **Dependencies:** Planning docs.
- [ ] Decide whether to add a dedicated intent to `.claude/contract/intents.yaml`.
  - **Files:** `.claude/contract/intents.yaml` existing; dev docs existing.
  - **Acceptance:** Decision recorded; if intent is added, architecture tests pass.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`
  - **Effort:** S
  - **Dependencies:** User review.
- [x] Re-baseline current dirty worktree before implementation or before accepting in-progress code.
  - **Files:** all future touched files.
  - **Acceptance:** Context file lists current branch status, unrelated dirty changes, the status of the existing `Event.Web.BffHosting`/`Explore.Blazor` extraction candidate, and any implementation-relevant changes since planning.
  - **Validation:** `git status --short`, targeted reads/searches, `dotnet build --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** User approval.

## Phase 1: Shared BFF Hosting Foundation - Completed

- [x] **1.1 Create or complete `Event.Web.BffHosting`.**
  - **Files:** `Event.Web.BffHosting/Event.Web.BffHosting.csproj` new/in-progress; `Abstractions/*` new; `Options/*` new; `Proxy/*` new/accepted for this slice; `Security/*` new/accepted for this slice; `Extensions/*` new/accepted for this slice.
  - **Acceptance:** Project builds; generated `bin/` and `obj/` outputs are not treated as source; accepted Phase 1 owns host profiles, proxy/header options, YARP API proxy registration, API base-address resolution, privileged-header sanitization, token safety, neutral host adapter contracts, reusable OIDC option construction, safe auth diagnostics, and token-refresh cookie events; no UI, generated-client, Application, Domain, Persistence, or provisioning dependencies.
  - **Validation:** `dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity quiet`; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** Phase 0 re-baseline.
- [x] **1.2 Move shared OIDC, cookie, token-refresh, and safe diagnostic primitives.**
  - **Files:** `Explore.Blazor/Extensions/AuthenticationExtensions.cs` existing/modified; `Explore.Blazor/Services/DynamicAuthSchemeManager.cs` existing/modified; `Explore.Blazor/Services/ExploreBffCookieSessionHandler.cs` new; `Explore.Blazor/Services/TokenRefreshCookieEvents.cs` removed; `Explore.Blazor/Services/SafeAuthDiagnosticsPolicy.cs` removed; `Event.Web.BffHosting/Authentication/*` new; `Event.Web.BffHosting/Extensions/ServiceCollectionExtensions.cs` modified.
  - **Acceptance:** Existing `Explore.Blazor` login/logout/token-refresh behavior remains intact through shared registration; no browser-visible secrets/tokens; OIDC errors are safely redacted; token-refresh HTTP backchannel is managed through a named `HttpClientFactory` client.
  - **Validation:** `dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity minimal --no-incremental` passed with 0 warnings; `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity minimal` passed; focused `SafeAuthDiagnosticsPolicyTests` passed 2/2.
  - **Effort:** L
  - **Dependencies:** 1.1
- [x] **1.3 Move shared YARP proxy and privileged-header security primitives.**
  - **Files:** `Explore.Blazor/Extensions/YarpProxyExtensions.cs` existing; `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs` existing; `Event.Web.BffHosting/Proxy/*` new; `Event.Web.BffHosting/Security/*` new.
  - **Acceptance:** Browser-supplied `X-Tenant-Slug`, `X-Setup-Secret`, support/break-glass headers, and tokens cannot become trusted downstream state; trusted tenant hints come only from server context.
  - **Validation:** `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** 1.1
- [x] **1.4 Make `Explore.Blazor` consume `Event.Web.BffHosting`.**
  - **Files:** `Explore.Blazor/Program.cs` existing; `Explore.Blazor/Extensions/*` existing; `Explore.Blazor/appsettings*.json` existing; `Explore.Blazor.IntegrationTests/*` existing.
  - **Acceptance:** Public web host uses `EventBffHostProfile.PublicWeb`; existing public/tenant/admin web behavior remains stable for the accepted Phase 1 slice; `/api/*` YARP route/cluster/transform setup, safe auth diagnostics, reusable OIDC option construction, and token-refresh cookie events delegate to `Event.Web.BffHosting`.
  - **Validation:** `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet`; architecture tests; Blazor integration tests.
  - **Effort:** L
  - **Dependencies:** 1.3
- [x] **1.5 Add accepted-slice BFF hosting architecture and security coverage.**
  - **Files:** `Event.Architecture.Tests/*` existing/new; `Explore.Blazor.IntegrationTests/*` existing/new.
  - **Acceptance:** Tests fail if `Event.Web.BffHosting` references UI/business/generated-client projects, if `Explore.Blazor` stops delegating `/api/*` YARP proxy setup to `Event.Web.BffHosting`, or if accepted proxy/header/auth-diagnostics behavior regresses. Separate-host token/client-secret browser-state assertions remain part of the `Event.ControlPlane.Blazor` matrix.
  - **Validation:** `EventWebBffHostingArchitectureTests` passed 3/3; `BffProxyHeaderSanitizerTests` passed 2/2; `SafeAuthDiagnosticsPolicyTests` passed 2/2. Full broad suites currently have unrelated SupportAccess failures documented in context.
  - **Effort:** M
  - **Dependencies:** 1.1-1.4

## Phase 2: Shared Control-Plane Client Library - Not Started

- [ ] **2.1 Create `Event.ControlPlane.Client` Razor class library.**
  - **Files:** `Event.ControlPlane.Client/Event.ControlPlane.Client.csproj` new; `_Imports.razor` new; route/DI files new; solution file existing.
  - **Acceptance:** Project builds; references are host-neutral; no dependency on `Explore.Blazor.Client`, API, Infrastructure, Persistence, or Domain.
  - **Validation:** Build and architecture tests.
  - **Effort:** M
  - **Dependencies:** Phase 0 re-baseline.
- [ ] **2.2 Add route constants and service registration extension.**
  - **Files:** `Event.ControlPlane.Client/Routing/ControlPlaneRoutes.cs` new; `Event.ControlPlane.Client/Extensions/*` new.
  - **Acceptance:** Embedded and separate hosts can register shared routes/services without duplicating route strings.
  - **Validation:** Build.
  - **Effort:** S
  - **Dependencies:** 2.1
- [ ] **2.3 Define host-neutral control-plane service contracts.**
  - **Files:** `Event.ControlPlane.Client/Contracts/*` new; `Event.ControlPlane.Client/Services/*` new.
  - **Acceptance:** Components depend on contracts, not generated clients; contracts can model HAL links and failure states.
  - **Validation:** Build and initial unit/component test fakes.
  - **Effort:** M
  - **Dependencies:** 2.1
- [ ] **2.4 Resolve shared design-system dependency without duplication.**
  - **Files:** existing `Explore.Blazor.Client/Components/*`; optional neutral shared UI project only if separately approved.
  - **Acceptance:** Control-plane components use existing design conventions without circular references or copy-pasted wrappers.
  - **Validation:** Build, architecture tests, component smoke tests.
  - **Effort:** M
  - **Dependencies:** 2.1
- [ ] **2.5 Add architecture coverage for the new shared UI library.**
  - **Files:** `Event.Architecture.Tests/*` existing/new.
  - **Acceptance:** Tests catch forbidden references and missing ABOUTME headers for new projects/files.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`
  - **Effort:** M
  - **Dependencies:** 2.1

## Phase 3: Control-Plane API And Application Capabilities - Not Started

- [ ] **3.1 Inventory existing admin endpoints and handlers.**
  - **Files:** `Explore.API/Controllers/*` existing; `Explore.Application/Features/InstanceOnboarding/*` existing; tenant/settings handlers existing.
  - **Acceptance:** Context file lists reusable endpoints and missing endpoints for overview, tenants, domains, jobs, storage, security, policies, and backups.
  - **Validation:** Targeted source reads/searches recorded in context.
  - **Effort:** M
  - **Dependencies:** Phase 0 re-baseline.
- [ ] **3.2 Add control-plane overview query and endpoint.**
  - **Files:** `Explore.Application/Features/ControlPlane/Queries/*` new; API controller/route/HAL files existing or new.
  - **Acceptance:** Multi-tenant instance admins receive version, mode, hosts, counts, warnings, provider summaries, and links; single-tenant mode keeps the existing settings abstraction.
  - **Validation:** Application unit tests and API integration tests.
  - **Effort:** L
  - **Dependencies:** 3.1
- [ ] **3.3 Add tenant lifecycle read/actions surface.**
  - **Files:** tenant lifecycle handlers/controllers/repositories existing or new after inventory.
  - **Acceptance:** Tenant create/provision/suspend/archive/purge scheduling is instance-admin-only, HAL-gated, audited, and tenant-safe.
  - **Validation:** Domain/Application tests, persistence tests if schema changes, API integration tests.
  - **Effort:** XL
  - **Dependencies:** 3.1
- [ ] **3.4 Add domains and DNS verification read model.**
  - **Files:** domain/routing settings handlers existing/new; DNS checklist service new if needed.
  - **Acceptance:** API returns public platform, wildcard tenant, admin host, and custom-domain guidance/status.
  - **Validation:** Unit tests and API integration tests for host/domain cases.
  - **Effort:** L
  - **Dependencies:** 3.1
- [ ] **3.5 Add operations/jobs/outbox/email/storage/provider status read models.**
  - **Files:** outbox/email/storage/provider handlers and controllers existing/new.
  - **Acceptance:** Operators can see operational status without tenant data leakage; mutation actions are HAL-gated and audited where present.
  - **Validation:** Application/API tests and infrastructure tests where provider adapters change.
  - **Effort:** XL
  - **Dependencies:** 3.1
- [ ] **3.6 Regenerate/update API contract artifacts if endpoints change.**
  - **Files:** `schemas/openapi.json` existing; `docs/API_CHANGELOG.md` existing.
  - **Acceptance:** OpenAPI and changelog match implemented endpoints.
  - **Validation:** API contract/inventory tests.
  - **Effort:** M
  - **Dependencies:** API endpoint tasks.

## Phase 4: Embedded Instance Console And Multi-Tenant Control-Plane UI - Not Started

- [ ] **4.1 Reference `Event.ControlPlane.Client` from `Explore.Blazor.Client`.**
  - **Files:** `Explore.Blazor.Client/Explore.Blazor.Client.csproj` existing; solution/project references.
  - **Acceptance:** Embedded client builds and can discover control-plane component assembly.
  - **Validation:** Build.
  - **Effort:** S
  - **Dependencies:** 2.1
- [ ] **4.2 Register embedded control-plane routes under `/admin/instance/*`.**
  - **Files:** `Explore.Blazor.Client/Routes.razor` existing; control-plane route files new.
  - **Acceptance:** Multi-tenant instance admins can route to control-plane overview; single-tenant route behavior remains correct.
  - **Validation:** Blazor client route/component tests.
  - **Effort:** M
  - **Dependencies:** 4.1
- [ ] **4.3 Add embedded control-plane navigation and shell behavior.**
  - **Files:** shell/navigation components existing; control-plane shell components new.
  - **Acceptance:** Control-plane nav appears only in multi-tenant mode for instance admins; public/tenant nav remains unchanged.
  - **Validation:** bUnit tests.
  - **Effort:** M
  - **Dependencies:** 4.2
- [ ] **4.4 Build overview, tenants, and domains first slice.**
  - **Files:** `Event.ControlPlane.Client/Pages/Overview/*` new; `Pages/Tenants/*` new; `Pages/Domains/*` new; CSS isolation files new.
  - **Acceptance:** Pages have `PageTitle`, `h1`, accessible controls, responsive layout, and HAL-gated actions.
  - **Validation:** bUnit tests plus browser/visual smoke if available.
  - **Effort:** L
  - **Dependencies:** 3.2, 3.3, 3.4
- [ ] **4.5 Add single-tenant suppression regression tests.**
  - **Files:** Blazor client tests existing/new; API integration tests existing/new.
  - **Acceptance:** Single-tenant admins do not see tenant/platform control-plane navigation/routes; existing settings page remains the single-tenant instance-console abstraction.
  - **Validation:** Blazor client tests and API integration tests.
  - **Effort:** M
  - **Dependencies:** 4.2, 4.3

## Phase 5: Multi-Tenant Onboarding Control-Plane And DNS Guidance - Not Started

- [ ] **5.1 Add multi-tenant administration access choice.**
  - **Files:** onboarding DTOs/settings handlers existing/new; onboarding UI existing/new.
  - **Acceptance:** Multi-tenant onboarding asks how platform administration should be accessed; single-tenant onboarding does not.
  - **Validation:** Application unit tests, API integration tests, component tests.
  - **Effort:** M
  - **Dependencies:** 3.4
- [ ] **5.2 Add DNS checklist and preflight results.**
  - **Files:** DNS checklist read model new; onboarding components existing/new.
  - **Acceptance:** Checklist shows public platform, wildcard tenant, control-plane host, and custom-domain CNAME guidance; skipped DNS is shown as an actionable warning.
  - **Validation:** Unit/component tests.
  - **Effort:** L
  - **Dependencies:** 5.1
- [ ] **5.3 Persist only runtime-relevant onboarding settings.**
  - **Files:** onboarding command handlers existing; settings services existing.
  - **Acceptance:** Persisted values affect host/control-plane behavior; informational choices are not over-modeled.
  - **Validation:** Application and persistence tests if storage changes.
  - **Effort:** M
  - **Dependencies:** 5.1

## Phase 6: Dedicated Control-Plane Hostname Using Existing App Image - Not Started

- [ ] **6.1 Add static admin host configuration and classification.**
  - **Files:** configuration settings classes existing/new; BFF host files existing; `Event.Web.BffHosting/*` new.
  - **Acceptance:** `admin.example.org` style hosts are recognized after trusted forwarded headers; invalid config fails clearly.
  - **Validation:** Unit/integration tests.
  - **Effort:** M
  - **Dependencies:** 1.4, 3.4
- [ ] **6.2 Implement host-based shell separation in the existing app.**
  - **Files:** `Explore.Blazor` existing; `Explore.Blazor.Client` shell/routing existing; control-plane shell new.
  - **Acceptance:** Admin host renders control-plane shell; public and tenant hosts keep their shells; instance-admin auth is enforced.
  - **Validation:** Blazor integration and component tests.
  - **Effort:** L
  - **Dependencies:** 4.2, 6.1
- [ ] **6.3 Add dedicated-host security options.**
  - **Files:** `Event.Web.BffHosting/*` new; BFF auth config existing; rate limiting config existing; security/config docs existing.
  - **Acceptance:** Optional IP allowlist, stricter CSP, cookie naming/domain guidance, and tighter mutation rate limits are implemented or explicitly documented as deferred.
  - **Validation:** Integration tests for protected host behavior.
  - **Effort:** L
  - **Dependencies:** 6.1
- [ ] **6.4 Update reverse-proxy and self-hosting docs for dedicated host.**
  - **Files:** `docs/SELF_HOSTING.md`; `docs/CONFIGURATION.md`; `docs/DEPLOYMENT_MODES.md`.
  - **Acceptance:** Docs show public host, wildcard tenant host, admin host, and forwarded-header requirements.
  - **Validation:** Docs review and build.
  - **Effort:** M
  - **Dependencies:** 6.1, 6.2

## Phase 7: Separate Self-Hostable Control Plane Blazor/BFF App - Not Started

- [ ] **7.1 Scaffold `Event.ControlPlane.Blazor`.**
  - **Files:** `Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj` new; `Program.cs` new; `appsettings.json` new; solution file existing.
  - **Acceptance:** App builds, references `Event.Web.BffHosting` and `Event.ControlPlane.Client`, authenticates through Keycloak OIDC as a confidential BFF client, denies non-instance-admin users, and renders control-plane root only after auth.
  - **Validation:** Build and integration smoke.
  - **Effort:** L
  - **Dependencies:** 1.4, 2.1, 3.2, 4.4
- [ ] **7.2 Define dedicated Keycloak OIDC client and secret boundary.**
  - **Files:** `docker/keycloak/realm-export.json` existing; `docker/keycloak/keycloak-init.sh` existing; `.env.example` existing; `Explore.AppHost/AppHost.cs` existing; `docs/CONFIGURATION.md` existing; `docs/SELF_HOSTING.md` existing; `docs/SECURITY-MODEL.md` existing.
  - **Acceptance:** Dedicated client such as `islamu-event-control-plane` has documented redirect/logout URIs, server-only client secret handling, local Compose/Aspire provisioning, and external-Keycloak guidance; browser-visible config never contains secrets.
  - **Validation:** Keycloak config review, Blazor auth integration tests, docs review.
  - **Effort:** L
  - **Dependencies:** 7.1
- [ ] **7.3 Consume shared BFF hosting with the control-plane profile.**
  - **Files:** `Event.Web.BffHosting/*` new; `Event.ControlPlane.Blazor/*` new; `Explore.Blazor.IntegrationTests/*` existing/new.
  - **Acceptance:** `Event.ControlPlane.Blazor` uses `EventBffHostProfile.ControlPlane`; no local duplicate OIDC/YARP/header setup; tests prove both hosts strip privileged headers and redact OIDC failures.
  - **Validation:** Blazor integration tests proving privileged headers are stripped in both hosts and OIDC failures do not leak client-secret/provider diagnostics.
  - **Effort:** L
  - **Dependencies:** 1.5, 7.1, 7.2
- [ ] **7.4 Add Docker Compose profile and image configuration.**
  - **Files:** `docker-compose.yml` existing; Dockerfile new if needed; `.env.example` existing.
  - **Acceptance:** Self-hosters can run the separate control-plane app as an optional profile/service; Keycloak client secret, authority, metadata, callback, logout, TLS, and reverse-proxy settings are documented.
  - **Validation:** Compose smoke where available; docs review.
  - **Effort:** L
  - **Dependencies:** 7.1, 7.2
- [ ] **7.5 Add Aspire AppHost resource.**
  - **Files:** `Explore.AppHost/AppHost.cs` existing; launch settings existing.
  - **Acceptance:** Aspire can start/describe the control-plane app resource without breaking existing topology and supplies local Keycloak control-plane client settings in full-local mode.
  - **Validation:** Aspire smoke commands per `aspire` skill where available.
  - **Effort:** M
  - **Dependencies:** 7.1, 7.2
- [ ] **7.6 Add separate app integration/E2E tests.**
  - **Files:** existing Blazor integration/E2E fixtures.
  - **Acceptance:** Tests cover Keycloak OIDC challenge redirect, callback failure handling, cookie issuance, non-instance-admin denial, root overview, proxy behavior, header stripping, shell isolation, and no browser-visible tokens/client secrets.
  - **Validation:** Project-specific integration/E2E commands.
  - **Effort:** L
  - **Dependencies:** 7.1, 7.2, 7.3

## Phase 8: Hardening, Docs, And Release Readiness - Not Started

- [ ] **8.1 Review destructive operations for audit, idempotency, and async execution.**
  - **Files:** control-plane mutation handlers/controllers/audit/outbox files existing/new.
  - **Acceptance:** Tenant purge, restore, dead-letter replay, backup/restore, and similar actions have confirmation, audit, and recovery behavior.
  - **Validation:** Unit/integration tests.
  - **Effort:** L
  - **Dependencies:** Phase 3 mutations.
- [ ] **8.2 Add observability and operator-visible failure states.**
  - **Files:** health/logging/metrics files existing/new; troubleshooting docs.
  - **Acceptance:** Control-plane and shared BFF hosting failures have structured logs, status cards, and troubleshooting guidance.
  - **Validation:** Tests/manual smoke.
  - **Effort:** M
  - **Dependencies:** Phase 3 operations.
- [ ] **8.3 Update product and architecture docs.**
  - **Files:** `docs/ADMIN_GUIDE.md`; `docs/DEPLOYMENT_MODES.md`; `docs/MULTI_TENANCY.md`; `docs/SELF_HOSTING.md`; `docs/CONFIGURATION.md`; `docs/BLAZOR.md`; `docs/SECURITY-MODEL.md`; `docs/OPERATIONS.md`; `docs/CODEBASE_STRUCTURE.md`.
  - **Acceptance:** Docs describe `Event.Web.BffHosting`, Instance Console language, implemented deployment shapes, and multi-tenant-only tenant/platform capabilities without listing future app projects.
  - **Validation:** Architecture/context tests and docs review.
  - **Effort:** L
  - **Dependencies:** Phases 1-7.
- [ ] **8.4 Update API changelog and OpenAPI schema.**
  - **Files:** `docs/API_CHANGELOG.md`; `schemas/openapi.json`.
  - **Acceptance:** New/changed endpoints are reflected in API docs/contracts.
  - **Validation:** API contract/inventory tests.
  - **Effort:** M
  - **Dependencies:** Phase 3 endpoints.
- [ ] **8.5 Refresh dev docs and final handoff.**
  - **Files:** `dev/active/multi-tenant-control-plane/*`.
  - **Acceptance:** Plan/context/tasks reflect final state, validation, remaining work, and next steps.
  - **Validation:** Manual final review.
  - **Effort:** S
  - **Dependencies:** All completed implementation slices.

## Verification Checklist

- [ ] LSP diagnostics clean for modified files where applicable.
- [x] `dotnet build --configuration Release --verbosity quiet` passes. On 2026-07-04 it passed with 26 projects, 0 errors, and existing package warnings.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj` passes. Current broad run is blocked by unrelated SupportAccess raw HTTP JSON helper failure; focused `EventWebBffHostingArchitectureTests` passed 3/3 for this slice.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj` passes when Application changes.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` passes when persistence/migrations change.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj` passes when API changes.
- [ ] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj` passes when BFF changes. Current broad run is blocked by unrelated `BffSupportAccessEndpointsTests`; focused BFF auth/header tests passed 4/4 for this slice.
- [x] `Event.Web.BffHosting` architecture/security checks cover forbidden dependencies, `Explore.Blazor` delegation to the shared proxy, privileged-header stripping, safe OIDC failure redaction, shared token-refresh registration, and server-side token-forwarding behavior for the accepted Phase 1 slice.
- [x] Existing dirty-worktree BFF extraction files are accepted only after build, architecture checks, and BFF security tests pass; otherwise they are refined or replaced during Phase 1.
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj` passes when UI/client changes.
- [ ] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` passes when infrastructure/config/provider changes.
- [ ] E2E/manual browser smoke covers embedded, dedicated-host, and separate-app UI where feasible.
- [ ] Docker Compose and Aspire smoke checks run or skipped with documented reason when DevOps changes.
- [ ] Keycloak OIDC control-plane client smoke covers challenge, callback, logout, missing config, non-instance-admin denial, and no browser-visible token/client-secret leakage.
- [ ] Docs updated where behavior/config/operations/API changed.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work

- True reserved-resource management API/worker for operational rescue under public traffic saturation. Deferred until the shared control-plane model is stable.
- Single-tenant to multi-tenant migration wizard/runbook. Deferred and must not become a casual settings toggle.
- Enterprise managed-hosting/fleet-console features beyond this one-instance self-hostable control-plane UI are out of scope and not planned as future app projects in this workstream.
- Mandatory MFA for instance admins. Document as a Keycloak realm/client policy expectation unless current auth provider work already supports enforcing it in-app.
- Full backup/restore orchestration if no current backend exists. Plan should start with readiness/status and add execution only after backend design is approved.

<!-- ABOUTME: Resume context for the Event Instance Console and multi-tenant control-plane implementation plan. -->
<!-- ABOUTME: Captures current progress, key files, decisions, constraints, validation, risks, and handoff notes. -->

# Event Instance Console And Multi-Tenant Control Plane - Context

Last Updated: 2026-07-04 Europe/Brussels

## SESSION PROGRESS (2026-07-04 Europe/Brussels)

### Completed

- Created initial dev-docs planning set for `multi-tenant-control-plane`.
- Read `.claude/commands/dev-docs.md` and matched the required plan/context/tasks structure.
- Loaded the repository contract from `AGENTS.md`, intent registry, quick reference, governance docs, path rules, and relevant skills.
- Investigated current deployment-mode, multi-tenancy, BFF, admin settings, and onboarding implementation.
- Verified during initial planning that no `Explore.ControlPlane.*`, `Event.ControlPlane.Client`, or `Event.ControlPlane.Blazor` project existed; current dirty-worktree re-baseline separately found only an in-progress `Event.Web.BffHosting` candidate.
- Ran baseline build before writing the plan: `dotnet build --configuration Release --verbosity quiet` passed with 25 projects, 0 errors, and existing warnings.
- Post-doc whitespace and required-marker checks passed for the three new files.
- Applied Senior CTO feedback from user review: new planned control-plane projects now use `Event.*` names, and the separate control-plane app now has an explicit Keycloak OIDC confidential-client BFF security contract.
- Re-ran `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj`; it passed with 239 succeeded and 1 intentionally skipped test.
- Applied latest CTO feedback: `Event.Web.BffHosting` is now a required shared BFF security/hosting foundation before `Event.ControlPlane.Blazor`, and future app projects remain out of scope.
- Re-baselined the planning docs against the current dirty worktree: an untracked `Event.Web.BffHosting/` candidate, modified `Explore.Blazor` BFF files, and an untracked BFF architecture-test candidate already exist and must be audited before Phase 1 can be marked complete.
- Reconciled plan/task phase numbering after inserting the shared BFF hosting phase; task checklist now reports 8 completed out of 70 total checklist items.
- Re-ran doc hygiene checks for this update: stale old-direction/future-project scan clean and trailing-whitespace scan clean. `Event.Architecture.Tests` passed earlier in the planning update with 239 succeeded and 1 intentionally skipped test; rerun it after accepting project/context-rule changes.
- Accepted the current `Event.Web.BffHosting` proxy/header foundation after repair: added the missing `Microsoft.Extensions.Hosting` import for `BffDevelopmentHostPolicy`, confirmed `Event.Web.BffHosting` builds, confirmed `Explore.Blazor` builds while consuming it, and confirmed `Event.Architecture.Tests` plus `Explore.Blazor.IntegrationTests` pass.
- `Event.Web.BffHosting` now owns shared YARP `/api/*` route/cluster construction, API base-address resolution, development TLS trust policy, privileged-header sanitization, token safety, and neutral adapter contracts for access-token, tenant, setup-secret, and support-access forwarding.
- `Explore.Blazor` now consumes the shared proxy foundation through `AddEventBffHosting(..., EventBffHostProfile.PublicWeb)` and `AddEventApiProxy(...)`, with host-specific adapter implementations in `Explore.Blazor/Services/EventBffHostingAdapters.cs`.
- Completed Phase 1 Task 1.2 auth extraction: `Event.Web.BffHosting` now owns shared safe auth diagnostics, provider-neutral Keycloak/Google OIDC option construction, token refresh cookie events, the OIDC scheme cookie key, and a named `HttpClientFactory` token-refresh backchannel.
- `Explore.Blazor` now consumes the shared auth primitives through `EventBffTokenRefreshCookieEvents`, `IEventBffOidcOptionsFactory`, and `ISafeAuthDiagnosticsPolicy`; host-specific dynamic provider orchestration remains in `DynamicAuthSchemeManager`, and host-specific admin-claim enrichment/circuit cleanup/setup redirects moved into `ExploreBffCookieSessionHandler`.
- Deleted the old `Explore.Blazor/Services/SafeAuthDiagnosticsPolicy.cs` and `Explore.Blazor/Services/TokenRefreshCookieEvents.cs` implementations after moving the reusable pieces into `Event.Web.BffHosting`.
- Focused validation for Phase 1.2 passed: `Event.Web.BffHosting` Release build passed with 0 warnings; `Explore.Blazor` Release build passed; safe auth diagnostics tests passed 2/2; BFF proxy header sanitizer tests passed 2/2; `EventWebBffHostingArchitectureTests` passed 3/3.

### In Progress

- Phase 1 is complete for the current shared BFF hosting scope.
- Phase 2 has not started. No control-plane client library or separate control-plane Blazor app has been created yet.

### Next

1. Start Phase 2 Task 2.1: create `Event.ControlPlane.Client` as a host-neutral Razor class library.
2. Keep `Event.ControlPlane.Client` free of `Explore.Blazor.Client`, API, Application, Domain, Infrastructure, Persistence, and generated-client dependencies.
3. Add architecture coverage for the shared control-plane UI library before embedding it.
4. Update this context file after the next meaningful implementation slice.

### Blockers

- None for Phase 2 planning.
- The wider worktree remains heavily dirty with unrelated changes; do not revert unrelated files.
- Broad verification is currently blocked by unrelated dirty-worktree SupportAccess changes: full `Explore.Blazor.IntegrationTests` ran 187 tests with 186 passing and 1 failing in `BffSupportAccessEndpointsTests.StartWhenApiSucceedsStoresSessionAndPreservesFlattenedHalBody`; full `Event.Architecture.Tests` fails `Rule_1_17_RawHttpJsonHelpers_MustStayIn_ApprovedBoundaries` for `Explore.Blazor.Client/Services/SupportAccessClientService.cs`.
- A concurrent static-web-assets build lock appeared during one targeted test attempt. Use `--no-build` for filtered TUnit runs after building once.

## Quick Resume

1. Read `dev/active/multi-tenant-control-plane/multi-tenant-control-plane-plan.md`.
2. Read `dev/active/multi-tenant-control-plane/multi-tenant-control-plane-tasks.md`.
3. Continue with Phase 2 Task 2.1 unless the user gives a narrower instruction.
4. Keep all three dev docs updated after each meaningful implementation slice.
5. Do not expose control-plane concepts in single-tenant mode.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Infrastructure/DeploymentSettings.cs` | Existing | Infrastructure | Deployment mode settings. | Verified source for `Mode`, `DefaultTenantId`, `HidePlatformAdminInSingleTenant`, and helper flags. |
| `Explore.Infrastructure/Services/DeploymentModeProvider.cs` | Existing | Infrastructure | Runtime/configured deployment-mode resolution. | Persists post-onboarding mode authority and falls back safely pre-onboarding. |
| `Explore.Application/Contracts/Services/IDeploymentModeProvider.cs` | Existing | Application | Deployment-mode abstraction. | Use instead of reading config directly in application flow. |
| `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs` | Existing | API | API-authoritative tenant resolution. | Multi-tenant unresolved requests fail closed with 404. |
| `Explore.API/Middleware/ApiTenantPostAuthenticationMiddleware.cs` | Existing | API | API-key tenant reconciliation after auth. | Important for instance-admin API key behavior. |
| `Explore.API/Filters/BlockInSingleTenantAttribute.cs` | Existing | API | Single/multi-tenant endpoint visibility filters. | Use for multi-tenant-only control-plane endpoints where appropriate. |
| `Explore.Blazor/Extensions/YarpProxyExtensions.cs` | Existing/modified | Blazor BFF | Host adapter registration for shared API proxy. | Now delegates route/cluster/transform setup to `Event.Web.BffHosting.Proxy.AddEventApiProxy`. |
| `Explore.Blazor/Extensions/AuthenticationExtensions.cs` | Existing/modified | Blazor BFF | Host auth registration. | Uses `EventBffTokenRefreshCookieEvents`, shared safe auth diagnostics, and the `ExploreBffCookieSessionHandler` host adapter. |
| `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs` | Existing | Blazor BFF | Trusted tenant header forwarding. | Do not let browser supply tenant authority. |
| `Explore.Blazor.Client/Routes.razor` | Existing | Blazor Client | Current route map. | Contains `/admin/instance/settings` and onboarding route guards. |
| `Explore.Blazor.Client/Routing/Guards/MultiTenantOnboardingRouteGuard.cs` | Existing | Blazor Client | Multi-tenant onboarding guard. | Existing mode-aware UI behavior. |
| `Explore.Blazor.Client/Routing/Guards/TenantAdminRouteGuard.cs` | Existing | Blazor Client | Tenant/admin route behavior. | Single-tenant instance admin can use tenant admin route where intended. |
| `Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor` | Existing | Blazor Client | Current instance settings page. | Keep as single-tenant administration abstraction. |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/*` | Existing | Blazor Client | Current instance settings sections. | Potential source material for control-plane pages, but do not duplicate. |
| `Event.Web.BffHosting/` | New / accepted Phase 1 foundation | Blazor/BFF | Shared ASP.NET Core browser-BFF hosting library. | Builds and is consumed by `Explore.Blazor` for YARP proxying, privileged-header stripping, token/tenant/setup/support forwarding adapters, API base resolution, token safety, dev TLS trust policy, safe auth diagnostics, provider-neutral OIDC option construction, and token refresh cookie events. |
| `Event.Web.BffHosting/Authentication/EventBffOidcOptionsFactory.cs` | New | Blazor/BFF | Shared OIDC option construction. | Centralizes PKCE, token persistence, safe OIDC events, scopes, metadata, callback paths, and IPv4 backchannel behavior without owning dynamic provider orchestration. |
| `Event.Web.BffHosting/Authentication/EventBffTokenRefreshCookieEvents.cs` | New | Blazor/BFF | Shared cookie token-refresh event. | Refreshes server-side access tokens using stored refresh tokens and delegates host-specific enrichment/cleanup/redirects to `IEventBffCookieSessionHandler`. |
| `Event.Web.BffHosting/Authentication/SafeAuthDiagnosticsPolicy.cs` | New | Blazor/BFF | Shared safe auth diagnostics. | Builds browser-safe login redirects with bounded error codes and correlation ids, without exposing provider/client-secret details. |
| `Explore.Blazor/Services/EventBffHostingAdapters.cs` | New | Blazor BFF | Host-specific adapter bridge into `Event.Web.BffHosting`. | Preserves circuit-aware token fallback, tenant route context, setup-secret resolver, and support-access session forwarding outside the shared library. |
| `Explore.Blazor/Services/ExploreBffCookieSessionHandler.cs` | New | Blazor BFF | Host-specific cookie session adapter. | Preserves admin claim enrichment, circuit token updates, auth cookie/session cleanup, and setup-aware expired-session redirects outside the shared library. |
| `Event.Architecture.Tests/EventWebBffHostingArchitectureTests.cs` | New | Tests | Boundary test for shared BFF hosting library. | Guards no project references, no forbidden layer tokens, and `Explore.Blazor` proxy delegation to shared BFF hosting. |
| `Event.ControlPlane.Client/` | New | Blazor Client Library | Shared control-plane Razor class library. | New projects use `Event.*`; must be consumed by embedded and separate app. |
| `Event.ControlPlane.Blazor/` | New | Blazor BFF | Self-hostable control-plane app. | Must authenticate through Keycloak OIDC as a confidential BFF client. |
| `docker/keycloak/realm-export.json` | Existing | DevOps/Auth | Local Keycloak realm export. | Add dedicated control-plane OIDC client only if implementation provisions local Keycloak automatically. |
| `docker/keycloak/keycloak-init.sh` | Existing | DevOps/Auth | Compose Keycloak client-secret synchronization. | Must support the dedicated control-plane client if realm/export is updated. |
| `.env.example` | Existing | DevOps/Auth | Self-hosting environment template. | Must document control-plane Keycloak client id/secret env vars if implemented. |
| `docker-compose.yml` | Existing | DevOps | Self-hosting topology. | Add separate control-plane profile/image when implemented. |
| `Explore.AppHost/AppHost.cs` | Existing | DevOps | Aspire orchestration. | Add control-plane resource when implemented. |
| `docs/DEPLOYMENT_MODES.md` | Existing | Docs | Deployment-mode authority. | Must remain clear that mode is not a casual runtime toggle. |
| `docs/MULTI_TENANCY.md` | Existing | Docs | Tenant isolation and resolver model. | Control-plane host must not weaken fail-closed resolution. |
| `docs/BLAZOR.md` | Existing | Docs | Blazor/BFF architecture. | Update for shared library and separate app. |
| `docs/SELF_HOSTING.md` | Existing | Docs | Docker/self-hosting guidance. | Update embedded/dedicated/separate deployment shapes. |

## Key Decisions

| Decision | Status | Reason |
|---|---|---|
| Event Instance Console exists in both modes, but tenant/platform control-plane features are multi-tenant-only. | Planned | Single-tenant mode keeps the existing administration settings page as its current instance-console abstraction. |
| Create `Event.Web.BffHosting` as a required shared BFF hosting library before the separate app. | Implemented for Phase 1 scope | Proxy/header/token-adapter foundation, reusable OIDC option construction, shared safe auth diagnostics, and token-refresh cookie events are accepted. Later work may add control-plane-specific profile defaults and health checks. |
| Create `Event.ControlPlane.Client` as a shared Razor class library. | Planned | Both embedded and separate app must share the same control-plane implementation; `Explore.ControlPlane.*` must not be created for new projects. |
| Create `Event.ControlPlane.Blazor` as a separate self-hostable BFF app. | Planned | Separate app must preserve server-side token handling and BFF security. |
| Authenticate `Event.ControlPlane.Blazor` through Keycloak OIDC. | Planned | Operators should sign in through the established Keycloak OIDC confidential-client BFF model, not API keys, setup secrets, or browser-stored tokens. |
| Add a dedicated Keycloak client such as `islamu-event-control-plane`. | Planned | Separate app needs clear redirect/logout URIs and server-only secret handling for self-hosters. |
| Keep one control-plane capability, not two products. | Planned | Prevent duplicated auth, clients, layouts, components, and security decisions. |
| Do not add a single-tenant to multi-tenant toggle. | Planned | Existing docs require migration/runbook semantics for mode changes. |
| Use HAL links for resource action affordances. | Required | Project invariant. |
| Prefer async/audited jobs for destructive operations. | Planned | Tenant purge, restore, dead-letter replay, and similar operations need audit/retry safety. |
| Document separate UI host limitations. | Planned | Separate UI does not solve shared API/database saturation by itself. |

## Constraints And Rules To Remember

- Repositories return entities, never DTOs.
- Validators are manually instantiated where the project pattern requires it.
- Use `int` for lookups, `Guid` UUIDv7 for aggregates, and `long` for cursors.
- GET/write attributes must follow project rules, with control-plane endpoints enforcing instance-admin authority.
- HAL `_links` are the source of truth for edit/delete/suspend/purge/retry and similar UI actions.
- `Event.Web.BffHosting` is required and must stay limited to authentication, cookies, proxying, header security, diagnostics, health, and options validation.
- `Event.Web.BffHosting` must not contain UI pages/components, generated clients, Application handlers, Domain entities, Persistence repositories, Keycloak provisioning scripts, Docker Compose definitions, or tenant lifecycle business logic.
- BFF tokens stay server-side; browser code never receives tokens.
- `Event.ControlPlane.Blazor` must use Keycloak OIDC Authorization Code flow plus PKCE with a confidential client and HttpOnly cookies.
- Keycloak client secrets remain server-side through env/config/secret provider paths and must never appear in browser config, logs, or diagnostics.
- Non-instance-admin authenticated users must not enter the separate control-plane shell.
- BFF strips browser-supplied privileged headers and forwards trusted tenant hints only.
- API tenant resolution is authoritative and fail-closed.
- Single-tenant mode must hide tenant/platform control-plane concepts and keep the current administration settings abstraction.
- All new files require two `ABOUTME:` lines.
- Do not revert unrelated user changes in the dirty worktree.
- Do not run solution-level `dotnet test`; run project test commands.
- New planned BFF/control-plane project names use `Event.*`. Existing `Explore.*` projects remain unchanged unless a separate repository-wide rename is approved.

## Validation Baseline

Baseline already run during planning:

```bash
dotnet build --configuration Release --verbosity quiet
```

Result: passed with 25 projects, 0 errors, and existing warnings.

Post-doc verification:

```bash
git diff --check -- dev/active/multi-tenant-control-plane/multi-tenant-control-plane-plan.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-context.md dev/active/multi-tenant-control-plane/multi-tenant-control-plane-tasks.md
rg -n "^(<!-- ABOUTME|Last Updated:|## 0\\.|## 17\\.|## SESSION PROGRESS|## Status Summary)" dev/active/multi-tenant-control-plane
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj
```

Results:

- `git diff --check` passed.
- Required `ABOUTME`, `Last Updated`, and key dev-docs sections are present.
- `Event.Architecture.Tests` passed with 240 total, 239 succeeded, 0 failed, and 1 intentionally skipped API contract metadata test.
- Latest checklist count is 8 completed out of 70 total checklist items.
- Latest stale-reference scan found no old "do not create BFF project" direction and no future app project list in the workstream docs.
- Latest trailing-whitespace scan was clean for the three workstream files.
- This wording-only re-baseline reran `git diff --check` and targeted stale/future-scope searches; architecture tests were not rerun after this final documentation adjustment.
- Use the project-level architecture command above; this repo's TUnit runner rejected the earlier `--filter` argument form.

Minimum validation after implementation depends on touched layers:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj
```

Add these when relevant:

```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj
```

Phase 1 proxy/header slice validation on 2026-07-04:

```bash
dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity quiet
dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

Results:

- `Event.Web.BffHosting` build passed: 1 project, 0 errors, 0 warnings.
- `Explore.Blazor` build passed: 9 projects, 0 errors, existing package/analyzer warnings.
- `Event.Architecture.Tests` passed: 243 total, 242 succeeded, 1 intentionally skipped API metadata test.
- `Explore.Blazor.IntegrationTests` passed: 186 total, 186 succeeded, 0 skipped.
- Full solution build passed: 26 projects, 0 errors, existing package warnings.

Phase 1 OIDC/cookie/token-refresh slice validation on 2026-07-04:

```bash
dotnet build Event.Web.BffHosting/Event.Web.BffHosting.csproj --configuration Release --verbosity minimal --no-incremental
dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity minimal
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/SafeAuthDiagnosticsPolicyTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/BffProxyHeaderSanitizerTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/EventWebBffHostingArchitectureTests/*" --minimum-expected-tests 1 --log-level Error --no-progress
```

Results:

- `Event.Web.BffHosting` build passed: 1 project, 0 errors, 0 warnings.
- `Explore.Blazor` build passed: 9 projects, 0 errors, existing package/analyzer warnings.
- `SafeAuthDiagnosticsPolicyTests` passed: 2 total, 2 succeeded.
- `BffProxyHeaderSanitizerTests` passed: 2 total, 2 succeeded.
- `EventWebBffHostingArchitectureTests` passed: 3 total, 3 succeeded.
- Full `Explore.Blazor.IntegrationTests` currently fails only unrelated SupportAccess test `StartWhenApiSucceedsStoresSessionAndPreservesFlattenedHalBody`; the same run reported 186 succeeded out of 187.
- Full `Event.Architecture.Tests` currently fails only unrelated SupportAccess raw HTTP JSON helper rule for `Explore.Blazor.Client/Services/SupportAccessClientService.cs`.

## Current Known Risks / Unknowns

- Later `Event.Web.BffHosting` expansion must stay profile/config/health focused and avoid absorbing UI, business, generated-client, or provisioning responsibilities.
- Exact project-system shape for `Event.ControlPlane.Client` must be verified before creating components.
- Exact Keycloak configuration shape for `Event.ControlPlane.Blazor` must be decided: reuse `Keycloak:*` with a dedicated client id/secret or introduce a documented control-plane-specific config section.
- Local/external Keycloak provisioning must be planned so self-hosters can create the control-plane confidential client, redirect URIs, logout URIs, and client secret safely.
- Existing tenant lifecycle/domain/job code may have changed in the dirty worktree; re-read before adding endpoints.
- Dedicated admin host must not conflict with tenant host/domain resolution.
- Separate app self-hosting needs truthful docs: it is a separate UI host, not a true reserved-resource management plane.
- Control-plane operational summaries must not leak tenant business data to instance admins.

## Handoff Notes

- **Current state:** Phase 1 shared BFF hosting foundation accepted. `Event.Web.BffHosting` builds cleanly and `Explore.Blazor` consumes it for shared YARP proxying, privileged-header stripping, safe auth diagnostics, reusable OIDC option construction, and token-refresh cookie events. The overall control-plane implementation is still early; `Event.ControlPlane.Client` and `Event.ControlPlane.Blazor` do not exist yet.
- **Next action:** Start Phase 2 Task 2.1 by creating `Event.ControlPlane.Client` as a host-neutral Razor class library.
- **Blockers:** No blockers for Phase 2. Broad full-suite verification has unrelated SupportAccess failures listed in the validation section.
- **Modified files:** `dev/active/multi-tenant-control-plane/multi-tenant-control-plane-plan.md`, `dev/active/multi-tenant-control-plane/multi-tenant-control-plane-context.md`, `dev/active/multi-tenant-control-plane/multi-tenant-control-plane-tasks.md`.
- **Validation:** `Event.Web.BffHosting` build passed with 0 warnings; `Explore.Blazor` build passed; focused BFF auth/header/architecture tests passed. Full `Explore.Blazor.IntegrationTests` and full `Event.Architecture.Tests` are currently blocked by unrelated SupportAccess failures.
- **Documentation impact:** Dev-docs updated for the accepted Phase 1 BFF hosting foundation. Product docs are planned for future implementation.
- **Risks:** Phase 2 must not create a dependency cycle with `Explore.Blazor.Client` and must not put generated clients, API contracts, tokens, or local authorization decisions into the shared control-plane UI library.
- **Notes for next contributor/agent:** Do not start from memory. Continue at Phase 2 Task 2.1 and re-read the plan/tasks plus the current `Explore.Blazor.Client` component/service conventions before creating `Event.ControlPlane.Client`.

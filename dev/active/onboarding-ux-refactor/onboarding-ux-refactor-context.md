<!-- ABOUTME: Operational memory for the implemented onboarding UX refactor and its remaining verification work. -->
<!-- ABOUTME: Records decisions, evidence, risks, validation blockers, and the next safe action for a cold agent. -->

# Onboarding UX Refactor — Context

> **Current state (2026-07-12):** Production Keycloak manageability and explicit authorization-provider deployment reconciliation are implemented with focused green regression coverage. Tasks 6.3, 8.1, 8.2, and 8.3 remain open for fresh runtime/visual, required-suite, authenticated, and final-review evidence.

## Production Detection Update

- `AuthProviderConfigurationService` now resolves one effective Keycloak tuple for public setup reads, administrator reads/status, control-plane summaries, BFF refresh, realm operations, and configured-state checks.
- In application-managed mode, a complete enabled stored authority/client-ID tuple wins. Explicit deployment-managed metadata makes a complete deployment tuple authoritative; otherwise a complete deployment tuple is a bootstrap fallback. Partial tuples fail closed, and the API audience is never substituted for the browser client ID.
- Public and administrator DTOs always return an empty client secret. Deployment-managed writes ignore submitted replacements, trusted server reads apply ownership precedence, and secret rotation derives ownership from authoritative server configuration.
- Current focused evidence is 6 service tests, 2 confidential-client validator tests, 5 rotation-handler tests, 1 compatibility-mapping test, and 1 real TestServer HTTP projection test, all passing. Save/update handler regression cases for server-derived configured state and forged request ownership also pass.
- This resolves the producer/service boundary. A deployed Keycloak browser login and create/repair/reconcile realm journey remains open because the current browser visual artifact used a stubbed detected-provider response.

Last Updated: 2026-07-12 Europe/Brussels

## SESSION PROGRESS (2026-07-12 Europe/Brussels)

### ✅ COMPLETED

- Classified the work under `external-infrastructure-bootstrap` and captured its complete contract in the plan.
- Read relevant repository contracts, canonical deployment/multi-tenancy/accessibility docs, rules, and skills.
- Traced setup, BFF/auth, instance onboarding, tenant onboarding, deployment-mode, authorization-provider, Cerbos, RFC 7807, idempotency, audit, deployment, and test flows.
- Compared the work against `dev/pause/tenant-onboarding-enterprise/` and determined this needs a separate active workstream.
- Created a decision-complete planning baseline with target journeys, authority matrix, phased tasks, tests, recovery, operations, and risks.
- Consulted Plan and Oracle reviewers; incorporated the required authority-matrix, recovery, deferred-scope, localization, and no-snapshot corrections.
- Reviewed all three planning files for agreement and corrected every test-project command to use the repository's `tests/` paths.
- Ran the current full architecture suite: 263 tests passed, one was skipped, and four unrelated managed-control-plane architecture failures remain in the dirty workspace.
- Implemented the semantic task-list UX, instance and tenant server-authoritative flows, HAL status contracts/policies/assemblers, generated client mapping, BFF status-secret forwarding, operator documentation, API inventory, and focused regression coverage.
- Retained configured Keycloak as a complete task with a HAL-gated management route before and after launch.
- Hardened setup-secret forwarding against browser-header spoofing, query-string confusion, and similar-route prefix confusion; the final focused BFF suite passes 14/14.
- Hardened the provider editor so setup reads only the public redacted contract, clears any returned secret fields before render, never prefills an existing client secret into bootstrap controls, and clears all one-time bootstrap values in `finally`.
- Completed the earlier Keycloak-focused browser QA across base/detected, desktop/mobile, LTR/RTL, light/dark, long-text, disclosure-keyboard, focus, and provider-action states; that independent visual gate is `PASS`. Fresh authorization-page QA remains open.
- Implemented explicit blank/Local/Cerbos authorization intent, runtime precedence, deployment-owned writes, instance PDP verification, instance-only policy publication, bounded startup retries, singleton single-flight state, safe failure remediation, authoritative route skipping, post-launch retry refresh, and the one-column Local-first page.
- Updated canonical configuration, secrets, self-hosting, troubleshooting, Blazor, and API change documentation for the authorization flow.

### 🟡 IN PROGRESS

- Core UI, production Keycloak detection, and authorization-provider automation are implemented. Tasks 6.3, 8.1, and 8.2 remain open because fresh authorization visual/runtime evidence and post-change required suites are incomplete, API/Architecture retain failures, and authenticated/assisted-screen-reader journeys are unverified.
- The current Release build passes with zero errors; full Application, serialized Client, and BFF Integration projects pass.
- Context7 `/mudblazor/mudblazor` guidance informed semantic markup, keyboard-safe native links, live regions, and bUnit coverage.
- `add-hal-link`, `openapi-contract-change`, and `blazor-component-affordance` were implemented; the aggregate snapshot remains deferred after measurement.

### Implementation Evidence

- `StartupGate.razor` now sends completed MultiTenant platform administrators to the canonical control-plane overview while preserving SingleTenant and non-admin event handoffs, incomplete setup routing, and fail-closed error states.
- `OnboardingTaskList.razor` is a display-only semantic ordered list. Parent pages own state derivation, localized strings, authority labels, and route availability; the component owns only accessible rendering.
- `OnboardingTaskList.razor.css` uses the project token system, BEM isolation, logical properties, visible focus, one-column container behavior, and 24px minimum action targets.
- Focused task-list and startup tests cover semantic metadata, native navigation, status live regions, both deployment-mode handoffs, zero-tenant-independent platform launch, incomplete routing, and null/exception recovery.
- `InstanceOnboarding.razor` composes existing status/profile/provider/preflight reads, distinguishes unknown provider state from unconfigured state, gates all actions by HAL, and confirms completion with a fresh server status. Configured authentication keeps **Manage authentication**, using the setup page before launch and the admin provider section after launch.
- `TenantOnboarding.razor` uses trusted server tenant context, HAL-gated completion/management, post-command status confirmation, and tenant-drift rejection.
- Instance and tenant status controllers now return shared HAL resources assembled through permission-aware policies. The NSwag/OpenAPI artifacts and `HalResourceExtensions` preserve `_links` for the existing plain service DTO boundary.
- `SetupSecretForwardingHandler` removes `X-Setup-Secret` from every outbound request and adds only a trusted resolver value for exact/slash-delimited instance-onboarding paths.
- `AuthProviderConfiguration.razor` uses native disclosures, mode-specific Keycloak validation, visible focus, responsive logical CSS, and both patch-existing/create-if-missing bootstrap actions. Detected state keeps **Configure Authentication Providers** instead of forcing the operator directly to login.
- `InstanceOnboardingService.GetAuthProviderConfigurationAsync()` uses the public redacted API read. The page defensively scrubs Keycloak and Google secret values before render and preserves an already-configured Google provider without reading its secret back.
- `CONFIGURATION.md`, `SECRETS.md`, `SELF_HOSTING.md`, `TROUBLESHOOTING.md`, `DEPLOYMENT_MODES.md`, `BLAZOR.md`, `API_CHANGELOG.md`, and the generated API inventory describe the implemented journeys and recovery behavior.

### Reuse Inventory

- **Localization:** existing `ITranslationService.T(key, fallback)`; no parallel localization abstraction.
- **Accessibility:** existing `IAccessibilityAnnouncerService`; MainLayout continues to own global focus and live-region infrastructure.
- **Navigation:** existing focused provider routes plus canonical `ControlPlaneRoutes` constants.
- **Styling:** existing three-tier `--isl-*` tokens and component-scoped BEM CSS; no new global override or wrapper.
- **Trust boundary:** existing BFF setup-secret session and resolver are reused; the forwarding handler now also serves status reads and uses strict path matching.
- **Security coverage:** BFF/API tests cover global browser-header stripping, trusted replacement, near-route/query rejection, secret redaction, invalid/inactive/rate-limited setup, safe ProblemDetails, permission-bound HAL relations, and tenant isolation/drift.

### ⏭️ NEXT

1. Restart the real FullLocal Aspire stack from current source and prove explicit Cerbos background readiness plus the authorization-page skip/remediation routes.
2. Run fresh desktop/mobile authorization visual QA without using browser tooling for file exploration.
3. Rerun the required post-change build and project suites, preserving unrelated workspace changes and attributing API/Architecture failures.
4. Execute authenticated SingleTenant/MultiTenant, deployment-supplied Keycloak create/repair/reconcile, and assisted screen-reader journeys.

### ⚠️ BLOCKERS

- **Workspace isolation risk:** The current working tree contains authorization/onboarding work plus unrelated managed-tenant provisioning changes that must not be reverted or folded into this slice accidentally.
- **Runtime:** The prior `.slnx` and migration blockers are resolved. The current local stack still reports S3 storage unhealthy; the retired browser fixture no longer participates in verification.
- **Full suites:** Post-change broad suites are pending. The latest baseline has eight API failures and four Architecture failures requiring attribution; Application, Client, BFF Integration, build, and Compose configuration were green.
- **Accessibility:** No supported assisted screen reader is installed in this environment; browser semantics/keyboard evidence cannot substitute for that gate.

## Quick Resume

1. Read `onboarding-ux-refactor-plan.md`.
2. Read `onboarding-ux-refactor-tasks.md`.
3. Review current `git status` without modifying unrelated managed-control-plane work or introducing `.codex` changes.
4. Start with open Tasks 6.3, 8.1, and 8.2; core implementation and canonical docs are complete.
5. Fresh authorization browser QA is still required. Do not use browser tooling for file exploration.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `src/Explore.Blazor.Client/Routes.razor` | Existing | Blazor Client | Central route table | Preserve focused routes unless approved otherwise. |
| `src/Explore.Blazor.Client/Pages/Setup.razor` | Existing | Blazor Client | Setup-secret gateway and auth handoff | Separate pre-auth flow. |
| `src/Explore.Blazor.Client/Pages/Onboarding/StartupGate.razor` | Existing | Blazor Client | Server-derived startup routing | Must distinguish platform completion from tenant onboarding. |
| `src/Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor` | Existing, refactored | Blazor Client | Instance setup overview and launch | Composes authoritative reads and HAL actions. |
| `src/Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor` | Existing | Blazor Client | Focused authentication-provider task | Reuse; do not expose provider secrets. |
| `src/Explore.Blazor.Client/Pages/Onboarding/AuthorizationProviderConfiguration.razor` | Existing, refactored | Blazor Client | Single-column Local default, advanced Cerbos configuration, and failed deployment remediation | Deployment pending/ready bypasses the choice page; failure is locked and retry-only. |
| `src/Explore.Blazor.Client/Pages/Onboarding/TenantOnboarding.razor` | Existing, refactored | Blazor Client | Tenant-scoped onboarding | Optional after MultiTenant platform launch; tenant drift fails closed. |
| `src/Explore.Blazor.Client/Pages/Onboarding/Components/OnboardingTaskList.razor` | New, implemented | Blazor Client | Minimal accessible display component | No authority/business logic. |
| `src/Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor` | Existing | Blazor Client | Post-launch instance editor | Reuse/link from tasks. |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor` | Existing | Blazor Client | Post-launch tenant editor | Reuse/link from tenant tasks. |
| `src/Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs` | Existing | BFF | Setup-secret session endpoints | Trusted server boundary. |
| `src/Explore.Blazor/Services/SetupSecretResolver.cs` | Existing | BFF | Resolves trusted secret source | Browser never owns value. |
| `src/Explore.Blazor/Services/SetupSecretForwardingHandler.cs` | Existing | BFF | Strips/replaces privileged header | Security regression target. |
| `src/Explore.API/Controllers/InstanceOnboardingController.cs` | Existing | API | Setup/status/provider/completion API | Thin controller contract. |
| `src/Explore.API/Controllers/SystemController.cs` | Existing | API | Public/system onboarding status and preflight | Compose existing read state. |
| `src/Explore.API/Controllers/TenantOnboardingController.cs` | Existing | API | Tenant status/settings/progress/completion | Needs explicit integration coverage. |
| `src/Explore.API/Controllers/InstanceSettingsController.cs` | Existing | API | Post-launch authz/settings/sync/package endpoints | Deployment mode remains immutable here. |
| `src/Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetOnboardingPreflightQueryHandler.cs` | Existing | Application | Required checks and warnings | Server source for launch gating. |
| `src/Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs` | Existing | Application | Authoritative bootstrap commit | Uses configured mode, creates grants/state, locks setup. |
| `src/Explore.Application/Services/AuthProviderConfigurationService.cs` | Existing, changed | Application | Shared stored/deployment Keycloak tuple resolution, configured-state producer, sanitized reads, and secret ownership precedence | Focused service and TestServer HTTP coverage pass; real deployment/browser journey remains open. |
| `src/Explore.Infrastructure/Services/DeploymentModeProvider.cs` | Existing | Infrastructure | Configured onboarding/persisted runtime mode | Not a UI preference. |
| `src/Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs` | Existing, changed | Infrastructure | Shared deployment intent, provider ownership/config, endpoint verification, and policy reconciliation | Explicit Local avoids Cerbos; explicit Cerbos becomes ready only after verify and publish; credentials stay redacted/write-only. |
| `src/Explore.Infrastructure/Services/AuthorizationProviderDeploymentOptions.cs` | New | Infrastructure | Validated blank/local/cerbos deployment selector | Invalid explicit values fail options validation at startup. |
| `src/Explore.Infrastructure/Services/AuthorizationProviderBootstrapState.cs` | New | Infrastructure | Process-local pending/ready/failed projection plus single-flight coordination | Contains safe status only, never secrets; concurrent boot/admin callers share one attempt. |
| `src/Explore.API/BackgroundServices/CerbosPolicyBootSyncRunner.cs` | Existing, changed | API | Bounded background provider reconciliation | Keeps status pending between transient failures and delegates each attempt to the shared service. |
| `src/Explore.Infrastructure/Services/CerbosPolicyPackageService.cs` | Existing, changed | Infrastructure | Cerbos package manifest/sync/archive | Automatic instance bootstrap bypasses ambient tenant BYO targets; interactive tenant-aware sync remains available. |
| `dev/pause/tenant-onboarding-enterprise/` | Existing, paused | Dev docs | Broader lifecycle/invitation/self-service work | Only its route/wizard assumptions are superseded. |

## Key Decisions

1. `/setup` remains pre-auth and setup-secret protected; visual consistency does not merge authority.
2. Post-auth onboarding becomes a single-column conditional task list driven by server state.
3. `DEPLOYMENT_MODE` and dedicated admin host are deployment/operator configuration and read-only context in UI.
4. SingleTenant launches to events/settings; MultiTenant launches the platform/control plane first.
5. First-tenant onboarding is optional, separate, and tenant-scoped.
6. Compose current status/provider/preflight/progress endpoints initially.
7. Add a snapshot only after a reproduced race, unacceptable request amplification, duplicated derivation across more than two consumers, or required server atomicity.
8. Reuse existing settings editors and provider pages.
9. Cerbos inventory and arbitrary decision-test APIs remain deferred.
10. Recovery, retry, refresh, and partial completion are first-class states.
11. Authentication-provider completion and ongoing manageability are separate: a configured task retains a HAL-authorized **Manage authentication** link to the focused setup page before launch and the admin provider editor after launch for Keycloak realm diagnosis, repair, or reconciliation; missing/error authoritative state still fails closed.
12. Deployment/configured Keycloak credentials must never remove authentication-provider management. Detection may offer a fast continue action, but the operator retains the full setup editor before launch and the HAL-authorized admin provider editor after launch.
13. Authorization provider intent is explicit: `local` and `cerbos` are deployment-owned; Local and pending/ready Cerbos bypass the choice page, while final failed Cerbos opens locked remediation. Blank/unset defaults the manual page to Local with Cerbos progressively disclosed. Endpoint/credential presence alone never selects Cerbos.

### Current Slice Evidence — 2026-07-12 Europe/Brussels

- Implementation and test coverage were added for the persistent **Manage authentication** affordance: configured state keeps the focused setup action before launch and switches to the admin provider editor after launch when authoritative HAL state permits it, while missing/error state remains fail-closed.
- Exact-diff review found and implementation resolved two correctness issues: instance completion now re-fetches server status instead of setting a local completion flag, and instance/tenant task actions consume `_links` from HAL-wrapped existing status endpoints.
- Composition measurement retained the no-snapshot decision. Instance load/refresh performs one status read followed by one parallel set of five existing reads (system status, branding, authentication status, authorization status, preflight); tenant load/refresh performs status plus settings only when incomplete. Tests prove one call set per initial/explicit refresh, overlapping instance/tenant refreshes are deduplicated, and post-mutation status is re-fetched. No contradictory-state, request-amplification, duplicated-derivation, or atomicity escalation trigger was reproduced.

## Constraints And Rules To Remember

- Primary intent: `external-infrastructure-bootstrap`; complete contract is in Plan §0.1.
- Active conditional intents: `add-hal-link`, `openapi-contract-change`, and `blazor-component-affordance`; their API/HATEOAS/Blazor rules and minimum tests are loaded. Endpoint/CQRS snapshot and Cerbos-policy intents remain deferred.
- HAL controls action affordances; never reveal actions from local roles/claims.
- MultiTenant tenant resolution fails closed; do not fabricate tenant context.
- Setup secret and tokens stay in BFF/server flows and out of browser storage/logs/DTOs.
- Repositories return entities; handlers map DTOs; validators are manually instantiated.
- Controllers are thin, named, classified, and advertise ProblemDetails.
- Generated API client is regenerated discretely and never hand-edited.
- InteractiveAuto/WASM cannot assume `HttpContext`.
- WCAG 2.2 AA, localized strings, logical CSS, RTL, semantic headings, keyboard and live-region behavior are mandatory.
- Preserve unrelated working-tree changes.

## Validation Baseline

Required eventual commands:

```bash
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

Planning-doc validation should include architecture/context schema and link tests. Manual completion requires keyboard, screen reader, RTL, dark/light, long localization strings, refresh/retry, provider failure, SingleTenant, MultiTenant with zero tenants, and first-tenant handoff.

Final verification evidence on 2026-07-12:

- Pre-producer Release build baseline: passed with zero errors. Existing package/dependency warnings remain.
- Pre-producer full `Event.Application.UnitTests` baseline: passed.
- Focused instance page 19/19, tenant page 14/14, task-list 4/4, instance service 29/29, startup/tenant service, API HAL 11/11, BFF forwarding 14/14, Application completion/preflight, setup-secret filter, docs/context, and HATEOAS parity suites: passed.
- Latest full Application: 2,205 passed.
- Pre-producer full serialized Client: 1,618 passed, one governed skip, zero failures.
- Pre-producer full Blazor Integration: 241 passed with real containerized Keycloak.
- Latest full API: 1,722/1,733 passed, eight failed, three skipped; each failure still requires explicit attribution.
- Latest full Architecture: 263/268 passed, four failed, one governed skip.
- Focused provider source 9/9, redacted endpoint mapping 1/1, and setup-layout source 1/1: passed.
- Current authorization slice: configuration/options 13/13, provider/single-flight 19/19, policy-package target isolation 22/22, runtime provider 23/23, boot runner 4/4, authorization page 13/13, instance onboarding service 34/34, admin provider layout 10/10, Setup 9/9, and authentication source 10/10 passed. Client/API/Infrastructure Release builds have zero errors.
- Final browser base and detected runs: exit zero. The independent visual gate is `PASS`; only pre-existing CSP meta/inline-script console warnings remain.
- The prior `.slnx` and migration blockers are resolved. The last real stack ran API, Blazor, Keycloak, database, cache, and Cerbos; `/health` remained `503` because S3 storage was unhealthy. A fresh current-source replay is pending.
- Post-refresh DocumentationQuality passes 4/4, AgentContextLink passes 8/8, and AgentContextSchema passes 9/9.

## Current Known Risks / Unknowns

- **Resolved / Tasks 3.1 and 6.1:** Measured endpoint composition, refresh deduplication, and post-mutation confirmation did not meet the snapshot escalation threshold.
- **Resolved / Tasks 1.2 and 4.1:** Platform, tenant, setup, completion, and management relations have permission/setup-secret-scoped HAL coverage and fail-closed tests.
- **Resolved at producer/service boundary:** Deployment-only Keycloak has current-source shared-service and TestServer integration coverage for sanitized detection, configured state, precedence, and secret exclusion.
- **Resolved / Task 6.3 implementation/docs:** Explicit provider mapping, runtime precedence, deployment ownership, instance-only background reconciliation, bounded retries/single-flight, authoritative navigation, one-column UI, canonical docs, and focused server/client tests pass. Live Aspire/browser proof remains open.
- **Open / Tasks 8.1 and 8.2:** Do not claim a full deployment/login/realm-management or authorization bootstrap journey until it is verified manually against the real stack.
- **Open / Task 8.2:** Responsive, keyboard, focus, RTL, dark/light, and long-text browser checks pass; authenticated postlaunch and assisted screen-reader journeys still require a healthy real stack.
- **Open / Task 8.1:** Eight API and four Architecture failures prevent a fully green required-project baseline until they are attributed or fixed.
- **Open / all work:** Unrelated managed-control-plane changes must remain isolated from any future commit or diagnosis; `.codex/config.toml` currently has no diff.

## Overlap And Supersession

`dev/pause/tenant-onboarding-enterprise/` remains the owner of tenant invitations, lifecycle transitions, self-service registration, and broader tenant creation. This workstream supersedes only its stale assumptions that first-run routing should enter a tenant wizard or that current tenant creation/authorization handlers are absent. Do not update the paused files unless the user separately reactivates that scope.

## Handoff Notes

### Handoff — 2026-07-12 Europe/Brussels

- **Current state:** Core onboarding UI, production Keycloak detection/manageability, explicit authorization intent/reconciliation, generated contracts, and focused tests are implemented. Tasks 6.3, 8.1, 8.2, and 8.3 remain open.
- **Next action:** Run real Aspire/browser authorization QA, rerun required suites, then complete authenticated Keycloak and assisted screen-reader journeys.
- **Blockers:** Eight current API failures, four unrelated Architecture failures, S3 readiness in the local stack, and unavailable assisted screen-reader tooling. The prior `EMFILE` and `.slnx` root-discovery blockers are resolved.
- **Modified files:** Onboarding Blazor/BFF/API HAL/controller/service/test files, shared authentication and authorization provider services/handlers, API background/configuration mapping, AppHost/Compose deployment configuration, generated OpenAPI/client artifacts, canonical docs, and these three workstream documents. Unrelated work remains preserved.
- **Validation:** Current focused Keycloak and authorization tests pass, including instance-target isolation, single-flight coordination, retries, endpoint hardening, route behavior, and one-column UI. Client/API/Infrastructure Release builds pass; earlier full Release/Application/Client/BFF runs were green, while post-change broad reruns remain pending. Exact API/Architecture/runtime blockers are recorded above and in the checklist.
- **Documentation impact:** Canonical configuration, secrets, self-hosting, troubleshooting, deployment-mode, Blazor, API changelog, and API inventory documentation now match implemented behavior.
- **Risks:** Environment-detected Keycloak is production-proven at the service/API boundary but not through a real browser login/realm-management deployment journey; authenticated and assisted screen-reader journeys remain open; unrelated workspace changes can contaminate broad test results.
- **Notes for next contributor/agent:** Do not mark Tasks 8.1/8.2 complete without direct green evidence. Do not implement a snapshot, Cerbos inventory, decision-test API, tenant invitation, lifecycle, or self-service flow without explicit reclassification and evidence.

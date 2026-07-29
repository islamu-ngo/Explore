<!-- ABOUTME: Tactical implementation checklist for the onboarding foundation and unified workspace refactor. -->
<!-- ABOUTME: Sequences design contract, shared shell, secure route integration, authority-safe journeys, and reference-fidelity QA. -->

# Onboarding UX Refactor — Task Checklist

> **Current status (2026-07-29):** The shared onboarding workspace exists for numbered post-entry routes, while `/setup` is restored as the separate split-screen setup-secret gateway.
> **Progress:** 15/22 unconditional tasks complete. Tasks 6.3, 8.1-8.3, 9.4-9.5, and 10.1 remain unchecked; conditional Task 6.2 remains deferred.
> **Current priority:** Task 9.4 — add narrow profile persistence and integrate the remaining post-authentication instance steps.
> **Final review gate:** Fresh visual, goal, QA, code-quality, security, and context review is required after Phase 9.
> **Corrected re-baseline:** Sharing `SetupLayout` does not equal a unified experience. Persistent journey orientation, focused step content, contextual navigation/status, and consistent actions are now explicit implementation scope.

Last Updated: 2026-07-29 Europe/Brussels

## Status Summary

- **Overall status:** Behavioral foundation and shared workspace implemented; post-entry route integration remains in progress
- **Checklist completed:** 15/22 unconditional tasks; conditional Task 6.2 remains deferred
- **Planning completed:** Corrected repository/current-screenshot re-baseline is complete
- **Current priority:** Task 9.4 — profile persistence and post-authentication instance steps
- **Next recommended slice:** Task 9.4 after the restored setup-entry evidence is reviewed
- **Implementation has started:** Prior foundation yes; Phase 9 Tasks 9.1-9.3 complete
- **Planning re-baseline:** Complete — new workspace scope added without runtime edits

## Implementation Maintenance Rules

- [x] Before starting work, read plan/context/tasks.
- [x] Capture current repository status and preserve unrelated user changes.
- [x] After each completed task, update this checklist immediately.
- [x] If scope, architecture, authority, or risks change, update the plan before continuing.
- [x] If discoveries affect future work, update context and the relevant task.
- [x] Reclassify conditional intents before endpoint, HAL, OpenAPI, CQRS, or Cerbos policy work. (`add-hal-link`, `openapi-contract-change`, and `blazor-component-affordance` activated 2026-07-12; snapshot/CQRS/Cerbos intents remain deferred.)
- [x] Final implementation summary uses Implemented / Verified / Remaining / Next / Docs updated.
- [x] Before pause/handoff/PR, refresh all three dev docs.

## Phase 0: Plan Review And Baseline

- [x] **0.1 User approves or corrects the plan**
  - **Files:** all three planning docs
  - **Acceptance:** status changes from Draft to User-reviewed/Approved; SingleTenant journey, MultiTenant platform-first split, authority matrix, snapshot threshold, and deferred scope are accepted or corrected.
  - **Validation:** plan/context/tasks agree after review.
  - **Effort:** S
  - **Dependencies:** none

- [x] **0.2 Implementation agent confirms current repository state before first edit**
  - **Files:** no edits; inspect status and current symbols/contracts
  - **Acceptance:** unrelated managed-control-plane and `.codex` changes are identified and left untouched; no stale planning assumption is used blindly.
  - **Validation:** status recorded in context; targeted source paths re-verified.
  - **Effort:** S
  - **Dependencies:** 0.1

## Phase 1: Contract And TDD Baseline ✅ COMPLETE

- [x] **1.1 Encode SingleTenant and MultiTenant journeys in Blazor tests**
  - **Files:** existing onboarding/startup/setup/route-guard tests under `tests/Explore.Blazor.Client.Tests/`
  - **Acceptance:** SingleTenant completes to events/settings; MultiTenant completes platform to control-plane settings; first tenant is optional; mode chooser absent; blocker/warning semantics explicit; completed authentication-provider state retains **Manage authentication** when its authoritative HAL affordance is present.
  - **Validation:** `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - **Evidence:** focused `InstanceOnboardingTests` 19/19, `TenantOnboardingTests` 14/14, and `StartupGateTests` passed on 2026-07-12; full project verification remains Task 8.1.
  - **Effort:** M
  - **Dependencies:** 0.2

- [x] **1.2 Encode setup, platform, and tenant authority boundaries**
  - **Files:** existing BFF setup-secret tests; API onboarding tests; **new if absent and needed:** `tests/Event.API.IntegrationTests/Features/TenantOnboardingControllerTests.cs`
  - **Acceptance:** browser privileged headers ignored; no secret echo; platform/tenant denial cases fail closed; RFC 7807 safe; setup inactive/invalid/rate-limit behavior preserved.
  - **Validation:** targeted `Explore.Blazor.IntegrationTests` and `Event.API.IntegrationTests` runs.
  - **Evidence:** 11 onboarding HAL policy tests and all 14 focused `SetupSecretForwardingHandlerTests` pass; every outbound request strips a spoofed setup-secret header, only exact/slash-delimited onboarding endpoint paths can receive the trusted BFF session secret, and query-string or near-route lookalikes fail closed. Red/green audit: the prior substring matcher failed all three lookalike cases (11/14), while the final strict matcher passes 14/14.
  - **Effort:** M
  - **Dependencies:** 0.2

## Phase 2: Accessible Task-List Primitive ✅ COMPLETE

- [x] **2.1 Inventory reusable wrappers, tokens, and localization resources**
  - **Files:** existing onboarding pages, common components, design tokens, localization resources
  - **Acceptance:** selected components/resource keys recorded in context; no duplicate wrapper or hard-coded English workflow text planned.
  - **Validation:** source evidence and path list added to context.
  - **Effort:** S
  - **Dependencies:** 1.1

- [x] **2.2 Add the minimal display-only onboarding task list**
  - **Files:** **new** `src/Explore.Blazor.Client/Pages/Onboarding/Components/OnboardingTaskList.razor`; **new** isolated CSS and tests; item/model file only if proven necessary
  - **Acceptance:** one-column responsive layout; semantic list; required/optional and status text; keyboard-safe links/actions; live-region-compatible updates; logical CSS; ≥24px targets; no role/claim/business logic.
  - **Validation:** Blazor client tests plus keyboard, RTL, dark/light, and long-string QA.
  - **Evidence:** semantic component, isolated CSS, live-region behavior, and four focused component tests implemented and passing; cross-viewport/manual checks remain Task 8.2.
  - **Effort:** M
  - **Dependencies:** 2.1, 1.2

## Phase 3: Instance Setup Overview And Launch ✅ COMPLETE

- [x] **3.1 Compose existing instance status/provider/preflight contracts into tasks**
  - **Files:** existing `InstanceOnboarding.razor`, `InstanceOnboardingService.cs`, focused provider pages, existing instance-status API response plus HAL policy/assembler; new local typed state only if needed
  - **Acceptance:** authoritative status; blockers required; warnings optional/remediation; absent/error state fails closed; request deduplication/cancellation; no snapshot endpoint or local claim authority; authentication-provider completion does not remove a HAL-authorized **Manage authentication** action: the focused setup page is used before launch and the admin provider editor after launch.
  - **Validation:** state permutation and request-count tests in `Explore.Blazor.Client.Tests`.
  - **Evidence:** instance status is HAL-wrapped; service mapping preserves `_links`; provider-status failures are distinct from unconfigured state; completion is re-fetched from the server; 19 page and 29 service tests pass.
  - **Effort:** L
  - **Dependencies:** 2.2

- [x] **3.2 Implement accessible review and launch state**
  - **Files:** existing `InstanceOnboarding.razor` and CSS/service/tests
  - **Acceptance:** PageTitle + one h1; resolved mode is read-only; safe retry; RFC 7807 display; success/error announcements; SingleTenant and MultiTenant handoffs match the plan.
  - **Validation:** Blazor tests, Application completion/preflight tests, API integration, manual refresh/failure checks.
  - **Evidence:** one-h1 review page, read-only deployment context, safe alerts/live announcements, server-confirmed completion, and mode-specific handoffs implemented; broader API/manual validation remains Phase 8.
  - **Effort:** L
  - **Dependencies:** 3.1

## Phase 4: Optional Tenant-Scoped Onboarding ✅ COMPLETE

- [x] **4.1 Refactor tenant onboarding to the same task-list language**
  - **Files:** existing `TenantOnboarding.razor`, `TenantOnboardingService.cs`, tenant status API response plus HAL policy/assembler, tenant settings components, tests
  - **Acceptance:** explicit trusted tenant context; tenant-scoped status/settings/progress/completion; platform and tenant handoffs distinct; completion and management actions gated by server HAL links; locked settings server-authoritative; no invitation/lifecycle/self-service scope.
  - **Validation:** Blazor tenant tests, tenant Application tests, tenant API integration coverage.
  - **Evidence:** trusted tenant context, locked settings, HAL-only complete/management actions, tenant-drift protection, and post-mutation server confirmation implemented; 14 page tests and the tenant service suite pass.
  - **Effort:** L
  - **Dependencies:** 2.2, 3.2

## Phase 5: Recovery And Trust-Boundary Hardening ✅ COMPLETE

- [x] **5.1 Test and document partial-completion recovery**
  - **Files:** existing BFF setup services/endpoints, onboarding pages/services, setup/provider tests, context recovery matrix
  - **Acceptance:** invalid/expired secret, interrupted provider verification, unavailable authz provider, preflight blocker, repeated completion, refresh after partial success, and post-lock rerun each have detection, safe message, retry/remediation, and operator action.
  - **Validation:** targeted BFF/API/Application tests and manual local failure injection.
  - **Evidence:** recovery matrix documents all seven required scenarios; focused UI failure-state tests, setup-secret forwarding/filter tests, HAL tests, preflight handler tests, and instance/tenant completion handler tests pass. BFF forwarding additionally rejects query-string and near-route endpoint confusion while stripping browser-controlled headers globally.
  - **Effort:** L
  - **Dependencies:** 3.2, 4.1

## Phase 6: Backend Composition Decision Gate ✅ COMPLETE — SNAPSHOT DEFERRED

- [x] **6.1 Measure endpoint composition and record the decision**
  - **Files:** existing client services/tests; dev docs; no new endpoint initially
  - **Acceptance:** request counts, refresh behavior, cancellation, and state consistency measured. Composition retained unless a plan escalation trigger is reproduced.
  - **Validation:** evidence in context; plan decision updated; tests prove selected design.
  - **Evidence:** instance uses one status read plus five parallel reads; tenant uses status plus settings while incomplete; initial/refresh call counts and overlapping-refresh deduplication pass; no D5 escalation trigger reproduced.
  - **Effort:** S/M
  - **Dependencies:** 3.2, 4.1

- [ ] **6.2 Conditional only — design an aggregate snapshot if evidence requires it**
  - **Files:** exact new Application DTO/query/handler, API route/response/HAL contract, generated client, tests — to be named only after reclassification
  - **Acceptance:** `add-get-endpoint`, `add-cqrs-handler`, `openapi-contract-change`, and any HAL intent captured first; DTO secret-free; atomicity/cache semantics explicit; integration/client tests pass.
  - **Validation:** all intent-specific tests and NSwag regeneration.
  - **Effort:** L
  - **Dependencies:** 6.1 and demonstrated escalation trigger
  - **Default:** Deferred/not required

- [ ] **6.3 Reconcile deployment-selected authorization and simplify its onboarding surface**
  - **Files:** API configuration compatibility and existing Cerbos boot-sync worker; shared authorization-provider configuration/runtime services and DTO; AppHost/Compose/env examples; `Setup.razor`, `AuthProviderConfiguration.razor`, `AuthorizationProviderConfiguration.razor` plus scoped CSS; focused API/Infrastructure/Application/Client tests; canonical configuration/secrets/self-hosting docs.
  - **Acceptance:** `AUTHORIZATION_PROVIDER=local` selects deployment-managed Local and skips authorization onboarding without any Cerbos call; `cerbos` keeps runtime fail-closed, verifies the configured PDP, publishes the bundled policies server-side, never sends the automatic journey through the provider-choice page, and is considered configured only after both checks succeed; blank/unset does not infer intent from Cerbos endpoints or credentials and renders Local by default with Cerbos behind native progressive disclosure; invalid explicit values fail startup; deployment-managed values cannot be overridden by browser DTO flags; failure is visible as safe remediation from the instance task without secrets; provider controls remain one content column without nested cards or mouse-only choices. Phase 9 supplies the single shared journey rail/summary around that content.
  - **Validation:** focused compatibility, service, runtime-provider, boot-sync, navigation, and bUnit tests; Release build; Client/API/Architecture gates; Compose validation; real Aspire Local/Cerbos readiness and browser QA at desktop/mobile, LTR/RTL, light/dark, keyboard, focus, and long text.
  - **Evidence:** Implementation and canonical docs are complete; live Aspire/browser QA remains. Added validated `AUTHORIZATION_PROVIDER` compatibility mapping and deployment precedence shared by onboarding and `RuntimeAuthorizationProvider`. The background runner makes Local ready without Cerbos, or verifies the instance PDP before publishing only to the instance Admin API; it uses bounded retries and singleton single-flight state so startup/admin attempts cannot double-publish or overwrite ready state. Deployment-managed writes fail closed, pending/ready providers bypass the choice page, and final Cerbos failure exposes locked retry-only remediation. The Blazor page is one centered column with a real Local radio default and native Cerbos `<details>` disclosure; failed routing and post-launch retry refresh are server-authoritative, while Keycloak management remains available. Focused evidence passes: configuration mapping/options 13/13, provider service/single-flight 19/19, policy package target isolation 22/22, runtime provider 23/23, boot runner 4/4, authorization page 13/13, instance onboarding service 34/34, admin provider layout 10/10, Setup 9/9, authentication source 10/10, and Client/API/Infrastructure Release builds with zero errors.
  - **Effort:** L
  - **Dependencies:** 3.1, 5.1, 7.1

## Phase 7: Documentation And Operations ✅ FOUNDATION COMPLETE

- [x] **7.1 Update operator and developer documentation**
  - **Files:** existing `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, `docs/DEPLOYMENT_MODES.md`, `docs/BLAZOR.md`; conditional `docs/API_CHANGELOG.md`
  - **Acceptance:** mode/admin host are operator config; setup secret BFF-owned; authority matrix and both journeys documented; required/warning checks, rerun, rotation, backup, partial failure, Cerbos scope, optional first tenant, and persistent pre/post-launch Keycloak realm management covered.
  - **Validation:** docs link/schema/context tests; commands/config examples reviewed.
  - **Evidence:** six operator/developer guides plus `API_CHANGELOG.md` updated; OpenAPI and API inventory regenerated; documentation-quality, context-link, and context-schema tests pass.
  - **Effort:** M
  - **Dependencies:** 5.1, 6.1 or 6.2

## Phase 8: Foundation Verification And Re-baseline ⏳ IN PROGRESS

- [ ] **8.1 Run diagnostics and required project tests individually**
  - **Files:** all modified files
  - **Acceptance:** clean diagnostics and all five intent minimum test projects pass; no solution-level `dotnet test`.
  - **Validation:** exact commands from plan §14 recorded in context.
  - **Evidence (in progress):** The latest broad Release build passed with zero errors. `Event.Application.UnitTests` passed 2,205/2,205; serialized `Explore.Blazor.Client.Tests` passed 1,618 with one governed skip; and `Explore.Blazor.IntegrationTests` passed 241/241. `Event.API.IntegrationTests` completed with 1,722/1,733 passed, eight failed, and three skipped; `Event.Architecture.Tests` completed with 263/268 passed, four failed, and one governed skip. Those failures still require attribution before Task 8.1 can close. Compose configuration passed. Current authorization focused coverage is green at 13 configuration/options, 19 provider/single-flight, 22 policy-package target-isolation, 23 runtime-provider, four boot-runner, 13 page, 34 client-service, ten admin-layout, nine Setup, and ten authentication-source tests. The final corrected-plan Release build passed with zero errors and 13,735 existing warnings. An earlier transient compile blocker in unrelated Actor changes cleared before that build; the Architecture project was not rerun during the docs-only correction.
  - **Effort:** L
  - **Dependencies:** 7.1

- [ ] **8.2 Run build, manual UX, accessibility, localization, and deployment checks**
  - **Files:** all modified files and affected deployment config
  - **Acceptance:** Release build; SingleTenant; MultiTenant zero-tenant; optional first tenant; provider failure/retry; keyboard; screen reader; RTL; dark/light; long translations. `docker compose config`/Aspire smoke only if deployment config changed.
  - **Validation:** evidence and residual risks recorded in context.
  - **Runtime evidence:** `aspire doctor` passes all four environment checks. The `.slnx` root-discovery and migration blockers recorded earlier are resolved: AppHost recognizes `Explore.slnx`, migration and `keycloak-init` exited zero, and API, Blazor, Keycloak, database, cache, and Cerbos resources ran. The last health probe was `503` only because S3 storage readiness was unhealthy. Previous browser checks passed at 1440x900 and 390x844 across LTR, Arabic RTL, dark mode, long text, keyboard disclosure/focus, and overflow/label/target checks; the independent visual gate was `PASS`. Those runs predate the authorization-page refactor and used a stubbed detected response, so fresh real-stack authorization screenshots and routing evidence remain required. Authenticated postlaunch instance/tenant, real deployed Keycloak realm-management, and assisted screen-reader journeys also remain open.
  - **Effort:** L
  - **Dependencies:** 8.1

- [ ] **8.3 Refresh foundation dev docs and verification handoff**
  - **Files:** this plan/context/tasks
  - **Acceptance:** completed boxes, decisions, changed files, validation, risks, remaining/deferred work, and next action match reality.
  - **Validation:** cold-agent resume review.
  - **Evidence:** plan, context, and checklist are refreshed at each verification checkpoint. They record pre-producer broad-suite results separately from focused producer/API results, unrelated-work ownership, the foundation-only visual pass, redacted-read hardening, Aspire/runtime limitations, and open Tasks 8.1/8.2. The newly confirmed unified-experience requirement supersedes that visual pass for Phase 9; a new final review is required. The previous post-refresh DocumentationQuality passes 4/4, AgentContextLink passes 8/8, and AgentContextSchema passes 9/9.
  - **Effort:** S
  - **Dependencies:** 8.2 (performed early so the blocked verification handoff remains accurate)

## Phase 9: Unified Onboarding Workspace 🟡 IN PROGRESS

- [x] **9.1 Define the workspace visual and state contract before component code**
  - **Files:** `docs/DESIGN.md`; focused component/source test files; current screenshots as baseline
  - **Acceptance:** user approval explicitly widens the current intent allow-list to `docs/DESIGN.md`, `*.razor.css`, and focused Blazor tests; `OnboardingWorkspace` documents desktop main/summary grid, tablet/mobile summary disclosure, header, conditional segmented progress, focused main step, contextual help, footer actions, loading/error/locked/skipped/complete/dirty states, RTL, themes, forced colors, reduced motion, and long-copy behavior. Access is outside numbered progress; the default instance journey is Authentication → Site profile → Authorization → Readiness/Launch, with visible-count recomputation for deployment-managed omission.
  - **Validation:** design-system review and failing bUnit/source tests for the declared structure/state matrix.
  - **Effort:** M
  - **Dependencies:** corrected plan approval

- [x] **9.2 Implement the shared display/navigation workspace primitive**
  - **Files:** likely new `src/Explore.Blazor.Client/Pages/Onboarding/Components/OnboardingWorkspace.razor` and isolated CSS; minimal step descriptor/model; `SetupLayout.razor` only where outer chrome changes; reuse/adapt `OnboardingTaskList` where appropriate
  - **Acceptance:** semantic header/nav/section/aside/footer inside `SetupLayout`'s existing `main#main-content`; `aria-current="step"`; native controls; one page h1; server-supplied status; project tokens/wrappers; no API/provider/role logic; no nested `main` or full-page card; no viewport overflow at 375/768/1280px.
  - **Validation:** bUnit state matrix plus component visual harness across LTR/RTL and light/dark.
  - **Effort:** L
  - **Dependencies:** 9.1

- [x] **9.3 Integrate setup access, authentication provider, and OIDC handoff**
  - **Files:** `Setup.razor`, `AuthProviderConfiguration.razor`, CSS/tests, `Routes.razor`, `SetupLayout`, existing BFF setup-secret tests
  - **Acceptance:** access remains setup-secret gated in the original split-screen `/setup` surface outside `OnboardingWorkspace`; detected Keycloak quick continue uses the authoritative post-entry destination, while missing Keycloak or explicit configuration enters `/onboarding/auth-provider`; required HttpOnly-cookie/OIDC hard reloads remain; no setup secret or draft enters browser storage; `StartupGate` remains the authoritative return router.
  - **Validation:** setup/provider component tests, BFF forwarding/session tests, route/focus/dirty-exit/resume tests.
  - **Evidence:** The original setup-entry characterization passed 1/1 before production edits. The corrected source contract then failed 0/1 against the rejected workspace wrapper at the missing split-panel assertion. After restoration, `SetupSourceTests` pass 3/3 and `SetupTests` pass 13/13, including both authoritative Keycloak post-entry destinations and explicit authentication configuration routing.
  - **Effort:** L
  - **Dependencies:** 9.2

- [ ] **9.4 Add profile draft persistence and integrate site profile, authorization, readiness, and launch**
  - **Files:** reuse `SelfHostOnboardingProfileDto`/validator; new `SaveInstanceOnboardingProfileCommand`/handler; `InstanceOnboardingController`, `RouteNames`, instance-status HAL policy, generated client; `InstanceOnboarding.razor`, `AuthorizationProviderConfiguration.razor`, `StartupGate.razor`, `InstanceOnboardingService.cs`, CSS/tests
  - **Acceptance:** `PATCH /api/instance-onboarding/profile` is `[Authorize]`, `[SetupSecretRequired]`, setup-rate-limited, manually validated, audited, RFC-7807-safe, and exposed only through `save-profile` HAL while setup is active and the caller authenticated; it persists only non-secret profile settings and no generic route history. The UI calls it only when HAL permits, then shows truthful saved/resumable status. One authoritative conditional step projection drives stable Back/Continue/Review/Launch; mode is read-only; Local/Cerbos skip/remediation, warnings, completion confirmation, and SingleTenant/MultiTenant-zero-tenant handoffs remain correct.
  - **Validation:** command/validator/API/HAL/generated-client tests, bUnit state permutations/request counts, completion/preflight tests, mode journey tests, reference screenshots.
  - **Effort:** XL
  - **Dependencies:** 9.3

- [ ] **9.5 Reuse the workspace for separate optional tenant onboarding**
  - **Files:** `TenantOnboarding.razor`, CSS/service/tests, shared step projection
  - **Acceptance:** tenant context is visible and trusted; tenant progress never appears in instance-launch progress; first tenant remains optional; locked settings, HAL actions, tenant drift, completion confirmation, and control-plane handoff remain server-authoritative.
  - **Validation:** tenant page/service/API tests plus desktop/mobile/RTL visual states.
  - **Effort:** L
  - **Dependencies:** 9.4

## Phase 10: Reference-Fidelity Verification And Final Handoff ⏳ NOT STARTED

- [ ] **10.1 Run final UX, security, test, docs, and review gates**
  - **Files:** all Phase 9 files, `docs/DESIGN.md`, affected canonical docs, and these three workstream files
  - **Acceptance:** `/visual-qa` reference-fidelity pass at 375/768/1280px for access, provider, instance, readiness, authorization remediation, and tenant states; keyboard/screen-reader/RTL/theme/forced-colors/long-copy checks; no secret/HAL/tenant/completion/recovery regression; required projects and Release build pass or unrelated blockers are attributed.
  - **Validation:** exact plan §14 commands, real-stack mode journeys, dual-review visual QA, final handoff refresh.
  - **Effort:** L
  - **Dependencies:** 9.5 and remaining Phase 8 gates

## Verification Checklist

- [x] Production Keycloak detection resolves complete deployment metadata, applies stored/deployment precedence, reports configured state consistently, and keeps client secrets out of public/admin responses.
  - Current focused evidence: 6/6 `AuthProviderConfigurationServiceTests`, 2/2 confidential-client validator tests, 5/5 `RotateKeycloakClientSecretCommandHandlerTests`, 1/1 API compatibility mapping test, and 1/1 TestServer onboarding projection test pass.
  - Save/update handler regression cases pass and cover server-derived configured-secret state versus forged request ownership.
  - The HTTP test covers public setup read, configured status, and authenticated administrator read, and asserts the deployment secret is absent from serialized responses.
  - The existing detected-provider browser visual run used a stubbed response; it is not a full deployed Keycloak login or realm-repair runtime proof.

- [x] LSP diagnostics and compiler checks are clean for modified onboarding source files.
- [x] `dotnet build --configuration Release --verbosity quiet` passes.
- [x] `Event.Application.UnitTests` passes individually.
- [ ] `Event.API.IntegrationTests` passes individually.
- [x] `Explore.Blazor.IntegrationTests` passes individually (241/241).
- [x] `Explore.Blazor.Client.Tests` passes individually and serialized (1,618 passed, one governed skip).
- [ ] `Event.Architecture.Tests` passes individually.
- [x] Onboarding HATEOAS architecture, context schema/link, and documentation-quality tests pass.
- [x] API client regenerated only because the status contracts changed; generated code was not hand-edited.
- [x] Setup-secret header stripping/replacement and no-secret-output tests pass.
- [x] Platform-admin and tenant-admin HAL denial/isolation tests pass.
- [x] Completed authentication-provider state retains **Manage authentication** only when the authoritative HAL affordance is present; missing/error state fails closed.
- [ ] Manual SingleTenant and MultiTenant journeys pass.
- [ ] Keyboard, focus/live regions, screen reader, contrast, RTL, dark/light, and localization checks pass (foundation browser evidence exists; Phase 9 workspace and assisted-screen-reader coverage remain open).
- [x] Docs updated for behavior/configuration/operations/API changes; documentation verification remains open.
- [ ] `docker compose config` and Aspire smoke pass if deployment files changed (`docker compose config` passed; fresh post-change Aspire reconciliation and UI evidence remain).
- [ ] Dev docs receive their final refresh after live QA, broad suites, and the final five-lane review (this interim checkpoint is current).
- [x] Unrelated working-tree changes remain untouched.
- [ ] `docs/DESIGN.md` defines the `OnboardingWorkspace` primitive and complete state matrix before UI implementation.
- [ ] Shared workspace progress/content/journey-navigation/action semantics pass bUnit tests.
- [ ] Conditional step count, skipped/deployment-managed state, revisits, and `aria-current` are server-derived and tested.
- [ ] Exit/resume is tested without browser storage for setup/provider secrets or unsaved configuration.
- [ ] Normal visual QA against `docs/DESIGN.md` passes at 375/768/1280px; prior fragmented screenshots are baseline only.

## Remaining / Deferred Work

- Current-source focused authorization tests pass. The last broad build/client/Application/BFF baseline was green; eight API failures and four Architecture failures remain to be attributed. Fresh authorization visual/runtime QA and post-change broad reruns are pending.
- Deployment-only Keycloak is now implemented and proved at the shared service/API boundary: complete metadata sets sanitized detected/enabled/configured state, partial tuples fail closed, stored/deployment precedence is explicit, and public/admin responses omit the secret. HAL management navigation remains available before and after launch so operators can still create, repair, or reconcile the realm. A real deployed browser login and realm-management journey is still required before Tasks 8.1/8.2 can close.
- Tenant invitations, lifecycle transitions, self-service registration, and public tenant creation remain in `dev/pause/tenant-onboarding-enterprise/`.
- Cerbos inventory and arbitrary policy decision-test APIs are deferred without an accepted operator workflow and threat model.
- Aggregate onboarding snapshot is deferred unless Task 6.1 proves an escalation trigger.
- Federation-specific onboarding is not part of this workstream.
- The behavioral foundation is complete; Phase 9 unified workspace implementation and Phase 10 verification are not started.

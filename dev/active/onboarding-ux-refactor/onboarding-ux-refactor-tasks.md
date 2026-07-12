<!-- ABOUTME: Tactical implementation checklist for the onboarding UX refactor workstream. -->
<!-- ABOUTME: Sequences tests, accessible task-list UI, authority-safe journeys, recovery, docs, and verification. -->

# Onboarding UX Refactor — Task Checklist

> **Current status (2026-07-12):** Core UI and production environment-detection implementation are complete; required-suite and authenticated runtime verification remain open.
> **Progress:** 13/15 tasks complete. Tasks 8.1 and 8.2 remain unchecked.
> **Current priority:** Finish confidential-client handler verification and required post-change suites, restore the real Aspire/authenticated journey, and complete assisted screen-reader verification. The production Keycloak producer gap is resolved.
> **Final review gate:** INCONCLUSIVE. Goal, QA, code-quality, security, and context lanes could not independently inspect or execute after the host reached persistent `EMFILE`; no PASS is inferred.

Last Updated: 2026-07-12 Europe/Brussels

## Status Summary

- **Overall status:** Core UI and production environment-detection implementation are complete; required-suite and authenticated runtime verification remain open
- **Checklist completed:** 13/15 unconditional tasks; conditional Task 6.2 remains deferred
- **Planning completed:** Repository evidence, decisions, phases, context, and checklist created
- **Current priority:** Tasks 8.1 and 8.2 — finish required post-change suites, then complete authenticated runtime and assisted screen-reader journeys
- **Next recommended slice:** Restore the real Aspire stack, verify SingleTenant and MultiTenant journeys against deployment-supplied Keycloak, and complete assisted screen-reader checks
- **Implementation has started:** Yes — core implementation and focused verification are complete; only Tasks 8.1 and 8.2 remain open

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

## Phase 7: Documentation And Operations ✅ COMPLETE

- [x] **7.1 Update operator and developer documentation**
  - **Files:** existing `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`, `docs/DEPLOYMENT_MODES.md`, `docs/BLAZOR.md`; conditional `docs/API_CHANGELOG.md`
  - **Acceptance:** mode/admin host are operator config; setup secret BFF-owned; authority matrix and both journeys documented; required/warning checks, rerun, rotation, backup, partial failure, Cerbos scope, optional first tenant, and persistent pre/post-launch Keycloak realm management covered.
  - **Validation:** docs link/schema/context tests; commands/config examples reviewed.
  - **Evidence:** six operator/developer guides plus `API_CHANGELOG.md` updated; OpenAPI and API inventory regenerated; documentation-quality, context-link, and context-schema tests pass.
  - **Effort:** M
  - **Dependencies:** 5.1, 6.1 or 6.2

## Phase 8: Verification And Handoff ⏳ IN PROGRESS — VISUAL GATE PASSED

- [ ] **8.1 Run diagnostics and required project tests individually**
  - **Files:** all modified files
  - **Acceptance:** clean diagnostics and all five intent minimum test projects pass; no solution-level `dotnet test`.
  - **Validation:** exact commands from plan §14 recorded in context.
  - **Evidence (in progress):** Before the producer slice, the Release build passed with zero errors; full `Event.Application.UnitTests` passed 2,170/2,170; serialized `Explore.Blazor.Client.Tests` passed 1,618 with one governed skip and zero failures; and `Explore.Blazor.IntegrationTests` passed 241/241 with real containerized Keycloak. The latest broad `Event.API.IntegrationTests` run completed with 1,721 passed, six failed, and three governed skips; the six failures are outside this workstream. `Event.Architecture.Tests` completed with 263 passed, four unrelated managed-control-plane failures, and one governed skip. Current-source focused producer coverage now passes: 6/6 `AuthProviderConfigurationServiceTests`, 2/2 confidential-client validator tests, 5/5 rotation-handler tests, 1/1 API compatibility-mapping test, and 1/1 TestServer onboarding projection test. Save/update handler regression cases for server-derived configured-secret state and forged request ownership compile but still need focused execution. The HTTP projection proves public setup, configured status, and authenticated administrator reads expose sanitized deployment metadata without serializing the client secret. Post-change broad suites remain pending because the execution host remains intermittently constrained by `EMFILE`; Task 8.1 remains open.
  - **Effort:** L
  - **Dependencies:** 7.1

- [ ] **8.2 Run build, manual UX, accessibility, localization, and deployment checks**
  - **Files:** all modified files and affected deployment config
  - **Acceptance:** Release build; SingleTenant; MultiTenant zero-tenant; optional first tenant; provider failure/retry; keyboard; screen reader; RTL; dark/light; long translations. `docker compose config`/Aspire smoke only if deployment config changed.
  - **Validation:** evidence and residual risks recorded in context.
  - **Runtime evidence:** `aspire doctor` passes all four environment checks, but a real full-stack replay remains blocked by two unrelated runtime defects: `FindRepositoryRoot` recognizes obsolete `Explore.sln` instead of `Explore.slnx`, and the concurrent managed-control-plane migrations leave the local provisioning-operation relation inconsistent with EF migration history. A temporary root marker proved the first diagnosis and was removed; no product workaround or database-destructive repair was retained. Final Playwright QA therefore used the production Blazor UI/CSS with a temporary read-only API stub. Base and deployment-detected runs both exited zero at 1440x900 and 390x844, including LTR, Arabic RTL, dark mode, long text, native Enter/Space disclosures, one h1, visible 2px focus, and zero horizontal overflow, duplicate IDs, unlabeled visible inputs, sub-24px targets, or switcher/heading collisions. Both **Patch existing realm** and **Create realm if missing** are present, and detected Keycloak retains **Configure Authentication Providers**. The first independent review returned `REVISE`; fixes moved the mobile switchers into document flow, shortened visible labels while preserving full accessible names, restored native disclosures, isolated fallback direction, raised scoped contrast, added the rendered switch focus ring, kept mobile markers with their headings, made credential gating mode-specific, sanitized exception logging, and cleared all transient bootstrap fields in `finally`. The final independent visual-fidelity gate is `PASS`. The setup page now reads the public redacted provider endpoint, clears returned secret fields before render, and never prefills an existing Keycloak secret into bootstrap controls. The production producer/service boundary is now covered by passing shared-service and TestServer HTTP tests, including sanitized detected/configured state. Authenticated postlaunch instance/tenant and real deployed Keycloak realm-management journeys remain untested; the stubbed browser run is not claimed as end-to-end deployment proof.
  - **Effort:** L
  - **Dependencies:** 8.1

- [x] **8.3 Refresh dev docs and create final handoff**
  - **Files:** this plan/context/tasks
  - **Acceptance:** completed boxes, decisions, changed files, validation, risks, remaining/deferred work, and next action match reality.
  - **Validation:** cold-agent resume review.
  - **Evidence:** plan, context, and checklist are refreshed at each verification checkpoint. They now record pre-producer broad-suite results separately from current focused producer/API results, unrelated-work ownership, final visual `PASS`, redacted-read hardening, Aspire/runtime limitations, and open Tasks 8.1/8.2. The earlier goal/code review `FAIL` identified the producer gap that this slice resolves; a new final review is required before claiming pass. The previous post-refresh DocumentationQuality passes 4/4, AgentContextLink passes 8/8, and AgentContextSchema passes 9/9; post-change reruns remain pending during host `EMFILE`.
  - **Effort:** S
  - **Dependencies:** 8.2 (performed early so the blocked verification handoff remains accurate)

## Verification Checklist

- [x] Production Keycloak detection resolves complete deployment metadata, applies stored/deployment precedence, reports configured state consistently, and keeps client secrets out of public/admin responses.
  - Current focused evidence: 6/6 `AuthProviderConfigurationServiceTests`, 2/2 confidential-client validator tests, 5/5 `RotateKeycloakClientSecretCommandHandlerTests`, 1/1 API compatibility mapping test, and 1/1 TestServer onboarding projection test pass.
  - Save/update handler regression cases compile and cover server-derived configured-secret state versus forged request ownership; their focused run remains pending.
  - The HTTP test covers public setup read, configured status, and authenticated administrator read, and asserts the deployment secret is absent from serialized responses.
  - The existing detected-provider browser visual run used a stubbed response; it is not a full deployed Keycloak login or realm-repair E2E proof.

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
- [ ] Keyboard, focus/live regions, screen reader, contrast, RTL, dark/light, and localization checks pass (browser keyboard/focus/contrast/RTL/theme/long-text evidence and visual gate pass; assisted screen-reader coverage remains open).
- [x] Docs updated for behavior/configuration/operations/API changes; documentation verification remains open.
- [ ] `docker compose config` and Aspire smoke pass if deployment files changed (Aspire application resources are healthy through a temporary, removed root-marker toggle; the pre-existing `.slnx` root-discovery defect remains).
- [x] Dev docs refreshed with final state.
- [x] Unrelated working-tree changes remain untouched.

## Remaining / Deferred Work

- Final visual QA is green and current-source focused producer/API tests pass. The last broad build/client/Application/BFF baseline was green; six unrelated API failures and four unrelated Architecture failures remained. Post-change broad reruns are pending during host `EMFILE`.
- Deployment-only Keycloak is now implemented and proved at the shared service/API boundary: complete metadata sets sanitized detected/enabled/configured state, partial tuples fail closed, stored/deployment precedence is explicit, and public/admin responses omit the secret. HAL management navigation remains available before and after launch so operators can still create, repair, or reconcile the realm. A real deployed browser login and realm-management journey is still required before Tasks 8.1/8.2 can close.
- Tenant invitations, lifecycle transitions, self-service registration, and public tenant creation remain in `dev/pause/tenant-onboarding-enterprise/`.
- Cerbos inventory and arbitrary policy decision-test APIs are deferred without an accepted operator workflow and threat model.
- Aggregate onboarding snapshot is deferred unless Task 6.1 proves an escalation trigger.
- Federation-specific onboarding is not part of this workstream.
- Core implementation is complete; Tasks 8.1 and 8.2 remain open until full-suite and live-runtime evidence is green.

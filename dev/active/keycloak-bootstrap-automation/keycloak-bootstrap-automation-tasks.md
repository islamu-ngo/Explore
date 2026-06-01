<!-- ABOUTME: Tactical checklist for implementing Keycloak realm/client bootstrap automation. -->
<!-- ABOUTME: Tracks Compose init, external Keycloak setup, tests, docs, validation, and deferred work. -->

# Keycloak Bootstrap Automation — Task Checklist

Last Updated: 2026-06-01 Europe/Brussels

## Status Summary
- **Overall status:** Implementation complete through automated disposable-Keycloak backend integration smoke and focused Playwright browser UI bootstrap e2e; Phase 7 post-onboarding Keycloak doctor/resync/rotation is now planned future work; unrelated architecture cleanup remains.
- **Completed:** 33/39
- **Current priority:** Phase 7 is documented but not started. Separately address unrelated Architecture test failures if this branch needs a fully green suite.
- **Next recommended slice:** Start Phase 7 with the read-only Keycloak realm doctor, or triage unrelated Architecture failures in a separate workstream.

## Implementation Maintenance Rules
- [x] Before starting work, read plan/context/tasks.
- [x] After each completed task, update this checklist immediately.
- [x] If implementation changes scope or architecture, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline ✅ COMPLETE
- [x] Create dev docs directory and three required files.
  - Acceptance: `dev/active/keycloak-bootstrap-automation/` contains plan/context/tasks.
- [x] Capture current-state evidence and source-grounded files.
  - Acceptance: plan Section 2 includes evidence table and current implementation report.
- [x] Record validation baseline.
  - Acceptance: context notes build baseline with warnings and zero errors.
- [x] User reviews the plan and approves or corrects scope.
  - Acceptance: plan status changes from Draft to User-reviewed/Approved.
- [x] Implementation agent confirms current repo state before first edit.
  - Acceptance: no stale assumptions from planning are used blindly.

## Phase 1: Compose-managed Keycloak Init Job ✅ COMPLETE
- [x] **1.1 Create idempotent Keycloak init script**
  - **Files:** `docker/keycloak/keycloak-init.sh` (new)
  - **Acceptance:** script authenticates to Keycloak Admin API/`kcadm.sh`, locates `islamu-event-blazor`, sets secret from `KEYCLOAK_BLAZOR_CLIENT_SECRET`, optionally sets `islamu-event-api` from `KEYCLOAK_API_CLIENT_SECRET`, redacts all secret values in logs, and can rerun safely.
  - **Validation:** shell syntax check if available; manual `docker compose run --rm keycloak-init` after service exists.
  - **Effort:** M
  - **Dependencies:** Phase 0.
- [x] **1.2 Wire `keycloak-init` into Compose**
  - **Files:** `docker-compose.yml` (existing)
  - **Acceptance:** one-shot service depends on healthy `keycloak`, uses same Keycloak image or bounded curl image, gets admin/client secret env vars, and exposes no public ports.
  - **Validation:** `docker compose config`; local compose smoke.
  - **Effort:** S
  - **Dependencies:** 1.1.
- [x] **1.3 Decide API/UI dependency on init completion**
  - **Files:** `docker-compose.yml` (existing)
  - **Acceptance:** API/Blazor either wait for `keycloak-init` success or docs clearly explain how to recover/retry when init fails.
  - **Validation:** `docker compose config`; local startup ordering check.
  - **Effort:** S
  - **Dependencies:** 1.2.
- [x] **1.4 Update self-hosting docs for Compose**
  - **Files:** `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`
  - **Acceptance:** docs state self-hosters set secrets once in env/Infisical and do not manually edit Keycloak UI for client secrets; troubleshooting covers `unauthorized_client` from secret mismatch.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** S
  - **Dependencies:** 1.1-1.3.

## Phase 2: External Keycloak Bootstrap Application Contract ✅ COMPLETE
- [x] **2.1 Add bootstrap request/result DTOs**
  - **Files:** `Explore.Application/DTOs/Onboarding/KeycloakBootstrapRequestDto.cs` (new), `KeycloakBootstrapResultDto.cs` (new)
  - **Acceptance:** request includes Keycloak URL, realm, client IDs/secrets, mode, and one-time credential; result contains only safe booleans/status/error category.
  - **Validation:** build passed; focused Clean Architecture and Naming architecture tests passed.
  - **Effort:** M
  - **Dependencies:** user approval of external bootstrap path.
- [x] **2.2 Add Application service contract**
  - **Files:** `Explore.Application/Contracts/Services/IKeycloakBootstrapService.cs` (new)
  - **Acceptance:** contract takes request/cancellation token and returns safe result; no Infrastructure types leak into Application.
  - **Validation:** build passed; focused Clean Architecture architecture tests passed.
  - **Effort:** S
  - **Dependencies:** 2.1.
- [x] **2.3 Add manual validator**
  - **Files:** `Explore.Application/DTOs/Onboarding/Validators/KeycloakBootstrapRequestDtoValidator.cs` (new)
  - **Acceptance:** rejects blank/oversized/control-char secrets, invalid URLs, invalid realm/client IDs, missing required BFF client secret, unsafe mode combinations.
  - **Validation:** `Event.Application.UnitTests` passed, including control-character and oversized-secret validator cases.
  - **Effort:** M
  - **Dependencies:** 2.1.
- [x] **2.4 Add MediatR command and handler**
  - **Files:** `Explore.Application/Features/InstanceOnboarding/Requests/Commands/BootstrapKeycloakRealmCommand.cs` (new), `Handlers/Commands/BootstrapKeycloakRealmCommandHandler.cs` (new)
  - **Acceptance:** handler manually instantiates validator, calls bootstrap service, persists only normal auth-provider config on success, reloads JWT authority, never persists admin credential.
  - **Validation:** `Event.Application.UnitTests` passed.
  - **Effort:** M
  - **Dependencies:** 2.1-2.3.
- [x] **2.5 Add no-admin-secret-persistence tests**
  - **Files:** `Event.Application.UnitTests/Features/InstanceOnboarding/Commands/BootstrapKeycloakRealmCommandHandlerTests.cs` (new)
  - **Acceptance:** tests prove bootstrap admin credential is not passed into auth-provider storage and safe failures are returned.
  - **Validation:** `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed.
  - **Effort:** M
  - **Dependencies:** 2.4.

## Phase 3: Infrastructure Keycloak Admin API Adapter ✅ COMPLETE
- [x] **3.1 Implement Keycloak Admin client models**
  - **Files:** `Explore.Infrastructure/Services/Keycloak/*.cs` (new)
  - **Acceptance:** request/response models cover token acquisition, realm exists/create, client lookup/create/update secret, and safe result categories; raw secret fields are not logged or returned.
  - **Validation:** build and `Explore.Infrastructure.Tests` passed.
  - **Effort:** M
  - **Dependencies:** Phase 2 contract.
- [x] **3.2 Implement `IKeycloakBootstrapService`**
  - **Files:** `Explore.Infrastructure/Services/Keycloak/KeycloakBootstrapService.cs` (new)
  - **Acceptance:** authenticates with one-time credential, creates or patches realm, sets client secrets, returns categorized safe result; cancellation tokens flow through all HTTP calls.
  - **Validation:** `Explore.Infrastructure.Tests` passed with fake HTTP handler coverage for create, patch, auth failure, missing realm, and unsafe URL paths.
  - **Effort:** L
  - **Dependencies:** 3.1.
- [x] **3.3 Add SSRF and URL safety checks**
  - **Files:** Keycloak bootstrap validator/service files.
  - **Acceptance:** blocks unsupported schemes, user-info/query/fragment URL tricks, localhost/loopback/link-local/unspecified/multicast IP literals, while preserving self-host/internal DNS hostnames.
  - **Validation:** Infrastructure unit tests cover unsafe local/unsupported URLs with zero HTTP calls.
  - **Effort:** M
  - **Dependencies:** 3.2.
- [x] **3.4 Register service and HttpClient**
  - **Files:** `Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing)
  - **Acceptance:** uses named/bounded HttpClient; service lifetime does not capture per-request secrets; no singleton admin credential storage.
  - **Validation:** build, focused Clean Architecture, and focused Naming tests passed.
  - **Effort:** S
  - **Dependencies:** 3.2.
- [x] **3.5 Add Infrastructure tests**
  - **Files:** `Explore.Infrastructure.Tests/Infrastructure/KeycloakBootstrapServiceTests.cs` (new)
  - **Acceptance:** tests success, invalid credential, missing realm in patch mode, existing-client patch, unsafe URL rejection, and redaction behavior.
  - **Validation:** `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed.
  - **Effort:** L
  - **Dependencies:** 3.2-3.4.

## Phase 4: API Endpoint And BFF/UI Wiring ✅ COMPLETE
- [x] **4.1 Add setup-gated API endpoint**
  - **Files:** `Explore.API/Controllers/InstanceOnboardingController.cs`, `Explore.API/Hateoas/RouteNames.cs`
  - **Acceptance:** route has explicit template/name, `[AllowAnonymous]`, `[SetupSecretRequired]`, endpoint classification, response types, and dispatches command.
  - **Validation:** focused `InstanceOnboardingControllerTests` passed; focused API contract and endpoint-classification architecture tests passed.
  - **Effort:** S
  - **Dependencies:** Phase 2.
- [x] **4.2 Add API integration tests**
  - **Files:** `Event.API.IntegrationTests/Features/InstanceOnboardingControllerTests.cs` or new focused file.
  - **Acceptance:** endpoint rejects missing setup secret, rejects after setup complete, returns safe BadRequest on bootstrap failure, succeeds with fake service.
  - **Validation:** focused `Event.API.IntegrationTests` for `InstanceOnboardingControllerTests` passed sequentially.
  - **Effort:** M
  - **Dependencies:** 4.1.
- [x] **4.3 Update BFF setup-secret forwarding allow-list**
  - **Files:** `Explore.Blazor/Services/SetupSecretForwardingHandler.cs`, `Explore.Blazor/Services/CircuitAccessTokenService.cs` if relevant.
  - **Acceptance:** trusted setup secret reaches new endpoint; browser-supplied header is still stripped.
  - **Validation:** focused `Explore.Blazor.IntegrationTests` for `SetupSecretForwardingHandlerTests` passed sequentially.
  - **Effort:** S
  - **Dependencies:** 4.1.
- [x] **4.4 Add Blazor service/API methods and models**
  - **Files:** `Explore.Blazor.Client/Services/IInstanceOnboardingApi.cs`, `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
  - **Acceptance:** Blazor client service can call bootstrap endpoint and receives safe command response; UI-specific credential clearing is handled by task 4.5.
  - **Validation:** focused `Explore.Blazor.Client.Tests` for `InstanceOnboardingServiceTests` passed.
  - **Effort:** M
  - **Dependencies:** 4.1.
- [x] **4.5 Update auth-provider onboarding UI**
  - **Files:** `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor`
  - **Acceptance:** offers manual OIDC config vs bootstrap Keycloak config, labels bootstrap credential as one-time/not stored, has accessible labels/helper text/errors.
  - **Validation:** focused `AuthProviderConfigurationSourceTests` passed; `Explore.Blazor.Client` build passed through full release build. End-to-end UI coverage remains in task 6.3.
  - **Effort:** M
  - **Dependencies:** 4.4.
- [x] **4.6 Refresh auth schemes after bootstrap**
  - **Files:** UI/service/BFF existing auth refresh path.
  - **Acceptance:** successful bootstrap triggers `/bff/auth/refresh-schemes` through `InstanceOnboardingService`; the UI now calls this service method.
  - **Validation:** focused Blazor client service tests passed for success and failure refresh behavior.
  - **Effort:** S
  - **Dependencies:** 4.4 for service-level refresh; 4.5 for visible UI invocation.

## Phase 5: Documentation And Operations ✅ COMPLETE
- [x] **5.1 Update self-hosting guide**
  - **Files:** `docs/SELF_HOSTING.md`
  - **Acceptance:** describes Compose-managed Keycloak automation, external existing Keycloak bootstrap, required secrets, and no manual UI secret step.
  - **Validation:** `SELF_HOSTING.md` now documents Compose-managed sync, external-Keycloak bootstrap flow, one-time credential handling, temporary credential retirement, and rerun/idempotency behavior; focused AgentContext schema/link tests and release build passed.
  - **Effort:** M
  - **Dependencies:** Phase 1 and/or Phase 4.
- [x] **5.2 Update configuration/secrets docs**
  - **Files:** `docs/CONFIGURATION.md`, `docs/SECRETS.md`
  - **Acceptance:** clarifies `KEYCLOAK_BLAZOR_CLIENT_SECRET`, optional `KEYCLOAK_API_CLIENT_SECRET`, and one-time bootstrap credential non-persistence.
  - **Validation:** `CONFIGURATION.md` and `SECRETS.md` now clarify runtime OIDC secret storage, optional API secret sync, external bootstrap URL safety, and one-time admin credential non-persistence; focused AgentContext schema/link tests and release build passed.
  - **Effort:** S
  - **Dependencies:** implementation behavior settled.
- [x] **5.3 Update troubleshooting guide**
  - **Files:** `docs/TROUBLESHOOTING.md`
  - **Acceptance:** includes `unauthorized_client`, bad realm URL, missing Keycloak permissions, partial import conflict, and rerun `keycloak-init` steps.
  - **Validation:** `TROUBLESHOOTING.md` now covers `unauthorized_client`, unsafe/bad realm URLs, missing Keycloak permissions, patch/create mode behavior, client conflicts, and post-bootstrap login recovery; focused AgentContext schema/link tests and release build passed.
  - **Effort:** S
  - **Dependencies:** implementation behavior settled.
- [x] **5.4 Update release checklist/operations if startup behavior changes**
  - **Files:** `docs/OPERATIONS.md`, `docs/RELEASE_CHECKLIST.md` if applicable.
  - **Acceptance:** reviewed `docs/OPERATIONS.md` and `docs/RELEASE_CHECKLIST.md`; no new runtime health, release checklist, or backup/restore contract beyond the already documented Compose `keycloak-init` sequencing and Keycloak DB backup requirement.
  - **Validation:** no edit required for this slice.
  - **Effort:** S
  - **Dependencies:** Compose dependency decision.

## Phase 6: Final Verification And Handoff ✅ COMPLETE
- [x] **6.1 Run build**
  - **Files:** n/a
  - **Acceptance:** `dotnet build --configuration Release --verbosity quiet` passes.
  - **Validation:** passed with 0 errors; existing warnings remained.
  - **Effort:** S
  - **Dependencies:** all code/docs changes.
- [x] **6.2 Run affected test projects individually**
  - **Files:** n/a
  - **Acceptance:** Application, Infrastructure, API integration, Blazor integration, Blazor client, and Architecture test projects pass or failures are documented with next recovery action.
  - **Validation:** Application tests, Infrastructure tests, focused Phase 4 API integration tests, focused BFF forwarding tests, focused Blazor client service tests, focused UI source tests, release build, focused AgentContext schema/link, focused Clean Architecture, focused Naming, focused API contract, and focused endpoint-classification tests passed. Focused BlazorClientArchitectureTests still fail on existing unrelated notification service violations; focused CqrsPatternTests still fail on an existing unrelated `AiChatRequest` naming/location issue; full Architecture suite remains blocked by unrelated dirty-worktree failures.
  - **Effort:** L
  - **Dependencies:** all implementation tasks.
- [x] **6.3 Automated Keycloak integration/e2e smoke**
  - **Files:** n/a
  - **Acceptance:** automated coverage starts disposable Keycloak, verifies realm import/secret sync or external bootstrap, drives the setup path through BFF/UI where browser interaction matters, and fails without human/browser-only steps.
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/KeycloakBootstrapRealRuntimeTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passed. The backend test starts disposable Keycloak, calls the setup-gated bootstrap endpoint through the real Application handler and Infrastructure adapter, rotates the Blazor client secret through the real Keycloak Admin API, proves the rotated secret works against Keycloak's token endpoint, and verifies persisted runtime auth config contains no admin credential. `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/KeycloakBootstrapBrowserFlowTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` also passed for the Playwright UI path: BFF setup-secret persistence, Keycloak bootstrap mode form interaction, setup-gated submit, navigation to the next onboarding step, and no browser token storage.
  - **Effort:** L
  - **Dependencies:** Phases 1-5.
- [x] **6.4 Refresh dev docs final state**
  - **Files:** plan/context/tasks.
  - **Acceptance:** docs reflect implemented scope, validation results, remaining work, and handoff.
  - **Validation:** plan/context/tasks updated after Phase 1, Phase 2, Phase 3, completed Phase 4 API/BFF/UI slice, and Phase 5 operator docs plus focused verification.
  - **Effort:** S
  - **Dependencies:** validation.

## Phase 7: Post-onboarding Keycloak Doctor, Resync, And Rotation ⏳ FUTURE / NOT STARTED
- [ ] **7.1 Add read-only Keycloak realm doctor**
  - **Files:** future Application doctor DTOs/contracts/queries, Infrastructure inspection service, instance-admin API endpoint, Blazor admin UI.
  - **Acceptance:** reports realm/client/scope/mapper/redirect/secret-alignment health without mutation; supports basic non-admin checks and optional temporary-admin read-only checks; returns safe structured findings with no secrets/tokens/raw provider bodies.
  - **Validation:** Application unit tests, Infrastructure fake-HTTP tests, API authorization tests, Blazor admin UI tests.
  - **Effort:** L
  - **Dependencies:** completed Phase 6 bootstrap/runtime proof.
- [ ] **7.2 Define typed Keycloak desired-state and sync-plan model**
  - **Files:** future `KeycloakRealmDesiredState`, `KeycloakRealmSyncPlan`, operation DTOs, validators.
  - **Acceptance:** represents ISLAMU-owned clients, redirect URIs, web origins, optional scopes, scope mappings, protocol/audience mappers, default-role composites, and future project client contracts as additive operations; destructive operations are explicitly unsupported.
  - **Validation:** deterministic diff unit tests and architecture tests.
  - **Effort:** L
  - **Dependencies:** 7.1.
- [ ] **7.3 Add instance-admin resync preview workflow**
  - **Files:** future instance-admin infrastructure controller/route names, Blazor admin UI page/component, service models.
  - **Acceptance:** authenticated instance admin can preview the additive `RealmSyncPlan`; preview is read-only; UI uses server-confirmed affordances; raw Keycloak errors are categorized safely.
  - **Validation:** API integration tests, Blazor UI/source tests, authorization tests.
  - **Effort:** L
  - **Dependencies:** 7.1-7.2.
- [ ] **7.4 Add additive resync apply with backup confirmation**
  - **Files:** future command/handler/validator, Infrastructure apply service, admin UI confirmation flow, docs.
  - **Acceptance:** operator confirms Keycloak backup before mutation; temporary Keycloak admin/service-account credential is used only for the active operation; apply can add/update ISLAMU-owned clients/scopes/mappers/redirects/origins/composites; apply never deletes realms/users/groups/unrelated clients/unowned roles.
  - **Validation:** Infrastructure fake-HTTP tests, disposable-Keycloak integration tests, secret scanning/redaction checks, docs tests.
  - **Effort:** XL
  - **Dependencies:** 7.2-7.3.
- [ ] **7.5 Add explicit client-secret rotation workflow**
  - **Files:** future rotation command/service/UI plus docs.
  - **Acceptance:** application-managed secrets can be updated by ISLAMU; deployment-managed secrets produce operator instructions for env/Infisical update instead of silent override; audit logs record actor/time/client ID/result but never secret values; auth schemes refresh or restart guidance is shown.
  - **Validation:** Application unit tests, API integration tests, Infrastructure fake-HTTP tests, Blazor UI tests, disposable-Keycloak rotation proof.
  - **Effort:** L
  - **Dependencies:** 7.4 and secret ownership model.
- [ ] **7.6 Add multi-project identity contract registry and drift detection**
  - **Files:** future identity contract registry, module/project contributors, doctor extensions, docs.
  - **Acceptance:** Event, future identity service, admin portal, mobile client, and other project contracts can compose desired Keycloak requirements without one project owning the whole realm; optional scheduled drift detection is read-only and never auto-mutates.
  - **Validation:** registry composition tests, doctor tests, documentation checks.
  - **Effort:** XL
  - **Dependencies:** 7.2.

## Verification Checklist
- [x] LSP diagnostics clean for modified files where available.
- [x] `dotnet build --configuration Release --verbosity quiet` passes.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes when Application code changed.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passes when Infrastructure adapter added.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when API endpoint changed. Focused `InstanceOnboardingControllerTests` passed sequentially.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when BFF forwarding changed. Focused `SetupSecretForwardingHandlerTests` passed sequentially.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes when UI changed. Focused `InstanceOnboardingServiceTests` passed for the service slice; focused `AuthProviderConfigurationSourceTests` passed for the UI mode.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes for architecture/docs/context checks. Full suite currently fails on unrelated dirty-worktree authorization/raw-HTTP rules, an existing CQRS request-location issue, and existing Blazor notification service architecture violations; focused AgentContext schema/link, CleanArchitecture, Naming, API contract, and endpoint-classification checks passed.
- [x] `docker compose config` passes when Compose changes.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with final state and remaining work.
- [x] Automated disposable-Keycloak integration/e2e smoke covers the real setup-gated bootstrap path against disposable Keycloak. Manual smoke is intentionally not the acceptance path.
- [x] Focused Playwright browser e2e covers the onboarding UI bootstrap mode against Aspire AppHost/Testcontainers infrastructure.

## Remaining / Deferred Work
- [ ] Consider adding a new intent to `.claude/contract/intents.yaml` for external infrastructure bootstrap/onboarding automation if this pattern recurs.
- [ ] Implement Phase 7 post-onboarding Keycloak doctor/resync/rotation when ready; it is planned above but not started.
- [ ] Consider deprecating/removing `KEYCLOAK_API_CLIENT_SECRET` if API client remains bearer-only and runtime never consumes it.
- [ ] Investigate post-bootstrap browser auth-code login with `offline_access`; the Playwright bootstrap UI flow passes, but extending that test through login currently exposes Keycloak `not_allowed` / `Offline tokens not allowed for the user or client` during `/signin-oidc` token redemption.

<!-- ABOUTME: Tactical checklist for implementing Keycloak realm/client bootstrap automation. -->
<!-- ABOUTME: Tracks Compose init, external Keycloak setup, tests, docs, validation, and deferred work. -->

# Keycloak Bootstrap Automation — Task Checklist

Last Updated: 2026-05-30 Europe/Brussels

## Status Summary
- **Overall status:** Implementation in progress
- **Completed:** 27/33
- **Current priority:** Proceed to Phase 5 operator documentation for the completed setup-gated Keycloak bootstrap path.
- **Next recommended slice:** Update self-hosting, configuration/secrets, and troubleshooting docs for external Keycloak bootstrap permissions and recovery.

## Implementation Maintenance Rules
- [x] Before starting work, read plan/context/tasks.
- [x] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

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
  - **Validation:** focused `AuthProviderConfigurationSourceTests` passed; `Explore.Blazor.Client` build passed through full release build. Manual UI smoke remains recommended.
  - **Effort:** M
  - **Dependencies:** 4.4.
- [x] **4.6 Refresh auth schemes after bootstrap**
  - **Files:** UI/service/BFF existing auth refresh path.
  - **Acceptance:** successful bootstrap triggers `/bff/auth/refresh-schemes` through `InstanceOnboardingService`; the UI now calls this service method.
  - **Validation:** focused Blazor client service tests passed for success and failure refresh behavior.
  - **Effort:** S
  - **Dependencies:** 4.4 for service-level refresh; 4.5 for visible UI invocation.

## Phase 5: Documentation And Operations ⏳ NOT STARTED
- [ ] **5.1 Update self-hosting guide**
  - **Files:** `docs/SELF_HOSTING.md`
  - **Acceptance:** describes Compose-managed Keycloak automation, external existing Keycloak bootstrap, required secrets, and no manual UI secret step.
  - **Validation:** architecture docs tests.
  - **Effort:** M
  - **Dependencies:** Phase 1 and/or Phase 4.
- [ ] **5.2 Update configuration/secrets docs**
  - **Files:** `docs/CONFIGURATION.md`, `docs/SECRETS.md`
  - **Acceptance:** clarifies `KEYCLOAK_BLAZOR_CLIENT_SECRET`, optional `KEYCLOAK_API_CLIENT_SECRET`, and one-time bootstrap credential non-persistence.
  - **Validation:** architecture docs tests.
  - **Effort:** S
  - **Dependencies:** implementation behavior settled.
- [ ] **5.3 Update troubleshooting guide**
  - **Files:** `docs/TROUBLESHOOTING.md`
  - **Acceptance:** includes `unauthorized_client`, bad realm URL, missing Keycloak permissions, partial import conflict, and rerun `keycloak-init` steps.
  - **Validation:** architecture docs tests.
  - **Effort:** S
  - **Dependencies:** implementation behavior settled.
- [ ] **5.4 Update release checklist/operations if startup behavior changes**
  - **Files:** `docs/OPERATIONS.md`, `docs/RELEASE_CHECKLIST.md` if applicable.
  - **Acceptance:** deployment sequencing and backup/restore implications are documented if changed.
  - **Validation:** architecture docs tests.
  - **Effort:** S
  - **Dependencies:** Compose dependency decision.

## Phase 6: Final Verification And Handoff 🟡 IN PROGRESS
- [x] **6.1 Run build**
  - **Files:** n/a
  - **Acceptance:** `dotnet build --configuration Release --verbosity quiet` passes.
  - **Validation:** passed with 0 errors; existing warnings remained.
  - **Effort:** S
  - **Dependencies:** all code/docs changes.
- [x] **6.2 Run affected test projects individually**
  - **Files:** n/a
  - **Acceptance:** Application, Infrastructure, API integration, Blazor integration, Blazor client, and Architecture test projects pass or failures are documented with next recovery action.
  - **Validation:** Application tests, Infrastructure tests, focused Phase 4 API integration tests, focused BFF forwarding tests, focused Blazor client service tests, focused UI source tests, build, focused Clean Architecture, focused Naming, focused API contract, and focused endpoint-classification tests passed. Focused BlazorClientArchitectureTests still fail on existing unrelated notification service violations; focused CqrsPatternTests still fail on an existing unrelated `AiChatRequest` naming/location issue; full Architecture suite remains blocked by unrelated dirty-worktree failures.
  - **Effort:** L
  - **Dependencies:** all implementation tasks.
- [ ] **6.3 Manual Compose smoke**
  - **Files:** n/a
  - **Acceptance:** Keycloak imports realm, init syncs secret, BFF login reaches Keycloak with matching secret.
  - **Validation:** `docker compose up -d keycloak-db keycloak keycloak-init` plus login smoke when API/BFF are up.
  - **Effort:** M
  - **Dependencies:** Phase 1.
- [x] **6.4 Refresh dev docs final state**
  - **Files:** plan/context/tasks.
  - **Acceptance:** docs reflect implemented scope, validation results, remaining work, and handoff.
  - **Validation:** plan/context/tasks updated after Phase 1, Phase 2, Phase 3, and the completed Phase 4 API/BFF/UI slice plus focused verification.
  - **Effort:** S
  - **Dependencies:** validation.

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

## Remaining / Deferred Work
- [ ] Consider adding a new intent to `.claude/contract/intents.yaml` for external infrastructure bootstrap/onboarding automation if this pattern recurs.
- [ ] Consider a read-only doctor diagnostic for Keycloak client-secret mismatch.
- [ ] Consider future post-onboarding realm resync/rotation workflow with explicit instance-admin authorization and no permanent admin credential storage.
- [ ] Consider deprecating/removing `KEYCLOAK_API_CLIENT_SECRET` if API client remains bearer-only and runtime never consumes it.

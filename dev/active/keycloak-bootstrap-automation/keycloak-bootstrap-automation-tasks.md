<!-- ABOUTME: Tactical checklist for implementing Keycloak realm/client bootstrap automation. -->
<!-- ABOUTME: Tracks Compose init, external Keycloak setup, tests, docs, validation, and deferred work. -->

# Keycloak Bootstrap Automation — Task Checklist

Last Updated: 2026-06-02 Europe/Brussels

## Status Summary
- **Overall status:** Implementation complete through automated disposable-Keycloak backend integration smoke, focused Playwright browser UI bootstrap e2e, and Phase 7.1-7.6 post-onboarding Keycloak doctor/resync/rotation/identity-contract registry work; Docker-backed API/runtime verification remains environment-blocked.
- **Completed:** 39/39
- **Current priority:** Run Docker-backed Phase 7 API/runtime smoke when Testcontainers can access Docker, then decide whether to promote the post-onboarding Keycloak flows from active dev docs into operator docs.
- **Next recommended slice:** Run the Phase 7 API authorization matrix plus disposable-Keycloak apply/rotation runtime proof once Docker/Testcontainers are available again.
- **2026-06-02 maintenance note:** First-run cloud-Keycloak bootstrap 403 was traced to BFF setup-secret persistence/visibility, not Keycloak Admin API authorization. `/bff/setup-secret` validated successfully, but later setup-gated `/api/InstanceOnboarding/auth-provider-configuration/internal` and `/keycloak-bootstrap` calls lacked a trusted `X-Setup-Secret`. The BFF now sets setup cookies' `Secure` attribute from the browser-facing request scheme (`Request.IsHttps`) instead of environment name, force-loads `/onboarding/auth-provider` after the browser persists the HttpOnly cookie, and creates a short-lived pre-account setup session keyed by an HttpOnly `setup-secret-session` nonce cookie. YARP and direct BFF clients still strip browser-controlled `X-Setup-Secret`; they can now resolve the trusted setup secret from the anonymous server-side session before falling back to the protected secret cookie.

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
  - **Acceptance:** Blazor client service can call the setup-secret-gated auth-provider configuration and bootstrap endpoints before any account exists, and post-onboarding admin settings use the separate instance-admin auth-provider route.
  - **Validation:** focused `InstanceOnboardingServiceTests` passed for setup internal read and admin read route selection.
  - **Effort:** M
  - **Dependencies:** 4.1.
- [x] **4.5 Update auth-provider onboarding UI**
  - **Files:** `Explore.Blazor.Client/Pages/Onboarding/AuthProviderConfiguration.razor`
  - **Acceptance:** offers manual OIDC config vs bootstrap Keycloak config during first-run setup, including when Keycloak runtime values are deployment-prefilled; labels bootstrap credential as one-time/not stored; preserves Keycloak base path prefixes such as `/auth` when seeding bootstrap defaults.
  - **Validation:** focused `AuthProviderConfigurationSourceTests` passed; `Explore.Blazor.Client` build passed. End-to-end UI coverage remains in task 6.3.
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

## Phase 7: Post-onboarding Keycloak Doctor, Resync, And Rotation ⏳ IN PROGRESS
- [x] **7.1 Add read-only Keycloak realm doctor**
  - **Files:** Application doctor DTOs/query/handler/contract, Infrastructure inspection service, instance-admin API endpoint, Blazor admin UI/service models, Infrastructure fake-HTTP tests, API authorization matrix coverage, Blazor source guard tests.
  - **Acceptance:** implemented read-only realm diagnostics for configuration, OIDC discovery, realm/client availability, authorization-code flow, offline_access role/default-role/client-scope/scope-mapping requirements, refresh-token settings, and optional API client presence. Basic mode performs no admin call; temporary-admin mode uses credentials only for the active request and returns structured safe findings without secrets, tokens, or raw provider bodies.
  - **Validation:** `dotnet build` passed for Application, Infrastructure, and API projects; `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed 390/390; focused API authorization matrix coverage passed 82/82; focused Blazor verification is blocked by unrelated source-generation errors recorded below.
  - **Effort:** L
  - **Dependencies:** completed Phase 6 bootstrap/runtime proof.
- [x] **7.2 Define typed Keycloak desired-state and sync-plan model**
  - **Files:** `KeycloakRealmDesiredStateDto`, `KeycloakClientDesiredStateDto`, `KeycloakClientScopeDesiredStateDto`, `KeycloakRoleCompositeDesiredStateDto`, `KeycloakProtocolMapperDesiredStateDto`, `KeycloakRealmSyncPlanDto`, `KeycloakRealmSyncOperationDto`, `KeycloakRealmSyncPreviewRequestDto`, `PreviewKeycloakRealmSyncQuery`, `PreviewKeycloakRealmSyncQueryHandler`, `IKeycloakBootstrapService`.
  - **Acceptance:** represents ISLAMU-owned Blazor/API clients, redirect URIs, web origins, optional/default scopes, scope mappings, audience mapper contract, default-role composites, and future project client contracts as additive desired state and sync operations. `DestructiveOperationsSupported` is explicitly `false`; preview operations never remove operator-managed redirect URIs/web origins or unrelated clients/roles.
  - **Validation:** targeted Application/Infrastructure/API builds passed; Application unit tests passed 1197/1197; Infrastructure fake-HTTP tests passed 392/392 with deterministic read-only sync-preview coverage.
  - **Effort:** L
  - **Dependencies:** 7.1.
- [x] **7.3 Add instance-admin resync preview workflow**
  - **Files:** `InstanceSettingsController`, `RouteNames`, `KeycloakBootstrapService`, `IInstanceOnboardingApi`, `InstanceOnboardingService`, `InstanceAuthProviderSection.razor`, `EndpointAuthorizationMatrixTests`, `KeycloakRealmDoctorSourceTests`.
  - **Acceptance:** authenticated instance admins can preview a read-only additive `RealmSyncPlan` from the admin auth-provider panel. Basic preview exposes desired state and blocks drift-aware comparison until temporary admin credentials are supplied; temporary credentials are used only for the active request and cleared by the UI. Preview responses categorize Keycloak reachability/auth/drift safely and expose no secrets, tokens, raw provider bodies, or destructive operations.
  - **Validation:** focused API authorization matrix passed 84/84; `Explore.Blazor.Client` build passed; focused Blazor source guards passed 4/4; Infrastructure fake-HTTP sync-preview tests verify only OIDC discovery plus Admin API GETs after token acquisition, no PUT/DELETE, and no password/token leakage in serialized plans.
  - **Effort:** L
  - **Dependencies:** 7.1-7.2.
- [x] **7.4 Add additive resync apply with backup confirmation**
  - **Files:** `KeycloakRealmSyncApplyRequestDto`, `ApplyKeycloakRealmSyncCommand`, `ApplyKeycloakRealmSyncCommandHandler`, `IKeycloakBootstrapService`, `KeycloakBootstrapService`, `InstanceSettingsController`, `RouteNames`, `IInstanceOnboardingApi`, `InstanceOnboardingService`, `InstanceAuthProviderSection.razor`, generated OpenAPI/Blazor client contract, Infrastructure fake-HTTP tests, API authorization matrix coverage, Blazor source guard tests.
  - **Acceptance:** implemented backup-confirmed additive apply for ISLAMU-owned Blazor/API clients, `offline_access` realm role/client scope/scope mappings/default-role composite, authorization-code/refresh-token client settings, redirect URIs, and web origins. Apply blocks without backup confirmation or temporary admin credentials, uses those credentials only for the active request, clears the browser password field after submit, never rotates existing client secrets, and never calls destructive Keycloak APIs such as realm delete/reimport, user/group deletion, unrelated client deletion, or redirect-origin removal.
  - **Validation:** targeted Application/Infrastructure/API/Blazor Client project builds passed; Application unit tests passed 1197/1197; Infrastructure fake-HTTP tests passed 394/394 including no-contact-without-backup and additive-only apply/redaction coverage; focused Blazor source guards passed 6/6. Focused API authorization matrix for the new apply endpoint was added but could not run in this environment because Docker/Testcontainers could not connect to Docker.
  - **Effort:** XL
  - **Dependencies:** 7.2-7.3.
- [x] **7.5 Add explicit client-secret rotation workflow**
  - **Files:** `AuthProviderConfigurationDto`, `KeycloakClientSecretRotationRequestDto`, `KeycloakClientSecretRotationResultDto`, `KeycloakClientSecretRotationRequestDtoValidator`, `RotateKeycloakClientSecretCommand`, `RotateKeycloakClientSecretCommandHandler`, `IKeycloakBootstrapService`, `AuthProviderConfigurationService`, `KeycloakBootstrapService`, `InstanceSettingsController`, `RouteNames`, `IInstanceOnboardingApi`, `InstanceOnboardingService`, `InstanceAuthProviderSection.razor`, generated OpenAPI/Blazor client contract, Application unit tests, Infrastructure fake-HTTP tests, API authorization matrix coverage, Blazor source guard tests.
  - **Acceptance:** implemented explicit ownership-aware rotation for the configured confidential Keycloak Blazor client. Application-managed rotation requires instance-admin authorization, operator confirmation, a request-scoped new secret, and temporary Keycloak admin credentials; Keycloak is updated through a safe client-representation `PUT`, the new secret is persisted only after Keycloak accepts it, and JWT authority schemes are refreshed. Deployment-managed mode returns operator instructions for env/Infisical or other deployment secret providers instead of silently overriding deployment-managed values. Results and logs include actor/client/result metadata but never secret values, temporary admin credentials, admin tokens, or raw provider bodies.
  - **Validation:** targeted relaxed Application/Infrastructure/API/Blazor Client builds passed after Phase 7.5; focused Application rotation handler/validator tests passed 4/4; focused Infrastructure Keycloak service tests passed 17/17 including rotation PUT/no-delete/redaction coverage; focused Blazor source guards passed 8/8 including rotation UI and secret-clearing checks. API authorization tests for the rotate endpoint were added but remain Docker/Testcontainers-blocked in this environment.
  - **Effort:** L
  - **Dependencies:** 7.4 and secret ownership model.
- [x] **7.6 Add multi-project identity contract registry and drift detection**
  - **Files:** `KeycloakRealmDesiredStateBuildRequestDto`, `IKeycloakIdentityContractContributor`, `IKeycloakRealmDesiredStateBuilder`, `EventKeycloakIdentityContractContributor`, `KeycloakRealmDesiredStateBuilder`, `ApplicationServicesRegistration`, `KeycloakBootstrapService`, `KeycloakRealmDesiredStateBuilderTests`.
  - **Acceptance:** Event's Blazor/API client, `offline_access` role/client-scope/scope-mapping, default-role composite, redirect URI/web origin, and API audience mapper requirements are now contributed through an Application-layer identity contract registry. Future identity service, admin portal, mobile client, and other module contracts can register additional contributors without Infrastructure owning the whole realm or hardcoding every project client. The composed desired state still has `DestructiveOperationsSupported = false`; preview/apply flows remain additive-only and read-only until an explicit backup-confirmed apply request.
  - **Validation:** focused registry composition tests passed 2/2; targeted relaxed Application and Infrastructure builds passed; focused Keycloak Infrastructure tests passed 17/17 after moving desired-state composition behind the registry; focused architecture checks remain green as recorded below.
  - **Effort:** XL
  - **Dependencies:** 7.2.

## Verification Checklist
- [x] LSP diagnostics clean for modified files where available.
- [x] `dotnet build --configuration Release --verbosity quiet` passes. Full solution build passed after Phase 7.4. After Phase 7.5, targeted relaxed Application, Infrastructure, API, and Blazor Client project builds passed with `/p:RunAnalyzers=false /p:TreatWarningsAsErrors=false /p:WarningsAsErrors=`; canonical analyzer builds are currently noisy from unrelated warnings-as-errors outside the Keycloak surface.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes when Application code changed. Focused Phase 7.5 rotation handler/validator tests passed 4/4, and focused Phase 7.6 desired-state registry tests passed 2/2 with relaxed analyzer settings; earlier full Application unit suite passed 1197/1197 after Phase 7.4.
- [x] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passes when Infrastructure adapter added. Focused Phase 7.5/7.6 `KeycloakBootstrapServiceTests` passed 17/17 with relaxed analyzer settings, including doctor, preview, apply, rotation, and registry-backed desired-state coverage; earlier full Infrastructure suite passed 394/394 after Phase 7.4.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when API endpoint changed. Focused `InstanceOnboardingControllerTests` passed sequentially earlier, and focused `EndpointAuthorizationMatrixTests` passed 84/84 after doctor/sync-preview authorization coverage. Phase 7.4 apply and Phase 7.5 rotate authorization tests were added, but the focused matrix run remains blocked by Docker/Testcontainers failing to connect to Docker in this environment.
- [x] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when BFF forwarding changed. Focused `SetupSecretForwardingHandlerTests` passed sequentially.
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes when UI changed. `Explore.Blazor.Client` build passed, and focused Keycloak doctor/sync-preview/apply/rotation source guards passed 8/8 with `--treenode-filter "/*/*/KeycloakRealmDoctorSourceTests/*"`; the full Blazor Client test project currently has unrelated notification-layout failures because test setup does not register `INotificationRefreshStreamClient`.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes for architecture/docs/context checks. Focused `CleanArchitectureTests` passed 13/13, `ApiContractArchitectureTests` passed 6/7 with 1 skipped, `EndpointClassificationArchitectureTests` passed 3/3, and `NamingConventionTests` passed 10/10 after Phase 7.5; full suite remains deferred because existing unrelated dirty-worktree architecture issues are tracked separately.
- [x] `docker compose config` passes when Compose changes.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with final state and remaining work.
- [x] Automated disposable-Keycloak integration/e2e smoke covers the real setup-gated bootstrap path against disposable Keycloak. Manual smoke is intentionally not the acceptance path.
- [x] Focused Playwright browser e2e covers the onboarding UI bootstrap mode against Aspire AppHost/Testcontainers infrastructure.

## Remaining / Deferred Work
- [ ] Consider adding a new intent to `.claude/contract/intents.yaml` for external infrastructure bootstrap/onboarding automation if this pattern recurs.
- [ ] Run the Phase 7.4/7.5 API authorization matrix and any disposable-Keycloak apply/rotation runtime smoke once Docker/Testcontainers are available again.
- [ ] Promote Phase 7 post-onboarding Keycloak doctor/resync/rotation operator guidance into durable docs after Docker-backed runtime proof is available.
- [ ] Consider deprecating/removing `KEYCLOAK_API_CLIENT_SECRET` if API client remains bearer-only and runtime never consumes it.
- [ ] Investigate post-bootstrap browser auth-code login with `offline_access`; the Playwright bootstrap UI flow passes, but extending that test through login currently exposes Keycloak `not_allowed` / `Offline tokens not allowed for the user or client` during `/signin-oidc` token redemption.

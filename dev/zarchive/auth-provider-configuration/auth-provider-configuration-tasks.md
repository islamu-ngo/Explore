# Authentication Provider Configuration — Task Checklist

> Last Updated: 2026-03-04 (Session 14 — integration coverage expansion)

## Phase 0: Requirements Finalization ✅ COMPLETE
- [x] Analyze current auth flow and identify chicken-and-egg problem
- [x] Explore existing domain model (User, UserExternalLogin, Actor, federation entities)
- [x] Explore governance settings infrastructure
- [x] Confirm ATProto split: Login vs Decentralization (2 toggles)
- [x] Create initial plan, context, and tasks documents
- [x] Resolve: backward compatibility — N/A, not released yet
- [x] Resolve: ATProto OAuth technical approach — Full OAuth with DPoP via FishyFlip library
- [x] Resolve: first admin creation — same SyncUser pattern for all providers
- [x] Resolve: credential validation — yes, validate before saving (Keycloak OIDC discovery, Google format)
- [x] Resolve: multi-tenant — same IsLocked pattern as existing governance settings
- [x] Resolve: login page UX — vertical buttons + ATProto handle input field
- [x] Resolve: account linking — all providers bind to same account, email auto-match + explicit ATProto linking
- [x] Resolve: ATProto client metadata — pre-fill URL from request, warn on private URLs, don't hard-disable
- [x] Review: info icon text and confirmation dialog copy — approved
- [x] Finalize plan after all questions resolved

## Phase 1: Domain & Application — Auth Provider Settings ✅ COMPLETE
- [x] Add `Authentication` group to `GovernanceSettingKeys` (incl. `AtprotoPublicUrl`)
- [x] Add `Federation` group to `GovernanceSettingKeys`
- [x] Add `InfrastructureSecretSettingKeys` entries for auth secrets
- [x] Create `AuthProviderConfigurationDto` + `AuthProviderConfigurationDtoValidator`
- [x] Create `IAuthProviderConfigurationService` + `AuthProviderConfigurationService`
- [x] Create `SaveAuthProviderConfigurationCommand` + handler (setup-token-protected)
- [x] Create `GetAuthProviderConfigurationQuery` + handler
- [x] Add validation: at least one provider must be enabled
- [x] Add API endpoints to `InstanceOnboardingController` (GET/PUT auth config, GET configured status)
- [x] Register `IAuthProviderConfigurationService` in DI (`ApplicationServicesRegistration.cs`)
- [x] Build verified: 0 errors, 9 pre-existing NuGet warnings
- [x] Tests verified: 335 app + 32 arch + 79 domain + 190 secrets = all pass (2 pre-existing flaky bUnit tests unrelated)
- [ ] Unit tests for command/query handlers (deferred to Phase 8)
- [ ] Add credential validation logic — Keycloak connectivity test, Google client ID format (deferred)
## Phase 2: Infrastructure — Dynamic Auth Scheme Registration ✅ COMPLETE
- [x] Refactor `AuthenticationExtensions` — Cookie-only default, dynamic scheme registration at startup
- [x] Create `IDynamicAuthSchemeManager` interface
- [x] Implement `DynamicAuthSchemeManager` using `IAuthenticationSchemeProvider` + `IOptionsMonitorCache`
- [x] Implement Keycloak OIDC dynamic registration
- [x] Implement Google OAuth dynamic registration
- [x] Implement ATProto OAuth handler stub (`AuthenticationHandler<T>`, returns NoResult/501)
- [x] Read auth config from API at startup → register enabled schemes
- [x] Register schemes immediately after setup-time save (via `/bff/auth/refresh-schemes`)
- [x] Handle env-var-configured Keycloak as auto-enabled provider
- [x] Create `AuthSchemeNames` constants (Domain + BFF local mirror)
- [x] Create `AuthProviderConfigurationResponse` model in BFF for API deserialization
- [x] Add internal API endpoint (`auth-provider-configuration/internal`) for BFF to read config with secrets
- [x] Multi-provider BFF endpoints: `/auth/challenge?provider=`, `/auth/providers`, `/bff/auth/refresh-schemes`
- [x] Fix BFF architecture: no `Explore.Application`/`Explore.Domain` references (HTTP calls instead)
- [x] Build verified: 0 errors
- [x] Tests verified: 335 app + 32 arch + 79 domain + 190 secrets + 516 blazor = all pass (2 pre-existing flaky bUnit tests unrelated)
- [ ] Unit tests for dynamic scheme manager (deferred to Phase 8)

## Phase 3: Blazor UI — Auth Provider Configuration Page ✅ COMPLETE
- [x] Create `AuthProviderConfiguration.razor` component/page
- [x] Route: accessible after setup token validation, before login
- [x] Keycloak toggle with conditional credential inputs
- [x] ATProto Login toggle with info icon + public URL input (pre-filled, warn on private)
- [x] Google SSO toggle with conditional credential inputs
- [x] "At least 1 required" validation with visual feedback
- [x] Info icon tooltips with provider explanations
- [x] Save button → API → dynamic registration
- [x] Auto-detect Keycloak from env vars and pre-fill
- [x] Integration with setup flow (Setup → AuthConfig → Login → Onboarding)

## Phase 4: Instance Onboarding — Decentralization Toggle ✅ COMPLETE
- [x] Add `DecentralizationEnabled` + `LockDecentralizationEnabled` to `InstanceGovernanceSettingsDto`
- [x] Add `DecentralizationEnabled` + `LockDecentralizationEnabled` to `InstanceGovernanceSettingsModel` (client)
- [x] Add read/write for decentralization in `InstanceGovernanceSettingService`
- [x] Add read/write for decentralization in `InstanceGovernanceSettingHelpers`
- [x] Add Federation section to `InstanceOnboarding.razor` with decentralization toggle
- [x] ATProto Decentralization toggle (disabled if ATProto Login not enabled)
- [x] Confirmation dialog (MudDialog) with warning text about public data
- [x] Info icon tooltip explaining public data implications
- [x] Lock toggle for tenant override control
- [x] Decentralization status in Review & Complete step
- [x] Load auth provider config in OnInitializedAsync to check ATProto Login status
- [x] Build verified: 0 errors

## Phase 5: Login Page — Multi-Provider Support ✅ COMPLETE
- [x] Update login page to query enabled providers (`GET /auth/providers`)
- [x] Replace `/login` redirect screen with multi-provider UI (`LoginRedirect.razor`)
- [x] Vertical stack layout: provider buttons + ATProto handle input section
- [x] Keycloak login → `/auth/challenge?provider=keycloak&returnUrl=...`
- [x] Google login → `/auth/challenge?provider=google&returnUrl=...`
- [x] ATProto login → handle input + `/auth/challenge?provider=atproto&returnUrl=...&login_hint=...`
- [x] Handle provider-specific challenge routing in UI (button vs handle_input)
- [x] If only one button provider enabled, auto-redirect directly to that provider challenge
- [x] If only ATProto provider is enabled and `login_hint` exists, auto-redirect using ATProto flow
- [x] Show Keycloak first with "Recommended" badge when backend marks it recommended
- [x] Build verified: 0 errors (pre-existing warnings remain)

## Phase 6: User Sync & Account Linking — Multi-Provider Support 🟡 IN PROGRESS
- [x] Refactor `SyncUserCommandHandler` for provider-agnostic sync
- [x] Keycloak sync (existing behavior, extracted)
- [x] Google sync (new: map Google claims to User + Actor)
- [x] ATProto sync (new: map DID to User + Actor with self-sovereign DID)
- [x] Link external logins via `UserExternalLogin` entity
- [x] Auto-match accounts by verified email (Keycloak ↔ Google)
- [x] Add non-GUID provider-subject resolution for user profile/admin-authority/update/delete controller flows
- [x] Add internal user-id claim propagation for non-GUID subjects in infrastructure identity services (`AdminClaimsTransformation`, `CurrentUserService`, `AdminContext`, `UserContext`) and Blazor `GroupAdminRouteGuard`
- [x] Explicit ATProto linking via Account Settings page
- [x] Safety: cannot unlink last remaining provider
- [ ] Unit tests for each provider sync path
- [x] Build verified: 0 errors (warnings remain)
- [x] `Event.Application.UnitTests` baseline currently green in this branch (345/345)
- [ ] Existing unrelated suite failures observed: `Event.API.IntegrationTests` (2), `Explore.Blazor.Client.Tests` (3)

## Phase 7: Admin Settings — Post-Onboarding Management ✅ COMPLETE
- [x] Add auth provider section to instance admin settings page
- [x] Safety check: cannot disable sole provider for current admin
- [x] Allow adding/modifying providers after onboarding
- [x] Update credentials without disabling provider
- [x] Add admin endpoint: `PUT /api/InstanceOnboarding/admin/auth-provider-configuration`
- [x] Add app-layer command/handler for post-onboarding updates
- [x] Add unit tests for new handler (`UpdateAuthProviderConfigurationCommandHandlerTests`)

## Phase 8: Testing & Documentation 🟡 IN PROGRESS
- [x] Architecture tests for new governance setting keys
- [ ] Integration tests: setup → auth config → login → onboarding flow
- [ ] Integration tests: dynamic scheme registration lifecycle
- [x] Integration tests: admin provider management
- [x] Integration tests: account linking (email auto-match, explicit ATProto)
- [x] Update `docs/SECURITY.md` with multi-provider auth model
- [x] Update `docs/CONFIGURATION.md` with new auth config keys
- [x] Update `docs/FEDERATION.md` with decentralization toggle

### Session 9 notes
- [x] Fixed API integration isolation in `AuthenticatedWebApplicationFactory` by using a unique in-memory DB name per factory instance
- [x] Re-ran `Event.API.IntegrationTests` and reduced failures to 2 known unrelated baseline smoke tests

### Session 10 notes
- [x] Fixed smoke-test authorization classification in `ApiEndpointSmokeTests` by treating `SetupSecretRequiredAttribute` as protected
- [x] Re-ran `Event.API.IntegrationTests` with full pass (403/403)

### Session 11 notes
- [x] Re-ran `Event.Persistence.IntegrationTests` after Docker startup with full pass (2/2)

### Session 12 notes
- [x] Updated `docs/FEDERATION.md` to reflect shipped decentralization governance toggle and ATProto-login dependency while keeping protocol bridge work as roadmap

### Session 13 notes
- [x] Added architecture coverage for auth/federation governance keys in `Event.Architecture.Tests/GovernanceSettingKeysTests.cs`
- [x] Verified `Event.Architecture.Tests` full pass (36/36)

### Session 14 notes
- [x] Expanded onboarding API integration coverage in `InstanceOnboardingControllerTests`:
  - setup-secret save of auth config
  - configured status endpoint assertion
  - completion transition assertion with post-completion public-read lockout
  - internal auth-config secret access behavior (`/auth-provider-configuration/internal`)
- [x] Expanded provider-sync/account-linking integration coverage in `UserExternalLoginIntegrationTests`:
  - Google verified-email auto-match links to existing local user
  - ATProto sync blocked when no explicit link exists
  - ATProto sync succeeds when explicit DID link already exists
- [x] Re-ran `Event.API.IntegrationTests` with full pass (411/411)

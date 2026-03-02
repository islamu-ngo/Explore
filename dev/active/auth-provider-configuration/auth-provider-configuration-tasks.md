# Authentication Provider Configuration — Task Checklist

> Last Updated: 2026-03-02 (Session 4 — Phase 2 verified complete)

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
- [ ] Add credential validation logic — Keycloak connectivity test, Google client ID format (deferred to Phase 2)
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

## Phase 3: Blazor UI — Auth Provider Configuration Page ⏳ NOT STARTED
- [ ] Create `AuthProviderConfiguration.razor` component/page
- [ ] Route: accessible after setup token validation, before login
- [ ] Keycloak toggle with conditional credential inputs
- [ ] ATProto Login toggle with info icon + public URL input (pre-filled, warn on private)
- [ ] Google SSO toggle with conditional credential inputs
- [ ] "At least 1 required" validation with visual feedback
- [ ] Info icon tooltips with provider explanations
- [ ] Save button → API → dynamic registration
- [ ] Auto-detect Keycloak from env vars and pre-fill
- [ ] Integration with setup flow (Setup → AuthConfig → Login → Onboarding)

## Phase 4: Instance Onboarding — Decentralization Toggle ⏳ NOT STARTED
- [ ] Add decentralization section to `InstanceOnboarding.razor`
- [ ] ATProto Decentralization toggle (disabled if ATProto Login not enabled)
- [ ] Confirmation dialog with warning text
- [ ] Info icon explaining public data implications
- [ ] Save setting via existing governance settings flow

## Phase 5: Login Page — Multi-Provider Support ⨼ NOT STARTED
- [ ] Update login page to query enabled providers
- [ ] Vertical stack layout: Keycloak button, Google button, ATProto handle input
- [ ] Keycloak login → existing OIDC flow
- [ ] Google login → Google OIDC flow
- [ ] ATProto login → handle input → ATProto OAuth flow (resolve DID → PDS → AuthServer)
- [ ] Handle provider-specific callbacks
- [ ] If only 1 provider enabled, skip multi-provider UI
- [ ] If Keycloak env vars present, show Keycloak first with "Recommended" badge

## Phase 6: User Sync & Account Linking — Multi-Provider Support ⨼ NOT STARTED
- [ ] Refactor `SyncUserCommandHandler` for provider-agnostic sync
- [ ] Keycloak sync (existing behavior, extracted)
- [ ] Google sync (new: map Google claims to User + Actor)
- [ ] ATProto sync (new: map DID to User + Actor with self-sovereign DID)
- [ ] Link external logins via `UserExternalLogin` entity
- [ ] Auto-match accounts by verified email (Keycloak ↔ Google)
- [ ] Explicit ATProto linking via Account Settings page
- [ ] Safety: cannot unlink last remaining provider
- [ ] Unit tests for each provider sync path

## Phase 7: Admin Settings — Post-Onboarding Management ⏳ NOT STARTED
- [ ] Add auth provider section to instance admin settings page
- [ ] Safety check: cannot disable sole provider for current admin
- [ ] Allow adding/modifying providers after onboarding
- [ ] Update credentials without disabling provider

## Phase 8: Testing & Documentation ⨼ NOT STARTED
- [ ] Architecture tests for new governance setting keys
- [ ] Integration tests: setup → auth config → login → onboarding flow
- [ ] Integration tests: dynamic scheme registration lifecycle
- [ ] Integration tests: admin provider management
- [ ] Integration tests: account linking (email auto-match, explicit ATProto)
- [ ] Update `docs/SECURITY.md` with multi-provider auth model
- [ ] Update `docs/CONFIGURATION.md` with new auth config keys
- [ ] Update `docs/FEDERATION.md` with decentralization toggle

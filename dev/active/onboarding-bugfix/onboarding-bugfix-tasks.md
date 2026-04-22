# Onboarding Bugfix — Task Checklist

## Phase 1: Root Cause Investigation ✅ COMPLETE

- [x] Read active task folder + related docs
- [x] Investigate tenant resolution during onboarding (ApiTenantResolutionMiddleware + DeploymentModeProvider)
- [x] Investigate JWT signature validation failure (IDX10500 — static JwtBearerOptions)
- [x] Investigate token refresh 'Token is not active' error (TokenRefreshCookieEvents)
- [x] Trace instance onboarding submit flow end-to-end
- [x] Launch background explore agents for tenant resolution + JWT config
- [x] Confirm root causes with user (all three approved)

## Phase 2: Implementation ✅ COMPLETE

- [x] Bug 1: DeploymentModeProvider — add explicit config Layer 1 + pre-onboarding SingleTenant fallback
- [x] Bug 2: Create IJwtAuthorityRefreshNotifier contract in Application
- [x] Bug 2: Implement DynamicJwtConfigurationService + IDisposable singleton in API
- [x] Bug 2: Implement DynamicJwtBearerPostConfigureOptions (IPostConfigureOptions)
- [x] Bug 2: Wire ConfigurationManager into JwtBearerOptions via AuthenticationExtensions
- [x] Bug 2: Call ReloadAsync from CompleteInstanceOnboarding handler
- [x] Bug 2: Call ReloadAsync from SaveAuthProviderConfiguration handler
- [x] Bug 2: Call ReloadAsync from UpdateAuthProviderConfiguration handler
- [x] Bug 2: Fix CA1001 (add IDisposable to DynamicJwtConfigurationService)
- [x] Bug 2: Fix UpdateAuthProviderConfigurationCommandHandlerTests (add mock notifier)
- [x] Bug 3: RefreshResult struct + ParseOidcErrorCode + RejectAndSignOutAsync + IsHtmlNavigation
- [x] Bug 4: SetupSecretForwardingHandler — add ExtractUserIdFromAuthorizationHeader for circuit context
- [x] Bug 5: InstanceOnboarding.razor — replace dead sessionStorage sync with JS interop syncSetupSecret
- [x] Bug 5: InstanceOnboardingTests.cs — add JS interop mock for bff.js module

## Phase 3: Testing ✅ COMPLETE

- [x] Build: 0 errors, all warnings pre-existing
- [x] Event.Application.UnitTests: 840/840
- [x] Event.Domain.UnitTests: 207/207
- [x] Event.Architecture.Tests: 90/90
- [x] Explore.Secrets.UnitTests: 201/201
- [x] Event.Persistence.IntegrationTests: 58/58
- [x] Event.API.IntegrationTests: 563/564 (1 pre-existing baseline)
- [x] Explore.Blazor.IntegrationTests: 23/23
- [x] Explore.Blazor.Client.Tests: 794/795 → 795/796 (1 pre-existing MudBlazor skip, new test added)
- [x] Fix Bug 1 regression: 6 API integration tests that set MultiTenant in config — added explicit config Layer 1

## Phase 4: Verification ⏳ PENDING

- [ ] Visual end-to-end test: rebuild Aspire AppHost, fresh run, walk through complete onboarding flow
- [ ] Verify: no "Tenant not resolved" 404s during onboarding page load
- [ ] Verify: JWT validation works after onboarding saves Keycloak config
- [ ] Verify: invalid_grant from Keycloak → user redirected to /login?session=expired (not infinite loop)
- [ ] Verify: SetupSecretForwardingHandler extracts userId from JWT in circuit context
- [ ] Verify: InstanceOnboarding.razor syncs setup-secret via JS interop to SetupSecretSessionService
- [ ] Verify: Complete instance onboarding succeeds (POST /api/InstanceOnboarding/complete with X-Setup-Secret header)
- [ ] Verify: DeploymentModeProvider explicit config Layer 1 works (test fixtures with MultiTenant config still pass)

## Phase 5: Optional Enhancements ⏳ NOT STARTED

- [ ] Add DeploymentModeProvider unit tests for bootstrap edge cases (null, incomplete, completed single/multi, corrupted string)
- [ ] Add DynamicJwtConfigurationService unit tests (env-based startup, DB reload, fallback)
- [ ] Add TokenRefreshCookieEvents unit tests for RejectAndSignOutAsync + ParseOidcErrorCode
- [ ] Add SetupSecretForwardingHandler unit tests for JWT userId extraction circuit context fallback
- [ ] Consider: Replace `CreateBffSelfClient()` in InstanceOnboarding.razor with JS interop for consistency
- [ ] Consider: API endpoint for manual JWT authority reload (ops/diagnostic tool)
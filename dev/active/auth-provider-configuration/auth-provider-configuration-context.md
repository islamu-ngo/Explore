# Authentication Provider Configuration — Context

> Last Updated: 2026-03-04 (Session 14 — integration coverage expansion)

## SESSION PROGRESS (2026-03-04 — Session 14)

### ✅ COMPLETED
- Phase 0: All requirements finalized, plan documents created
- Phase 1: Domain, Application, API layer — all files created, build+tests verified
- Phase 2: Dynamic Auth Scheme Registration — all files created, build+tests verified
- Phase 3: Auth Provider Configuration Blazor page — created, build verified
- Phase 4: Decentralization Toggle — backend (DTO, service, helpers) + UI (InstanceOnboarding.razor) complete, build verified
  - Added `DecentralizationEnabled` + `LockDecentralizationEnabled` to DTO and client model
  - Added read/write in `InstanceGovernanceSettingService` and `InstanceGovernanceSettingHelpers`
  - Added Federation section to InstanceOnboarding.razor: toggle, confirmation dialog, info tooltip, lock switch
  - Added decentralization status to Review & Complete step
  - Loads auth provider config in OnInitializedAsync to gate toggle on ATProto Login being enabled
- Phase 5: Login Page Multi-Provider Support — complete, build verified
  - Replaced `/login` loading-redirect page with provider selection UI in `LoginRedirect.razor`
  - Login page now fetches enabled providers from `GET /auth/providers`
  - Supports provider-specific flows:
    - Keycloak/Google button challenge links
    - ATProto handle input with `login_hint` forwarding
  - Added single-provider auto-redirect behavior:
    - auto-redirect for one button provider
    - auto-redirect for sole ATProto provider when `login_hint` is present
  - Keycloak recommended badge is shown when backend marks it recommended
  - Fixed misplaced ATProto config-load block in `InstanceOnboarding.razor` that caused compile errors

### 🟡 IN PROGRESS
- Phase 6: User Sync & Account Linking (follow-up integration coverage pending)
  - `SyncUserCommandHandler` refactored for provider-aware sync and linking.
  - Provider support implemented for Keycloak, Google, and ATProto (including DID propagation for ATProto actor identity).
  - External login linking now ensured through `UserExternalLogin` creation path with duplicate-link protection.
  - Email auto-match linking implemented for verified Keycloak/Google identities.
  - ATProto without email now requires pre-existing explicit link (guarded path).
  - `UserController` now resolves current user from provider identity when `sub` is non-GUID via a new MediatR query:
    - `ResolveCurrentUserIdByIdentityRequest`
    - `ResolveCurrentUserIdByIdentityRequestHandler`
  - Infrastructure and Blazor claim-parsing paths now consume `internal_user_id` first:
    - `AdminClaimsTransformation` resolves non-GUID subjects through `UserExternalLogin` (+ verified email fallback for keycloak/google) and stamps `internal_user_id`
    - `CurrentUserService`, `AdminContext`, `UserContext`, and `GroupAdminRouteGuard` now read `internal_user_id` before `sub/nameidentifier/sid`
  - `DeleteUserExternalLoginCommandHandler` now blocks unlinking the last remaining provider (`BadRequestException`)
  - `SettingsSecurity.razor` now includes explicit ATProto DID linking UI for the current account (loads linked providers + posts `/api/userexternallogin` link request)
  - Added tests for unlink safety: `DeleteUserExternalLoginCommandHandlerTests`

- Phase 7: Admin Settings — post-onboarding auth provider management
  - Added admin-protected update endpoint: `PUT /api/InstanceOnboarding/admin/auth-provider-configuration`
  - New command + handler:
    - `UpdateAuthProviderConfigurationCommand`
    - `UpdateAuthProviderConfigurationCommandHandler`
  - Added lockout safeguard in handler:
    - blocks disabling all currently linked providers for the current instance admin
  - Added Blazor admin section + wiring:
    - `InstanceAuthProviderSection.razor`
    - nav + section integration in `InstanceAdminSettingsLayout.razor`
    - service method `UpdateAuthProviderConfigurationAsAdminAsync` in `InstanceOnboardingService`
  - Controller user-id resolution now prefers `internal_user_id` claim (then `sub/nameidentifier/sid`)
  - Integration test isolation fix applied for API integration suite:
    - `AuthenticatedWebApplicationFactory` now uses a unique in-memory database name per factory instance
    - Removes cross-test state leakage in `Event.API.IntegrationTests`
  - Fixed API smoke auth classification:
    - `ApiEndpointSmokeTests.IsProtected(...)` now treats `SetupSecretRequiredAttribute` endpoints as protected
    - This aligns smoke expectations with setup-secret-gated onboarding endpoints (which can legitimately return `403`/`410` without setup token)
  - Re-verified `Event.API.IntegrationTests` after smoke fix: full green (403/403)
  - Updated `docs/FEDERATION.md` to document shipped decentralization governance controls and ATProto-login dependency, while retaining protocol bridge items as roadmap.
  - Added architecture tests for new governance key coverage:
    - `Event.Architecture.Tests/GovernanceSettingKeysTests.cs`
    - validates `auth.*` and `federation.*` key prefixes
    - validates flat alias mappings and secret-key mappings for auth providers
  - Expanded onboarding/auth-config API integration flow coverage in `InstanceOnboardingControllerTests`:
    - setup-token protected save of auth provider configuration
    - `auth-provider-configured` transition assertion after save
    - onboarding completion transition and post-completion anonymous lockout for public auth-config endpoint
    - internal auth-config endpoint behavior (`/auth-provider-configuration/internal`) with and without setup secret
  - Expanded provider-sync/account-linking integration coverage in `UserExternalLoginIntegrationTests`:
    - Google verified-email auto-match links to existing local user via `/api/user/sync`
    - ATProto sync without email fails when explicit DID link is missing
    - ATProto sync without email succeeds when explicit DID link exists
  - Re-verified `Event.API.IntegrationTests` after new tests: full green (411/411)

### ⚠️ BLOCKERS
- Branch baseline remains noisy with many pre-existing warnings and unrelated test-suite instability.
- Verified in this session (green):
  - `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity minimal` (345/345 passed)
  - `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` (79/79 passed)
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` (32/32 passed)
  - `dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet` (190/190 passed)
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet` (403/403 passed)
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` (411/411 passed)
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` (2/2 passed)
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` (36/36 passed)
  - `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (515/518 passed, 3 known unrelated flaky/UI timing failures)
## Key Design Decisions

1. **ATProto is two toggles, not one**:
   - ATProto Login = authentication method (before instance onboarding)
   - ATProto Decentralization = data distribution to PDS (during instance onboarding)
   - Decentralization requires Login enabled, but not vice versa

2. **Auth provider config happens BEFORE authentication**:
   - Protected by setup token only (not `[Authorize]`)
   - New step between `/setup` (token validation) and `/auth/login`
   - After saving, auth schemes become available for login

3. **Keycloak can be completely absent**:
   - No env vars needed if admin chooses ATProto or Google
   - Authorization layer unaffected (DB RBAC + Cerbos work independently)
   - Enables lighter self-hosting

4. **Dynamic auth scheme registration**:
   - App starts with Cookie auth only
   - Reads DB for configured providers → registers their schemes
   - During first-run: save config → register immediately (no restart)

5. **Credential validation before saving**:
   - Keycloak: fetch OIDC discovery endpoint, fail with user-friendly error if unreachable
   - Google: validate client ID format (Google discovery always reachable)
   - ATProto: no credentials to validate (zero-config toggle)

6. **Multi-tenant: same IsLocked pattern**:
   - Instance admin sets defaults; tenants override if not locked
   - Credential secrets are instance-level only (tenants can't provide own OAuth creds)

7. **Login page: vertical stack + ATProto handle input**:
   - Keycloak and Google are buttons
   - ATProto is a handle text input + sign-in button (protocol, not Bluesky-bound)
   - If Keycloak env vars present → Keycloak first with "Recommended" badge
   - If only 1 provider → skip multi-provider UI, go directly to that flow

8. **Account linking: all providers bind to same account**:
   - Auto-match by verified email (Keycloak ↔ Google, bidirectional)
   - Explicit linking for ATProto (DID-based, no email guarantee) via Account Settings
   - Cannot unlink last remaining provider

9. **ATProto client metadata: pre-fill + warn**:
   - Pre-fill public URL from browser request during setup
   - Detect private/local URLs and warn (don't hard-disable — tunneling exists)
   - Persist confirmed URL in governance settings
   - Serve `/.well-known/atproto-oauth-client-metadata.json` at runtime
## Key Files

### Current Auth Flow
- **`Explore.Blazor/Extensions/AuthenticationExtensions.cs`** — Cookie + OIDC setup. MUST be refactored to support conditional/dynamic registration.
- **`Explore.Blazor.Client/Pages/Setup.razor`** — Setup token entry. "Continue to Login" link needs to route to auth provider config first (if not yet configured).
- **`Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`** — Instance settings wizard with federation decentralization controls.
- **`Explore.Blazor.Client/Pages/Auth/LoginRedirect.razor`** — Multi-provider login selection UI (button + handle input modes).
- **`Explore.Blazor/Extensions/BffEndpointExtensions.cs`** — BFF `/auth/providers` contract and `/auth/challenge?provider=` behavior.

### Domain Model (Existing, Relevant)
- **`Explore.Domain/User.cs`** — `AuthProvider`, `AuthProviderId`, `ActorId` fields
- **`Explore.Domain/UserExternalLogin.cs`** — Multi-provider login storage
- **`Explore.Domain/Actor.cs`** — Federation identity with DID, Handle, DidCustodyType
- **`Explore.Domain/Constants/GovernanceSettingKeys.cs`** — Where new `auth.*` and `federation.*` keys go

### Application Layer (Existing, Relevant)
- **`Explore.Application/Services/InstanceGovernanceSettingService.cs`** — Pattern for reading/writing governance settings
- **`Explore.Application/Features/InstanceOnboarding/`** — Existing onboarding command/query handlers
- **`Explore.Application/Features/Users/Handlers/Commands/SyncUserCommandHandler.cs`** — User sync from Keycloak. Needs multi-provider support.

### Infrastructure (Existing, Relevant)
- **`Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`** — Shows runtime-switchable provider pattern
- **`Explore.Infrastructure/Identity/UserContext.cs`** — Claim extraction. Needs multi-provider claim mapping.

## Interface Signatures (Core Domain Logic)

### New Governance Setting Keys (Planned)
```csharp
public static class Authentication
{
    public const string KeycloakEnabled = "auth.keycloak_enabled";
    public const string KeycloakAuthority = "auth.keycloak_authority";
    public const string KeycloakClientId = "auth.keycloak_client_id";
    public const string AtprotoLoginEnabled = "auth.atproto_login_enabled";
    public const string AtprotoPublicUrl = "auth.atproto_public_url";
    public const string GoogleSsoEnabled = "auth.google_sso_enabled";
    public const string GoogleClientId = "auth.google_client_id";
}

public static class Federation
    {
    public const string DecentralizationEnabled = "federation.decentralization_enabled";
}

### New DTO (Planned)
```csharp
public class AuthProviderConfigurationDto
{
    public bool KeycloakEnabled { get; set; }
    public string KeycloakAuthority { get; set; } = string.Empty;
    public string KeycloakClientId { get; set; } = string.Empty;
    public string KeycloakClientSecret { get; set; } = string.Empty; // write-only, not returned on read
    public bool AtprotoLoginEnabled { get; set; }
    public string AtprotoPublicUrl { get; set; } = string.Empty; // confirmed public URL for client metadata
    public bool GoogleSsoEnabled { get; set; }
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty; // write-only, not returned on read
}
```

## Quick Resume
1. Read this file for current state
2. Check plan file for overall strategy and all resolved design questions
3. Check tasks file for remaining work
4. Phase 7 implementation is complete in code; expand integration coverage in Phase 8.
5. Phase 8: Testing & Documentation

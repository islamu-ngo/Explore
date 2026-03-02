# Authentication Provider Configuration — Context

> Last Updated: 2026-03-02 (Session 4 — Phase 2 verified complete)

## SESSION PROGRESS (2026-03-02 — Session 4)

### ✅ COMPLETED
- Phase 0: All requirements finalized, plan documents created
- Phase 1: Domain, Application, API layer — all files created, build+tests verified
- Phase 2: Dynamic Auth Scheme Registration — all files created, build+tests verified
  - Fixed BFF architecture violation: `DynamicAuthSchemeManager` now uses HTTP client to call API instead of directly referencing `Explore.Application`/`Explore.Domain`
  - Created `Explore.Blazor/Constants/AuthSchemeNames.cs` (BFF-local mirror of Domain constants)
  - Created `Explore.Blazor/Models/AuthProviderConfigurationResponse.cs` for API deserialization
  - Added internal API endpoint `GET auth-provider-configuration/internal` (setup-secret-protected, returns secrets)
  - `DynamicAuthSchemeManager` reads config from env vars at startup, from API on refresh
  - `RefreshSchemesAsync` accepts optional setup secret to call internal endpoint with credentials
  - Multi-provider BFF endpoints: `/auth/challenge?provider=`, `/auth/providers`, `/bff/auth/refresh-schemes`
  - `AuthenticationExtensions` registers Cookie-only + singleton `DynamicAuthSchemeManager`
  - ATProto handler stub returns NoResult for auth, 501 for challenge (placeholder for FishyFlip)

### 🟡 IN PROGRESS
- None — ready to begin Phase 3 (Blazor UI for auth provider configuration page)

### ⚠️ BLOCKERS
- None
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
- **`Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`** — Instance settings wizard. Needs new decentralization toggle.
- **`Explore.Blazor/Extensions/BffEndpointExtensions.cs`** — BFF endpoints including `/auth/*` and `/bff/setup-secret`. Login endpoints need multi-provider support.

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
4. **Next step**: Review remaining requirements questions (info icon copy, edge cases)
5. If all questions resolved → finalize plan and begin Phase 1 implementation

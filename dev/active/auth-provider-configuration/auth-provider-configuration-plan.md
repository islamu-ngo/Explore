# Authentication Provider Configuration — Implementation Plan

> Last Updated: 2026-03-01 (Session 2)

## Executive Summary

Add instance-level authentication provider configuration so self-hosters can choose which identity providers to enable (Keycloak, ATProto Login, Google SSO) without requiring Keycloak as a hard dependency. This removes the heavyweight Keycloak requirement for lightweight deployments and introduces ATProto decentralization as a separate, opt-in instance setting.

Currently, Keycloak is configured at startup via environment variables and is the only authentication path. This feature makes all auth providers optional and configurable during instance onboarding — before the admin authenticates for the first time.

## Current State Analysis

### Authentication Flow (Today)
1. App starts → `AuthenticationExtensions.AddBffAuthentication()` reads `Keycloak:*` from config
2. OIDC middleware is registered at startup with Keycloak authority/client
3. User navigates to `/setup` → enters setup token → clicks "Continue to Login"
4. Redirected to Keycloak → authenticates → returns to `/setup` with cookie
5. Proceeds to `/onboarding/instance` (requires `[Authorize]`) → configures instance settings
6. `CompleteInstanceOnboardingCommandHandler` marks bootstrap complete, assigns InstanceAdmin role

### Key Files
- `Explore.Blazor/Extensions/AuthenticationExtensions.cs` — Hardcoded Cookie + OIDC (Keycloak) setup
- `Explore.Blazor.Client/Pages/Setup.razor` — Setup token entry, routes to login then onboarding
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor` — Instance settings wizard (4 steps)
- `Explore.Domain/User.cs` — Has `AuthProvider` and `AuthProviderId` fields (already supports multi-provider concept)
- `Explore.Domain/UserExternalLogin.cs` — External login entity (Provider, ProviderKey, ProviderDisplayName)
- `Explore.Domain/Constants/GovernanceSettingKeys.cs` — All governance setting key constants
- `Explore.Application/Services/InstanceGovernanceSettingService.cs` — Reads/writes instance governance settings
- `Explore.Blazor.Client/Services/InstanceOnboardingService.cs` — Client service + models

### What Already Exists for Multi-Provider
- `User.AuthProvider` / `User.AuthProviderId` — ready for multi-provider
- `UserExternalLogin` entity — designed for multiple OAuth providers per user
- `RuntimeAuthorizationProvider` — shows runtime-switchable provider pattern (authorization side)
- Federation entities (Actor, IndexedDid, ActorKeyStore) — Phase 1 complete
- Authorization is fully independent of Keycloak (DB-backed RBAC + Cerbos)

### The Chicken-and-Egg Problem
The instance onboarding page requires `[Authorize]`. But if the admin wants to choose their auth provider, they can't authenticate until the provider is configured. Keycloak must be pre-configured before the app starts.

## Proposed Future State

### New Flow
```
/setup (enter token) → validate
  → NEW STEP: Auth Provider Configuration (setup-token-protected, NOT authenticated)
    ├── Keycloak toggle + credential inputs (if not in env vars)
    ├── ATProto Login toggle (zero config needed from admin)
    ├── Google SSO toggle + credential inputs
    ├── At least 1 must be enabled
    ├── Info icons explaining each option's implications
    └── Save → dynamic auth scheme registration
  → Authenticate with chosen provider
  → /onboarding/instance (existing settings, now with added decentralization toggle)
    └── NEW: ATProto Decentralization toggle (requires ATProto Login enabled)
        └── Confirmation: "Enabling decentralization means all event data will be public"
```

### Auth Provider Settings (New Governance Keys)
| Setting Key | Type | Default | Description |
|---|---|---|---|
| `auth.keycloak_enabled` | bool | false | Enable Keycloak OIDC login |
| `auth.keycloak_authority` | string | "" | Keycloak realm URL |
| `auth.keycloak_client_id` | string | "" | OIDC client ID |
| `auth.atproto_login_enabled` | bool | false | Enable ATProto DID-based login |
| `auth.google_sso_enabled` | bool | false | Enable Google OAuth login |
| `auth.google_client_id` | string | "" | Google OAuth client ID |
| `federation.decentralization_enabled` | bool | false | Enable ATProto data distribution (PDS sync) |

Secrets (`keycloak_client_secret`, `google_client_secret`) stored via `InfrastructureSecretSettingKeys` pattern (not in SystemSetting plain text).

### Two Separate Toggles for ATProto
1. **ATProto Login** (`auth.atproto_login_enabled`) — Authentication only. Users log in with their DID/PDS. Zero config from self-hoster. Shown during auth provider configuration step (before authentication).
2. **ATProto Decentralization** (`federation.decentralization_enabled`) — Data distribution. Event records synced to user's PDS → public. Requires ATProto Login enabled. Shown during instance onboarding (after authentication) with confirmation dialog.

### Dynamic Auth Scheme Registration
ASP.NET Core supports dynamic auth scheme management via `IAuthenticationSchemeProvider.AddScheme()` and `IOptionsMonitorCache<T>`. The approach:
1. At startup, register only Cookie auth (always needed for session)
2. Check DB for configured providers → register their schemes dynamically
3. During first-run setup, save provider config → register schemes immediately
4. No app restart required

### Info Icon Content (Draft)
- **Keycloak**: "Self-hosted identity provider. Full control over user accounts, passwords, and SSO. Requires running a separate Keycloak server."
- **ATProto Login**: "Users authenticate with their AT Protocol identity (e.g., Bluesky account). No external server needed — users bring their own identity."
- **Google SSO**: "Users authenticate with their Google account. Requires a Google Cloud project with OAuth credentials configured."
- **Decentralization Warning**: "Enabling ATProto decentralization means event records will be published to users' Personal Data Servers (PDS). This data becomes publicly accessible on the AT Protocol network. Self-hosters wanting private events should keep this disabled."

## Implementation Phases

### Phase 0: Requirements Finalization (This Session)
Resolve open questions through requirements gathering.

### Phase 1: Domain & Application Layer — Auth Provider Settings
- Add governance setting keys for auth providers
- Add governance setting keys for federation/decentralization
- Create DTOs for auth provider configuration
- Create command/handler for saving auth provider config (setup-token-protected)
- Create query/handler for reading auth provider config
- **Effort**: M
- **Skills**: `clean-architecture-rules`, `cqrs-mediatr-guidelines`

### Phase 2: Infrastructure — Dynamic Auth Scheme Registration
- Make `AuthenticationExtensions` conditional (don't fail if Keycloak config missing)
- Create `DynamicAuthenticationSchemeManager` service
- Implement runtime registration for Keycloak OIDC scheme
- Implement runtime registration for Google OAuth scheme
- Implement ATProto OAuth handler (custom auth scheme — NOT standard OIDC)
- Read auth config from DB at startup, register enabled schemes
- **Effort**: XL
- **Skills**: `auth-patterns`, `clean-architecture-rules`

### Phase 3: Blazor UI — Auth Provider Configuration Page
- New page/component between Setup and Login in the onboarding flow
- Keycloak toggle with credential inputs (conditionally shown)
- ATProto Login toggle with info icon
- Google SSO toggle with credential inputs
- "At least 1 required" validation
- Info icons with explanatory text
- Save button → API call → dynamic scheme registration
- **Effort**: L
- **Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

### Phase 4: Blazor UI — Decentralization Toggle in Instance Onboarding
- Add new step or section to `InstanceOnboarding.razor`
- ATProto Decentralization toggle (disabled if ATProto Login not enabled)
- Confirmation dialog with warning text about public data
- Info icon explaining implications
- **Effort**: S
- **Skills**: `blazor-ui-conventions`

### Phase 5: Login Page — Multi-Provider Support
- Update login page to show only enabled providers
- Provider-specific login buttons (Keycloak, ATProto, Google)
- Redirect logic per provider
- **Effort**: M
- **Skills**: `blazor-ui-conventions`, `blazor-bff-patterns`

### Phase 6: User Sync — Multi-Provider Support
- Update `SyncUserCommandHandler` to handle multiple provider types
- ATProto user sync: create User + Actor with self-sovereign DID
- Google user sync: create User + Actor with custodial DID
- Link external logins via `UserExternalLogin`
- **Effort**: L
- **Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`

### Phase 7: Admin Settings — Post-Onboarding Management
- Add auth provider management to instance admin settings page
- Safety check: cannot disable provider if it's the only one and admin uses it
- **Effort**: M
- **Skills**: `blazor-ui-conventions`

### Phase 8: Testing
- Unit tests for dynamic scheme registration
- Unit tests for auth provider config commands/queries
- Integration tests for the setup → auth config → login → onboarding flow
- Architecture tests for new governance setting keys
- **Effort**: L

## Risk Assessment

### High Risk
1. **Dynamic Auth Scheme Registration**: While ASP.NET Core supports it, runtime OIDC scheme registration with dynamic discovery document endpoints is rarely done. Edge cases around concurrent requests during scheme registration, scheme removal, and options caching.

### Medium Risk
2. **ATProto OAuth Integration via FishyFlip**: FishyFlip handles the protocol complexity (DPoP, PAR, client metadata), but integrating it as an ASP.NET Core `AuthenticationHandler<T>` that interacts with the BFF session cookie flow is uncharted territory. FishyFlip's OAuth is designed for standalone use, not as middleware.
3. **Secret Storage for Auth Credentials**: Keycloak client secret and Google client secret entered via UI need secure storage. Current `InfrastructureSecretSettingKeys` pattern exists but hasn't been used for UI-entered secrets before.
4. **Admin Lockout**: Admin disables their only auth provider → locked out. Safety checks needed.

### Low Risk
5. **UI Complexity**: The setup flow adds one more step. MudBlazor components handle this well.

## Resolved Design Questions

1. ~~**Backward compatibility**: No longer applicable — still in development, no released instances.~~ RESOLVED
2. ~~**ATProto OAuth scope**: Full ATProto OAuth with DPoP via FishyFlip library.~~ RESOLVED
3. ~~**First admin creation**: Same pattern as Keycloak — extract identity from provider claims, create User, assign InstanceAdmin.~~ RESOLVED
4. ~~**Credential validation**: Yes — validate before saving. Test Keycloak connectivity (fetch OIDC discovery endpoint), test Google OAuth discovery endpoint. Fail fast with user-friendly error if unreachable.~~ RESOLVED
5. ~~**Multi-tenant implications**: Same `IsLocked` pattern as existing governance settings. Instance admin sets defaults; tenants can override which providers are available to their users unless the setting is locked.~~ RESOLVED
6. ~~**Login page UX**: Vertical stack of buttons for Keycloak and Google. ATProto gets a **handle input field** (not a "Sign in with Bluesky" button — ATProto is a protocol, not bound to Bluesky). If Keycloak env vars are present, Keycloak is the default/recommended option.~~ RESOLVED
7. ~~**Account linking**: All providers bind to the same account. Auto-match by verified email (Keycloak ↔ Google). Explicit linking via account settings for ATProto (DID-based, no email guarantee).~~ RESOLVED
8. ~~**ATProto client metadata hosting**: Pre-fill public URL from browser request during setup, ask user to confirm, persist it. Detect local/private URLs (localhost, 192.168.x.x, 10.x.x.x) and warn (don't hard-disable — tunneling tools like ngrok/Cloudflare Tunnels exist). If URL changes later, provide admin setting to update.~~ RESOLVED

## Open Questions

**All questions resolved.** Plan finalized 2026-03-01.

## ATProto OAuth — Technical Research Summary

### Protocol Overview
ATProto OAuth is NOT standard OIDC. Key differences:
- **No `client_secret`**: The `client_id` is a public URL pointing to a JSON client metadata document
- **Mandatory DPoP** (RFC 9449): All token requests and resource access require DPoP proof
- **PAR** (Pushed Authorization Requests): Initial auth request is POSTed server-to-server, not via browser redirect params
- **Dynamic discovery**: User handle → DID resolution → PDS → Authorization Server metadata
- **`sub` verification**: Token response includes `sub` (the DID) which MUST be verified against the original identity resolution
- **ES256 key pair**: Client generates and hosts a JWKS for `private_key_jwt` token endpoint auth

### Server-Side Flow
1. App hosts client metadata at public URL (e.g., `/.well-known/atproto-oauth-client-metadata.json`)
2. App hosts JWKS at public URL (contains ES256 public key)
3. User enters handle → app resolves DID → discovers PDS → discovers Authorization Server
4. App sends PAR request to Authorization Server (includes DPoP proof, `client_id` URL)
5. Authorization Server fetches and validates client metadata, returns `request_uri`
6. User redirected to Authorization Server to authenticate and authorize
7. Callback returns authorization code → app exchanges for tokens (with DPoP)
8. App verifies `sub` (DID) in token response matches resolved identity
9. BFF session cookie established

### For Auth-Only (No PDS Resource Access)
From the spec: "Clients which are only using atproto OAuth for account authentication (without authorization to access PDS resources) should request minimal scopes, but still need to implement most of the authorization flow."
- Use scope: `atproto` (or minimal subset)
- Critical: still verify `sub` to confirm account identity
- No need to make authenticated PDS API calls for auth-only use

### FishyFlip (.NET Library — MIT License)
**`drasticactions/FishyFlip`** already implements ATProto OAuth for .NET:
- `ATProtocolBuilder().Build()` → `GenerateOAuth2AuthenticationUrlResultAsync(clientId, redirectUri, scopes, instanceUrl)`
- `AuthenticateWithOAuth2CallbackResultAsync(callbackUrl)` → returns session with DID
- Handles: DPoP, PAR, client metadata, JWKS generation, identity resolution
- **Caveat**: Designed for standalone client use, not as ASP.NET Core auth middleware
- **Integration approach**: Wrap FishyFlip's OAuth flow in a custom `AuthenticationHandler<AtprotoAuthOptions>` that bridges to the BFF cookie session

### Client Metadata Document (Must Be Publicly Hosted)
```json
{
  "client_id": "https://your-instance.example.com/.well-known/atproto-oauth-client-metadata.json",
  "client_name": "ISLAMU Explore",
  "client_uri": "https://your-instance.example.com",
  "application_type": "web",
  "grant_types": ["authorization_code", "refresh_token"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "private_key_jwt",
  "token_endpoint_auth_signing_alg": "ES256",
  "scope": "atproto",
  "redirect_uris": ["https://your-instance.example.com/signin-atproto"],
  "dpop_bound_access_tokens": true,
  "jwks_uri": "https://your-instance.example.com/.well-known/atproto-oauth-jwks.json"
}
```

## Potential Risks & Unknowns

The highest risk is **dynamic auth scheme registration at runtime**. While `IAuthenticationSchemeProvider.AddScheme()` exists, the interaction with `IOptionsMonitorCache<OpenIdConnectOptions>` and the OIDC discovery document caching can cause subtle issues. If this proves too fragile, the fallback is to require an app restart after auth provider configuration (acceptable for first-run setup, less ideal for runtime changes).

## Credential Validation (Resolved)

Before saving auth provider configuration, validate credentials:
- **Keycloak**: Fetch `{authority}/.well-known/openid-configuration`. If unreachable → error: "Cannot reach Keycloak at this URL. Verify the authority URL and ensure Keycloak is running."
- **Google**: Fetch `https://accounts.google.com/.well-known/openid-configuration` (always reachable), then validate client ID format. Full validation happens on first login attempt.
- **ATProto Login**: No credentials to validate (zero-config). Toggle on/off only.

## Multi-Tenant Auth Settings (Resolved)

Follows the existing `IsLocked` pattern from governance settings:
- Instance admin configures auth providers during setup or in admin panel
- Each provider setting has an `IsLocked` flag
- If `IsLocked = false`: tenant admins can override (enable/disable providers for their tenant)
- If `IsLocked = true`: tenant inherits instance setting, cannot override
- Same `SettingsResolver` cascade: system → tenant override (if not locked)
- Credential secrets (Keycloak client secret, Google client secret) are instance-level only — tenants cannot provide their own OAuth credentials

## Login Page UX (Resolved)

Vertical stack layout:
```
┌─────────────────────────────────┐
│  🔑 Sign in with Keycloak       │  ← Button (if enabled; bold/recommended if env vars present)
│  🔵 Sign in with Google          │  ← Button (if enabled)
│──────────────── or ──────────────│
│  AT Protocol                     │
│  ┌──────────────────────────┐   │
│  │ Enter your handle        │   │  ← Text input (e.g. user.bsky.social or custom domain)
│  └──────────────────────────┘   │
│  [ Sign In ]                    │  ← Submit button for ATProto
└─────────────────────────────────┘
```

Key UX decisions:
- ATProto is NOT a button like Keycloak/Google — it's a handle input field
- ATProto is a protocol, not bound to Bluesky — no Bluesky branding
- If only one provider is enabled, skip the multi-provider UI and go directly to that flow
- If Keycloak env vars detected at startup, Keycloak button appears first with "Recommended" badge

## Account Linking (Resolved)

All identity providers bind to the same user account:

### Auto-Matching (Email-Based)
- When user logs in via Google with email `user@example.com`
- If a Keycloak user with the same **verified** email exists → link automatically
- Creates `UserExternalLogin` record linking Google provider to existing User
- Works bidirectionally (Keycloak first → Google links, or Google first → Keycloak links)

### Explicit Linking (ATProto)
- ATProto DIDs don't guarantee email availability
- User must explicitly link ATProto identity via Account Settings page
- Flow: Logged in (via Keycloak/Google) → Account Settings → "Link AT Protocol identity" → enter handle → ATProto OAuth flow → link DID to account
- Creates `UserExternalLogin` record with ATProto provider and DID as ProviderKey

### Safety Rules
- Cannot unlink the last remaining provider (would lock user out)
- Admin can force-link/unlink via admin panel (for support cases)

## ATProto Client Metadata Hosting (Resolved)

ATProto OAuth requires a publicly reachable URL for the client metadata JSON document.

### Setup Flow
1. During auth provider configuration, if ATProto Login is enabled:
2. Pre-fill the public URL from the browser's current request URL (e.g., `https://events.example.com`)
3. Ask user to confirm or edit the URL
4. Validate: detect private/local URLs (localhost, 127.0.0.1, 192.168.x.x, 10.x.x.x, 172.16-31.x.x)
5. If private URL detected → show warning: "ATProto OAuth requires a publicly accessible URL. If you're using a tunneling service (ngrok, Cloudflare Tunnels), you may proceed. Otherwise, ATProto login will not work."
6. Do NOT hard-disable — user confirms they understand the limitation
7. Persist the confirmed public URL in governance settings

### Runtime
- Serve `/.well-known/atproto-oauth-client-metadata.json` using the persisted URL
- Serve `/.well-known/atproto-oauth-jwks.json` with the ES256 public key (generated once, stored)
- Admin can update the public URL later via admin settings if domain changes

### Governance Setting Key
```
auth.atproto_public_url = "https://events.example.com"  // persisted
```
The second risk is **wrapping FishyFlip in an ASP.NET Core auth handler**. FishyFlip handles ATProto OAuth internally with its own session management, but we need it to bridge to ASP.NET Core's cookie-based auth pipeline. This may require forking or extending FishyFlip's OAuth internals, or writing a thin adapter layer.

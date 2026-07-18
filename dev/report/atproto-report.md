<!-- ABOUTME: Architecture report for AT Protocol integration: OAuth login (Part A) and community-lexicon event/RSVP federation (Part B). -->
<!-- ABOUTME: Covers scaffolding inventory, CarpaNet adoption, API trust bridge, governance-gated publish/ingest pipelines, sequencing, risks. -->

# AT Protocol Integration — Architecture Report

> **Status:** Guidance / pre-implementation (rev 3 — adds Part B: community lexicon events & RSVP)
> **Date:** 2026-07-18
> **Author:** architect-agent
> **Library:** [CarpaNet](https://www.nuget.org/packages/CarpaNet/) — the official replacement for FishyFlip by the same author (docs reviewed at `~/dev/Github/CarpaNet/docs/docs/`)
> **Related settings:** `auth.atproto_login_enabled`, `AtprotoPublicUrl`, `federation.decentralization_enabled` (+ new keys in Part B §B3)
> **Related docs:** `docs/FEDERATION.md`, `docs/LEXICONS.md`, `docs/OUTBOX_PATTERN.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/adr/`

| Part | Feature | Independent toggle |
|---|---|---|
| **A** (§1–§9) | ATProto OAuth **login** (handle → PDS OIDC-style flow → session) | `auth.atproto_login_enabled` |
| **B** (§B1–§B9) | Community-lexicon **event & RSVP federation** (fetch network events for `/` + `/eventlist`; publish event/RSVP records to users' PDS) | New `federation.atproto_event_*` keys, instance-lockable per tenant |

A self-hoster may enable **either part alone, both, or neither**. Part B *fetch* has zero dependency
on Part A; Part B *publish* soft-depends on Part A only in that a record can be written to a
user's PDS solely when that user has a linked ATProto identity.

---

# Part A — ATProto OAuth Authentication

## 1. Architecture Summary

Add **AT Protocol OAuth** (handle → DID → PDS discovery → PAR + PKCE + DPoP OAuth) as a second
login provider next to Keycloak. The **BFF (`Explore.Blazor`) owns the OAuth dance and the cookie
session** using **`CarpaNet.OAuth`**; the **API (`Explore.API`) owns identity persistence and mints
a first-party session JWT** so the existing `[Authorize]` / YARP bearer-forwarding pipeline works
unchanged.

Two decisive findings:

1. **~40% of the feature already exists as intentional scaffolding** in this repo (entities,
   repositories, handlers, scheme registration, login UI handle input). The work completes a
   designed-in seam.
2. **CarpaNet eliminates the hardest protocol work.** The repo stubs name FishyFlip, but CarpaNet
   is its successor. `OAuthSession.AuthorizeAsync(handle)` internally performs identity resolution
   + AS discovery + PAR + PKCE + DPoP and returns the redirect URL; `CallbackAsync(url)` performs
   state validation + DPoP-bound token exchange. We do **not** hand-roll any of the OAuth
   cryptography — our work is the glue: session/state stores, client metadata hosting, cookie
   sign-in, and the API trust bridge.

---

## 2. Current State Inventory (verified in source)

### Already implemented — reuse as-is

| Layer | Artifact | State |
|---|---|---|
| Domain | `Explore.Domain/UserAuthenticationToken.cs` (`Provider`, `AccessToken`, `RefreshToken`, `PdsHost`, `DpopKey`, `IdToken`, `ExpiresAt`; tenant-scoped, auditable) | ✅ Done |
| Domain | `Explore.Domain/IndexedDid.cs` (DID PK, `Handle`, `PdsHost`, `SigningKey`) | ✅ Done |
| Domain | `AuthSchemeNames.Atproto` + `AllProviders` | ✅ Done |
| Persistence | `UserAuthenticationTokenConfiguration/Repository`, `IndexedDidConfiguration/Repository`; tables in `init` migration | ✅ Done |
| Application | `UserAuthenticationToken` DTOs + validators + CRUD handlers; `SyncUserCommandHandler` already normalizes the ATProto provider and manages `UserExternalLogin` | ✅ Done |
| API | `UserAuthenticationTokenController`, `IndexedDidController`, MultiAuth policy scheme (JWT Bearer + API Key) with dynamic Keycloak authority | ✅ Done |
| BFF | `DynamicAuthSchemeManager.RegisterAtprotoScheme()` gated by `AtprotoLoginEnabled` | ✅ Done |
| BFF | `/auth/providers` returns `type: "handle_input"`; `BffProviderReadinessService` maps `atproto` ↔ scheme | ✅ Done |
| UI | `LoginRedirect.razor` renders the handle input and calls `/auth/challenge?provider=atproto&login_hint={handle}` | ✅ Done |
| Config | `AuthProviderConfigurationDto.AtprotoLoginEnabled / AtprotoPublicUrl / LockAtprotoLoginEnabled` + onboarding & admin UI | ✅ Done |
| Federation | `PdsSyncWorker` + `PdsService` (XRPC `putRecord`/`deleteRecord`) — future consumer of stored user tokens | ✅ Done |

### The gap — what this report plans

| Layer | Artifact | State | CarpaNet coverage |
|---|---|---|---|
| Packages | `CarpaNet`, `CarpaNet.OAuth` (evaluate `CarpaNet.AspNetCore`) in `Directory.Packages.props` | ❌ Missing | n/a — adoption task |
| BFF | `AtprotoAuthenticationHandler.HandleChallengeAsync` (real flow; stub returns **501**) | ❌ Stub | ✅ `OAuthSession.AuthorizeAsync(handle)` returns the auth URL |
| BFF | Callback endpoint `/signin-atproto` (`AtprotoAuthenticationOptions.CallbackPath`) | ❌ Missing | ✅ `OAuthSession.CallbackAsync(callbackUrl)` → `ATProtoOAuthClient` |
| BFF | Handle → DID → PDS resolution | ❌ Missing | ✅ `CarpaNet.Identity.IdentityResolver.CreateWithCache()` (also implicit inside `AuthorizeAsync`) |
| BFF | PAR, PKCE, DPoP keys, `use_dpop_nonce` retry | ❌ Missing | ✅ Fully internal to `CarpaNet.OAuth` |
| BFF | `IOAuthSessionStore` implementation (DPoP keys + tokens, keyed by DID `sub`) | ❌ Missing | ⚙️ Interface provided; **we implement** backed by API (`UserAuthenticationToken`) |
| BFF | `StateStore` for pending web flows (transient, TTL) | ❌ Missing | ⚙️ Interface provided; **we implement** (HybridCache/distributed cache) |
| BFF | Client metadata document + JWKS hosting at `AtprotoPublicUrl` | ❌ Missing | ❓ Spike: `CarpaNet.AspNetCore` may help; else small endpoint |
| API | ATProto session bridge endpoint + first-party session JWT scheme in MultiAuth | ❌ Missing | ⚙️ Verification call via CarpaNet client (`com.atproto.server.getSession` lexicon) |
| BFF | ATProto branch in session refresh + signout | ❌ Missing | ✅ Auto-refresh + `TokenRefreshed` event + `SignOutAsync()` |

---

## 3. Library Selection: CarpaNet (supersedes the FishyFlip intent in the stubs)

CarpaNet is the designated replacement for FishyFlip (same author, drasticactions). Verified from
its docs:

| Package | Purpose | Used by us in |
|---|---|---|
| `CarpaNet` | Core XRPC client, **source-generated lexicon bindings**, identity resolution, DAG-CBOR/CAR | BFF (resolution), API/Infrastructure (verification, future `PdsService` modernization) |
| `CarpaNet.OAuth` | **OAuth 2.0 with PAR, PKCE, DPoP**; `OAuthSession`, `OAuthClientConfig`, `IOAuthSessionStore`, `MemoryOAuthSessionStore` | BFF (whole login flow), API (restore DPoP session for on-behalf-of PDS writes) |
| `CarpaNet.AspNetCore` | ASP.NET Core integration (docs sparse) | Spike in Phase 1 — may provide metadata/callback plumbing |
| `CarpaNet.Jetstream` | Event stream ingest | **Not needed for auth**; future federation ingest option |

### Properties that matter for this repo

1. **Web-app flow is first-class**: `OAuthClientConfig` takes a URL `ClientId`
   (`https://…/client-metadata.json`), `RedirectUri`, `Scope = "atproto transition:generic"`,
   plus pluggable `SessionStore` / `StateStore` — exactly our BFF shape.
2. **`IOAuthSessionStore` ≅ `UserAuthenticationToken`**: the store persists *DPoP keys + tokens*
   keyed by `sub` (DID). Columns `AccessToken`/`RefreshToken`/`DpopKey`/`PdsHost`/`ExpiresAt`
   map 1:1 onto `OAuthSessionData`. The entity was evidently modeled for this.
3. **`RestoreSessionAsync(did)`** rebuilds a DPoP-bound `ATProtoOAuthClient` from the store —
   this is precisely how `PdsSyncWorker`/`PdsService` will later write records **as the user**
   (`com.atproto.repo.createRecord` with the user's token), closing the federation loop.
4. **Automatic token refresh** (`AutoRetryOnAuthFailure`, `TokenRefreshed` event) — persistence of
   rotated tokens flows back through our session store implementation.
5. **Lexicons are declared in `.csproj`** (`<LexiconResolve Include="com.atproto.server.getSession" />`
   etc.) and resolved **via DNS at build time** by a Roslyn source generator (AOT-safe generated
   JSON contexts — compatible with the repo's source-generated serialization discipline).
   ⚠️ This has CI implications — see Risk #3.
6. **Exception-based error model** (`ATProtoException`), .NET 8+/net10.0 target — matches repo.

### What CarpaNet does NOT do (our glue work)

- Host the **client metadata JSON + JWKS** at the `ClientId` URL (unless the AspNetCore package
  covers it — spike).
- Cookie sign-in, claims principal construction, `returnUrl` safety, scheme registration — our
  existing BFF machinery.
- The **API trust bridge** (first-party session JWT) — our design, Section 5/D1.
- SSRF/network-policy hardening around its resolver HTTP calls — verify configurability (spike).

---

## 4. Target Flow (end-to-end, CarpaNet-annotated)

```
[1] Login page (LoginRedirect.razor — EXISTS)
    user types handle → GET /auth/challenge?provider=atproto&login_hint=<handle>

[2] BFF: AtprotoAuthenticationHandler.HandleChallengeAsync (IMPLEMENT — thin)
    a. Build OAuthClientConfig: ClientId = "<AtprotoPublicUrl>/oauth/client-metadata.json",
       RedirectUri = "<AtprotoPublicUrl>/signin-atproto", Scope = "atproto transition:generic",
       SessionStore = ApiBackedOAuthSessionStore, StateStore = CacheBackedStateStore
    b. authUrl = await oauthSession.AuthorizeAsync(login_hint)
       → CarpaNet internally: handle→DID (DNS TXT + .well-known) → DID doc (plc.directory /
         did:web) → PDS → protected-resource → AS metadata → PAR + PKCE + DPoP
    c. 302 redirect to authUrl (works for bsky.social, eurosky, self-hosted PDS)

[3] User authenticates on their PDS/entryway and consents.

[4] BFF: GET /signin-atproto?code=…&state=…&iss=… (IMPLEMENT — thin)
    a. atClient = await oauthSession.CallbackAsync(fullRequestUrl)
       → CarpaNet validates state/iss, exchanges code with DPoP (nonce retry), stores
         session (tokens + DPoP key) into our SessionStore keyed by DID
    b. SECURITY: assert atClient.AuthenticatedDid == DID resolved for the submitted handle
    c. Call API session bridge [5]; SignInAsync on the cookie scheme; stash the returned
       first-party JWT in the existing token slots (CircuitAccessTokenService / auth tokens)
    d. Redirect to safe returnUrl (IBffReturnUrlService)

[5] API: POST /api/auth/atproto/session (IMPLEMENT — the trust bridge)
    Input: DID, handle, PDS host, token set + DPoP key (or store-reference), tenant from BFF header
    a. Independently verify: restore CarpaNet OAuth session server-side and call
       com.atproto.server.getSession against the PDS — never blindly trust the BFF payload
    b. SyncUserCommand (EXISTS): provider=Atproto, providerUserId=DID → User/Actor/UserExternalLogin
    c. Upsert IndexedDid (did, handle, pdsHost) — existing repository
    d. Upsert UserAuthenticationToken (tokens + DpopKey, encrypted at rest) — the API-side
       IOAuthSessionStore reads/writes THIS table, shared with PdsSyncWorker
    e. Mint first-party session JWT (instance issuer, persisted signing key via Explore.Secrets /
       Data Protection): sub=<user Guid>, did, handle, tenant, provider=atproto, short TTL
    f. Return { jwt, userId, displayName, expiresAt }

[6] Steady state — ZERO changes to the proxy path:
    Cookie session at BFF → EventBffBearerForwardingHandler forwards the first-party JWT →
    API MultiAuth ForwardDefaultSelector routes by issuer → "AtprotoSession" JwtBearer branch.
    User-ID fallback (sub → nameidentifier → sid) preserved: sub = user Guid.

[7] Refresh & signout:
    - CarpaNet auto-refreshes PDS tokens; TokenRefreshed → session store → UserAuthenticationToken
      stays current. BFF re-mints the session JWT via [5] before expiry (BffSessionRefreshService branch).
    - /auth/signout: atClient.SignOutAsync() best-effort revocation + store delete; local cookie
      clearing remains the security boundary (existing pattern, unchanged).
```

---

## 5. Key Architectural Decisions

### D1 — API trust bridge: first-party session JWT (propose ADR-014)
A PDS access token is **DPoP-bound and audienced to the PDS** — `Explore.API` cannot validate it
like a Keycloak JWT. The API mints its own short-lived session JWT after independently verifying
the ATProto identity; MultiAuth gains a third branch (API Key | Keycloak JWT | AtprotoSession JWT)
via issuer sniffing in the existing `ForwardDefaultSelector`.

**Rejected:** Keycloak identity brokering (cannot do per-user AS discovery, PAR, DPoP, URL
client_id); BFF-trusted identity headers (breaks zero-trust BFF↔API boundary); per-request PDS
token validation at the API (per-PDS issuers + DPoP proof forwarding through YARP — fragile).

### D2 — OAuth client in the BFF, persistence behind the API
Cookie session, challenge/callback and scheme registration already live in `Explore.Blazor`.
**Rule 23 (Blazor isolation)** forbids the BFF from referencing Application/Persistence, so the
BFF's `IOAuthSessionStore` is **API-backed** (via `IEventApiClient` → `UserAuthenticationToken`
endpoints), and the transient `StateStore` uses BFF-local cache (never needs persistence).
Scheme-name constants come from the existing `Explore.Blazor.Constants` mirror, not `Explore.Domain`.

### D3 — Client identity: URL client_id + published metadata (we host it)
Serve at `AtprotoPublicUrl`: `GET /oauth/client-metadata.json` (client_id, redirect_uris =
`[<PublicUrl>/signin-atproto]`, `dpop_bound_access_tokens: true`, `token_endpoint_auth_method:
"private_key_jwt"`, scope `atproto transition:generic`, `jwks_uri`) and `GET /oauth/jwks.json`
(public ES256 client key; private key in Infisical via `Explore.Secrets`, rotation-capable).
Local dev uses CarpaNet's loopback helpers (`OAuthClientConfig.CreateLoopbackClientId/RedirectUri`).

### D4 — Library: CarpaNet (supersedes FishyFlip named in the stubs)
`CarpaNet` + `CarpaNet.OAuth` via `Directory.Packages.props`; spike `CarpaNet.AspNetCore` in
Phase 1. FishyFlip is deprecated in favor of CarpaNet by its author — do not adopt FishyFlip.
The `AtprotoAuthenticationHandler` seam keeps the library swappable if needed.

### D5 — One session store contract, two backings
Implement CarpaNet's `IOAuthSessionStore` twice against the **same table**:
- **BFF**: API-backed (rule 23) — used by the login flow.
- **API/Infrastructure**: repository-backed — used for verification [5a] and later by
  `PdsSyncWorker`/`PdsService` via `RestoreSessionAsync(did)` for on-behalf-of record writes.
`UserAuthenticationToken` is the single source of truth for ATProto tokens + DPoP keys (encrypted).

---

## 6. Security Requirements (non-negotiable)

1. **DID re-verification**: `AuthenticatedDid` must equal the pre-challenge resolved DID (BFF),
   **and** the API independently verifies via `getSession` before minting the session JWT.
2. **SSRF guardrails** around resolution/AS fetches: HTTPS-only, block private/loopback ranges,
   timeouts, size caps. Verify how much CarpaNet's HTTP stack lets us constrain (Phase 1 spike);
   wrap with our own delegating handler if configurable, otherwise isolate egress at deployment.
3. **State/PKCE/`iss`**: CarpaNet handles these — our `StateStore` must be single-use with TTL.
4. **Rate limiting**: challenge/callback under existing `global`/`write` policies; the handle
   field is attacker-controlled input reaching DNS/HTTP fetchers — sanitize `login_hint`.
5. **Token encryption at rest** for `AccessToken`/`RefreshToken`/`DpopKey` (value-converter or
   Secrets-layer envelope), consistent with SECRETS.md.
6. **Session JWT hygiene**: short TTL, dedicated issuer/audience, rotating persisted signing key;
   never exposed to WASM — the cookie remains the only browser credential (BFF invariant).

---

## 7. Sequencing (dependency-ordered phases)

> Task folder at implementation start: `dev/active/atproto-auth/`
> (`atproto-auth-plan.md`, `atproto-auth-context.md`, `atproto-auth-tasks.md`)

| Phase | Scope | Key files (touch) | Depends on |
|---|---|---|---|
| **0. ADR + task docs** | ADR-014 (session-JWT trust bridge + CarpaNet adoption), dev/active three-file set | `docs/adr/ADR-014-*.md`, `dev/active/atproto-auth/*` | — |
| **1. CarpaNet adoption + stores + metadata (BFF)** | Add `CarpaNet`/`CarpaNet.OAuth` (+AspNetCore spike) to `Directory.Packages.props`; **vendor lexicon JSONs** (`LexiconFiles`) for reproducible CI builds; API-backed `IOAuthSessionStore` + cache `StateStore`; `/oauth/client-metadata.json` + `/oauth/jwks.json` endpoints; ES256 client key in Secrets; SSRF-configurability spike | `Directory.Packages.props`, `Explore.Blazor/Services/Auth/*`, new endpoints ext., `lexicons/**` | 0 |
| **2. Challenge + callback (BFF)** | Real `HandleChallengeAsync` (`OAuthSession.AuthorizeAsync`), `/signin-atproto` (`CallbackAsync`), DID assertion, cookie sign-in | `Explore.Blazor/Authentication/AtprotoAuthenticationHandler.cs`, `BffAuthEndpoints.cs`, `MiddlewareExtensions.cs` | 1 |
| **3. API session bridge** | `POST /api/auth/atproto/session` (controller-authoring standard: explicit route/name/classification/ProducesResponseType), CarpaNet `getSession` verification, reuse `SyncUserCommand` + token/DID upserts, session-JWT minting, MultiAuth third branch | `Explore.API/Controllers/`, `Explore.API/Extensions/AuthenticationExtensions.cs`, `Explore.Application/Features/...` | 1 (parallel to 2) |
| **4. Session lifecycle** | Store API JWT in existing token slots; refresh branch (CarpaNet auto-refresh + JWT re-mint); signout revocation (`SignOutAsync`); `BffProviderReadinessService` ATProto readiness = config-present (no OIDC discovery ping) | `CircuitAccessTokenService.cs`, `BffSessionRefreshService`, `BffProviderReadinessService.cs` | 2 + 3 |
| **5. Hardening + docs + tests** | Rate limits, encryption-at-rest verification, NSwag client regen (failure pattern #1!), integration tests vs local dev PDS, update `docs/FEDERATION.md` (remove "not implemented" caveat), `AUTHORIZATION.md`, `CONFIGURATION.md`, `API_CHANGELOG.md`; note future: modernize `PdsService` onto CarpaNet + user-context writes via `RestoreSessionAsync` | tests + docs | 4 |

**Minimum test gates:** `Event.Architecture.Tests`, `Explore.Blazor.IntegrationTests`,
`Event.Application.UnitTests` (SyncUser/token handlers), store round-trip tests
(`OAuthSessionData` ↔ `UserAuthenticationToken`); manual E2E matrix: bsky.social, eurosky,
self-hosted PDS.

---

## 8. Risks & Open Questions

| # | Risk / question | Mitigation / needed decision |
|---|---|---|
| 1 | **Multi-tenant custom domains vs fixed redirect_uris** in the client metadata doc | v1: canonical instance host (`AtprotoPublicUrl`) handles all ATProto callbacks, then redirects to the tenant origin with session handoff; document limitation |
| 2 | **CarpaNet maturity** — docs state "site under construction"; OAuth surface may shift | Handler seam isolates it; pin exact version in `Directory.Packages.props`; Phase 1 spike validates web flow + AspNetCore package before committing Phase 2 |
| 3 | **Build-time lexicon resolution via DNS** (source generator, `CarpaNet_LexiconFailOnError=true`) breaks hermetic CI | Vendor lexicon JSONs into the repo via `<LexiconFiles Include="lexicons/**/*.json" />`; avoid `LexiconResolve*` network items in CI-built projects |
| 4 | SSRF-hardening hooks in CarpaNet's resolver/HTTP stack unknown | Phase 1 spike; wrap with delegating handler if supported, else network-egress policy at deployment tier |
| 5 | `UserAuthenticationToken` stores tokens as plain columns today | Phase 5 verifies encryption-at-rest; add value-converter encryption before GA if absent |
| 6 | Account linking (existing Keycloak user later logs in via ATProto) | `SyncUserCommandHandler` email auto-match is provider-gated; ATProto has no verified email → **no auto-match**; explicit linking UX = follow-up task |
| 7 | Session-JWT signing-key ops (rotation, multi-node) | Data Protection key-ring or Infisical-backed key with `kid` rotation; decide in ADR-014 |
| 8 | AS metadata variance (Bluesky entryway vs eurosky vs bare PDS) | CarpaNet abstracts discovery; still run the E2E matrix in Phase 5 |

---

## 9. Definition of Done

1. Handle input → PDS login → callback → authenticated cookie session works for Bluesky **and**
   Eurosky **and** a self-hosted PDS account.
2. DB after first login: `User` + `Actor` + `UserExternalLogin(Atproto, did)` +
   `UserAuthenticationToken(Provider=Atproto, PdsHost, DpopKey, tokens)` + `IndexedDid` rows.
3. Protected API calls succeed via the forwarded first-party JWT; HAL affordances render for the
   ATProto user identically to a Keycloak user.
4. Keycloak login regression-free; `auth.atproto_login_enabled=false` cleanly removes the provider.
5. CarpaNet packages pinned; lexicons vendored; CI builds hermetically.
6. ADR-014 merged; FEDERATION.md caveat replaced; NSwag client regenerated; all gate suites green.

What CarpaNet changes architecturally

**The hardest 30% of the original plan just evaporated.** In rev 1, Phase 1–2 included hand-rolling handle→DID resolution (DNS TXT + `.well-known`), PLC directory lookups, auth-server discovery, PAR requests, PKCE, ES256 DPoP proof generation, and `use_dpop_nonce` retry. CarpaNet's `CarpaNet.OAuth` package does **all of that internally**: `OAuthSession.AuthorizeAsync(handle)` returns the finished redirect URL, and `CallbackAsync(url)` validates state/`iss` and performs the DPoP-bound token exchange, returning an authenticated `ATProtoOAuthClient` with `AuthenticatedDid`. The web-app flow is first-class — URL `client_id` pointing at a metadata JSON, exactly the ATProto confidential-client pattern.

## The strongest validation of your existing schema

CarpaNet's `IOAuthSessionStore` contract persists **DPoP keys + tokens keyed by DID `sub`** — that maps 1:1 onto your existing `UserAuthenticationToken` columns (`AccessToken`, `RefreshToken`, `DpopKey`, `PdsHost`, `ExpiresAt`). The report's new **D5** decision implements this store twice against the same table: API-backed in the BFF (respecting Rule 23 Blazor isolation, going through `IEventApiClient`), and repository-backed in Infrastructure — which is exactly what lets `PdsSyncWorker` later call `RestoreSessionAsync(did)` to write `com.atproto.repo.createRecord` **as the user**, closing your federation loop. `TokenRefreshed` events keep the table current automatically.

## What stays ours (unchanged from rev 1)

- **ADR-014 trust bridge**: API still mints a first-party session JWT after independently verifying the DID via `com.atproto.server.getSession`; MultiAuth gets a third issuer-sniffed branch. CarpaNet doesn't touch this — it's our zero-trust BFF↔API design.
- Client metadata + JWKS hosting, cookie sign-in, `returnUrl` safety, scheme registration.

## New risks added to the report

1. **Build-time lexicon resolution via DNS** — CarpaNet's Roslyn source generator fetches lexicon schemas over the network at build time with `FailOnError=true` default. That breaks hermetic CI, so the report mandates **vendoring lexicon JSONs** via `<LexiconFiles>` instead of `<LexiconResolve>` network items.
2. **Library maturity** — docs literally say "site under construction"; mitigated by version pinning and keeping the `AtprotoAuthenticationHandler` seam library-agnostic.
3. **SSRF configurability spike** — need to verify whether CarpaNet's resolver HTTP stack accepts a delegating handler for private-IP blocking.

The sequencing table was rewritten: Phase 1 is now "CarpaNet adoption + stores + metadata hosting" instead of "build a resolver," and FishyFlip (named in your stubs' ABOUTME comments) is explicitly superseded in **D4** — same author, deprecated in CarpaNet's favor.

One execution step**: append the fully-drafted Part B (§B1–§B9, embedded verbatim in the plan file) to the report. The rev-3 header with the Part A/B split table was already applied before plan mode resumed.

## How your validation clarification shaped §B5

I verified the exact pattern you described in `src/Explore.Application/DTOs/Event/Validators/`: nullable-first DB columns with strictness living in manually-instantiated per-lifecycle validators — `CreateEventRequestValidator` (moderate), `UpdateEventDraftRequestDtoValidator` (lenient), `PublishEventRequestDtoValidator` (strict), `Archive/CancelEventRequestDtoValidator` (lenient). Part B extends this with **two coexisting lexicon profiles**:

- `AtprotoCalendarEventRecordValidator` — validates the **mapped lexicon projection** (not the domain DTO), exactly as strict as the vendored lexicon: only `name` + `createdAt` required; `mode`/`status` tokens, datetime formats, URI shapes checked only when present.
- `AtprotoCalendarRsvpRecordValidator` — `subject.uri` + `subject.cid` + `status` ∈ known tokens.

The coexistence contract: primary lifecycle validator runs first and fails the command as today; the lexicon profile runs after, only when the governance/consent/linked-identity guard chain passes, and its failure **never fails the primary command** — it skips the outbox enqueue with a structured warning. Both are manually instantiated per Critical Rule #2.

## Other key Part B decisions in the plan

- **Governance**: 4 new keys — `federation.atproto_event_fetch_enabled` + `_publish_enabled` with two matching `lock_tenant_*` instance locks, riding the existing 5-tier cascade (instance decides; unlocked → tenant chooses), plus mandatory **User-tier consent** since writing to a user's PDS repo is consent-sensitive. Fully independent of `auth.atproto_login_enabled`.
- **Publish**: first real wiring of your existing-but-unused `PdsSyncOutbox` transactional outbox across 7 handlers (Publish/Update/Cancel/Delete/Redact + 3 registration handlers), per-user DPoP writes via CarpaNet `RestoreSessionAsync`, RSVP strongRef ordering chained on the event record's `uri`/`cid` round-trip.
- **Fetch**: `CarpaNet.Jetstream` ingest worker filtered on the two community collections, curated-allowlist moderation default, tombstone purging, tenant-gated display via a home block + separate eventlist section behind HAL affordances.
- **Flagged schema gap**: `AtprotoRecord` has no payload column or entity link — migration decision assigned to ADR-015/Phase B0.

The plan file is ready for your approval to execute the append.

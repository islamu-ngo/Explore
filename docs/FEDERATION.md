ABOUTME: Documents implemented AT Protocol authentication and governed event federation boundaries.
ABOUTME: Covers DB-first PDS delivery, exhaustive projection, Jetstream discovery, HAL, and roadmap protocols.

# Federation

> **Audience:** Contributors | Integrators | AI agents
> **Status:** Implemented AT Protocol integration; ActivityPub/PDS hosting remain roadmap
> **Owner:** API
> **Last Verified:** 2026-07-19
> **Source Anchors:** `src/Explore.API/Controllers/AtprotoSessionController.cs`, `src/Explore.API/Controllers/EventController.cs`, `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`, `src/Explore.Blazor/Authentication/AtprotoAuthenticationHandler.cs`, `src/Explore.Infrastructure/Services/Federation/AtprotoJetstreamSubscriber.cs`, `src/Explore.Application/Features/Federation/Atproto/`, `docs/LEXICONS.md`, `docs/OUTBOX_PATTERN.md`

## Status

AT Protocol OAuth authentication is implemented for accounts that are already linked to a platform user. Governed outbound event/RSVP publication, exact-collection Jetstream ingestion, tenant-gated discovery, safe source HAL, and administrator/user client controls are implemented. ActivityPub bridging and operating an ATProto PDS/AppView remain roadmap features.

### ✅ Implemented (Current Runtime + Foundation)
- **Domain entities**: federation models exist in `Explore.Domain`:
  - `Actor` (federated identity representation)
  - `AtprotoRecord` (ATProto record storage)
  - `PdsSyncOutbox` (outbound sync queue model)
  - `IndexedDid` (DID cache model)
  - `ActorKeyStore` (key storage model)
  - supporting enums: `ActorType`, `DidCustodyType`
- **Persistence**: federation tables are represented through `ExploreDbContext` DbSets, entity configurations, and repositories, including ATProto record and PDS sync outbox repositories.
- **OAuth authentication**: the BFF uses CarpaNet confidential-client OAuth with protected single-use state, PKCE, DPoP, a server-private bootstrap bridge, encrypted DID-keyed session persistence, and a short-lived first-party ES256 API session JWT. The browser receives only the HttpOnly BFF cookie and safe display state.
- **Linked-account boundary**: the verified DID, PDS, tenant, and existing `UserExternalLogin` must agree. ATProto login does not create or email-match platform users.
- **Safe public API**: `GET /api/event` returns a typed local-or-federated discovery collection. Federated items receive only a policy-produced `source` relation to `GET /api/event/federated/{atprotoRecordId}/source`; no raw `AtprotoRecord` read or mutation API exists.
- **Governance**: `federation.atproto_events_enabled` controls inbound tenant presentation/stream demand and eligible outbound enqueue. `federation.atproto_event_validation_profile` selects platform or community-lexicon publication requirements, subject to instance locks; the community profile relaxes only required local business fields. `federation.atproto_publish_my_events` remains self-scoped user consent.
- **Projection**: one typed community event record maps native lexicon fields and renders every other public event value—including all sessions, aspects, resolved lookups, and EAV values—into one deterministic description. Coverage, privacy, or exact-size failures prevent enqueue; values are never silently dropped or truncated.
- **Inbound ingestion**: one leased CarpaNet Jetstream consumer accepts exactly the community event and RSVP collections from a fixed endpoint. An empty DID filter discovers all public publishers of those collections; a configured `AllowedDids` list restricts ingestion to curated publishers. The consumer persists one global canonical DID/collection/record-key row with its current source version, typed event projection, tenant presentation, tombstone/quarantine effects, and cursor state atomically.
- **Outbound delivery**: event lifecycle handlers atomically commit the local publication and immutable `PdsSyncOutbox` intent. A fenced worker rechecks capability, self-consent, linked session, source version, payload, and public-location privacy immediately before CarpaNet PDS I/O, then settles URI/CID and links the canonical record back to the committed local event.
- **Client surfaces**: instance administrators manage defaults/locks, unlocked tenant administrators manage effective capability/profile, and users manage only their own publication consent. Federated cards and local delivery status use text plus color and render actions only from HAL.
- **Authorization fallback**: local fallback authorization treats actor records as read-only for authenticated users and denies ATProto record/indexed DID writes except for instance-admin bypass.

### ATProto/PDS Account Email Ownership

ATProto OAuth login is implemented, while PDS account hosting is not. Identity lifecycle email remains PDS/account-authority owned:

- External PDS hosts own email confirmation, password reset, email update, account migration, and PDS security email for their accounts.
- Future ISLAMU-operated PDS cells also own those PDS credential emails for the accounts they host, even when ISLAMU operates the infrastructure or shares SMTP plumbing.
- ISLAMU Event must not send PDS credential-token emails through product `EmailDispatchOutbox`, `IEmailService`, RabbitMQ, TickerQ, or product unsubscribe flows.
- If ATProto account email is unavailable or unverified for product notification purposes, collect a separately verified app-level notification email or use in-app notifications.

### ⏳ Not Yet Implemented (Protocol Roadmap)
- **Public protocol endpoints** are still not implemented:
  - WebFinger (`/.well-known/webfinger`)
  - Actor profile endpoints (`/actors/{handle}`)
  - ActivityPub inbox/outbox
  - ATProto PDS server behavior
  - ATProto AppView indexing behavior
- **Bridge/gateway** translation (ActivityPub ↔ ATProto) is not implemented.
- **Cryptographic federation verification** (HTTP signatures / full protocol validation paths) is not implemented.
- **Public social collections** (followers/following/liked) are not exposed as protocol collections.

**Important**: ATProto login and ATProto Events governance are independent. A successful login never enables ingestion or publication, and enabling the administrator capability never substitutes for user publication consent.

## Protocol Overview

> **Note**: This section describes the **intended** federation model for ISLAMU Event.
> **Current Status**: ATProto OAuth, governed event/RSVP projection and delivery, exact-collection Jetstream ingestion, tenant-gated discovery, and client governance/status surfaces are implemented. Public ActivityPub endpoints and ATProto PDS/AppView server behavior are not implemented.

- **Server-to-Server (S2S)** (planned): Instances exchange activities via inbox/outbox.
- **HTTP Signatures** (planned): Cryptographic verification of federated messages.
- **WebFinger** (planned): Actor discovery via `/.well-known/webfinger`.
- **Collections** (planned): Followers, Following, Liked as ordered collections.

## Planned Architecture Philosophy

> **⚠️ Planned Feature**: The architecture diagrams below represent the **intended** public federation design.
> **Current Implementation**: Domain persistence, confidential ATProto OAuth, event/RSVP projection and DB-first delivery, governance, exact-collection ingestion, tenant discovery, and HAL-driven client surfaces exist. Public ActivityPub endpoints, ATProto PDS/AppView server behavior, and bridge/gateway behavior remain planned.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ISLAMU Event Architecture                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────────────────────┐   │
│   │   Users     │────▶│    PDS      │────▶│    ATProto Network          │   │
│   │  (DIDs)     │     │  (Hosting)  │     │  (Relay/Firehose/AppView)   │   │
│   └─────────────┘     └─────────────┘     └─────────────────────────────┘   │
│         │                                              │                    │
│         │                                              │                    │
│         ▼                                              ▼                    │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                    ISLAMU Event AppView                              │   │
│   │  • Indexes community event and RSVP records                          │   │
│   │  • Provides search/discovery APIs                                    │   │
│   │  • Manages cultural/audience filtering                               │   │
│   │  • Hosts ActivityPub Gateway                                         │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                        │                                    │
│                                        ▼                                    │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │               ActivityPub Gateway (Bridge)                           │   │
│   │  • Exposes ATProto events as ActivityPub Event objects              │   │
│   │  • Translates ActivityPub Follow → ATProto follow records           │   │
│   │  • Translates ActivityPub RSVP → ATProto participation records      │   │
│   │  • Would provide WebFinger, Actor endpoints, Inbox/Outbox (planned) │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                        │                                    │
│                                        ▼                                    │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                      Fediverse                                       │   │
│   │              (Mastodon, Mobilizon, Pleroma, etc.)                   │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Event Data Flow

The diagram below is a product-level illustration, not the record-shape contract. The implemented projection creates one community event record: native lexicon fields are mapped directly, and every remaining public event value—including every session, aspect, resolved lookup, and EAV value—is rendered into the same deterministic description. No companion session records are emitted, and any coverage, privacy, or size failure stops PDS enqueue instead of truncating data. The transactional outbox worker writes only after the local publication commits and uses a stable record key plus URI/CID reconciliation for retry safety.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Event Data Flow                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ATProto/ActivityPub Federation                    ISLAMU Event WebApp      │
│  ─────────────────────────────────                 ───────────────────      │
│                                                                             │
│  ┌─────────────────────────────┐                  ┌─────────────────────┐   │
│  │  Federated Event Record     │                  │  Full Event Detail  │   │
│  │  ─────────────────────────  │                  │  ─────────────────  │   │
│  │  • Title                    │                  │  • All event info   │   │
│  │  • Summary/Description      │    Link to       │  • Session 1        │   │
│  │  • Start Date (first)       │ ──────────────▶  │    - Time, Location │   │
│  │  • Location (primary)       │                  │    - Agenda         │   │
│  │  • Cover Image              │                  │    - Speakers       │   │
│  │  • URL → webapp             │                  │  • Session 2        │   │
│  │  • Basic audience info      │                  │    - Time, Location │   │
│  └─────────────────────────────┘                  │    - Agenda         │   │
│                                                    │  • Session N...     │   │
│  What Mastodon/Bluesky users see:                 │  • Registration     │   │
│  "🗓️ Community Iftar 2025                         │  • Comments         │   │
│   March 15-17, Amsterdam                          │  • Participants     │   │
│   Join us for 3 days of..."                       └─────────────────────┘   │
│   [View Event →]                                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## CID
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          How CID Works                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. User creates event record                                               │
│     ┌─────────────────────────────────┐                                     │
│     │ {                               │                                     │
│     │   "$type": "community.lexicon.calendar.event",│                       │
│     │   "name": "Community Iftar",    │                                     │
│     │   "startsAt": "2025-03-15...",  │                                     │
│     │   ...                           │                                     │
│     │ }                               │                                     │
│     └─────────────────────────────────┘                                     │
│                    │                                                        │
│                    ▼                                                        │
│  2. PDS serializes to DAG-CBOR (deterministic binary format)               │
│                    │                                                        │
│                    ▼                                                        │
│  3. PDS computes SHA-256 hash → CID                                        │
│     ┌─────────────────────────────────────────────────────────────┐        │
│     │ bafyreigxyz123... (base32 encoded multihash)                │        │
│     └─────────────────────────────────────────────────────────────┘        │
│                    │                                                        │
│  4. CID is included in the repo commit (signed by user's key)              │
│                                                                             │
│  WHY THIS MATTERS:                                                          │
│  ─────────────────                                                          │
│  • Anyone can verify the record hasn't been tampered with                  │
│  • If content changes → CID changes → old references become invalid        │
│  • Enables "strong references" (URI + CID = exact version)                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Identity System Hybrid Approach

> **Current State**: Keycloak/OIDC and CarpaNet ATProto OAuth are both active BFF authentication choices when configured. ATProto accepts only an already-linked DID and never creates or email-matches a platform account. Custodial DID provisioning for Keycloak users in the diagram remains planned and is separate from login.

The current login paths converge on the existing platform user identifier so authorization and tenant isolation remain unchanged:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    Hybrid Auth Architecture                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                      ┌─────────────────┐                                    │
│                      │   Login Page    │                                    │
│                      └────────┬────────┘                                    │
│                               │                                             │
│              ┌────────────────┼────────────────┐                            │
│              ▼                                 ▼                            │
│    ┌──────────────────┐              ┌──────────────────┐                  │
│    │ Login with Email │              │ Login with ATProto│                  │
│    │   (Keycloak)     │              │ (Bring your DID)  │                  │
│    └────────┬─────────┘              └────────┬─────────┘                  │
│             │                                  │                            │
│             ▼                                  ▼                            │
│    ┌──────────────────┐              ┌──────────────────┐                  │
│    │ Keycloak returns │              │ ATProto OAuth    │                  │
│    │ UUID + email     │              │ returns DID      │                  │
│    └────────┬─────────┘              └────────┬─────────┘                  │
│             │                                  │                            │
│             ▼                                  ▼                            │
│    ┌──────────────────┐              ┌──────────────────┐                  │
│    │ Create custodial │              │ Link existing    │                  │
│    │ DID for user     │              │ DID to account   │                  │
│    └────────┬─────────┘              └────────┬─────────┘                  │
│             │                                  │                            │
│             └────────────────┬─────────────────┘                            │
│                              ▼                                              │
│                    ┌──────────────────┐                                     │
│                    │  user + actor    │                                     │
│                    │  records created │                                     │
│                    └──────────────────┘                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Handling Nullable DID (`did_status`)

The key insight is that DID creation is **async**. Here's how to handle it:
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    DID Status State Machine                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────┐     Background job      ┌─────────┐                           │
│  │ pending │ ──────────────────────▶ │ active  │                           │
│  └─────────┘     creates DID         └─────────┘                           │
│       │                                                                     │
│       │ DID creation fails                                                  │
│       ▼                                                                     │
│  ┌─────────┐     Retry succeeds      ┌─────────┐                           │
│  │ failed  │ ──────────────────────▶ │ active  │                           │
│  └─────────┘                         └─────────┘                           │
│                                                                             │
│  WHAT USERS CAN DO:                                                         │
│  ──────────────────                                                         │
│  pending → Browse, edit profile (read-heavy)                                │
│  failed  → Same as pending, show "retry" button                            │
│  active  → Full access: post events, comment, etc. (write access)          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Lexicon Boundary

[LEXICONS.md](LEXICONS.md) defines the exact vendored community event/RSVP, `strongRef`, location, and session lexicons compiled by CarpaNet. It also records the stricter product mapping: one exhaustive event description and outbound `#going` only. The lexicons define record vocabulary; lifecycle handlers, governance, privacy evaluation, `PdsSyncOutbox`, Jetstream persistence, and HAL remain the publication authorities.

## Outbox Boundary

[OUTBOX_PATTERN.md](OUTBOX_PATTERN.md) documents the generic transactional outbox and names `PdsSyncOutbox` as a specialized AT Protocol sync variant. This supports reliable outbound processing, but it is not equivalent to a public ActivityPub inbox/outbox server API.

## Operator And Product Guidance

- Treat `federation.atproto_events_enabled` as one governed capability for both canonical Jetstream ingestion and eligible outbound PDS delivery. Keep it disabled until migrations, worker configuration, and operator recovery procedures are ready.
- The `community_lexicon` validation profile changes required local publication fields only; it never authorizes invalid supplied values, incomplete projections, private disclosure, or oversized records.
- Treat `auth.atproto_login_enabled` as authentication only; it does not enable event federation or user publication consent.
- Keep public-facing release notes precise: AT Protocol OAuth, event discovery, Jetstream ingestion, and lifecycle-owned PDS publication are implemented; ActivityPub interoperability and first-party PDS hosting remain roadmap work.
- Keep ATProto/PDS account email separate from ISLAMU product notification email. PDS SMTP is account-authority transport, not a general product-email provider.
- When adding another federation protocol, update this document with its exact routes, auth/trust boundaries, conformance tests, and rollback guidance without weakening the AT Protocol database-first publication invariant.

## Related

- [PROJECT.md](PROJECT.md) — product scope and maturity.
- [LEXICONS.md](LEXICONS.md) — NSID hierarchy and schema boundaries.
- [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md) — transactional outbox and PDS sync variant.
- [AUTHORIZATION.md](AUTHORIZATION.md) — authorization provider model.

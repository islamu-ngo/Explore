ABOUTME: Documents federation status, boundaries, and planned protocol architecture.
ABOUTME: Distinguishes shipped governance/auth controls from roadmap protocol features.

# Federation

> **Audience:** Contributors | Integrators | AI agents
> **Status:** Mixed
> **Owner:** API
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Explore.Domain/Actor.cs`, `Explore.Domain/Federation/PdsSyncOutbox.cs`, `Explore.API/Controllers/AtprotoRecordController.cs`, `Explore.API/BackgroundServices/PdsSyncWorker.cs`, `Explore.Infrastructure/Services/Federation/PdsService.cs`, `docs/LEXICONS.md`, `docs/OUTBOX_PATTERN.md`

## Status

Federation protocol support (ATProto / ActivityPub bridge behavior) remains a **roadmap feature**. The project currently ships foundation models, persistence, internal API resources, outbound PDS sync plumbing, and federation-related governance/auth controls, but not public protocol endpoints.

### ✅ Implemented (Current Runtime + Foundation)
- **Domain entities**: federation models exist in `Explore.Domain`:
  - `Actor` (federated identity representation)
  - `AtprotoRecord` (ATProto record storage)
  - `PdsSyncOutbox` (outbound sync queue model)
  - `IndexedDid` (DID cache model)
  - `ActorKeyStore` (key storage model)
  - supporting enums: `ActorType`, `DidCustodyType`
- **Persistence**: federation tables are represented through `ExploreDbContext` DbSets, entity configurations, and repositories, including ATProto record and PDS sync outbox repositories.
- **Internal API resources**: CRUD-style APIs exist for ATProto records and indexed DID records. These are platform API resources, not public ATProto or ActivityPub protocol endpoints.
- **Outbound PDS sync**: `PdsSyncWorker` polls `PdsSyncOutbox` entries and calls `IPdsService`; `PdsService` sends XRPC `com.atproto.repo.putRecord` and `com.atproto.repo.deleteRecord` requests to a configured PDS host.
- **Governance toggle**: instance-level decentralization setting is available via `federation.decentralization_enabled`.
- **Settings surface**: decentralization is represented in governance setting DTOs and can be locked through `LockDecentralizationEnabled` when instance governance settings are updated.
- **Related auth setting**: `auth.atproto_login_enabled` is a separate governance setting for ATProto login support. Do not document public ATProto OAuth login as implemented until source adds that flow.
- **Authorization fallback**: local fallback authorization treats actor records as read-only for authenticated users and denies ATProto record/indexed DID writes except for instance-admin bypass.

### ATProto/PDS Account Email Ownership

Current federation code is foundation-only and does not implement public ATProto OAuth login or PDS account hosting. When those features arrive, identity lifecycle email remains PDS/account-authority owned:

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

**Important**: The decentralization toggle is now part of runtime governance configuration, and outbound PDS sync foundation exists, but full external federation protocol behavior is still phased roadmap work.

## Protocol Overview (Planned)

> **Note**: This section describes the **intended** federation model for ISLAMU Event.
> **Current Status**: Foundation models, internal resources, governance controls, and outbound PDS sync plumbing exist. Public ActivityPub and ATProto server protocol endpoints are not implemented.

- **Server-to-Server (S2S)** (planned): Instances exchange activities via inbox/outbox.
- **HTTP Signatures** (planned): Cryptographic verification of federated messages.
- **WebFinger** (planned): Actor discovery via `/.well-known/webfinger`.
- **Collections** (planned): Followers, Following, Liked as ordered collections.

## Planned Architecture Philosophy

> **⚠️ Planned Feature**: The architecture diagrams below represent the **intended** public federation design.
> **Current Implementation**: Domain models, persistence, internal ATProto resources, governance settings, and outbound PDS sync plumbing exist. Public ActivityPub endpoints, ATProto PDS/AppView server behavior, and bridge/gateway behavior are not implemented.

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
│   │  • Indexes ngo.islamu.event.* records                               │   │
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

## Event Data Flow (Planned Implementation)

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
│     │   "$type": "ngo.islamu.event...",│                                    │
│     │   "title": "Community Iftar",   │                                     │
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

## Identity System Hybrid Approach (Planned)

> **⚠️ Planned Feature**: The hybrid authentication system is not yet implemented.
> **Current State**: Keycloak/OIDC authentication is active. ATProto login support is represented by governance settings and foundation models, but public ATProto OAuth login is not implemented.

**Planned Approach**: Support BOTH Keycloak (traditional) AND ATProto OAuth, with custodial DIDs for Keycloak users:

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

[LEXICONS.md](LEXICONS.md) defines the current NSID hierarchy used for stable event and event-session projections. That document explicitly excludes ATProto PDS publication, bridgy-fed wiring, ActivityPub federation, and outbox/publication machinery. Link to it when documenting schema vocabulary; do not use it as evidence that public federation publication is implemented.

## Outbox Boundary

[OUTBOX_PATTERN.md](OUTBOX_PATTERN.md) documents the generic transactional outbox and names `PdsSyncOutbox` as a specialized AT Protocol sync variant. This supports reliable outbound processing, but it is not equivalent to a public ActivityPub inbox/outbox server API.

## Operator And Product Guidance

- Treat decentralization settings as governance controls for foundation behavior, not a promise that external federation is live.
- Keep public-facing release notes explicit: federation protocol interoperability is roadmap work until protocol endpoints, conformance tests, and operator runbooks exist.
- Keep ATProto/PDS account email separate from ISLAMU product notification email. PDS SMTP is account-authority transport, not a general product-email provider.
- If implementing public protocol support later, update this document with exact routes, auth/trust boundaries, conformance tests, and rollback guidance.

## Related

- [PROJECT.md](PROJECT.md) — product scope and maturity.
- [LEXICONS.md](LEXICONS.md) — NSID hierarchy and schema boundaries.
- [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md) — transactional outbox and PDS sync variant.
- [AUTHORIZATION.md](AUTHORIZATION.md) — authorization provider model.

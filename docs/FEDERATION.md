# Federation

## Status

Federation (ATProto / ActivityPub) is a **roadmap feature** - currently in **Phase 1: Data Model Only**.

### ✅ Implemented (Phase 1: Foundation)
- **Domain Entities**: Federation data models exist in `Explore.Domain`:
  - `Actor` - Federated identity representation
  - `AtprotoRecord` - ATProto record storage
  - `PdsSyncOutbox` - Sync queue for outbound records
  - `IndexedDid` - DID resolution cache
  - `ActorKeyStore` - Cryptographic key storage
  - Supporting entities: `ActorType`, `DidCustodyType` enums

### ⏳ Not Yet Implemented (Roadmap)
- **HTTP Endpoints**: No federation endpoints exposed in `Explore.API`:
  - ❌ WebFinger (`/.well-known/webfinger`)
  - ❌ Actor endpoints (`/actors/{handle}`)
  - ❌ Inbox/Outbox (ActivityPub)
  - ❌ ATProto PDS server
  - ❌ ATProto AppView indexing
- **Protocol Implementation**: No active federation protocol logic
- **Bridge/Gateway**: ActivityPub ↔ ATProto translation not implemented
- **DID Resolution**: No PLC/DNS-based DID resolution
- **HTTP Signatures**: Cryptographic message verification not implemented
- **Collections**: Followers/Following/Liked collections not exposed

**Timeline**: Federation implementation planned for future release. Foundation (entities) is complete.

## Protocol Overview (Planned)

> **Note**: This section describes the **intended** federation model for ISLAMU Event.
> **Current Status**: Protocol implementation not yet started. Only domain entities exist.

- **Server-to-Server (S2S)** (planned): Instances exchange activities via inbox/outbox
- **HTTP Signatures** (planned): Cryptographic verification of federated messages
- **WebFinger** (planned): Actor discovery via `/.well-known/webfinger`
- **Collections** (planned): Followers, Following, Liked as ordered collections

## Planned Architecture Philosophy

> **⚠️ Planned Feature**: The architecture diagrams below represent the **intended design**.
> **Current Implementation**: Only domain entities (Actor, AtprotoRecord, etc.) exist. No HTTP endpoints or federation logic implemented.

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
> **Current State**: Only Keycloak/OIDC authentication is active. ATProto OAuth integration is planned.

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

## Handling Nullable DID (did_status)

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

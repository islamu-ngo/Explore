## 1. Project Overview

## What is ISLAMU Event?

**ISLAMU Event** (also known as ISLAMU Explore or Event Explorer) is an **open-source, federated event discovery platform**  built **ATProto-first** with an ActivityPub gateway for interoperability with the existing Fediverse (Mastodon, Mobilizon, etc.).

## Core Value Proposition

| Component | Value |
  |-----------|-------|
| **Software** | Multi-tenant federated event platform (AGPL) |
| **Primary Instance** | Trusted Islamic event directory with verification |
| **Filtering System** | Culturally-appropriate discovery (age, gender, madhab) |

## Organizational Context

- **Organization**: ISLAMU Non Profit Organization (Islamic Software Lighthouse Alliance of the Muslim Ummah)
- **Legal Entity**: Belgian non-profit organization ASBL (Association sans but lucratif)
- **GitHub Organization**: `islamu-ngo`
- **Repository**: `https://github.com/islamu-ngo/explore`
- **License**: AGPL-3.0

---

# Strategic Context

## Ecosystem Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      ISLAMU Event Ecosystem                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │           ISLAMU-Hosted Instance (Primary Focus)            │   │
│  │  ─────────────────────────────────────────────────────────  │   │
│  │  • Islamic events globally                                  │   │
│  │  • Verified organizations (fact-checked)                    │   │
│  │  • User-submitted events (flagged as unverified)            │   │
│  │  • Strike/ban system for policy violations                  │   │
│  │  • Advanced filtering (age, gender, location, language, and more)│   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              │                                      │
│                    ActivityPub Federation                           │
│                              │                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │ Community A  │  │ Community B  │  │ Organization │  ...         │
│  │ Instance     │  │ Instance     │  │ Instance     │              │
│  │ (3rd party)  │  │ (3rd party)  │  │ (3rd party)  │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
│         │                  │                  │                     │
│         └──────────────────┴──────────────────┘                     │
│                    Managed Hosting Partners                         │
│                    (Revenue share with ISLAMU)                      │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Architecture Philosophy

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
│   │  • Provides WebFinger, Actor endpoints, Inbox/Outbox                │   │
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

# Core Features

## Two-Tier Verification System

```
┌─────────────────────────────────────────────────────────────────────┐
│                    VERIFICATION SYSTEM                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  TIER 1: User-Submitted Events                                      │
│  ├── Anyone can create account                                      │
│  ├── Can post events immediately                                    │
│  ├── Events marked as "User Reported"                               │
│  ├── Subject to community moderation                                │
│  └── Strike/ban system for violations                               │
│                                                                     │
│  TIER 2: Verified Organizations                                     │
│  ├── Application required                                           │
│  └── Fact-checking process:                                         │
│      └── Organization exists? (registration check)                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Advanced Filtering System

| Filter | Options | Description |
|--------|---------|-------------|
| **Audience Age** | Children, Youth, Adults, Seniors, All Ages | Target demographic |
| **Gender** | Men-only, Women-only, Mixed, Family | Audience type |
| **Location** | Country, Region, City, Radius | Geographic filtering with PostGIS |
| **Language** | Arabic, English, French, etc. | Event language |
| **Event Type** | Webinar, Conference | Event Type |
| **Madhab** | Hanafi, Maliki, Shafi'i, Hanbali | Islamic jurisprudence school |
| **Category** | Aqidah, Fiqh, Tafsir, Hadith, etc. | Event classification |
| **Tag** | Mohammed Hijab, Mufti Menk etc... | Event tags |
| **TagType** | Person, Channel, oeuvres | Tag classification |
| **TagTypeTags** | Specific tags within TagType | e.g., Person → Mohammed Hijab |
| **Event Format** | In-person Local (physical), Digital (online), Hybrid | Event format |
| **Date/Time** | Upcoming, This Week, This Month, Custom | Temporal filtering |
| **Verification** | Verified only, All | Trust level |

## Liturgical Temporal Engine

**Dynamic Prayer-Relative Scheduling**:

- Events can be scheduled relative to prayer times (e.g., "15 minutes after Maghrib")
- exact times based on event geolocation using Third-party API

## Moderation System

Still todo

## Data Portability

Export formats supported:
- **iCal/ICS**: Standard calendar format for events
- **CSV**: Attendee lists, organizations, bulk data
- **ActivityPub-native**: Federation-compatible JSON-LD
- **Full database dump**: Complete data export for self-hosters

---

# Domain Model

## Overview

The domain layer contains all business entities, enums, and value objects. Located in `Explore.Domain/`.

**Key Principles**:
- All IDs are `Guid` except lookup tables use `int`
- Use `int` instead of `long` except for size/cursor fields
- No default values in entities (set in code or database)
- Tenant isolation: All tenant-scoped entities have `TenantId`

## Core Entities

### Tenant Management

#### Tenant
- **PK**: `Id` (Guid)
- **Properties**: `FullName`, `Slug`, `IsActive`
- **Purpose**: Multi-tenant isolation for SaaS mode
- **Key**: Used across all tenant-scoped entities for data isolation

#### TenantUser
- **PK**: `Id` (int)
- **FK**: `UserId` → User, `TenantId` → Tenant
- **Purpose**: Maps users to tenants with roles
- **Key**: Enables users to belong to multiple tenants

#### TenantSettings
- **PK**: `Id` (int)
- **FK**: `TenantId` → Tenant
- **Purpose**: Tenant-specific configuration
- **Key**: Stores tenant-level settings and policies

#### UserRole
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Defines user roles within a tenant
- **Key**: Controls user permissions (Admin, Moderator, User, etc.)

### User Management

#### User
- **PK**: `Id` (Guid)
- **FK**: `ActorId` → Actor (optional)
- **Properties**: `Email`, `FirstName`, `LastName`, `AuthProvider`, `EmailVerified`
- **Purpose**: Represents system users
- **Key**: Links to ATProto Actor for federation

#### UserAuthenticationToken
- **PK**: `Id` (Guid)
- **FK**: `UserId` → User, `TenantId` → Tenant
- **Properties**: `Provider`, `AccessToken`, `RefreshToken`, `PdsHost`, `DpopKey`
- **Purpose**: Stores authentication tokens (Keycloak, ATProto OAuth)
- **Key**: Handles hybrid auth (Keycloak + ATProto)

#### UserExternalLogin
- **PK**: `Id` (Guid)
- **FK**: `UserId` → User, `TenantId` → Tenant
- **Properties**: `Provider`, `ProviderKey`, `ProviderDisplayName`
- **Purpose**: Stores external login credentials (DID, private keys)
- **Key**: Enables ATProto OAuth users to bring their own DID

### Federation (ATProto)

#### Actor
- **PK**: `Id` (Guid)
- **FK**: `ActorTypeId` → ActorType, `DidCustodyTypeId` → DidCustodyType
- **Properties**: `DisplayName`, `Did`, `Handle`, `PdsHost`, `Description`
- **Purpose**: Represents federated identities (DIDs)
- **Key**: Decentralized-first identity model

#### ActorType
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Defines actor types (User, Organization, Service, Bot)
- **Key**: Differentiates between entity types in federation

#### DidCustodyType
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Defines DID custody types (Self-Custodied, Custodial, Managed)
- **Key**: Determines who controls DID keys

#### ActorKeyStore
- **PK**: `Id` (Guid)
- **FK**: `ActorId` → Actor, `TenantId` → Tenant
- **Properties**: `KeyPurpose`, `PrivateKeyEncrypted`, `PublicKey`, `IsActive`
- **Purpose**: Stores actor encryption/rotation keys
- **Key**: Uses vault transit encryption for private keys

#### IndexedDid
- **PK**: `Did` (varchar)
- **Properties**: `Handle`, `PdsHost`, `SigningKey`, `IsActive`, `LastIndexedAt`
- **Purpose**: Tracks indexed DIDs from ATProto network
- **Key**: Maintains index of federated actors

#### SyncState
- **PK**: `Id` (int)
- **Properties**: `Service` (unique), `Cursor`, `LastSeqTime`
- **Purpose**: Tracks federation sync progress
- **Key**: Resumable sync from firehose/relay

#### AtprotoRecord
- **PK**: `Id` (Guid)
- **Properties**: `Did`, `Collection`, `RecordKey`, `Cid`, `Uri`
- **Purpose**: Links entities to ATProto records
- **Key**: Enables strong references (URI + CID = exact version)

### Organization Management

#### Organization
- **PK**: `Id` (Guid)
- **FK**: `ApprovalStatusId` → ApprovalStatus, `ActorId` → Actor
- **Properties**: `FullName`, `Email`, `Country`, `City`, `Address`, `Postcode`, `WebsiteUrl`
- **Purpose**: Represents event-organizing entities
- **Key**: Two-tier verification (UserSubmitted vs Verified)
- **Navigation**: `Members` (readonly, use OrganizationMemberRepository for writes)

#### OrganizationMember
- **PK**: `Id` (int)
- **FK**: `OrganizationId` → Organization, `UserId` → User
- **Properties**: `OrganizationRoleId`, `OrganizationPositionId`
- **Purpose**: Links users to organizations
- **Key**: User can belong to multiple organizations with different roles

#### OrganizationRole
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Defines organization member roles (Owner, Admin, Member)
- **Key**: Controls permissions within organization

#### OrganizationPosition
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Defines member positions (President, Secretary, Member)
- **Key**: Optional role-based classification

#### ApprovalStatus
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Defines approval status (Pending, Approved, Rejected, Verified)
- **Key**: Two-tier verification workflow

#### OrganizationReview
- **PK**: `Id` (Guid)
- **Properties**: `Rating`, `Comment`
- **Purpose**: Community reviews of organizations
- **Key**: Trust signals for verified orgs

### Event Management

#### Event
- **PK**: `Id` (Guid)
- **FK**: `EventTypeId` → EventType, `AudienceGenderId` → AudienceGender, `AudienceAgeId` → AudienceAge, `ActorId` → Actor, `EventStatusId` → EventStatus, `VisibilityTypeId` → VisibilityType, `EventFormatId` → EventFormat, `MadhabId` → Madhab, `FeaturedImageId` → StorageObject, `AtprotoRecordId` → AtprotoRecord
- **Properties**: `Title`, `Description`, `Price`, `CurrencyCode`, `TotalViews`, `IsRegistrationRequired`, `EventUrl`, `ExternalRegistrationUrl`, `SessionCount`, `FirstSessionDate`, `LastSessionDate`, `Timezone`, `Slug`
- **Purpose**: Core event entity with all metadata
- **Key**: Federation-ready domain model (ATProto/ActivityPub concepts). HTTP federation endpoints are not implemented in `Explore.API` yet.
- **Computed**: `FirstSessionDate`/`LastSessionDate` from sessions

#### EventSession
- **PK**: `Id` (Guid)
- **FK**: `EventId` → Event, `LocationId` → Location, `RegistrationModeId` → RegistrationMode
- **Properties**: `StartTime`, `EndTime`, `Title`, `Description`, `Slug`, `MaxAudienceAttendees`, `CurrentAudienceAttendees`
- **Purpose**: Individual sessions within multi-session events
- **Key**: Allows events to span multiple dates/times/locations

#### EventSessionAgendaItem
- **PK**: `Id` (Guid)
- **FK**: `EventSessionId` → EventSession, `LocationId` → Location
- **Properties**: `StartTime`, `EndTime`, `Title`, `Description`
- **Purpose**: Sub-items within sessions (speakers, breaks, activities)
- **Key**: Detailed agenda structure

#### EventSessionSpeaker
- **PK**: `Id` (int)
- **FK**: `ActorId` → Actor, `EventSessionId` → EventSession
- **Purpose**: Links actors (speakers) to sessions
- **Key**: Many-to-many relationship with link table

#### EventSessionLanguage
- **PK**: `Id` (int)
- **FK**: `EventSessionId` → EventSession, `LanguageId` → Language
- **Purpose**: Session language support (multi-lingual events)
- **Key**: Filter events by language

#### EventRegistration
- **PK**: `Id` (Guid)
- **FK**: `UserId` → User, `EventSessionId` → EventSession, `ApprovalStatusId` → ApprovalStatus, `AtprotoRecordId` → AtprotoRecord
- **Purpose**: User registration for sessions
- **Key**: Supports approval workflow for certain events

#### RegistrationMode
- **PK**: `Id` (int)
- **Properties**: `MasterCode`, `FullName`, `Description`
- **Purpose**: Defines registration modes (Open, ApprovalRequired, InvitationOnly)
- **Key**: Controls registration workflow

### Event Metadata

#### EventType
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Event type classification (Conference, Webinar, Workshop, Seminar)
- **Key**: Primary event categorization

#### EventStatus
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Event lifecycle status (Draft, Published, Cancelled, Completed)
- **Key**: Event workflow management

#### VisibilityType
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Event visibility (Public, Private, Unlisted)
- **Key**: Access control

#### EventFormat
- **PK**: `Id` (int)
- **Properties**: `FullName`, `MasterCode`, `Description`
- **Purpose**: Event delivery format (In-person Local, Digital Online, Hybrid)
- **Key**: Physical vs digital classification

#### Madhab
- **PK**: `Id` (int)
- **Properties**: `MasterCode`, `FullName`, `Description`
- **Purpose**: Islamic jurisprudence school (Hanafi, Maliki, Shafi'i, Hanbali)
- **Key**: Islamic context filtering

#### AudienceAge
- **PK**: `Id` (int)
- **Properties**: `MasterCode`, `FullName`, `Description`, `MinAge`, `MaxAge`
- **Purpose**: Target age demographic (Children, Youth, Adults, Seniors, All Ages)
- **Key**: Age-appropriate event filtering

#### AudienceGender
- **PK**: `Id` (int)
- **Properties**: `MasterCode`, `FullName`, `Description`
- **Purpose**: Target gender (Men-only, Women-only, Mixed, Family)
- **Key**: Gender-appropriate event filtering

#### Category
- **PK**: `Id` (Guid)
- **FK**: `ParentId` → Category (self-referencing)
- **Properties**: `MasterCode`, `FullName`
- **Purpose**: Event categorization (Aqidah, Fiqh, Tafsir, Hadith, etc.)
- **Key**: Hierarchical categories for filtering

#### EventCategories
- **PK**: `Id` (int)
- **FK**: `EventId` → Event, `CategoryId` → Category
- **Purpose**: Many-to-many relationship between events and categories
- **Key**: Events can belong to multiple categories

#### Tag
- **PK**: `Id` (Guid)
- **Properties**: `MasterCode`, `FullName`
- **Purpose**: Event tags (speakers, topics, channels, etc.)
- **Key**: Flexible tagging system

#### TagType
- **PK**: `Id` (int)
- **Properties**: `MasterCode`, `FullName`
- **Purpose**: Tag type classification (Person, Channel, Oeuvre)
- **Key**: Organizes tags into groups

#### TagTypeTags
- **PK**: `Id` (int)
- **FK**: `TagId` → Tag, `TagTypeId` → TagType
- **Purpose**: Classifies tags by type
- **Key**: Tag type taxonomy

#### EventTags
- **PK**: `Id` (int)
- **FK**: `EventId` → Event, `TagId` → Tag
- **Purpose**: Many-to-many relationship between events and tags
- **Key**: Events can have multiple tags

### Location Management

#### Location
- **PK**: `Id` (Guid)
- **Properties**: `FullName`, `Address`, `Postcode`, `Country`, `City`, `Coordinates` (PostGIS point), `Latitude`, `Longitude`, `Timezone`
- **Purpose**: Physical or virtual event locations
- **Key**: PostGIS for spatial queries, timezone support

### Storage Management

#### StorageObject
- **PK**: `Id` (Guid)
- **FK**: `FileTypeId` → FileType, `ActorId` → Actor (owner)
- **Properties**: `Uri`, `FullName`, `Extension`, `Size`
- **Purpose**: File storage (images, documents, etc.)
- **Key**: Current implementation targets S3-compatible object storage via `Explore.Infrastructure` (multi-provider BYOK is a future capability).

#### FileType
- **PK**: `Id` (int)
- **Properties**: `MasterCode`, `FullName`, `Description`
- **Purpose**: File type classification (Image, Document, Video, Audio)
- **Key**: File type validation

### Language Support

#### Language
- **PK**: `Id` (int)
- **Properties**: `MasterCode`, `FullName`, `Description`
- **Purpose**: Supported languages (Arabic, English, French, etc.)
- **Key**: Multi-lingual event filtering

## Entity Relationships

### Core Relationships

```
Tenant
  ├── TenantUser (many-to-many with User)
  │
  ├── User
  │   ├── Actor (ATProto identity)
  │   ├── UserAuthenticationToken
  │   ├── UserExternalLogin
  │   ├── OrganizationMember
  │   └── EventRegistration
  │
  ├── Organization
  │   ├── Actor
  │   ├── OrganizationMember (many-to-many with User)
  │   └── OrganizationReview
  │
  ├── Event
  │   ├── Actor (owner)
  │   ├── StorageObject (featured image)
  │   ├── EventSession
  │   │   ├── Location
  │   │   ├── EventSessionAgendaItem
  │   │   ├── EventSessionSpeaker
  │   │   ├── EventSessionLanguage
  │   │   └── EventRegistration
  │   ├── EventCategories (many-to-many with Category)
  │   └── EventTags (many-to-many with Tag)
  │
  └── StorageObject
```

### Lookup Tables (Read-only)

All lookup tables use `int` PK and have seed data:
- `ApprovalStatus`
- `EventType`, `EventStatus`, `VisibilityType`, `EventFormat`
- `Madhab`, `AudienceAge`, `AudienceGender`
- `Language`, `RegistrationMode`
- `OrganizationRole`, `OrganizationPosition`
- `ActorType`, `DidCustodyType`, `FileType`
- `UserRole`

### Link Tables (Explicit Entities)

All link tables are explicit entities (not implicit many-to-many):
- `EventCategories` (Event ↔ Category)
- `EventTags` (Event ↔ Tag)
- `TagTypeTags` (Tag ↔ TagType)
- `EventSessionLanguages` (EventSession ↔ Language)
- `EventSessionSpeakers` (EventSession ↔ Actor)
- `OrganizationMember` (Organization ↔ User)
- `TenantUser` (Tenant ↔ User)

**IMPORTANT**: Navigation properties on link tables are **readonly**. Writes must go through the link table repository directly.

## Key Design Decisions

### Why Link Tables as Explicit Entities?

1. **Tenant Isolation**: Link tables carry `TenantId` to prevent cross-tenant data mixing
2. **Additional Metadata**: Link tables can store relationship-specific data
3. **Explicit Writes**: Clear separation between read (navigation) and write (repository)
4. **Audit Trail**: Easy to track when relationships are created/modified

### Why ATProto-First Identity?

1. **Decentralization**: Users own their identity (DID), not tied to platform
2. **Portability**: Move between platforms without losing identity
3. **Interoperability**: Works with Blusky, other ATProto services
4. **ActivityPub Gateway**: Planned bridge to the Fediverse (not implemented in `Explore.API` today)

### Why Hybrid Auth?

1. **Keycloak OAuth**: Traditional web auth, familiar to users
2. **ATProto OAuth**: Bring your own DID, true decentralization
3. **Custodial DIDs**: Keycloak users get custodial DIDs automatically
4. **Async DID Creation**: DID creation is async, handles pending/failed states

## Domain Invariants

### Event Validation

- `EventSession.EndTime` must be after `EventSession.StartTime`
- `EventSessionAgendaItem.EndTime` must be within parent session time
- Event must have at least one session to be published
- `TotalViews` starts at 0, incremented by views

### Organization Validation

- Organization email must be unique within tenant
- Organization cannot be its own parent (for hierarchical orgs)
- Owner role required for organization creation

### Category Validation

- Category cannot be its own parent
- Circular references not allowed in category hierarchy
- Root categories (ParentId = null) can exist

### Actor Validation

- DID must be unique globally
- Handle must be unique within PDS
- Custodial DIDs require ActorKeyStore entries

## Value Objects

### Geographic Coordinates

```csharp
// PostGIS point type
public class Coordinates
{
    public double Latitude { get; set; }  // -90 to 90
    public double Longitude { get; set; } // -180 to 180
}
```

### ATProto URI

```csharp
// AT URI format: at://did:plc:xxx/collection/rkey
public class AtProtoUri
{
    public string Did { get; set; }
    public string Collection { get; set; }
    public string RecordKey { get; set; }
}
```

## Domain Events

### Event Lifecycle Events

- `EventCreated`
- `EventPublished`
- `EventCancelled`
- `EventStarted`
- `EventEnded`

### Organization Events

- `OrganizationCreated`
- `OrganizationVerified`
- `OrganizationMemberAdded`
- `OrganizationMemberRemoved`

### Registration Events

- `UserRegisteredForSession`
- `UserCancelledRegistration`
- `RegistrationApproved`
- `RegistrationRejected`

# Domain Model

## Overview

The domain layer (`Explore.Domain/`) is the heart of the application, containing all business entities, enums, and value objects. It defines **what** the system is about, independent of any technical implementation details.

For details on how these domain models are implemented using Entity Framework Core, including conventions for IDs, numeric types, default values, and link tables, refer to the **`dotnet-efcore-guidelines` skill**.

## Core Entities

### Tenant Management

#### Tenant
*   **Purpose**: Represents a distinct client or organization within the multi-tenant system, ensuring data isolation.
*   **Key Relationships**: Core of multi-tenancy; all tenant-scoped entities link to `Tenant`.
*   **Associated Concepts**: `TenantUser`, `TenantSettings`.

#### TenantUser
*   **Purpose**: Links a User to a specific Tenant, often with associated roles.
*   **Key Relationships**: Many-to-many relationship between `User` and `Tenant`.

#### TenantSettings
*   **Purpose**: Stores tenant-specific configuration and policies.
*   **Key Relationships**: One-to-one relationship with `Tenant`.

#### UserRole
*   **Purpose**: Defines roles users can have within a tenant (e.g., Admin, Moderator, Member).
*   **Key Relationships**: Used to classify `TenantUser` permissions.

### User Management

#### User
*   **Purpose**: Represents a user account in the system.
*   **Key Relationships**: Can link to an `Actor` for federation, and associated with `UserAuthenticationToken`, `UserExternalLogin`, `OrganizationMember`, `EventRegistration`.

#### UserAuthenticationToken
*   **Purpose**: Stores authentication-related tokens for users (e.g., from Keycloak, ATProto OAuth).
*   **Key Relationships**: Links to `User` and `Tenant`.

#### UserExternalLogin
*   **Purpose**: Records external login details, such as DID (Decentralized Identifier) or private keys for ATProto OAuth.
*   **Key Relationships**: Links to `User` and `Tenant`.

### Federation (ATProto)

#### Actor
*   **Purpose**: Represents a federated identity (DID) in the ATProto network. This is the core identity for all content creators and organizations.
*   **Key Relationships**: Linked to `ActorType`, `DidCustodyType`, `User`.

#### ActorType
*   **Purpose**: Categorizes actors (e.g., User, Organization, Service, Bot).

#### DidCustodyType
*   **Purpose**: Defines who controls an Actor's DID keys (e.g., Self-Custodied, Custodial).

#### ActorKeyStore
*   **Purpose**: Securely stores cryptographic keys associated with an `Actor`.
*   **Key Relationships**: Links to `Actor` and `Tenant`.

#### IndexedDid
*   **Purpose**: Tracks DIDs discovered and indexed from the ATProto network.

#### SyncState
*   **Purpose**: Manages the synchronization progress with federation firehoses/relays.

#### AtprotoRecord
*   **Purpose**: Links local entities (like `Event` or `EventRegistration`) to their corresponding records on the ATProto network.
*   **Key Relationships**: Linked to entities that are federated.

### Organization Management

#### Organization
*   **Purpose**: Represents an entity that creates and manages events (e.g., a mosque, a university, a community group).
*   **Key Relationships**: Linked to `ApprovalStatus`, `Actor`, and has associated `OrganizationMember`s and `OrganizationReview`s.
*   **Associated Concepts**: Supports a two-tier verification system (User-Submitted vs. Verified).

#### OrganizationMember
*   **Purpose**: Represents a `User`'s membership within an `Organization`, defining their role and position.
*   **Key Relationships**: Links `Organization` with `User`.

#### OrganizationRole
*   **Purpose**: Defines predefined roles for `OrganizationMember`s (e.g., Owner, Admin, Member).

#### OrganizationPosition
*   **Purpose**: Defines specific positions within an `Organization` (e.g., President, Secretary).

#### ApprovalStatus
*   **Purpose**: Defines the various stages of approval for entities like `Organization`s or `EventRegistration`s (e.g., Pending, Approved, Verified).

#### OrganizationReview
*   **Purpose**: Captures community reviews and ratings for `Organization`s.

### Event Management

#### Event
*   **Purpose**: The central entity representing a scheduled occurrence (e.g., a conference, webinar, workshop).
*   **Key Relationships**: Links to `EventType`, `AudienceGender`, `AudienceAge`, `Actor`, `EventStatus`, `VisibilityType`, `EventFormat`, `Madhab`, `StorageObject` (for featured image), and potentially `AtprotoRecord`.
*   **Associated Concepts**: Can have multiple `EventSession`s.

#### EventSession
*   **Purpose**: Represents a specific time slot or segment of an `Event`. Events can have one or many sessions.
*   **Key Relationships**: Links to `Event`, `Location`, `RegistrationMode`.
*   **Associated Concepts**: Can contain `EventSessionAgendaItem`s, `EventSessionSpeaker`s, `EventSessionLanguage`s, and `EventRegistration`s.

#### EventSessionAgendaItem
*   **Purpose**: Details specific activities or segments within an `EventSession` (e.g., a speaker's talk, a break).
*   **Key Relationships**: Links to `EventSession`, `Location`.

#### EventSessionSpeaker
*   **Purpose**: Links an `Actor` (speaker) to an `EventSession`.
*   **Key Relationships**: Many-to-many relationship between `Actor` and `EventSession`.

#### EventSessionLanguage
*   **Purpose**: Indicates the languages supported or used in an `EventSession`.
*   **Key Relationships**: Many-to-many relationship between `EventSession` and `Language`.

#### EventRegistration
*   **Purpose**: Records a `User`'s registration for an `EventSession`.
*   **Key Relationships**: Links to `User`, `EventSession`, `ApprovalStatus`, and potentially `AtprotoRecord`.

#### RegistrationMode
*   **Purpose**: Defines the method of registration for an `EventSession` (e.g., Open, Approval Required, Invitation Only).

### Event Metadata

#### EventType
*   **Purpose**: Classifies `Event`s (e.g., Conference, Webinar, Workshop).

#### EventStatus
*   **Purpose**: Tracks the lifecycle status of an `Event` (e.g., Draft, Published, Cancelled).

#### VisibilityType
*   **Purpose**: Controls the visibility of an `Event` (e.g., Public, Private, Unlisted).

#### EventFormat
*   **Purpose**: Describes the delivery format of an `Event` (e.g., In-person, Online, Hybrid).

#### Madhab
*   **Purpose**: Classifies `Event`s or `Actor`s by Islamic jurisprudence school (e.g., Hanafi, Maliki).

#### AudienceAge
*   **Purpose**: Categorizes the target age demographic for an `Event` (e.g., Children, Youth, Adults).

#### AudienceGender
*   **Purpose**: Categorizes the target gender for an `Event` (e.g., Men-only, Women-only, Mixed).

#### Category
*   **Purpose**: Provides hierarchical categorization for `Event`s (e.g., Aqidah, Fiqh, Tafsir).
*   **Key Relationships**: Can be self-referencing for parent-child relationships.

#### EventCategories
*   **Purpose**: Links `Event`s to `Category`s.

#### Tag
*   **Purpose**: Provides flexible tagging for `Event`s, `Actor`s, or other entities.

#### TagType
*   **Purpose**: Classifies the type of a `Tag` (e.g., Person, Channel, Oeuvre).

#### TagTypeTags
*   **Purpose**: Links `Tag`s to `TagType`s.

#### EventTags
*   **Purpose**: Links `Event`s to `Tag`s.

### Location Management

#### Location
*   **Purpose**: Defines physical or virtual locations where `EventSession`s can occur.
*   **Associated Concepts**: Can include geographic coordinates for spatial queries.

### Storage Management

#### StorageObject
*   **Purpose**: Represents files stored in the system (e.g., images, documents).
*   **Key Relationships**: Links to `FileType` and `Actor` (owner).

#### FileType
*   **Purpose**: Classifies `StorageObject`s (e.g., Image, Document, Video).

### Language Support

#### Language
*   **Purpose**: Represents supported human languages for `EventSession`s.

## Domain Invariants

Domain invariants are rules that must always be true for an entity to be in a valid state. These are typically enforced within the domain entities themselves or orchestrated by the application layer.

*   **Event-related**: Session end time must be after start time; an event must have at least one session to be published.
*   **Organization-related**: Organization email must be unique within a tenant; an owner role is required for organization creation.
*   **Category-related**: Categories cannot form circular references in their hierarchy.
*   **Actor-related**: DID must be globally unique; custodial DIDs require associated keys.

## Domain Events

Domain events are discrete occurrences within the domain that other parts of the system might need to react to.

*   **Event Lifecycle Events**: `EventCreated`, `EventPublished`, `EventCancelled`, `EventStarted`, `EventEnded`.
*   **Organization Events**: `OrganizationCreated`, `OrganizationVerified`, `OrganizationMemberAdded`.
*   **Registration Events**: `UserRegisteredForSession`, `RegistrationApproved`.
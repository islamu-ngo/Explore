# API Reference

## REST API Conventions

- **Base Path**: `/api/v1`
- **Content-Type**: `application/json`
- **Authentication**: Bearer token (JWT) in `Authorization` header
- **Authorization Pattern**:
  - **GET endpoints**: `[AllowAnonymous]` - public read access
  - **POST/PUT/DELETE**: `[Authorize]` - authenticated write access
- **Routing**: `api/v1/[controller]` - controller name in plural
- **User ID Extraction**: Fallback order: `sub` → `nameidentifier` → `sid`

## User ID Extraction Pattern

```csharp
// Extract userId from JWT claims with fallback
var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
```

## Core Endpoints

### Event Management

#### Events

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/event` | AllowAnonymous | List all events |
| `GET` | `/api/v1/event/my` | Authorize | Get events created by current user's organizations |
| `GET` | `/api/v1/event/{id}` | AllowAnonymous | Get event by ID |
| `POST` | `/api/v1/event` | Authorize | Create event |
| `PUT` | `/api/v1/event/{id}` | Authorize | Update event |
| `DELETE` | `/api/v1/event/{id}` | Authorize | Delete event |

**Response Types**:
- `GET /api/v1/event`: `List<EventListDto>`
- `GET /api/v1/event/{id}`: `EventDto`
- `POST/PUT`: `BaseCommandResponse<Guid>`

#### EventSessions

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/eventsession` | AllowAnonymous | List all sessions |
| `GET` | `/api/v1/eventsession/{id}` | AllowAnonymous | Get session by ID |
| `GET` | `/api/v1/eventsession/by-event/{eventId}` | AllowAnonymous | Get sessions by event |
| `POST` | `/api/v1/eventsession` | Authorize | Create session |
| `PUT` | `/api/v1/eventsession/{id}` | Authorize | Update session |
| `DELETE` | `/api/v1/eventsession/{id}` | Authorize | Delete session |

#### EventSessionAgendaItems

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/eventsessionagendaitem` | AllowAnonymous | List all agenda items |
| `GET` | `/api/v1/eventsessionagendaitem/{id}` | AllowAnonymous | Get agenda item by ID |
| `GET` | `/api/v1/eventsessionagendaitem/by-session/{sessionId}` | AllowAnonymous | Get agenda items by session |
| `POST` | `/api/v1/eventsessionagendaitem` | Authorize | Create agenda item |
| `PUT` | `/api/v1/eventsessionagendaitem/{id}` | Authorize | Update agenda item |
| `DELETE` | `/api/v1/eventsessionagendaitem/{id}` | Authorize | Delete agenda item |

#### EventSessionSpeakers

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/eventsessionspeaker` | AllowAnonymous | List all speaker assignments |
| `GET` | `/api/v1/eventsessionspeaker/{id}` | AllowAnonymous | Get speaker assignment by ID |
| `GET` | `/api/v1/eventsessionspeaker/by-session/{sessionId}` | AllowAnonymous | Get speakers by session |
| `GET` | `/api/v1/eventsessionspeaker/by-actor/{actorId}` | AllowAnonymous | Get sessions by actor |
| `POST` | `/api/v1/eventsessionspeaker` | Authorize | Assign speaker |
| `PUT` | `/api/v1/eventsessionspeaker/{id}` | Authorize | Update assignment |
| `DELETE` | `/api/v1/eventsessionspeaker/{id}` | Authorize | Remove speaker |

#### EventSessionLanguages

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/eventsessionlanguage` | AllowAnonymous | List all session languages |
| `GET` | `/api/v1/eventsessionlanguage/{id}` | AllowAnonymous | Get session language by ID |
| `POST` | `/api/v1/eventsessionlanguage` | Authorize | Add language to session |
| `PUT` | `/api/v1/eventsessionlanguage/{id}` | Authorize | Update session language |
| `DELETE` | `/api/v1/eventsessionlanguage/{id}` | Authorize | Remove language from session |

#### EventRegistrations

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/eventregistration` | AllowAnonymous | List all registrations |
| `GET` | `/api/v1/eventregistration/{id}` | AllowAnonymous | Get registration by ID |
| `POST` | `/api/v1/eventregistration` | Authorize | Register for session |
| `PUT` | `/api/v1/eventregistration/{id}` | Authorize | Update registration |
| `DELETE` | `/api/v1/eventregistration/{id}` | Authorize | Cancel registration |

### Organization Management

#### Organizations

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/organization` | AllowAnonymous | List all organizations |
| `GET` | `/api/v1/organization/my` | Authorize | Get organizations for current user |
| `GET` | `/api/v1/organization/{id}` | AllowAnonymous | Get organization by ID |
| `POST` | `/api/v1/organization` | Authorize | Create organization |
| `PUT` | `/api/v1/organization/{id}` | Authorize | Update organization |
| `DELETE` | `/api/v1/organization/{id}` | Authorize | Delete organization |

#### OrganizationMembers

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/organizationmember` | AllowAnonymous | List all organization members |
| `GET` | `/api/v1/organizationmember/{id}` | AllowAnonymous | Get member by ID |
| `GET` | `/api/v1/organizationmember/by-organization/{orgId}` | AllowAnonymous | Get members by organization |
| `GET` | `/api/v1/organizationmember/by-user/{userId}` | AllowAnonymous | Get user's organizations |
| `POST` | `/api/v1/organizationmember` | Authorize | Add member to organization |
| `PUT` | `/api/v1/organizationmember/{id}` | Authorize | Update member role |
| `DELETE` | `/api/v1/organizationmember/{id}` | Authorize | Remove member from organization |

#### OrganizationReviews

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/organizationreview` | AllowAnonymous | List all reviews |
| `GET` | `/api/v1/organizationreview/{id}` | AllowAnonymous | Get review by ID |
| `GET` | `/api/v1/organizationreview/by-organization/{orgId}` | AllowAnonymous | Get reviews by organization |
| `POST` | `/api/v1/organizationreview` | Authorize | Create review |
| `PUT` | `/api/v1/organizationreview/{id}` | Authorize | Update review |
| `DELETE` | `/api/v1/organizationreview/{id}` | Authorize | Delete review |

### User Management

#### Users

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/user` | AllowAnonymous | List all users |
| `GET` | `/api/v1/user/{id}` | AllowAnonymous | Get user by ID |
| `POST` | `/api/v1/user` | Authorize | Create user |
| `PUT` | `/api/v1/user/{id}` | Authorize | Update user |
| `DELETE` | `/api/v1/user/{id}` | Authorize | Delete user |

### Location Management

#### Locations

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/location` | AllowAnonymous | List all locations |
| `GET` | `/api/v1/location/{id}` | AllowAnonymous | Get location by ID |
| `GET` | `/api/v1/location/by-city/{city}` | AllowAnonymous | Get locations by city |
| `GET` | `/api/v1/location/by-country/{country}` | AllowAnonymous | Get locations by country |
| `POST` | `/api/v1/location` | Authorize | Create location |
| `PUT` | `/api/v1/location/{id}` | Authorize | Update location |
| `DELETE` | `/api/v1/location/{id}` | Authorize | Delete location |

### Discovery Metadata

#### Categories

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/category` | AllowAnonymous | List all categories |
| `GET` | `/api/v1/category/{id}` | AllowAnonymous | Get category by ID |
| `POST` | `/api/v1/category` | Authorize | Create category |
| `PUT` | `/api/v1/category/{id}` | Authorize | Update category |
| `DELETE` | `/api/v1/category/{id}` | Authorize | Delete category |

**Note**: Categories support hierarchical relationships via `ParentId`.

#### Tags

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/tag` | AllowAnonymous | List all tags |
| `GET` | `/api/v1/tag/{id}` | AllowAnonymous | Get tag by ID |
| `POST` | `/api/v1/tag` | Authorize | Create tag |
| `PUT` | `/api/v1/tag/{id}` | Authorize | Update tag |
| `DELETE` | `/api/v1/tag/{id}` | Authorize | Delete tag |

**Note**: Tags are classified by TagType (Person, Channel, Oeuvre).

#### Languages

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/language` | AllowAnonymous | List all languages (lookup) |
| `GET` | `/api/v1/language/{id}` | AllowAnonymous | Get language by ID |

**Note**: Languages are read-only lookup tables. No create/update/delete endpoints.

### Lookup Tables

#### ApprovalStatus

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/approvalstatus` | AllowAnonymous | List all approval statuses (lookup) |
| `GET` | `/api/v1/approvalstatus/{id}` | AllowAnonymous | Get approval status by ID |

#### AudienceAge

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/audienceage` | AllowAnonymous | List all audience age groups (lookup) |
| `GET` | `/api/v1/audienceage/{id}` | AllowAnonymous | Get audience age by ID |

#### AudienceGender

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/audiencegender` | AllowAnonymous | List all audience gender types (lookup) |
| `GET` | `/api/v1/audiencegender/{id}` | AllowAnonymous | Get audience gender by ID |

#### EventType

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/eventtype` | AllowAnonymous | List all event types (lookup) |
| `GET` | `/api/v1/eventtype/{id}` | AllowAnonymous | Get event type by ID |

### Storage Management

#### StorageObjects

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/v1/storageobject` | AllowAnonymous | List all storage objects |
| `GET` | `/api/v1/storageobject/{id}` | AllowAnonymous | Get storage object by ID |
| `POST` | `/api/v1/storageobject` | Authorize | Upload file |
| `PUT` | `/api/v1/storageobject/{id}` | Authorize | Update storage object |
| `DELETE` | `/api/v1/storageobject/{id}` | Authorize | Delete storage object |

**Note**: StorageObjects use BYOK (Bring Your Own Keys) for AWS S3, Azure Blob, MinIO, etc.

### ActivityPub (Federation)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/.well-known/webfinger` | AllowAnonymous | Actor discovery |
| `GET` | `/actors/{username}` | AllowAnonymous | Get actor profile |
| `POST` | `/actors/{username}/inbox` | AllowAnonymous | Receive activity |
| `GET` | `/actors/{username}/outbox` | AllowAnonymous | Get outgoing activities |
| `GET` | `/actors/{username}/followers` | AllowAnonymous | Get followers |
| `GET` | `/actors/{username}/following` | AllowAnonymous | Get following |

**Note**: ActivityPub endpoints follow the ActivityPub specification for federation with Mastodon, Mobilizon, etc.

## Response Types

### BaseCommandResponse<Guid>

Used for all Create/Update/Delete operations:

```json
{
  "success": true,
  "message": "Event created successfully.",
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "errors": []
}
```

### DTO Types

**EventListDto**: Minimal properties for list views
```json
{
  "id": "guid",
  "title": "string",
  "description": "string",
  "eventTypeName": "string",
  "audienceGenderName": "string",
  "audienceAgeName": "string"
}
```

**EventDto**: Full details with navigation properties
```json
{
  "id": "guid",
  "title": "string",
  "description": "string",
  "eventTypeId": int,
  "eventTypeName": "string",
  "audienceGenderId": int,
  "audienceGenderName": "string",
  "actorId": "guid",
  "actorDisplayName": "string",
  "featuredImageId": "guid",
  "featuredImageUri": "string",
  // ... all entity properties with navigation details
}
```

**CreateEventDto**: Create payload (no Id, no TenantId)
```json
{
  "title": "string",
  "description": "string",
  "eventTypeId": 1,
  "audienceGenderId": 1,
  "audienceAgeId": 1,
  "actorId": "guid",
  "featuredImageId": "guid",
  "tenantId": "guid"
}
```

**UpdateEventDto**: Update payload (Id required)
```json
{
  "id": "guid",
  "title": "string",
  "description": "string",
  "eventTypeId": 1,
  "audienceGenderId": 1,
  "audienceAgeId": 1,
  // ... updatable properties only
}
```

## Error Responses

### Validation Errors (400)

```json
{
  "error": "Event creation failed.",
  "success": false,
  "message": "Event creation failed.",
  "errors": [
    "Title is required",
    "Start time must be in the future"
  ]
}
```

### Unauthorized (401)

```json
{
  "error": "User ID not found in token"
}
```

### Not Found (404)

```json
{
  "error": "Event not found or you don't have permission to delete it"
}
```

### Server Error (500)

```json
{
  "error": "Internal server error",
  "stackTrace": "..."
}
```

## API Documentation

- **Scalar**: `https://localhost:7001/scalar/v1`
- **Swagger UI**: `https://localhost:7001/swagger`
- **OpenAPI Spec**: `https://localhost:7001/swagger/v1/swagger.json`

## Pagination

**Not currently implemented** - All list endpoints return full results.

When implemented, will use:
- `?page=1&pageSize=20`
- Response will include `TotalCount` and `PageSize`

## Filtering

**Not currently implemented** - All list endpoints return all records.

When implemented, will use:
- `?filter[status]=published`
- `?filter[visibility]=public`
- `?filter[country]=Belgium`

## Sorting

**Not currently implemented** - Results are returned in database order.

When implemented, will use:
- `?sortBy=createdAt&sortOrder=desc`
- `?sortBy=title&sortOrder=asc`

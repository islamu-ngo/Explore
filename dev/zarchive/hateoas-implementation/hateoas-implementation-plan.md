# HATEOAS Implementation Plan - ISLAMU Event API

> **Enterprise-Grade Hypermedia REST API Implementation**
>
> Implementing HATEOAS (Hypermedia as the Engine of Application State) following RFC 8288 Web Linking,
> HAL+JSON specification, and Clean Architecture with CQRS patterns.

**Last Updated**: 2026-01-23

---

## Executive Summary

This plan outlines the implementation of HATEOAS (Hypermedia as the Engine of Application State) for the ISLAMU Event REST API. HATEOAS transforms the API from a collection of endpoints into a self-documenting, discoverable system where clients navigate through hypermedia links rather than hardcoded URLs.

### Key Objectives

1. **RFC 8288 Compliance**: Implement Web Linking standard with proper link relation types
2. **HAL+JSON Format**: Use `application/hal+json` media type for standardized hypermedia responses
3. **Clean Architecture Integration**: Maintain separation of concerns with CQRS pattern
4. **State-Driven Links**: Links dynamically reflect only currently valid actions based on resource state
5. **Enterprise Patterns**: Centralized link generation, testable, maintainable

### Why HATEOAS?

- **Discoverability**: Clients navigate API without hardcoded URLs
- **Evolvability**: API can change without breaking clients
- **Self-Documentation**: Responses describe available actions
- **Decoupling**: Loose coupling between client and server

### Design Philosophy: HATEOAS by Default

**HATEOAS is always ON** - This is REST Level 3 maturity. All responses include hypermedia links.

**Opt-Out via RFC 7240** - Clients who want minimal payloads send:
```
Prefer: return=minimal
```

This approach:
- Single response shape to maintain and test
- True REST compliance (not "REST-ish")
- Industry-standard opt-out mechanism
- No fragmented API surface

---

## Current State Analysis

### Existing API Structure

```
Explore.API/Controllers/
├── OrganizationController.cs     # CRUD operations, pagination
├── EventController.cs            # Events with sessions, filtering
├── EventSessionController.cs     # Session management
├── CategoryController.cs         # Lookup table
├── TagController.cs              # Tags management
├── LocationController.cs         # Location CRUD
├── ActorController.cs            # Actor management
└── ... (20+ controllers)
```

### Current Response Format

```json
// GET /api/organization/{id}
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "fullName": "ISLAMU Foundation",
  "email": "info@islamu.org",
  "country": "Belgium",
  "city": "Brussels"
  // No links, no navigation hints
}
```

### Issues with Current Approach

1. **No Discoverability**: Clients must know all endpoints upfront
2. **Hardcoded URLs**: Changes to routes break clients
3. **No State Awareness**: No indication of valid operations
4. **Missing Navigation**: No pagination links, no related resources

---

## Proposed Future State

### HAL+JSON Response Format

```json
// GET /api/organization/{id}
// Content-Type: application/hal+json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "fullName": "ISLAMU Foundation",
  "email": "info@islamu.org",
  "country": "Belgium",
  "city": "Brussels",
  "_links": {
    "self": { "href": "/api/organization/550e8400-e29b-41d4-a716-446655440000" },
    "collection": { "href": "/api/organization" },
    "events": { "href": "/api/organization/550e8400-e29b-41d4-a716-446655440000/events" },
    "members": { "href": "/api/organization/550e8400-e29b-41d4-a716-446655440000/members" },
    "update": { "href": "/api/organization/550e8400-e29b-41d4-a716-446655440000", "method": "PUT" },
    "delete": { "href": "/api/organization/550e8400-e29b-41d4-a716-446655440000", "method": "DELETE" }
  },
  "_embedded": {
    "actor": {
      "id": "...",
      "displayName": "ISLAMU Foundation",
      "_links": {
        "self": { "href": "/api/actor/..." }
      }
    }
  }
}
```

### Paginated Collection Response

```json
// GET /api/event?pageNumber=2&pageSize=10
// Content-Type: application/hal+json
{
  "pageNumber": 2,
  "pageSize": 10,
  "totalCount": 150,
  "totalPages": 15,
  "_links": {
    "self": { "href": "/api/event?pageNumber=2&pageSize=10" },
    "first": { "href": "/api/event?pageNumber=1&pageSize=10" },
    "prev": { "href": "/api/event?pageNumber=1&pageSize=10" },
    "next": { "href": "/api/event?pageNumber=3&pageSize=10" },
    "last": { "href": "/api/event?pageNumber=15&pageSize=10" },
    "create": { "href": "/api/event", "method": "POST" }
  },
  "_embedded": {
    "items": [
      {
        "id": "...",
        "title": "Ramadan Conference 2026",
        "_links": {
          "self": { "href": "/api/event/..." },
          "sessions": { "href": "/api/event/.../sessions" }
        }
      }
    ]
  }
}
```

### Opt-Out: Minimal Response (RFC 7240)

Clients requesting minimal payloads (e.g., mobile apps, high-frequency polling) send:

```http
GET /api/organization/550e8400-e29b-41d4-a716-446655440000
Prefer: return=minimal
```

Response (links stripped):

```json
// Response includes: Preference-Applied: return=minimal
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "fullName": "ISLAMU Foundation",
  "email": "info@islamu.org",
  "country": "Belgium",
  "city": "Brussels"
}
```

**RFC 7240 Prefer Header Values**:
| Value | Behavior |
|-------|----------|
| `return=representation` | Full HAL response with links (default) |
| `return=minimal` | Data only, no `_links` or `_embedded` |

---

## Architecture Design

### Clean Architecture Layer Mapping

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           HATEOAS ARCHITECTURE                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    API LAYER (Explore.API)                       │   │
│  │  ─────────────────────────────────────────────────────────────  │   │
│  │  • Controllers use IResourceAssembler<TDto, TResource>          │   │
│  │  • HAL responses by default (REST Level 3)                      │   │
│  │  • Prefer: return=minimal strips links (RFC 7240)               │   │
│  │  • PreferHeaderMiddleware processes opt-out                     │   │
│  │  • LinkGenerator injection for URL generation                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                              │                                          │
│                              ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              APPLICATION LAYER (Explore.Application)             │   │
│  │  ─────────────────────────────────────────────────────────────  │   │
│  │  • DTOs remain unchanged (data only)                            │   │
│  │  • Handlers return DTOs (no link knowledge)                     │   │
│  │  • ILinkPolicy<TDto> interfaces define link rules               │   │
│  │  • No infrastructure dependencies                                │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                              │                                          │
│                              ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │           INFRASTRUCTURE (Explore.Infrastructure.Hateoas)        │   │
│  │  ─────────────────────────────────────────────────────────────  │   │
│  │  • Resource assemblers (DTO → HalResource)                      │   │
│  │  • Link generators (route-aware)                                │   │
│  │  • Authorization-aware link filtering                           │   │
│  │  • HAL+JSON serialization                                       │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Key Components

#### 1. HAL Resource Model (Domain-Agnostic)

```csharp
// Explore.Application/Hateoas/HalResource.cs
namespace Explore.Application.Hateoas;

public class HalResource<T>
{
    public T Data { get; set; }

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; set; } = new();

    [JsonPropertyName("_embedded")]
    public Dictionary<string, object>? Embedded { get; set; }
}

public class HalLink
{
    public string Href { get; set; }
    public string? Method { get; set; }  // Optional: PUT, POST, DELETE
    public string? Title { get; set; }
    public bool? Templated { get; set; }
}
```

#### 2. Resource Assembler Interface (Application Layer)

```csharp
// Explore.Application/Contracts/Hateoas/IResourceAssembler.cs
namespace Explore.Application.Contracts.Hateoas;

public interface IResourceAssembler<TDto, TResource>
    where TResource : HalResource<TDto>
{
    TResource ToResource(TDto dto, HttpContext context);
    HalCollectionResource<TDto> ToCollectionResource(
        IEnumerable<TDto> dtos,
        PaginationMetadata pagination,
        HttpContext context);
}
```

#### 3. Link Policy Interface (Application Layer)

```csharp
// Explore.Application/Contracts/Hateoas/ILinkPolicy.cs
namespace Explore.Application.Contracts.Hateoas;

public interface ILinkPolicy<TDto>
{
    IEnumerable<LinkDefinition> GetLinks(TDto dto, ClaimsPrincipal? user);
}

public record LinkDefinition(
    string Rel,
    string RouteName,
    object? RouteValues = null,
    string? Method = null,
    bool RequiresAuth = false,
    string[]? RequiredRoles = null
);
```

#### 4. Prefer Header Processing (RFC 7240)

```csharp
// Explore.API/Middleware/PreferHeaderMiddleware.cs
namespace Explore.API.Middleware;

public class PreferHeaderMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Parse Prefer header
        var preferHeader = context.Request.Headers["Prefer"].FirstOrDefault();
        var returnMinimal = preferHeader?.Contains("return=minimal") ?? false;

        // Store preference in HttpContext.Items for later use
        context.Items["HateoasMinimal"] = returnMinimal;

        await next(context);

        // Add Preference-Applied header if minimal was requested
        if (returnMinimal)
        {
            context.Response.Headers["Preference-Applied"] = "return=minimal";
        }
    }
}

// Usage in ResourceAssembler
public HalResource<T> ToResource(T dto, HttpContext context)
{
    var minimal = context.Items["HateoasMinimal"] as bool? ?? false;

    var resource = new HalResource<T> { Data = dto };

    if (!minimal)
    {
        resource.Links = GenerateLinks(dto, context);
        resource.Embedded = GenerateEmbedded(dto, context);
    }

    return resource;
}
```

### IANA Link Relations Used

| Relation | Usage | RFC |
|----------|-------|-----|
| `self` | Current resource | RFC 8288 |
| `collection` | Parent collection | RFC 6573 |
| `item` | Collection item | RFC 6573 |
| `first` | First page | RFC 8288 |
| `last` | Last page | RFC 8288 |
| `prev` | Previous page | RFC 8288 |
| `next` | Next page | RFC 8288 |
| `edit` | Editable resource | RFC 5023 |
| `related` | Related resource | RFC 8288 |

### Custom Link Relations (ISLAMU-Specific)

| Relation | Usage |
|----------|-------|
| `events` | Organization's events |
| `sessions` | Event's sessions |
| `speakers` | Session's speakers |
| `registration` | Event registration action |
| `members` | Organization members |
| `agenda-items` | Session agenda |

---

## Implementation Phases

### Phase 1: Core Infrastructure (Foundation)

**Objective**: Establish base HATEOAS infrastructure without modifying existing behavior

**Effort**: L (Large)
**Duration**: 2-3 days

#### Tasks

##### Task 1.1: Create HAL Resource Models
- **File**: `Explore.Application/Hateoas/HalResource.cs`
- **File**: `Explore.Application/Hateoas/HalLink.cs`
- **File**: `Explore.Application/Hateoas/HalCollectionResource.cs`
- **Acceptance Criteria**:
  - [ ] Generic HalResource<T> with _links and _embedded
  - [ ] HalLink with href, method, title, templated
  - [ ] HalCollectionResource for paginated responses
  - [ ] JSON serialization with correct property names (_links, _embedded)
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

##### Task 1.2: Create Link Definition Models
- **File**: `Explore.Application/Hateoas/LinkDefinition.cs`
- **File**: `Explore.Application/Hateoas/LinkRelations.cs` (constants)
- **Acceptance Criteria**:
  - [ ] LinkDefinition record with Rel, RouteName, RouteValues
  - [ ] IANA link relation constants (self, collection, next, prev, etc.)
  - [ ] Custom relation constants (events, sessions, members)
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

##### Task 1.3: Create Resource Assembler Interfaces
- **File**: `Explore.Application/Contracts/Hateoas/IResourceAssembler.cs`
- **File**: `Explore.Application/Contracts/Hateoas/ILinkPolicy.cs`
- **File**: `Explore.Application/Contracts/Hateoas/ILinkGenerator.cs`
- **Acceptance Criteria**:
  - [ ] IResourceAssembler<TDto, TResource> interface
  - [ ] ILinkPolicy<TDto> for link determination logic
  - [ ] IHateoasLinkGenerator abstraction over ASP.NET LinkGenerator
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`, `cqrs-mediatr-guidelines`

##### Task 1.4: Create Base Resource Assembler Implementation
- **File**: `Explore.API/Hateoas/ResourceAssemblerBase.cs`
- **Acceptance Criteria**:
  - [ ] Abstract base with common link generation logic
  - [ ] Integration with ASP.NET Core LinkGenerator
  - [ ] Authorization-aware link filtering
  - [ ] Support for conditional links based on resource state
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

##### Task 1.5: Create Prefer Header Middleware (RFC 7240)
- **File**: `Explore.API/Middleware/PreferHeaderMiddleware.cs`
- **File**: `Explore.API/Extensions/PreferHeaderExtensions.cs`
- **Acceptance Criteria**:
  - [ ] Parse `Prefer: return=minimal` header
  - [ ] Store preference in `HttpContext.Items`
  - [ ] Add `Preference-Applied` response header when honored
  - [ ] Support `return=representation` (explicit full response)
  - [ ] Default behavior: full HAL response with links
- **Effort**: M
- **Related Skills**: None (API layer specific)

##### Task 1.6: Register HATEOAS Services
- **File**: `Explore.API/Extensions/HateoasServiceExtensions.cs`
- **File**: `Explore.API/Program.cs` (minimal changes)
- **Acceptance Criteria**:
  - [ ] Extension method `AddHateoas()` for service registration
  - [ ] Extension method `UseHateoas()` for middleware pipeline
  - [ ] PreferHeaderMiddleware registered in pipeline
  - [ ] Resource assemblers registered with DI (scoped lifetime)
  - [ ] HAL is default response format for all endpoints
- **Effort**: S
- **Related Skills**: None

---

### Phase 2: Entity-Specific Assemblers (Core Entities)

**Objective**: Implement resource assemblers for primary business entities

**Effort**: XL (Extra Large)
**Duration**: 3-4 days

#### Tasks

##### Task 2.1: Organization Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/OrganizationResourceAssembler.cs`
- **File**: `Explore.API/Hateoas/Policies/OrganizationLinkPolicy.cs`
- **Acceptance Criteria**:
  - [ ] Links: self, collection, events, members, update (auth), delete (admin)
  - [ ] Embedded: actor (optional)
  - [ ] Authorization-aware (update only if member, delete only if admin)
  - [ ] Supports both OrganizationDto and OrganizationListDto
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

##### Task 2.2: Event Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/EventResourceAssembler.cs`
- **File**: `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- **Acceptance Criteria**:
  - [ ] Links: self, collection, sessions, categories, tags, organization
  - [ ] Links: registration (if open), update (if owner), delete (if owner/admin)
  - [ ] State-driven: published events don't show "publish" link
  - [ ] Embedded: sessions, categories (optional for detail view)
- **Effort**: L
- **Related Skills**: `clean-architecture-rules`

##### Task 2.3: Event Session Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/EventSessionResourceAssembler.cs`
- **File**: `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
- **Acceptance Criteria**:
  - [ ] Links: self, event (parent), speakers, agenda-items, location
  - [ ] Links: registration (session-level), update, delete
  - [ ] Embedded: speakers, location (optional)
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

##### Task 2.4: Actor Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/ActorResourceAssembler.cs`
- **File**: `Explore.API/Hateoas/Policies/ActorLinkPolicy.cs`
- **Acceptance Criteria**:
  - [ ] Links: self, collection, events (as organizer), sessions (as speaker)
  - [ ] Links based on actor type (user, organization)
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

##### Task 2.5: Location Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/LocationResourceAssembler.cs`
- **Acceptance Criteria**:
  - [ ] Links: self, collection, events-at-location
  - [ ] Minimal link policy (locations are simple)
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

##### Task 2.6: Category Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/CategoryResourceAssembler.cs`
- **Acceptance Criteria**:
  - [ ] Links: self, collection, parent (if subcategory), children, events
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

##### Task 2.7: Tag Resource Assembler
- **File**: `Explore.API/Hateoas/Assemblers/TagResourceAssembler.cs`
- **Acceptance Criteria**:
  - [ ] Links: self, collection, events (with this tag), tag-type
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

---

### Phase 3: Controller Integration

**Objective**: Update controllers to return HAL resources while maintaining backward compatibility

**Effort**: L (Large)
**Duration**: 2-3 days

#### Tasks

##### Task 3.1: Create HateoasControllerBase
- **File**: `Explore.API/Controllers/HateoasControllerBase.cs`
- **Acceptance Criteria**:
  - [ ] Base controller with helper methods for HAL responses
  - [ ] Content negotiation helper (HAL vs JSON)
  - [ ] Consistent response formatting
- **Effort**: M
- **Related Skills**: None

##### Task 3.2: Update OrganizationController
- **File**: `Explore.API/Controllers/OrganizationController.cs`
- **Acceptance Criteria**:
  - [ ] Inject IResourceAssembler<OrganizationDto>
  - [ ] Return HAL resources for Accept: application/hal+json
  - [ ] Maintain JSON compatibility for Accept: application/json
  - [ ] All endpoints support HATEOAS
- **Effort**: M
- **Related Skills**: None

##### Task 3.3: Update EventController
- **File**: `Explore.API/Controllers/EventController.cs`
- **Acceptance Criteria**:
  - [ ] HAL resources with session links
  - [ ] Paginated collection with navigation links
  - [ ] State-driven links (draft vs published)
- **Effort**: M
- **Related Skills**: None

##### Task 3.4: Update EventSessionController
- **File**: `Explore.API/Controllers/EventSessionController.cs`
- **Acceptance Criteria**:
  - [ ] HAL resources with parent event link
  - [ ] Speaker and agenda item links
- **Effort**: M
- **Related Skills**: None

##### Task 3.5: Update Remaining Controllers
- **Files**: All remaining controllers
- **Acceptance Criteria**:
  - [ ] ActorController
  - [ ] LocationController
  - [ ] CategoryController
  - [ ] TagController
  - [ ] All lookup table controllers (minimal links)
- **Effort**: L
- **Related Skills**: None

---

### Phase 4: Pagination Enhancement

**Objective**: Standardize pagination with HATEOAS navigation links

**Effort**: M (Medium)
**Duration**: 1-2 days

#### Tasks

##### Task 4.1: Create PaginatedHalResource
- **File**: `Explore.Application/Hateoas/PaginatedHalResource.cs`
- **Acceptance Criteria**:
  - [ ] Extends HalCollectionResource with pagination metadata
  - [ ] Auto-generates first, prev, next, last links
  - [ ] Handles edge cases (first page, last page)
- **Effort**: M
- **Related Skills**: `clean-architecture-rules`

##### Task 4.2: Create Pagination Link Generator
- **File**: `Explore.API/Hateoas/PaginationLinkGenerator.cs`
- **Acceptance Criteria**:
  - [ ] Generates pagination links from PaginatedResult
  - [ ] Preserves query parameters (filters, sorting)
  - [ ] RFC 8288 compliant link relations
- **Effort**: M
- **Related Skills**: None

##### Task 4.3: Update PaginatedResult<T>
- **File**: `Explore.Application/Responses/PaginatedResult.cs`
- **Acceptance Criteria**:
  - [ ] Add HasPrevious, HasNext computed properties
  - [ ] Add helper methods for pagination calculations
  - [ ] No breaking changes to existing usage
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

---

### Phase 5: Documentation & OpenAPI

**Objective**: Document HATEOAS responses in OpenAPI/Scalar

**Effort**: M (Medium)
**Duration**: 1-2 days

#### Tasks

##### Task 5.1: Create HAL Schema Examples
- **File**: `Explore.API/OpenApi/HalSchemaFilter.cs`
- **Acceptance Criteria**:
  - [ ] OpenAPI schema filter for HAL resources
  - [ ] Example responses with _links and _embedded
  - [ ] Document custom media type
- **Effort**: M
- **Related Skills**: None

##### Task 5.2: Update API Documentation
- **File**: `docs/API.md`
- **Acceptance Criteria**:
  - [ ] Document HATEOAS usage
  - [ ] Link relation reference
  - [ ] Client integration guide
- **Effort**: S
- **Related Skills**: None

##### Task 5.3: Add Response Examples to Controllers
- **Files**: All controllers
- **Acceptance Criteria**:
  - [ ] ProducesResponseType with HAL examples
  - [ ] Document both JSON and HAL+JSON responses
- **Effort**: M
- **Related Skills**: None

---

### Phase 6: Testing & Validation

**Objective**: Comprehensive testing of HATEOAS implementation

**Effort**: L (Large)
**Duration**: 2-3 days

#### Tasks

##### Task 6.1: Unit Tests for Resource Assemblers
- **File**: `Explore.Application.UnitTests/Hateoas/`
- **Acceptance Criteria**:
  - [ ] Test link generation for each assembler
  - [ ] Test authorization-based link filtering
  - [ ] Test state-driven link visibility
- **Effort**: L
- **Related Skills**: None

##### Task 6.2: Unit Tests for Link Policies
- **File**: `Explore.Application.UnitTests/Hateoas/Policies/`
- **Acceptance Criteria**:
  - [ ] Test each link policy independently
  - [ ] Test role-based link visibility
- **Effort**: M
- **Related Skills**: None

##### Task 6.3: Integration Tests
- **File**: `Explore.API.IntegrationTests/Hateoas/`
- **Acceptance Criteria**:
  - [ ] Test HAL content negotiation
  - [ ] Test pagination links
  - [ ] Test embedded resources
  - [ ] Verify RFC 8288 compliance
- **Effort**: L
- **Related Skills**: None

##### Task 6.4: Manual API Testing
- **File**: N/A (Scalar/Postman)
- **Acceptance Criteria**:
  - [ ] Test all endpoints with HAL Accept header
  - [ ] Verify link URLs are correct and functional
  - [ ] Test navigation through links only
- **Effort**: M
- **Related Skills**: None

---

## Risk Assessment

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Increased payload size | Medium | Low | `Prefer: return=minimal` opt-out for bandwidth-sensitive clients |
| Performance overhead | Low | Medium | Lazy link generation, link template caching |
| Complexity in CQRS | Medium | Medium | Keep DTOs pure, links added at API layer only |
| Route changes break links | Low | High | Named routes, centralized route constants |
| Client confusion with new format | Low | Medium | Clear documentation, OpenAPI examples |

### Mitigation Strategies

1. **Payload Size**: Clients needing minimal responses use RFC 7240 `Prefer: return=minimal`. Default includes links for discoverability.

2. **Performance**: Link generation is lightweight (string concatenation). Consider caching link templates for high-traffic endpoints.

3. **Maintainability**: Centralize link definitions in policies. Use route names, not hardcoded paths. Single response shape to maintain.

4. **Testing**: Single response format means simpler testing. Unit tests for assemblers, integration tests for link correctness.

5. **Client Migration**: Document the change. Links are additive - existing clients can ignore `_links` and `_embedded` if not needed.

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| API Discoverability | Client can navigate full API via links only | Manual testing |
| REST Maturity | Level 3 (Hypermedia Controls) on all endpoints | Audit checklist |
| Prefer Header Support | `return=minimal` strips all links | Integration tests |
| Test Coverage | 80%+ for HATEOAS code | Code coverage report |
| Documentation | All endpoints documented with HAL examples | OpenAPI spec |
| Performance | <5ms link generation overhead per request | Load testing |
| Single Response Shape | 1 format to maintain (not 2) | Code review |

---

## Dependencies

### External Libraries

| Library | Purpose | Version |
|---------|---------|---------|
| System.Text.Json | JSON serialization | Built-in |
| Microsoft.AspNetCore.Mvc | LinkGenerator, OutputFormatter | Built-in |

**Note**: No external HATEOAS libraries (like RiskFirst.Hateoas) to minimize dependencies and maintain full control.

### Internal Dependencies

- `Explore.Application` DTOs and response models
- `Explore.API` controllers and routing
- Authorization infrastructure (for link filtering)

---

## Effort Summary

| Phase | Effort | Duration |
|-------|--------|----------|
| Phase 1: Core Infrastructure | L | 2-3 days |
| Phase 2: Entity Assemblers | XL | 3-4 days |
| Phase 3: Controller Integration | L | 2-3 days |
| Phase 4: Pagination | M | 1-2 days |
| Phase 5: Documentation | M | 1-2 days |
| Phase 6: Testing | L | 2-3 days |
| **Total** | **XL** | **11-17 days** |

---

## References

- [RFC 8288 - Web Linking](https://datatracker.ietf.org/doc/html/rfc8288)
- [HAL Specification](https://stateless.group/hal_specification.html)
- [IANA Link Relations Registry](https://www.iana.org/assignments/link-relations/link-relations.xhtml)
- [ASP.NET Core LinkGenerator](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing)
- [REST Maturity Model (Richardson)](https://martinfowler.com/articles/richardsonMaturityModel.html)

---

## Appendix A: Link Relations Reference

### IANA Standard Relations

```
self        - The target IRI is a resource equivalent to the context IRI
collection  - The target represents a collection of which the context is a member
item        - The target IRI points to a resource that is a member of the collection
first       - The first resource in a series of resources
last        - The last resource in a series of resources
prev        - The previous resource in an ordered series
next        - The next resource in an ordered series
edit        - The target can be used to edit the context resource
related     - The target IRI is related to the context IRI
```

### Custom Relations (ISLAMU)

```
events          - Events belonging to organization/actor
sessions        - Sessions belonging to event
speakers        - Speakers for a session
agenda-items    - Agenda items for a session
members         - Members of organization
registration    - Registration action for event/session
categories      - Categories assigned to event
tags            - Tags assigned to event
location        - Location of session/event
organization    - Parent organization
```

---

## Appendix B: Example Responses

### Single Resource

```json
GET /api/event/550e8400-e29b-41d4-a716-446655440000
Accept: application/hal+json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Ramadan Conference 2026",
  "description": "Annual Ramadan gathering",
  "eventStatusId": 2,
  "eventStatusName": "Published",
  "_links": {
    "self": {
      "href": "/api/event/550e8400-e29b-41d4-a716-446655440000"
    },
    "collection": {
      "href": "/api/event"
    },
    "sessions": {
      "href": "/api/event/550e8400-e29b-41d4-a716-446655440000/sessions"
    },
    "organization": {
      "href": "/api/organization/123e4567-e89b-12d3-a456-426614174000"
    },
    "edit": {
      "href": "/api/event/550e8400-e29b-41d4-a716-446655440000",
      "method": "PUT"
    }
  },
  "_embedded": {
    "sessions": [
      {
        "id": "...",
        "title": "Opening Ceremony",
        "_links": {
          "self": { "href": "/api/eventsession/..." }
        }
      }
    ]
  }
}
```

### Collection Resource

```json
GET /api/event?pageNumber=1&pageSize=10
Accept: application/hal+json

{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5,
  "_links": {
    "self": { "href": "/api/event?pageNumber=1&pageSize=10" },
    "first": { "href": "/api/event?pageNumber=1&pageSize=10" },
    "next": { "href": "/api/event?pageNumber=2&pageSize=10" },
    "last": { "href": "/api/event?pageNumber=5&pageSize=10" },
    "create": { "href": "/api/event", "method": "POST" }
  },
  "_embedded": {
    "items": [
      {
        "id": "...",
        "title": "Event 1",
        "_links": { "self": { "href": "/api/event/..." } }
      }
    ]
  }
}
```

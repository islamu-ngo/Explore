# External API Access with Tenant-Aware API Keys - PRD

## Introduction/Overview

The external API access feature will enable third-party applications and programmatic access to event data through tenant-aware API keys. This allows for integrations, automation, and data export while maintaining security and tenant isolation in single-tenant deployments.

## Goals

- Enable secure external access to event platform data for integrations and automation
- Support full CRUD operations for events, organizations, and related entities
- Implement combination rate limiting (per-key and tenant/global levels)
- Provide instance-admin visibility and management of API keys
- Ensure role/scoped authorization for API operations

## User Stories

### As an instance admin, I want to create and manage API keys so that I can control external access to my tenant's data.
**Acceptance Criteria:**
- Admin can create API keys with custom scopes and descriptions
- Admin can view list of active keys with usage statistics
- Admin can revoke or regenerate keys
- Admin can set rate limits per key
- Verify in browser using dev-browser skill

### As a developer, I want to use API keys to perform CRUD operations on events so that I can integrate with the platform.
**Acceptance Criteria:**
- API endpoints accept API key authentication in headers
- Full CRUD operations available for events, organizations, sessions
- Proper error responses for invalid keys or insufficient permissions
- API documentation available via Swagger
- Verify API calls work with curl/postman

### As a tenant owner, I want rate limits on API keys so that I can prevent abuse and control costs.
**Acceptance Criteria:**
- Configurable rate limits per API key (requests per minute/hour)
- Global rate limits for all API usage
- Clear error responses when limits exceeded
- Usage metrics tracked and visible to admins
- Verify rate limiting works with automated tests

### As a security auditor, I want API access to be properly logged so that I can monitor for suspicious activity.
**Acceptance Criteria:**
- All API calls logged with key ID, endpoint, timestamp
- Failed authentication attempts logged
- Rate limit violations logged
- Logs integrated with existing Serilog/OpenTelemetry setup
- Verify logs appear in monitoring dashboard

## Functional Requirements

1. API key entity with tenant association, scopes, and rate limit settings
2. Authentication middleware for API key validation and user context setup
3. Tenant isolation ensuring keys only access their tenant's data
4. Rate limiting middleware with per-key and global policies
5. Role-based authorization checks for API operations
6. RESTful API endpoints following existing HATEOAS patterns
7. Admin UI for key management in Blazor interface
8. Comprehensive API documentation and OpenAPI spec
9. Usage analytics and metrics collection
10. Error handling and proper HTTP status codes

## Non-Goals

- Multi-tenant API key management (single tenant scope only)
- Real-time streaming or webhook APIs
- Third-party OAuth flows or complex authentication schemes
- API versioning beyond initial implementation
- Mobile SDKs or client libraries

## Design Considerations

- Follow existing Clean Architecture and CQRS patterns
- Use HATEOAS for API responses to maintain consistency
- Integrate with current BFF authentication and tenant resolution systems
- Ensure UI follows MudBlazor conventions and BEM methodology
- Consider backward compatibility with existing admin interfaces

## Technical Considerations

- Extend existing UserAuthenticationToken entity or create new ApiKey entity
- Add ASP.NET Core rate limiting policies for per-key limits
- Ensure EF Core global query filters maintain tenant isolation
- Add structured logging hooks for API usage analytics
- Implement token forwarding through BFF for authenticated requests
- Use existing Cerbos/local authorization for role checks

## Success Metrics

- API key adoption: Number of active API keys created
- Usage volume: Average API calls per day per key
- Error rates: API error rate < 5%
- Performance: Average API response time < 500ms
- Security: Zero successful unauthorized access attempts

## Open Questions

- Which specific API endpoints should be exposed in the initial release?
- How should API versioning be handled for future changes?
- What level of granularity for API key scopes (per-endpoint vs per-resource)?
- How to handle API key rotation and backward compatibility?
- Integration points with existing monitoring and alerting systems?

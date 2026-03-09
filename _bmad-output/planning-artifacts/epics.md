---
stepsCompleted: ["step-01-validate-prerequisites"]
inputDocuments: ["dev/active/prd.md", "docs/ARCHITECTURE.md"]
---

# Explore - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Explore, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: API key entity with tenant association, scopes, and rate limit settings
FR2: Authentication middleware for API key validation and user context setup
FR3: Tenant isolation ensuring keys only access their tenant's data
FR4: Rate limiting middleware with per-key and global policies
FR5: Role-based authorization checks for API operations
FR6: RESTful API endpoints following existing HATEOAS patterns
FR7: Admin UI for key management in Blazor interface
FR8: Comprehensive API documentation and OpenAPI spec
FR9: Usage analytics and metrics collection
FR10: Error handling and proper HTTP status codes

### NonFunctional Requirements

NFR1: API key adoption: Number of active API keys created
NFR2: Usage volume: Average API calls per day per key
NFR3: Error rates: API error rate < 5%
NFR4: Performance: Average API response time < 500ms
NFR5: Security: Zero successful unauthorized access attempts

### Additional Requirements

- Follow Clean Architecture layers (Domain, Application, Persistence, API)
- Use MediatR for CQRS command/query handling
- Implement HATEOAS HAL responses with ResourceAssemblerBase
- Ensure tenant isolation via EF Core global query filters in ExploreDbContext
- Integrate with authorization pipeline (Cerbos or local provider)
- Use specification pattern for complex queries if needed
- Implement appropriate caching layers (Output Cache, HybridCache, ETag)
- Follow existing API patterns and middleware pipeline order
- Include performance monitoring via PerformanceBehavior (>500ms warnings)
- Support runtime multi-tenancy modes (SingleTenant/MultiTenant)

### FR Coverage Map

{{requirements_coverage_map}}

## Epic List

{{epics_list}}

<!-- Repeat for each epic in epics_list (N = 1, 2, 3...) -->

## Epic {{N}}: {{epic_title_N}}

{{epic_goal_N}}

<!-- Repeat for each story (M = 1, 2, 3...) within epic N -->

### Story {{N}}.{{M}}: {{story_title_N_M}}

As a {{user_type}},
I want {{capability}},
So that {{value_benefit}}.

**Acceptance Criteria:**

<!-- for each AC on this story -->

**Given** {{precondition}}
**When** {{action}}
**Then** {{expected_outcome}}
**And** {{additional_criteria}}

<!-- End story repeat -->

---
description: Create a comprehensive strategic plan with structured task breakdown
argument-hint: Describe what you need planned (e.g., "refactor authentication system", "implement event RSVP")
---

You are an elite strategic planning specialist for the ISLAMU Event platform (.NET 10 + Blazor + Clean Architecture). Create a comprehensive, actionable plan for: $ARGUMENTS

## Technology Context

Before planning, understand the stack:
- **Backend**: .NET 10, ASP.NET Core, Clean Architecture with CQRS (MediatR)
- **Frontend**: Blazor Server + WebAssembly (Hybrid), MudBlazor components
- **Database**: PostgreSQL + PostGIS (Entity Framework Core)
- **Auth**: Keycloak (OIDC/JWT) + Cerbos (Authorization)
- **Orchestration**: .NET Aspire

## Instructions

1. **Analyze the request** and determine the scope of planning needed
2. **Examine relevant files** in the codebase to understand current state
   - Review `CLAUDE.md` for project overview
   - Check `docs/ARCHITECTURE.md` for architecture details
   - Consult `docs/DOMAIN.md` for domain model
   - Reference `docs/SECURITY.md` for auth/authz patterns
   - Review `.claude/skills/` for architectural guidelines

3. **Create a structured plan** with:
   - Executive Summary
   - Current State Analysis
   - Proposed Future State
   - Implementation Phases (broken into Clean Architecture layers)
   - Detailed Tasks (actionable items with clear acceptance criteria)
   - Risk Assessment and Mitigation Strategies
   - Success Metrics
   - Required Resources and Dependencies
   - Effort Estimates

4. **Task Breakdown Structure**:
   - Each major section represents a phase or architectural layer
   - Number and prioritize tasks within sections
   - Include clear acceptance criteria for each task
   - Specify dependencies between tasks
   - Estimate effort levels (S/M/L/XL)
   - Reference relevant skills for each task (e.g., `clean-architecture-rules`, `cqrs-mediatr-guidelines`)

5. **Create task management structure**:
   - Create directory: `dev/active/[task-name]/` (relative to project root)
   - Generate three files:
     - `[task-name]-plan.md` - The comprehensive plan
     - `[task-name]-context.md` - Key files, decisions, dependencies
     - `[task-name]-tasks.md` - Checklist format for tracking progress
   - Include "Last Updated: YYYY-MM-DD" in each file

## Quality Standards
- Plans must be self-contained with all necessary context
- Use clear, actionable language
- Include specific technical details (file paths, class names, namespaces)
- Consider both technical and business perspectives
- Account for potential risks and edge cases
- Follow Clean Architecture principles (Domain → Application → Infrastructure → API/Blazor)
- Include Cerbos authorization policies if the feature involves access control
- Include EF Core migrations if database schema changes are needed
- Include unit and integration tests in task breakdown

## Context References
- **CLAUDE.md** - Project overview and quick reference
- **docs/PROJECT.md**
- **docs/ARCHITECTURE.md** - Technical architecture and stack
- **docs/DOMAIN.md** - Domain model and entities
- **docs/SECURITY.md** - Authentication and authorization
- **docs/CONFIGURATION.md** - Deployment Modes & Customization
- **docs/GOVERNANCE.md** - Code conventions and standards
**docs/OPERATIONS.md** - Deployment and maintenance procedures
- **docs/TROUBLESHOOTING.md** - Common issues to avoid
- **dev/active/README.md** - Task management guidelines
- **.claude/skills/** - Architectural guidelines and patterns
- **docs/FEDERATION.md**
- **docs/API.md**


## ISLAMU Event Specific Considerations

When planning features, always consider:
- **Multi-tenancy**: If applicable to the feature
- **Federation**: ATProto and ActivityPub compatibility
- **Cultural Filtering**: Age, gender, madhab, language filters
- **Liturgical Scheduling**: Prayer-relative event times
- **PostGIS Spatial**: Geographic queries for event discovery
- **Verification System**: Two-tier (user-submitted vs verified organizations)

## Example Task Structure

```markdown
# Plan: Implement Event RSVP System

## Executive Summary
...

## Phase 1: Domain Layer (Week 1)
### Task 1.1: Create EventRegistration Entity
- **File**: `Explore.Domain/EventRegistration.cs`
- **Acceptance Criteria**:
  - [ ] Entity has UserId, EventId, Status properties
  - [ ] Includes CancellationReason nullable property
  - [ ] Follows UUIDv7 pattern for primary key
- **Effort**: S
- **Related Skills**: `clean-architecture-rules`

### Task 1.2: Add EventRegistrationStatus Enum
...

## Phase 2: Application Layer
...

## Phase 3: Infrastructure Layer
...

## Phase 4: API Layer
...

## Phase 5: Blazor UI
...

## Phase 6: Testing & Documentation
...
```

**Note**: This command is ideal to use AFTER exiting plan mode when you have a clear vision of what needs to be done. It will create the persistent task structure that survives context resets.

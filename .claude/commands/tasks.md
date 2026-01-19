---
name: tasks
description: "Convert the approved PRD into the standardized dev/active task documentation structure"
argument-hint: (Optional) Feature name if different from PRD title
---

# Convert PRD to Dev-Docs System

Activate the **architect skill** to convert the strategic Product Requirements Document into actionable engineering tasks.

## Context
You are the Technical Lead for the ISLAMU Event platform (.NET 10 + Blazor + Clean Architecture). You are transitioning a feature from "Product Definition" (PRD) to "Engineering Execution".

## Source Material
- **Primary Source**: `dev/active/prd.md` (The approved PRD)
- **Architecture**: `docs/ARCHITECTURE.md`
- **Domain**: `docs/DOMAIN.md`

## Instructions

1. **Read and Analyze**:
   - Read `dev/active/prd.md` to understand the feature scope.
   - Determine a short kebab-case slug for the feature (e.g., `event-rsvp`, `cultural-filters`).
   - Create the directory: `dev/active/[feature-slug]/`.

2. **Generate Structure**:
   Create the three standard files based on the PRD content.

   ### A. `[feature-slug]-plan.md`
   Convert the PRD high-level requirements into a technical implementation plan.
   - **Executive Summary**: Summarize the PRD.
   - **Architecture**: How this fits into Clean Architecture (Domain -> App -> Infra -> API -> UI).
   - **Phasing**: Break the PRD user stories into logical engineering phases (e.g., "Phase 1: Core Domain Entities", "Phase 2: Use Cases", "Phase 3: UI Components").

   ### B. `[feature-slug]-context.md`
   Extract context and constraints from the PRD.
   - **Dependencies**: List required NuGet packages or external services mentioned in PRD.
   - **Key Decisions**: specific rules (e.g., "Must use PostGIS for location", "Must filter by Madhab").
   - **Reference Files**: List existing files that will be modified.

   ### C. `[feature-slug]-tasks.md`
   Convert PRD Atomic Details into a checklist.
   - **Format**: Markdown checkboxes `- [ ]`.
   - **Granularity**: Small, verifiable steps (S/M effort).
   - **Ordering**:
     1. Domain (Entities, Value Objects)
     2. Application (CQRS Commands/Queries, Validators)
     3. Infrastructure (EF Core Config, Migrations, External Services)
     4. API (Controllers/Endpoints)
     5. UI (Blazor Pages, MudBlazor Components)
     6. Tests (Unit & Integration)

3. **ISLAMU Specific Constraints**:
   Ensure the tasks reflect the project standards:
   - **Auth**: Use Cerbos policies for permissions mentioned in PRD.
   - **Data**: Use EF Core + PostGIS if location is involved.
   - **Orchestration**: Ensure `.NET Aspire` configuration is updated if new services are added.
   - **Testing**: Every major logic block must have a corresponding test task.

## Output Generation

Perform the file creation immediately.

**1. Create Directory:** `dev/active/[feature-slug]/`

**2. Create File: `dev/active/[feature-slug]/[feature-slug]-plan.md`**
   - Populate with content mapped from PRD.

**3. Create File: `dev/active/[feature-slug]/[feature-slug]-context.md`**
   - Populate with context.

**4. Create File: `dev/active/[feature-slug]/[feature-slug]-tasks.md`**
   - Populate with the actionable checklist.

## Example `[feature-slug]-tasks.md` Content

```markdown
# Tasks: [Feature Name]

## Phase 1: Domain & Logic
- [ ] Define `[EntityName]` in `Explore.Domain`
- [ ] Add domain events (e.g., `[Entity]CreatedEvent`)
- [ ] Create EF Core Configuration in `Explore.Infrastructure`
- [ ] Create Migration: `dotnet ef migrations add Add[Entity]`

## Phase 2: Application (CQRS)
- [ ] Implement `Create[Entity]Command` and Handler
- [ ] Implement `Get[Entity]ByIdQuery` and Handler
- [ ] Add Cerbos policy checks in Handlers
- [ ] Write Unit Tests for Handlers

## Phase 3: UI Implementation
- [ ] Create `[Entity]Card.razor` component
- [ ] Integrate with `Explore.Blazor` router
- [ ] Verify responsive design on mobile

## Phase 4: Verification
- [ ] Run full test suite
- [ ] Manual verification of user flow

**Important** : .claude\commands\dev-docs.md is the file you should read to grab additional context and guidelines for creating detailed strategic plans and task breakdowns.
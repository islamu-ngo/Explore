# Tasks: Documentation Refactor

**Last Updated: 2026-01-15**

## Phase 1: Inventory & Detailed Analysis ⏳

- [ ] **Task 1.1: Content Inventory**
    - [ ] Recursively list all markdown files in `docs/`, `.claude/skills/`, and `.claude/agents/`.
    - [ ] Create a summary table mapping each file to its purpose.
    - [ ] Identify content that is redundant, outdated, or needs to be moved.

- [ ] **Task 1.2: Document Frontend Conventions**
    - [ ] Analyze MudBlazor component usage and document common patterns.
    - [ ] Investigate and document the project's theming strategy.
    - [ ] Search for and document any evidence of BEAM methodology in CSS/SCSS files.
    - [ ] Create a new skill named `blazor-ui-conventions` to house this information.

- [ ] **Task 1.3: Document Security Patterns**
    - [ ] Investigate controller and handler code to find the standard pattern for user ID extraction from JWT claims.
    - [ ] Document this pattern in a new `auth-patterns` skill.
    - [ ] Update `docs/SECURITY.md` with a high-level overview and a link to the new skill.

## Phase 2: Refactor High-Level Documentation (`/docs`) ⏳

- [ ] **Task 2.1: Consolidate Architecture Documents**
    - [ ] Merge content from `ARCHITECTURE.md`, `BLAZOR.md`, and `API.md` into a single, updated `ARCHITECTURE.md`.
    - [ ] Create Mermaid.js diagrams for the BFF auth flow and the CQRS command flow.
    - [ ] Remove detailed code examples, replacing them with links to the relevant skills.

- [ ] **Task 2.2: Refine `DOMAIN.md`**
    - [ ] Review `DOMAIN.md` and ensure it focuses only on the conceptual model and business rules.
    - [ ] Create a new `dotnet-efcore-guidelines` skill.
    - [ ] Move all EF Core and persistence-related implementation details from `DOMAIN.md` to the new skill.

- [ ] **Task 2.3: Refine `SECURITY.md`**
    - [ ] Review `SECURITY.md` to ensure it focuses on high-level concepts and threat models.
    - [ ] Move detailed implementation code (like JWT claim extraction) to the `auth-patterns` skill.

## Phase 3: Refactor Developer & AI Skills (`/.claude/skills`) ⏳

- [ ] **Task 3.1: Create Foundational Skills**
    - [ ] Create the `clean-architecture-rules` skill.
    - [ ] Create the `cqrs-mediatr-guidelines` skill.
    - [ ] Create the `blazor-bff-patterns` skill.

- [ ] **Task 3.2: Consolidate and Refine Existing Skills**
    - [ ] Go through the content inventory from Phase 1.
    - [ ] Merge skills with overlapping content.
    - [ ] Archive or delete outdated skills.
    - [ ] Move large code examples into the `resources` folder for the relevant skill.
    - [ ] Ensure all skills are self-contained and focused on a single domain.

## Phase 4: Refactor AI Agents (`/.claude/agents`) ⏳

- [ ] **Task 4.1: Review and Update Agents**
    - [ ] Audit each agent in the `/.claude/agents/` directory.
    - [ ] Update their instructions to be more concise.
    - [ ] Replace detailed inline explanations with references to the newly created/refactored skills.

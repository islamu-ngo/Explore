# Plan: Documentation Refactoring

**Last Updated: 2026-01-15**

## 1. Executive Summary

This plan outlines a phased approach to refactor the project's entire documentation suite (`docs/`, `.claude/skills/`, `.claude/agents/`). The objective is to create an enterprise-grade, maintainable, and clear documentation structure that is perfectly aligned with the codebase. The new structure will separate high-level, conceptual documentation from detailed implementation guides, improving usability for both human developers and AI agents.

## 2. Current State Analysis

The current documentation is spread across several locations with overlapping concerns. High-level architecture documents in `docs/` contain large code blocks better suited for implementation guides, while skills and agents may lack references to a central source of truth. This refactoring will address this by creating a clear, hierarchical documentation system.

## 3. Proposed Documentation Architecture

*   **`/docs`**: **High-Level & Conceptual Documentation.**
    *   **Audience**: New developers, architects, project managers.
    *   **Content**: Architectural diagrams (C4 style, data flow), core principles, key design decisions, and conventions. Code examples will be minimal (1-3 lines) and for illustrative purposes only. Files will be concise and focused.
    *   **Goal**: Provide a quick and comprehensive understanding of the *why* and *what* of the project.

*   **`/.claude/skills`**: **Detailed Implementation Guides & Patterns.**
    *   **Audience**: Developers writing code, AI agents performing implementation tasks.
    *   **Content**: Deep dives into specific patterns (e.g., CQRS handler structure, Blazor component patterns, repository implementation). Will be rich with detailed, correct code examples, anti-patterns, and best practices.
    *   **Goal**: Provide a "living cookbook" of patterns that enforce consistency and quality.

*   **`/.claude/agents`**: **Autonomous Task Definitions.**
    *   **Audience**: AI agents.
    *   **Content**: Clear, step-by-step instructions for executing complex, multi-step tasks. These will be updated to reference the refactored skills as their knowledge base.
    *   **Goal**: Enable reliable, autonomous execution of complex engineering tasks.

## 4. Refactoring Phases & Task Breakdown

### **Phase 1: Inventory & Detailed Analysis**

This phase will fill the gaps from the initial investigation and create a complete map of the existing documentation.

*   **Task 1.1: Content Inventory**
    *   **Action**: Recursively list and summarize every markdown file in `docs/`, `.claude/skills/`, and `.claude/agents/`.
    *   **Output**: A markdown table mapping file paths to their purpose and identifying content to be moved, merged, or deleted.
    *   **Effort**: Medium

*   **Task 1.2: Document Frontend Conventions**
    *   **Action**: Investigate and document MudBlazor usage, component library structure, and theming strategy.
    *   **Action**: Search for and document the BEAM methodology in `.css`/`.scss` files within `Explore.Blazor.Client`.
    *   **Output**: A new skill `blazor-ui-conventions` containing this information.
    *   **Effort**: Medium

*   **Task 1.3: Document Security Patterns**
    *   **Action**: Confirm and document the standard pattern for user ID extraction from JWT claims in controllers.
    *   **Output**: Update the `docs/SECURITY.md` file and create a new, detailed `auth-patterns` skill.
    *   **Effort**: Small

### **Phase 2: Refactor High-Level Documentation (`/docs`)**

This phase focuses on making the `/docs` directory a source of high-level, conceptual knowledge.

*   **Task 2.1: Consolidate Architecture Documents**
    *   **Action**: Merge `ARCHITECTURE.md`, `BLAZOR.md`, and `API.md` into a single, authoritative `ARCHITECTURE.md`.
    *   **Action**: Create high-level diagrams (using Mermaid.js) for the BFF pattern, auth flow, and CQRS data flow.
    *   **Action**: Move all detailed code examples and implementation patterns to new or existing skills.
    *   **Effort**: Large

*   **Task 2.2: Refine `DOMAIN.md`**
    *   **Action**: Update `DOMAIN.md` to focus on the conceptual domain model, business rules, and entity relationships.
    *   **Action**: Move EF Core implementation details to a new `dotnet-efcore-guidelines` skill.
    *   **Effort**: Medium

*   **Task 2.3: Refine `SECURITY.md`**
    *   **Action**: Update `SECURITY.md` to focus on the high-level security posture, threat model, and authentication/authorization concepts.
    *   **Action**: Move implementation details (e.g., JWT claim extraction code, OIDC setup) to the `auth-patterns` skill.
    *   **Effort**: Medium

### **Phase 3: Refactor Developer & AI Skills (`/.claude/skills`)**

This phase will build out the detailed "cookbook" of implementation patterns.

*   **Task 3.1: Create Foundational Skills**
    *   **Action**: Create `clean-architecture-rules`: The definitive guide to dependency rules and layer responsibilities.
    *   **Action**: Create `cqrs-mediatr-guidelines`: Detailed patterns for Commands, Queries, Handlers, and the manual validation strategy.
    *   **Action**: Create `blazor-bff-patterns`: Explain the YARP proxy, token forwarding, and cookie management in detail.
    *   **Effort**: Large

*   **Task 3.2: Consolidate and Refine Existing Skills**
    *   **Action**: Review all existing skills. Merge duplicates, archive outdated information, and align them with the new documentation structure.
    *   **Action**: Move all large code examples from the old `docs/` files into the `resources` folder of the most relevant skill, and reference them from the skill's `SKILL.md`.
    *   **Effort**: Large

### **Phase 4: Refactor AI Agents (`/.claude/agents`)**

This phase ensures the autonomous agents are aligned with the new, structured documentation.

*   **Task 4.1: Review and Update Agents**
    *   **Action**: Audit each agent file (e.g., `code-refactor-master`, `blazor-component-architect`).
    *   **Action**: Update their instructions to be more concise and to explicitly reference the refactored skills as their primary source of truth for patterns and conventions.
    *   **Example**: Instead of explaining the repository pattern inside the agent, instruct it to "Follow the repository pattern as defined in the `dotnet-efcore-guidelines` skill."
    *   **Effort**: Medium

## 5. Maintainability & Governance

*   **Documentation as Code**: The documentation will be treated as a first-class citizen of the codebase.
*   **Pull Request Checklist**: An item will be added to the PR template: "Have you updated relevant documentation (`/docs` or `/.claude/skills`) for your changes?"
*   **Ownership**: A `CODEOWNERS` file will be considered to assign ownership of documentation sections to relevant teams or individuals.

# Agent Architecture Modernization & Schema Governance Plan

Modernize, refactor, and govern the subagent portfolio in `.agents/agents/` to enforce Clean Architecture, .NET 10 CQRS/EF Core invariants, Blazor BFF HAL link affordance gating, IP Clean Room rules, and enterprise self-hosting readiness.

## User Review Required

> [!IMPORTANT]
> **Complete Breaking Refactor of Subagent Infrastructure (Pre-v1 Development Mode)**
> - We reject legacy v1 migration drafts and obsolete `.claude/` path references entirely.
> - The canonical subagent portfolio is consolidated into **5 role-scoped domain agents**: `architect-agent`, `backend-engineer-agent`, `presentation-engineer-agent`, `quality-verifier-agent`, and `librarian-agent`.
> - `_AGENT_SCHEMA.md` is updated to be the sole, authoritative contract for `.agents/agents/*.md`, enforcing strict YAML frontmatter enums, required 10-section structure, line limits (50–120 target, 160 max), and zero duplication of repository invariants.
> - All root references (`AGENTS.md`, `docs/index.md`, `.agents/contract/schema.json`, `.agents/skills/_SKILL_SCHEMA.md`) are updated to point to `.agents/agents/`.

## Proposed Changes

### Phase 1: Subagent Schema & Infrastructure Governance

#### [MODIFY] [_AGENT_SCHEMA.md](file:///home/amir/ISLAMU/Github/Event/.agents/agents/_AGENT_SCHEMA.md)
- Update canonical directory location from `.claude/agents/*.md` to `.agents/agents/<kebab-case-name>.md`.
- Update Section 7 (Migration Scope) to deprecate and purge the 13 old v1 agents, establishing the 5 canonical role subagents as the active registry.
- Update cross-references to point to `.agents/contract/README.md` and `.agents/skills/_SKILL_SCHEMA.md`.

#### [MODIFY] [README.md](file:///home/amir/ISLAMU/Github/Event/.agents/agents/README.md)
- Update the Agent Selection Guide to accurately reflect the 5 active role subagents, their mandatory reads, allowed tools, and non-overlapping domain responsibilities.
- Add strict usage policies: mandatory file read before invocation, single-agent file mutation rule, and explicit skill vs agent boundaries.

---

### Phase 2: Role-Scoped Subagent Overhaul

#### [MODIFY] [architect-agent.md](file:///home/amir/ISLAMU/Github/Event/.agents/agents/architect-agent.md)
- Set frontmatter: `type: implementation`, `enforcement: suggest`, `priority: critical`, `tools: Read, Write, Edit, Bash, Glob, Grep`.
- Mandatory Reads: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/ARCHITECTURE.md`, `docs/GOVERNANCE.md`, `docs/adr/`, `docs/legal/IP_GOVERNANCE.md`, `.agents/skills/ip-clean-room/SKILL.md`.
- Purpose & Scope: System boundary design, CQRS/MediatR architectural alignment, Aspire resource orchestration, ADR creation, transactional outbox boundaries, and IP clean-room verification.
- Forbidden Moves: Generating raw implementation code directly, introducing circular dependencies, violating clean room IP governance, bypassing `dev/active/` 3-file dev-docs workstream pattern.

#### [MODIFY] [backend-engineer-agent.md](file:///home/amir/ISLAMU/Github/Event/.agents/agents/backend-engineer-agent.md)
- Set frontmatter: `type: implementation`, `enforcement: suggest`, `priority: high`, `tools: Read, Write, Edit, Bash, Glob, Grep`.
- Mandatory Reads: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `.agents/rules/application-layer.md`, `.agents/rules/domain.md`, `.agents/rules/efcore-persistence.md`, `.agents/rules/efcore-migrations.md`.
- Technical Invariants Enforced:
  - Repositories return domain entities, never DTOs (mapping in handlers).
  - Validators manually instantiated (no DI injection of `IValidator<T>`).
  - Immutable specification builder pattern (`EventQuerySpecification`).
  - `Guid` (UUIDv7) for core aggregates, `int` for lookups, `long` for cursors.
  - Soft-delete query filter preservation (`QueryFilterNames.SoftDelete`).
  - Transactional Outbox pattern for asynchronous side-effects.

#### [MODIFY] [presentation-engineer-agent.md](file:///home/amir/ISLAMU/Github/Event/.agents/agents/presentation-engineer-agent.md)
- Set frontmatter: `type: implementation`, `enforcement: suggest`, `priority: high`, `tools: Read, Write, Edit, Bash, Glob, Grep`.
- Mandatory Reads: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/ACCESSIBILITY.md`, `.agents/rules/api-controllers.md`, `.agents/rules/api-hateoas.md`, `.agents/rules/blazor-client.md`, `.agents/rules/blazor-server.md`.
- Technical Invariants Enforced:
  - **HAL link presence is single source of truth for UI affordances** (`_links` presence check, never local role/claim checks).
  - `RouteNames` constant usage for HATEOAS policies (`yield return` pattern).
  - MudBlazor v9 wrapper controls (`design-system` skill).
  - Blazor client fully isolated from backend implementation assemblies (communicates only via generated `IEventApiClient`).
  - BFF token forwarding & HttpOnly cookie trust boundaries.

#### [MODIFY] [quality-verifier-agent.md](file:///home/amir/ISLAMU/Github/Event/.agents/agents/quality-verifier-agent.md)
- Set frontmatter: `type: diagnostic`, `enforcement: inform`, `priority: high`, `tools: Read, Bash, Glob, Grep`.
- Mandatory Reads: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/TESTING.md`, `docs/OPERATIONS.md`, `.agents/rules/tests.md`.
- Technical Invariants Enforced:
  - Verification via TUnit test framework.
  - Architecture test compliance (`Event.Architecture.Tests`).
  - Release profile build verification (`dotnet build --configuration Release --verbosity quiet`).
  - Forbidden: Swallowing exceptions, deleting/disabling failing tests, patching symptoms without root-cause analysis.

#### [MODIFY] [librarian-agent.md](file:///home/amir/ISLAMU/Github/Event/.agents/agents/librarian-agent.md)
- Set frontmatter: `type: research`, `enforcement: inform`, `priority: medium`, `tools: Read, Write, Edit, Bash, Glob, Grep`.
- Mandatory Reads: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/index.md`, `dev/_journal/README.md`, `docs/DOCUMENTATION_STYLE_GUIDE.md`, `docs/legal/IP_GOVERNANCE.md`, `docs/AI_AGENT_CONTRACT_INVENTORY.md`.
- Technical Invariants Enforced:
  - Clean room research compliance (no third-party source ingestion).
  - Mandatory 2-line `ABOUTME:` comments on all new markdown files.
  - Promotion of durable insights into `dev/_journal/journal.md`.

---

### Phase 3: Repository Root & System Contract Alignment

#### [MODIFY] [AGENTS.md](file:///home/amir/ISLAMU/Github/Event/AGENTS.md)
- Update Section 9 path from `.claude/agents/README.md` to `.agents/agents/README.md`.
- Update Section 1 links for Contribution Contract to `.agents/contract/intents.yaml`.

#### [MODIFY] [docs/index.md](file:///home/amir/ISLAMU/Github/Event/docs/index.md)
- Update line 79 reference from `.claude/agents/_AGENT_SCHEMA.md` to `.agents/agents/_AGENT_SCHEMA.md`.

#### [MODIFY] [.agents/skills/_SKILL_SCHEMA.md](file:///home/amir/ISLAMU/Github/Event/.agents/skills/_SKILL_SCHEMA.md)
- Update line 155 reference from `.claude/agents/_AGENT_SCHEMA.md` to `.agents/agents/_AGENT_SCHEMA.md`.

#### [MODIFY] [.agents/contract/schema.json](file:///home/amir/ISLAMU/Github/Event/.agents/contract/schema.json)
- Update pattern definitions referencing `.claude/agents/` to `.agents/agents/`.

---

## Verification Plan

### Automated Verification
1. **Schema Structure Validation**:
   - Verify every agent file in `.agents/agents/*.md` adheres to all 10 required sections in order.
   - Verify line counts are between 50–120 lines (hard max 160).
   - Verify frontmatter `type` is one of `diagnostic | review | implementation | domain | research`.
   - Verify tool whitelists match allowed capabilities.
2. **Link Integrity Check**:
   - Verify every markdown link in `Mandatory Reads` resolves to an existing file in the repository.
3. **Repository Build Baseline**:
   - Run Release build to confirm zero compilation breakages:
     ```bash
     dotnet build --configuration Release --verbosity quiet
     ```

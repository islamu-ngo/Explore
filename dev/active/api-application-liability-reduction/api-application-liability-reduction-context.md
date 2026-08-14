<!-- ABOUTME: Resume context for the API-wide code-liability reduction program. -->
<!-- ABOUTME: Records audited hotspots, architecture decisions, blockers, and the next executable slice. -->

# API-Wide Code Liability Reduction — Context

Last Updated: 2026-08-14 Europe/Brussels

## SESSION PROGRESS (2026-08-14 Europe/Brussels)

### ✅ COMPLETED
- Re-baselined the initial controller-syntax plan after user feedback.
- Audited all major `Explore.API` directories, largest files, controller actions/dependencies/helpers, identity patterns, problem mapping, HAL registration, background loops, MCP tools, hosting composition, architecture tests, and build state.
- Replaced the narrow two-phase plan with eight feature-preserving consolidation phases.
- Added a mandatory canonical documentation-convergence task to every phase; code completion without doc convergence is explicitly incomplete.
- Classified Gemini proposals independently; rejected metric-driven and rule-conflicting rewrites.
- Completed the Phase 1 contract matrix, deleted three confirmed compatibility paths, normalized 28 mediator-only controllers, and converged current API/tenant/authorization documentation.
- Removed 174 net lines from the controller cohort plus the unwired API tenant implementations and obsolete enum-to-string permission bridge.
- Completed Phase 2.1 identity characterization across ordinary users, provider bootstrap, machine/API-key, purpose-bound ATProto/setup/control-plane, receipt, and diagnostic principals.
- Began Phase 2.2 by constructor-injecting auth/authz configuration services into the two instance controllers; only the base `IUserContext` service locator remains in controller code.

### 🟡 IN PROGRESS
- Phase 1 repository-wide verification: changed projects compile and targeted guardrails pass, but the canonical gates are blocked outside this workstream.

### ⏭️ NEXT
1. Re-run the canonical build in an environment where the .NET WebAssembly task host can start.
2. Re-run the full architecture project after the unrelated dirty-worktree violations are resolved by their owners.
3. Execute Phase 2.2 with explicit `IUserContext`/configuration-service injection and one provider-identity authority, without merging purpose-bound principals.

### ⚠️ BLOCKERS
- Tavily research returned usage-limit status 432; Context7 MCP is unavailable in this session.
- The canonical solution build fails in `Explore.Blazor.Client` WebAssembly SDK task-host startup (`MSB4216`/`MSB4027`); API/Application projects compile serially.
- The architecture suite has four unrelated failures from concurrent worktree changes; the two Phase 1-affected guardrails pass.

## Quick Resume
1. Read this file and `api-application-liability-reduction-tasks.md`.
2. Read the current phase and non-negotiable rules from the plan.
3. Load the phase-specific repo skills/rules before edits.
4. Keep tasks hot; update context/plan only at their documented triggers.

## Audited Hotspots

| Area | Current signal | Planned response |
|---|---|---|
| Controllers | 119 / 24,882 LOC; five 672–1,061-line hotspots | Remove orchestration, then partition by stable capability. |
| Identity | Manual claims + base service locator + `IUserContext` | One explicit trusted identity authority. |
| Command failures | 643-line shared mapper plus many private switches | Characterize and converge on typed API mapping policy. |
| HAL registration | 278 explicit `AddScoped` calls | Compile-time generic helpers, no reflection. |
| Background workers | 11+ repeated scheduling loops | One tested periodic lifecycle; retain worker-specific work. |
| MCP | 2,516-line event tool class | Pin security/disclosure, partition capabilities, deduplicate pure helpers. |
| Host composition | 509-line mixed registration method | Named capability registration methods, visible topology. |
| Query models | 844 lines across two mixed files | Group by capability only when touched; retain shared validation rules. |

## Key Decisions
1. Code liability includes duplicated decisions and mixed responsibilities, not only LOC.
2. Explicit HTTP/HAL/OpenAPI/security metadata is valuable code and remains visible.
3. Consolidate only patterns proven semantically identical by tests.
4. Use existing native abstractions before adding one; any new abstraction must replace at least three implementations.
5. No Minimal APIs, generic controllers, validation pipeline, reflection registration, blanket records/projections, or new packages.
6. No compatibility shims; intentional removals update all internal callers atomically.
7. Public contract drift is not part of this refactor despite development mode.
8. Documentation is executable agent context: stale examples are technical debt and must be removed in the same phase as the code they describe.
9. Update canonical owners instead of appending parallel guidance; use links from lower-authority rules and preserve historical decisions in ADR/Git history.

## Key Files

| Path | Responsibility |
|---|---|
| `src/Explore.API/Controllers/ExploreControllerBase.cs` | Current identity/concurrency base; contains service location and provider reconstruction. |
| `src/Explore.API/ExceptionHandling/CommandResponseResultMapper.cs` | Existing shared command/problem mapping authority. |
| `src/Explore.API/Extensions/HateoasAssemblerRegistration.cs` | Explicit HAL service graph registration. |
| `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` | API composition root. |
| `src/Explore.API/BackgroundServices/` | Hosted scheduling wrappers and processors. |
| `src/Explore.API/Mcp/EventManagementMcpTools.cs` | MCP event tools, gates, mapping, bounds, sanitization. |
| `tests/Event.Architecture.Tests/` | Static/reflection architectural contract gates. |
| `tests/Event.API.IntegrationTests/` | HTTP behavior and host integration evidence. |

## Validation Baseline
- `dotnet build --configuration Release --verbosity quiet`: passed, 0 errors, 758 warnings.
- Planning check: `git diff --check -- dev/active/api-application-liability-reduction` required after artifact rewrite.
- Each implementation phase: one Release build and the single project named in its plan section.

## Current Risks / Unknowns
- Identity bootstrap paths may intentionally differ from already-resolved `IUserContext`; Phase 2.1 must prove boundaries.
- Failure codes with similar names may require different public status/detail safety.
- Periodic workers differ in interval units, scope behavior, retry/fencing, and health semantics.
- MCP helpers combine disclosure ceilings with mapping; careless reuse could leak location or hidden event data.
- Large-controller partitioning may add attribute repetition; orchestration deletion must happen first so cohesion improves rather than merely moving lines.

## Handoff — 2026-08-14 Europe/Brussels
- **Current state:** Phase 1 implementation tasks are complete; canonical repository-wide verification remains open for pre-existing environment/worktree reasons.
- **Next action:** restore the two Phase 1 gates, then execute Phase 2 identity characterization.
- **Blockers:** Tavily quota and unavailable Context7 prevent external-doc retrieval; .NET WebAssembly task-host startup blocks the solution build; four unrelated architecture violations block the full suite.
- **Validation:** API, Application, and architecture projects compile serially; the two directly affected architecture tests pass; scoped diff checks pass.
- **Documentation:** OpenAPI artifact paths, tenant authority paths, and authorization parity guidance were converged with the deleted code.
- **Notes:** do not reintroduce the API-local tenant implementations or enum permission bridge; current authorities are infrastructure tenant context, API middleware, and `AuthorizationActions` strings.

<!-- ABOUTME: Working context for the approved API accidental complexity reduction workstream. -->
<!-- ABOUTME: Preserves session progress, file responsibilities, decisions, constraints, risks, and the exact next step. -->

# API Accidental Complexity Reduction Context

Last Updated: 2026-08-06 Europe/Brussels

## Current Status

- Planning status: **Approved**
- Implementation status: **Not started**
- CTO disposition: architecture accepted after Senior CTO review integration
- First implementation gate: Phase 0 characterization tests before any refactoring

## NEXT

1. User reviews and approves the implementation plan.
2. Start Phase 0: extend AuthorizationBehavior characterization tests.
3. Start Phase 0: add controller error-mapping characterization tests.
4. Once Phase 0 is green, proceed to Phase 1 (typed error contract).

Do not start with Phase 2 (auth redesign) until Phase 0 characterization
tests are green and Phase 1 is complete or in progress.

## Quick Resume

1. Read this context and the tasks file.
2. Read only the current phase from the plan.
3. Start from the first unchecked high-priority task.
4. Keep `api-accidental-complexity-reduction-tasks.md` current during implementation.

## Key Files And Responsibilities

| Path | Status | Layer | Purpose | Notes |
|------|--------|-------|---------|-------|
| `src/Explore.Application/Responses/FailureCodes.cs` | Existing | Application | Machine-readable failure codes | Add `not_found`, `admin_required`, `concurrency_conflict` |
| `src/Explore.Application/Responses/BaseCommandResponse.cs` | Existing | Application | Command response wrapper | `FailureCode` property already exists |
| `src/Explore.API/ExceptionHandling/CommandResponseResultMapper.cs` | Existing | API | Extension methods for ProblemDetails | Add `MapCommandResponse` helper |
| `src/Explore.API/Controllers/ExploreControllerBase.cs` | Existing | API | Base controller | Add `TryParseConcurrencyStamp` |
| `src/Explore.Application/Behaviors/AuthorizationBehavior.cs` | Existing | Application | MediatR pipeline behavior | REDESIGN: 818 → ~40 lines |
| `src/Explore.Application/Authorization/IAuthorizationContextEnricher.cs` | NEW | Application | Per-command auth context interface | O(1) DI dispatch |
| `src/Explore.Application/Authorization/AuthorizationContext.cs` | NEW | Application | Auth context record | `ResourceId` + `Attributes` |
| ~12 enricher files co-located with handlers | NEW | Application | Per-command auth context resolvers | Replace else-if branches |
| `src/Explore.Infrastructure/Services/Keycloak/KeycloakFailureCodes.cs` | NEW | Infrastructure | Keycloak error code constants | Replace inline strings |
| `src/Explore.Application/Helpers/AttributeResolver.cs` | NEW | Application | Safe typed attribute extraction | Replace inline type-casting |
| `tests/Event.Application.UnitTests/Behaviors/AuthorizationBehaviorTests.cs` | Existing | Tests | Auth behavior unit tests | 1,601 lines, extend with characterization |
| `tests/Event.Architecture.Tests/` (new test classes) | NEW | Tests | Architecture enforcement | String-inspection ban, auth dependency ban |
| ~159 handler files in `Features/` | Existing | Application | Command handlers | Add `FailureCode` to failure responses |
| ~36 controller files | Existing | API | Controllers | Replace string inspection with `FailureCode` switch |

## Key Decisions

1. **Extension methods for response mapping** — Use extension methods on `ControllerBase` (not base class methods), consistent with existing `CommandResponseResultMapper` pattern.
2. **`IAuthorizationContextEnricher<TRequest>` via DI** — O(1) dispatch replaces 12 `else-if` branches. Enrichers co-located with handlers.
3. **Leave essential-complexity components alone** — 8 components identified as correctly complex.
4. **No backward compatibility needed** — Development mode.
5. **`snake_case` for new FailureCodes** — Matching existing `quota_exceeded`, `storage_upload_too_large` convention.
6. **Fallback contract** — No matching intent in `intents.yaml`.

## Constraints And Rules

- **Intent:** Fallback (no matching intent in `intents.yaml`)
- **Skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`
- **Rules:** `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`
- Repositories return entities, not DTOs.
- Validators manually instantiated.
- Every file starts with a two-line `ABOUTME:` comment.
- HAL links are single source of truth for UI.
- Clean Architecture: Domain → Application → Infrastructure → API.

## Validation Baseline

```bash
# Build
dotnet build --configuration Release --verbosity quiet

# Phase-specific tests (run one per phase, not per task)
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1
```

## Current Known Risks

1. **Phase 2 (auth redesign) is highest-risk** — behavioral change requires characterization tests as safety net.
2. **Phase 1 scope is large** (159 handlers + 36 controllers) — each change is mechanical but volume creates merge-conflict risk.
3. **Some handlers may have non-standard failure patterns** — discovery during Phase 1 execution.
4. **Pessimistic locking in AuthorizationBehavior** — must be carefully transitioned during Phase 2.

## Handoff Notes

Planning complete. Three workstream artifacts written:
- `api-accidental-complexity-reduction-plan.md` — Full architecture and phased implementation plan
- `api-accidental-complexity-reduction-context.md` — This file
- `api-accidental-complexity-reduction-tasks.md` — Execution checklist

Awaiting user review and approval before Phase 0 execution.

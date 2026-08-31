<!-- ABOUTME: Active planning and resume context for the strong-typing and reflection-debt remediation. -->
<!-- ABOUTME: Records current approval state, decisions, evidence, blockers, validation, and dirty-tree boundaries. -->

# Strong Typing And Reflection Debt Remediation — Context

Last Updated: 2026-08-30 Europe/Brussels

## Review State

- **I-VSD report:** `islamic-value-sensitive-design/i-vsd-strong-typing-reflection-remediation.md`
- **I-VSD reviewed input revision:** `sha256:1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157`
- **I-VSD status / disposition:** current / plan-aligned
- **CTO review:** Not reviewed
- **User approval:** Approved by the user on 2026-08-30 for this exact workstream revision
- **Planning status:** Approved; Phase 0 and Phase 1 completed and verified
- **Change classification:** Behavioral Delta with behavior-preserving structural sub-slices

## Session Progress — 2026-08-31 Europe/Brussels

### COMPLETED

- **Task 0.1**: Added primary cross-cutting intent `strong-typing-refactor` to `.agents/contract/intents.yaml`, added matching scenario to `.agents/benchmarks/cold-start-tasks.yaml`, updated `docs/GOVERNANCE.md` Decision Framework, cleaned up stale missing-triad active paths in archived intents, and verified `dotnet run eng/agent-context/validate-contract.cs -- . --intent strong-typing-refactor` and unscoped `validate-contract.cs` exit 0.
- **Task 0.2**: Refreshed `code-review-graph` blast-radius and AST inventory into `.omo/evidence/2026-08-31-strong-typing-reflection-remediation/blast-radius.yaml` with semantic categories and phase mappings for all candidates without raw source excerpts or PII.
- **Task 0.3**: Added `StrongTypingIntentArchitectureTests` in `tests/Event.Architecture.Tests/StrongTypingIntentArchitectureTests.cs`, verified intent integrity, benchmark parity, and executable architecture rules.
- **Phase 0 Verification**: Solution builds in Release configuration (`dotnet build --configuration Release --verbosity quiet`) with 0 errors, and all 511 tests in `Event.Architecture.Tests` pass (`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`).
- **Task 1.1**: Created red Invariant-Breaker Domain tests in `tests/Event.Domain.UnitTests/ValueObjects/AtprotoDidTests.cs` and `tests/Event.Domain.UnitTests/Entities/AtprotoIdentityDidBoundaryTests.cs`, proving case-sensitive value preservation, AT Protocol syntax validation, 2048-character bounds, no query/fragment/whitespace/percent encoding, and tombstone distinction.
- **Task 1.2**: Implemented `AtprotoDid` value object in `src/Explore.Domain/ValueObjects/AtprotoDid.cs` using source-generated regex, ordinal comparisons, explicit scalar conversions, and updated `AtprotoIdentity.RefreshVerifiedMetadata` to accept `AtprotoDid`.
- **Task 1.3**: Migrated Domain and Application callers (e.g. `AtprotoSubjectOnboardingOperation.cs`, `BootstrapAtprotoSessionCommandValidator.cs`, `AtprotoCurrentSessionIdentityValidator.cs`, and `AtprotoIdentityLifecycleTests.cs`) to `AtprotoDid`.
- **Phase 1 Verification**: Solution builds cleanly in Release configuration (`dotnet build --configuration Release --verbosity quiet`) with 0 errors, and all 1079 tests in `Event.Domain.UnitTests` pass (`dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`).

### IN PROGRESS

- Phase 2 — Platform Identity Authority.

### NEXT

1. Task 2.1: Add hostile-principal matrices and verify conflicting, malformed, provider-linked, and excluded schemes expose current divergence in `tests/Explore.Infrastructure.Tests/Identity/`.
2. Task 2.2: Implement `PlatformPrincipalReader` in `Explore.Infrastructure` with deterministic priority order and scheme exclusion.
3. Task 2.3: Reconcile `IUserContext`, `IAdminContext`, and middleware to use `PlatformPrincipalReader`.
4. Task 2.4: Update and verify all identity authority tests.

### BLOCKERS

- None. Phase 0 and Phase 1 gates are verified green.

## Quick Resume

1. Read this context and `strong-typing-reflection-remediation-tasks.md`.
2. Read only the current phase and referenced decisions/constraints in `strong-typing-reflection-remediation-plan.md`.
3. Confirm current dirty-tree ownership.
4. Start from approved Task 0.1 unless the user overrides it.
5. Refresh context after a phase, decision, blocker, validation failure, scope discovery, or handoff.

## Evidence And Research

- **Frozen evidence packet:** `dev/active/strong-typing-reflection-remediation/strong-typing-reflection-remediation-evidence.md`
- **Evidence SHA-256:** `1a2fa2e4cfaca23086cb49648c0111b5be9c68e85ab5abdddee08e20b1f9b157`
- **I-VSD report:** `islamic-value-sensitive-design/i-vsd-strong-typing-reflection-remediation.md`
- **Repository evidence cutoff:** 2026-08-30
- **External sources:** recorded in the evidence packet; official documentation only.
- **Clean-room posture:** No third-party implementation source or source-derived design entered the plan; no dependency is proposed.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Responsibility |
|---|---|---|---|
| `.agents/contract/intents.yaml` | Existing, currently dirty | Governance | New mixed-source intent and exact scope |
| `.agents/benchmarks/cold-start-tasks.yaml` | Existing, currently dirty | Governance | Deterministic routing scenario |
| `docs/TESTING.md` | Existing | Governance | Executable-seam and source-assurance policy |
| `eng/tools/Explore.AssuranceAudit/` | New, planned | Engineering | Roslyn recurrence audit without historical allowlists |
| `src/Explore.Domain/ValueObjects/AtprotoDid.cs` | New, planned | Domain | Live DID syntax, equality, safe diagnostics |
| `src/Explore.Domain/AtprotoIdentity.cs` | Existing | Domain | Aggregate-owned live/refresh/erasure transitions |
| `src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs` | Existing | Application | Sole ambient platform GUID authority |
| `src/Explore.Application/Authentication/PlatformIdentityClaimTypes.cs` | New, planned | Application | Internal platform claim spelling only |
| `src/Event.Web.BffHosting/Security/EventBffPrincipalExtensions.cs` | New, planned | BFF hosting | Opaque provider subject/session purposes, not platform identity |
| `src/Explore.API/Hateoas/RouteNames.cs` | Existing, currently dirty | API | Stable named-route catalog |
| `src/Explore.ServiceDefaults/HealthChecks/HealthCheckResponseWriter.cs` | Existing | Service defaults | Standard/custom health response headers |
| `tests/Event.Persistence.IntegrationTests/ConfigurationManifest/ConfigurationManifestAuditProviderMigrationTests.cs` | Existing | Tests | Five-provider product-catalog pending-model seam |
| `tests/Explore.GeneratedContracts.Tests/` | Existing | Tests/tooling | Generated transformer and wire-contract determinism |
| `schemas/openapi_islamu-event.json` | Existing generated | API contract | API build-owned OpenAPI source for client generation |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Existing generated | Blazor client | NSwag/Roslyn generated client output |
| `eng/tools/Explore.ApiContractInventory/` | Existing | Engineering | OpenAPI-to-inventory generator |
| `tests/Event.Application.UnitTests/Contracts/Admissions/Support/AdmissionContractRuntime.cs` | Existing, delete planned | Tests | Obsolete reflected behavior runtime |
| `tests/Event.Persistence.IntegrationTests/FairReturnWaitlistConcurrencyTests.cs` | Existing | Tests | Real-provider ordering, tenant, and concurrency behavior |
| named Blazor component/service tests | Existing | Tests | Typed rendering, HAL, accessibility, conflict behavior |
| `dev/pause/blazor-clean-code-refactor/` | Existing paused work | Coordination | Broad Blazor work remains paused; exact ownership transfers only |

## Key Decisions

1. Add a dedicated mixed-source intent; do not widen or inherit the stale test-only intent.
2. Classify string/reflection use by semantic ownership, not syntax.
3. Eliminate runtime-name behavior dispatch and raw product-source/prose assurance.
4. Retain compiled architecture/EF/endpoint metadata and real machine-artifact parsing.
5. Preserve one Application platform-identity authority and separate BFF/provider/session purposes.
6. Use one API-local Admin policy; do not create global role/action conflation.
7. Use `nameof` for self-valued route constants and `HeaderNames` for standard HTTP headers.
8. Introduce only `AtprotoDid`; keep currency/country/email/tenant slug scalar under current owners.
9. Keep DID wire and database representation scalar; no EF/OpenAPI migration expected.
10. Remove browser-side role/current-user mutation inference and rely on HAL.
11. Supersede paused Blazor Tasks 16.1 and 16.6 here; leave paused Task 16.7 and Phase 6A provider/concurrency work untouched.
12. Add a repository-owned Roslyn command, not a source-scraping test or permanent debt allowlist.
13. No backward compatibility.

## Constraints And Rules To Remember

- Repositories return entities.
- Validators are manually instantiated.
- HAL links are the sole client action-affordance authority.
- Platform identity order is `sub -> nameidentifier -> sid -> internal_user_id`, GUID-only.
- Purpose-bound schemes stay isolated.
- Tenant and soft-delete filters remain enabled.
- Critical tests are replaced before old coverage is deleted.
- No fixed sleeps, mock-mirroring, source/prose assurance, or EF query fakes.
- Generated artifacts are generator-owned and never hand-edited.
- No secrets/PII/claims/DIDs in logs, evidence, or ProblemDetails.
- No new dependency without IP/license review.
- No compatibility aliases, overloads, readers, routes, or adapters.
- Work remains on `develop`; unrelated shared changes are not reverted or overwritten.

## Validation Baseline

Planning-only session:

- No product build or product test was run, per Markdown-only planning scope.
- `git diff --check -- .agents/skills/implementation-plan dev/active islamic-value-sensitive-design` exited zero.
- Per-file `git diff --no-index --check` calls returned the expected content-diff exit code `1` with no whitespace diagnostics for all five new artifacts.
- Every linked planning artifact exists; no repository Markdown/link validator was present under `eng/` or `.ci/`.
- Architecture tests are not required unless agent-context/skill infrastructure itself is changed during planning; implementation Task 0.1 will change it and must run the Phase 0 gate.

Implementation phases each run:

- one `dotnet build --configuration Release --verbosity quiet`;
- one selected project test command listed in plan/tasks;
- targeted red/green selectors only for the named invariant under active development.

## Current Known Risks / Unknowns

- The current dirty tree can change counts and ownership before implementation; Task 0.2 refreshes evidence.
- DID is privacy-bearing and case-sensitive; a convenience conversion could leak or normalize identity incorrectly.
- Identity cleanup can change who the system believes a caller is if purpose-bound schemes are conflated.
- Deleting reflection/source tests can remove the only safety proof if replacement-first sequencing is ignored.
- The recurrence audit can become brittle if implemented as a file allowlist instead of semantic Roslyn categories.
- Blazor overlap can duplicate the paused program unless Tasks 8.5 and 8.6 reconcile superseded versus retained ownership.
- Unexpected OpenAPI/EF drift is a design failure requiring re-baselining, not a generated-file patch.

## Handoff Notes

### Handoff — 2026-08-30 Europe/Brussels

- **Current state:** Planning triad, evidence packet, and I-VSD report approved; no implementation started.
- **Next action:** Optional Senior CTO review or approved Task 0.1.
- **Blockers:** Graph refresh and dirty-tree ownership.
- **Modified files:** Only new files under `dev/active/strong-typing-reflection-remediation/` and `islamic-value-sensitive-design/i-vsd-strong-typing-reflection-remediation.md`.
- **Validation:** Final independent audit passed; I-VSD/task/scenario/path/command mappings agree; Markdown whitespace and linked-artifact checks passed. Product build/tests were intentionally not run for this Markdown-only planning change.
- **Documentation impact:** Planned in owning phases; no runtime docs changed now.
- **Risks:** Identity/DID semantics, invariant loss, dirty-tree collisions, Roslyn guard brittleness.
- **Notes for next contributor/agent:** Do not rewrite the full paused Blazor plan. Read its context and exact Phase 16/Phase 6A ranges only when Tasks 8.5–8.6 reconcile ownership. Do not touch unrelated configuration-manifest, secrets-control-plane, migration, generated-client, API, or workstream changes already present in the shared tree.

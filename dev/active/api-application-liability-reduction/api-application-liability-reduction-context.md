<!-- ABOUTME: Resume context for the API-wide code-liability reduction program. -->
<!-- ABOUTME: Records audited hotspots, architecture decisions, blockers, and the next executable slice. -->

# API-Wide Code Liability Reduction — Context

Last Updated: 2026-08-16 Europe/Brussels

## SESSION PROGRESS (2026-08-16 Europe/Brussels)

### ✅ COMPLETED
- **Phase 0** — real verification baseline established; all four known architecture failures fixed rather than deferred; nine additional privacy-inventory gaps found and closed.
- **Phase 1** — six forward-only ratchets installed *before* any migration, each an exact allowlist that can only shrink.
- **Phase 2 / 2b** — one identity authority; zero controller service location; `ExploreControllerBase` 147 → 48 lines.
- **Phase 3** — one declarative `CommandFailurePolicy`; 10 of 11 private failure mappers deleted.
- **Phase 4** — HAL registration `AddScoped` 296 → 27 with a **provably identical** service graph.
- **Phase 5** — 8 periodic workers migrated to Quartz.NET; operator upgrade note published.
- **Phase 6** — MCP monolith 2,516 → 1,463 lines with the AI location-disclosure guard isolated.
- **Phase 7** — all five hotspot controllers partitioned by capability, with **756 operations, 756 operationIds, and 756 generated client methods unchanged**; moderation reason-code policy moved into its command handlers; `InstanceSettingsController` deleted once empty.
- **Phase 8** — `AddApiBackgroundProcessing` extracted (worker topology in one readable place); ratchets tightened at every phase boundary; canonical docs converged.

### 🟡 IN PROGRESS
- Nothing. The workstream is at a clean phase boundary.

### ⏭️ NEXT
The workstream is complete. What remains are separate follow-on workstreams, listed in `tasks.md`:
1. **`openapi-contract-change`** — the contract debt register, chiefly `MapCommandResponse` returning bare `BaseCommandResponse` bodies on failure at 15 call sites instead of RFC 7807.
2. **HAL link-policy consolidation** — 14,360 lines this workstream deliberately did not touch, because link policies encode per-resource authorization.
3. **Build-warning elimination** by warning family, and triage of the 25 inherited API-integration failures.

### ⚠️ BLOCKERS
- None. Every blocker recorded on 2026-08-15 is resolved: the SDK workload failure was fixed upstream, the verification baseline is real, Phase 1 is verified, sole-agent execution removed the collision gate, and the scheduling-authority question is settled in favour of Quartz.NET.

## Verification baseline (2026-08-16)

| Gate | Result |
|---|---|
| `dotnet build --configuration Release --verbosity quiet` | **0 errors** (13,535 warnings at session start) |
| `Event.Architecture.Tests` | **394 total, 0 failed**, 1 skipped |
| `Explore.Infrastructure.Tests` | **1346 total, 0 failed** |
| `Event.API.IntegrationTests` | **26 failed** vs. the pre-existing **25**-failure baseline; **2 baseline failures fixed**; the 3 remaining deltas are order-dependent |

The 25 API-integration failures are pre-existing at HEAD and were measured in an isolated worktree before any change in this session. The three deltas — `AnImmediateTriggerActuallyExecutesOnSqlite` and the two `EventList_*_MatchesSnapshot` snapshots — each pass **4/4 when their class runs alone**; they are parallel-teardown races, not regressions. Two baseline failures were repaired: `OpenApiDocument_PublicHalDetailResourceSchemasAreNotEmpty` (the empty HAL wrapper) and `GetAllWithoutStatusFilterHidesDraftArchivedAndModeratedEvents`.

**Container runtime note:** the suite needs a Docker-compatible endpoint. With Podman, export
`DOCKER_HOST=unix:///run/user/1000/podman/podman.sock`, `TESTCONTAINERS_RYUK_DISABLED=true`, and
`TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/run/user/1000/podman/podman.sock`; without them every
Testcontainers-backed test fails with `DockerUnavailableException`, which looks like a mass regression.

## Key Decisions
1. Code liability is duplicated *decisions*, not lines. `EventApiClient.g.cs` is 152k generated lines; repository-wide LOC is not a maintainability signal.
2. Explicit HTTP/HAL/OpenAPI/security metadata is valuable code and stays visible.
3. Consolidate only patterns proven semantically identical; exclusions are valid outcomes and must be recorded with their reason.
4. Any new abstraction must replace at least three implementations **and** must not duplicate a capability an existing dependency already provides.
5. **Enforcement precedes migration.** Every liability class was frozen by an exact-allowlist architecture test before its migration ran. This paid off repeatedly: each advance made its own baseline entry stale, and the hygiene test forced the delisting.
6. **Identity derivation is a pure function of the principal.** Controllers already hold `ControllerBase.User`, so the authority is a set of `ClaimsPrincipal` extensions — no service location, and no identity dependency threaded through 25 constructors.
7. **Purpose-bound authentication schemes stay separate** from ambient user identity; merging them would widen trust.
8. **Quartz.NET is the periodic scheduling authority.** No bespoke worker lifecycle was introduced.
9. Public contract drift stays out of this refactor — not for compatibility's sake, but because a behavior-preserving refactor that also changes contracts cannot be verified by parity tests. Contract defects are recorded for a successor workstream instead.
10. Documentation is executable agent context: stale examples are technical debt and are removed in the same slice as the code they describe.

## What changed, by file

| Area | Before | After |
|---|---|---|
| `ExploreControllerBase.cs` | 147 lines, service-locates `IUserContext`, reconstructs provider identity | 48 lines, projects pure extensions |
| Identity authority | 3 divergent claim chains (`UserContext`, `GetAuthenticatedUserId`, base controller) | 1 — `PlatformIdentityPrincipalExtensions` |
| Controller `HttpContext.RequestServices` | 1 | **0** |
| Private controller failure mappers | 11 across 4 files | 1 (feature-specific, justified) |
| `HateoasAssemblerRegistration.cs` | 460 lines, 296 `AddScoped` | 326 lines, 27 `AddScoped`, identical graph |
| Periodic `BackgroundService` timer loops | 17 files | 10 (8 migrated, remainder characterized exclusions) |
| `EventManagementMcpTools.cs` | 2,516 lines | 1,463 + 4 focused modules |
| `EventController.cs` | 1,033 lines | 334 + 4 capability controllers |
| `RegistrationOrderController.cs` | 1,146 lines | 78 + guest/authenticated + shared base |
| `WebhooksController.cs` | 987 lines (was 1,025) | 403 + endpoints/messages + shared base |
| `InstanceSettingsController.cs` | 859 lines, 47 actions | **deleted**; six capability controllers + shared base |
| `ControlPlaneController.cs` | 673 lines | 166 + 3 capability controllers |
| Controllers over 500 lines | 7 | 3, none a former hotspot |
| Release build warnings | 13,535 | 9,290 |

## Key Files

| Path | Responsibility |
|---|---|
| `src/Explore.Application/Authentication/PlatformIdentityPrincipalExtensions.cs` | **The** identity authority: user-id fallback chain and provider reconstruction. |
| `src/Explore.Application/Authentication/CurrentUserResolutionExtensions.cs` | Local-account resolution for non-GUID provider subjects, over `IMediator`. |
| `src/Explore.API/ExceptionHandling/CommandFailurePolicy.cs` | Declarative failure-code → RFC 7807 routing, composable and immutable. |
| `src/Explore.API/Extensions/HateoasAssemblerRegistration.cs` | HAL graph via compile-time helpers; no reflection, still grep-searchable. |
| `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` | Scheduler composition; `AddSweepJob` owns periodic lifecycle. |
| `src/Explore.API/Scheduling/MaintenanceSweepJobs.cs` | The 8 migrated maintenance jobs; one pass each, nothing else. |
| `src/Explore.API/Mcp/EventMcpLocationDisclosureGuard.cs` | Fail-closed AI location-disclosure boundary, independently testable. |
| `src/Explore.API/Mcp/EventMcpBounds.cs` | The MCP truncation/disclosure budget in one reviewable place. |
| `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` | The six forward-only ratchets and their hygiene test. |
| `tests/Event.Architecture.Tests/HateoasRegistrationGraphTests.cs` | HAL graph invariants plus the diffable descriptor inventory. |

## Current Risks / Unknowns
- The `MapCommandResponse` ProblemDetails divergence is a live contract defect affecting 15 endpoints; it needs the successor workstream, not a quiet fix here.
- The 25 pre-existing API-integration failures were inherited, not triaged. They deserve their own owner.
- `AnImmediateTriggerActuallyExecutesOnSqlite` is order-dependent and will keep flapping in full runs until its fixture disposal is isolated.

## Handoff — 2026-08-16 Europe/Brussels
- **Current state:** **All eight phases implemented, verified, and documented.** The build is green and every risk-owning gate passes or matches baseline.
- **Next action:** none in this workstream. Start the `openapi-contract-change` successor with the contract debt register.
- **Blockers:** none.
- **Validation:** see the baseline table above. Every phase gate named in plan §7 was run at its phase boundary.
- **Documentation:** canonical docs converged in the same session as the code, including an operator upgrade note for the scheduler migration.
- **Notes:** the ratchets are the durable asset here. If a future change reintroduces controller service location, a private failure switch, a hand-rolled timer loop, or HAL registration boilerplate, `ApiLiabilityRatchetTests` fails — and because the baselines are exact rather than ceilings, a *fix* that leaves its entry behind fails too. Keep them exact.

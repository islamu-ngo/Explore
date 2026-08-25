<!-- ABOUTME: Resume context for the repository-wide semantics-first C# records adoption workstream. -->
<!-- ABOUTME: Keeps current decisions, next task, constraints, risks, and verification state synchronized. -->

# Records Adoption — Context

Last Updated: 2026-08-25 Europe/Brussels

## SESSION PROGRESS (2026-08-25 Europe/Brussels)

### COMPLETED

- Classified the work against `openapi-contract-change` and the Tier 1 identity/tenant guardrail.
- Investigated current DTO, MediatR, outbox, API, OpenAPI/NSwag, and Blazor patterns.
- Verified representative current debt, including body-owned Category `TenantId`, mutable command classes, grouped PATCH semantics, generated DTO ownership, and mutable outbox lifecycle state.
- Reviewed official Microsoft record, System.Text.Json, ASP.NET Core model-binding, and EF Core guidance.
- Completed the mandatory Grill-Me decisions with the user.
- Created [I-VSD records-adoption consultation](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md).
- Created `records-adoption-plan.md`.
- Cross-checked all three planning artifacts: 24 task IDs/names, seven phases, statuses, gates, decisions, release strategy, and I-VSD links agree.
- `git diff --check` passed for the records-adoption planning and I-VSD artifacts.
- Ran `Event.Architecture.Tests`: 442 passed, 1 skipped, and 1 pre-existing/shared-workspace failure was observed.
- Created isolated detached worktree `/home/amir/ISLAMU/Github/Event-records-adoption` from clean `aa74b645c`.
- Restored the required repository-owned ignored authorization artifact with matching SHA-256.
- Established a green isolated architecture baseline: 443 passed, 1 skipped, 0 failed.
- Revalidated current .NET 10 records, System.Text.Json, model-binding, OpenAPI, and EF identity behavior through official Microsoft documentation.
- Completed Task 0.1 after three independent verifier passes: deterministic discovery now reports 590 concrete MediatR class requests, 824 Application contract classes, and 15 body-authority members.
- Completed Task 0.2 and Phase 0: 1,414 exact class dispositions, 15 body dispositions, canonical governance policy, six focused ratchets green, Release build green, and architecture suite at 449 passed/1 skipped/0 failed.
- Completed Task 1.1: eight behavioral value-contract tests pass and exhaustive source/mapping review found no unresolved Domain class conversion candidate.
- Completed Task 1.2 as an independently confirmed no-op: the approved bounded values already use sealed record or readonly record-struct semantics.
- Completed Task 1.3 and Phase 1: zero Domain/Persistence model delta, no baseline removal, green Release build, and 857/857 Domain tests.
- Completed Task 2.1: one independent architecture RED enumerates all 590 class requests and 12 authorization tests prove isolated forged facts fail closed.
- Completed Tasks 2.2 and 2.3: all 590 compiled MediatR class requests are records, 18 collection-bearing requests defensively snapshot 37 properties, and slow-request logging contains no request values or user/tenant identifiers.
- Recovered the Phase 2 gate with the Phase 3 implementation present: Release build is green and all 3,942 Application tests pass.
- Completed Phase 3 Tasks 3.1–3.4: approved handwritten DTO/read/projection/body/payload candidates are records; the class baseline shrank from 824 to eight retained mutable response envelopes.
- Removed eight ambient tenant-authority body members and shrank the body disposition baseline from 15 to seven legitimate operation-target identifiers; trusted tenant facts now flow from server context on commands.
- Preserved PATCH absent/set/clear behavior, HAL/pagination/mapping semantics, JSON/AOT serialization, and mutable persisted outbox lifecycle entities. Immutable notification payload collections now defensively snapshot inputs.
- Reconciled the focused record ratchet to exact retained-class equality: 9/9 architecture tests pass. DTO mapping/serialization tests pass 12/12, outbox payload tests pass 7/7, and notification payload tests pass 2/2.
- Completed bounded Tier 1 reviews with no findings: contract review approved 829 changed DTO record declarations and zero changed persisted outbox entity files; security/privacy review approved after 81 focused tests.

### IN PROGRESS

- All 24 implementation tasks and Phase 6 are complete. The overall workstream remains verification-blocked because the exact Phase 4 API integration gate is still red.
- Phase 6 Release verification passed 39 projects with 0 errors and 0 warnings; `Event.Architecture.Tests` passed 453/453 with 0 warnings.
- Pre-PR review is not merge-ready until the privacy-replay/Infisical startup blocker is resolved and the broad shared dirty worktree is curated.

### NEXT

1. Resolve the privacy-replay/Infisical startup blocker without weakening production-auth guardrails.
2. Rerun only the exact Phase 4 API integration gate and require it to pass before merge readiness.
3. Curate the shared 1,186-path dirty worktree into an owned review/commit set; do not commit, tag, push, or publish without authorization.

### BLOCKERS

- The shared main-worktree architecture baseline still fails `UserPiiInventoryArchitectureTests.InventoryCoversCurrentEfAndDesignatedProviderSurfaces` because unrelated untracked paid-checkout fields are absent from the PII inventory. Records adoption must not modify that work.
- This is not an active implementation blocker: all records-adoption product edits and verification run in the isolated green worktree.
- AnySearch MCP was not registered. Context7 and official web research were available for official .NET/MediatR facts; no implementation source was copied, and the clean-room handoff/evidence remains source-free.

## Quick Resume

1. Read this context and `records-adoption-tasks.md`.
2. Read only the current phase, constraints, or changed decision in `records-adoption-plan.md`.
3. Resume from the current priority in `records-adoption-tasks.md` unless the user overrides it.
4. Keep `records-adoption-tasks.md` as the hot ledger; update this context only after a phase, decision, blocker, failed validation, material discovery, or handoff.
5. Do not implement feature vertical slices. Preserve horizontal ownership: Domain → Application → API/OpenAPI → generated client/Blazor.

## Current Status

- **Planning status:** Approved.
- **Implementation status:** All 24 implementation tasks and Phase 6 are complete; Phase 4 verification remains blocked by API integration startup infrastructure.
- **Completed implementation tasks:** 24/24.
- **Current priority:** Recover the Phase 4 API integration gate.
- **Next recommended slice:** Resolve privacy-replay/Infisical startup deterministically, rerun only the red API gate, then curate the shared dirty worktree.
- **I-VSD:** [C# Records Adoption I-VSD consultation](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md).

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `records-adoption-plan.md` | New | Planning | Architecture, phases, tasks, risks, and acceptance | Update only for material strategy changes. |
| `records-adoption-tasks.md` | New | Planning | Hot implementation ledger | Update immediately as substantial tasks change. |
| `islamic-value-sensitive-design/i-vsd-records-adoption.md` | New | Governance | Provider-responsibility review | Mandatory linked intake artifact. |
| `tests/Event.Architecture.Tests/RecordContractArchitectureTests.cs` | Planned new | Tests | Permanent record/class/body-authority ratchet | Task 0.1. |
| `tests/Event.Architecture.Tests/Baselines/record-contract-class-baseline.json` | Planned new | Tests | Reasoned shrinking class debt | Must reject stale/unclassified entries. |
| `tests/Event.Architecture.Tests/Baselines/http-body-authority-dispositions.json` | Planned new | Tests | Current-authority versus target-ID dispositions | No current-authority exception may remain at completion. |
| `src/Explore.Domain/ValueObjects/**/*.cs` | Existing | Domain | Bounded value-object candidate surface | Entity/reference identity remains class-based. |
| `src/Explore.Application/Features/**` | Existing | Application | MediatR command/query surface | Concrete requests should become sealed records. |
| `src/Explore.Application/DTOs/**/*.cs` | Existing | Application | Handwritten DTO surface | 657 source files match `*Dto*.cs`; not all are candidates. |
| `src/Explore.Application/Responses/BaseCommandResponse.cs` | Existing | Application | Mutable command result/failure builder | Explicitly retained as a class. |
| `src/Explore.Domain/OutboxMessage.cs` | Existing | Domain | Mutable persisted outbox lifecycle entity | Explicitly retained as a class. |
| `src/Explore.API/Controllers/**/*.cs` | Existing | API | Trusted request adapters | Route/principal/tenant authority, not body authority. |
| `src/Explore.API/Serialization/OptionalUpdateJsonConverterFactory.cs` | Existing | API | PATCH absent/set/clear JSON semantics | Must remain behaviorally intact. |
| `schemas/openapi_islamu-event.json` | Generated | API contract | Canonical checked-in OpenAPI | Never hand-edit. |
| `docs/API_CONTRACT_INVENTORY.md` | Generated | Docs/API | Generated operation inventory | Never hand-edit. |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Generated | Blazor | NSwag API client and DTO classes | Must remain generated classes. |
| `src/Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs` | Existing | Blazor | AOT JSON metadata | Align after regeneration/local records. |
| `docs/releases/changes/CHG-2026-0010.yaml` | Planned new | Release | Breaking/OpenAPI/security change fragment | Final task only; recheck ID availability. |

## Key Decisions

1. **Classification-first and ambitious:** convert every handwritten type whose correct semantics are immutable data plus value equality.
2. **No record uniformity:** retain classes for generation, entity/reference identity, lifecycle mutation, framework-populated binding, editable UI state, handlers/services/controllers/validators, and mutable command responses.
3. **Horizontal Clean Architecture:** policy → Domain → Application requests → Application DTOs/payloads → API/OpenAPI → generated client/Blazor → governance closure.
4. **Green phase boundaries:** a horizontal phase may repair downstream compilation breakages before its single gate.
5. **Trusted facts:** remove current user/tenant from bodies; put trusted IDs on commands only when authorization or business intent uses them.
6. **Permanent ratchet:** new class debt, unclassified body authority, and stale baseline entries fail architecture tests.
7. **Record form:** positional for short stable contracts; nominal `required`/`init` records for long, optional, PATCH, validation, or named-construction contracts.
8. **Collections:** harden only converted immutable contracts with serializer-compatible read-only members and defensive copies; do not assume record equality compares sequence contents.
9. **`BaseCommandResponse<T>`:** retain as class; immutable result redesign is deferred.
10. **Generated DTOs:** backend sources may be records; NSwag output remains generated classes.
11. **Outbox:** immutable payload snapshots may be records; persisted entities/processors remain classes.
12. **Breaking changes:** explicitly approved for development. Update tests, OpenAPI, generated client, API changelog, and change fragment; add no compatibility shim.
13. **Tests:** exploit `with` for one-fact adversarial variants, equality only where consumed, immutable construction, JSON round trips, PATCH presence, and identity tampering; do not test compiler prose.

## Constraints And Rules To Remember

- Domain has no outward/framework dependencies.
- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Current user authority is `PlatformIdentityPrincipalExtensions` / `IUserContext`; current tenant authority is resolved `ITenantContext`.
- Controllers must not resolve services through `HttpContext.RequestServices`.
- `ISecureRequest` authorization facts fail closed.
- Writes remain `[Authorize]`; failures remain RFC 7807 ProblemDetails.
- HAL `_links` remains the only client-side action authority.
- Generated OpenAPI, API inventory, NSwag code, migrations, and snapshots are never hand-edited.
- Every new file needs two `ABOUTME:` lines.
- No compatibility aliases/readers/constructors.
- Do not log/destructure entire records with PII, tokens, free text, or current identity.
- Phase verification is one Release build and one selected non-browser test project.
- Tier 1 Task 4.4 additionally requires scoped Stryker score above 85%, zero-PII evidence, and anonymized MAD review.
- No app startup, browser, Playwright, Aspire, Docker, or live-service verification belongs in this planning workflow.

## Validation Baseline

### Phase 4 handoff — 2026-08-25

- HTTP trust-boundary tests prove a forged `tenantId` is rejected as a generic 400 ProblemDetails response before mediator dispatch, without echoing forged or authenticated identifiers. Eight affected writes retain `[Authorize]` and publish 400/401/403 metadata.
- Generated OpenAPI changed by +108/-33 lines and the generated NSwag client by +60/-25; API inventory regenerated byte-identically. All 776 endpoints/operation IDs remained stable.
- Exact Release build: 39 projects, 0 errors, 6,901 warnings.
- Exact API integration gate: 2,264 total, 2,255 passed, 1 skipped, 8 failed. Focused reruns prove the five deterministic record/order/caller failures green after repairing the AI record comparison and storage failure code. Three production-auth guardrail cases remain blocked at host startup by privacy-erasure replay/Infisical credentials.
- Zero-PII evidence is green. An anonymized Security-60% MAD accepted one finding; the trust-boundary test was strengthened with generic-message and no-identity-echo assertions and passes 2/2.
- Scoped Stryker 4.16.0 passed at 85.19%: 23 killed, 2 survived, with only `ImportEventCommandHandler.cs` in the executed mutation set. Its analysis temporarily created 62,949 whole-project mutants, then the configured filter discarded 56,577 before executing exactly 25 target mutants; concurrency 1 avoided the reported MTP race. Three survivor-focused assertions were added and the focused handler suite passed 11/11 before the green rerun.
- Evidence: `/home/amir/ISLAMU/Github/Event-records-adoption/.omo/evidence/records-phase4/`.

### Phase 5 handoff — 2026-08-25

- Task 5.1 captured intentional RED at 4 passed/2 failed for reference equality and caller-mutable snapshots; generated authority-free DTOs and mutable edit state were already green. Final focused contract coverage passes 8/8.
- NSwag regeneration is byte-identical. `EventApiClient.g.cs` SHA-256 remained `32e942022fb4c9b4cb971a0af169385f288c47f02932e566035cc7257b77d419`; generated DTOs remain classes and the eight affected write bodies contain no tenant authority.
- Nine approved local classes became sealed records: generic/non-generic service results, paginated state, three webhook snapshots, URL filter state, tenant-branding save result, and persisted event-detail state. Published collections defensively snapshot caller inputs. Mutable form/component/auth/PII/BFF payload state remains class-based.
- The bounded handwritten `Models`/`Contracts`/`Services`/`Helpers` surface moved from 157 public classes and 41 public records to 149 classes and 50 records; the additional record is the AOT dock storage envelope.
- `AppJsonSerializerContext` now registers the dock-layout snapshot/envelope, and local-storage persistence uses its source-generated options. Round-trip tests pass while `ReportingProviderCredentialsUpdateDto` remains absent from the context.
- Accessibility-specific validation was not applicable: no Razor markup, form interaction, focus, ARIA, keyboard, or styling behavior changed.
- Exact Release build passed: 39 projects, 0 errors, 7,115 warnings. The first exact client gate exposed a detached-worktree `.git` directory/file assumption (2,552 passed, 1 failed, 1 skipped); the path detector now accepts both forms, its focused scenario passes 1/1, and the full recovery run passes 2,553 tests with 0 warnings.
- Phase 4's API integration blocker remains unchanged and is not relabeled complete.
- Evidence: `/home/amir/ISLAMU/Github/Event-records-adoption/.omo/evidence/records-phase5/`.

### Phase 6 handoff — 2026-08-25

- The final record/body ratchet passes 10/10: exactly eight retained mutable response hierarchies, seven legitimate operation targets, and zero concrete MediatR class requests. Candidate categories are no longer accepted, so new, missing, or stale debt fails bidirectionally.
- Canonical Architecture, API, Outbox, Blazor, Governance, and contributor rules now describe the implemented split: trusted server authority, generated ownership, PATCH presence semantics, shallow collection/equality limits, immutable payload snapshots versus mutable outbox lifecycle, and immutable presentation snapshots versus mutable edit/component state.
- `CHG-2026-0010.yaml` is append-only, uses the approved `architecture` scope, and passes the repository `ReleaseInputPolicy` across all six impact objects. The Conventional Commit is composed but was not executed.
- Scoped docs whitespace/link checks and `git diff --check` pass. The I-VSD and clean-room source-of-truth evidence remain linked and source-free.
- Exact Release build passed 39 projects with 0 errors and 0 warnings. Exact `Event.Architecture.Tests` passed 453/453 with 0 warnings.
- Generated artifacts remain repository-command-owned: OpenAPI +108/-33, NSwag +60/-25, API inventory byte-identical, and all 776 operation IDs stable. NSwag SHA-256 remains `32e942022fb4c9b4cb971a0af169385f288c47f02932e566035cc7257b77d419`.
- Pre-PR review is not merge-ready: the exact Phase 4 API gate remains red, and the shared worktree has 1,186 dirty paths (1,178 tracked modifications and 12 untracked files), requiring explicit ownership curation before any commit.
- Evidence: `/home/amir/ISLAMU/Github/Event-records-adoption/.omo/evidence/records-phase6/`.

Planning ran the architecture hook once:

- `git diff --check -- .agents/skills/implementation-plan dev/active/records-adoption islamic-value-sensitive-design/i-vsd-records-adoption.md` — passed.
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — 444 total, 442 passed, 1 skipped, 1 failed from unrelated shared paid-checkout PII inventory drift.

Do not rerun the unchanged failure. After the owning work changes, Task 0.1 requires one green baseline before editing records-adoption tests.

Each implementation phase then runs exactly:

| Phase | Build | Selected test |
|---|---|---|
| 0 Architecture policy | `dotnet build --configuration Release --verbosity quiet` | `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj` |
| 1 Domain values | same | `tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj` |
| 2 Application requests | same | `tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj` |
| 3 Application DTOs/payloads | same | `tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj` |
| 4 API/OpenAPI | same | `tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj` |
| 5 Blazor | same | `tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj` |
| 6 Governance close | same | `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj` |

The repeated Application and Architecture projects have distinct phase ownership and concrete reasons. Successful commands are not rerun unchanged.

## Current Known Risks / Unknowns

- The exact eligible DTO/request set remains a Phase 0 output; do not infer it from filenames.
- The current architecture baseline is blocked by unrelated paid-checkout PII inventory drift; do not add those fields from this workstream.
- Body `UserId`/`TenantId` names can represent current authority or a legitimate target; trace every caller before disposition.
- AutoMapper and LINQ constructor projection compatibility is candidate-specific.
- Some Domain value objects may be EF-mapped; a model delta blocks and requires separate migration classification.
- Record collections remain shallowly immutable unless copied.
- Record equality does not provide structural sequence equality.
- Generated `ToString()` increases accidental PII/logging risk.
- Horizontal phases are implementation checkpoints, not releaseable API/client combinations.
- The Phase 4 OpenAPI breaking diff is known (+108/-33 with 776 operation IDs stable); scoped Stryker is green at 85.19% with two non-critical surviving string mutants.
- The append-only `CHG-2026-0010` fragment is now claimed; its terminal commit must retain both `BREAKING CHANGE:` and `Change-Id: CHG-2026-0010`.

## Deferred Work

- Immutable redesign of `BaseCommandResponse<T>` and handler result factories.
- Any new domain concept such as `Money`, `GeoCoordinate`, or `DateRange` not already justified by repository duplication.
- EF schema migration caused by value-object redesign.
- Repository-wide immutable-collection standard beyond converted immutable contracts.
- Changing NSwag generator representation from POCO classes.

## Handoff Notes

### Handoff — 2026-08-24 Europe/Brussels

- **Current state:** Investigation, user decisions, synchronized planning artifacts, and planning verification are complete. No runtime code changed.
- **Next action:** Wait for the shared paid-checkout PII inventory baseline to turn green, obtain user implementation approval, then start Task 0.1.
- **Blockers:** One unrelated architecture-test failure owned by concurrent paid-checkout work; implementation approval is also pending.
- **Modified files:** `islamic-value-sensitive-design/i-vsd-records-adoption.md`; all three files under `dev/active/records-adoption/`.
- **Validation:** Planning diff check passed. Architecture suite: 442 passed, 1 skipped, 1 pre-existing/shared failure.
- **Documentation impact:** Plan requires final updates to Governance, Architecture, API/API changelog, Outbox, Blazor, rules, and Tier 2 change fragment.
- **Risks:** The Phase 0 baseline can become a loophole if reasons are vague or authority IDs are classified without caller tracing.
- **Notes for next contributor:** Do not begin conversions before the empty-baseline architecture tests visibly fail for the intended debt. Do not turn feature families into vertical slices.

### Handoff — 2026-08-25 Europe/Brussels

- **Current state:** All 24 implementation tasks and Phase 6 are complete; Phase 4 API verification remains red, so the workstream is not fully green or merge-ready.
- **Next action:** Resolve the privacy-replay/Infisical startup blocker, rerun only the exact API integration project, then curate the records-adoption paths from the shared dirty worktree.
- **Blockers:** Exact API gate remains 2,264 total, 2,255 passed, 1 skipped, 8 failed; five repaired cases pass focused reruns, while three production-auth guardrail cases cannot start because privacy-erasure replay requires unavailable Infisical credentials.
- **Modified files:** Shared records-adoption delta spans Domain/Application/API/Blazor/tests/docs/generated artifacts; final Phase 6 additions include the tightened architecture ratchet, canonical docs/rules, `CHG-2026-0010.yaml`, and the repository fragment-validation test.
- **Validation:** Phase 6 Release build 39 projects/0 errors/0 warnings; architecture 453 passed; fragment validator 1 passed/2 warnings; focused ratchet 10 passed/352 warnings; whitespace/schema/link/diff checks pass.
- **Documentation impact:** Tasks/context reconciled; the plan was unchanged because scope, architecture, phase order, acceptance, risk, and validation strategy did not change.
- **Risks:** The broad shared 1,186-path dirty state prevents reliable ownership from an unstaged diff; no commit may be attempted until paths are curated.
- **Notes for next contributor:** Preserve the exact eight/seven baselines, do not add shims or hand-edit generated artifacts, and do not relabel Phase 4 complete until its exact test project is green.

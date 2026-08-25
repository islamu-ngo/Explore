<!-- ABOUTME: Executable task ledger for semantics-first C# record adoption across ISLAMU Event. -->
<!-- ABOUTME: Mirrors the horizontal Clean Architecture plan, Red-first sequencing, and verification gates. -->

# Records Adoption — Task Checklist

Last Updated: 2026-08-25 Europe/Brussels

## Status Summary

- **Overall status:** All 24 implementation tasks are complete; Phase 4 API verification remains externally blocked, so the workstream is not fully green or merge-ready.
- **Completed:** 24/24 implementation tasks (phase verification tracked separately).
- **Current priority:** Recover the Phase 4 API integration gate without weakening production-auth guardrails.
- **Next recommended slice:** Resolve the privacy-replay/Infisical startup blocker, rerun only the failed API gate, then curate the shared dirty worktree for review.
- **Isolation:** Product edits run only in `/home/amir/ISLAMU/Github/Event-records-adoption`; its architecture baseline is green at 443 passed, 1 skipped, 0 failed.
- **Shared-worktree note:** The unrelated paid-checkout PII inventory failure remains in main and is not modified by this workstream.
- **Planning verification:** `git diff --check` passed. `Event.Architecture.Tests` ran 444 tests: 442 passed, 1 skipped, 1 unrelated shared-workspace failure.
- **I-VSD:** [C# Records Adoption I-VSD consultation](../../../islamic-value-sensitive-design/i-vsd-records-adoption.md).
- **Plan:** [Records Adoption implementation plan](records-adoption-plan.md).
- **Context:** [Records Adoption resume context](records-adoption-context.md).

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `IN PROGRESS` when it will span multiple edits or a handoff; skip this churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work under its owning horizontal layer phase with acceptance criteria and dependencies.
- Keep completed count, priority, next slice, deferred work, and update date accurate.
- Check a phase complete only after every implementation and phase-verification checkbox passes.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance, risk, or validation changes.
- Do not run repeated checks after individual Green tasks. Red tasks observe the focused expected failure once; full verification runs once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools, or live services.
- Do not hand-edit generated OpenAPI, API inventory, NSwag client, EF migration, or snapshot files.
- Do not add a baseline exception merely to make a test pass; every retained class requires an evidence-backed reason and removal trigger.
- Preserve horizontal Clean Architecture ownership; do not reorganize this workstream into feature vertical slices.

## Phase 0: Architecture Policy And Candidate Baseline — COMPLETE

- [x] **0.1 Author Failing Record And Body-Authority Ratchets**
  - **Status:** COMPLETE — intentional RED independently confirmed.
  - **Files:** `tests/Event.Architecture.Tests/RecordContractArchitectureTests.cs` (new); `tests/Event.Architecture.Tests/Baselines/record-contract-class-baseline.json` (new, initially empty); `tests/Event.Architecture.Tests/Baselines/http-body-authority-dispositions.json` (new, initially empty).
  - **Acceptance:**
    - [x] Compiled MediatR request and Application DTO discovery is deterministic.
    - [x] Current class requests/unclassified DTOs fail for the expected reason.
    - [x] Undisposed body `TenantId`/`UserId`-shaped members fail.
    - [x] Generated DTOs, validators, entities, and mutable edit state are explicitly categorized.
  - **Effort:** L.
  - **Dependencies:** Green architecture baseline after the owning paid-checkout work changes; then observe this task’s intentional RED.
  - **Guidance:** Tier 1 Red phase; `criticality-guardrail`, `auth-patterns`, Test rule.

- [x] **0.2 Classify Current Contracts And Establish Shrinking Baselines**
  - **Status:** COMPLETE.
  - **Files:** both baseline JSON files; `docs/GOVERNANCE.md`; `.agents/rules/application-layer.md`; `.agents/rules/domain.md`; `.agents/rules/blazor-client.md`; `.agents/rules/tests.md`.
  - **Acceptance:**
    - [x] Every entry names category, reason, owner, and removal trigger.
    - [x] Every candidate is positional record, nominal record, record struct, or retained class.
    - [x] Build output/generated files are absent.
    - [x] New debt and stale baseline entries fail automatically.
  - **Effort:** XL.
  - **Dependencies:** 0.1.
  - **Guidance:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `ast-grep`.

### Phase 0 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 1: Domain Value Semantics — COMPLETE

- [x] **1.1 Author Failing Value-Semantics Specifications**
  - **Status:** COMPLETE — exhaustive green characterization; no artificial RED.
  - **Files:** `tests/Event.Domain.UnitTests/ValueObjects/RecordValueObjectContractTests.cs` (new); existing candidate-specific tests.
  - **Acceptance:**
    - [x] Tests cover construction invariants, intended equality, one-fact `with` variants, copy behavior, and invalid values.
    - [x] Sequence equality applicability is explicit.
    - [x] No compiler-prose or pinned `ToString()` tests.
  - **Effort:** M.
  - **Dependencies:** 0.2.
  - **Guidance:** Domain/Test rules, `dotnet-efcore-guidelines`.

- [x] **1.2 Convert Approved Domain Value Types**
  - **Status:** COMPLETE — independently confirmed no-op.
  - **Files:** Phase 0 candidates under `src/Explore.Domain/ValueObjects/**/*.cs`; direct callers.
  - **Acceptance:**
    - [x] Approved types use sealed record class or justified `readonly record struct`.
    - [x] Constructor invariants and defensive-copy requirements hold.
    - [x] Entities, aggregates, outbox lifecycle rows, and large reference-rich types remain classes.
  - **Effort:** L.
  - **Dependencies:** 1.1.
  - **Guidance:** `clean-architecture-rules`, Domain rule.

- [x] **1.3 Repair Mappings And Remove Resolved Domain Baselines**
  - **Status:** COMPLETE — independently confirmed no-op.
  - **Files:** verified callers/EF configurations; record class baseline.
  - **Acceptance:**
    - [x] Downstream constructors compile without outward Domain dependencies.
    - [x] EF model shape has no schema delta.
    - [x] No migration/snapshot file is edited.
    - [x] No resolved Domain baseline entry exists to remove.
  - **Effort:** M.
  - **Dependencies:** 1.2.
  - **Guidance:** `dotnet-efcore-guidelines`, `clean-architecture-rules`.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Application MediatR Requests — COMPLETE

- [x] **2.1 Author Failing Request And Authorization Specifications**
  - **Status:** COMPLETE — intentional RED and authorization invariants confirmed.
  - **Files:** `RecordContractArchitectureTests.cs`; focused `tests/Event.Application.UnitTests/Features/**` tests.
  - **Acceptance:**
    - [x] Selected class requests fail the record ratchet.
    - [x] `with` variants forge tenant/user/resource facts and prove fail-closed behavior.
    - [x] Empty-ID and changed-resource cases are covered where applicable.
    - [x] Sensitive request values are absent from log assertions.
  - **Effort:** XL.
  - **Dependencies:** 1.3.
  - **Guidance:** `criticality-guardrail`, `auth-patterns`, `cqrs-mediatr-guidelines`.

- [x] **2.2 Convert Commands And Queries By Application Ownership**
  - **Status:** COMPLETE — 590 compiled requests converted and request baseline entries removed; phase verification remains separate below.
  - **Files:** compiled request candidates under `src/Explore.Application/Features/**` and verified legacy locations; validators/handlers/downstream constructors.
  - **Acceptance:**
    - [x] Concrete eligible requests are sealed records.
    - [x] Required inheritance bases are abstract records.
    - [x] Commands carry only semantically used trusted IDs.
    - [x] Manual validation, cancellation, caching, and `BaseCommandResponse<T>` behavior remain.
    - [x] Resolved request baseline entries are removed.
  - **Effort:** XL.
  - **Dependencies:** 2.1.
  - **Guidance:** `cqrs-mediatr-guidelines`, `auth-patterns`, Application rule.

- [x] **2.3 Harden Request Collections And Logging Boundaries**
  - **Status:** COMPLETE — 18 request types/37 collection properties snapshot mutable inputs; Tier 1 request logging is bounded and value-free.
  - **Files:** collection-bearing converted requests; callers; zero-PII tests.
  - **Acceptance:**
    - [x] Mutable constructor inputs cannot alter immutable requests where promised.
    - [x] Equality tests do not assume sequence structural equality.
    - [x] No raw request, token, free text, user ID, or tenant ID enters Tier 1 logs.
  - **Effort:** L.
  - **Dependencies:** 2.2.
  - **Guidance:** `criticality-guardrail`, Application/Test rules.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Application DTOs And Immutable Payloads — COMPLETE

- [x] **3.1 Author Failing DTO Mapping And Serialization Specifications**
  - **Status:** COMPLETE — five intentional RED behaviors cover DTO equality/immutability and immutable payload snapshotting; mapping/wire/PATCH characterization remains green.
  - **Files:** focused `tests/Event.Application.UnitTests/DTOs/**`; mapping, payload-factory, and JSON-context tests.
  - **Acceptance:**
    - [x] RED tests cover construction, consumed equality, `with` variants, mapping, JSON, required/null behavior, PATCH presence, and payload serialization.
    - [x] PATCH omission, explicit clear, and replacement are distinct.
    - [x] Payload tests preserve version/idempotency/privacy facts.
  - **Effort:** XL.
  - **Dependencies:** 2.3.
  - **Guidance:** `outbox-pattern`, Application/Test rules.

- [x] **3.2 Convert Read And Projection DTOs**
  - **Status:** COMPLETE — all approved handwritten read/projection candidates are records; nominal forms preserve existing named construction, mapping composition, HAL enrichment, and serializer contracts.
  - **Files:** Phase 0 candidates under `src/Explore.Application/DTOs/**` and `Features/**/DTOs`; mappings/callers.
  - **Acceptance:**
    - [x] Short stable projections use positional records where safe.
    - [x] Long/optional/attribute-heavy projections use nominal records.
    - [x] Mapping/projection remains deterministic and translatable.
    - [x] HAL, ETag, pagination, and lookup fields retain intended meaning.
  - **Effort:** XL.
  - **Dependencies:** 3.1.
  - **Guidance:** `cqrs-mediatr-guidelines`, `clean-architecture-rules`.

- [x] **3.3 Convert HTTP Body DTOs And Remove Ambient Authority**
  - **Status:** COMPLETE — approved body contracts are records; eight ambient tenant fields were removed and server-owned tenant facts now flow through commands from trusted context.
  - **Files:** Phase 0 HTTP-body candidates/validators; downstream API/MCP/BFF constructors needed for compilation.
  - **Acceptance:**
    - [x] Bodies contain only client-owned fields.
    - [x] Route/current user/current tenant authority is absent.
    - [x] Legitimate target IDs have explicit dispositions.
    - [x] PATCH uses nominal records where presence semantics require it.
    - [x] ASP.NET positional records have one public constructor and parameter metadata.
  - **Effort:** XL.
  - **Dependencies:** 3.1, 2.2.
  - **Guidance:** `auth-patterns`, `criticality-guardrail`, Application/API rules.

- [x] **3.4 Convert Immutable Outbox Payload Snapshots**
  - **Status:** COMPLETE — immutable payload snapshots are records with defensive collection copies where published; persisted outbox lifecycle types remain mutable classes.
  - **Files:** Phase 0 payload contracts/factories across notification, registration, moderation, federation, integration, and webhook features.
  - **Acceptance:**
    - [x] Immutable payload snapshots are sealed records.
    - [x] Outbox entities/repositories/leases/retries/processors remain classes.
    - [x] Versioned payload JSON round-trips.
    - [x] PII-bearing payloads are never whole-record logged.
    - [x] No compatibility reader is added.
  - **Effort:** L.
  - **Dependencies:** 3.1.
  - **Guidance:** `outbox-pattern`, `criticality-guardrail`.

- [x] **3.5 Align Mapping, JSON Contexts, And Downstream Compilation**
  - **Status:** COMPLETE — existing Phase 3 evidence proves mapping, source-generated JSON, repaired downstream callers, generator-only artifact changes, and the exact eight-entry retained class baseline.
  - **Files:** `src/Explore.Application/Profiles/**/*.cs`; `Serialization/ExploreJsonContext.cs`; verified callers; DTO baseline.
  - **Acceptance:**
    - [x] AutoMapper and constructor mappings are valid.
    - [x] JSON source generation includes required records.
    - [x] Object-initializer callers are updated.
    - [x] No generated output is hand-edited.
    - [x] Only reasoned class baseline entries remain.
  - **Effort:** XL.
  - **Dependencies:** 3.2–3.4.
  - **Guidance:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 4: API Trust Boundary And OpenAPI Contract — VERIFICATION BLOCKED

- [x] **4.1 Author Failing HTTP And Trust-Boundary Invariant Breakers**
  - **Status:** COMPLETE — HTTP forged-tenant input fails closed before dispatch; live OpenAPI pins all eight authority-free schemas and existing tests cover identity absence, route/body conflict, PATCH presence, ProblemDetails, and legitimate targets.
  - **Files:** focused `tests/Event.API.IntegrationTests/Features/**`, `Authentication/**`, `Hosting/**`; OpenAPI tests; body-authority dispositions.
  - **Acceptance:**
    - [x] Body current-user/current-tenant spoofing fails.
    - [x] Missing, wrong-tenant, and conflicting identity cases fail under existing policy.
    - [x] Route/body conflicts, PATCH presence, validation metadata, ProblemDetails codes, and OpenAPI required/null schemas are covered.
    - [x] Legitimate target IDs remain operation-scoped.
  - **Effort:** XL.
  - **Dependencies:** 3.3.
  - **Guidance:** Tier 1 Red phase; `criticality-guardrail`, `auth-patterns`, API/Test rules.

- [x] **4.2 Refactor Controllers And API Models**
  - **Status:** COMPLETE — affected controllers use trusted tenant/route context, reject removed authority fields, preserve authentication and command dispatch, and now publish complete 400/401/403 ProblemDetails metadata.
  - **Files:** affected `src/Explore.API/Controllers/**/*.cs`; `Models/**/*.cs`; trusted context dependencies; direct MCP/BFF adapters.
  - **Acceptance:**
    - [x] Controllers use established principal/tenant/route authority.
    - [x] No raw-claim helper or `RequestServices` lookup is introduced.
    - [x] Immutable API models become records; framework-populated query/form/inheritance models retain classes.
    - [x] Writes remain authorized and failures remain ProblemDetails.
  - **Effort:** XL.
  - **Dependencies:** 4.1.
  - **Guidance:** `auth-patterns`, `cqrs-mediatr-guidelines`, API/Auth rules.

- [x] **4.3 Regenerate And Document The API Contract**
  - **Status:** COMPLETE — documented generators refreshed OpenAPI, the byte-identical API inventory, and NSwag client; eight tenant fields were removed, typed authorization errors added, and all 776 operation IDs remained stable.
  - **Files:** generated `schemas/openapi_islamu-event.json`; generated `docs/API_CONTRACT_INVENTORY.md`; `docs/API_CHANGELOG.md`; OpenAPI catalogs/tests.
  - **Acceptance:**
    - [x] Source generators, not manual edits, create artifacts.
    - [x] Body schemas omit current-authority fields.
    - [x] Runtime and OpenAPI required/null metadata agree.
    - [x] Every intentional break is documented with no alias.
  - **Effort:** L.
  - **Dependencies:** 4.2.
  - **Guidance:** `openapi-contract-change`, API rule.

- [x] **4.4 Close Tier 1 Mutation And Adversarial Review Evidence**
  - **Status:** COMPLETE — scoped Stryker passed at 85.19% (23 killed, 2 survived; only `ImportEventCommandHandler.cs` executed), zero-PII evidence passed, and Security-60% anonymized MAD approved after the accepted finding was repaired.
  - **Files:** existing `stryker-config.json` only if a reusable scoped change is required; changed identity code/tests; `.omo/start-work/artifacts/records-adoption/phase4/` structured findings.
  - **Acceptance:**
    - [x] Scoped mutation score is above 85%.
    - [x] MAD output is anonymized and uses Security 60% weighting.
    - [x] Accepted findings include reproducible tests and corrections.
    - [x] Zero-PII log evidence passes.
    - [x] Any unresolved critical finding blocks the phase.
  - **Effort:** L.
  - **Dependencies:** 4.1–4.3.
  - **Guidance:** `criticality-guardrail`, `epistemic-mad-review`, `auth-patterns`.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 39 projects, 0 errors, 6,901 warnings.
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - **Blocked result:** 2,264 total; 2,255 passed, 1 skipped, 8 failed. Five deterministic failures pass focused reruns after two caller repairs; three production-auth guardrail cases remain blocked by privacy-erasure replay startup requiring unavailable Infisical credentials.

## Phase 5: Generated Client And Blazor Immutable State — COMPLETE

- [x] **5.1 Author Failing Client Serialization And State Specifications**
  - **Status:** COMPLETE — focused RED captured 4 passed/2 failed for missing value equality and defensive snapshots; generated authority-free classes and mutable form state were green anchors, and final contract/AOT coverage passes 8/8.
  - **Files:** `tests/Explore.Blazor.Client.Tests/Services/EventApiClientSerializationTests.cs`; candidate model/component/validator tests.
  - **Acceptance:**
    - [x] Stale generated request/response shapes fail.
    - [x] Authority fields are absent.
    - [x] Immutable snapshot equality/`with` behavior is tested where consumed.
    - [x] Mutable edit-state classes are explicitly protected.
    - [x] HAL remains the only action authority.
  - **Effort:** L.
  - **Dependencies:** 4.3.
  - **Guidance:** `blazor-ui-conventions`, Blazor/Test rules.

- [x] **5.2 Regenerate NSwag And Repair Client Services**
  - **Status:** COMPLETE — documented generation was byte-identical (`32e942…d419` before/after); generated classes remain untouched, eight write bodies contain no tenant authority, and existing HAL/ProblemDetails service paths compile unchanged.
  - **Files:** generated `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`; affected handwritten services/validators/components.
  - **Acceptance:**
    - [x] `GenerateApiClient` produces deterministic generated classes.
    - [x] Generated code has no manual edits.
    - [x] Services send only client-owned body data.
    - [x] HAL and ProblemDetails handling remain.
  - **Effort:** XL.
  - **Dependencies:** 5.1.
  - **Guidance:** `blazor-ui-conventions`, API/Blazor rules.

- [x] **5.3 Convert Immutable Presentation Models**
  - **Status:** COMPLETE — nine approved local result/filter/snapshot classes are sealed records with defensive collection copies; mutable form/component/auth/PII/BFF payload state remains class-based and accessibility behavior was not touched.
  - **Files:** Phase 0 Blazor result/filter/dialog candidates; direct consumers; class baseline.
  - **Acceptance:**
    - [x] Immutable local snapshots use sealed records.
    - [x] Collection/equality/rerender semantics are intentional.
    - [x] Form/edit/component identity state remains class-based.
    - [x] No generated DTO wrapper exists solely for keyword consistency.
    - [x] Accessibility behavior is unchanged.
  - **Effort:** L.
  - **Dependencies:** 5.1–5.2.
  - **Guidance:** `blazor-ui-conventions`, Blazor rule.

- [x] **5.4 Align Blazor JSON Source Generation**
  - **Status:** COMPLETE — dock-layout snapshot/envelope metadata is source-generated and used by persistence, AOT round trips pass, and provider-credential metadata remains absent.
  - **Files:** `src/Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs`; affected tests; baseline.
  - **Acceptance:**
    - [x] Required local records/generated DTOs are registered.
    - [x] Stale registrations are removed.
    - [x] No provider credential contract is included.
    - [x] AOT source-generated round trips match service settings.
  - **Effort:** M.
  - **Dependencies:** 5.2–5.3.
  - **Guidance:** `blazor-ui-conventions`, Blazor/Test rules.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 39 projects, 0 errors, 7,115 warnings.
- [x] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` — recovery run 2,553 passed, 0 warnings after repairing detached-worktree `.git` file discovery; initial run was 2,552 passed, 1 failed, 1 skipped.

## Phase 6: Governance Closure And Release Contribution — COMPLETE

- [x] **6.1 Tighten Final Ratchets And Remove Resolved Debt**
  - **Status:** COMPLETE — final schemas accept only ten retained mutable response hierarchies and seven legitimate targets after integrating the paid-registration response envelopes from `develop`; zero concrete MediatR class debt and exact bidirectional stale/missing checks pass 10/10.
  - **Files:** `RecordContractArchitectureTests.cs`; both baseline JSON files.
  - **Acceptance:**
    - [x] No eligible concrete MediatR class debt remains.
    - [x] DTO baseline contains only semantic/framework/generated exclusions.
    - [x] No current-authority body disposition remains.
    - [x] Every stale/missing/new debt entry fails.
  - **Effort:** M.
  - **Dependencies:** 5.4.
  - **Guidance:** `review-pr`, Architecture/Test rules.

- [x] **6.2 Synchronize Architecture And Contributor Documentation**
  - **Status:** COMPLETE — named canonical docs/rules now agree on shallow record semantics, trusted authority, generated/PATCH/outbox/Blazor ownership, and no-shim migration without deep-immutability claims.
  - **Files:** `docs/GOVERNANCE.md`; `docs/ARCHITECTURE.md`; `docs/API.md`; `docs/API_CHANGELOG.md`; `docs/OUTBOX_PATTERN.md`; `docs/BLAZOR.md`; relevant `.agents/rules/*.md`.
  - **Acceptance:**
    - [x] Docs describe implemented type selection and trusted flow.
    - [x] Generated ownership, PATCH binding, collection/equality, outbox split, and UI-state split are accurate.
    - [x] No deep-immutability/thread-safety/structural-sequence claim is overstated.
    - [x] I-VSD link and source-of-truth references agree.
  - **Effort:** M.
  - **Dependencies:** 6.1.
  - **Guidance:** matched docs/rules.

- [x] **6.3 Changelog Contribution And Final Commit Composition**
  - **Status:** COMPLETE — previously unclaimed CHG-2026-0010 uses the approved architecture scope, all six impact objects pass ReleaseInputPolicy, and the breaking commit message is composed but not executed.
  - **Files:** `docs/releases/changes/CHG-2026-0010.yaml` (new; availability rechecked before creation).
  - **Acceptance:**
    - [x] Tier 2 fragment covers Breaking, Security, Migration, Configuration, OpenAPI, and Operator impacts.
    - [x] `ReleaseInputPolicy` validation passes.
    - [x] Proposed subject is `refactor(architecture)!: adopt semantics-first record contracts`.
    - [x] `BREAKING CHANGE:` and `Change-Id: CHG-2026-0010` are composed.
    - [x] No `Changelog: skip` trailer is used.
    - [x] No commit/tag/push/publish occurs without explicit user authorization.
  - **Effort:** S.
  - **Dependencies:** 6.2 and all functional tasks.
  - **Guidance:** release governance, `conventional-commit`.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` — 39 projects, 0 errors, 0 warnings.
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — 453 passed, 0 failed, 0 warnings.

## Remaining / Deferred Work

- Immutable `BaseCommandResponse<T>`/result-factory redesign — separate workstream because it changes handler failure construction.
- New `Money`, `GeoCoordinate`, `DateRange`, or other domain concepts — only after independent domain-duplication evidence.
- EF schema changes from value-object redesign — separate migration intent; generated migrations only.
- Repository-wide immutable-collection standard — outside converted immutable contracts.
- Changing NSwag DTO generation from POCO classes — not part of records adoption.

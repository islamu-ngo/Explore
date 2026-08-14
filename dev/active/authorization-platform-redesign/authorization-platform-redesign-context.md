<!-- ABOUTME: Resume context for the simplified authorization platform redesign workstream. -->
<!-- ABOUTME: Records verified architecture, CTO decisions, research provenance, baseline warnings, and the next implementation slice. -->

# Authorization Platform Redesign — Context

Last Updated: 2026-08-15 Europe/Brussels

## SESSION PROGRESS (2026-08-15 Europe/Brussels)

### ✅ COMPLETED

- Classified the work against `.agents/contract/intents.yaml` and loaded the authorization, CQRS, persistence, HAL, BFF, Blazor, operations, planning, CTO-review, and clean-room rules.
- Verified the current endpoint → MediatR `AuthorizationBehavior` → `RuntimeAuthorizationProvider` → Local/Cerbos architecture and the parallel HAL evaluation surface.
- Reviewed the previous 10-phase plan, context, and task ledger against repository conventions, Clean Architecture, tenant-isolation, self-hosting, and enterprise-operations requirements.
- Researched current Microsoft authorization guidance through Context7 and primary Microsoft, NIST, Cerbos, and OpenID sources under the clean-room boundary.
- Rejected the generic policy AST/compiler/store and Policy Studio scope; replaced it with a typed Application decision port, two provider adapters, shared scenarios, operability, bounded query protection, and legacy deletion.
- Completed Phase 0 runtime containment: 171 mutating MediatR surfaces now have deterministic evidence-backed dispositions with zero unresolved entries.
- Added provider-neutral MediatR, HAL, Runtime, Local, and Cerbos scenarios, then removed the abandoned cross-project scenario catalog.
- Made BYO Cerbos failures, missing bootstrap ownership, broad instance-administrator grants, machine identity bleed, and locked machine tenant settings fail closed.
- Secured analytics and localization administration, invitation ownership, organization-review authorship, EventSeries tenant administration, and group creator binding.
- Consumed the Phase 0 Release build and architecture-test gate and recorded the pre-existing failures below.

### 🟡 IN PROGRESS

- Phase 1 is active: introduce the typed Application authorization boundary, beginning with trusted storage pre-create owner facts.

### ⏭️ NEXT

1. Add the smallest Application-owned typed request, decision, capability, provider-metadata, and fact contracts.
2. Introduce trusted `StorageUploadIntentFacts` from authenticated subject, tenant, and loaded owning-resource evidence.
3. Adapt the provider port without moving provider concerns into Domain or introducing a generic fact dictionary, AST, or DSL.

### ⚠️ BLOCKERS

- The local .NET workload manifest set is currently inconsistent (`MSB4242`, missing workload set `10.0.301.1`). Focused tests run with `MSBuildEnableWorkloadResolver=false`; repair of the developer SDK installation is outside this workstream.
- Permission widening remains forbidden without separate explicit approval.
- Tavily MCP research was attempted as requested but every call returned HTTP 432 because the configured Tavily plan usage limit was exceeded. Context7 and primary-source Anysearch results were used for the remaining research.
- Phase 1 must resolve storage pre-create facts, lifecycle vocabulary, contact-share typed facts, and Local/Cerbos instance-administrator parity without compatibility shims.

## Quick Resume

Phase 0 is complete and runtime authorization behavior is deliberately narrower. The deterministic inventory has 171 evidence-backed MediatR dispositions and zero unresolved violations; BYO/provider, administrator, machine, governance, ownership, and invitation gaps now fail closed.

Phase 1 is active. Implement the typed Application boundary and trusted storage pre-create facts first, then adapt Local and Cerbos behind the typed port. Do not recreate the rejected policy language, compiler, generic dictionary, or control-plane design.

## Verified Current Architecture

- Endpoint metadata controls authentication defaults: GET is anonymous unless explicitly protected; writes require authorization.
- `AuthorizationBehavior` is the authoritative MediatR resource/action enforcement point.
- `RuntimeAuthorizationProvider` routes to Local and Cerbos behavior and currently contains provider-selection complexity.
- Local behavior is distributed across `FallbackAuthorizationService` partials and feature-specific evaluators; Cerbos has separately maintained semantics.
- `HateoasAuthorizationEvaluator` evaluates candidate links; `_links` is the sole client-side action-affordance authority.
- Keycloak authenticates. Event domain facts, tenant state, membership, consent, legal entities, and resource state authorize.
- Tenant query filters and entity-first repositories remain mandatory isolation boundaries.
- The BFF keeps tokens server-side and is not an authorization decision point.

## Fixed Design Decisions

1. The typed authorization request/decision contract belongs to Application; Domain owns facts and invariants only.
2. The typed contract is not a policy DSL. Capability-specific typed facts are allowed; a generic AST or arbitrary fact dictionary is not.
3. Local and Cerbos remain explicit provider adapters behind one port and must pass one provider-neutral scenario corpus.
4. MediatR and HAL cut over together so enforcement and affordances cannot drift.
5. Local mode is self-contained and deploys policy behavior with the application. Cerbos mode uses Cerbos-native policy artifacts and observed revisions.
6. Provider selection/configuration is versioned and observable; sensitive operations fail closed on health/revision uncertainty.
7. Existing cache and outbox infrastructure carries Event-owned configuration invalidation. No new policy publication subsystem is planned.
8. Query authorization is added only to named disclosure-sensitive collections and executes before count/pagination/projection.
9. Breaking replacement is required; no compatibility shim or dual production authority.
10. Permission behavior may be preserved or narrowed only unless explicit widening approval is added.

## Deleted And Deferred Scope

Deleted from this workstream: Domain policy AST, persisted universal policy generations, AST-to-Cerbos compiler, generic policy store/control plane, compatibility translations, and BFF/UI-local authorization gates.

Deferred to separate future workstreams: tenant external PDP, policy administration API, Policy Studio, user-authored DSL, policy import/export, Keycloak/OpenID CAEP integration, and database row-level security.

## Research Source Register

External sources were used only for neutral behavioral constraints. No source code, policy text, schemas, ASTs, SQL, migrations, tests, assets, or copied implementation structure were retained.

| Source | Accessed | Repository-relevant observation |
|---|---|---|
| Microsoft Learn, ASP.NET Core policy-based authorization, via Context7 `/dotnet/aspnetcore.docs` | 2026-08-14 | Requirements/handlers and policies are native extension points; default and fallback policies have distinct roles. |
| Microsoft Learn, ASP.NET Core resource-based authorization | 2026-08-14 | Loaded-resource decisions require imperative authorization after resource retrieval; endpoint metadata alone is insufficient. |
| Microsoft Learn, ASP.NET Core authorization views | 2026-08-14 | Hiding UI controls does not secure the protected operation. |
| NIST SP 800-207, Zero Trust Architecture, `https://csrc.nist.gov/pubs/sp/800/207/final` | 2026-08-14 | Separate decision/enforcement responsibilities, least privilege, and continuing evaluation. |
| Cerbos documentation, PDP deployment/policy distribution/audit guidance, `https://docs.cerbos.dev/` | 2026-08-14 | Cerbos owns Cerbos policy validation/distribution/revision behavior; Event should adapt rather than compile a second policy language. |
| OpenID Shared Signals Framework 1.0 and CAEP 1.0 final specifications, `https://openid.net/specs/` | 2026-08-14 | Standards-based identity/session security events are a future integration concern, not an authorization publication protocol. |

## Validation Baseline

- Phase 0 Release build passed with 0 errors and 10 existing `SSH.NET` advisory warnings.
- Phase 0 architecture suite: 377 total, 372 passed, 1 skipped, and 4 pre-existing failures covering DTO suffixes, an unnamed tenant-filter bypass, Blazor-owned analytics contracts, and privacy-inventory omissions. No new authorization failure was introduced.
- The authorization guardrail independently confirmed 171 deterministic dispositions, 0 unresolved violations, and 0 unclassified mutation surfaces.
- Phase 0 evidence is recorded in `.omo/start-work/artifacts/authorization-platform-redesign/phase0-verification.json` and the task01/task03 artifact directories.

- Planning-start build succeeded in 7.11 seconds with 0 errors and 10 warnings.
- Final Release build succeeded in 50.40 seconds with 0 errors and 765 pre-existing warnings. These include `SSH.NET` 2025.1.0 advisory `GHSA-q939-rpr3-3284`, analyzer findings, obsolete test-container constructors, and existing test-code warnings; none originate from these Markdown-only changes.
- Final architecture test result: 373 total, 368 passed, 1 skipped, and 4 pre-existing failures. The failures cover two registration-form input types without the `Dto` suffix, an unnamed tenant-filter bypass in `RegistrationFormAuthoringRepository`, Blazor-owned registration-answer analytics contracts, and two missing privacy-inventory properties.
- The previous context claim of 9,099 warnings was stale and has been removed.

## Current Risks And Unknowns

- The full live capability/bypass inventory must be completed in Phase 0 before naming the final typed catalog.
- Exact Local/Cerbos semantic mismatches must be classified as defects, intentional narrowing, or unsupported deny cases.
- Existing cache/outbox infrastructure must be traced before setting the cross-replica convergence bound.
- Disclosure-sensitive collection inventory is intentionally deferred to Phase 4 entry; a universal query planner is forbidden.
- Cerbos observed-revision and health signals must be confirmed against the installed integration before implementation details are fixed.

## Handoff Notes

### Handoff — 2026-08-15 Europe/Brussels

- Start with `authorization-platform-redesign-tasks.md`; Phase 1 Task 1.1 is active.
- Do not recreate the rejected AST/compiler/policy-store design under different names.
- Treat HAL parity as authorization correctness, not UI work.
- Preserve tenant filters, entity-returning repositories, handler DTO mapping, manual validators, BFF token secrecy, and generated-migration discipline.
- Introduce typed storage pre-create ownership evidence before tightening `StorageObjects.Create`; missing subject, tenant, or ownership facts must deny.
- Record exact phase-end build/test results here and update the task count immediately.

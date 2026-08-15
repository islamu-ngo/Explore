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
- Completed Phase 0 runtime containment: 177 mutating MediatR surfaces now have deterministic evidence-backed dispositions with zero unresolved or unclassified entries, including all six Phase 17 promotion apply/remove requests.
- Added provider-neutral MediatR, HAL, Runtime, Local, and Cerbos scenarios, then removed the abandoned cross-project scenario catalog.
- Made BYO Cerbos failures, missing bootstrap ownership, broad instance-administrator grants, machine identity bleed, and locked machine tenant settings fail closed.
- Secured analytics and localization administration, invitation ownership, organization-review authorship, EventSeries tenant administration, and group creator binding.
- Consumed the Phase 0 Release build and architecture-test gate and recorded the pre-existing failures below.
- Introduced the Phase 1 typed port. `IAuthorizationProvider` exposes only `AuthorizeAsync` and `AuthorizeBatchAsync`, and stale interface consumers/test doubles were migrated without a compatibility shim; Task 1.1 is reopened because the request still carries a legacy attribute dictionary fallback.
- Cut both consumers over to the typed methods: `AuthorizationBehavior` uses the single-decision path and `HateoasAuthorizationEvaluator` normalizes, deduplicates, and batch-evaluates requests. Phase 2 Task 2.1 is reopened until migrated requests no longer derive authority from the legacy dictionary.
- Removed the ambiguous unused generic `RequirePermission` overload while preserving resource-kind and descriptor call shapes.
- Verified focused current-source lanes: API HATEOAS authorization 28/28, Application authorization 37/37 plus the exact follow-up 1/1, and authorization guardrails 6/6. Relevant Application/API compilation succeeded with the workload resolver disabled.
- Fixed the focused Local contact-share regression: missing tenant metadata again uses the trusted current tenant while an explicit mismatch still denies. The exact test passed 1/1, full `FallbackAuthorizationServiceTests` passed 150/150, and the resolver-disabled Infrastructure Release build completed with 0 warnings and 0 errors.

### 🟡 IN PROGRESS

- Independent Phase 2 security review reopened Phase 1 Task 1.1 and Phase 2 Task 2.1: the typed port is live, but providers still consult `AuthorizationRequest.ResourceAttributes` whenever closed typed facts are absent.
- Protected Blazor fallbacks are removed for organization members, EventTeam, and event publisher selection. EventTeam now preserves collection `assign-event-role` and item `revoke` links through its service, but its CQRS requests still need the same canonical `events.manage-team` enforcement as HAL.
- The six focused Cerbos/runtime regressions are repaired. The seven-case provider-neutral test is correctly classified as adapter/projection smoke coverage, not full Local/Cerbos policy parity; machine, support, missing tenant/resource, consent, guest, public, and live-policy cases remain open.
- Do not mark Phase 2 parity or any Phase 3-5 work complete until their own acceptance and phase gates pass.

### ⏭️ NEXT

1. Put EventTeam reads, presets, assignment, and revocation behind the same typed `events.manage-team` request used for its HAL relations; keep the role-authority ceiling as a post-allow business invariant.
2. Remove legacy dictionary influence from migrated request paths, then execute the complete Phase 0 provider-neutral corpus against Local and live Cerbos semantics.
3. Record only capability/category/outcome/provider/reason/revision diagnostics, then run the prescribed Phase 1 and Phase 2 gates once their criteria are satisfied.

### ⚠️ BLOCKERS

- The local .NET workload manifest set is currently inconsistent (`MSB4242`, missing workload set `10.0.301.1`). Focused tests run with `MSBuildEnableWorkloadResolver=false`; repair of the developer SDK installation is outside this workstream.
- Permission widening remains forbidden without separate explicit approval.
- Tavily MCP research was attempted as requested but every call returned HTTP 432 because the configured Tavily plan usage limit was exceeded. Context7 and primary-source Anysearch results were used for the remaining research.
- Phase 2 must complete provider-neutral Local/Cerbos parity and differential diagnostics without compatibility shims; the focused Local contact-share mismatch is resolved.

## Quick Resume

Phase 0 is complete and runtime authorization behavior is deliberately narrower. The deterministic inventory has 177 evidence-backed MediatR dispositions and zero unresolved or unclassified entries; BYO/provider, administrator, machine, governance, ownership, invitation, and all six Phase 17 promotion surfaces are classified.

The typed provider port and both MediatR/HAL consumers exist, but Phase 1 Task 1.1 and Phase 2 Task 2.1 are reopened until migrated requests cannot fall back to arbitrary attribute dictionaries. The focused Local contact-share regression and six Cerbos/runtime regressions are fixed; the full provider-neutral Local/Cerbos corpus remains open. Do not recreate the rejected policy language, compiler, generic dictionary, or control-plane design, and do not infer that focused green tests prove full Phase 2 parity.

## Verified Current Architecture

- Endpoint metadata controls authentication defaults: GET is anonymous unless explicitly protected; writes require authorization.
- `AuthorizationBehavior` is the authoritative MediatR resource/action enforcement point and consumes `IAuthorizationProvider.AuthorizeAsync`.
- `RuntimeAuthorizationProvider` routes to Local and Cerbos behavior and currently contains provider-selection complexity.
- Local behavior is distributed across `FallbackAuthorizationService` partials and feature-specific evaluators; Cerbos has separately maintained semantics.
- `HateoasAuthorizationEvaluator` normalizes and deduplicates candidate links, then consumes `IAuthorizationProvider.AuthorizeBatchAsync`; `_links` is the sole client-side action-affordance authority.
- `IAuthorizationProvider` exposes only typed `AuthorizeAsync`/`AuthorizeBatchAsync` production methods. Concrete Local/Cerbos test-helper vocabulary is not a second production interface authority.
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
- The authorization guardrail independently confirmed 177 deterministic dispositions, 0 unresolved violations, and 0 unclassified mutation surfaces.
- Phase 0 evidence is recorded in `.omo/start-work/artifacts/authorization-platform-redesign/phase0-verification.json` and the task01/task03 artifact directories.

- Planning-start build succeeded in 7.11 seconds with 0 errors and 10 warnings.
- Final Release build succeeded in 50.40 seconds with 0 errors and 765 pre-existing warnings. These include `SSH.NET` 2025.1.0 advisory `GHSA-q939-rpr3-3284`, analyzer findings, obsolete test-container constructors, and existing test-code warnings; none originate from these Markdown-only changes.
- Final architecture test result: 373 total, 368 passed, 1 skipped, and 4 pre-existing failures. The failures cover two registration-form input types without the `Dto` suffix, an unnamed tenant-filter bypass in `RegistrationFormAuthoringRepository`, Blazor-owned registration-answer analytics contracts, and two missing privacy-inventory properties.
- The previous context claim of 9,099 warnings was stale and has been removed.
- Current closeout receipts: API `HateoasAuthorizationEvaluatorTests` passed 28/28; the Application authorization compatibility selector passed 37/37 and its exact follow-up passed 1/1; `AuthorizationSurfaceGuardrailTests` passed 6/6; relevant resolver-disabled Application/API builds compiled with zero errors.
- Resolved regression receipt: the contact-share guard now uses `ResolveTenantId`'s trusted current-tenant fallback while preserving explicit mismatch denial; the exact test passed 1/1, full `FallbackAuthorizationServiceTests` passed 150/150, and the resolver-disabled Infrastructure Release build completed with 0 warnings/errors. This focused receipt is not a provider-parity completion signal.

## Current Risks And Unknowns

- Remaining Local/Cerbos semantic mismatches must be classified as defects, intentional narrowing, or unsupported deny cases through the full provider-neutral corpus.
- Existing cache/outbox infrastructure must be traced before setting the cross-replica convergence bound.
- Disclosure-sensitive collection inventory is intentionally deferred to Phase 4 entry; a universal query planner is forbidden.
- Cerbos observed-revision and health signals must be confirmed against the installed integration before implementation details are fixed.

## Handoff Notes

### Handoff — 2026-08-15 Europe/Brussels

- Start with `authorization-platform-redesign-tasks.md`; 5/18 implementation tasks are currently confirmed. Task 1.1 and Tasks 2.1-2.3 are open, and their exact phase gates remain pending.
- Preserve the typed production-only `IAuthorizationProvider` port and the shared MediatR/HAL decision flow; do not restore stale interface methods or the removed ambiguous `RequirePermission` overload.
- Resume with the complete Local/Cerbos provider-neutral corpus and safe differential diagnostics. The contact-share regression is already fixed and verified 1/1 plus 150/150; do not redo it or treat it as full parity.
- Do not recreate the rejected AST/compiler/policy-store design under different names.
- Treat HAL parity as authorization correctness, not UI work.
- Preserve tenant filters, entity-returning repositories, handler DTO mapping, manual validators, BFF token secrecy, and generated-migration discipline.
- Missing subject, tenant, resource, or trusted facts must continue to deny; Cerbos storage pre-create remains fail closed until its policy mapping is implemented.
- Do not check Phase 2 parity, its acceptance criteria, or any Phase 3-5 item from the current focused receipts. Run and record each exact phase gate only when its phase criteria are actually met.

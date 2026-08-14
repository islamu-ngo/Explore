<!-- ABOUTME: Hot execution ledger for API-wide code-liability reduction. -->
<!-- ABOUTME: Separates behavior characterization, implementation, and phase verification. -->

# API-Wide Code Liability Reduction — Task Checklist

Last Updated: 2026-08-14 Europe/Brussels

## Status Summary
- **Overall status:** Phase 1 implementation complete; repository-wide verification blocked by pre-existing worktree/toolchain failures
- **Completed:** 5/26 implementation tasks (verification separate)
- **Current priority:** Phase 2.2 explicit identity injection and provider-identity authority.
- **Next recommended slice:** Replace controller service location and ordinary claim parsing while preserving purpose-bound machine/bootstrap/receipt principals.

## Maintenance Rules
- Read all three artifacts once initially; on resume read context/tasks and only the current plan phase.
- Mark substantial tasks immediately and reconcile small tasks by phase end.
- Characterization and implementation are separate tasks; never consolidate an unpinned security/reliability seam.
- Update context for phase/decision/blocker/failure/discovery/handoff; update plan only for strategy changes.
- Run verification once at phase end, not per task. Never start browser, Aspire, Docker, or live services for this workstream.

## Phase 1: Contracts, dead paths, mechanical adapters ⏳
- [x] **1.1 Pin externally observable API invariants** — hotspot route/auth/cache/status/ProblemDetails/HAL authorities recorded without duplicate tests; **Effort L**.
- [x] **1.2 Delete confirmed compatibility/dead presentation paths** — removed zero-caller tenant and permission compatibility paths with reference evidence; **Effort M**; depends 1.1.
- [x] **1.3 Normalize truly mechanical controller adapters** — 28 controllers, net 174 lines removed, independent review approved; **Effort M**; depends 1.1.
- [x] **1.4 Converge API contract and controller documentation** — generated OpenAPI path, tenant ownership, and authorization test guidance now match code; **Effort M**; depends 1.2–1.3.

### Phase 1 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2: Identity authority ⏳
- [x] **2.1 Characterize internal, provider-bootstrap, API-key, and diagnostic identity paths** — complete call-site matrix recorded in the evidence register; **Effort L**; depends Phase 1.
- [ ] **2.2 Remove controller service location/manual claims through explicit existing identity contracts** — all trust/fallback behavior tested; **Effort XL**; depends 2.1.
- [ ] **2.3 Make the identity authority unambiguous in canonical documentation** — delete claim-parsing/service-location guidance; **Effort M**; depends 2.2.

### Phase 2 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Command result and ProblemDetails authority ⏳
- [ ] **3.1 Inventory/pin failure taxonomies and public mappings** — status/detail/extensions/retry parity matrix; **Effort L**.
- [ ] **3.2 Generalize existing mapper with the smallest typed policy** — API-only, smaller than removed mappings; **Effort L**; depends 3.1.
- [ ] **3.3 Migrate proven controller cohorts and delete private switches** — net LOC decrease; **Effort XL**; depends 3.2.
- [ ] **3.4 Converge error-contract documentation and examples** — one mapping authority and taxonomy; **Effort M**; depends 3.3.

### Phase 3 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4: Hotspot controller families ⏳
- [ ] **4.1 Move non-HTTP orchestration into CQRS handlers** — HAL/headers/ProblemDetails stay API-owned; **Effort XL**; depends Phases 2–3.
- [ ] **4.2 Partition Event, Registration Order, Webhooks, Instance Settings, and Control Plane by stable capability** — one family at a time, exact routes preserved; **Effort XL/family**; depends 4.1.
- [ ] **4.3 Update capability ownership and endpoint maps per completed family** — no stale monolith paths; **Effort M/family**; depends 4.2 for that family.

### Phase 4 Verification — once per family
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 5: HAL registration ⏳
- [ ] **5.1 Characterize and service-resolve the complete HAL registration graph** — triples/exceptions/lifetimes explicit; **Effort M**.
- [ ] **5.2 Replace repeated triples with compile-time generic helpers** — no scanning/reflection; **Effort L**; depends 5.1.
- [ ] **5.3 Update HAL authoring and registration guidance** — one current helper example plus explicit exceptions; **Effort S**; depends 5.2.

### Phase 5 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Periodic worker lifecycle ⏳
- [ ] **6.1 Pin enablement, delays, intervals, scopes, cancellation, errors, and health semantics** — intentional differences recorded; **Effort L**.
- [ ] **6.2 Consolidate qualifying timer loops behind one small tested lifecycle** — at least three loops replaced, no outbox/retry weakening; **Effort XL**; depends 6.1.
- [ ] **6.3 Converge worker lifecycle and operations documentation** — implementation and runbooks agree; **Effort M**; depends 6.2.

### Phase 6 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 7: MCP decomposition ⏳
- [ ] **7.1 Pin every tool's authorization, HAL gate, bounds, truncation, disclosure, descriptor, and serialization contract** — complete matrix; **Effort L**.
- [ ] **7.2 Partition event MCP capabilities and consolidate only pure identical helpers** — protocol/security parity; **Effort XL**; depends 7.1.
- [ ] **7.3 Update MCP capability, security, and debugging documentation** — no stale monolith or bypass guidance; **Effort M**; depends 7.2.

### Phase 7 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 8: Composition and ratchets ⏳
- [ ] **8.1 Extract feature-cohesive host registration methods with visible concrete topology** — no module framework; **Effort L**; depends Phases 5–7.
- [ ] **8.2 Add forward-only architecture gates for eliminated liabilities** — no LOC/style tests; **Effort M**; depends completed consolidations.
- [ ] **8.3 Canonical documentation convergence and stale-guidance audit** — one authority per rule, zero retired patterns in current docs; **Effort L**; depends 8.2.

### Phase 8 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Deferred / Separate Workstreams
- Eliminate 758 build warnings by warning family and owning project; never suppress.
- Measured repository-query optimization and EF projection changes under `update-repository-query`.
- Intentional OpenAPI breaking changes under `openapi-contract-change`, with direct removal and no shim.
- UI/BFF simplification, persistence/migrations, and infrastructure-wide refactors outside the API seam.

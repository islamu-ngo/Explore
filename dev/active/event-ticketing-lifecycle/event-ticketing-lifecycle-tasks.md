<!-- ABOUTME: Hot execution ledger for the re-baselined Event Ticketing Lifecycle workstream. -->
<!-- ABOUTME: Owns atomic Red/Green tasks, PR dependencies, selected phase gates, and review handoff. -->

# Event Ticketing Lifecycle — Task Checklist

Last Updated: 2026-08-28 Europe/Brussels

## Status Summary

- **Overall status:** Purchase, readiness, transfer, and fair-return implementation surfaces are merged into `develop`, but exact task-closeout evidence is incomplete; ticketing branches/worktrees were removed.
- **Completed:** 0/51 tasks are claimed complete under the ledger's exact acceptance-evidence contract. Implemented surfaces exist through Phase 6, but their checkboxes remain open until RED chronology and required command/report evidence are retained.
- **Current priority:** reconcile missing Phase 0–6 task evidence, beginning with Phase 6 Task 6.8, before starting Phase 7.
- **Next implementation slice:** evidence-only closeout for the merged Phase 6 surface; do not change behavior unless the audit discovers a real defect.
- **I-VSD report:** [`i-vsd-event-ticketing-lifecycle.md`](../../../islamic-value-sensitive-design/i-vsd-event-ticketing-lifecycle.md)
- **I-VSD revision:** authoritative exact binding is recorded in the report metadata and current context.
- **I-VSD status / disposition:** current / plan-aligned.
- **CTO:** the revision-bound read-only review approved implementation; status-only reconciliation does not change its design inputs.
- **User approval:** continuation approved on 2026-08-27; implemented scope merged on 2026-08-28.
- **Compatibility:** direct replacement; delete superseded behavior/contracts/tests/docs in the owning PR.

## Execution Rules

1. Resume from context and the current task; read only its plan section.
2. Establish one scoped green baseline after exact-revision approval.
3. RED compiles/discovers and fails by assertion before production edits.
4. All focused commands use Release, exact TUnit selectors, `--minimum-expected-tests 1`, no progress, and one test at a time.
5. Concurrency tests install barriers/events before triggering contenders; sleeps, polling, and timing luck are forbidden.
6. Expected policy/order/money values are literal or independently calculated.
7. Every PR node owns Red, smallest Green, refactor/debt deletion, generated artifacts, affected docs, and focused evidence.
8. Phase closeout runs exactly one Release build plus at most the selected full non-browser project.
9. Tier 0–2 changes own real PostgreSQL races, zero-sentinel telemetry scans, phase-scoped Stryker break threshold 86 JSON, and anonymized MAD YAML.
10. Verification is limited to repository non-interactive unit/integration projects and generated/static contract checks.
11. Stop and refresh I-VSD/CTO after any authority, access, consent, refund, recovery, payout, scenario, or task-mapping change.
12. Every task resolves its ownership, planned test/evidence file, and required effort from the matrix below; paths are repository-relative.

Effort scale: **S** ≤0.5 day, **M** 1–2 days, **L** 3–5 days, **XL** must be split before implementation if it cannot finish inside one reviewable PR node.

Before editing an existing production seam, run this bounded discovery once and retain only paths under the active task's listed roots:

```bash
find src/Explore.Domain src/Explore.Application src/Explore.Persistence src/Explore.Infrastructure src/Explore.API src/Explore.Blazor src/Event.Web.BffHosting src/Explore.Blazor.Client src/Explore.Secrets src/Event.Wire.Contracts -type f \( -name '*.cs' -o -name '*.razor' -o -name '*.json' -o -name '*.md' \) -print
```

Record the exact existing files selected from that output in the task handoff. New tests/evidence use the exact planned paths below.

## Task Ownership And Effort Matrix

| Task | Bounded production/docs ownership | Exact planned test/evidence file | Effort |
|---|---|---|---:|
| 0.1 | `src/Explore.Domain`, `src/Explore.Application`, `src/Explore.Persistence`, `src/Explore.API` | `tests/Event.Architecture.Tests/TicketingLifecycleAuthorityArchitectureTests.cs` | M |
| 0.2 | `src/Explore.Domain`, `src/Explore.Application`, `src/Explore.Persistence`, `src/Explore.API` | `tests/Event.Architecture.Tests/TicketingLifecycleAuthorityArchitectureTests.cs` | L |
| 0.3 | `docs/GOVERNANCE.md`, `docs/OPERATIONS.md`, `dev/active/event-ticketing-lifecycle/evidence` | `tests/Event.Architecture.Tests/TicketingCriticalEvidenceContractTests.cs` | M |
| 1.1 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/TicketPurchaseGovernancePersistenceTests.cs` | L |
| 1.2 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/TicketPurchaseGovernancePersistenceTests.cs` | L |
| 1.3 | `src/Explore.Application` | `tests/Event.Application.UnitTests/TicketPurchaseGovernanceHandlerTests.cs` | M |
| 1.4 | `src/Explore.Application`, `docs/PAYMENTS.md`, `SECURITY.md`, `docs/SECURITY-MODEL.md`, `dev/active/event-ticketing-lifecycle/evidence` | `tests/Event.Architecture.Tests/TicketingCriticalEvidenceContractTests.cs` | M |
| 2.1 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Event.API.IntegrationTests/TicketPurchaseGovernanceApiTests.cs` | M |
| 2.2 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Explore.GeneratedContracts.Tests/TicketPurchaseGeneratedContractTests.cs` | M |
| 2.3 | `src/Explore.Blazor`, `src/Event.Web.BffHosting` | `tests/Explore.Blazor.IntegrationTests/TicketPurchaseGovernanceBffTests.cs` | M |
| 2.4 | `src/Explore.Blazor`, `src/Event.Web.BffHosting` | `tests/Explore.Blazor.IntegrationTests/TicketPurchaseGovernanceBffTests.cs` | M |
| 2.5 | `src/Explore.Blazor.Client` | `tests/Explore.Blazor.Client.Tests/TicketPurchaseGovernanceComponentTests.cs` | M |
| 2.6 | `src/Explore.Blazor.Client`, `docs/API_CONTRACT_INVENTORY.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/TESTING.md`, `docs/BLAZOR_DEV_WORKFLOW.md` | `tests/Explore.Blazor.Client.Tests/TicketPurchaseGovernanceComponentTests.cs` | M |
| 3.1 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/ParticipantAdmissionEligibilityPersistenceTests.cs` | L |
| 3.2 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/ParticipantAdmissionEligibilityPersistenceTests.cs` | L |
| 3.3 | `src/Explore.Application` | `tests/Event.Application.UnitTests/ParticipantAdmissionEligibilityTests.cs` | M |
| 3.4 | `src/Explore.Application`, `docs/PRIVACY_ERASURE.md`, `SECURITY.md`, `docs/SECURITY-MODEL.md`, `dev/active/event-ticketing-lifecycle/evidence` | `tests/Event.Architecture.Tests/TicketingCriticalEvidenceContractTests.cs` | M |
| 4.1 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Event.API.IntegrationTests/ParticipantReadinessApiTests.cs` | M |
| 4.2 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Explore.GeneratedContracts.Tests/ParticipantReadinessGeneratedContractTests.cs` | M |
| 4.3 | `src/Explore.Blazor`, `src/Event.Web.BffHosting` | `tests/Explore.Blazor.IntegrationTests/ParticipantReadinessBffTests.cs` | M |
| 4.4 | `src/Explore.Blazor`, `src/Event.Web.BffHosting` | `tests/Explore.Blazor.IntegrationTests/ParticipantReadinessBffTests.cs` | M |
| 4.5 | `src/Explore.Blazor.Client` | `tests/Explore.Blazor.Client.Tests/ParticipantReadinessComponentTests.cs` | M |
| 4.6 | `src/Explore.Blazor.Client`, `docs/PRIVACY_ERASURE.md`, `SECURITY.md`, `docs/SECURITY-MODEL.md`, `docs/BLAZOR.md`, `docs/TESTING.md` | `tests/Explore.Blazor.Client.Tests/ParticipantReadinessComponentTests.cs` | M |
| 5.1 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/TicketTransferConcurrencyTests.cs` | L |
| 5.2 | `src/Explore.Domain`, `src/Explore.Persistence`, `src/Explore.Infrastructure` | `tests/Event.Persistence.IntegrationTests/TicketTransferConcurrencyTests.cs` | L |
| 5.3 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Event.API.IntegrationTests/TicketTransferApiTests.cs` | M |
| 5.4 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Explore.GeneratedContracts.Tests/TicketTransferGeneratedContractTests.cs` | M |
| 5.5 | `src/Explore.Blazor`, `src/Event.Web.BffHosting`, `src/Explore.Blazor.Client` | `tests/Explore.Blazor.IntegrationTests/TicketTransferBffTests.cs`; `tests/Explore.Blazor.Client.Tests/TicketTransferComponentTests.cs` | M |
| 5.6 | `src/Explore.Blazor`, `src/Explore.Blazor.Client`, `SECURITY.md`, `docs/SECURITY-MODEL.md`, `docs/PRIVACY_ERASURE.md`, `dev/active/event-ticketing-lifecycle/evidence` | `tests/Event.Architecture.Tests/TicketingCriticalEvidenceContractTests.cs` | M |
| 6.1 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/FairReturnWaitlistConcurrencyTests.cs` | L |
| 6.2 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/FairReturnWaitlistConcurrencyTests.cs` | L |
| 6.3 | `src/Explore.Application`, `src/Explore.Infrastructure` | `tests/Explore.Infrastructure.Tests/FairReturnWaitlistOrchestrationTests.cs` | L |
| 6.4 | `src/Explore.Application`, `src/Explore.Infrastructure` | `tests/Explore.Infrastructure.Tests/FairReturnWaitlistOrchestrationTests.cs` | L |
| 6.5 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Event.API.IntegrationTests/FairReturnWaitlistApiTests.cs` | M |
| 6.6 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Explore.GeneratedContracts.Tests/FairReturnWaitlistGeneratedContractTests.cs` | M |
| 6.7 | `src/Explore.Blazor`, `src/Event.Web.BffHosting`, `src/Explore.Blazor.Client` | `tests/Explore.Blazor.IntegrationTests/FairReturnWaitlistBffTests.cs`; `tests/Explore.Blazor.Client.Tests/FairReturnWaitlistComponentTests.cs` | M |
| 6.8 | `src/Explore.Blazor`, `src/Explore.Blazor.Client`, `docs/PAYMENTS.md`, `docs/OPERATIONS.md`, `dev/active/event-ticketing-lifecycle/evidence` | `tests/Event.Architecture.Tests/TicketingCriticalEvidenceContractTests.cs` | L |
| 7.1 | `src/Explore.Domain`, `src/Explore.Persistence` | `tests/Event.Persistence.IntegrationTests/EventAddOnPersistenceTests.cs` | L |
| 7.2 | `src/Explore.Domain`, `src/Explore.Persistence`, `src/Explore.Infrastructure` | `tests/Event.Persistence.IntegrationTests/EventAddOnPersistenceTests.cs` | L |
| 7.3 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Event.API.IntegrationTests/EventAddOnApiTests.cs` | M |
| 7.4 | `src/Explore.API`, `src/Event.Wire.Contracts` | `tests/Explore.GeneratedContracts.Tests/EventAddOnGeneratedContractTests.cs` | M |
| 7.5 | `src/Explore.Blazor`, `src/Event.Web.BffHosting`, `src/Explore.Blazor.Client` | `tests/Explore.Blazor.IntegrationTests/EventAddOnBffTests.cs`; `tests/Explore.Blazor.Client.Tests/EventAddOnComponentTests.cs` | M |
| 7.6 | `src/Explore.Blazor`, `src/Explore.Blazor.Client`, `docs/PAYMENTS.md`, `docs/OPERATIONS.md`, `dev/active/event-ticketing-lifecycle/evidence` | `tests/Event.Architecture.Tests/TicketingCriticalEvidenceContractTests.cs` | M |
| 8.1 | `src/Explore.Domain`, `src/Explore.Persistence`, `src/Explore.Infrastructure` | `tests/Event.Persistence.IntegrationTests/TicketingLifecycleRecoveryInvariantTests.cs` | L |
| 8.2 | `src/Explore.Domain`, `src/Explore.Persistence`, `src/Explore.Infrastructure` | `tests/Event.Persistence.IntegrationTests/TicketingLifecycleRecoveryInvariantTests.cs` | L |
| 8.3 | `src/Explore.Infrastructure`, `src/Explore.Secrets`, `.env.example`, `docs/OPERATIONS.md` | `tests/Explore.Secrets.UnitTests/TicketingRecoveryOperatorContractTests.cs` | M |
| 8.4 | `src/Explore.Infrastructure`, `src/Explore.Secrets`, `.env.example`, `docs/OPERATIONS.md`, `SECURITY.md`, `docs/SECURITY-MODEL.md`, `dev/active/event-ticketing-lifecycle/evidence` | `tests/Event.Architecture.Tests/TicketingCriticalEvidenceContractTests.cs` | L |
| 9.1 | `src/Explore.API`, `src/Explore.Infrastructure`, `src/Explore.Blazor.Client` | `tests/Event.Architecture.Tests/TicketingDeploymentCapabilityMatrixTests.cs` | M |
| 9.2 | `src/Explore.API`, `src/Explore.Infrastructure`, `src/Explore.Blazor.Client` | `tests/Event.Architecture.Tests/TicketingDeploymentCapabilityMatrixTests.cs` | M |
| 9.3 | `src/Event.Wire.Contracts`, `docs/API_CONTRACT_INVENTORY.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `dev/active/event-ticketing-lifecycle` | `tests/Event.Architecture.Tests/TicketingLifecycleContractConvergenceTests.cs` | M |
| 9.4 | `docs/releases/changes`, `docs/API_CHANGELOG.md` | `eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ReleaseInputPolicyTests.cs` | S |

## Critical Evidence Artifact Contract

Closeout Tasks 0.3, 1.4, 3.4, 5.6, 6.8, 7.6, and 8.4 create the exact phase artifacts below. `TicketingCriticalEvidenceContractTests` parses the YAML/JSON rather than pinning prose and fails unless every required changed-project mutation report scores strictly greater than 85, sentinel PII scan is `pass`, anonymized MAD decision is `pass` with no unresolved critical vote, required docs/comments/generated artifacts are enumerated, and every referenced file exists.

Evidence validator command:

```bash
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketingCriticalEvidenceContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
```

| Phase | Evidence manifest | Stryker JSON | MAD YAML |
|---|---|---|---|
| 0 | `dev/active/event-ticketing-lifecycle/evidence/phase-0-evidence.yaml` | `dev/active/event-ticketing-lifecycle/evidence/phase-0/domain/mutation-report.json`; `dev/active/event-ticketing-lifecycle/evidence/phase-0/application/mutation-report.json` | `dev/active/event-ticketing-lifecycle/evidence/phase-0-mad-review.yaml` |
| 1 | `dev/active/event-ticketing-lifecycle/evidence/phase-1-evidence.yaml` | `dev/active/event-ticketing-lifecycle/evidence/phase-1/mutation-report.json` | `dev/active/event-ticketing-lifecycle/evidence/phase-1-mad-review.yaml` |
| 3 | `dev/active/event-ticketing-lifecycle/evidence/phase-3-evidence.yaml` | `dev/active/event-ticketing-lifecycle/evidence/phase-3/mutation-report.json` | `dev/active/event-ticketing-lifecycle/evidence/phase-3-mad-review.yaml` |
| 5 | `dev/active/event-ticketing-lifecycle/evidence/phase-5-evidence.yaml` | `dev/active/event-ticketing-lifecycle/evidence/phase-5/mutation-report.json` | `dev/active/event-ticketing-lifecycle/evidence/phase-5-mad-review.yaml` |
| 6 | `dev/active/event-ticketing-lifecycle/evidence/phase-6-evidence.yaml` | `dev/active/event-ticketing-lifecycle/evidence/phase-6/domain/mutation-report.json`; `dev/active/event-ticketing-lifecycle/evidence/phase-6/application/mutation-report.json`; `dev/active/event-ticketing-lifecycle/evidence/phase-6/infrastructure/mutation-report.json` | `dev/active/event-ticketing-lifecycle/evidence/phase-6-mad-review.yaml` |
| 7 | `dev/active/event-ticketing-lifecycle/evidence/phase-7-evidence.yaml` | `dev/active/event-ticketing-lifecycle/evidence/phase-7/mutation-report.json` | `dev/active/event-ticketing-lifecycle/evidence/phase-7-mad-review.yaml` |
| 8 | `dev/active/event-ticketing-lifecycle/evidence/phase-8-evidence.yaml` | `dev/active/event-ticketing-lifecycle/evidence/phase-8/domain/mutation-report.json`; `dev/active/event-ticketing-lifecycle/evidence/phase-8/infrastructure/mutation-report.json` | `dev/active/event-ticketing-lifecycle/evidence/phase-8-mad-review.yaml` |

## Evidence Reconciliation — 2026-08-28

The merged repository contains implementation and regression-test surfaces for Phases 0–6, but implementation presence is not equivalent to satisfying this ledger's exact task contract.

- No retained artifact proves the intended pre-GREEN assertion failure for any RED task. Do not manufacture retrospective RED evidence against already merged code.
- Phase 0, 1, 3, and 5 manifests, PII scans, MAD reviews, and mutation summaries exist, but several exact Stryker report paths and focused command transcripts named by the tasks are not retained.
- Phase 2 and Phase 4 implementation/tests exist, but no phase evidence manifest is retained and their exact focused command transcripts are absent.
- Phase 6 implementation, MAD, and PII evidence exist, but `phase-6-evidence.yaml`, the three required mutation reports, and deterministic scale transcripts are absent.
- The checked phase-verification rows below record commands whose final results were directly observed during merge verification. They do not close the task checkboxes above them.
- Historical RED chronology needs an explicit governance disposition if its original transcript cannot be recovered; current passing regression tests cannot honestly be relabeled as prior RED evidence.

## Phase 0: Lifecycle Authority Remediation — `FND` 🟡 IMPLEMENTATION PRESENT / TASK EVIDENCE OPEN

- [ ] **0.1 RED direct-mutation and duplicated-decision architecture contract**
  - **Acceptance:** prove persistence-level lifecycle authority, repeated command/worker/HAL decisions, and additions to oversized seams remain possible.
  - **Verify:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketingLifecycleAuthorityArchitectureTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** exact-revision approval and baseline.
- [ ] **0.2 GREEN semantic aggregate mutation and shared decision surface**
  - **Acceptance:** aggregate methods own normal transitions; commands/workers/HAL consume one domain decision surface; persistence remains storage authority only.
  - **Verify:** the Task 0.1 command passes.
  - **Dependencies:** 0.1.
- [ ] **0.3 REFACTOR debt deletion, seam-size ratchet, docs, mutation, and MAD**
  - **Acceptance:** duplicate/direct paths are deleted; capability-specific coordinators remain bounded; architecture/docs/comments converge; phase mutation score is >85; MAD has no unresolved critical vote.
  - **Verify:** the Task 0.1 command remains green; `dotnet stryker --project src/Explore.Domain/Explore.Domain.csproj --test-project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-0/domain` exits 0; `dotnet stryker --project src/Explore.Application/Explore.Application.csproj --test-project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-0/application` exits 0; `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketingCriticalEvidenceContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes; `git diff --check -- docs/GOVERNANCE.md docs/OPERATIONS.md` exits 0.
  - **Dependencies:** 0.2.

### Phase 0 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — final merged Release build passed with 0 errors.
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - Ticketing-specific architecture failures are cleared; the full project retains 12 inherited non-ticketing failures.

## Phase 1: Purchase Governance Core — `PUR-CORE` → `PUR-APP` 🟡 IMPLEMENTATION PRESENT / TASK EVIDENCE OPEN

- [ ] **1.1 RED S1-A/S1-B/S1-C persistence and authority races**
  - **Acceptance:** cover stable authority matrix, honest name-only controls, literal effective ceiling 4, context switching, unrelated members, durable business replay, loser rollback, and cross-tenant negatives.
  - **Verify:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketPurchaseGovernancePersistenceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `FND` / Task 0.3.
- [ ] **1.2 GREEN versioned purchase policy and tenant-qualified persistence**
  - **Acceptance:** pinned authority/policy dimension, UUIDv7 concurrency, tenant uniqueness, canonical locks, serializable retry, durable operation identity, generated migrations, and no provider I/O pass.
  - **Verify:** the Task 1.1 command passes on real PostgreSQL.
  - **Dependencies:** 1.1.
- [ ] **1.3 RED S1-A/S1-B/S1-C CQRS and failure-code contract**
  - **Acceptance:** handler tests fail for missing server-owned actor resolution, manual validation, cancellation, stable failure codes, and zero-sentinel output.
  - **Verify:** `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketPurchaseGovernanceHandlerTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 1.2.
- [ ] **1.4 GREEN CQRS integration, zero-PII evidence, docs, mutation, and MAD**
  - **Acceptance:** handlers integrate the policy/hold/order path without duplicate authority; docs and non-obvious comments converge; mutation >85 and MAD close.
  - **Verify:** the Task 1.3 command passes; `dotnet stryker --project src/Explore.Application/Explore.Application.csproj --test-project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-1` exits 0; the Task 0.3 `TicketingCriticalEvidenceContractTests` command passes for Phase 1; `git diff --check -- docs/PAYMENTS.md SECURITY.md docs/SECURITY-MODEL.md` exits 0.
  - **Dependencies:** 1.3.

### Phase 1 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — final merged Release build passed with 0 errors.
- [x] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` — 1,067 passed.

## Phase 2: Purchase Public Surfaces — `PUR-API` → `PUR-UI` 🟡 IMPLEMENTATION PRESENT / TASK EVIDENCE OPEN

- [ ] **2.1 RED S1-A/S1-B/S1-C API/HAL/OpenAPI contract**
  - **Acceptance:** cover auth, tenant spoofing, capability equivalence, private/no-store failures, scoped idempotency/rate limits, HAL actions, honest name-only copy, and schema shape.
  - **Verify:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketPurchaseGovernanceApiTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `PUR-APP` / Task 1.4.
- [ ] **2.2 GREEN API authorization, HAL, OpenAPI, and generated contracts**
  - **Acceptance:** server policy is authoritative; HAL advertises only valid actions; capability outcomes are generic; OpenAPI/client regenerate atomically; payout stays absent.
  - **Verify:** the Task 2.1 command passes; `dotnet test --project tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketPurchaseGeneratedContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes.
  - **Dependencies:** 2.1.
- [ ] **2.3 RED S1-A/S1-B/S1-C BFF forwarding and antiforgery contract**
  - **Acceptance:** cover token isolation, same-origin proxying, antiforgery, tenant/header sanitization, generated-client use, and safe failures.
  - **Verify:** `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketPurchaseGovernanceBffTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 2.2.
- [ ] **2.4 GREEN purchase BFF boundary**
  - **Acceptance:** BFF keeps tokens server-side, forwards only trusted context, and exposes the generated contract safely.
  - **Verify:** the Task 2.3 command passes.
  - **Dependencies:** 2.3.
- [ ] **2.5 RED S1-A/S1-B/S1-C Blazor affordance/accessibility contract**
  - **Acceptance:** cover HAL-only actions, exact ceiling/access disclosure, rendered keyboard/focus/live status relationships, localization/RTL, and immutable generated state.
  - **Verify:** `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketPurchaseGovernanceComponentTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 2.4.
- [ ] **2.6 GREEN purchase UI and owned API/BFF/accessibility docs**
  - **Acceptance:** components consume HAL/generated clients only; rendered states and canonical docs pass; no local role inference.
  - **Verify:** the Task 2.5 command passes; `git diff --check -- docs/API_CONTRACT_INVENTORY.md docs/API.md docs/BLAZOR.md docs/TESTING.md docs/BLAZOR_DEV_WORKFLOW.md` exits 0.
  - **Dependencies:** 2.5.

### Phase 2 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — final merged Release build passed with 0 errors.
- [x] `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` — 495 passed.

## Phase 3: Participant Readiness Core — `RDY-CORE` → `RDY-APP` 🟡 IMPLEMENTATION PRESENT / TASK EVIDENCE OPEN

- [ ] **3.1 RED S2-A/S2-B/S2-C persistence and eligibility races**
  - **Acceptance:** cover order/participant scope, provisional purchaser data, adult-owned consent, approval/revocation, payment-before-readiness, issuance/check-in convergence, and tenant negatives.
  - **Verify:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ParticipantAdmissionEligibilityPersistenceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `PUR-APP` / Task 1.4.
- [ ] **3.2 GREEN readiness Domain model and tenant persistence**
  - **Acceptance:** typed answers remain canonical; purchaser consent is never copied; one decision surface gates active credential/check-in; migrations are generated.
  - **Verify:** the Task 3.1 command passes.
  - **Dependencies:** 3.1.
- [ ] **3.3 RED S2-A/S2-B/S2-C CQRS completion/revocation/issuance**
  - **Acceptance:** handler tests fail for absent subject-correct completion, approval/revocation, stable codes, cancellation, and zero-PII behavior.
  - **Verify:** `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ParticipantAdmissionEligibilityTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 3.2.
- [ ] **3.4 GREEN readiness orchestration, docs, mutation, and MAD**
  - **Acceptance:** CQRS and issuance/check-in use one readiness authority; docs/comments converge; mutation >85 and MAD close.
  - **Verify:** the Task 3.3 command passes; `dotnet stryker --project src/Explore.Application/Explore.Application.csproj --test-project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-3` exits 0; the Task 0.3 `TicketingCriticalEvidenceContractTests` command passes for Phase 3; `git diff --check -- docs/PRIVACY_ERASURE.md SECURITY.md docs/SECURITY-MODEL.md` exits 0.
  - **Dependencies:** 3.3.

### Phase 3 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — final merged Release build passed with 0 errors.
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` — 4,819 passed.

## Phase 4: Participant Readiness Surfaces — `RDY-API` → `RDY-UI` 🟡 IMPLEMENTATION PRESENT / TASK EVIDENCE OPEN

- [ ] **4.1 RED S2-A/S2-B/S2-C private API/HAL contract**
  - **Acceptance:** cover private/minimal reads, generic capability failures, subject/organizer HAL actions, scanner/support state, ProblemDetails, and sentinel PII absence.
  - **Verify:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ParticipantReadinessApiTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `RDY-APP` / Task 3.4.
- [ ] **4.2 GREEN readiness API/HAL/OpenAPI/generated contracts**
  - **Acceptance:** server authorization/readiness is authoritative; no roster leakage; schemas and HAL are deterministic.
  - **Verify:** the Task 4.1 command passes; `dotnet test --project tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ParticipantReadinessGeneratedContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes.
  - **Dependencies:** 4.1.
- [ ] **4.3 RED S2-A/S2-B/S2-C readiness BFF contract**
  - **Acceptance:** cover private proxying, antiforgery, token isolation, safe caching, and generated-client forwarding.
  - **Verify:** `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ParticipantReadinessBffTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 4.2.
- [ ] **4.4 GREEN readiness BFF boundary**
  - **Acceptance:** BFF exposes only safe private contracts and trusted server context.
  - **Verify:** the Task 4.3 command passes.
  - **Dependencies:** 4.3.
- [ ] **4.5 RED S2-A/S2-B/S2-C readiness component/accessibility contract**
  - **Acceptance:** cover pending/denied/support states, focus/error/live relationships, localization/RTL, and HAL-only actions.
  - **Verify:** `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ParticipantReadinessComponentTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 4.4.
- [ ] **4.6 GREEN readiness UI and privacy/accessibility docs**
  - **Acceptance:** UI uses HAL/generated state and canonical privacy/security/accessibility docs converge.
  - **Verify:** the Task 4.5 command passes; `git diff --check -- docs/PRIVACY_ERASURE.md SECURITY.md docs/SECURITY-MODEL.md docs/BLAZOR.md docs/TESTING.md` exits 0.
  - **Dependencies:** 4.5.

### Phase 4 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — final merged Release build passed with 0 errors.
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - Focused readiness/admission/purchase/transfer/fair-return API selectors passed; no retained successful full-project transcript is available.

## Phase 5: Transfer And Credential Rotation — `TRN-CORE` → `TRN-API` → `TRN-UI` 🟡 IMPLEMENTATION PRESENT / TASK EVIDENCE OPEN

- [ ] **5.1 RED S3-A/S3-B/S3-C transfer PostgreSQL races**
  - **Acceptance:** cover policy/expiry/hops, subject data, no resale/commerce mutation, transfer/check-in, consent/approval/reissue races, stale generation, replay, tenant/resource negatives.
  - **Verify:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketTransferConcurrencyTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `RDY-UI` / Task 4.6.
- [ ] **5.2 GREEN transfer state machine, shared fence, capability, and persistence**
  - **Acceptance:** one holder/credential; canonical lock order; credential generation rotates; commerce/check-in history remains immutable; notifications commit atomically.
  - **Verify:** the Task 5.1 command passes on real PostgreSQL.
  - **Dependencies:** 5.1.
- [ ] **5.3 RED S3-A/S3-B/S3-C transfer API/HAL contract**
  - **Acceptance:** cover offer/accept/cancel/correction/reissue auth, generic capabilities, HAL, ProblemDetails, OpenAPI, and zero-sentinel output.
  - **Verify:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketTransferApiTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 5.2.
- [ ] **5.4 GREEN transfer API/HAL/OpenAPI/generated contracts**
  - **Acceptance:** server state is authoritative; capability outcomes are indistinguishable; generated contracts converge.
  - **Verify:** the Task 5.3 command passes; `dotnet test --project tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketTransferGeneratedContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes.
  - **Dependencies:** 5.3.
- [ ] **5.5 RED S3-A/S3-B/S3-C transfer BFF/component contract**
  - **Acceptance:** cover token isolation, antiforgery, HAL-only actions, transfer/support rendered states, focus/live status, localization/RTL.
  - **Verify:** `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketTransferBffTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` and `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketTransferComponentTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fail by assertion.
  - **Dependencies:** 5.4.
- [ ] **5.6 GREEN transfer BFF/UI, docs, mutation, and MAD**
  - **Acceptance:** BFF/UI consume generated contracts/HAL only; docs/comments converge; zero-PII, mutation >85, and MAD close.
  - **Verify:** both Task 5.5 commands pass; `dotnet stryker --project src/Explore.Domain/Explore.Domain.csproj --test-project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-5` exits 0; the Task 0.3 `TicketingCriticalEvidenceContractTests` command passes for Phase 5; `git diff --check -- SECURITY.md docs/SECURITY-MODEL.md docs/PRIVACY_ERASURE.md` exits 0.
  - **Dependencies:** 5.5.

### Phase 5 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — final merged Release build passed with 0 errors.
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - Focused ticketing persistence selectors passed 24 tests; no retained successful full-project transcript is available.

## Phase 6: Fair Return And Waitlist — `WAI-CORE` → `WAI-ORCH` → `WAI-API` → `WAI-UI` 🟡 IMPLEMENTATION PRESENT / TASK EVIDENCE OPEN

- [ ] **6.1 RED S4-A/S4-B/S4-C/S4-D/S4-E/S4-F/WB-1 persistence races**
  - **Acceptance:** cover literal queue order, commercial equivalence, allocate/withdraw/substitute/expire/finalize interleavings, lock order/deadlock, crash, duplicate/stale provider observations, and loser rollback.
  - **Verify:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*FairReturnWaitlistConcurrencyTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `PUR-APP` / Task 1.4 and `TRN-UI` / Task 5.6.
- [ ] **6.2 GREEN waitlist/supply policy, queue/offer persistence, and locks**
  - **Acceptance:** one-winner capacity, full commercial equivalence, immutable buyer snapshots, deterministic order, tenant uniqueness, and generated migrations pass.
  - **Verify:** the Task 6.1 command passes on real PostgreSQL.
  - **Dependencies:** 6.1.
- [ ] **6.3 RED S4-E/S4-F/WB-1 durable orchestration contract**
  - **Acceptance:** fail for missing payment/refund atomic intent, Unknown/poison/dead-letter matrix, stable idempotency, Quartz pointer-only behavior, restart, deterministic 10,000-effect drain without starvation, and zero-sentinel telemetry.
  - **Verify:** `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*FairReturnWaitlistOrchestrationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 6.2.
- [ ] **6.4 GREEN outbox/payment/refund reconciliation and Quartz orchestration**
  - **Acceptance:** payment settlement/refund intent commits locally before provider I/O; replay converges; Quartz only wakes durable services; a deterministic 10,000-effect drain completes without starvation; health is fixed-cardinality.
  - **Verify:** the Task 6.3 command passes.
  - **Dependencies:** 6.3.
- [ ] **6.5 RED S4-A/S4-B/S4-C/S4-D/S4-E/S4-F/WB-1 API/HAL contract**
  - **Acceptance:** cover bounded position/reason, no paid priority, generic identity, seller conflict, stop controls, no-store, HAL, OpenAPI, and zero-sentinel output.
  - **Verify:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*FairReturnWaitlistApiTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 6.4.
- [ ] **6.6 GREEN waitlist API/HAL/OpenAPI/generated contracts**
  - **Acceptance:** server policy is authoritative; private outcomes and generated shapes converge.
  - **Verify:** the Task 6.5 command passes; `dotnet test --project tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*FairReturnWaitlistGeneratedContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes.
  - **Dependencies:** 6.5.
- [ ] **6.7 RED S4-A/S4-B/S4-C/S4-D/S4-E/S4-F/WB-1 BFF/component contract**
  - **Acceptance:** cover token isolation, antiforgery, HAL-only actions, waitlist/conflict states, focus/live status, localization/RTL.
  - **Verify:** `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*FairReturnWaitlistBffTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` and `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*FairReturnWaitlistComponentTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fail by assertion.
  - **Dependencies:** 6.6.
- [ ] **6.8 GREEN waitlist BFF/UI, docs, scale, mutation, and MAD**
  - **Acceptance:** BFF/UI use generated contracts/HAL; deterministic fixture meets 50-way/10,000-effect targets; docs/comments, mutation >85, and MAD close.
  - **Verify:** both Task 6.7 commands pass; the Task 6.1 command passes for the deterministic 50-contender race; the Task 6.3 command passes for the deterministic 10,000-effect/no-starvation drain; `dotnet stryker --project src/Explore.Domain/Explore.Domain.csproj --test-project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-6/domain` exits 0; `dotnet stryker --project src/Explore.Application/Explore.Application.csproj --test-project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-6/application` exits 0; `dotnet stryker --project src/Explore.Infrastructure/Explore.Infrastructure.csproj --test-project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-6/infrastructure` exits 0; the Task 0.3 `TicketingCriticalEvidenceContractTests` command passes for Phase 6; `git diff --check -- docs/PAYMENTS.md docs/OPERATIONS.md` exits 0.
  - **Current gap:** runtime/BFF/UI behavior and focused tests are merged, and Phase 6 MAD/PII artifacts exist, but the required Phase 6 evidence manifest and mutation summaries are not retained. Keep this task open until that evidence contract passes.
  - **Dependencies:** 6.7.

### Phase 6 Verification

- [x] `dotnet build --configuration Release --verbosity quiet` — final merged Release build passed with 0 errors.
- [x] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` — 1,653 passed.

## Phase 7: Event-Bound Add-Ons — `ADD-CORE` → `ADD-API` → `ADD-UI` ⏳ NOT STARTED

- [ ] **7.1 RED S5-A/S5-B/S5-C add-on persistence and money contract**
  - **Acceptance:** cover optionality, literal totals, `long` overflow before effects, conservation, inventory race, partial refund, fulfillment replay, tenant isolation, and no admission mutation.
  - **Verify:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*EventAddOnPersistenceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `PUR-APP` / Task 1.4.
- [ ] **7.2 GREEN add-on catalog/line/inventory/fulfillment and checked money**
  - **Acceptance:** immutable snapshots, `MinorUnitMath`, separate inventory/fulfillment/refund, stable replay, generated migrations, and admission-separation ratchet pass.
  - **Verify:** the Task 7.1 command passes.
  - **Dependencies:** 7.1.
- [ ] **7.3 RED S5-A/S5-B/S5-C add-on API/HAL contract**
  - **Acceptance:** cover exact optional disclosure, totals, fulfillment/refund state, auth, HAL, OpenAPI, and zero-sentinel output.
  - **Verify:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*EventAddOnApiTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 7.2.
- [ ] **7.4 GREEN add-on API/HAL/OpenAPI/generated contracts**
  - **Acceptance:** server authority, optional disclosure, and generated schemas converge.
  - **Verify:** the Task 7.3 command passes; `dotnet test --project tests/Explore.GeneratedContracts.Tests/Explore.GeneratedContracts.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*EventAddOnGeneratedContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes.
  - **Dependencies:** 7.3.
- [ ] **7.5 RED S5-A/S5-B/S5-C add-on BFF/component contract**
  - **Acceptance:** cover token isolation, antiforgery, HAL-only actions, no dark-pattern required add-ons, focus/live status, localization/RTL.
  - **Verify:** `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*EventAddOnBffTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` and `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*EventAddOnComponentTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fail by assertion.
  - **Dependencies:** 7.4.
- [ ] **7.6 GREEN add-on BFF/UI, docs, mutation, and MAD**
  - **Acceptance:** BFF/UI use generated contracts/HAL; docs/comments converge; mutation >85 and MAD close.
  - **Verify:** both Task 7.5 commands pass; `dotnet stryker --project src/Explore.Domain/Explore.Domain.csproj --test-project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-7` exits 0; the Task 0.3 `TicketingCriticalEvidenceContractTests` command passes for Phase 7; `git diff --check -- docs/PAYMENTS.md docs/OPERATIONS.md` exits 0.
  - **Dependencies:** 7.5.

### Phase 7 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 8: Recovery And Operator Controls — `REC-CORE` → `REC-OPS` ⏳ NOT STARTED

- [ ] **8.1 RED S6-A/S6-B/S6-C/WB-1 persistence recovery contract**
  - **Acceptance:** cover mixed/stale manifest, missing key/cursor/fence/idempotency, pre-revocation backup, stale worker, interrupted queues, provider ambiguity, cross-tenant restore, and duplicate-effect prevention.
  - **Verify:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketingLifecycleRecoveryInvariantTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** `PUR-UI` / Task 2.6, `RDY-UI` / Task 4.6, `TRN-UI` / Task 5.6, `WAI-UI` / Task 6.8, and `ADD-UI` / Task 7.6.
- [ ] **8.2 GREEN recovery state, manifest, fencing, and bearer rotation**
  - **Acceptance:** recovery-only state, manifest validation, capability cancellation, credential generation rotation/reissue, stale-fence rejection, and fail-closed authority pass.
  - **Verify:** the Task 8.1 command passes.
  - **Dependencies:** 8.1.
- [ ] **8.3 RED S6-A/S6-B/S6-C operator health/configuration contract**
  - **Acceptance:** fail for missing stop/pause/reconcile/reopen controls, Unknown/poison/dead-letter actions, fixed-cardinality health thresholds, typed options, key-reference validation, and runbook/schema ownership.
  - **Verify:** `dotnet test --project tests/Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketingRecoveryOperatorContractTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` fails by assertion.
  - **Dependencies:** 8.2.
- [ ] **8.4 GREEN operator controls, health, config, runbooks, mutation, and MAD**
  - **Acceptance:** authenticated HAL controls and recovery state/action matrix converge; SQLite/server-replica rules, RPO/RTO declarations, zero-PII, docs/comments, mutation >85, and MAD close.
  - **Verify:** the Task 8.3 command passes; `dotnet stryker --project src/Explore.Domain/Explore.Domain.csproj --test-project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-8/domain` exits 0; `dotnet stryker --project src/Explore.Infrastructure/Explore.Infrastructure.csproj --test-project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --break-at 86 --reporter json --output dev/active/event-ticketing-lifecycle/evidence/phase-8/infrastructure` exits 0; the Task 0.3 `TicketingCriticalEvidenceContractTests` command passes for Phase 8; `git diff --check -- .env.example docs/OPERATIONS.md SECURITY.md docs/SECURITY-MODEL.md` exits 0.
  - **Dependencies:** 8.3.

### Phase 8 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 9: Deployment Boundary, Contracts, And Release — `REL` ⏳ NOT STARTED

- [ ] **9.1 BASELINE S7-A payout absence and RED capability-manifest gap**
  - **Acceptance:** existing payout-absence assertions pass; then the new test fails only for the missing machine-readable capability/status matrix.
  - **Verify:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketingDeploymentCapabilityMatrixTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` records the passing baseline then fails by the intended assertion.
  - **Dependencies:** `REC-OPS` / Task 8.4.
- [ ] **9.2 GREEN S7-A capability matrix and payout-absence ratchet**
  - **Acceptance:** every capability is `production-approved`, `test-only`, or `disabled`; protected payout has no route/HAL/job/config/secret/client/UI surface; tests validate machine values, not prose.
  - **Verify:** the Task 9.1 command passes.
  - **Dependencies:** 9.1.
- [ ] **9.3 GREEN exact-revision contract convergence**
  - **Acceptance:** generated contracts/docs/intent/I-VSD/task mappings converge; I-VSD is current/plan-aligned; fresh CTO has no blocker; user approves exact hashes.
  - **Verify:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*TicketingLifecycleContractConvergenceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes.
  - **Dependencies:** 9.2 and independent review gates.
- [ ] **9.4 Changelog contribution and final commit composition**
  - **Acceptance:** create valid `docs/releases/changes/CHG-YYYY-NNNN.yaml`; `ReleaseInputPolicy` passes; intentional API breakage updates `docs/API_CHANGELOG.md`; terminal authorized commit carries `Change-Id: CHG-YYYY-NNNN` and `BREAKING CHANGE:`; plumbing commits use `Changelog: skip` plus `Changelog-Reason:`.
  - **Verify:** `dotnet test --project eng/release/tests/ISLAMU.ReleaseEngineering.Tests/ISLAMU.ReleaseEngineering.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ReleaseInputPolicyTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1` passes.
  - **Dependencies:** 9.3; do not commit unless explicitly requested.

### Phase 9 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## External Launch Gates

- Provider/account/country/currency/controller evidence.
- Belgian/EU legal and tax/accounting determination.
- Qualified Islamic scholarly review.
- Accessibility, privacy, security, stakeholder, and staffed operator review.
- Production-like timed restore, multi-replica takeover, and declared RPO/RTO evidence performed under the operator release process, not this implementation-plan verification.
- Separate I-VSD/ADR/workstream before any protected delayed payout.

Fixture-green code cannot close these gates or convert `test-only`/`disabled` to `production-approved`.

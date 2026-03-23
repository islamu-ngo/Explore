# Tasks: UnitOfWork Pattern

Last Updated: 2026-03-22

## Phase 0 — Baseline (DONE)

- [x] Redesign `IUnitOfWork` to `ExecuteInTransactionAsync(Func<CancellationToken, Task>, CancellationToken)`
- [x] Rewrite `EfCoreUnitOfWork` to use `CreateExecutionStrategy().ExecuteAsync()` wrapper
- [x] Add InMemory provider bypass (integration test compatibility)
- [x] Register `IUnitOfWork` as scoped in `PersistenceServicesRegistration`
- [x] Migrate `CompleteInstanceOnboardingCommandHandler` to new UoW pattern
- [x] Verified end-to-end: build passes, onboarding completes successfully

---

## Phase 1 — Harden `EfCoreUnitOfWork` (DONE)

- [x] **1.1** Add generic `Task<T>` overload to `IUnitOfWork` interface
  - File: `Explore.Application/Contracts/Persistence/IUnitOfWork.cs`
  - Add: `Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)`
  - Effort: S

- [x] **1.2** Implement generic overload in `EfCoreUnitOfWork`
  - File: `Explore.Persistence/EfCoreUnitOfWork.cs`
  - Refactor: void overload delegates to generic (`return null` wrapper)
  - Single transaction execution path — no code duplication
  - Effort: S

- [x] **1.3** Add nested transaction guard to `EfCoreUnitOfWork`
  - Detect: `if (_dbContext.Database.CurrentTransaction != null) throw new InvalidOperationException(...)`
  - Message must name the problem clearly: "Nested transactions are not supported"
  - Skip guard for InMemory (InMemory has no CurrentTransaction)
  - Effort: S

- [x] **1.4** Unit tests updated — all 501 pass
  - Effort: S

---

## Phase 2 — Handler Audit (DONE)

All handlers audited. All needed UoW (multi-step write workflows):

- [x] **2.1** `CompleteTenantOnboardingCommandHandler` — Y: ApplyTenantSettings (many writes) + OnboardingState create/update
- [x] **2.2** `CreateEventCommandHandler` — Y: Event + StorageObject + EventSession
- [x] **2.3** `CreateEventWithSessionsCommandHandler` — Y: Event + StorageObject + Sessions + IslamicAspects + Languages
- [x] **2.4** `UpdateInstanceGovernanceSettingsCommandHandler` — Y: EnsureDefaultTenant + ApplySettings (many writes) + BootstrapState
- [x] **2.5** `UpdateTenantPolicySettingsCommandHandler` — Y: ApplyTenantSettings (25+ writes)
- [x] **2.6** `UpdateInstanceSubResourceHandlers` (7 handlers) — Y: each calls service with multiple setting writes

---

## Phase 3 — Migrate Flagged Handlers (DONE)

All handlers migrated:

- [x] **3.1** `CreateEventCommandHandler` — IUnitOfWork injected, writes wrapped, metrics/cache post-commit
- [x] **3.2** `CreateEventWithSessionsCommandHandler` — IUnitOfWork injected, all writes wrapped
- [x] **3.3** `CompleteTenantOnboardingCommandHandler` — IUnitOfWork injected, writes wrapped
- [x] **3.4** `UpdateInstanceGovernanceSettingsCommandHandler` — IUnitOfWork injected, writes wrapped
- [x] **3.5** `UpdateTenantPolicySettingsCommandHandler` — IUnitOfWork injected, writes wrapped
- [x] **3.6** `UpdateInstanceSubResourceHandlers` (7 handlers) — IUnitOfWork injected into each
- [x] **3.7** `SyncUserCommandHandler` — IUnitOfWork injected; user/actor/externalLogin writes atomic; login conflict detection throws inside lambda; cache post-commit; `CustomPropertyDefinitionRepository` internal transactions removed
- [x] **3.8** `DeleteUserCommandHandler` — IUnitOfWork injected; all PII/token/actor deletes atomic; cache post-commit
- [x] **3.9** `CreateCustomPropertyDefinitionCommandHandler` — IUnitOfWork injected, `CreateWithOptions` wrapped
- [x] **3.10** `UpdateCustomPropertyDefinitionCommandHandler` — IUnitOfWork injected, `UpdateWithOptions` wrapped
- [x] **3.11** `CustomPropertyDefinitionRepository` refactored — internal `executionStrategy`/transaction removed from `CreateWithOptions`, `UpdateWithOptions`, `DeleteDefinition`
- [x] Unit tests updated for all handlers — 501 tests green

---

## Phase 4 — Transactional Correctness Tests (DONE)

- [x] **4.1** `Testcontainers.PostgreSql` already in `Directory.Packages.props` (v4.10.0)
- [x] **4.2** Test: mid-workflow failure triggers full rollback — `Event.Persistence.IntegrationTests/UnitOfWork/EfCoreUnitOfWorkTests.cs`
- [x] **4.3** Test: successful commit — all writes persisted
- [x] **4.4** Test: generic overload returns correct value
- [x] **4.5** Test: nested transaction throws `InvalidOperationException` with "Nested transactions are not supported"

---

## Phase 5 — Architecture Enforcement

- [x] **5.1** Add code review checklist item to `docs/GOVERNANCE.md`:
  > Before approving any command handler PR: check if the handler performs multi-step writes. If yes, verify `IUnitOfWork` is used.
  - Effort: S

- [ ] **5.2** (Future) Evaluate `ITransactionalCommand<TResult>` marker + MediatR pipeline behavior
  - Design in `dev/active/` when 10+ handlers consistently use the same pattern
  - Do NOT implement now — per-handler explicit pattern has better debuggability at current scale
  - Effort: XL (when triggered)

---

## Phase 6 — Outbox Pattern (Future / Cross-Process Side Effects)

> Trigger: when any handler needs reliable email, webhook, or integration event delivery.

- [ ] **6.1** Create `OutboxMessage` domain entity with EF configuration
- [ ] **6.2** Write outbox entries inside transaction lambda (same DB transaction as business writes)
- [ ] **6.3** Create `OutboxProcessor` hosted background service
- [ ] **6.4** Mark messages as processed after successful delivery
- [ ] **6.5** Ensure outbox consumer is idempotent (duplicate delivery on retry must be safe)
  - Effort: L (full phase)

---

## Retry-Safety Checklist (Use Before Each Migration)

Before wrapping any handler in `ExecuteInTransactionAsync`, confirm:

- [ ] All `Guid.NewGuid()` / random values generated **before** the lambda
- [ ] No `DateTime.UtcNow` used as a uniqueness key inside the lambda
- [ ] No external HTTP calls inside the lambda
- [ ] No message broker publishes inside the lambda
- [ ] No email / SMS / push notifications inside the lambda
- [ ] All unique entities protected by a DB `UNIQUE` constraint (safe on retry)
- [ ] All foreign-key writes done in correct order (parent before child)

---

## Done-Definition Per Handler Migration

A handler migration is done when:
- [ ] `IUnitOfWork` injected and used
- [ ] All pre-validation is outside the lambda
- [ ] All DB writes are inside the lambda
- [ ] All post-commit side effects are after `ExecuteInTransactionAsync` returns
- [ ] Retry-safety checklist passed
- [ ] Unit tests updated and green
- [ ] Integration tests green

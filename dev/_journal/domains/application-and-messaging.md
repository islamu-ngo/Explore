<!-- ABOUTME: Domain journal for Application layer, MediatR/CQRS, Outbox messaging, and settings cascade. -->
<!-- ABOUTME: Captures durable findings on command handlers, event dispatching, and asynchronous workflows. -->

# Application & Messaging Knowledge Ledger

> **Scope**: `Explore.Application`, CQRS/MediatR pipeline, Transactional Outbox, RabbitMQ/MQContract, and EAV.

---

## 1. Architectural Decisions

- **Transactional Outbox Mandate**: Any domain event, webhook, or external notification resulting from an aggregate state change must be written to the local outbox in the same database transaction.
- **Provider Abstraction for Messaging**: `IMessagingProvider` defines the application contract, wrapped by `RuntimeMessagingProvider` (scoped with cache) and `RabbitMqMessagingProvider` (singleton with lazy init).
- **5-Tier Governance Settings Cascade**: Settings resolve through User $\rightarrow$ Group $\rightarrow$ Organization $\rightarrow$ Tenant $\rightarrow$ Instance. Instance-level locks prevent higher-tier overrides unless running in single-tenant mode.

---

## 2. Technical Insights & Patterns

- **MQContract OpenTelemetry Source Alignment**: `RabbitMqMessagingProvider` calls `contractConnection.EnableOpenTelemetry(activitySource: "MQContract")`, which requires `Explore.ServiceDefaults/Extensions.cs` to declare `.AddSource("MQContract")`. Mismatched source names cause missing traces.
- **Lazy Singleton Connection Pattern**: Connection instances (`ContractConnection`) are expensive and managed as singletons behind `SemaphoreSlim(1,1)` thread synchronization, properly disposed on application shutdown.
- **Quartz.NET Options Collision**: Quartz ships its own `Quartz.QuartzSchedulerOptions`. The application configuration class must be named `QuartzSchedulerSettings` to avoid namespace collisions.
- **Quartz `JobDataMap` Key Safety**: `JobDataMap.GetString(key)` throws `KeyNotFoundException` on missing keys. Always probe using `TryGetValue` and treat absent payloads as logged no-ops to avoid infinite scheduler retry loops.
- **Quartz 6-Field Cron Expressions**: Quartz rejects cron expressions that set both day-of-month and day-of-week to `*`. One must be `?` (e.g. `*/10 * * * * ?`).

---

## 3. Failed Approaches & Lessons

- **Scope Creep in Delegated Handler Cleanup**: Never rename or mutate repository interfaces (e.g. `IGenericRepository`) or CQRS request signatures during handler cleanup tasks. Always enforce strict boundaries on contract modifications.
- **Generic BackgroundService Timer Loops**: Hand-rolled timer loops in `BackgroundService` are banned for periodic sweeps. All periodic work belongs in Quartz.NET registered via `AddSweepJob<TJob>`.

---

[2026-08-26 Europe/Brussels] — Validate detached policy candidates before tracked revisions

**Context**: While implementing the Tier-0 `ConfigurationManifest` paid-policy
authority, adversarial review exercised tenant broadening against an existing
active tenant policy.

**Symptom / Observation**: A rejected broadening returned a validation failure,
but `PaidEventPolicyVersion.CreateRevision` had already retired the
repository-tracked active policy in
`src/Explore.Application/Features/PaidEventPolicies/PaidEventPolicyMutationBoundary.cs:155`.
A later save on the same scoped context could persist that retirement without a
replacement.

**Root Cause**: Domain revision creation is an intentional state transition on
the current aggregate; using the resulting revision as the validation candidate
therefore mutates tracked authority before all pure narrowing rules pass.

**Resolution**: Build and validate a detached tenant candidate first, then call
`CreateRevision` only after `PaidEventPolicyRules.ValidateTenantPolicy`
succeeds. The regression
`PaidEventPolicyMutationBoundaryTests.ReviseTenantInCurrentTransaction_BroadeningKeepsTrackedPolicyActive`
now proves failure leaves the current policy active and performs no repository
write. Verification:
`dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*PaidEventPolicyMutationBoundaryTests/*" --no-progress --maximum-parallel-tests 1`
(6/6 passed).

**Why This Matters for Future Work**: Never use a state-transition method on a
tracked aggregate to construct a candidate for validation. Validate a detached
candidate first, then mutate only on the accepted path.

**References**:
- `src/Explore.Application/Features/PaidEventPolicies/PaidEventPolicyMutationBoundary.cs:155`
- `tests/Event.Application.UnitTests/Features/PaidEventPolicies/PaidEventPolicyMutationBoundaryTests.cs:169`
- `.agents/skills/criticality-guardrail/SKILL.md`

**Promotion Consideration**:
- [ ] Candidate for `docs/QUICK_REFERENCE.md` (new non-inferable rule)
- [ ] Candidate for new `.claude/rules/*.md` entry
- [ ] Candidate for skill update: `<skill name>`
- [ ] Candidate for ADR / `MAJOR_DECISIONS.md`
- [x] Stays in journal only (one-off debugging lesson)

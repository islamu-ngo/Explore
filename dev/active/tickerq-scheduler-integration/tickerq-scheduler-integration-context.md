<!-- ABOUTME: Operational context for the TickerQ scheduler integration workstream. -->
<!-- ABOUTME: Preserves current state, decisions, evidence, blockers, and resume notes for future implementation agents. -->

# TickerQ Scheduler Integration — Context

Last Updated: 2026-05-28 Europe/Brussels

## SESSION PROGRESS (2026-05-28 Europe/Brussels)

### COMPLETED

- Created initial dev-docs planning set for TickerQ scheduler adoption.
- Read `AGENTS.md`, `.claude/commands/dev-docs.md`, `dev/active/README.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/OUTBOX_PATTERN.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, and `docs/SELF_HOSTING.md` sections relevant to background work, outbox, health, and configuration.
- Loaded relevant skills/rules: `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `outbox-pattern`, `error-tracking`, `aspire`, and the Application/Persistence/Migration/API/HAL/Test rule files.
- Used Context7 for TickerQ documentation with library ID `/arcenox-co/tickerq`.
- Verified no existing TickerQ integration in the repo by search.
- Verified current EmailDispatch architecture:
  - registration creates `EmailDispatchOutbox` durable intent;
  - `EmailDispatchProcessor` currently polls PostgreSQL and sends SMTP;
  - RabbitMQ pointer publisher/topology/health exists, but manual-ack consumer remains pending;
  - admin/HAL status uses `EmailDispatchOutbox`, not scheduler state.

### IN PROGRESS

- Awaiting user review/correction/approval of this implementation plan.

### NEXT

1. User reviews `tickerq-scheduler-integration-plan.md`, especially sections 3-6 and 13.
2. First implementation slice starts with Phase 0 package/API spike and then Phase 2 drain-service extraction.
3. Implementation agent updates all three dev docs after the first slice.

### BLOCKERS

- None for planning.
- Implementation must account for unrelated dirty worktree state and known unrelated validation blockers recorded in `dev/active/enterprise-data-model-hardening/enterprise-data-model-hardening-context.md`.
- Exact TickerQ package/API shape must be compile-verified before broad edits.

## Quick Resume

1. Read `tickerq-scheduler-integration-plan.md`.
2. Read `tickerq-scheduler-integration-tasks.md`.
3. Start from Phase 0 unless the user has approved a later slice.
4. Keep all three dev docs updated after each meaningful implementation change.
5. Do not change or revert unrelated dirty files.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Directory.Packages.props` | Existing | DevOps | Central package versions | Add TickerQ package versions here, not inline. |
| `Explore.API/Program.cs` | Existing | API | Composition root and hosted worker registration | Current `EmailDispatchProcessor` registration lives here. |
| `Explore.API/BackgroundServices/EmailDispatchProcessor.cs` | Existing | API | Current Basic Dispatch Mode polling worker | Extract orchestration into drain service before TickerQ job. |
| `Explore.Infrastructure/EmailDispatchProcessorSettings.cs` | Existing | Infrastructure | Basic dispatch worker options | Needs explicit mode setting if TickerQ becomes default. |
| `Explore.Application/Contracts/Persistence/IEmailDispatchOutboxRepository.cs` | Existing | Application | EmailDispatch repository contract | Entity-returning and atomic state methods remain authoritative. |
| `Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs` | Existing | Persistence | EF state transitions for EmailDispatch | Must remain final business-state writer. |
| `Explore.Application/Contracts/Infrastructure/IEmailDispatchTransport.cs` | Existing | Application | Optional broker transport port | TickerQ does not replace this. |
| `Explore.Application/Contracts/Infrastructure/EmailDispatchPointer.cs` | Existing | Application | Pointer-only RabbitMQ payload | Pattern to copy for scheduler payloads. |
| `Explore.Infrastructure/Messaging/RabbitMqEmailDispatchTransport.cs` | Existing | Infrastructure | Optional RabbitMQ publisher/topology/health | Consumer still pending; keep separate from TickerQ. |
| `Explore.API/Hateoas/Policies/EmailDispatchStatusLinkPolicy.cs` | Existing | API/HAL | Server-owned replay/park affordances | Product UI uses HAL links, not TickerQ dashboard. |
| `Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` | Existing | Tests | Prevents direct side effects | Extend for scheduler boundary. |
| `docs/adr/ADR-008-email-dispatch-state-machine.md` | Existing | Docs | Authoritative EmailDispatch decision | TickerQ plan must not contradict it. |
| `dev/active/crmworx-event-api-adaptation/` | Existing | Dev docs | Durable side-effect workstream | Update if EmailDispatch runtime behavior changes. |
| `dev/active/tickerq-scheduler-integration/*` | New | Dev docs | This planning workstream | Must be kept current during implementation. |

## Key Decisions

1. TickerQ is approved only as scheduler and operations layer.
2. PostgreSQL `EmailDispatchOutbox` remains business truth.
3. First safe slice is `email-dispatch-drain`, replacing timer mechanics but not domain state.
4. Expected SMTP/provider outcomes persist in EmailDispatch state and should not rely on TickerQ retries.
5. TickerQ payloads must be pointer-only.
6. Dashboard is operator-only, disabled/protected by default, and never tenant UI.
7. RabbitMQ manual-ack consumer semantics stay broker-native.

## Constraints And Rules To Remember

- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Domain must not reference TickerQ, EF, SMTP, RabbitMQ, ASP.NET Core, or MediatR.
- Application may define scheduler contracts but must not reference TickerQ concrete APIs unless explicitly approved by architecture decision.
- API/Infrastructure compose TickerQ.
- HAL remains the sole source of UI action affordances.
- Every new source/doc file starts with two `ABOUTME:` lines.
- No backward compatibility shims are required, but rollback config may be used for safe rollout.

## Validation Baseline

Minimum expected commands as implementation progresses:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

Add focused `Event.API.IntegrationTests` for dashboard/health behavior and optional Aspire/Mailpit E2E for final EmailDispatch proof.

## Current Known Risks / Unknowns

- Exact TickerQ package/API shape needs compile proof.
- Separate TickerQ DbContext/schema is preferred but not yet proven in this codebase.
- Dashboard authentication policy name/path needs final selection.
- Current worktree is dirty with unrelated changes; implementation agents must scope their work tightly.
- Full validation may be blocked by unrelated active workstream failures; record exact failures if encountered.

## Handoff Notes

### Handoff — 2026-05-28 Europe/Brussels

- **Current state:** Planning docs created. No implementation work has started.
- **Next action:** User review; then Phase 0 package/API spike.
- **Blockers:** None for planning. Implementation must handle unrelated dirty worktree and compile-verify TickerQ APIs.
- **Modified files:** `dev/active/tickerq-scheduler-integration/tickerq-scheduler-integration-plan.md`, `tickerq-scheduler-integration-context.md`, `tickerq-scheduler-integration-tasks.md`.
- **Validation:** Markdown docs reviewed by read/search only; no build/tests run because this is planning-only.
- **Documentation impact:** Future implementation will need updates to configuration, operations, self-hosting, architecture, outbox docs, and possibly CRMWorx workstream docs.
- **Risks:** Scheduler/workflow boundary and dashboard exposure are the main risks.
- **Notes for next contributor/agent:** Do not start by adding TickerQ everywhere. Extract EmailDispatch drain logic first or in parallel with a small package/API spike.

# Flexible and Contextual Event End Times — Context

Last Updated: 2026-07-01 Europe/Brussels

## SESSION PROGRESS (2026-07-01 Europe/Brussels)

### ✅ COMPLETED
- Strategic implementation plan updated and improved to address projection calculator and lifecycle policy gaps.
- I-VSD consultancy report persisted to `islamic-value-sensitive-design/i-vsd-flexible-event-end-times.md`.
- Plan reviewed and aligned with user feedback (skipped `TillEndOfDay`, suggested estimated duration as Layer 3 custom property).

### 🟡 IN PROGRESS
- Awaiting user approval of the revised strategic implementation planning files.

### ⏭️ NEXT
1. User reviews the planning docs and approves them.
2. The first implementation agent begins Phase 1 (Domain & Persistence Changes).

### ⚠️ BLOCKERS
- None.

---

## Quick Resume
1. Read `flexible-event-end-times-plan.md`.
2. Read `flexible-event-end-times-tasks.md`.
3. Start from Phase 1, Task 1.1 (`Explore.Domain/Enums/SessionEndTimeType.cs` creation).
4. Update the tasks checklist (`flexible-event-end-times-tasks.md`) and this context file after each implementation slice.

---

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| [EventSession.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/EventSession.cs) | Existing | Domain | Holds event session properties, start/end times, and projections. | Add `EndTimeType` property. Refactor `ReprojectLocalTimes` to allow null `EndTime`. Update `ContributesToPublicScheduleSummary`. |
| [EventSessionIslamicAspect.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/EventSessionIslamicAspect.cs) | Existing | Domain | Extensions for Islamic event session properties (segregation, prayer scheduling). | Add `EndReferencePrayer` and `EndOffsetMinutes`. |
| [SessionEndTimeType.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Enums/SessionEndTimeType.cs) | New | Domain | Enum for `Fixed`, `OpenEnded`, and `RelativeToPrayer`. | |
| [LocalScheduleProjection.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Services/Scheduling/LocalScheduleProjection.cs) | Existing | Domain | Projection record struct. | Make end fields nullable. |
| [IEventScheduleProjectionCalculator.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Domain/Services/Scheduling/IEventScheduleProjectionCalculator.cs) | Existing | Domain | Projection calculator interface. | Make `endUtc` and end-field return values nullable. |
| [CreateEventRequest.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/DTOs/Event/CreateEventRequest.cs) | Existing | Application | DTO contracts for MediatR event creation command. | Update `EndTime` to nullable and add `EndTimeType`/relative fields. |
| [CreateEventRequestValidator.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs) | Existing | Application | Request validators. | Apply conditional validation to relax rules for imported events. Validate location requirement for relative scheduling. |
| [EventLifecycleReadinessEvaluator.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs) | Existing | Application | Lifecycle readiness evaluator. | Update `ScheduleEnd` validation to bypass missing-field checks for `OpenEnded` sessions. |
| [EventSessionConfiguration.cs](file:///home/amir/ISLAMU/Github/Event/Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs) | Existing | Persistence | Entity configuration for `EventSession`. | Map the new properties and add PostgreSQL check constraints. |

---

## Key Decisions
- **Skip `TillEndOfDay`:** Excluded because users can select `23:59` to represent end of the day.
- **Estimated Duration as EAV Suggestion:** Suggested in docs rather than implementing as a core field to demonstrate clean schema boundaries.
- **Bypass Validation for AI Import:** Allow missing timezone and end times when `ProvenanceSource` is set.
- **Update Projection Calculator:** Change calculator contract to support nullable end times, preserving it as the single source of truth for timezone projection calculations.
- **Update Lifecycle Evaluator:** Relax readiness check for `OpenEnded` session end times to allow successful publication.

---

## Constraints And Rules To Remember
- **Manual Validators:** Never use dependency-injected validators in command handlers.
- **Pure Domain:** No external references inside `Explore.Domain`.
- **ABOUTME comments:** Keep the two-line header in all new and edited files.

---

## Validation Baseline
- `dotnet build --configuration Release --verbosity quiet` must pass.
- Test projects:
  - `Event.Domain.UnitTests`
  - `Event.Application.UnitTests`
  - `Event.Persistence.IntegrationTests`
  - `Event.Architecture.Tests`

---

## Current Known Risks / Unknowns
- Correctly resolving `RelativeToPrayer` end times to real UTC timestamps. Requires location data and timezone mapping.

---

## Handoff Notes

### Handoff — 2026-07-01 Europe/Brussels
- **Current state:** Strategic implementation planning docs updated to include critical calculator and lifecycle evaluation adjustments.
- **Next action:** Phase 1 execution (Domain & Persistence changes).
- **Blockers:** None.
- **Modified files:** None (planning phase only).
- **Validation:** Planning docs verified against `.claude/commands/dev-docs.md` criteria.

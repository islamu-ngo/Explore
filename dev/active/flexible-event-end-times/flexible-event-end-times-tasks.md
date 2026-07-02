# Flexible and Contextual Event End Times — Task Checklist

Last Updated: 2026-07-02 Europe/Brussels

## Status Summary
- **Overall status:** Completed
- **Completed:** 14/14
- **Current priority:** Closed
- **Next recommended slice:** N/A

---

## Implementation Maintenance Rules
- [x] Before starting work, read plan/context/tasks.
- [x] After each completed task, update this checklist immediately.
- [x] If implementation changes scope or architecture, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

---

## Phase 0: Plan Review And Baseline
- [x] **0.1 Plan Approval**
  - **Acceptance:** User reviews and approves the plan, changing planning status from Draft to Approved.
- [x] **0.2 Confirm Repo State**
  - **Acceptance:** Verify build passes cleanly before making any edits:
    `dotnet build --configuration Release --verbosity quiet`

---

## Phase 1: Domain & Persistence Changes
- [x] **1.1 Define `SessionEndTimeType` Enum**
  - **Files:** `Explore.Domain/Enums/SessionEndTimeType.cs` (new)
  - **Acceptance:** Define enum values: `Fixed = 0`, `OpenEnded = 1`, and `RelativeToPrayer = 2` with proper file-scoped namespaces.
  - **Effort:** S
- [x] **1.2 Refactor Projection Calculator Contract and Models**
  - **Files:** `Explore.Domain/Services/Scheduling/LocalScheduleProjection.cs` (modify), `Explore.Domain/Services/Scheduling/IEventScheduleProjectionCalculator.cs` (modify), `Explore.Domain/Services/Scheduling/EventScheduleProjectionCalculator.cs` (modify)
  - **Acceptance:** Make the projection calculator's end-time parameters and properties nullable.
  - **Effort:** M
  - **Dependencies:** 1.1
- [x] **1.3 Refactor `EventSession` properties and projections**
  - **Files:** `Explore.Domain/EventSession.cs` (modify)
  - **Acceptance:** Expose `EndTimeType` property. Refactor `ReprojectLocalTimes` and `Reschedule` to support nullable `EndTime` utilizing the updated calculator. Update `ContributesToPublicScheduleSummary`.
  - **Effort:** M
  - **Dependencies:** 1.1, 1.2
- [x] **1.4 Update `EventSessionIslamicAspect` scheduling rules**
  - **Files:** `Explore.Domain/EventSessionIslamicAspect.cs` (modify)
  - **Acceptance:** Add `EndReferencePrayer` and `EndOffsetMinutes` to the Islamic aspect model and update scheduling validations.
  - **Effort:** M
  - **Dependencies:** 1.1
- [x] **1.5 Configure EF Mappings and PostgreSQL Check Constraints**
  - **Files:** `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` (modify), `Explore.Persistence/Configurations/Entities/EventSessionIslamicAspectConfiguration.cs` (modify)
  - **Acceptance:** Configure column mapping for the new fields. Add check constraints for `end_time_type` states and relative end prayer configurations.
  - **Effort:** M
  - **Dependencies:** 1.3, 1.4
- [x] **1.6 Generate EF Core migration**
  - **Files:** `Explore.Persistence/Migrations/[Timestamp]_AddFlexibleEndTimes.cs` (new)
  - **Acceptance:** Generate migration containing new database columns and apply to database. Verify model snapshot.
  - **Effort:** M
  - **Dependencies:** 1.5

---

## Phase 2: Application Layer & API Changes
- [x] **2.1 Update DTO Contracts**
  - **Files:** `Explore.Application/DTOs/Event/CreateEventRequest.cs`, `Explore.Application/DTOs/EventSession/EventSessionIslamicAspectDto.cs`, `Explore.Application/DTOs/EventSession/EventSessionDto.cs` (modify)
  - **Acceptance:** Update contracts to match domain fields (nullable `EndTime`, `EndTimeType`, relative end prayer options).
  - **Effort:** S
  - **Dependencies:** Phase 1
- [x] **2.2 Refactor Validators**
  - **Files:** `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs` (modify), `Explore.Application/DTOs/EventSession/Validators/EventSessionIslamicAspectValidationRules.cs` (modify)
  - **Acceptance:** Enforce strict validation rules for direct API/UI creation, but bypass/relax validation for events containing a `ProvenanceSource` (AI posters). Validate location requirements for relative scheduling.
  - **Effort:** M
  - **Dependencies:** 2.1
- [x] **2.3 Update `EventLifecycleReadinessEvaluator`**
  - **Files:** `Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs` (modify)
  - **Acceptance:** Bypass `ScheduleEnd` missing-field validation errors if the session has `EndTimeType == SessionEndTimeType.OpenEnded`.
  - **Effort:** M
  - **Dependencies:** 2.1
- [x] **2.4 Add Formatted End Time Resolution**
  - **Files:** `Explore.Application/DTOs/EventSession/EventSessionDto.cs` (modify)
  - **Acceptance:** Expose `FormattedEndTime` string computed on the server side based on timezone/prayer rules.
  - **Effort:** M
  - **Dependencies:** 2.1

---

## Phase 3: Verification & Documentation
- [x] **3.1 Run Verification Suite**
  - **Acceptance:** Run unit tests (`Event.Domain.UnitTests`, `Event.Application.UnitTests`) and integration tests (`Event.Persistence.IntegrationTests`) to ensure zero regressions.
  - **Effort:** S
  - **Dependencies:** Phase 1, Phase 2
- [x] **3.2 Update Project Documentation**
  - **Files:** `docs/DOMAIN.md`, `docs/CUSTOM_PROPERTIES.md`, `schemas/islamu-event.md` (modify)
  - **Acceptance:** Update database schema inventory and add documentation suggesting `EstimatedDurationMinutes` as a Layer 3 custom property.
  - **Effort:** S
  - **Dependencies:** Phase 1, Phase 2

---

## Verification Checklist
- [x] LSP diagnostics clean for modified files.
- [x] `dotnet build --configuration Release --verbosity quiet` passes.
- [x] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj` passes.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj` passes.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` passes.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj` passes.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with final state and remaining work.

---

## Remaining / Deferred Work
- **Estimated Duration Property:** Deferred implementation on core aggregate root as suggested by the user. Documented as a suggestion for Layer 3 (EAV/templates) custom properties.

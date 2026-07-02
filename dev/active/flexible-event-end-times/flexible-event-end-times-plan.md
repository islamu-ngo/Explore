# Flexible and Contextual Event End Times — Implementation Plan

Last Updated: 2026-07-01 Europe/Brussels

## 0. Planning Metadata
- **Request:** Support flexible and relative event end times, relax validations for AI-imported events (posters), and support relative options like "Open Ended / Leave whenever you want" and "Until [Prayer] prayer".
- **Task directory:** `dev/active/flexible-event-end-times/`
- **Planning status:** Approved with Required Changes (Pending CTO Review)
- **Matched intents:**
  - `add-ef-migration` (Title: "Add or modify an Entity Framework Core migration")
  - `add-cqrs-handler` (Title: "Add a new Command or Query handler in the Application layer")
- **Relevant skills:**
  - `clean-architecture-rules`
  - `cqrs-mediatr-guidelines`
  - `dotnet-efcore-guidelines`
  - `i-vsd` (Islamic Value Sensitive Design)
- **Relevant rules:**
  - `.claude/rules/domain.md`
  - `.claude/rules/efcore-migrations.md`
  - `.claude/rules/application-layer.md`
- **Primary layers touched:** Domain, Application, Persistence, API, Docs
- **Estimated complexity:** M (Medium)

---

## 1. Executive Summary
This plan details the implementation of flexible, open-ended, and prayer-contextual end times for event sessions in the ISLAMU Event platform. 

* **Why it matters:** Events (especially at mosques or community circles) often lack a strict, artificial end time (e.g. lectures ending "before Asr" or community circles having "open endings"). Forcing users to enter artificial end times violates *Truthfulness* and creates false expectations.
* **Scope:**
  - Introduce `SessionEndTimeType` enum (`Fixed`, `OpenEnded`, `RelativeToPrayer`).
  - Update `LocalScheduleProjection` struct and `IEventScheduleProjectionCalculator` signature to support nullable end-time projection fields.
  - Refactor `EventSession` properties, projection recalculation (`ReprojectLocalTimes` and `Reschedule`), and public summary rollup trigger (`ContributesToPublicScheduleSummary()`).
  - Update the `EventSessionIslamicAspect` entity to store `EndReferencePrayer` and `EndOffsetMinutes` and validate their scheduling states.
  - Update EF Core configuration mappings and check constraints on PostgreSQL.
  - Make API/DTO contract requests for `EndTime` nullable.
  - Update `EventLifecycleReadinessEvaluator` to allow null end-time fields when a session is configured as `OpenEnded`.
  - Relax API validations for AI-imported events (e.g. from posters) so that end times and timezones can be omitted.
  - Expose formatted end-time strings from the API for simple client-side rendering.
* **Out of scope:** 
  - Implementing a `TillEndOfDay` option (user decided it is redundant; creators can just select 23:59).
  - Persisting or adding a dedicated `EstimatedDuration` column on the domain entity.

---

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| Database constraint allows null `end_time` on sessions. | `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` line 120 | High | `CK_EventSession_EndAfterStart` check constraint: `end_time IS NULL OR start_time IS NULL OR end_time > start_time`. |
| Projection logic currently clears local fields if `EndTime` is null. | `Explore.Domain/EventSession.cs` lines 115-124 | High | `ReprojectLocalTimes` returns early and clears all projections if `StartTime` or `EndTime` is null. |
| MediatR event creation has strict start/end time validations. | `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs` lines 132-133 | High | Requires both `StartTime` and `EndTime` to be present and `EndTime` > `StartTime`. |
| Islamic start-time scheduling types already exist. | `Explore.Domain/EventSessionIslamicAspect.cs` lines 23-34 | High | Supports `SessionStartTimeType` (`Fixed` or `RelativeToPrayer`) with reference prayer and offset minutes. |
| Room overlap checks skip rows with null end times. | `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` lines 83-111 | High | Exclusion constraint `EX_EventSession_RoomNoOverlap` predicate includes `end_time IS NOT NULL`. |
| `EventLifecycleReadinessEvaluator` fails if session end time is null. | `Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs` lines 334-350 | High | Evaluator checks `session.EndTime` and adds a missing field error if null. |

### 2.2 Existing Implementation
- **Domain Layer:** `EventSession` is the aggregate child containing UTC `StartTime` and `EndTime`. Projections are calculated using `IEventScheduleProjectionCalculator` which requires non-nullable start and end UTC timestamps and returns `LocalScheduleProjection` (a record struct where all 6 fields are currently non-nullable).
- **Application Layer:** `CreateEventRequest` receives a list of `CreateEventSessionRequest` instances. Validators enforce that `StartTime` and `EndTime` are both non-empty and sequential.
- **Persistence Layer:** Database constraints enforce valid minute-of-day offsets and positive pricing, but allow `end_time` to be null.

### 2.3 Existing Tests And Verification Coverage
- `Event.Domain.UnitTests/Aspects/EventSessionIslamicAspectTests.cs` (tests Islamic aspect start scheduling state).
- `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs` (verifies MediatR event creation logic).
- `Event.Application.UnitTests/Features/EventSessions/Commands/CreateEventSessionCommandHandlerTests.cs` (verifies session CRUD MediatR commands).
- `Event.Application.UnitTests/Services/EventLifecycleReadinessEvaluatorTests.cs` (verifies readiness checks).

### 2.4 Existing Documentation And Contracts
- `docs/DOMAIN.md` (describes aggregate structure, scheduling source of truth).
- `docs/QUICK_REFERENCE.md` (describes general validation constraints, repositories returning entities, and mapping patterns).

### 2.5 Current Pain Points / Improvement Areas
- **Strict Validation for AI Parsing:** When parsing posters to create events, the AI parser encounters validation failures because posters rarely specify end times, which makes automated imports difficult.
- **Projections Break on Null End Times:** If the end time is missing, the entire session is treated as "unscheduled," clearing the local start dates and times, which prevents it from showing on public calendars.
- **Readiness Checks Block Publication:** The lifecycle policy checks enforce non-null end times even for published open-ended events.

### 2.6 Unknowns After Investigation
- None. The schema design, aggregate behaviors, and validation flow are clear.

---

## 3. Proposed Future State
- Users can choose:
  - **Specific Time** (`Fixed`): Enter standard start and end times.
  - **Open-Ended / Flexible** (`OpenEnded`): Leave end time empty.
  - **Until [Prayer] prayer** (`RelativeToPrayer`): Define the ending relative to congregational prayer.
- For AI imports (detected via `ProvenanceSource` in the request), the validator relaxes requirements, letting timezone and end times fall back to defaults.
- The API read models return `dto.FormattedEndTime` (e.g. `"Open-ended"`, `"Until Asr prayer"`) computed by the server based on timezone/prayer lookup.
- Open-ended sessions are correctly registered in timezone projections and contribute to the parent event's schedule summaries.

---

## 4. Non-Negotiable Constraints
- **Validators manual instantiation:** `CreateEventRequestValidator` and child validators must be manually instantiated in the command handlers (no DI injection).
- **Clean Architecture Purity:** `Explore.Domain` must not reference any other layer. All relative calculation rules remain within pure domain services.
- **File-scoped namespaces:** All new C# files must use file-scoped namespaces and start with a two-line `ABOUTME:` comment.

---

## 5. Architecture And Design Decisions

### 5.1 Introduce `SessionEndTimeType` Enum
- **Decision:** Add enum to `Explore.Domain.Enums` namespace:
  ```csharp
  namespace Explore.Domain.Enums;
  public enum SessionEndTimeType { Fixed = 0, OpenEnded = 1, RelativeToPrayer = 2 }
  ```
- **Why:** Declares the scheduling intent for end times. Open-endedness is a general scheduling capability, while relative to prayer is module-specific.

### 5.2 Refactor `LocalScheduleProjection` and `IEventScheduleProjectionCalculator`
- **Decision:** Change `LocalScheduleProjection` to support nullable end fields:
  ```csharp
  public readonly record struct LocalScheduleProjection(
      DateOnly LocalStartDate,
      DateOnly? LocalEndDate,
      TimeOnly LocalStartTime,
      TimeOnly? LocalEndTime,
      int LocalStartMinuteOfDay,
      int? LocalEndMinuteOfDay);
  ```
  Update `IEventScheduleProjectionCalculator.Project` to accept `DateTimeOffset? endUtc` and return null values for end fields if `endUtc` is null.
- **Why:** Bypassing the projection calculator for open-ended sessions would duplicate timezone-conversion logic. Keeping the calculator as the single source of truth for projections is an architectural invariant.

### 5.3 Decouple Projection Calculation for Null EndTimes
- **Decision:** Refactor `EventSession.ReprojectLocalTimes` to project start fields even when `EndTime` is null.
- **Why:** Enables open-ended events to be properly sorted and displayed on calendars.

### 5.4 Update `EventSession.ContributesToPublicScheduleSummary`
- **Decision:** Update the check:
  ```csharp
  public bool ContributesToPublicScheduleSummary()
  {
      return !IsDeleted
          && EventSessionStatusId == (int)EventSessionStatusEnum.Published
          && StartTime is not null
          && (EndTimeType == SessionEndTimeType.OpenEnded || EndTimeType == SessionEndTimeType.RelativeToPrayer || EndTime is not null);
  }
  ```
- **Why:** Ensures that open-ended sessions are counted in the parent event's schedule rollup metrics.

---

## 6. Implementation Phases

### Phase 1: Domain & Persistence Changes
- **Goal:** Update entities, projection models, local projection logic, and generate the EF Core migration.
- **Acceptance criteria:**
  - `SessionEndTimeType` enum defined.
  - `LocalScheduleProjection` and `IEventScheduleProjectionCalculator` updated.
  - `EventSession` refactored to support nullable `EndTime`, `EndTimeType`, and timezone projections.
  - `EventSessionIslamicAspect` updated with `EndReferencePrayer` and `EndOffsetMinutes`.
  - Migration generated and applied.

#### Task 1.1: Create `SessionEndTimeType` Enum
- **Type:** create | **Layer:** Domain
- **Files:** [NEW] `Explore.Domain/Enums/SessionEndTimeType.cs`

#### Task 1.2: Refactor Projection Models and Calculator
- **Type:** modify | **Layer:** Domain
- **Files:** `Explore.Domain/Services/Scheduling/LocalScheduleProjection.cs`, `Explore.Domain/Services/Scheduling/IEventScheduleProjectionCalculator.cs`, `Explore.Domain/Services/Scheduling/EventScheduleProjectionCalculator.cs`
- **Description:** Allow nullable `endUtc` and return nullable end fields in the projection structure.

#### Task 1.3: Modify `EventSession` properties and projections
- **Type:** modify | **Layer:** Domain
- **Files:** `Explore.Domain/EventSession.cs`
- **Description:** Add `EndTimeType` property. Update `ReprojectLocalTimes` and `Reschedule` to support nullable `EndTime`. Update `ContributesToPublicScheduleSummary` to include open-ended/relative sessions.

#### Task 1.4: Update `EventSessionIslamicAspect`
- **Type:** modify | **Layer:** Domain
- **Files:** `Explore.Domain/EventSessionIslamicAspect.cs`
- **Description:** Add `EndReferencePrayer` and `EndOffsetMinutes` columns. Add validation rules for end scheduling state.

#### Task 1.5: Configure EF Mappings and Check Constraints
- **Type:** modify | **Layer:** Persistence
- **Files:** `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`, `Explore.Persistence/Configurations/Entities/EventSessionIslamicAspectConfiguration.cs`
- **Description:** Map new properties. Add database check constraints:
  - `CK_EventSession_EndTimeTypeState`: `((end_time_type = 0 AND end_time IS NOT NULL) OR (end_time_type = 1 AND end_time IS NULL) OR (end_time_type = 2))`
  - `CK_EventSessionIslamicAspect_EndTimeState`: `((end_reference_prayer IS NULL AND end_offset_minutes IS NULL) OR (end_reference_prayer IS NOT NULL AND end_offset_minutes IS NOT NULL))`
  - `CK_EventSessionIslamicAspect_EndOffsetRange`: `end_offset_minutes IS NULL OR end_offset_minutes BETWEEN -180 AND 180`
  - `CK_EventSessionIslamicAspect_EndReferencePrayerRange`: `end_reference_prayer IS NULL OR end_reference_prayer BETWEEN 1 AND 6`

#### Task 1.6: Generate and apply EF Core Migration
- **Type:** create | **Layer:** Persistence
- **Files:** [NEW] `Explore.Persistence/Migrations/[Timestamp]_AddFlexibleEndTimes.cs`
- **Description:** Run `dotnet ef migrations add` to generate schema changes. Update model snapshots.

---

### Phase 2: Application & API Layer Changes
- **Goal:** Update DTOs, request commands, validation logic, readiness evaluation, and read-model formatting.
- **Acceptance criteria:**
  - API schemas reflect nullable `EndTime` and `EndTimeType`.
  - Validators conditionally enforce constraints based on `ProvenanceSource` / `IsImported`.
  - `EventLifecycleReadinessEvaluator` supports open-ended sessions.
  - Read-model projections contain a computed `FormattedEndTime`.

#### Task 2.1: Update Application DTO contracts
- **Type:** modify | **Layer:** Application
- **Files:** `Explore.Application/DTOs/Event/CreateEventRequest.cs`, `Explore.Application/DTOs/EventSession/EventSessionIslamicAspectDto.cs`, `Explore.Application/DTOs/EventSession/EventSessionDto.cs`
- **Description:** Update properties to match the domain model (nullable `EndTime`, `EndTimeType`, relative end prayer).

#### Task 2.2: Implement validator changes
- **Type:** modify | **Layer:** Application
- **Files:** `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs`, `Explore.Application/DTOs/EventSession/Validators/EventSessionIslamicAspectValidationRules.cs`, `Explore.Application/DTOs/EventSession/Validators/CreateEventSessionDtoValidator.cs`, `Explore.Application/DTOs/EventSession/Validators/UpdateEventSessionDtoValidator.cs`
- **Description:** Apply conditional validation logic: bypass strict requirements if the event has a `ProvenanceSource` (imported). Validate that relative scheduling requires location details.

#### Task 2.3: Update `EventLifecycleReadinessEvaluator`
- **Type:** modify | **Layer:** Application
- **Files:** `Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs`
- **Description:** Relax `ScheduleEnd` validation check so that open-ended sessions do not emit missing-field errors.

#### Task 2.4: Implement Formatted End Time Resolution
- **Type:** modify | **Layer:** Application
- **Files:** `Explore.Application/DTOs/EventSession/EventSessionDto.cs` (or query projection mapping files)
- **Description:** Add `FormattedEndTime` string generation logic.

---

### Phase 3: Verification & Docs
- **Goal:** Run test suites and update project documentation.
- **Acceptance criteria:**
  - Build passes.
  - All unit/integration tests pass.
  - `schemas/islamu-event.md` updated with the new columns.

---

## 7. Testing Strategy
- **Unit Tests:**
  - Verify `EventSessionIslamicAspectTests.cs` for validation of the new fields.
  - Verify `CreateEventRequestValidator` using unit tests with mock imported vs. direct requests.
  - Verify `EventLifecycleReadinessEvaluatorTests.cs` to ensure open-ended sessions pass readiness checks.
- **Integration Tests:**
  - Run `Event.Persistence.IntegrationTests` to verify database constraint validation.

---

## 8. Documentation, Configuration, And Operations Impact
- Update [docs/DOMAIN.md](file:///home/amir/ISLAMU/Github/Event/docs/DOMAIN.md) with details of the new end-time types.
- Add a suggestion section in [docs/CUSTOM_PROPERTIES.md](file:///home/amir/ISLAMU/Github/Event/docs/CUSTOM_PROPERTIES.md) for configuring `EstimatedDurationMinutes` via Layer 3 EAV templates.
- Update `schemas/islamu-event.md` schema inventory.

---

## 9. Security, Authorization, Privacy, And Abuse Considerations
- Non-strict end times must not compromise rate limiting or allow scraping of unlisted/private events. Ensure that visibility rules (`VisibilityTypeId`) still apply as designed.

---

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations
- **Localization:** `FormattedEndTime` strings (e.g. `"Open-ended"`, `"Until Asr prayer"`) must be localizable through standard language localization keys.
- **Multi-Tenancy:** Timezone resolving must always use the target tenant's default timezone as a fallback for imported events if none is provided.

---

## 11. Observability And Operations
- Log validation warnings for AI imported events when fields are missing but bypassed, to help monitor AI parsing accuracy.

---

## 12. Migration And Compatibility Plan
- Migration is non-destructive since `EndTime` in the database was already nullable. Default `end_time_type` values for existing sessions will be mapped to `0` (`Fixed`).

---

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Null end times break third-party calendar exports (ICS). | Low | Medium | Exclude open-ended times from ICS or map them to a default duration (e.g. 1 hour). | ICS parsing errors or visual bugs in external calendars. | Phase 2 |
| Post-migration database constraints fail. | Low | High | Ensure default values are populated for existing rows. | Migration application failure. | Phase 1 |
| Room overlap checks bypass open-ended events. | Medium | Medium | Implement Application/BFF warnings when multiple open-ended sessions book the same room. | Room double-booking reports. | Phase 2 |

---

## 14. Success Metrics And Definition Of Done
- `dotnet build --configuration Release --verbosity quiet` passes.
- All integration and unit tests pass.
- AI imported drafts can be successfully created with null end times and empty timezones.
- Direct creations fail validation if `EndTimeType = Fixed` but no end time is specified.

---

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT
Future agents implementing this plan MUST follow this contract:
1. Before starting, read this plan, `flexible-event-end-times-context.md`, and `flexible-event-end-times-tasks.md`.
2. After completing each task, update these files to keep the progress visible.
3. final implementation summary must follow the progress reporting contract below.

---

## 16. Progress Reporting Contract
When an implementation agent finishes a slice, its final response should use this concise structure:
- **Implemented:** technical developer summary of changes.
- **Verified:** test/command runs.
- **Remaining:** outstanding tasks.
- **Next:** next task to execute.
- **Docs updated:** plan/context/tasks updated? yes/no

---

## 17. Potential Risks & Unknowns
The part most likely to cause complexity is the correct resolution of prayer-relative end times to real UTC timestamps. If a session is scheduled "Until Asr", the backend must successfully fetch the prayer time for that specific date and location, apply offsets, and update the session's UTC `EndTime` correctly. If the location details are missing, this resolution will fail. Robust validation rules are needed to ensure location data is attached before saving `RelativeToPrayer` schedules.

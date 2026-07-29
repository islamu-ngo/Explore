// ABOUTME: Policy-aware readiness evaluator that checks an Event against required fields per validation profile.
// ABOUTME: Replaces the static EventPublishReadinessEvaluator with an injectable, machine-readable error model.
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services.Lifecycle;

/// <summary>
/// Evaluates event lifecycle readiness by checking each required field in the
/// <see cref="EventLifecyclePolicy.RequiredEventFields"/> set against the supplied <see cref="Event"/>.
/// Hard invariants (cancelled/moderated/archived status blocks) are always evaluated regardless of profile.
/// </summary>
public sealed class EventLifecycleReadinessEvaluator : IEventLifecycleReadinessEvaluator
{
    /// <inheritdoc />
    public LifecycleReadinessResult Evaluate(Event @event, ValidationProfile profile, EventLifecyclePolicy policy)
    {
        var errors = new List<LifecycleReadinessError>();

        AddHardInvariantErrors(@event, profile, errors);

        foreach (Enum fieldKey in policy.RequiredEventFields)
        {
            AddFieldError(@event, fieldKey, profile, errors);
        }

        return errors.Count == 0
            ? LifecycleReadinessResult.Success(profile)
            : LifecycleReadinessResult.Failure(profile, errors);
    }

    public LifecycleReadinessResult Evaluate(EventSession session, Event? parentEvent, ValidationProfile profile, EventLifecyclePolicy policy)
    {
        var errors = new List<LifecycleReadinessError>();

        AddSessionHardInvariantErrors(session, profile, errors);

        foreach (Enum fieldKey in policy.RequiredSessionFields)
        {
            AddSessionFieldError(session, parentEvent, fieldKey, profile, errors);
        }

        return errors.Count == 0
            ? LifecycleReadinessResult.Success(profile)
            : LifecycleReadinessResult.Failure(profile, errors);
    }

    /// <summary>
    /// Hard invariants that apply to every profile: an event in Cancelled, Moderated, or Archived
    /// status can never transition to a publish-ready state.
    /// </summary>
    private static void AddHardInvariantErrors(Event @event, ValidationProfile profile, List<LifecycleReadinessError> errors)
    {
        if (@event.EventStatusId == (int)EventStatusEnum.Cancelled)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "event_cancelled",
                FieldKey: EventFieldKey.Status,
                FieldPath: "status",
                Message: "Event is cancelled and cannot be published or transitioned to a ready state.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }

        if (@event.EventStatusId == (int)EventStatusEnum.Moderated)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "event_moderated",
                FieldKey: EventFieldKey.Status,
                FieldPath: "status",
                Message: "Event is moderated and cannot be published or transitioned to a ready state.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }

        if (@event.EventStatusId == (int)EventStatusEnum.Archived)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "event_archived",
                FieldKey: EventFieldKey.Status,
                FieldPath: "status",
                Message: "Event is archived and cannot be published or transitioned to a ready state.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }
    }

    /// <summary>
    /// Checks a single required field key against the Event entity and appends an error if missing.
    /// </summary>
    private static void AddFieldError(Event @event, Enum fieldKey, ValidationProfile profile, List<LifecycleReadinessError> errors)
    {
        switch (fieldKey)
        {
            case EventFieldKey.Title:
                if (string.IsNullOrWhiteSpace(@event.Title))
                {
                    errors.Add(MissingField(profile, EventFieldKey.Title, "title", "title_required", "Event title is required."));
                }
                break;

            case EventFieldKey.Tenant:
                if (@event.TenantId == Guid.Empty)
                {
                    errors.Add(MissingField(profile, EventFieldKey.Tenant, "tenant", "tenant_required", "Event tenant is required."));
                }
                break;

            case EventFieldKey.Owner:
                if (@event.ActorId == Guid.Empty)
                {
                    errors.Add(MissingField(profile, EventFieldKey.Owner, "owner", "owner_required", "Event owner (actor) is required."));
                }
                break;

            case EventFieldKey.Status:
                if (@event.EventStatusId == 0)
                {
                    errors.Add(MissingField(profile, EventFieldKey.Status, "status", "status_required", "Event status is required."));
                }
                break;

            case EventFieldKey.Visibility:
                if (@event.VisibilityTypeId == 0)
                {
                    errors.Add(MissingField(profile, EventFieldKey.Visibility, "visibility", "visibility_required", "Event visibility type is required."));
                }
                break;

            case EventFieldKey.Format:
                if (@event.EventFormatId == 0)
                {
                    errors.Add(MissingField(profile, EventFieldKey.Format, "format", "format_required", "Event format is required."));
                }
                break;

            case EventFieldKey.Type:
                if (@event.EventTypeId is null or 0)
                {
                    errors.Add(MissingField(profile, EventFieldKey.Type, "type", "type_required", "Event type is required."));
                }
                break;

            case EventFieldKey.AudienceGender:
                if (@event.AudienceGenderId is null or 0)
                {
                    errors.Add(MissingField(profile, EventFieldKey.AudienceGender, "audience_gender", "audience_gender_required", "Event audience gender target is required."));
                }
                break;

            case EventFieldKey.AudienceAge:
                if (@event.AudienceAgeId is null or 0)
                {
                    errors.Add(MissingField(profile, EventFieldKey.AudienceAge, "audience_age", "audience_age_required", "Event audience age target is required."));
                }
                break;

            case EventFieldKey.ScheduleSessions:
                if (@event.FirstSessionStartUtc is null)
                {
                    errors.Add(MissingField(profile, EventFieldKey.ScheduleSessions, "schedule.sessions", "schedule_session_required", "At least one scheduled session is required."));
                }
                break;

            case EventFieldKey.ScheduleFirstStart:
                if (@event.FirstSessionStartUtc is null)
                {
                    errors.Add(MissingField(profile, EventFieldKey.ScheduleFirstStart, "schedule.first_start", "schedule_first_start_required", "Event schedule first start time is required."));
                }
                break;

            case EventFieldKey.ScheduleLastEnd:
                if (@event.LastSessionStartUtc is null)
                {
                    errors.Add(MissingField(profile, EventFieldKey.ScheduleLastEnd, "schedule.last_end", "schedule_last_end_required", "Event schedule last end time is required."));
                }
                break;

            case EventFieldKey.ScheduleTimeZone:
                if (string.IsNullOrWhiteSpace(@event.GetEffectiveScheduleTimeZoneId()))
                {
                    errors.Add(MissingField(profile, EventFieldKey.ScheduleTimeZone, "schedule.time_zone", "schedule_time_zone_required", "Event schedule time zone is required."));
                }
                break;

            case EventFieldKey.CoverImage:
                if (@event.FeaturedImageId is null)
                {
                    errors.Add(MissingField(profile, EventFieldKey.CoverImage, "cover_image", "cover_image_required", "Event cover image is required."));
                }
                break;

            case EventFieldKey.Description:
                if (string.IsNullOrWhiteSpace(@event.Description))
                {
                    errors.Add(MissingField(profile, EventFieldKey.Description, "description", "description_required", "Event description is required."));
                }
                break;

            case EventFieldKey.ProvenanceSource:
                if (string.IsNullOrWhiteSpace(@event.ProvenanceSource))
                {
                    errors.Add(MissingField(profile, EventFieldKey.ProvenanceSource, "provenance.source", "provenance_source_required", "Event provenance source is required."));
                }
                break;

            case EventFieldKey.ProvenanceExternalId:
                if (string.IsNullOrWhiteSpace(@event.ProvenanceExternalId))
                {
                    errors.Add(MissingField(profile, EventFieldKey.ProvenanceExternalId, "provenance.external_id", "provenance_external_id_required", "Event provenance external identifier is required."));
                }
                break;

            case EventFieldKey.Location:
                // Location is optional on Event; no required check at entity level.
                // Sessions carry room/location; event-level location is advisory.
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(fieldKey), fieldKey, $"Unknown event field key: {fieldKey}");
        }
    }

    private static void AddSessionHardInvariantErrors(EventSession session, ValidationProfile profile, List<LifecycleReadinessError> errors)
    {
        if (session.EventSessionStatusId == (int)EventSessionStatusEnum.Cancelled)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "session_cancelled",
                FieldKey: EventSessionFieldKey.Status,
                FieldPath: "status",
                Message: "Event session is cancelled and cannot be transitioned to a ready state.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }

        if (session.EventSessionStatusId == (int)EventSessionStatusEnum.Archived)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "session_archived",
                FieldKey: EventSessionFieldKey.Status,
                FieldPath: "status",
                Message: "Event session is archived and cannot be transitioned to a ready state.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }

        if (session.EventSessionStatusId == (int)EventSessionStatusEnum.Rejected)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "session_rejected",
                FieldKey: EventSessionFieldKey.Status,
                FieldPath: "status",
                Message: "Event session is rejected and cannot be transitioned to a ready state.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }

        if (session.EventSessionStatusId == (int)EventSessionStatusEnum.Completed)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "session_completed",
                FieldKey: EventSessionFieldKey.Status,
                FieldPath: "status",
                Message: "Event session is completed and cannot be transitioned to a ready state.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }

        if (session.EventSessionStatusId == (int)EventSessionStatusEnum.Moderated)
        {
            errors.Add(new LifecycleReadinessError(
                Code: "session_moderated",
                FieldKey: EventSessionFieldKey.Status,
                FieldPath: "status",
                Message: "Event session is moderated by its parent event and cannot be transitioned independently.",
                Severity: ReadinessErrorSeverity.Error,
                Source: ReadinessErrorSource.HardInvariant,
                Profile: profile));
        }
    }

    private static void AddSessionFieldError(
        EventSession session,
        Event? parentEvent,
        Enum fieldKey,
        ValidationProfile profile,
        List<LifecycleReadinessError> errors)
    {
        switch (fieldKey)
        {
            case EventSessionFieldKey.Title:
                if (string.IsNullOrWhiteSpace(session.Title))
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.Title, "title", "session_title_required", "Session title is required."));
                }
                break;

            case EventSessionFieldKey.ParentEvent:
                if (session.EventId == Guid.Empty)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.ParentEvent, "event", "session_parent_event_required", "Parent event is required."));
                }
                break;

            case EventSessionFieldKey.Tenant:
                if (session.TenantId == Guid.Empty)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.Tenant, "tenant", "session_tenant_required", "Session tenant is required."));
                }
                break;

            case EventSessionFieldKey.Status:
                if (session.EventSessionStatusId == 0)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.Status, "status", "session_status_required", "Session status is required."));
                }
                break;

            case EventSessionFieldKey.ScheduleStart:
                if (session.StartTime is null)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.ScheduleStart, "schedule.start", "session_schedule_start_required", "Session schedule start time is required."));
                }
                break;

            case EventSessionFieldKey.ScheduleEnd:
                if (session.EndTimeType == SessionEndTimeType.Fixed && session.EndTime is null)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.ScheduleEnd, "schedule.end", "session_schedule_end_required", "Session schedule end time is required."));
                }
                else if (session.StartTime is not null && session.EndTime is not null && session.EndTime <= session.StartTime)
                {
                    errors.Add(new LifecycleReadinessError(
                        Code: "session_schedule_range_invalid",
                        FieldKey: EventSessionFieldKey.ScheduleEnd,
                        FieldPath: "schedule.end",
                        Message: "Session schedule end time must be after the start time.",
                        Severity: ReadinessErrorSeverity.Error,
                        Source: ReadinessErrorSource.DomainRule,
                        Profile: profile));
                }
                break;

            case EventSessionFieldKey.Room:
                if (session.RoomId is null)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.Room, "room", "session_room_required", "Session room is required."));
                }
                break;

            case EventSessionFieldKey.Location:
                if (session.LocationId is null)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.Location, "location", "session_location_required", "Session location is required."));
                }
                break;

            case EventSessionFieldKey.Day:
                if (session.EventDayId is null)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.Day, "day", "session_day_required", "Session day is required."));
                }
                break;

            case EventSessionFieldKey.Kind:
                if (session.EventSessionKindId is null or 0)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.Kind, "kind", "session_kind_required", "Session kind is required."));
                }
                break;

            case EventSessionFieldKey.RegistrationMode:
                if (session.RegistrationModeId is null or 0)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.RegistrationMode, "registration_mode", "session_registration_mode_required", "Session registration mode is required."));
                }
                break;

            case EventSessionFieldKey.Speakers:
                break;

            case EventSessionFieldKey.ParentEventCompatibility:
                if (parentEvent is null)
                {
                    errors.Add(MissingField(profile, EventSessionFieldKey.ParentEventCompatibility, "event.status", "session_parent_event_missing", "Parent event must be loaded to validate session publication."));
                }
                else if (parentEvent.EventStatusId != (int)EventStatusEnum.Published)
                {
                    errors.Add(new LifecycleReadinessError(
                        Code: "session_parent_event_not_published",
                        FieldKey: EventSessionFieldKey.ParentEventCompatibility,
                        FieldPath: "event.status",
                        Message: "Parent event must be published before the session can be published.",
                        Severity: ReadinessErrorSeverity.Error,
                        Source: ReadinessErrorSource.DomainRule,
                        Profile: profile));
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(fieldKey), fieldKey, $"Unknown session field key: {fieldKey}");
        }
    }

    /// <summary>
    /// Builds a standard missing-field error with <see cref="ReadinessErrorSource.CommandProfile"/>.
    /// </summary>
    private static LifecycleReadinessError MissingField(
        ValidationProfile profile,
        Enum fieldKey,
        string fieldPath,
        string code,
        string message) =>
        new(
            Code: code,
            FieldKey: fieldKey,
            FieldPath: fieldPath,
            Message: message,
            Severity: ReadinessErrorSeverity.Error,
            Source: ReadinessErrorSource.CommandProfile,
            Profile: profile);
}

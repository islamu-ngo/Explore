// ABOUTME: Shared validation helpers for event-session Islamic aspect scheduling DTOs.
// ABOUTME: Keeps create-event, create-session, and update-session rules aligned with domain invariants.

using Explore.Domain;
using System;

namespace Explore.Application.DTOs.EventSession.Validators;

public static class EventSessionIslamicAspectValidationRules
{
    public const string SchedulingStateMessage =
        "Islamic session scheduling must be Fixed without ReferencePrayer/OffsetMinutes, or RelativeToPrayer with LocationId, ReferencePrayer, and OffsetMinutes.";

    public const string OffsetRangeMessage =
        "Islamic session prayer offset must be between -180 and 180 minutes.";

    public static bool HasValidSchedulingState(
        EventSessionIslamicAspectDto? aspect,
        Guid? locationId)
    {
        if (aspect is null)
        {
            return true;
        }

        return EventSessionIslamicAspect.IsValidSchedulingState(
                aspect.StartTimeType,
                aspect.ReferencePrayer,
                aspect.OffsetMinutes)
            && (aspect.StartTimeType != SessionStartTimeType.RelativeToPrayer || locationId.HasValue);
    }
}

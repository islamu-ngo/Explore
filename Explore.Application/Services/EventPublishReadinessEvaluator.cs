using Explore.Application.DTOs.Event;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public static class EventPublishReadinessEvaluator
{
    public static EventPublishReadinessDto Evaluate(Event @event)
    {
        var result = new EventPublishReadinessDto
        {
            EventId = @event.Id
        };

        AddStatusErrors(@event, result.Errors);
        AddRequiredFieldErrors(@event, result.Errors);

        result.IsReady = result.Errors.Count == 0;
        return result;
    }

    private static void AddStatusErrors(Event @event, ICollection<EventPublishReadinessErrorDto> errors)
    {
        if (@event.EventStatusId == (int)EventStatusEnum.Cancelled)
        {
            errors.Add(new EventPublishReadinessErrorDto
            {
                Code = "event_cancelled",
                FieldPath = "status",
                Message = "Cancelled events cannot be published."
            });
        }

        if (@event.EventStatusId == (int)EventStatusEnum.Archived)
        {
            errors.Add(new EventPublishReadinessErrorDto
            {
                Code = "event_archived",
                FieldPath = "status",
                Message = "Archived events cannot be published."
            });
        }
    }

    private static void AddRequiredFieldErrors(Event @event, ICollection<EventPublishReadinessErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(@event.Title))
        {
            errors.Add(new EventPublishReadinessErrorDto
            {
                Code = "title_required",
                FieldPath = "title",
                Message = "Event title is required before publishing."
            });
        }

        if (@event.FirstSessionStartUtc is null)
        {
            errors.Add(new EventPublishReadinessErrorDto
            {
                Code = "schedule_session_required",
                FieldPath = "schedule.sessions",
                Message = "At least one scheduled session is required before publishing."
            });
        }
    }
}

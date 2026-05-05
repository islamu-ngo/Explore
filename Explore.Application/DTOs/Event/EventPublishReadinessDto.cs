namespace Explore.Application.DTOs.Event;

public class EventPublishReadinessDto
{
    public Guid EventId { get; set; }
    public bool IsReady { get; set; }
    public List<EventPublishReadinessErrorDto> Errors { get; set; } = [];
}

namespace Explore.Application.DTOs.Event;

public sealed record EventPublishReadinessDto
{
    public Guid EventId { get; init; }
    public bool IsReady { get; init; }
    public List<EventPublishReadinessErrorDto> Errors { get; init; } = [];
}

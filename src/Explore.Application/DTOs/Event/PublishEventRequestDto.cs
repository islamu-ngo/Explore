namespace Explore.Application.DTOs.Event;

public sealed record PublishEventRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; init; }
}

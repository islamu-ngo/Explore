namespace Explore.Application.DTOs.Event;

public class PublishEventRequestDto
{
    public Guid ExpectedConcurrencyStamp { get; set; }
}

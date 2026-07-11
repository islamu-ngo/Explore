// ABOUTME: Publisher option available during event creation.
// ABOUTME: Represents personal, organization, or group publishing with create affordance metadata.

namespace Explore.Application.DTOs.Event;

public class EventCreationPublisherOptionDto
{
    public required string PublisherMode { get; set; }

    public Guid? PublisherId { get; set; }

    public required string DisplayName { get; set; }

    public int? RoleId { get; set; }

    public bool CanPublish { get; set; }

    public string? Reason { get; set; }
}

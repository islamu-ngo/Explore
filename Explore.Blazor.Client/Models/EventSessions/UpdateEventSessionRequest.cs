// ABOUTME: Blazor-facing request model for the dedicated program item edit composer.
// ABOUTME: Keeps generated API DTOs isolated at the EventService boundary.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Models.EventSessions;

public sealed class UpdateEventSessionRequest
{
    public Guid? Id { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public Guid? EventId { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public Guid? LocationId { get; set; }

    public Guid? FeaturedImageId { get; set; }

    public Guid? RoomId { get; set; }

    public int? SortOrder { get; set; }

    public string? Title { get; set; }

    public int? EventSessionKindId { get; set; }

    public string? Description { get; set; }

    public string? Slug { get; set; }

    public int? MaxAudienceAttendees { get; set; }

    public int? RegistrationModeId { get; set; }

    public HashSet<int> LanguageIds { get; set; } = [];

    public double? Price { get; set; }

    public string? CurrencyCode { get; set; }

    public EventSessionIslamicAspectDto? IslamicAspect { get; set; }
}

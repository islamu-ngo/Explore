// ABOUTME: Shared session editor model used by session summary cards, editor panel, and event pages.
// ABOUTME: Extracted from EventSessionEditor.razor inner class for cross-component reuse.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Pages.Events.Models;

public class SessionEditorModel
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Today.AddHours(9);
    public DateTime EndTime { get; set; } = DateTime.Today.AddHours(17);
    public Guid? LocationId { get; set; }
    public int? MaxAudienceAttendees { get; set; }
    public int? RegistrationModeId { get; set; } = 1;
    public IReadOnlyCollection<int> LanguageIds { get; set; } = new HashSet<int>();

    // Media
    public Guid? FeaturedImageId { get; set; }
    public string? FeaturedImagePreviewUrl { get; set; }
    public bool UseEventImage { get; set; } = true;
    public byte[]? PendingImageBytes { get; set; }
    public string? PendingImageFileName { get; set; }

    public CreateEventSessionDto ToCreateDto(Guid eventId, Guid tenantId)
    {
        return new CreateEventSessionDto
        {
            EventId = eventId,
            Title = Title ?? "Session",
            Description = Description,
            StartTime = DateTimeHelper.ConvertLocalToUtc(StartTime),
            EndTime = DateTimeHelper.ConvertLocalToUtc(EndTime),
            LocationId = LocationId,
            MaxAudienceAttendees = MaxAudienceAttendees,
            RegistrationModeId = RegistrationModeId,
            TenantId = tenantId,
            FeaturedImageId = UseEventImage ? null : FeaturedImageId
        };
    }

    public UpdateEventSessionDto ToUpdateDto(Guid eventId)
    {
        return new UpdateEventSessionDto
        {
            Id = Id ?? Guid.Empty,
            EventId = eventId,
            Title = Title ?? "Session",
            Description = Description,
            StartTime = DateTimeHelper.ConvertLocalToUtc(StartTime),
            EndTime = DateTimeHelper.ConvertLocalToUtc(EndTime),
            LocationId = LocationId,
            MaxAudienceAttendees = MaxAudienceAttendees,
            RegistrationModeId = RegistrationModeId,
            FeaturedImageId = UseEventImage ? null : FeaturedImageId
        };
    }

    public static SessionEditorModel FromDto(EventSessionListDto dto, string? eventImageUri = null)
    {
        var hasOwnImage = dto.FeaturedImageId.HasValue;
        return new SessionEditorModel
        {
            Id = dto.Id,
            Title = dto.Title ?? dto.EventTitle,
            Description = null,
            StartTime = dto.StartTime?.LocalDateTime ?? DateTime.Now,
            EndTime = dto.EndTime?.LocalDateTime ?? DateTime.Now.AddHours(1),
            LocationId = dto.LocationId,
            MaxAudienceAttendees = dto.MaxAudienceAttendees,
            RegistrationModeId = dto.RegistrationModeId,
            FeaturedImageId = dto.FeaturedImageId,
            FeaturedImagePreviewUrl = dto.FeaturedImageUri ?? eventImageUri,
            UseEventImage = !hasOwnImage
        };
    }

    /// <summary>
    /// Creates a deep copy for session duplication.
    /// Nullifies the Id and shifts dates forward by one day.
    /// New sessions inherit the event image by default.
    /// </summary>
    public SessionEditorModel Clone()
    {
        return new SessionEditorModel
        {
            Id = null,
            Title = string.IsNullOrWhiteSpace(Title) ? null : $"{Title} (Copy)",
            Description = Description,
            StartTime = StartTime.AddDays(1),
            EndTime = EndTime.AddDays(1),
            LocationId = LocationId,
            MaxAudienceAttendees = MaxAudienceAttendees,
            RegistrationModeId = RegistrationModeId,
            LanguageIds = new HashSet<int>(LanguageIds),
            FeaturedImageId = null,
            FeaturedImagePreviewUrl = null,
            UseEventImage = true,
            PendingImageBytes = null,
            PendingImageFileName = null
        };
    }
}

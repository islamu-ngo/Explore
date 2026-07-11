// ABOUTME: DTO for single-row detail views of EventSessionStatus lookup rows.
// ABOUTME: Mirrors EventStatusDto shape: Id, MasterCode (i18n key), FullName fallback, optional Description.
namespace Explore.Application.DTOs.EventSessionStatus;

public class EventSessionStatusDto
{
    public int Id { get; set; }

    /// <summary>Stable code used for i18n via Tolgee; never localized.</summary>
    public required string MasterCode { get; set; }

    /// <summary>Fallback display name used when no localization is available.</summary>
    public required string FullName { get; set; }

    public string? Description { get; set; }
}

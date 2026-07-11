// ABOUTME: DTO for the organiser contacts page showing granted email consents.
// ABOUTME: Returns email snapshot data for authorised organisation members to view/export.

namespace Explore.Application.DTOs.ContactShareConsent;

public class SharedContactDto
{
    public Guid ConsentId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public Guid? SourceEventId { get; set; }
    public string? SourceEventTitle { get; set; }
    public string PurposeCode { get; set; } = string.Empty;
}

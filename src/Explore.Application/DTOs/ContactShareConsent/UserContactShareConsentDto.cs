// ABOUTME: DTO for the user's Connected Apps page showing granted/withdrawn consents.
// ABOUTME: Includes organisation display info and consent status for user self-management.

namespace Explore.Application.DTOs.ContactShareConsent;

public class UserContactShareConsentDto
{
    public Guid Id { get; set; }
    public Guid RecipientActorId { get; set; }
    public string? OrganizationName { get; set; }
    public Guid? SourceEventId { get; set; }
    public string? SourceEventTitle { get; set; }
    public string PurposeCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public string EmailSnapshot { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
}

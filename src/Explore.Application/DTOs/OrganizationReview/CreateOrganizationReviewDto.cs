// ABOUTME: API request payload for creating an organization review.
// ABOUTME: Excludes reviewer user identity because the server derives it from the authenticated principal.

namespace Explore.Application.DTOs.OrganizationReview;

public class CreateOrganizationReviewDto
{
    public Guid OrganizationId { get; set; }
    public Guid ProgramId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

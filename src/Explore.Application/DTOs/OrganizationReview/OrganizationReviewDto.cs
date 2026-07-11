namespace Explore.Application.DTOs.OrganizationReview;

public class OrganizationReviewDto
{
    public Guid Id { get; set; }

    // Organization
    public Guid OrganizationId { get; set; }
    public string? OrganizationFullName { get; set; }

    // User
    public Guid UserId { get; set; }
    public string? UserFullName { get; set; }

    // Review
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

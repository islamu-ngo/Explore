namespace Explore.Blazor.Client.Models.DTOs
{
    public class OrganizationReviewDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid ProgramId { get; set; }
        public Guid UserId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

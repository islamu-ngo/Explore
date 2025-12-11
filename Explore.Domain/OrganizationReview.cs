using System;

namespace Explore.Domain
{
    public class OrganizationReview
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid ProgramId { get; set; }
        public virtual Program? Program { get; set; }
        public Guid UserId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

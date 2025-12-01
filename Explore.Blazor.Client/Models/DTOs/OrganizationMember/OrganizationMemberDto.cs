using Explore.Blazor.Client.Models.Enums;

namespace Explore.Blazor.Client.Models.DTOs.OrganizationMember
{
    public class OrganizationMemberDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public OrganizationRole Role { get; set; }
        public string? UserName { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}

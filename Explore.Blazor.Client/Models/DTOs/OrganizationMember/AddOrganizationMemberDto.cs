using Explore.Blazor.Client.Models.Enums;

namespace Explore.Blazor.Client.Models.DTOs.OrganizationMember
{
    public class AddOrganizationMemberDto
    {
        public Guid OrganizationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public OrganizationRole Role { get; set; }
    }
}

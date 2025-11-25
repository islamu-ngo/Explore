using Explore.Blazor.Client.Models.Enums;

namespace Explore.Blazor.Client.Models.DTOs.OrganizationMember
{
    public class UpdateOrganizationMemberRoleDto
    {
        public Guid Id { get; set; }
        public OrganizationRole Role { get; set; }
    }
}

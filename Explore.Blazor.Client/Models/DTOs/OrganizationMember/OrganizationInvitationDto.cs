using Explore.Blazor.Client.Models.Enums;

namespace Explore.Blazor.Client.Models.DTOs.OrganizationMember
{
    public class OrganizationInvitationDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public OrganizationRole Role { get; set; }
        public string Email { get; set; }
    }
}

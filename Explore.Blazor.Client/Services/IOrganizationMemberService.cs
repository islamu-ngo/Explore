using Explore.Blazor.Client.Models.DTOs.OrganizationMember;

namespace Explore.Blazor.Client.Services
{
    public interface IOrganizationMemberService
    {
        Task<List<OrganizationMemberDto>> GetMembersAsync(Guid organizationId);
        Task<OrganizationMemberDto> InviteMemberAsync(AddOrganizationMemberDto member);
        Task UpdateMemberRoleAsync(UpdateOrganizationMemberRoleDto updateDto);
        Task<List<OrganizationInvitationDto>> GetMyInvitationsAsync();
        Task AcceptInvitationAsync(Guid invitationId);
        Task DeclineInvitationAsync(Guid invitationId);
        Task DeleteMemberAsync(Guid memberId);
    }
}

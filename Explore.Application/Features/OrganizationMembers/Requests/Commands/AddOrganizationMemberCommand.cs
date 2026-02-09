using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

public class AddOrganizationMemberCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required AddOrganizationMemberDto AddOrganizationMemberDto { get; set; }
    public required string RequesterUserId { get; set; } // To check permissions
}

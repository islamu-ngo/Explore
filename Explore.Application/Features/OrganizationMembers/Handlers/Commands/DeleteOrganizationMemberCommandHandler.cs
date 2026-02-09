using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands;

public class DeleteOrganizationMemberCommandHandler : IRequestHandler<DeleteOrganizationMemberCommand, BaseCommandResponse<Guid>>
{
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public DeleteOrganizationMemberCommandHandler(
        IOrganizationMemberRepository organizationMemberRepository,
        IOrganizationRepository organizationRepository)
    {
        _organizationMemberRepository = organizationMemberRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeleteOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var memberToDelete = await _organizationMemberRepository.GetById(request.MemberId);

        if (memberToDelete == null)
        {
            response.Success = false;
            response.Message = "Member not found";
            return response;
        }

        var organization = await _organizationRepository.GetById(memberToDelete.OrganizationId);
        if (organization == null)
        {
            response.Success = false;
            response.Message = "Organization not found";
            return response;
        }

        // Check permissions - requester must be an Admin
        var members = await _organizationMemberRepository.GetMembersByOrganizationId(memberToDelete.OrganizationId);
        if (Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
        {
            var requesterMember = members.FirstOrDefault(m => m.UserId == requesterGuid);
            // Only Admin role (OrganizationRoleId = 1) can remove members
            if (requesterMember == null || requesterMember.OrganizationRoleId != (int)OrganizationRoleEnum.Admin)
            {
                response.Success = false;
                response.Message = "You do not have permission to remove members.";
                return response;
            }
        }
        else
        {
            response.Success = false;
            response.Message = "Invalid requester User ID.";
            return response;
        }

        // Prevent self-deletion if they are the only Admin
        var adminCount = members.Count(m => m.OrganizationRoleId == (int)OrganizationRoleEnum.Admin);
        if (memberToDelete.OrganizationRoleId == (int)OrganizationRoleEnum.Admin && adminCount <= 1)
        {
            response.Success = false;
            response.Message = "Cannot remove the last admin of the organization.";
            return response;
        }

        await _organizationMemberRepository.Delete(memberToDelete);

        response.Success = true;
        response.Message = "Member removed successfully";
        response.Id = memberToDelete.Id;

        return response;
    }
}

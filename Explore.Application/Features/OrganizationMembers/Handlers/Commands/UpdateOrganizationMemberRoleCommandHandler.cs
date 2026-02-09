using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands;

public class UpdateOrganizationMemberRoleCommandHandler : IRequestHandler<UpdateOrganizationMemberRoleCommand, BaseCommandResponse<Guid>>
{
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;

    public UpdateOrganizationMemberRoleCommandHandler(
        IOrganizationMemberRepository organizationMemberRepository,
        IOrganizationRepository organizationRepository,
        IMapper mapper)
    {
        _organizationMemberRepository = organizationMemberRepository;
        _organizationRepository = organizationRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateOrganizationMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var dto = request.UpdateOrganizationMemberRoleDto;

        var memberToUpdate = await _organizationMemberRepository.GetById(dto.Id);
        if (memberToUpdate == null)
        {
            response.Success = false;
            response.Message = "Member not found";
            return response;
        }

        var organization = await _organizationRepository.GetById(memberToUpdate.OrganizationId);
        if (organization == null)
        {
            response.Success = false;
            response.Message = "Organization not found";
            return response;
        }

        // Check permissions - requester must be an Admin
        var members = await _organizationMemberRepository.GetMembersByOrganizationId(memberToUpdate.OrganizationId);
        if (Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
        {
            var requesterMember = members.FirstOrDefault(m => m.UserId == requesterGuid);
            // Only Admin role (OrganizationRoleId = 1) can update roles
            if (requesterMember == null || requesterMember.OrganizationRoleId != (int)OrganizationRoleEnum.Admin)
            {
                response.Success = false;
                response.Message = "You do not have permission to update roles.";
                return response;
            }

            // Prevent demoting the last admin
            var adminCount = members.Count(m => m.OrganizationRoleId == (int)OrganizationRoleEnum.Admin);
            if (memberToUpdate.OrganizationRoleId == (int)OrganizationRoleEnum.Admin &&
                (int)dto.Role != (int)OrganizationRoleEnum.Admin &&
                adminCount <= 1)
            {
                response.Success = false;
                response.Message = "Cannot demote the last admin of the organization.";
                return response;
            }
        }
        else
        {
            response.Success = false;
            response.Message = "Invalid requester User ID.";
            return response;
        }

        memberToUpdate.OrganizationRoleId = (int)dto.Role;
        await _organizationMemberRepository.Update(memberToUpdate);

        response.Success = true;
        response.Message = "Member role updated successfully";
        response.Id = memberToUpdate.Id;

        return response;
    }
}

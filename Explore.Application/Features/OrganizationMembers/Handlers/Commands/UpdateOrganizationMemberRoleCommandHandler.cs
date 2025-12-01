using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands
{
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

            // Check permissions
            bool isOwner = organization.CreatedByUserId == request.RequesterUserId;
            
            if (!isOwner)
            {
                var members = await _organizationMemberRepository.GetMembersByOrganizationId(memberToUpdate.OrganizationId);
                if (Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
                {
                    var requesterMember = members.FirstOrDefault(m => m.UserId == requesterGuid);
                    // Only Admins, CoOwners and Creators can update roles
                    if (requesterMember == null || (requesterMember.Role != OrganizationRole.Admin && requesterMember.Role != OrganizationRole.CoOwner && requesterMember.Role != OrganizationRole.Creator))
                    {
                        response.Success = false;
                        response.Message = "You do not have permission to update roles.";
                        return response;
                    }
                    
                    // Admins cannot change role of Creator or other Admins (optional rule, but good practice)
                    if (memberToUpdate.Role == OrganizationRole.Creator)
                    {
                         response.Success = false;
                         response.Message = "Cannot change role of the Creator.";
                         return response;
                    }
                }
                else
                {
                     response.Success = false;
                     response.Message = "Invalid requester User ID.";
                     return response;
                }
            }

            memberToUpdate.Role = dto.Role;
            await _organizationMemberRepository.Update(memberToUpdate);

            response.Success = true;
            response.Message = "Member role updated successfully";
            response.Id = memberToUpdate.Id;

            return response;
        }
    }
}

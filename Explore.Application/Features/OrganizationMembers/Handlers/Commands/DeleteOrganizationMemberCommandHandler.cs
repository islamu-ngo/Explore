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

            // Check permissions
            bool isOwner = organization.CreatedByUserId == request.RequesterUserId;

            if (!isOwner)
            {
                var members = await _organizationMemberRepository.GetMembersByOrganizationId(memberToDelete.OrganizationId);
                if (Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
                {
                    var requesterMember = members.FirstOrDefault(m => m.UserId == requesterGuid);
                    if (requesterMember == null || (requesterMember.Role != OrganizationRole.Admin && requesterMember.Role != OrganizationRole.CoOwner && requesterMember.Role != OrganizationRole.Creator))
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
            }

            // Prevent deleting the Owner (if the member being deleted is the owner)
            // Note: CreatedByUserId is a string, member.UserId is a Guid?.
            // If memberToDelete.UserId matches CreatedByUserId, we shouldn't delete it unless we are deleting the org?
            // Or maybe ownership transfer is needed. For now, let's just say you can't delete the creator.
            if (memberToDelete.UserId.ToString() == organization.CreatedByUserId)
            {
                 response.Success = false;
                 response.Message = "Cannot remove the organization owner.";
                 return response;
            }

            await _organizationMemberRepository.Delete(memberToDelete);

            response.Success = true;
            response.Message = "Member removed successfully";
            response.Id = memberToDelete.Id;

            return response;
        }
    }
}

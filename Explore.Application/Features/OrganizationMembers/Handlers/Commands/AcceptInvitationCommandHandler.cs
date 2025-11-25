using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands
{
    public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, BaseCommandResponse<Guid>>
    {
        private readonly IOrganizationMemberRepository _organizationMemberRepository;

        public AcceptInvitationCommandHandler(IOrganizationMemberRepository organizationMemberRepository)
        {
            _organizationMemberRepository = organizationMemberRepository;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();
            var invitation = await _organizationMemberRepository.GetById(request.InvitationId);

            if (invitation == null)
            {
                response.Success = false;
                response.Message = "Invitation not found";
                return response;
            }

            if (invitation.UserId != null)
            {
                response.Success = false;
                response.Message = "Invitation already accepted";
                return response;
            }

            invitation.UserId = request.UserId;
            await _organizationMemberRepository.Update(invitation);

            response.Success = true;
            response.Message = "Invitation accepted";
            response.Id = invitation.Id;

            return response;
        }
    }
}

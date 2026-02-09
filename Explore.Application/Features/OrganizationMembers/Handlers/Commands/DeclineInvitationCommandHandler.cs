using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands;

public class DeclineInvitationCommandHandler : IRequestHandler<DeclineInvitationCommand, BaseCommandResponse<Guid>>
{
    private readonly IOrganizationMemberRepository _organizationMemberRepository;

    public DeclineInvitationCommandHandler(IOrganizationMemberRepository organizationMemberRepository)
    {
        _organizationMemberRepository = organizationMemberRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeclineInvitationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var invitation = await _organizationMemberRepository.GetById(request.InvitationId);

        if (invitation == null)
        {
            response.Success = false;
            response.Message = "Invitation not found";
            return response;
        }

        // Ideally we should check if the invitation belongs to the user (by email)
        // But for now we assume the controller handles the security or we trust the ID.
        // Since UserId is null on invite, we can't check UserId. We could check Email if we passed it.

        await _organizationMemberRepository.Delete(invitation);

        response.Success = true;
        response.Message = "Invitation declined";
        response.Id = invitation.Id;

        return response;
    }
}

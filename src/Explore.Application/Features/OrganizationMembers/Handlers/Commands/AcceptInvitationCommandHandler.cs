// ABOUTME: Handler for accepting an organization membership invitation.
// ABOUTME: Validates invitation ownership before confirming the membership record.

using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands;

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

        if (invitation.UserId == Guid.Empty || invitation.UserId != request.UserId)
        {
            response.Success = false;
            response.Message = "Invitation not found";
            return response;
        }

        response.Success = true;
        response.Message = "Invitation accepted";
        response.Id = invitation.Id;

        return response;
    }
}

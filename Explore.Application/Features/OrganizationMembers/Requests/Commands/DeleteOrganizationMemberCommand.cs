using System;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

public class DeleteOrganizationMemberCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid MemberId { get; set; }
    public required string RequesterUserId { get; set; }
}

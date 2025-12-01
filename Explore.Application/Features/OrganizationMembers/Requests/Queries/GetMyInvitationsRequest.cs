using Explore.Application.DTOs.OrganizationMember;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.OrganizationMembers.Requests.Queries
{
    public class GetMyInvitationsRequest : IRequest<List<OrganizationInvitationDto>>
    {
        public string Email { get; set; }
    }
}

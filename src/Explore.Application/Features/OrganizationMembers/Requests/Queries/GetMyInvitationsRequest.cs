// ABOUTME: MediatR query for fetching the current user's pending organization invitations.
// ABOUTME: Returns IEnumerable<OrganizationMemberDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.OrganizationMember;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Queries;

public class GetMyInvitationsRequest : IRequest<List<OrganizationInvitationDto>>
{
    public required string Email { get; set; }
}

// ABOUTME: MediatR query for fetching a single organization membership with related details.
// ABOUTME: Returns null when the requested membership does not exist.

using Explore.Application.DTOs.OrganizationMember;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Queries;

public sealed class GetOrganizationMemberDetailsRequest : IRequest<OrganizationMemberDto?>
{
    public Guid Id { get; init; }
}

// ABOUTME: MediatR query for fetching a single organization membership with related details.
// ABOUTME: Returns null when the requested membership does not exist.

using Explore.Application.Authorization;
using Explore.Application.DTOs.OrganizationMember;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Queries;

[AuthorizeResource(ResourceKinds.OrganizationMember, AuthorizationActions.OrganizationMembers.View)]
public sealed class GetOrganizationMemberDetailsRequest : IRequest<OrganizationMemberDto?>, ISecureRequest
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => Id == Guid.Empty ? null : Id.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D")
    };
}

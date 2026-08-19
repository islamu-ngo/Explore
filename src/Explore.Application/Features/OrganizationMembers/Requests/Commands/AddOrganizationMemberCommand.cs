// ABOUTME: MediatR command for adding a member to an organization.
// ABOUTME: Carries tenant and organization context for pre-create authorization.
using Explore.Application.Authorization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

[AuthorizeResource(ResourceKinds.OrganizationMember, AuthorizationActions.Create)]
public class AddOrganizationMemberCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required AddOrganizationMemberDto AddOrganizationMemberDto { get; set; }
    public required string RequesterUserId { get; set; } // To check permissions
    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => AddOrganizationMemberDto.OrganizationId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new OrganizationMemberAuthorizationFacts(TenantId, AddOrganizationMemberDto.OrganizationId, null, null);
}

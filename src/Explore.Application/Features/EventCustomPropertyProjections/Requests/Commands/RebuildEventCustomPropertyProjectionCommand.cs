// ABOUTME: Command to trigger a tenant-wide rebuild of event custom-property projection rows.
// ABOUTME: Authorized through custom-property projection resource metadata; uses Complex request timeout.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;

[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.Update)]
public class RebuildEventCustomPropertyProjectionCommand : IRequest<BaseCommandResponse<RebuildProjectionResponseDto>>, ISecureRequest
{
    public required RebuildProjectionRequestDto RequestDto { get; set; }

    private Guid TenantId => RequestDto?.TenantId ?? Guid.Empty;

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new CustomPropertyProjectionAuthorizationFacts(TenantId, null, null);
}

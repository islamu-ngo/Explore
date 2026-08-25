// ABOUTME: MediatR command for soft-deleting an organization through the Application layer.
// ABOUTME: Carries caller identity so handler-level authorization remains centralized and testable.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Delete)]
public sealed record DeleteOrganizationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
}

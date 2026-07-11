// ABOUTME: MediatR command for PATCH-based Group profile and hierarchy updates.
// ABOUTME: Carries route authority, current user authorization context, If-Match concurrency, and grouped payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Group;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Groups.Requests.Commands;

[AuthorizeResource(ResourceKinds.Group, AuthorizationActions.Update)]
public class UpdateGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid GroupId { get; set; }

    public required string UserId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateGroupDto UpdateGroupDto { get; set; }

    string? ISecureRequest.ResourceId => GroupId.ToString();
}

// ABOUTME: MediatR command for starting one idempotent asynchronous User erasure.
// ABOUTME: Carries the authenticated subject and required UUIDv7 request identity.
using Explore.Application.Authorization;
using Explore.Application.DTOs.PrivacyErasure;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

[AuthorizeResource(ResourceKinds.User, AuthorizationActions.Delete)]
public class DeleteUserCommand : IRequest<PrivacyErasureStartDto>, ISecureRequest
{
    public Guid UserId { get; set; }
    public Guid IntentId { get; set; }

    string? ISecureRequest.ResourceId => UserId.ToString();
}

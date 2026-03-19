// ABOUTME: Command request for deleting a shared Layer 3 custom-property definition.
// ABOUTME: Uses feature-specific delete semantics so recreated machine keys are not trapped accidentally.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;

[AuthorizeResource("tenant", PermissionAction.Update)]
public class DeleteCustomPropertyDefinitionCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}

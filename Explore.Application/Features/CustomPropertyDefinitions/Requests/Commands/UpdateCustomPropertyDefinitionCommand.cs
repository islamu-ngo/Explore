// ABOUTME: Command request for updating a shared Layer 3 custom-property definition.
// ABOUTME: Keeps tenant-governed update semantics aligned with the create flow.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;

[AuthorizeResource("tenant", PermissionAction.Update)]
public class UpdateCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateCustomPropertyDefinitionDto DefinitionDto { get; set; }

    string? ISecureRequest.ResourceId => DefinitionDto.Id.ToString();
}

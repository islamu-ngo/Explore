// ABOUTME: Command request for creating a shared Layer 3 custom-property definition for organization or group scopes.
// ABOUTME: Uses tenant-level authorization semantics because shared definitions are tenant-governed catalogs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class CreateCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateCustomPropertyDefinitionDto DefinitionDto { get; set; }

    string? ISecureRequest.ResourceId => null;
}

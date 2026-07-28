// ABOUTME: Command request for updating a shared Layer 3 custom-property definition.
// ABOUTME: Route ID and If-Match carry identity/concurrency; the body carries the update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid DefinitionId { get; set; }

    public required UpdateCustomPropertyDefinitionDto DefinitionDto { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    string? ISecureRequest.ResourceId => DefinitionId.ToString();
}

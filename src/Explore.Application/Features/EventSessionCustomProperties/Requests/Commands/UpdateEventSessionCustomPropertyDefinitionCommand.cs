// ABOUTME: Command request for updating a session-local custom-property definition.
// ABOUTME: Route ID and If-Match carry identity/concurrency; the body carries the update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateEventSessionCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid DefinitionId { get; set; }

    public required UpdateEventSessionCustomPropertyDefinitionDto DefinitionDto { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    string? ISecureRequest.ResourceId => DefinitionId.ToString();
}

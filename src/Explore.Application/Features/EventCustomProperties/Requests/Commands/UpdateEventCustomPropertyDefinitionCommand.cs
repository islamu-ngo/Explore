// ABOUTME: Command request for updating an event-local custom-property definition.
// ABOUTME: Route ID and If-Match carry identity/concurrency; the body carries the update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateEventCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid DefinitionId { get; set; }

    public required UpdateEventCustomPropertyDefinitionDto DefinitionDto { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    string? ISecureRequest.ResourceId => DefinitionId.ToString();
}

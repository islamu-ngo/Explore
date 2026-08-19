// ABOUTME: Command request for replacing all values of a multi-value custom property definition.
// ABOUTME: Atomically removes existing values and inserts the new set for the given definition+event.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class SetEventCustomPropertyMultiValuesCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid DefinitionId { get; set; }
    public Guid EventId { get; set; }
    public required List<SetEventCustomPropertyValueDto> Values { get; set; }

    string? ISecureRequest.ResourceId => null;
}

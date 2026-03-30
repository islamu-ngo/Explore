// ABOUTME: Command request for replacing all values of a multi-value session custom property definition.
// ABOUTME: Atomically removes existing values and inserts the new set for the given definition+session.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", PermissionAction.Update)]
public class SetEventSessionCustomPropertyMultiValuesCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid DefinitionId { get; set; }
    public Guid EventSessionId { get; set; }
    public required List<SetEventSessionCustomPropertyValueDto> Values { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}

// ABOUTME: Command request for replacing all values of a multi-value session custom property definition.
// ABOUTME: Atomically removes existing values and inserts the new set for the given definition+session.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public sealed record SetEventSessionCustomPropertyMultiValuesCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid DefinitionId { get; init; }
    public Guid EventSessionId { get; init; }
    private IReadOnlyList<SetEventSessionCustomPropertyValueDto> _values = Array.AsReadOnly(Array.Empty<SetEventSessionCustomPropertyValueDto>());

    public required IReadOnlyList<SetEventSessionCustomPropertyValueDto> Values
    {
        get => _values;
        init => _values = value is null ? null! : Array.AsReadOnly(value.ToArray());
    }

    string? ISecureRequest.ResourceId => null;
}

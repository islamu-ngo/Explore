// ABOUTME: Command request for setting a single session custom property value (upsert by definition+session+ordinal).
// ABOUTME: Single-value definitions use Ordinal=0; multi-value definitions use ascending ordinals.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", PermissionAction.Update)]
public class SetEventSessionCustomPropertyValueCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required SetEventSessionCustomPropertyValueDto ValueDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}

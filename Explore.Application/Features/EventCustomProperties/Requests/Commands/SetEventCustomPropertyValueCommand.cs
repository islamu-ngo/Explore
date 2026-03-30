// ABOUTME: Command request for setting a single event custom property value (upsert by definition+event+ordinal).
// ABOUTME: Single-value definitions use Ordinal=0; multi-value definitions use ascending ordinals.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Commands;

[AuthorizeResource("tenant", PermissionAction.Update)]
public class SetEventCustomPropertyValueCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required SetEventCustomPropertyValueDto ValueDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}

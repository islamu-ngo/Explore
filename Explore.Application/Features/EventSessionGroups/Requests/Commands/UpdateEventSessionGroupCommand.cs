// ABOUTME: MediatR command for updating an event session group.
// ABOUTME: Carries EventId so authorization and validation stay scoped to the owning event.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSessionGroup, AuthorizationActions.Update)]
public class UpdateEventSessionGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventSessionGroupRequestDto EventSessionGroup { get; set; }

    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionGroup.EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["eventId"] = EventSessionGroup.EventId.ToString()
    };
}

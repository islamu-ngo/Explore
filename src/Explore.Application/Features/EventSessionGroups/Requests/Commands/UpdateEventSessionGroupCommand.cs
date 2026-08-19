// ABOUTME: MediatR command for updating an event session group.
// ABOUTME: Carries route identity, concurrency, and server-bound authorization context for grouped PATCH.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSessionGroup, AuthorizationActions.Update)]
public class UpdateEventSessionGroupCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventSessionGroupId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventSessionGroupRequestDto EventSessionGroup { get; set; }

    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionGroupId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(TenantId, EventId);
}

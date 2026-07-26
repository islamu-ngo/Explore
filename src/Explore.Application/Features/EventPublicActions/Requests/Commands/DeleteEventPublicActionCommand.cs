// ABOUTME: Authorized CQRS request for soft-deleting an event public action.
// ABOUTME: Requires the action concurrency stamp while authorizing against its event.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventPublicActions.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePublicActions)]
public sealed class DeleteEventPublicActionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public Guid ActionId { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
}

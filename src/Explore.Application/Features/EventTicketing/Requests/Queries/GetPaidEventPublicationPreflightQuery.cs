// ABOUTME: Requests paid-ticket publication readiness for one event catalog draft.
// ABOUTME: Authorizes against ticket management so blockers can explain paid-commerce denials safely.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTicketing;
using MediatR;

namespace Explore.Application.Features.EventTicketing.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTickets)]
public sealed record GetPaidEventPublicationPreflightQuery(Guid EventId)
    : IRequest<PaidEventPublicationPreflightDto>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}

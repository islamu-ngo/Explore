// ABOUTME: Secured MediatR query for reading event moderation audit history.
// ABOUTME: Uses event view-management authorization so only management-capable principals can inspect moderation records.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed record GetEventModerationHistoryRequest : IRequest<IReadOnlyList<EventModerationHistoryDto>?>, ISecureRequest
{
    public Guid Id { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
}

// ABOUTME: Public CQRS query for the ordered external actions attached to one event.
// ABOUTME: Returns normalized lookup metadata without local capability booleans.

using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.EventPublicActions.Requests.Queries;

public sealed record GetEventPublicActionsRequest(Guid EventId) : IRequest<IReadOnlyList<EventPublicActionDto>>;

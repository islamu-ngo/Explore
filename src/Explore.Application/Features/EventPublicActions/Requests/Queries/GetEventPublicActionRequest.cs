// ABOUTME: Public CQRS query for one reviewed action addressed by stored identifiers.
// ABOUTME: Supports safe detail and redirect routes without accepting caller-provided destinations.

using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.EventPublicActions.Requests.Queries;

public sealed record GetEventPublicActionRequest(Guid EventId, Guid ActionId) : IRequest<EventPublicActionDto?>;

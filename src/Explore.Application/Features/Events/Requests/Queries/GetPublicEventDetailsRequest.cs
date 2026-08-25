// ABOUTME: MediatR query request for resolving public event detail from a slug-code URL token.
// ABOUTME: Keeps clean public URLs separate from GUID-based management and API routes.

using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record GetPublicEventDetailsRequest : IRequest<EventDto?>
{
    public required string SlugCode { get; init; }
}

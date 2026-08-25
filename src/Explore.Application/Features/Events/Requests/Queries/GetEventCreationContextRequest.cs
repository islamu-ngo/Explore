// ABOUTME: MediatR query for resolving event creation policy and publisher choices.
// ABOUTME: Gives clients a server-owned context before creating or publishing an event.

using Explore.Application.DTOs.Event;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Queries;

public sealed record GetEventCreationContextRequest : IRequest<EventCreationContextDto>
{
}

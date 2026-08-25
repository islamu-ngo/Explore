// ABOUTME: MediatR command for deleting (soft-deleting) an event series by ID.
// ABOUTME: Carries the series ID to DeleteEventSeriesCommandHandler.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Requests.Commands;

public sealed record DeleteEventSeriesCommand(Guid Id = default) : IRequest<BaseCommandResponse<bool>>;

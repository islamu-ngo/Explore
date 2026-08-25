// ABOUTME: MediatR query request for fetching all event formats.
// ABOUTME: Returns IEnumerable<EventFormatDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.EventFormat;
using MediatR;

namespace Explore.Application.Features.EventFormats.Requests.Queries;

public sealed record GetEventFormatListRequest : IRequest<List<EventFormatListDto>>
{
}

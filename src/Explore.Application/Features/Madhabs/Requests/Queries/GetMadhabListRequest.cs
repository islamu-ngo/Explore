// ABOUTME: MediatR query request for fetching all madhabs.
// ABOUTME: Returns IEnumerable<MadhabDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.Madhab;
using MediatR;

namespace Explore.Application.Features.Madhabs.Requests.Queries;

public sealed record GetMadhabListRequest : IRequest<List<MadhabListDto>>
{
}

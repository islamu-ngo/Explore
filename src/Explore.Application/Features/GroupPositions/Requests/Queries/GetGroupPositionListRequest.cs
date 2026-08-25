// ABOUTME: MediatR query request for retrieving all group positions.
// ABOUTME: Read-only lookup — no parameters needed.

using System.Collections.Generic;
using Explore.Application.DTOs.GroupPosition;
using MediatR;

namespace Explore.Application.Features.GroupPositions.Requests.Queries;

public sealed record GetGroupPositionListRequest : IRequest<List<GroupPositionListDto>>
{
}

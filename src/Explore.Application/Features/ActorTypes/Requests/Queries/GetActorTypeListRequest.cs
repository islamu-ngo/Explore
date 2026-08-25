// ABOUTME: MediatR query request for fetching the full list of actor types.
// ABOUTME: Returns IEnumerable<ActorTypeDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.ActorType;
using MediatR;

namespace Explore.Application.Features.ActorTypes.Requests.Queries;

public sealed record GetActorTypeListRequest : IRequest<List<ActorTypeListDto>>
{
}

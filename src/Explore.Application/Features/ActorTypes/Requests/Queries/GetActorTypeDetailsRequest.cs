// ABOUTME: MediatR query request for fetching a single actor type by ID.
// ABOUTME: Returns ActorTypeDto.
using Explore.Application.DTOs.ActorType;
using MediatR;

namespace Explore.Application.Features.ActorTypes.Requests.Queries;

public class GetActorTypeDetailsRequest : IRequest<ActorTypeDto>
{
    public int Id { get; set; }
}

// ABOUTME: MediatR query request for fetching a single actor type by ID.
// ABOUTME: Returns ActorTypeDto.
using Explore.Application.DTOs.ActorType;
using MediatR;

namespace Explore.Application.Features.ActorTypes.Requests.Queries;

public sealed record GetActorTypeDetailsRequest(int Id = default) : IRequest<ActorTypeDto>;

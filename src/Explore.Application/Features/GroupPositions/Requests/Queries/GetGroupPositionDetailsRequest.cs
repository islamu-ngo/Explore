// ABOUTME: MediatR query request for retrieving a single group position by ID.
// ABOUTME: Used by lookup detail endpoint.

using Explore.Application.DTOs.GroupPosition;
using MediatR;

namespace Explore.Application.Features.GroupPositions.Requests.Queries;

public sealed record GetGroupPositionDetailsRequest(int Id = default) : IRequest<GroupPositionDto>;

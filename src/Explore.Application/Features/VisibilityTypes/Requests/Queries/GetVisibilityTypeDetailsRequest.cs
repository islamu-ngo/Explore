// ABOUTME: MediatR query request for fetching a single visibility type by ID.
// ABOUTME: Returns VisibilityTypeDto.
using Explore.Application.DTOs.VisibilityType;
using MediatR;

namespace Explore.Application.Features.VisibilityTypes.Requests.Queries;

public sealed record GetVisibilityTypeDetailsRequest(int Id = default) : IRequest<VisibilityTypeDto>;

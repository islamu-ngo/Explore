// ABOUTME: MediatR query request for fetching a single audience age category by ID.
// ABOUTME: Returns AudienceAgeDto.
using Explore.Application.DTOs.AudienceAge;
using MediatR;

namespace Explore.Application.Features.AudienceAges.Requests.Queries;

public sealed record GetAudienceAgeDetailsRequest(int Id = default) : IRequest<AudienceAgeDto>;

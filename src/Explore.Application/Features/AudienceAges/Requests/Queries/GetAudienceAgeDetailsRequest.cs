// ABOUTME: MediatR query request for fetching a single audience age category by ID.
// ABOUTME: Returns AudienceAgeDto.
using Explore.Application.DTOs.AudienceAge;
using MediatR;

namespace Explore.Application.Features.AudienceAges.Requests.Queries;

public class GetAudienceAgeDetailsRequest : IRequest<AudienceAgeDto>
{
    public int Id { get; set; }
}

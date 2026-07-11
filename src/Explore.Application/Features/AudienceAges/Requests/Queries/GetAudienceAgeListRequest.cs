// ABOUTME: MediatR query request for fetching the full list of audience age categories.
// ABOUTME: Returns IEnumerable<AudienceAgeDto>.
using Explore.Application.DTOs.AudienceAge;
using MediatR;

namespace Explore.Application.Features.AudienceAges.Requests.Queries;

public class GetAudienceAgeListRequest : IRequest<List<AudienceAgeListDto>>
{
}

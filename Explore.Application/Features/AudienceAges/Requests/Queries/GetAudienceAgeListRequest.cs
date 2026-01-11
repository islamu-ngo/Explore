using Explore.Application.DTOs.AudienceAge;
using MediatR;

namespace Explore.Application.Features.AudienceAges.Requests.Queries
{
    public class GetAudienceAgeListRequest : IRequest<List<AudienceAgeListDto>>
    {
    }
}

using Explore.Application.DTOs.AudienceAge;
using MediatR;

namespace Explore.Application.Features.AudienceAges.Requests.Queries
{
    public class GetAudienceAgeDetailsRequest : IRequest<AudienceAgeDto>
    {
        public int Id { get; set; }
    }
}

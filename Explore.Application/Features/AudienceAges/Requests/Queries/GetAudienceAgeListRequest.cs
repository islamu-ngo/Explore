using Explore.Application.DTOs.AudienceAge;
using MediatR;

namespace Explore.Application.Features.AudienceAges.Requests.Queries
{
    public class GetAudienceAgeListRequest : IRequest<List<AudienceAgeListDto>>
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
    }
}

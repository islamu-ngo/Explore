using Explore.Application.DTOs.Madhab;
using MediatR;

namespace Explore.Application.Features.Madhabs.Requests.Queries
{
    public class GetMadhabDetailsRequest : IRequest<MadhabDto>
    {
        public int Id { get; set; }
    }
}

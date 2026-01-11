using Explore.Application.DTOs.OrganizationPosition;
using MediatR;

namespace Explore.Application.Features.OrganizationPositions.Requests.Queries
{
    public class GetOrganizationPositionDetailsRequest : IRequest<OrganizationPositionDto>
    {
        public int Id { get; set; }
    }
}

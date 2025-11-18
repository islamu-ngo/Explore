using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Queries
{
    public class GetMyOrganizationsRequest : IRequest<List<OrganizationListDto>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}

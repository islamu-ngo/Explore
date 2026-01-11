using MediatR;
using Explore.Application.DTOs.Tenant;

namespace Explore.Application.Features.Tenants.Requests.Queries
{
    public class GetTenantDetailsRequest : IRequest<TenantDto>
    {
        public Guid Id { get; set; }
    }
}

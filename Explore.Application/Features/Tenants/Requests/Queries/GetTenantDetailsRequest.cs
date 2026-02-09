using Explore.Application.DTOs.Tenant;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Queries;

public class GetTenantDetailsRequest : IRequest<TenantDto>
{
    public Guid Id { get; set; }
}

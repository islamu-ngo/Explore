using MediatR;
using Explore.Application.DTOs.Tenant;
using System.Collections.Generic;

namespace Explore.Application.Features.Tenants.Requests.Queries
{
    public class GetTenantListRequest : IRequest<List<TenantListDto>>
    {
    }
}

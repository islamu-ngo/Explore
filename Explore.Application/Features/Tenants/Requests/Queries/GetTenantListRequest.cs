// ABOUTME: MediatR query request for fetching a paginated tenant list.
// ABOUTME: Returns IEnumerable<TenantListDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.Tenant;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Queries;

public class GetTenantListRequest : IRequest<List<TenantListDto>>
{
}

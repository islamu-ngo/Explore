using MediatR;
using Explore.Application.DTOs.TenantUser;
using System.Collections.Generic;

namespace Explore.Application.Features.TenantUsers.Requests.Queries
{
    public class GetTenantUserListRequest : IRequest<List<TenantUserListDto>>
    {
    }
}

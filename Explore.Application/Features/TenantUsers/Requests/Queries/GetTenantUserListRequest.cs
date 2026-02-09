using System.Collections.Generic;
using Explore.Application.DTOs.TenantUser;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Queries;

public class GetTenantUserListRequest : IRequest<List<TenantUserListDto>>
{
}

using Explore.Application.DTOs.UserRole;
using MediatR;

namespace Explore.Application.Features.UserRoles.Requests.Queries
{
    public class GetUserRoleListRequest : IRequest<List<UserRoleListDto>>
    {
    }
}

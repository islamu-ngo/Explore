using Explore.Application.DTOs.UserRole;
using MediatR;

namespace Explore.Application.Features.UserRoles.Requests.Queries
{
    public class GetUserRoleDetailsRequest : IRequest<UserRoleDto>
    {
        public int Id { get; set; }
    }
}

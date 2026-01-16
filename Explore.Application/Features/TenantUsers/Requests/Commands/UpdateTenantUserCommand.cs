using MediatR;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Responses;

namespace Explore.Application.Features.TenantUsers.Requests.Commands
{
    public class UpdateTenantUserCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateTenantUserDto TenantUserDto { get; set; }
    }
}

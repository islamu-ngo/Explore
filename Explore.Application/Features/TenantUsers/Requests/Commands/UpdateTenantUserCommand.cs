using Explore.Application.DTOs.TenantUser;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Commands;

public class UpdateTenantUserCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateTenantUserDto TenantUserDto { get; set; }
}

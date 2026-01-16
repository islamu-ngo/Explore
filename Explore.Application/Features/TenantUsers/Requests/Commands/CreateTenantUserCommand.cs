using MediatR;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Responses;
using System;

namespace Explore.Application.Features.TenantUsers.Requests.Commands
{
    public class CreateTenantUserCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateTenantUserDto TenantUserDto { get; set; }
    }
}

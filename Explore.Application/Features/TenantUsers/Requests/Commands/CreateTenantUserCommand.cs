using System;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Commands;

public class CreateTenantUserCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateTenantUserDto TenantUserDto { get; set; }
}

using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands;

public class CreateTenantCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateTenantDto TenantDto { get; set; }
}

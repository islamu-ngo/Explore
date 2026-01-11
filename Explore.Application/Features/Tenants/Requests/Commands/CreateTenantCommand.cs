using MediatR;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;

namespace Explore.Application.Features.Tenants.Requests.Commands
{
    public class CreateTenantCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateTenantDto TenantDto { get; set; }
    }
}

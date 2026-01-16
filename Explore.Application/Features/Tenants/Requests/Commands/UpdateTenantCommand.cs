using MediatR;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;

namespace Explore.Application.Features.Tenants.Requests.Commands
{
    public class UpdateTenantCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateTenantDto TenantDto { get; set; }
    }
}

using MediatR;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Responses;

namespace Explore.Application.Features.TenantSettings.Requests.Commands
{
    public class UpdateTenantSettingsCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateTenantSettingsDto TenantSettingsDto { get; set; }
    }
}

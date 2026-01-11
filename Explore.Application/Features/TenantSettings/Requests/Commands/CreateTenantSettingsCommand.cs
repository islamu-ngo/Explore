using MediatR;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Responses;

namespace Explore.Application.Features.TenantSettings.Requests.Commands
{
    public class CreateTenantSettingsCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateTenantSettingsDto TenantSettingsDto { get; set; }
    }
}

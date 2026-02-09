using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Commands;

public class UpdateTenantSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateTenantSettingsDto TenantSettingsDto { get; set; }
}

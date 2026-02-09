using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Commands;

public class CreateTenantSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateTenantSettingsDto TenantSettingsDto { get; set; }
}

// ABOUTME: Handler for deleting a tenant settings record.
// ABOUTME: Fetches record by ID and delegates deletion.
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Features.TenantSettings.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Handlers.Commands;

public class DeleteTenantSettingsCommandHandler : IRequestHandler<DeleteTenantSettingsCommand, bool>
{
    private readonly ITenantSettingsRepository _tenantSettingsRepository;

    public DeleteTenantSettingsCommandHandler(ITenantSettingsRepository tenantSettingsRepository)
    {
        _tenantSettingsRepository = tenantSettingsRepository;
    }

    public async Task<bool> Handle(DeleteTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenantSettings = await _tenantSettingsRepository.GetById(request.Id);
        if (tenantSettings == null)
        {
            return false;
        }

        await _tenantSettingsRepository.Delete(tenantSettings);
        return true;
    }
}

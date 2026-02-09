using System;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.Tenants.Handlers.Commands;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, bool>
{
    private readonly ITenantRepository _tenantRepository;

    public DeleteTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<bool> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetById(request.Id);
        if (tenant == null)
        {
            return false;
        }

        await _tenantRepository.Delete(tenant);
        return true;
    }
}

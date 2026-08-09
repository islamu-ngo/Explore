// ABOUTME: Applies validated grouped metadata updates to one route-identified tenant.
// ABOUTME: Tenant lifecycle state remains owned by dedicated control-plane transition commands.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Handlers.Commands;

public sealed class UpdateTenantCommandHandler(
    ITenantRepository tenantRepository,
    ITenantSlugCache tenantSlugCache)
    : IRequestHandler<UpdateTenantCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateTenantDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Update, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Tenant update failed.",
                Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList()
            };
        }

        var tenant = await tenantRepository.GetById(request.TenantId);
        if (tenant is null)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Tenant not found.",
                FailureCode = FailureCodes.NotFound
            };
        }

        if (request.Update.FullName is not null)
            tenant.FullName = request.Update.FullName.Value.Trim();

        bool slugChanged = request.Update.Slug is not null
            && !string.Equals(tenant.Slug, request.Update.Slug.Value.Trim(), StringComparison.OrdinalIgnoreCase);
        if (request.Update.Slug is not null)
            tenant.Slug = request.Update.Slug.Value.Trim();

        await tenantRepository.Update(tenant);
        if (slugChanged)
            await tenantSlugCache.RefreshAsync(cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = tenant.Id,
            Message = "Tenant updated successfully."
        };
    }
}

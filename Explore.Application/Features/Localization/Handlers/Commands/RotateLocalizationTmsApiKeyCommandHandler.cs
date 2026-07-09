// ABOUTME: Rotates the tenant-scoped localization TMS API key through SecretBinding inline encryption.
// ABOUTME: Enforces admin authority and never logs or returns the plaintext API key.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.DTOs.Localization.Validators;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Localization.Handlers.Commands;

public sealed class RotateLocalizationTmsApiKeyCommandHandler(
    IAdminContext adminContext,
    ITenantContext tenantContext,
    ISecretBindingRepository secretBindingRepository,
    IInlineSecretProtector secretProtector,
    ISecretResolver secretResolver,
    ILogger<RotateLocalizationTmsApiKeyCommandHandler> logger)
    : IRequestHandler<RotateLocalizationTmsApiKeyCommand, BaseCommandResponse<Guid>>
{
    private const string SettingKey = SecretDefinitionRegistry.Keys.Localization.TmsApiKey;

    public async Task<BaseCommandResponse<Guid>> Handle(
        RotateLocalizationTmsApiKeyCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var actor = await adminContext.ResolveUserIdAsync(cancellationToken);
        if (!actor.HasValue)
        {
            response.Success = false;
            response.Message = "Authentication is required to rotate the localization TMS API key.";
            return response;
        }

        var tenantId = tenantContext.TenantId;
        var isAuthorized = await adminContext.IsInstanceAdminAsync(actor.Value, cancellationToken)
            || await adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        if (!isAuthorized)
        {
            response.Success = false;
            response.Message = "Tenant or instance administrator authority is required to rotate the localization TMS API key.";
            return response;
        }

        var validator = new RotateLocalizationTmsApiKeyDtoValidator();
        var validation = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Localization TMS API key rotation failed.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var instanceBinding = await secretBindingRepository.GetByKeyAndScopeAsync(
            SettingKey,
            SecretScope.Instance,
            null,
            cancellationToken);
        if (instanceBinding?.IsLocked == true)
        {
            response.Success = false;
            response.Message = "Localization TMS API key is locked by instance policy.";
            return response;
        }

        var protectedSecret = secretProtector.Protect(request.Dto.TmsApiKey!.Trim());
        var tenantBinding = await secretBindingRepository.GetByKeyAndScopeAsync(
            SettingKey,
            SecretScope.Tenant,
            tenantId,
            cancellationToken);

        if (tenantBinding is null)
        {
            tenantBinding = SecretBinding.CreateInlineEncrypted(
                SettingKey,
                SecretScope.Tenant,
                tenantId,
                protectedSecret.Ciphertext,
                protectedSecret.Version);
            tenantBinding.CreatedAt = DateTime.UtcNow;
            tenantBinding.CreatedBy = actor.Value;
            tenantBinding.UpdatedBy = actor.Value;
            await secretBindingRepository.Create(tenantBinding);
        }
        else
        {
            tenantBinding.SwitchToInlineEncrypted(protectedSecret.Ciphertext, protectedSecret.Version);
            tenantBinding.UpdatedBy = actor.Value;
            tenantBinding.UpdatedAt = DateTime.UtcNow;
            await secretBindingRepository.Update(tenantBinding);
        }

        await secretResolver.InvalidateAsync(SettingKey, SecretScope.Tenant, tenantId, cancellationToken);

        logger.LogInformation(
            "Localization TMS API key rotated for tenant {TenantId} by {Actor}.",
            tenantId,
            actor.Value);

        response.Success = true;
        response.Id = tenantId;
        response.Message = "Localization TMS API key rotated successfully.";
        return response;
    }
}

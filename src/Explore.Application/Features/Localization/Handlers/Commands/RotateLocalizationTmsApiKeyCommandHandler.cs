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
        var actor = await adminContext.ResolveUserIdAsync(cancellationToken);
        if (!actor.HasValue)
        {
            const string message = "Authentication is required to rotate the localization TMS API key.";
            return BaseCommandResponse.Validation<Guid>([message], message);
        }

        var tenantId = tenantContext.TenantId;
        var isAuthorized = await adminContext.IsInstanceAdminAsync(actor.Value, cancellationToken)
            || await adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        if (!isAuthorized)
        {
            const string message = "Tenant or instance administrator authority is required to rotate the localization TMS API key.";
            return BaseCommandResponse.Validation<Guid>([message], message);
        }

        var validator = new RotateLocalizationTmsApiKeyDtoValidator();
        var validation = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validation.IsValid)
        {
            return BaseCommandResponse.Validation<Guid>(
                validation.Errors.Select(e => e.ErrorMessage),
                "Localization TMS API key rotation failed.");
        }

        var instanceBinding = await secretBindingRepository.GetByKeyAndScopeAsync(
            SettingKey,
            SecretScope.Instance,
            null,
            cancellationToken);
        if (instanceBinding?.IsLocked == true)
        {
            const string message = "Localization TMS API key is locked by instance policy.";
            return BaseCommandResponse.Validation<Guid>([message], message);
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
                protectedSecret.Ciphertext.ToArray(),
                protectedSecret.Version);
            tenantBinding.CreatedAt = DateTime.UtcNow;
            tenantBinding.CreatedBy = actor.Value;
            tenantBinding.UpdatedBy = actor.Value;
            await secretBindingRepository.Create(tenantBinding);
        }
        else
        {
            tenantBinding.SwitchToInlineEncrypted(protectedSecret.Ciphertext.ToArray(), protectedSecret.Version);
            tenantBinding.UpdatedBy = actor.Value;
            tenantBinding.UpdatedAt = DateTime.UtcNow;
            await secretBindingRepository.Update(tenantBinding);
        }

        await secretResolver.InvalidateAsync(SettingKey, SecretScope.Tenant, tenantId, cancellationToken);

        logger.LogInformation(
            "Localization TMS API key rotated for tenant {TenantId} by {Actor}.",
            tenantId,
            actor.Value);

        return BaseCommandResponse.Success(tenantId, "Localization TMS API key rotated successfully.");
    }
}

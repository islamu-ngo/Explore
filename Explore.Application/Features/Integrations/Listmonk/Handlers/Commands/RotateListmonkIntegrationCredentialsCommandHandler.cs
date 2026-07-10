// ABOUTME: Rotates Listmonk Basic Auth credentials through tenant-scoped encrypted secret bindings.
// ABOUTME: Enforces admin authority and avoids logging or returning plaintext credential values.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.DTOs.Integrations;
using Explore.Application.DTOs.Integrations.Validators;
using Explore.Application.Features.Integrations.Listmonk.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Integrations.Listmonk.Handlers.Commands;

public sealed class RotateListmonkIntegrationCredentialsCommandHandler(
    IAdminContext adminContext,
    ITenantContext tenantContext,
    ISecretBindingRepository secretBindingRepository,
    IInlineSecretProtector secretProtector,
    ISecretResolver secretResolver,
    ILogger<RotateListmonkIntegrationCredentialsCommandHandler> logger)
    : IRequestHandler<RotateListmonkIntegrationCredentialsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        RotateListmonkIntegrationCredentialsCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var actor = await adminContext.ResolveUserIdAsync(cancellationToken);
        if (!actor.HasValue)
        {
            response.Success = false;
            response.Message = "Authentication is required to rotate Listmonk credentials.";
            return response;
        }

        var tenantId = tenantContext.TenantId;
        var isAuthorized = await adminContext.IsInstanceAdminAsync(actor.Value, cancellationToken)
            || await adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        if (!isAuthorized)
        {
            response.Success = false;
            response.Message = "Tenant or instance administrator authority is required to rotate Listmonk credentials.";
            return response;
        }

        var validator = new RotateListmonkIntegrationCredentialsDtoValidator();
        var validation = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Listmonk credential rotation failed.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var rotations = PendingRotations(request.Dto).ToArray();
        foreach (var (settingKey, _) in rotations)
        {
            var instanceBinding = await secretBindingRepository.GetByKeyAndScopeAsync(
                settingKey,
                SecretScope.Instance,
                null,
                cancellationToken);
            if (instanceBinding?.IsLocked == true)
            {
                response.Success = false;
                response.Message = "Listmonk credentials are locked by instance policy.";
                return response;
            }
        }

        foreach (var (settingKey, plaintext) in rotations)
        {
            await RotateCredentialAsync(settingKey, plaintext, tenantId, actor.Value, cancellationToken);
        }

        logger.LogInformation(
            "Listmonk credentials rotated for tenant {TenantId} by {Actor}. Updated keys: {CredentialCount}.",
            tenantId,
            actor.Value,
            rotations.Length);

        response.Success = true;
        response.Id = tenantId;
        response.Message = "Listmonk credentials rotated successfully.";
        return response;
    }

    private async Task RotateCredentialAsync(
        string settingKey,
        string plaintext,
        Guid tenantId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var protectedSecret = secretProtector.Protect(plaintext.Trim());
        var tenantBinding = await secretBindingRepository.GetByKeyAndScopeAsync(
            settingKey,
            SecretScope.Tenant,
            tenantId,
            cancellationToken);

        if (tenantBinding is null)
        {
            tenantBinding = SecretBinding.CreateInlineEncrypted(
                settingKey,
                SecretScope.Tenant,
                tenantId,
                protectedSecret.Ciphertext,
                protectedSecret.Version);
            tenantBinding.CreatedAt = DateTime.UtcNow;
            tenantBinding.CreatedBy = actorId;
            tenantBinding.UpdatedBy = actorId;
            await secretBindingRepository.Create(tenantBinding);
        }
        else
        {
            tenantBinding.SwitchToInlineEncrypted(protectedSecret.Ciphertext, protectedSecret.Version);
            tenantBinding.UpdatedBy = actorId;
            tenantBinding.UpdatedAt = DateTime.UtcNow;
            await secretBindingRepository.Update(tenantBinding);
        }

        await secretResolver.InvalidateAsync(settingKey, SecretScope.Tenant, tenantId, cancellationToken);
    }

    private static IEnumerable<(string SettingKey, string Plaintext)> PendingRotations(
        RotateListmonkIntegrationCredentialsDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.ApiUsername))
            yield return (SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiUsername, dto.ApiUsername);

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            yield return (SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiKey, dto.ApiKey);
    }
}

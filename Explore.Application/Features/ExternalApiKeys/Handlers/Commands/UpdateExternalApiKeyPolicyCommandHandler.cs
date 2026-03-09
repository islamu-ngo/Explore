// ABOUTME: Updates editable policy fields for persisted external API keys visible to the current caller.
// ABOUTME: Reuses existing ownership and organization permission checks while hiding unauthorized keys.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey.Validators;
using Explore.Application.Features.ExternalApiKeys.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.ExternalApiKeys.Handlers.Commands;

public class UpdateExternalApiKeyPolicyCommandHandler : IRequestHandler<UpdateExternalApiKeyPolicyCommand, BaseCommandResponse<Guid>>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IUserContext _userContext;
    private readonly ILogger<UpdateExternalApiKeyPolicyCommandHandler> _logger;

    public UpdateExternalApiKeyPolicyCommandHandler(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IUserContext userContext,
        ILogger<UpdateExternalApiKeyPolicyCommandHandler> logger)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateExternalApiKeyPolicyCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var currentUserId = _userContext.GetRequiredUserId();
        var externalApiKey = await _externalApiKeyRepository.GetById(request.ExternalApiKeyPolicyDto.Id);

        if (externalApiKey is null || !await CanManageAsync(externalApiKey, currentUserId, cancellationToken))
        {
            response.Success = false;
            response.Message = "External API key not found.";
            return response;
        }

        var validator = new UpdateExternalApiKeyPolicyDtoValidator(_externalApiKeyRepository, externalApiKey);
        var validationResult = await validator.ValidateAsync(request.ExternalApiKeyPolicyDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "External API key update failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        externalApiKey.Name = request.ExternalApiKeyPolicyDto.Name.Trim();
        externalApiKey.Scopes = NormalizeScopes(request.ExternalApiKeyPolicyDto.Scopes);
        externalApiKey.ExpiresAt = request.ExternalApiKeyPolicyDto.ExpiresAt;
        externalApiKey.UpdatedAt = DateTime.UtcNow;
        externalApiKey.UpdatedBy = currentUserId;

        await _externalApiKeyRepository.Update(externalApiKey);

        _logger.LogInformation(
            "External API key {KeyId} policy updated for tenant {TenantId} by user {UserId}.",
            externalApiKey.KeyId,
            externalApiKey.TenantId,
            currentUserId);

        response.Success = true;
        response.Id = externalApiKey.Id;
        response.Message = "External API key updated successfully.";
        return response;
    }

    private async Task<bool> CanManageAsync(Explore.Domain.ExternalApiKey externalApiKey, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (externalApiKey.OwnerType == ExternalApiKeyOwnerType.User)
        {
            return externalApiKey.OwnerId == currentUserId;
        }

        if (externalApiKey.OwnerType == ExternalApiKeyOwnerType.Organization)
        {
            return await _organizationMemberRepository.HasPermissionInOrganization(
                externalApiKey.OwnerId,
                currentUserId,
                PermissionCodes.OrganizationManage);
        }

        return false;
    }

    private static string NormalizeScopes(IEnumerable<string> scopes)
    {
        return string.Join(' ', scopes
            .Select(scope => scope.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(scope => scope, StringComparer.OrdinalIgnoreCase));
    }
}

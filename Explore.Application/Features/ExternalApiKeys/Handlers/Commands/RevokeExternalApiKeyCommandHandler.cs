// ABOUTME: Revokes persisted external API keys visible to the current caller.
// ABOUTME: Reuses existing organization permission checks and hides unauthorized keys behind a false result.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ExternalApiKeys.Requests.Commands;
using Explore.Application.Telemetry;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.ExternalApiKeys.Handlers.Commands;

public class RevokeExternalApiKeyCommandHandler : IRequestHandler<RevokeExternalApiKeyCommand, bool>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IUserContext _userContext;
    private readonly BusinessMetrics _metrics;
    private readonly ILogger<RevokeExternalApiKeyCommandHandler> _logger;

    public RevokeExternalApiKeyCommandHandler(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IUserContext userContext,
        BusinessMetrics metrics,
        ILogger<RevokeExternalApiKeyCommandHandler> logger)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _userContext = userContext;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<bool> Handle(RevokeExternalApiKeyCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _userContext.GetRequiredUserId();
        var externalApiKey = await _externalApiKeyRepository.GetById(request.Id);

        if (externalApiKey is null)
        {
            return false;
        }

        if (externalApiKey.OwnerType == ExternalApiKeyOwnerType.User && externalApiKey.OwnerId != currentUserId)
        {
            return false;
        }

        if (externalApiKey.OwnerType == ExternalApiKeyOwnerType.Organization)
        {
            var hasPermission = await _organizationMemberRepository.HasPermissionInOrganization(
                externalApiKey.OwnerId,
                currentUserId,
                Explore.Domain.Constants.PermissionCodes.OrganizationManage);

            if (!hasPermission)
            {
                return false;
            }
        }

        if (externalApiKey.Status == ExternalApiKeyStatus.Revoked)
        {
            return true;
        }

        externalApiKey.Status = ExternalApiKeyStatus.Revoked;
        externalApiKey.UpdatedAt = DateTime.UtcNow;
        externalApiKey.UpdatedBy = currentUserId;
        await _externalApiKeyRepository.Update(externalApiKey);

        _metrics.RecordExternalApiKeyRevoked(
            externalApiKey.TenantId.ToString(),
            externalApiKey.OwnerType.ToString());

        _logger.LogInformation(
            "External API key {KeyId} revoked for tenant {TenantId} by user {UserId}.",
            externalApiKey.KeyId,
            externalApiKey.TenantId,
            currentUserId);

        return true;
    }
}

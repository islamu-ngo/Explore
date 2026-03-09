// ABOUTME: Fetches a single external API key visible to the current caller.
// ABOUTME: Reuses existing owner and organization permission checks while exposing only safe metadata.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Features.ExternalApiKeys.Requests.Queries;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Handlers.Queries;

public class GetExternalApiKeyDetailsRequestHandler : IRequestHandler<GetExternalApiKeyDetailsRequest, ExternalApiKeyListDto?>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IUserContext _userContext;

    public GetExternalApiKeyDetailsRequestHandler(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IUserContext userContext)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _userContext = userContext;
    }

    public async Task<ExternalApiKeyListDto?> Handle(GetExternalApiKeyDetailsRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _userContext.GetRequiredUserId();
        var externalApiKey = await _externalApiKeyRepository.GetById(request.Id);

        if (externalApiKey is null || !await CanManageAsync(externalApiKey, currentUserId, cancellationToken))
        {
            return null;
        }

        return new ExternalApiKeyListDto
        {
            Id = externalApiKey.Id,
            Name = externalApiKey.Name,
            KeyId = externalApiKey.KeyId,
            OwnerType = externalApiKey.OwnerType,
            OwnerId = externalApiKey.OwnerId,
            Scopes = SplitScopes(externalApiKey.Scopes),
            Status = externalApiKey.Status,
            ExpiresAt = externalApiKey.ExpiresAt,
            LastUsedAt = externalApiKey.LastUsedAt
        };
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

    private static List<string> SplitScopes(string scopes)
    {
        return scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}

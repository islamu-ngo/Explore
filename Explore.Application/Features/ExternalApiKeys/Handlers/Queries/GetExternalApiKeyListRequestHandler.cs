// ABOUTME: Lists external API keys that the current user is allowed to manage.
// ABOUTME: Combines personal keys with organization-owned keys where the caller has organization-manage permission.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Features.ExternalApiKeys.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Handlers.Queries;

public class GetExternalApiKeyListRequestHandler : IRequestHandler<GetExternalApiKeyListRequest, List<ExternalApiKeyListDto>>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IUserContext _userContext;

    public GetExternalApiKeyListRequestHandler(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IUserContext userContext)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _userContext = userContext;
    }

    public async Task<List<ExternalApiKeyListDto>> Handle(GetExternalApiKeyListRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _userContext.GetRequiredUserId();
        var visibleKeys = new List<Explore.Domain.ExternalApiKey>();

        visibleKeys.AddRange(await _externalApiKeyRepository.GetByOwner(ExternalApiKeyOwnerType.User, currentUserId));

        var organizationIds = await _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
            currentUserId,
            Explore.Domain.Constants.PermissionCodes.OrganizationManage);

        if (organizationIds.Count > 0)
        {
            visibleKeys.AddRange(await _externalApiKeyRepository.GetByOwners(ExternalApiKeyOwnerType.Organization, organizationIds));
        }

        return visibleKeys
            .OrderByDescending(key => key.CreatedAt)
            .Select(key => new ExternalApiKeyListDto
            {
                Id = key.Id,
                Name = key.Name,
                KeyId = key.KeyId,
                OwnerType = key.OwnerType,
                OwnerId = key.OwnerId,
                Scopes = SplitScopes(key.Scopes),
                Status = key.Status,
                ExpiresAt = key.ExpiresAt,
                LastUsedAt = key.LastUsedAt
            })
            .ToList();
    }

    private static List<string> SplitScopes(string scopes)
    {
        return scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}

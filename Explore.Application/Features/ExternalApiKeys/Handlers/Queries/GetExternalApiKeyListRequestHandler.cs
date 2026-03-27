// ABOUTME: Lists external API keys that the current user is allowed to manage.
// ABOUTME: Aggregates personal, organization, group, tenant, and instance-admin keys based on caller authority.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Features.ExternalApiKeys.Requests.Queries;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Handlers.Queries;

public class GetExternalApiKeyListRequestHandler : IRequestHandler<GetExternalApiKeyListRequest, List<ExternalApiKeyListDto>>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IAdminContext _adminContext;
    private readonly IUserContext _userContext;

    public GetExternalApiKeyListRequestHandler(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository,
        IAdminContext adminContext,
        IUserContext userContext)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _groupMemberRepository = groupMemberRepository;
        _adminContext = adminContext;
        _userContext = userContext;
    }

    public async Task<List<ExternalApiKeyListDto>> Handle(GetExternalApiKeyListRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _userContext.GetRequiredUserId();
        var visibleKeys = new List<Explore.Domain.ExternalApiKey>();

        visibleKeys.AddRange(await _externalApiKeyRepository.GetByOwner(ExternalApiKeyOwnerType.User, currentUserId));

        var organizationIds = await _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
            currentUserId,
            PermissionCodes.OrganizationManage);

        if (organizationIds.Count > 0)
        {
            visibleKeys.AddRange(await _externalApiKeyRepository.GetByOwners(ExternalApiKeyOwnerType.Organization, organizationIds));
        }

        var groupIds = await _groupMemberRepository.GetGroupIdsWhereUserHasPermission(
            currentUserId,
            PermissionCodes.GroupManage);

        if (groupIds.Count > 0)
        {
            visibleKeys.AddRange(await _externalApiKeyRepository.GetByOwners(ExternalApiKeyOwnerType.Group, groupIds));
        }

        var tenantIds = await _adminContext.GetAdminTenantIdsAsync(cancellationToken);

        if (tenantIds.Count > 0)
        {
            visibleKeys.AddRange(await _externalApiKeyRepository.GetByOwners(ExternalApiKeyOwnerType.Tenant, tenantIds));
        }

        if (await _adminContext.IsInstanceAdminAsync(cancellationToken))
        {
            visibleKeys.AddRange(await _externalApiKeyRepository.GetByOwnerIgnoringTenantFilter(
                ExternalApiKeyOwnerType.InstanceAdmin, currentUserId));
        }

        return visibleKeys
            .OrderByDescending(key => key.CreatedAt)
            .Select(key => new ExternalApiKeyListDto
            {
                Id = key.Id,
                Name = key.Name,
                Description = key.Description,
                KeyId = key.KeyId,
                MaskedKeyId = MaskKeyId(key.KeyId),
                OwnerType = key.OwnerType,
                OwnerId = key.OwnerId,
                Scopes = SplitScopes(key.Scopes),
                Status = (ExternalApiKeyStatusEnum)key.ExternalApiKeyStatusId,
                StatusName = ((ExternalApiKeyStatusEnum)key.ExternalApiKeyStatusId).ToString(),
                ExpiresAt = key.ExpiresAt,
                LastUsedAt = key.LastUsedAt,
                CreditPeriod = (ExternalApiKeyCreditPeriodEnum)key.ExternalApiKeyCreditPeriodId,
                CreditLimit = key.CreditLimit,
                MaxRolloverCredits = key.MaxRolloverCredits
            })
            .ToList();
    }

    private static List<string> SplitScopes(string scopes)
    {
        return scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string MaskKeyId(string keyId)
    {
        return keyId.Length > 4
            ? string.Concat("".PadLeft(keyId.Length - 4, '\u2022'), keyId.AsSpan(keyId.Length - 4))
            : keyId;
    }
}

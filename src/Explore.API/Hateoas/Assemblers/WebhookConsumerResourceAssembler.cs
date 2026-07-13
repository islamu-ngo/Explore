// ABOUTME: HAL resource assembler for webhook consumer management rows.
// ABOUTME: Lets clients discover webhook management affordances through API-owned link policies.

namespace Explore.API.Hateoas.Assemblers;

using System.Security.Claims;
using Explore.API.Hateoas.Policies;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Hateoas;
using Explore.Domain;
using Microsoft.AspNetCore.Http;

public sealed class WebhookConsumerResourceAssembler : ResourceAssemblerBase<WebhookConsumerDto, WebhookConsumerDto>
{
    private readonly IWebhookProviderPortalEligibilityService _portalEligibilityService;

    public WebhookConsumerResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<WebhookConsumerDto> detailLinkPolicy,
        ICollectionLinkPolicy<WebhookConsumerDto> collectionLinkPolicy,
        IWebhookProviderPortalEligibilityService portalEligibilityService)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
        _portalEligibilityService = portalEligibilityService;
    }

    protected override async Task<IReadOnlyList<LinkDefinition>> GetDetailLinkDefinitionsAsync(
        WebhookConsumerDto dto,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        var definitions = await base.GetDetailLinkDefinitionsAsync(dto, user, httpContext);
        return await AddPortalLinkWhenEligibleAsync(dto, definitions, httpContext.RequestAborted);
    }

    protected override async Task<IReadOnlyList<LinkDefinition>> GetListItemLinkDefinitionsAsync(
        WebhookConsumerDto dto,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        var definitions = await base.GetListItemLinkDefinitionsAsync(dto, user, httpContext);
        return await AddPortalLinkWhenEligibleAsync(dto, definitions, httpContext.RequestAborted);
    }

    protected override async Task<IReadOnlyList<IReadOnlyList<LinkDefinition>>> GetCollectionItemLinkDefinitionsAsync(
        IReadOnlyList<WebhookConsumerDto> items,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        var definitionsByItem = await base.GetCollectionItemLinkDefinitionsAsync(items, user, httpContext);
        if (items.Count == 0)
        {
            return definitionsByItem;
        }

        var tenantIds = items.Select(item => item.TenantId).Distinct().ToArray();
        if (tenantIds.Length != 1 || tenantIds[0] == Guid.Empty)
        {
            return definitionsByItem;
        }

        var eligibleCandidates = items
            .Where(IsConsumerPortalCandidate)
            .Select(item => item.Id)
            .Distinct()
            .ToArray();
        var eligibleIds = await _portalEligibilityService.GetEligibleConsumerIdsAsync(
            tenantIds[0],
            eligibleCandidates,
            httpContext.RequestAborted);

        return definitionsByItem
            .Select((definitions, index) => eligibleIds.Contains(items[index].Id)
                ? AppendPortalLink(items[index], definitions)
                : definitions)
            .ToList();
    }

    private async Task<IReadOnlyList<LinkDefinition>> AddPortalLinkWhenEligibleAsync(
        WebhookConsumerDto dto,
        IReadOnlyList<LinkDefinition> definitions,
        CancellationToken cancellationToken)
    {
        if (!IsConsumerPortalCandidate(dto))
        {
            return definitions;
        }

        var eligibleIds = await _portalEligibilityService.GetEligibleConsumerIdsAsync(
            dto.TenantId,
            [dto.Id],
            cancellationToken);
        return eligibleIds.Contains(dto.Id)
            ? AppendPortalLink(dto, definitions)
            : definitions;
    }

    private static IReadOnlyList<LinkDefinition> AppendPortalLink(
        WebhookConsumerDto dto,
        IReadOnlyList<LinkDefinition> definitions) =>
        [.. definitions, WebhookConsumerDetailLinkPolicy.CreateProviderPortalLink(dto)];

    private static bool IsConsumerPortalCandidate(WebhookConsumerDto dto) =>
        dto.StatusId == (int)WebhookConsumerStatus.Active &&
        dto.ProviderModeId is (int)WebhookProviderMode.Svix or (int)WebhookProviderMode.Composite;
}

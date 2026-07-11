// ABOUTME: HAL link policy for webhook consumer management resources.
// ABOUTME: Emits server-authorized provider portal and create affordances for webhook administration.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Hateoas;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.API.Hateoas.Policies;

public sealed class WebhookConsumerDetailLinkPolicy(IOptionsMonitor<WebhookOptions> webhookOptions)
    : ILinkPolicy<WebhookConsumerDto>
{
    public IEnumerable<LinkDefinition> GetLinks(WebhookConsumerDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetWebhookConsumerById,
            new { consumerId = dto.Id },
            "GET",
            "Webhook consumer")
            .RequirePermission(AuthorizationActions.Webhooks.View, ResourceDescriptors.WebhookConsumer, dto);

        if (CanOpenProviderPortal(dto, webhookOptions.CurrentValue))
        {
            yield return new LinkDefinition(
                LinkRelations.OpenProviderPortal,
                RouteNames.OpenSvixAppPortal,
                null,
                "POST",
                "Open advanced webhook provider portal",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Webhooks.OpenProviderPortal,
                    ResourceDescriptors.WebhookConsumer,
                    dto);
        }
    }

    private static bool CanOpenProviderPortal(WebhookConsumerDto dto, WebhookOptions options) =>
        options is { IsDisabled: false, Svix: { AppPortalEnabled: true } } &&
        (options.IsProvider(WebhookOptions.ProviderSvix) ||
         options.IsProvider(WebhookOptions.ProviderComposite)) &&
        (string.Equals(dto.ProviderModeName, WebhookOptions.ProviderSvix, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(dto.ProviderModeName, WebhookOptions.ProviderComposite, StringComparison.OrdinalIgnoreCase));
}

public sealed class WebhookConsumerCollectionLinkPolicy(ILinkPolicy<WebhookConsumerDto> detailPolicy)
    : ICollectionLinkPolicy<WebhookConsumerDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(WebhookConsumerDto dto, ClaimsPrincipal? user) =>
        detailPolicy.GetLinks(dto, user);

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Create,
            RouteNames.CreateWebhookConsumer,
            null,
            "POST",
            "Create webhook consumer",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Webhooks.Create, ResourceKinds.Webhook);
    }
}

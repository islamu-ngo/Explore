// ABOUTME: HAL link policy for webhook consumer management resources.
// ABOUTME: Emits server-authorized provider portal and create affordances for webhook administration.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class WebhookConsumerDetailLinkPolicy : ILinkPolicy<WebhookConsumerDto>
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

        if (string.Equals(dto.ProviderModeName, "Svix", StringComparison.Ordinal) ||
            string.Equals(dto.ProviderModeName, "Composite", StringComparison.Ordinal))
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
}

public sealed class WebhookConsumerCollectionLinkPolicy : ICollectionLinkPolicy<WebhookConsumerDto>
{
    private readonly WebhookConsumerDetailLinkPolicy _detailPolicy = new();

    public IEnumerable<LinkDefinition> GetItemLinks(WebhookConsumerDto dto, ClaimsPrincipal? user) =>
        _detailPolicy.GetLinks(dto, user);

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

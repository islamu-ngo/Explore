// ABOUTME: HAL link policy for webhook endpoint management resources.
// ABOUTME: Emits server-authorized endpoint collection and detail affordances for webhook administration.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class WebhookEndpointDetailLinkPolicy : ILinkPolicy<WebhookEndpointDto>
{
    public IEnumerable<LinkDefinition> GetLinks(WebhookEndpointDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetWebhookEndpointById,
            new { endpointId = dto.Id },
            "GET",
            "Webhook endpoint")
            .RequirePermission(AuthorizationActions.Webhooks.View, ResourceDescriptors.WebhookEndpoint, dto);

        if (!string.Equals(dto.StatusName, "Archived", StringComparison.Ordinal))
        {
            yield return new LinkDefinition(
                LinkRelations.Update,
                RouteNames.UpdateWebhookEndpoint,
                new { endpointId = dto.Id },
                "PUT",
                "Update webhook endpoint")
                .RequirePermission(AuthorizationActions.Webhooks.Update, ResourceDescriptors.WebhookEndpoint, dto);

            yield return new LinkDefinition(
                LinkRelations.RotateSecret,
                RouteNames.RotateWebhookEndpointSecret,
                new { endpointId = dto.Id },
                "POST",
                "Rotate webhook endpoint secret")
                .RequirePermission(AuthorizationActions.Webhooks.RotateSecret, ResourceDescriptors.WebhookEndpoint, dto);

            if (string.Equals(dto.StatusName, "Active", StringComparison.Ordinal))
            {
                yield return new LinkDefinition(
                    LinkRelations.Test,
                    RouteNames.TestWebhookEndpoint,
                    new { endpointId = dto.Id },
                    "POST",
                    "Test webhook endpoint")
                    .RequirePermission(AuthorizationActions.Webhooks.Test, ResourceDescriptors.WebhookEndpoint, dto);
            }

            yield return new LinkDefinition(
                LinkRelations.Delete,
                RouteNames.DeleteWebhookEndpoint,
                new { endpointId = dto.Id },
                "DELETE",
                "Delete webhook endpoint")
                .RequirePermission(AuthorizationActions.Webhooks.Delete, ResourceDescriptors.WebhookEndpoint, dto);
        }
    }
}

public sealed class WebhookEndpointCollectionLinkPolicy : ICollectionLinkPolicy<WebhookEndpointDto>
{
    private readonly WebhookEndpointDetailLinkPolicy _detailPolicy = new();

    public IEnumerable<LinkDefinition> GetItemLinks(WebhookEndpointDto dto, ClaimsPrincipal? user) =>
        _detailPolicy.GetLinks(dto, user);

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Create,
            RouteNames.CreateWebhookEndpoint,
            null,
            "POST",
            "Create webhook endpoint",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Webhooks.Create, ResourceKinds.Webhook);
    }
}

// ABOUTME: Defines authenticated HAL links for account-owned registration-order detail resources.
// ABOUTME: Carries only server-known order authorization attributes and never guest capabilities.

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class RegistrationOrderLinkPolicy : ILinkPolicy<RegistrationOrderDto>
{
    public IEnumerable<LinkDefinition> GetLinks(RegistrationOrderDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
                LinkRelations.Self,
                RouteNames.GetCurrentRegistrationOrder,
                new { eventId = dto.EventId, orderId = dto.Id },
                HttpMethods.Get,
                RequiresAuth: true)
            .RequirePermission(
                AuthorizationActions.RegistrationOrders.View,
                resourceKind: ResourceKinds.RegistrationOrder,
                resourceId: dto.Id.ToString("D"),
                resourceAttributes: Attributes(dto));

        yield return new LinkDefinition(
                LinkRelations.ViewParticipants,
                RouteNames.GetAuthenticatedRegistrationOrderParticipants,
                new { eventId = dto.EventId, orderId = dto.Id },
                HttpMethods.Get,
                "View participants",
                RequiresAuth: true)
            .RequirePermission(
                AuthorizationActions.RegistrationOrders.View,
                resourceKind: ResourceKinds.RegistrationOrder,
                resourceId: dto.Id.ToString("D"),
                resourceAttributes: Attributes(dto));

        if (dto.StatusCode is "AWAITING_REQUIREMENTS" or "READY_FOR_CHECKOUT")
        {
            yield return new LinkDefinition(
                    LinkRelations.Continue,
                    RouteNames.ContinueAuthenticatedRegistrationOrder,
                    new { eventId = dto.EventId, orderId = dto.Id },
                    HttpMethods.Post,
                    "Continue registration",
                    RequiresAuth: true)
                .RequirePermission(
                    AuthorizationActions.RegistrationOrders.Continue,
                    resourceKind: ResourceKinds.RegistrationOrder,
                    resourceId: dto.Id.ToString("D"),
                    resourceAttributes: Attributes(dto));
        }

        if (dto.StatusCode == "AWAITING_REQUIREMENTS")
        {
            yield return new LinkDefinition(
                    LinkRelations.RequirementProgress,
                    RouteNames.GetAuthenticatedNativeRegistrationRequirementProgress,
                    new { eventId = dto.EventId, orderId = dto.Id },
                    HttpMethods.Get,
                    "Continue registration requirements",
                    RequiresAuth: true)
                .RequirePermission(
                    AuthorizationActions.RegistrationOrders.Continue,
                    resourceKind: ResourceKinds.RegistrationOrder,
                    resourceId: dto.Id.ToString("D"),
                    resourceAttributes: Attributes(dto));
        }

        if (dto.StatusCode == "READY_FOR_CHECKOUT")
        {
            if (string.IsNullOrWhiteSpace(dto.AppliedPromotionDisplayLabel))
            {
                yield return new LinkDefinition(
                        LinkRelations.ApplyPromotion,
                        RouteNames.ApplyAuthenticatedRegistrationOrderPromotion,
                        new { eventId = dto.EventId, orderId = dto.Id },
                        HttpMethods.Post,
                        "Apply promotion",
                        RequiresAuth: true)
                    .RequirePermission(
                        AuthorizationActions.RegistrationOrders.Continue,
                        resourceKind: ResourceKinds.RegistrationOrder,
                        resourceId: dto.Id.ToString("D"),
                        resourceAttributes: Attributes(dto));
            }
            else
            {
                yield return new LinkDefinition(
                        LinkRelations.RemovePromotion,
                        RouteNames.RemoveAuthenticatedRegistrationOrderPromotion,
                        new { eventId = dto.EventId, orderId = dto.Id },
                        HttpMethods.Delete,
                        "Remove promotion",
                        RequiresAuth: true)
                    .RequirePermission(
                        AuthorizationActions.RegistrationOrders.Continue,
                        resourceKind: ResourceKinds.RegistrationOrder,
                        resourceId: dto.Id.ToString("D"),
                        resourceAttributes: Attributes(dto));
            }

            yield return new LinkDefinition(
                    LinkRelations.Finalize,
                    RouteNames.FinalizeAuthenticatedRegistrationOrder,
                    new { eventId = dto.EventId, orderId = dto.Id },
                    HttpMethods.Post,
                    "Finalize registration",
                    RequiresAuth: true)
                .RequirePermission(
                    AuthorizationActions.RegistrationOrders.Finalize,
                    resourceKind: ResourceKinds.RegistrationOrder,
                    resourceId: dto.Id.ToString("D"),
                    resourceAttributes: Attributes(dto));
        }

        yield return new LinkDefinition(
                LinkRelations.Cancel,
                RouteNames.CancelAuthenticatedRegistrationOrder,
                new { eventId = dto.EventId, orderId = dto.Id },
                HttpMethods.Delete,
                "Cancel registration order",
                RequiresAuth: true)
            .RequirePermission(
                AuthorizationActions.RegistrationOrders.Cancel,
                resourceKind: ResourceKinds.RegistrationOrder,
                resourceId: dto.Id.ToString("D"),
                resourceAttributes: Attributes(dto));
    }

    private static Dictionary<string, object> Attributes(RegistrationOrderDto dto) => new()
    {
        ["tenantId"] = dto.TenantId.ToString("D"),
        ["eventId"] = dto.EventId.ToString("D"),
        ["accountUserId"] = dto.AccountUserId?.ToString("D") ?? string.Empty
    };
}

public sealed class RegistrationOrderCollectionLinkPolicy : ICollectionLinkPolicy<RegistrationOrderDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(RegistrationOrderDto dto, ClaimsPrincipal? user)
    {
        yield break;
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield break;
    }
}

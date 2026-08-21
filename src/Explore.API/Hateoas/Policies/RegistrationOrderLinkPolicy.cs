// ABOUTME: Defines authenticated HAL links for account-owned registration-order detail resources.
// ABOUTME: Carries only server-known order authorization attributes and never guest capabilities.

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Hateoas;
using Explore.Application.Services.Registration;

namespace Explore.API.Hateoas.Policies;

public sealed class RegistrationOrderLinkPolicy(TimeProvider timeProvider) : ILinkPolicy<RegistrationOrderDto>
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
                facts: Facts(dto));

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
                facts: Facts(dto));

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
                    facts: Facts(dto));
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
                    facts: Facts(dto));
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
                        facts: Facts(dto));
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
                        facts: Facts(dto));
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
                    facts: Facts(dto));
        }

        if (RegistrationPaymentPayability.IsCurrentlyPayable(
                dto.StatusId,
                dto.TotalDueMinor,
                dto.ExpiresAt,
                timeProvider.GetUtcNow().UtcDateTime))
        {
            yield return new LinkDefinition(
                    LinkRelations.StartPayment,
                    RouteNames.StartAuthenticatedRegistrationPayment,
                    new { eventId = dto.EventId, orderId = dto.Id },
                    HttpMethods.Post,
                    "Start payment",
                    RequiresAuth: true)
                .RequirePermission(
                    AuthorizationActions.RegistrationOrders.Continue,
                    resourceKind: ResourceKinds.RegistrationOrder,
                    resourceId: dto.Id.ToString("D"),
                    facts: Facts(dto));
        }

        if (dto.TotalDueMinor > 0 && dto.StatusCode is "AWAITING_PAYMENT" or "NEEDS_RECONCILIATION" or "CONFIRMED")
        {
            yield return new LinkDefinition(
                    LinkRelations.PaymentStatus,
                    RouteNames.GetAuthenticatedRegistrationPayment,
                    new { eventId = dto.EventId, orderId = dto.Id },
                    HttpMethods.Get,
                    "Payment status",
                    RequiresAuth: true)
                .RequirePermission(
                    AuthorizationActions.RegistrationOrders.View,
                    resourceKind: ResourceKinds.RegistrationOrder,
                    resourceId: dto.Id.ToString("D"),
                    facts: Facts(dto));

            yield return new LinkDefinition(
                    LinkRelations.StudioPaymentStatus,
                    RouteNames.GetStudioRegistrationPayment,
                    new { eventId = dto.EventId, orderId = dto.Id },
                    HttpMethods.Get,
                    "Studio payment status",
                    RequiresAuth: true)
                .RequirePermission(
                    AuthorizationActions.Events.ManagePaidEventCommerce,
                    resourceKind: ResourceKinds.Event,
                    resourceId: dto.EventId.ToString("D"),
                    facts: new EventScopedAuthorizationFacts(dto.TenantId, dto.EventId));
        }

        if (dto.StatusCode is not ("AWAITING_PAYMENT" or "NEEDS_RECONCILIATION" or "CONFIRMED"))
        {
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
                    facts: Facts(dto));
        }
    }

    private static IAuthorizationFacts Facts(RegistrationOrderDto dto) =>
        new RegistrationOrderAuthorizationFacts(dto.TenantId, dto.EventId, dto.AccountUserId);
}

/// <summary>
/// Order affordances are published on the detail policy, which has the order state needed to authorize
/// them. The collection shape deliberately adds none.
/// </summary>
public sealed class RegistrationOrderCollectionLinkPolicy : ICollectionLinkPolicy<RegistrationOrderDto>;

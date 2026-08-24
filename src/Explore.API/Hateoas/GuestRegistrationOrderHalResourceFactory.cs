// ABOUTME: Builds guest registration-order HAL resources outside the route controller.
// ABOUTME: Preserves exact named routes while keeping payment and lifecycle affordances server-authoritative.

using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Hateoas;
using Explore.Application.Services.Registration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Explore.API.Hateoas;

public static class GuestRegistrationOrderHalResourceFactory
{
    public static HalResource<GuestRegistrationOrderDto> Create(
        GuestRegistrationOrderDto order,
        IUrlHelper url,
        TimeProvider timeProvider)
    {
        var values = new { eventId = order.EventId, orderId = order.Id };
        var resource = new HalResource<GuestRegistrationOrderDto>(order)
            .WithLink(LinkRelations.Self, HalLink.Create(url.Link(RouteNames.GetGuestRegistrationOrder, values)!));

        resource.WithLink(LinkRelations.ClaimRegistrationOrder, HalLink.CreateAction(
            url.Link(RouteNames.ClaimGuestRegistrationOrder, values)!, HttpMethods.Post));

        if (order.StatusCode is "AWAITING_REQUIREMENTS" or "READY_FOR_CHECKOUT")
        {
            resource.WithLink(LinkRelations.Continue, HalLink.CreateAction(
                url.Link(RouteNames.ContinueGuestRegistrationOrder, values)!, HttpMethods.Post));
        }

        if (order.StatusCode == "AWAITING_REQUIREMENTS")
        {
            resource.WithLink(LinkRelations.RequirementProgress, HalLink.Create(
                url.Link(RouteNames.GetGuestNativeRegistrationRequirementProgress, values)!));
        }

        if (order.StatusCode == "READY_FOR_CHECKOUT")
        {
            string promotionRelation = string.IsNullOrWhiteSpace(order.AppliedPromotionDisplayLabel)
                ? LinkRelations.ApplyPromotion
                : LinkRelations.RemovePromotion;
            string promotionRoute = string.IsNullOrWhiteSpace(order.AppliedPromotionDisplayLabel)
                ? RouteNames.ApplyGuestRegistrationOrderPromotion
                : RouteNames.RemoveGuestRegistrationOrderPromotion;
            string promotionMethod = string.IsNullOrWhiteSpace(order.AppliedPromotionDisplayLabel)
                ? HttpMethods.Post
                : HttpMethods.Delete;
            resource.WithLink(promotionRelation, HalLink.CreateAction(url.Link(promotionRoute, values)!, promotionMethod));
            resource.WithLink(LinkRelations.Finalize, HalLink.CreateAction(
                url.Link(RouteNames.FinalizeGuestRegistrationOrder, values)!, HttpMethods.Post));
        }

        if (order.PaidCheckoutActivationAvailable && RegistrationPaymentPayability.IsCurrentlyPayable(
                order.StatusId, order.TotalDueMinor, order.ExpiresAt, timeProvider.GetUtcNow().UtcDateTime))
        {
            resource.WithLink(LinkRelations.PaymentAcceptance, HalLink.Create(
                url.Link(RouteNames.GetGuestPaidOrderAcceptance, values)!));
            resource.WithLink(LinkRelations.StartPayment, HalLink.CreateAction(
                url.Link(RouteNames.StartGuestRegistrationPayment, values)!, HttpMethods.Post));
        }

        if (order.TotalDueMinor > 0 && order.StatusCode is "AWAITING_PAYMENT" or "NEEDS_RECONCILIATION" or "CONFIRMED")
        {
            resource.WithLink(LinkRelations.PaymentStatus, HalLink.Create(
                url.Link(RouteNames.GetGuestRegistrationPayment, values)!));
        }

        if (order.StatusCode is "DRAFT" or "AWAITING_REQUIREMENTS" or "READY_FOR_CHECKOUT" or "AWAITING_PAYMENT" or "AWAITING_APPROVAL")
        {
            resource.WithLink(LinkRelations.Cancel, HalLink.CreateAction(
                url.Link(RouteNames.CancelGuestRegistrationOrder, values)!, HttpMethods.Delete));
        }

        return resource;
    }
}

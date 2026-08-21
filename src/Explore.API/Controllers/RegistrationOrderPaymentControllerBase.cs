// ABOUTME: Shared ProblemDetails and HAL assembly for registration-order payment controller capabilities.
// ABOUTME: Keeps provider checkout targets out of payment resources and exposes only same-origin BFF navigation.

using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Hateoas;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

public abstract class RegistrationOrderPaymentControllerBase(IMediator mediator) : ControllerBase
{
    protected const string CapabilityHeader = "X-Registration-Order-Capability";
    protected const string IdempotencyKeyHeader = "Idempotency-Key";
    protected IMediator Mediator { get; } = mediator;

    private static readonly ApiValidationProblemDescriptor PaymentValidationProblem = new(
        "registrationPayment", "Registration payment request failed", "Registration payment request failed.");
    private static readonly ApiNotFoundProblemDescriptor PaymentNotFoundProblem = new(
        "Registration payment not found", "Registration payment was not found.");
    private protected static readonly CommandFailurePolicy PaymentFailures = CommandFailurePolicy
        .ValidatedBy(PaymentValidationProblem)
        .NotFound(PaymentNotFoundProblem, "registration_order_not_found")
        .Conflict("Payment action unavailable", "The requested payment action is not available.", "not_payable", "payment_retry_not_available")
        .Unavailable(
            "Payment temporarily unavailable",
            "Payment could not be started.",
            "payment_start_failed",
            "payment_readiness_unavailable",
            "payment_organizer_unavailable",
            "payment_configuration_unavailable",
            "payment_connection_unavailable");

    protected ActionResult<HalResource<RegistrationPaymentDto>> MapResult(
        RegistrationPaymentCommandResultDto result,
        Guid eventId,
        Guid orderId,
        bool guest)
    {
        if (!result.Success || result.Payment is null)
        {
            return PaymentFailures.Map(this, result);
        }

        return Ok(ToResource(result.Payment, eventId, orderId, guest));
    }

    protected HalResource<RegistrationPaymentDto> ToResource(
        RegistrationPaymentDto payment,
        Guid eventId,
        Guid orderId,
        bool guest)
    {
        string statusRoute = guest ? RouteNames.GetGuestRegistrationPayment : RouteNames.GetAuthenticatedRegistrationPayment;
        string retryRoute = guest ? RouteNames.RetryGuestRegistrationPayment : RouteNames.RetryAuthenticatedRegistrationPayment;
        var values = new { eventId, orderId };
        var resource = new HalResource<RegistrationPaymentDto>(payment)
            .WithLink(LinkRelations.Self, HalLink.Create(Url.Link(statusRoute, values)!));
        resource.WithLink(LinkRelations.PaymentStatus, HalLink.Create(Url.Link(statusRoute, values)!));

        if (payment.HostedRedirectAvailable)
        {
            resource.WithLink(LinkRelations.CheckoutRedirect, HalLink.CreateAction(
                $"bff/registration-payments/events/{eventId:D}/orders/{orderId:D}/checkout-ticket", HttpMethods.Post));
        }

        if (payment.RetryAvailable)
        {
            resource.WithLink(LinkRelations.RetryPayment, HalLink.CreateAction(Url.Link(retryRoute, values)!, HttpMethods.Post));
        }

        return resource;
    }

    protected ActionResult<RegistrationPaymentCheckoutTargetDto> TargetOrNotFound(RegistrationPaymentCheckoutTargetDto? target) =>
        target is null ? this.ToNotFoundProblem(PaymentNotFoundProblem) : Ok(target);

    protected ActionResult PaymentNotFoundResult() => this.ToNotFoundProblem(PaymentNotFoundProblem);
}

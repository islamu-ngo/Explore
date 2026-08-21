// ABOUTME: Pins the payment start/status/retry HTTP contract for guest, account, and Studio callers.
// ABOUTME: Verifies transactional safeguards, private caching, and named route stability before implementation.

using System.Reflection;
using Explore.API.Attributes;
using Explore.API.Controllers;
using Explore.API.Extensions;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace Event.Api.IntegrationTests.Features;

public sealed class RegistrationPaymentContractTests
{
    [Test]
    public async Task GuestPaymentWritesAreCapabilityScopedPublicTransactionalAndIdempotent()
    {
        await AssertEndpoint<GuestRegistrationOrderPaymentController, HttpPostAttribute>(
            nameof(GuestRegistrationOrderPaymentController.Start), "guest/{orderId:guid}/payment", RouteNames.StartGuestRegistrationPayment, EndpointClass.PublicTransactional, true);
        await AssertEndpoint<GuestRegistrationOrderPaymentController, HttpPostAttribute>(
            nameof(GuestRegistrationOrderPaymentController.Retry), "guest/{orderId:guid}/payment/retry", RouteNames.RetryGuestRegistrationPayment, EndpointClass.PublicTransactional, true);
        await AssertEndpoint<GuestRegistrationOrderPaymentController, HttpGetAttribute>(
            nameof(GuestRegistrationOrderPaymentController.GetStatus), "guest/{orderId:guid}/payment", RouteNames.GetGuestRegistrationPayment, EndpointClass.Public, false);
    }

    [Test]
    public async Task AccountAndStudioPaymentReadsStayPrivateAndExplicitlyClassified()
    {
        await AssertEndpoint<AuthenticatedRegistrationOrderPaymentController, HttpPostAttribute>(
            nameof(AuthenticatedRegistrationOrderPaymentController.Start), "{orderId:guid}/payment", RouteNames.StartAuthenticatedRegistrationPayment, EndpointClass.Authenticated, true);
        await AssertEndpoint<AuthenticatedRegistrationOrderPaymentController, HttpPostAttribute>(
            nameof(AuthenticatedRegistrationOrderPaymentController.Retry), "{orderId:guid}/payment/retry", RouteNames.RetryAuthenticatedRegistrationPayment, EndpointClass.Authenticated, true);
        await AssertEndpoint<AuthenticatedRegistrationOrderPaymentController, HttpGetAttribute>(
            nameof(AuthenticatedRegistrationOrderPaymentController.GetStatus), "{orderId:guid}/payment", RouteNames.GetAuthenticatedRegistrationPayment, EndpointClass.Authenticated, false);
        await AssertEndpoint<StudioRegistrationOrderPaymentController, HttpGetAttribute>(
            nameof(StudioRegistrationOrderPaymentController.GetStatus), "{orderId:guid}/payment/studio", RouteNames.GetStudioRegistrationPayment, EndpointClass.Authenticated, false);
    }

    private static async Task AssertEndpoint<TController, THttpAttribute>(
        string methodName,
        string template,
        string routeName,
        EndpointClass endpointClass,
        bool requiresIdempotency)
        where THttpAttribute : HttpMethodAttribute
    {
        MethodInfo method = typeof(TController).GetMethod(methodName)!;
        THttpAttribute route = method.GetCustomAttribute<THttpAttribute>()!;
        await Assert.That(route.Template).IsEqualTo(template);
        await Assert.That(route.Name).IsEqualTo(routeName);
        await Assert.That(method.GetCustomAttribute<EndpointClassificationAttribute>()?.Class).IsEqualTo(endpointClass);
        await Assert.That(method.GetCustomAttribute<PrivateNoStoreAttribute>()).IsNotNull();
        await Assert.That(method.GetCustomAttribute<RequireIdempotencyKeyAttribute>() is not null).IsEqualTo(requiresIdempotency);

        if (endpointClass == EndpointClass.PublicTransactional)
        {
            await Assert.That(method.GetCustomAttribute<AllowAnonymousAttribute>()).IsNotNull();
            await Assert.That(method.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName)
                .IsEqualTo(RateLimitingExtensions.PublicTransactionalPolicy);
        }
        else if (endpointClass == EndpointClass.Authenticated)
        {
            await Assert.That(method.GetCustomAttribute<AuthorizeAttribute>()).IsNotNull();
        }
    }
}

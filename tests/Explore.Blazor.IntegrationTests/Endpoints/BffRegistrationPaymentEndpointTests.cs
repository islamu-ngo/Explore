// ABOUTME: Pins the BFF payment navigation boundary to server-resolved checkout targets and local callbacks.
// ABOUTME: Prevents browser-supplied external URLs and callback-driven payment confirmation.

using Explore.Blazor.Extensions;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Services.Http;
using Explore.Blazor.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffRegistrationPaymentEndpointTests
{
    [Test]
    public async Task HostedCheckoutValidationAllowsOnlyConfiguredHttpsHostsWithoutUserInfoOrFragment()
    {
        await Assert.That(BffRegistrationPaymentEndpoints.IsApprovedCheckoutTarget(
            new Uri("https://checkout.stripe.com/c/pay/cs_test"), ["checkout.stripe.com"])).IsTrue();
        await Assert.That(BffRegistrationPaymentEndpoints.IsApprovedCheckoutTarget(
            new Uri("https://user@checkout.stripe.com/c/pay/cs_test"), ["checkout.stripe.com"])).IsFalse();
        await Assert.That(BffRegistrationPaymentEndpoints.IsApprovedCheckoutTarget(
            new Uri("https://checkout.stripe.com/c/pay/cs_test#fragment"), ["checkout.stripe.com"])).IsFalse();
        await Assert.That(BffRegistrationPaymentEndpoints.IsApprovedCheckoutTarget(
            new Uri("https://attacker.example/c/pay/cs_test"), ["checkout.stripe.com"])).IsFalse();
        await Assert.That(BffRegistrationPaymentEndpoints.IsApprovedCheckoutTarget(
            new Uri("https://checkout.stripe.com:444/c/pay/cs_test"), ["checkout.stripe.com"])).IsFalse();
    }

    [Test]
    public async Task ProviderCallbacksOnlyNavigateToRecoveryWithNoStore()
    {
        IEventApiClient apiClient = Substitute.For<IEventApiClient>();
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using HttpResponseMessage success = await client.GetAsync("/payments/checkout/success");
        using HttpResponseMessage cancel = await client.GetAsync("/payments/checkout/cancel");

        await Assert.That(success.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(cancel.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(success.Headers.Location?.OriginalString).IsEqualTo("/registration/payment-recovery");
        await Assert.That(cancel.Headers.Location?.OriginalString).IsEqualTo("/registration/payment-recovery");
        await Assert.That(success.Headers.CacheControl?.NoStore).IsTrue();
        await Assert.That(cancel.Headers.CacheControl?.NoStore).IsTrue();
        await apiClient.DidNotReceiveWithAnyArgs().GetAuthenticatedRegistrationPaymentCheckoutTargetAsync(
            default, default, default!, default!, default);
        await apiClient.DidNotReceiveWithAnyArgs().GetGuestRegistrationPaymentCheckoutTargetAsync(
            default, default, default, default!, default!, default);
    }

    [Test]
    public async Task DirectForwardedHostCannotAlterPaymentRecoveryRedirect()
    {
        await using var factory = new BlazorBffWebApplicationFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/payments/checkout/success");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "attacker.example");

        using HttpResponseMessage response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location?.OriginalString).IsEqualTo("/registration/payment-recovery");
        await Assert.That(response.Headers.Location?.ToString()).DoesNotContain("attacker.example");
    }

    [Test]
    public async Task PublicBaseUrlSubpathPreservesBffAndRecoveryPaths()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_subpath");
        await using WebApplicationFactory<Program> factory = CreateFactory(
            apiClient,
            publicBaseUrl: "https://localhost/events");
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client, "/events/auth/status");
        using HttpResponseMessage issued = await IssueCheckoutAsync(client, "/events" + CheckoutIssuePath, session);
        BffRegistrationPaymentCheckoutTicketResponseDto? ticket = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string cookies = GetCheckoutCookie(issued);
        using var consumeRequest = new HttpRequestMessage(HttpMethod.Get, ticket!.CheckoutPath);
        AddBrowserSession(consumeRequest, session, cookies);
        using HttpResponseMessage consumed = await client.SendAsync(consumeRequest);
        using HttpResponseMessage success = await client.GetAsync("/events/payments/checkout/success");

        await Assert.That(issued.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(ticket.CheckoutPath).IsEqualTo("/events/bff/registration-payments/checkout");
        await Assert.That(consumed.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(success.Headers.Location?.OriginalString).IsEqualTo("/events/registration/payment-recovery");
    }

    [Test]
    public async Task AuthenticatedCheckoutTicket_IssuesOpaquePathAndConsumesOnce()
    {
        IEventApiClient apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetAuthenticatedRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RegistrationPaymentCheckoutTargetDto { Url = "https://checkout.stripe.com/c/pay/cs_test" }));
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        const string route = "/bff/registration-payments/events/018e4e5c-7f00-7000-8000-000000000101/orders/018e4e5c-7f00-7000-8000-000000000201/checkout-ticket";

        using var goodRequest = new HttpRequestMessage(HttpMethod.Post, route);
        AddBrowserSession(goodRequest, session);
        using HttpResponseMessage issued = await client.SendAsync(goodRequest);
        BffRegistrationPaymentCheckoutTicketResponseDto? ticket = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string checkoutCookie = GetCheckoutCookie(issued);
        string tamperedCookie = TamperTicketCookie(checkoutCookie);
        using var tamperedRequest = new HttpRequestMessage(HttpMethod.Get, ticket!.CheckoutPath);
        AddBrowserSession(tamperedRequest, session, tamperedCookie);
        using HttpResponseMessage tampered = await client.SendAsync(tamperedRequest);
        using var navigationRequest = new HttpRequestMessage(HttpMethod.Get, ticket.CheckoutPath);
        AddBrowserSession(navigationRequest, session, checkoutCookie);
        using HttpResponseMessage navigation = await client.SendAsync(navigationRequest);
        using var replayRequest = new HttpRequestMessage(HttpMethod.Get, ticket.CheckoutPath);
        AddBrowserSession(replayRequest, session, checkoutCookie);
        using HttpResponseMessage replay = await client.SendAsync(replayRequest);

        using var maliciousRequest = new HttpRequestMessage(HttpMethod.Post, route + "?url=https://attacker.example");
        AddBrowserSession(maliciousRequest, session);
        using HttpResponseMessage malicious = await client.SendAsync(maliciousRequest);

        await Assert.That(issued.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(issued.Headers.CacheControl?.NoStore).IsTrue();
        await Assert.That(ticket.CheckoutPath).IsEqualTo("/bff/registration-payments/checkout");
        await Assert.That(ticket.CheckoutPath).DoesNotContain("checkout.stripe.com");
        string setCookie = issued.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Secure-islamu-registration-payment-checkout=", StringComparison.Ordinal));
        await Assert.That(setCookie).Contains("httponly").IgnoringCase();
        await Assert.That(setCookie).Contains("secure").IgnoringCase();
        await Assert.That(setCookie).Contains("samesite=strict").IgnoringCase();
        await Assert.That(GetCheckoutCookie(issued).Length).IsLessThan(4096);
        await Assert.That(navigation.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(navigation.Headers.Location?.AbsoluteUri).IsEqualTo("https://checkout.stripe.com/c/pay/cs_test");
        await Assert.That(navigation.Headers.CacheControl?.NoStore).IsTrue();
        await Assert.That(navigation.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Secure-islamu-registration-payment-checkout=", StringComparison.Ordinal)))
            .Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT").IgnoringCase();
        await Assert.That(replay.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(tampered.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(malicious.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await apiClient.Received(1).GetAuthenticatedRegistrationPaymentCheckoutTargetAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), null, null, Arg.Any<CancellationToken>());
        await apiClient.DidNotReceiveWithAnyArgs().StartAuthenticatedRegistrationPaymentAsync(
            default, default, default!, default!, default!, default);
        await apiClient.DidNotReceiveWithAnyArgs().RetryAuthenticatedRegistrationPaymentAsync(
            default, default, default!, default!, default!, default);
    }

    [Test]
    public async Task GuestCheckoutTicket_UsesCapabilityHeaderWithoutDisclosure()
    {
        const string capability = "guest-secret-capability";
        IEventApiClient apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetGuestRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), capability, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RegistrationPaymentCheckoutTargetDto { Url = "https://checkout.stripe.com/c/pay/cs_guest" }));
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/bff/registration-payments/events/018e4e5c-7f00-7000-8000-000000000101/orders/018e4e5c-7f00-7000-8000-000000000201/checkout-ticket");
        AddBrowserSession(request, session);
        request.Headers.Add("X-Registration-Order-Capability", capability);

        using HttpResponseMessage issued = await client.SendAsync(request);
        string json = await issued.Content.ReadAsStringAsync();
        BffRegistrationPaymentCheckoutTicketResponseDto? ticket = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string checkoutCookie = GetCheckoutCookie(issued);
        using var navigationRequest = new HttpRequestMessage(HttpMethod.Get, ticket!.CheckoutPath);
        AddBrowserSession(navigationRequest, session, checkoutCookie);
        using HttpResponseMessage navigation = await client.SendAsync(navigationRequest);

        await Assert.That(issued.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(json).DoesNotContain(capability);
        await Assert.That(json).DoesNotContain("checkout.stripe.com");
        await Assert.That(ticket.CheckoutPath).IsEqualTo("/bff/registration-payments/checkout");
        await Assert.That(ticket.CheckoutPath).DoesNotContain(capability);
        await Assert.That(navigation.Headers.Location?.AbsoluteUri).IsEqualTo("https://checkout.stripe.com/c/pay/cs_guest");
        await apiClient.Received(1).GetGuestRegistrationPaymentCheckoutTargetAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), capability, null, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CheckoutCookie_KeepsSecretsOutOfOpenTelemetryRequestSurface()
    {
        const string capability = "otel-private-capability";
        var observed = new ConcurrentQueue<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => observed.Enqueue(
                activity.DisplayName + " " + string.Join(' ', activity.Tags.Select(tag => tag.Value)))
        };
        ActivitySource.AddActivityListener(listener);
        IEventApiClient apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetGuestRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), capability, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RegistrationPaymentCheckoutTargetDto { Url = "https://checkout.stripe.com/c/pay/cs_otel_private" }));
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using var issueRequest = new HttpRequestMessage(HttpMethod.Post, CheckoutIssuePath);
        AddBrowserSession(issueRequest, session);
        issueRequest.Headers.Add("X-Registration-Order-Capability", capability);
        using HttpResponseMessage issued = await client.SendAsync(issueRequest);
        BffRegistrationPaymentCheckoutTicketResponseDto? response = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string cookie = GetCheckoutCookie(issued);
        using var consumeRequest = new HttpRequestMessage(HttpMethod.Get, response!.CheckoutPath);
        AddBrowserSession(consumeRequest, session, cookie);
        using HttpResponseMessage consumed = await client.SendAsync(consumeRequest);
        string traceSurface = string.Join('\n', observed);

        await Assert.That(consumed.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(traceSurface).Contains("/bff/registration-payments/checkout");
        await Assert.That(traceSurface).DoesNotContain(capability);
        await Assert.That(traceSurface).DoesNotContain(cookie);
        await Assert.That(traceSurface).DoesNotContain("cs_otel_private");
    }

    [Test]
    public async Task CheckoutTicketIssue_RejectsMissingAntiforgeryAndAccessFailures()
    {
        IEventApiClient apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetAuthenticatedRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RegistrationPaymentCheckoutTargetDto>(ApiFailure(StatusCodes.Status401Unauthorized)));
        apiClient.GetGuestRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>(), null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RegistrationPaymentCheckoutTargetDto>(ApiFailure(StatusCodes.Status404NotFound)));
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        const string route = "/bff/registration-payments/events/018e4e5c-7f00-7000-8000-000000000101/orders/018e4e5c-7f00-7000-8000-000000000201/checkout-ticket";

        using HttpResponseMessage noAntiforgery = await client.PostAsync(route, content: null);
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using var missingRequest = new HttpRequestMessage(HttpMethod.Post, route);
        AddBrowserSession(missingRequest, session);
        using HttpResponseMessage missing = await client.SendAsync(missingRequest);
        using var wrongRequest = new HttpRequestMessage(HttpMethod.Post, route);
        AddBrowserSession(wrongRequest, session);
        wrongRequest.Headers.Add("X-Registration-Order-Capability", "wrong-capability");
        using HttpResponseMessage wrong = await client.SendAsync(wrongRequest);
        using var expiredRequest = new HttpRequestMessage(HttpMethod.Post, route);
        AddBrowserSession(expiredRequest, session);
        expiredRequest.Headers.Add("X-Registration-Order-Capability", "expired-capability");
        using HttpResponseMessage expired = await client.SendAsync(expiredRequest);

        await Assert.That(noAntiforgery.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(wrong.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(expired.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CheckoutTicket_ExpiresAndMaliciousTargetIsNeverIssued()
    {
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc));
        IEventApiClient apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetAuthenticatedRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RegistrationPaymentCheckoutTargetDto { Url = "https://checkout.stripe.com/c/pay/cs_expiring" }));
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient, timeProvider);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        const string route = "/bff/registration-payments/events/018e4e5c-7f00-7000-8000-000000000101/orders/018e4e5c-7f00-7000-8000-000000000201/checkout-ticket";
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        AddBrowserSession(request, session);
        using HttpResponseMessage issued = await client.SendAsync(request);
        BffRegistrationPaymentCheckoutTicketResponseDto? ticket = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        timeProvider.Advance(TimeSpan.FromMinutes(6));

        string checkoutCookie = GetCheckoutCookie(issued);
        using var expiredRequest = new HttpRequestMessage(HttpMethod.Get, ticket!.CheckoutPath);
        AddBrowserSession(expiredRequest, session, checkoutCookie);
        using HttpResponseMessage expired = await client.SendAsync(expiredRequest);

        apiClient.GetAuthenticatedRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RegistrationPaymentCheckoutTargetDto { Url = "https://attacker.example/checkout" }));
        using var maliciousRequest = new HttpRequestMessage(HttpMethod.Post, route);
        AddBrowserSession(maliciousRequest, session);
        using HttpResponseMessage malicious = await client.SendAsync(maliciousRequest);

        await Assert.That(expired.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(malicious.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CheckoutCookie_PathBaseAudienceAndSessionFailuresDoNotBurnNonce()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_pathbase");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using HttpClient secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession firstSession = await IssueBrowserSessionAsync(firstClient, "/t/acme/auth/status");
        BrowserSession secondSession = await IssueBrowserSessionAsync(secondClient, "/t/acme/auth/status");
        const string issuePath = "/t/acme/bff/registration-payments/events/018e4e5c-7f00-7000-8000-000000000101/orders/018e4e5c-7f00-7000-8000-000000000201/checkout-ticket";
        using HttpResponseMessage issued = await IssueCheckoutAsync(firstClient, issuePath, firstSession);
        BffRegistrationPaymentCheckoutTicketResponseDto? response = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string checkoutCookie = GetCheckoutCookie(issued);

        using var wrongSessionRequest = new HttpRequestMessage(HttpMethod.Get, response!.CheckoutPath);
        AddBrowserSession(wrongSessionRequest, secondSession, ReplaceCheckoutSession(checkoutCookie));
        using HttpResponseMessage wrongSession = await secondClient.SendAsync(wrongSessionRequest);
        using var wrongHostRequest = new HttpRequestMessage(HttpMethod.Get, "https://other.example" + response.CheckoutPath);
        AddBrowserSession(wrongHostRequest, firstSession, checkoutCookie);
        using HttpResponseMessage wrongHost = await firstClient.SendAsync(wrongHostRequest);
        using var wrongPathRequest = new HttpRequestMessage(HttpMethod.Get, "/bff/registration-payments/checkout");
        AddBrowserSession(wrongPathRequest, firstSession, checkoutCookie);
        using HttpResponseMessage wrongPath = await firstClient.SendAsync(wrongPathRequest);
        using var correctRequest = new HttpRequestMessage(HttpMethod.Get, response.CheckoutPath);
        AddBrowserSession(correctRequest, firstSession, checkoutCookie);
        using HttpResponseMessage correct = await firstClient.SendAsync(correctRequest);

        await Assert.That(response.CheckoutPath).IsEqualTo("/t/acme/bff/registration-payments/checkout");
        await Assert.That(issued.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Secure-islamu-registration-payment-checkout=", StringComparison.Ordinal)))
            .Contains("path=/t/acme/bff/registration-payments/checkout").IgnoringCase();
        await Assert.That(wrongSession.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(wrongHost.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(wrongPath.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(correct.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task CheckoutCookie_ConcurrentConsumeHasOneWinner()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_concurrent");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage issued = await IssueCheckoutAsync(client, CheckoutIssuePath, session);
        BffRegistrationPaymentCheckoutTicketResponseDto? response = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string cookie = GetCheckoutCookie(issued);
        var first = new HttpRequestMessage(HttpMethod.Get, response!.CheckoutPath);
        var second = new HttpRequestMessage(HttpMethod.Get, response.CheckoutPath);
        AddBrowserSession(first, session, cookie);
        AddBrowserSession(second, session, cookie);

        HttpResponseMessage[] results = await Task.WhenAll(client.SendAsync(first), client.SendAsync(second));
        int redirects = results.Count(result => result.StatusCode == HttpStatusCode.Redirect);
        int rejected = results.Count(result => result.StatusCode == HttpStatusCode.NotFound);
        foreach (HttpResponseMessage result in results)
        {
            result.Dispose();
        }

        await Assert.That(redirects).IsEqualTo(1);
        await Assert.That(rejected).IsEqualTo(1);
    }

    [Test]
    public async Task CheckoutCookie_AllowlistRotationDoesNotBurnNonce()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_rotation");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage issued = await IssueCheckoutAsync(client, CheckoutIssuePath, session);
        BffRegistrationPaymentCheckoutTicketResponseDto? response = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string cookie = GetCheckoutCookie(issued);
        IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();
        configuration["Payments:Stripe:AllowedCheckoutHosts:0"] = "other.example";
        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Get, response!.CheckoutPath);
        AddBrowserSession(rejectedRequest, session, cookie);
        using HttpResponseMessage rejected = await client.SendAsync(rejectedRequest);
        configuration["Payments:Stripe:AllowedCheckoutHosts:0"] = "checkout.stripe.com";
        using var acceptedRequest = new HttpRequestMessage(HttpMethod.Get, response.CheckoutPath);
        AddBrowserSession(acceptedRequest, session, cookie);
        using HttpResponseMessage accepted = await client.SendAsync(acceptedRequest);

        await Assert.That(rejected.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(accepted.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task StandaloneCheckoutCookie_ReplacesActiveTicketAndScavengesExpiry()
    {
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc));
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_replace");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient, timeProvider);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage firstIssue = await IssueCheckoutAsync(client, CheckoutIssuePath, session);
        string firstCookie = GetCheckoutCookie(firstIssue);
        using HttpResponseMessage secondIssue = await IssueCheckoutAsync(client, CheckoutIssuePath, session, firstCookie);
        BffRegistrationPaymentCheckoutTicketResponseDto? response = await secondIssue.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string secondCookie = GetCheckoutCookie(secondIssue);
        using var replacedRequest = new HttpRequestMessage(HttpMethod.Get, response!.CheckoutPath);
        AddBrowserSession(replacedRequest, session, firstCookie);
        using HttpResponseMessage replaced = await client.SendAsync(replacedRequest);
        timeProvider.Advance(TimeSpan.FromMinutes(6));
        using HttpResponseMessage thirdIssue = await IssueCheckoutAsync(client, CheckoutIssuePath, session);

        await Assert.That(replaced.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(secondCookie).IsNotEqualTo(firstCookie);
        await Assert.That(thirdIssue.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task SplitCheckoutCookie_MissingOrFailedRedisFailsClosed()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_redis");
        await using WebApplicationFactory<Program> missingFactory = CreateFactory(apiClient, requireRedis: true);
        using HttpClient missingClient = missingFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession missingSession = await IssueBrowserSessionAsync(missingClient);
        using HttpResponseMessage missing = await IssueCheckoutAsync(missingClient, CheckoutIssuePath, missingSession);

        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_ => throw new InvalidOperationException("redis unavailable"));
        await using WebApplicationFactory<Program> failedFactory = CreateFactory(apiClient, requireRedis: true, redis: redis);
        using HttpClient failedClient = failedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession failedSession = await IssueBrowserSessionAsync(failedClient);
        using HttpResponseMessage failed = await IssueCheckoutAsync(failedClient, CheckoutIssuePath, failedSession);

        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await Assert.That(failed.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await Assert.That(missing.Headers.TryGetValues("Set-Cookie", out var missingCookies)
            && missingCookies.Any(value => value.StartsWith("__Secure-islamu-registration-payment-checkout=", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task CheckoutCookie_CrossSiteGetIsRejectedWithoutBurningTicket()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_fetch_site");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage issued = await IssueCheckoutAsync(client, CheckoutIssuePath, session);
        BffRegistrationPaymentCheckoutTicketResponseDto? response = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string cookies = GetCheckoutCookie(issued);
        var rejectedStatuses = new List<HttpStatusCode>();
        foreach (string? fetchSite in new string?[] { "same-site", "cross-site", "none", null })
        {
            using var rejectedRequest = new HttpRequestMessage(HttpMethod.Get, response!.CheckoutPath);
            AddBrowserSession(rejectedRequest, session, cookies, fetchSite);
            using HttpResponseMessage rejected = await client.SendAsync(rejectedRequest);
            rejectedStatuses.Add(rejected.StatusCode);
        }
        using var sameSiteRequest = new HttpRequestMessage(HttpMethod.Get, response.CheckoutPath);
        AddBrowserSession(sameSiteRequest, session, cookies);
        using HttpResponseMessage sameSite = await client.SendAsync(sameSiteRequest);

        foreach (HttpStatusCode status in rejectedStatuses)
        {
            await Assert.That(status).IsEqualTo(HttpStatusCode.BadRequest);
        }
        await Assert.That(sameSite.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task CheckoutCookie_AuthCookieRenewalDoesNotBreakDedicatedSession()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_auth_refresh");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage issued = await IssueCheckoutAsync(client, CheckoutIssuePath, session);
        BffRegistrationPaymentCheckoutTicketResponseDto? response = await issued.Content.ReadFromJsonAsync<BffRegistrationPaymentCheckoutTicketResponseDto>();
        string checkoutCookies = GetCheckoutCookie(issued);
        var renewedSession = session with { CookieHeader = session.CookieHeader + "; .AspNetCore.Cookies=renewed-auth-ticket" };
        using var request = new HttpRequestMessage(HttpMethod.Get, response!.CheckoutPath);
        AddBrowserSession(request, renewedSession, checkoutCookies);
        using HttpResponseMessage result = await client.SendAsync(request);

        await Assert.That(result.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task CheckoutIssue_OversizedHostedTargetFailsWithoutTicketCookie()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/" + new string('a', 5000));
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage response = await IssueCheckoutAsync(client, CheckoutIssuePath, session);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(value => value.StartsWith("__Secure-islamu-registration-payment-checkout=", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task CheckoutIssue_RotatedAntiforgerySessionsShareEffectiveIpRateBound()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_rate");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient, checkoutPermitLimit: 2);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession firstSession = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage first = await IssueCheckoutAsync(client, CheckoutIssuePath, firstSession,
            "__Secure-islamu-registration-payment-session=caller-session-one");
        BrowserSession secondSession = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage second = await IssueCheckoutAsync(client, CheckoutIssuePath, secondSession,
            "__Secure-islamu-registration-payment-session=caller-session-two");
        BrowserSession thirdSession = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage third = await IssueCheckoutAsync(client, CheckoutIssuePath, thirdSession,
            "__Secure-islamu-registration-payment-session=caller-session-three");

        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(third.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task CheckoutIssue_InvalidAntiforgeryDoesNotConsumePermits()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_antiforgery_rate");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient, checkoutPermitLimit: 2);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        for (var attempt = 0; attempt < 10; attempt++)
        {
            using HttpResponseMessage invalid = await client.PostAsync(CheckoutIssuePath, content: null);
            await Assert.That(invalid.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        }

        BrowserSession session = await IssueBrowserSessionAsync(client);
        using HttpResponseMessage first = await IssueCheckoutAsync(client, CheckoutIssuePath, session);
        using HttpResponseMessage second = await IssueCheckoutAsync(client, CheckoutIssuePath, session);
        using HttpResponseMessage exhausted = await IssueCheckoutAsync(client, CheckoutIssuePath, session);

        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(exhausted.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task CheckoutIssue_TrustedForwardedClientIpsUseDistinctPartitions()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_forwarded_rate");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient, checkoutPermitLimit: 1);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        BrowserSession session = await IssueBrowserSessionAsync(client);

        using HttpResponseMessage firstClient = await IssueCheckoutAsync(
            client, CheckoutIssuePath, session, forwardedFor: "198.51.100.10");
        using HttpResponseMessage firstClientExhausted = await IssueCheckoutAsync(
            client, CheckoutIssuePath, session, forwardedFor: "198.51.100.10");
        using HttpResponseMessage secondClient = await IssueCheckoutAsync(
            client, CheckoutIssuePath, session, forwardedFor: "198.51.100.11");

        await Assert.That(firstClient.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(firstClientExhausted.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        await Assert.That(secondClient.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task CheckoutIssue_AuthenticatedRateBoundUsesStableUserId()
    {
        IEventApiClient apiClient = CheckoutTargetClient("https://checkout.stripe.com/c/pay/cs_user_rate");
        await using WebApplicationFactory<Program> factory = CreateFactory(apiClient, checkoutPermitLimit: 2);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateAuthHeaderValue(Guid.Parse("018e4e5c-7f00-7000-8000-000000000333")));
        BrowserSession session = await IssueBrowserSessionAsync(client);

        using HttpResponseMessage first = await IssueCheckoutAsync(client, CheckoutIssuePath, session,
            "__Secure-islamu-registration-payment-session=user-session-one");
        using HttpResponseMessage second = await IssueCheckoutAsync(client, CheckoutIssuePath, session,
            "__Secure-islamu-registration-payment-session=user-session-two");
        using HttpResponseMessage third = await IssueCheckoutAsync(client, CheckoutIssuePath, session,
            "__Secure-islamu-registration-payment-session=user-session-three");

        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(third.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    private const string CheckoutIssuePath = "/bff/registration-payments/events/018e4e5c-7f00-7000-8000-000000000101/orders/018e4e5c-7f00-7000-8000-000000000201/checkout-ticket";

    private static IEventApiClient CheckoutTargetClient(string target)
    {
        IEventApiClient apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetAuthenticatedRegistrationPaymentCheckoutTargetAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RegistrationPaymentCheckoutTargetDto { Url = target }));
        return apiClient;
    }

    private static async Task<HttpResponseMessage> IssueCheckoutAsync(
        HttpClient client,
        string path,
        BrowserSession session,
        string? checkoutCookies = null,
        string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        AddBrowserSession(request, session, checkoutCookies);
        if (forwardedFor is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        }
        HttpResponseMessage response = await client.SendAsync(request);
        request.Dispose();
        return response;
    }

    private static async Task<BrowserSession> IssueBrowserSessionAsync(HttpClient client, string path = "/auth/status")
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        string[] cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        string cookie = cookies
            .First(value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        int end = cookie.IndexOf(';');
        string token = Uri.UnescapeDataString(cookie["XSRF-TOKEN=".Length..end]);
        string cookieHeader = string.Join("; ", cookies.Select(value => value.Split(';', 2)[0]));
        return new(token, cookieHeader);
    }

    private static void AddBrowserSession(
        HttpRequestMessage request,
        BrowserSession session,
        string? checkoutCookies = null,
        string? fetchSite = "same-origin")
    {
        request.Headers.Add("Cookie", checkoutCookies is null
            ? session.CookieHeader
            : $"{session.CookieHeader}; {checkoutCookies}");
        if (request.Method != HttpMethod.Get)
        {
            request.Headers.Add("X-CSRF-TOKEN", session.AntiforgeryToken);
        }
        else if (fetchSite is not null)
        {
            request.Headers.Add("Sec-Fetch-Site", fetchSite);
        }
    }

    private static string GetCheckoutCookie(HttpResponseMessage response)
    {
        return string.Join("; ", response.Headers.GetValues("Set-Cookie")
            .Where(value => value.StartsWith("__Secure-islamu-registration-payment-", StringComparison.Ordinal))
            .Select(value => value.Split(';', 2)[0]));
    }

    private static string TamperTicketCookie(string checkoutCookies)
    {
        string[] cookies = checkoutCookies.Split("; ", StringSplitOptions.RemoveEmptyEntries);
        int index = Array.FindIndex(cookies, value => value.StartsWith("__Secure-islamu-registration-payment-checkout=", StringComparison.Ordinal));
        string ticket = cookies[index];
        cookies[index] = ticket[..^1] + (ticket[^1] == 'A' ? 'B' : 'A');
        return string.Join("; ", cookies);
    }

    private static string ReplaceCheckoutSession(string checkoutCookies)
    {
        string[] cookies = checkoutCookies.Split("; ", StringSplitOptions.RemoveEmptyEntries);
        int index = Array.FindIndex(cookies, value => value.StartsWith("__Secure-islamu-registration-payment-session=", StringComparison.Ordinal));
        cookies[index] = "__Secure-islamu-registration-payment-session=different-session";
        return string.Join("; ", cookies);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IEventApiClient apiClient,
        TimeProvider? timeProvider = null,
        bool requireRedis = false,
        IConnectionMultiplexer? redis = null,
        int? checkoutPermitLimit = null,
        string? publicBaseUrl = null) =>
        new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            if (publicBaseUrl is not null)
            {
                builder.UseSetting("PublicBaseUrl", publicBaseUrl);
            }
            if (checkoutPermitLimit is not null)
            {
                builder.UseSetting("RateLimiting:DisableInTesting", "false");
                builder.UseSetting("RateLimiting:RegistrationPaymentCheckoutIssue:PermitLimit", checkoutPermitLimit.Value.ToString());
                builder.UseSetting("RateLimiting:RegistrationPaymentCheckoutIssue:WindowSeconds", "60");
                builder.UseSetting("ForwardedHeadersTrust:TrustLoopbackProxy", "true");
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEventApiClient>();
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton(apiClient);
                if (redis is not null)
                {
                    services.AddSingleton(redis);
                }
                if (timeProvider is not null)
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton(timeProvider);
                }

                services.RemoveAll<RegistrationPaymentCheckoutTicketStoreOptions>();
                services.AddSingleton(new RegistrationPaymentCheckoutTicketStoreOptions(requireRedis));
            });
        });

    private static ApiException ApiFailure(int statusCode) => new(
        "Access denied.", statusCode, string.Empty,
        new Dictionary<string, IEnumerable<string>>(), null);

    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = new(utcNow);
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }

    private sealed record BrowserSession(string AntiforgeryToken, string CookieHeader);
}

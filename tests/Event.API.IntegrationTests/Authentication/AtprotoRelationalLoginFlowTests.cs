// ABOUTME: Proves Redis-free Production OAuth login across BFF replicas with actual browser cookie jars and PostgreSQL.
// ABOUTME: Covers same-origin cookies and unrelated-domain handoffs through real API session verification and persistence.

extern alias bff;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using BffAuth = bff::Explore.Blazor.Services.Auth;
using static Event.API.IntegrationTests.Authentication.AtprotoRelationalLoginFixture;

namespace Event.API.IntegrationTests.Authentication;

[ClassDataSource<AtprotoRelationalLoginFixture>(Shared = SharedType.PerClass)]
[NotInParallel("AtprotoTransientApi")]
public sealed class AtprotoRelationalLoginFlowTests(AtprotoRelationalLoginFixture fixture)
{
    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task ProductionReplicasBindRelationalLoginCookiesToLiveOriginProof(bool crossHost, bool expireDuringEnrichment)
    {
        var clock = new ProofClock();
        await using var first = fixture.CreateBff().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            if (expireDuringEnrichment)
                services.AddHttpClient("AdminAuthority")
                    .AddHttpMessageHandler(() => new ExpireAfterAuthorityResponse(clock));
        }));
        await using var second = fixture.CreateBff(rotateKeys: true);
        string origin = crossHost ? AtprotoRelationalLoginFixture.TenantOrigin : AtprotoRelationalLoginFixture.CanonicalOrigin;
        var browser = new CookieContainer();
        using var login = BrowserClient(first, origin, browser);
        using var callbackClient = BrowserClient(second, AtprotoRelationalLoginFixture.CanonicalOrigin, browser);
        foreach (var replica in new[] { first, second })
        {
            await Assert.That(replica.Services.GetRequiredService<IHostEnvironment>().IsProduction()).IsTrue();
            await Assert.That(replica.Services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(IConnectionMultiplexer))).IsFalse();
        }

        using var status = await login.GetAsync("/auth/status");
        await Assert.That(status.StatusCode).IsEqualTo(HttpStatusCode.OK);
        string xsrf = Uri.UnescapeDataString(browser.GetCookies(new Uri(origin))["XSRF-TOKEN"]!.Value);
        using var challengeRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
        {
            Content = JsonContent.Create(new { handle = "alice.example", classification = "person", returnPath = "/events?source=atproto" })
        };
        challengeRequest.Headers.Add("X-CSRF-TOKEN", xsrf);
        challengeRequest.Headers.Add("Origin", origin);
        using var challenge = await login.SendAsync(challengeRequest);
        await Assert.That(challenge.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(fixture.External.State).IsNotNull();
        await Assert.That(browser.GetCookies(new Uri(origin))[BffAuth.AtprotoBrowserProof.CookieName]).IsNotNull();
        using var challengeBody = JsonDocument.Parse(await challenge.Content.ReadAsStringAsync());
        await Assert.That(challengeBody.RootElement.GetProperty("authorizationUrl").GetString()).StartsWith("https://issuer.example/oauth/authorize?");

        int beforeVerification = fixture.External.VerifiedPdsRequests;
        await Assert.That(second.Services.GetRequiredService<BffAuth.AtprotoClientKeyProvider>().ActiveKeyId)
            .IsNotEqualTo(first.Services.GetRequiredService<BffAuth.AtprotoClientKeyProvider>().ActiveKeyId);
        string callbackPath = "/signin-atproto?code=" + Uri.EscapeDataString(fixture.External.AuthorizationCode)
            + "&state=" + Uri.EscapeDataString(fixture.External.State!) + "&iss=" + Uri.EscapeDataString("https://issuer.example");
        if (!crossHost)
        {
            using var thief = BrowserClient(second, origin, new CookieContainer());
            using var stolenCallback = await thief.GetAsync(callbackPath);
            await AssertRejectedWithoutCookie(stolenCallback);
            await Assert.That(fixture.External.VerifiedPdsRequests).IsEqualTo(beforeVerification);
        }
        using var callback = await callbackClient.GetAsync(callbackPath);
        await Assert.That(callback.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(callback.Headers.CacheControl?.NoStore).IsTrue();
        await Assert.That(fixture.External.VerifiedPdsRequests).IsGreaterThan(beforeVerification);
        await Assert.That(fixture.External.TokenClientKeyId)
            .IsEqualTo(first.Services.GetRequiredService<BffAuth.AtprotoClientKeyProvider>().ActiveKeyId);
        if (crossHost)
        {
            await Assert.That(browser.GetCookies(new Uri(AtprotoRelationalLoginFixture.CanonicalOrigin))[".AspNetCore.Cookies"]).IsNull();
            var handoff = callback.Headers.Location!;
            await Assert.That(handoff.GetLeftPart(UriPartial.Authority)).IsEqualTo(origin);
            await Assert.That(handoff.AbsolutePath).IsEqualTo("/auth/atproto/handoff");
            using var thief = BrowserClient(second, origin, new CookieContainer());
            using var stolenHandoff = await thief.GetAsync(handoff.PathAndQuery);
            await AssertRejectedWithoutCookie(stolenHandoff);
            using var completion = await login.GetAsync(handoff.PathAndQuery);
            if (expireDuringEnrichment)
            {
                await Assert.That(clock.GetUtcNow()).IsGreaterThan(DateTimeOffset.UtcNow.AddMinutes(15));
                await Assert.That(completion.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
                await Assert.That(completion.Headers.TryGetValues("Set-Cookie", out var cookies)
                    && cookies.Any(cookie => cookie.StartsWith(".AspNetCore.Cookies", StringComparison.Ordinal))).IsFalse();
                await Assert.That(browser.GetCookies(new Uri(origin))[".AspNetCore.Cookies"]).IsNull();
                return;
            }
            await Assert.That(completion.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
            await Assert.That(completion.Headers.Location?.OriginalString).IsEqualTo("/events?source=atproto");
        }
        else await Assert.That(callback.Headers.Location?.OriginalString).IsEqualTo("/events?source=atproto");

        var authCookie = browser.GetCookies(new Uri(origin))[".AspNetCore.Cookies"];
        await Assert.That(authCookie).IsNotNull();
        await Assert.That(authCookie!.Secure).IsTrue();
        await Assert.That(authCookie.HttpOnly).IsTrue();
        var format = first.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme).TicketDataFormat;
        var ticket = format.Unprotect(Uri.UnescapeDataString(authCookie.Value));
        await Assert.That(ticket).IsNotNull();
        await Assert.That(ticket!.Principal.FindFirstValue("did")).IsEqualTo(AtprotoRelationalLoginFixture.ExternalAtprotoTransport.Did);
        await Assert.That(ticket.Principal.FindFirstValue("tenant_id")).IsEqualTo(fixture.TenantId.ToString("D"));
        string platformToken = ticket.Properties.GetTokenValue("access_token")!;
        string visible = authCookie.Value + callback.Headers.Location + await challenge.Content.ReadAsStringAsync();
        foreach (string secret in new[] { platformToken, fixture.External.AccessToken, fixture.External.RefreshToken })
            await Assert.That(visible.Contains(secret, StringComparison.Ordinal)).IsFalse();

        using var authenticatedReplica = BrowserClient(second, origin, browser);
        using var authenticatedStatus = await authenticatedReplica.GetAsync("/auth/status");
        using var body = JsonDocument.Parse(await authenticatedStatus.Content.ReadAsStringAsync());
        await Assert.That(body.RootElement.GetProperty("isAuthenticated").GetBoolean()).IsTrue();
        using var otherBrowser = BrowserClient(second, origin, new CookieContainer());
        using var anonymousStatus = await otherBrowser.GetAsync("/auth/status");
        using var anonymousBody = JsonDocument.Parse(await anonymousStatus.Content.ReadAsStringAsync());
        await Assert.That(anonymousBody.RootElement.GetProperty("isAuthenticated").GetBoolean()).IsFalse();

        using var api = fixture.Api.CreateClient(new() { BaseAddress = new("https://api.test"), AllowAutoRedirect = false });
        using var persistedRequest = new HttpRequestMessage(HttpMethod.Get, BffAuth.AtprotoBootstrapAssertionService.SessionBridgePath);
        persistedRequest.Headers.Authorization = new("Bearer", platformToken);
        persistedRequest.Headers.Add("X-Tenant-Slug", fixture.TenantSlug);
        persistedRequest.Headers.Add(BffAuth.AtprotoBootstrapAssertionService.SessionBridgeHeaderName,
            first.Services.GetRequiredService<BffAuth.AtprotoBootstrapAssertionService>().IssueSessionBridge(fixture.TenantId,
                Guid.Parse(ticket.Principal.FindFirstValue("sub")!), AtprotoRelationalLoginFixture.ExternalAtprotoTransport.Did, HttpMethod.Get));
        using var persisted = await api.SendAsync(persistedRequest);
        await Assert.That(persisted.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var persistedBody = JsonDocument.Parse(await persisted.Content.ReadAsStringAsync());
        await Assert.That(persistedBody.RootElement.GetProperty("did").GetString()).IsEqualTo(AtprotoRelationalLoginFixture.ExternalAtprotoTransport.Did);
    }

    private static async Task AssertRejectedWithoutCookie(HttpResponseMessage response)
    {
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location?.OriginalString).StartsWith("/login?");
        await Assert.That(response.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(cookie => cookie.StartsWith(".AspNetCore.Cookies", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ParallelReplicaChallenges_CompleteOutOfOrderOnlyForTheRetainedBrowserProof(bool coldBrowser)
    {
        await using var first = fixture.CreateBff();
        await using var second = fixture.CreateBff(rotateKeys: true);
        var browser = new CookieContainer();
        using var firstClient = BrowserClient(first, CanonicalOrigin, browser);
        using var secondClient = BrowserClient(second, CanonicalOrigin, browser);
        using var status = await firstClient.GetAsync("/auth/status");
        await Assert.That(status.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = second.Services;
        if (!coldBrowser) _ = await StartFlowAsync(firstClient, browser, "/events?attempt=warm", CancellationToken.None);
        string? originalProof = browser.GetCookies(new Uri(CanonicalOrigin))[BffAuth.AtprotoBrowserProof.CookieName]?.Value;

        int entered = 0;
        var bothParRequests = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.External.BeforeParResponse = cancellationToken =>
        {
            if (Interlocked.Increment(ref entered) == 2) bothParRequests.TrySetResult();
            return bothParRequests.Task.WaitAsync(cancellationToken);
        };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        (string Callback, string ReturnPath, string? ProofCookie)[] flows;
        try
        {
            flows = await Task.WhenAll(
                StartFlowAsync(firstClient, browser, "/events?attempt=first", deadline.Token),
                StartFlowAsync(secondClient, browser, "/events?attempt=second", deadline.Token));
        }
        finally
        {
            bothParRequests.TrySetResult();
            fixture.External.BeforeParResponse = null;
        }
        await Assert.That(flows[0].Callback).IsNotEqualTo(flows[1].Callback);
        string keptProof = browser.GetCookies(new Uri(CanonicalOrigin))[BffAuth.AtprotoBrowserProof.CookieName]!.Value;
        if (coldBrowser)
        {
            await Assert.That(flows[0].ProofCookie).IsNotNull();
            await Assert.That(flows[1].ProofCookie).IsNotNull();
            await Assert.That(flows[0].ProofCookie).IsNotEqualTo(flows[1].ProofCookie);
        }
        else
        {
            await Assert.That(keptProof).IsEqualTo(originalProof);
            await Assert.That(flows.All(flow => flow.ProofCookie is null)).IsTrue();
        }

        int successes = 0;
        foreach (int index in new[] { 1, 0 })
        {
            var flow = flows[index];
            using var callback = await (index == 0 ? secondClient : firstClient).GetAsync(flow.Callback);
            if (coldBrowser && flow.ProofCookie != keptProof) await AssertRejectedWithoutCookie(callback);
            else
            {
                await Assert.That(callback.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
                await Assert.That(callback.Headers.Location?.OriginalString).IsEqualTo(flow.ReturnPath);
                successes++;
            }
            await Assert.That(browser.GetCookies(new Uri(CanonicalOrigin))[BffAuth.AtprotoBrowserProof.CookieName]!.Value)
                .IsEqualTo(keptProof);
        }
        await Assert.That(successes).IsEqualTo(coldBrowser ? 1 : 2);
    }

    private async Task<(string Callback, string ReturnPath, string? ProofCookie)> StartFlowAsync(
        HttpClient client, CookieContainer browser, string returnPath, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
        {
            Content = JsonContent.Create(new { handle = "alice.example", classification = "person", returnPath })
        };
        request.Headers.Add("X-CSRF-TOKEN", Uri.UnescapeDataString(browser.GetCookies(new Uri(CanonicalOrigin))["XSRF-TOKEN"]!.Value));
        request.Headers.Add("Origin", CanonicalOrigin);
        using var response = await client.SendAsync(request, cancellationToken);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var authorization = fixture.External.ResolveAuthorization(body.RootElement.GetProperty("authorizationUrl").GetString()!);
        string? proofCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Select(value => Microsoft.Net.Http.Headers.SetCookieHeaderValue.Parse(value))
                .SingleOrDefault(cookie => cookie.Name.Value == BffAuth.AtprotoBrowserProof.CookieName)?.Value.Value : null;
        return ("/signin-atproto?code=" + Uri.EscapeDataString(authorization.Code)
            + "&state=" + Uri.EscapeDataString(authorization.State) + "&iss=" + Uri.EscapeDataString("https://issuer.example"),
            returnPath, proofCookie);
    }

    private sealed class ProofClock : TimeProvider
    {
        private long offsetTicks;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddTicks(Interlocked.Read(ref offsetTicks));
        public void ExpireProof() => Interlocked.Exchange(ref offsetTicks, TimeSpan.FromMinutes(16).Ticks);
    }

    private sealed class ExpireAfterAuthorityResponse(ProofClock clock) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode && request.RequestUri!.AbsolutePath == "/api/user/admin-authority")
                clock.ExpireProof();
            return response;
        }
    }

}

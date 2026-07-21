// ABOUTME: Exercises AT Protocol OAuth challenge and callback security at the BFF endpoint boundary.
// ABOUTME: Proves antiforgery, pre-network input rejection, safe redirects, and authorization URL constraints.

using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarpaNet.Identity;
using CarpaNet.OAuth.Storage;
using Explore.Blazor.Authentication;
using Explore.Blazor.Constants;
using Explore.Blazor.Extensions;
using Explore.Blazor.IntegrationTests.Fixtures;
using Explore.Blazor.Services;
using Explore.Blazor.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class AtprotoAuthenticationFlowTests
{
    private const string CanonicalOrigin = "https://events.example.com";
    private const string State = "0123456789abcdef0123456789abcdef";
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    [Test]
    public async Task ChallengeIsPostOnlyAndRejectsMissingAntiforgery()
    {
        await using var factory = CreateFactory();
        var endpoint = GetChallengeEndpoint(factory);
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Should().Equal("POST");

        var response = await InvokeChallengeAsync(
            factory,
            endpoint,
            "{\"handle\":\"alice.example\"}");

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Body.Should().Contain("Antiforgery validation failed");
    }

    [Test]
    public async Task AtprotoChallengeAndCallbackUseRegisteredBoundedHttpRateLimitPolicy()
    {
        await using (var challengeFactory = CreateFactory(enableRealAtprotoRateLimit: true))
        {
            using var client = CreateClient(challengeFactory);
            using var firstContent = new StringContent(
                "{\"handle\":\"alice.example\"}", Encoding.UTF8, "application/json");
            using var first = await client.PostAsync(
                "/auth/atproto/challenge",
                firstContent);
            using var secondContent = new StringContent(
                "{\"handle\":\"alice.example\"}", Encoding.UTF8, "application/json");
            using var second = await client.PostAsync(
                "/auth/atproto/challenge",
                secondContent);

            first.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
            second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        await using (var callbackFactory = CreateFactory(enableRealAtprotoRateLimit: true))
        {
            using var client = CreateClient(callbackFactory);
            using var first = await client.GetAsync("/signin-atproto");
            using var second = await client.GetAsync("/signin-atproto");

            first.StatusCode.Should().Be(HttpStatusCode.Redirect);
            second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }
    }

    [Test]
    public async Task InvalidMissingAndOversizedHandlesAreRejectedWithoutCredentialReflection()
    {
        await using var factory = CreateFactory(bypassAntiforgery: true);
        var sensitiveHandle = $"oauth-access-token.{new string('a', 240)}.example";
        string[] payloads =
        [
            "{}",
            "{\"handle\":\"\"}",
            "{\"handle\":\"single-label\"}",
            "{\"handle\":\"bad..example\"}",
            JsonSerializer.Serialize(new { handle = sensitiveHandle })
        ];

        foreach (var payload in payloads)
        {
            var response = await InvokeChallengeHandlerAsync(factory, payload);

            response.StatusCode.Should().Be(
                StatusCodes.Status400BadRequest,
                "payload {0} must fail with a bounded problem response, but returned {1}",
                payload,
                response.Body);
            response.Location.Should().BeNull();
            response.Body.Should().Contain("ATProto sign-in could not be started.");
            response.Body.Should().NotContain("oauth-access-token");
            response.Body.Should().NotContain("login_hint");
            response.Body.Should().NotContain("credential");
        }

        var oversizedPayload = JsonSerializer.Serialize(new
        {
            handle = "alice.example",
            padding = new string('x', 2200)
        });
        var oversizedResponse = await InvokeChallengeHandlerAsync(factory, oversizedPayload);

        oversizedResponse.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        oversizedResponse.Location.Should().BeNull();
        oversizedResponse.Body.Should().NotContain(new string('x', 64));
    }

    [Test]
    public async Task CallbackWithMissingOrUnknownFlowRedirectsSafelyWithoutEchoingParameters()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        const string code = "oauth-code-secret";
        const string issuer = "https://issuer-secret.example/";

        using var missingResponse = await client.GetAsync("/signin-atproto");
        using var unknownResponse = await client.GetAsync(
            $"/signin-atproto?state={State}&code={code}&iss={Uri.EscapeDataString(issuer)}");

        AssertSafeCallbackFailure(missingResponse, []);
        AssertSafeCallbackFailure(unknownResponse, [State, code, issuer, "oauth-code", "issuer-secret"]);
    }

    [Test]
    public async Task IssuerSubstitutionConsumesStateAndReplayStillRedirectsSafely()
    {
        await using var factory = CreateFactory(withSigningKey: true);
        await StoreFlowStateAsync(factory);
        using var client = CreateClient(factory);
        const string code = "oauth-code-secret";
        const string attackerIssuer = "https://attacker.example/";

        using var substituted = await client.GetAsync(
            $"/signin-atproto?state={State}&code={code}&iss={Uri.EscapeDataString(attackerIssuer)}");
        using var replay = await client.GetAsync(
            $"/signin-atproto?state={State}&code={code}&iss={Uri.EscapeDataString("https://issuer.example/")}");

        AssertSafeCallbackFailure(substituted, [State, code, attackerIssuer]);
        AssertSafeCallbackFailure(replay, [State, code, "https://issuer.example/"]);
    }

    [Test]
    public async Task OAuthErrorCallbackConsumesStateOnceWithoutReflectingProviderContent()
    {
        await using var factory = CreateFactory(withSigningKey: true);
        await StoreFlowStateAsync(factory);
        using var client = CreateClient(factory);
        const string error = "access_denied";
        const string description = "provider-secret-description";
        var issuer = Uri.EscapeDataString("https://issuer.example/");
        var callback = $"/signin-atproto?state={State}&error={error}&error_description={description}&iss={issuer}";

        using var first = await client.GetAsync(callback);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<CacheBackedOAuthStateStore>();
            (await store.GetPinnedKeyIdAsync(State, CancellationToken.None)).Should().BeNull();
        }

        using var replay = await client.GetAsync(callback);

        AssertSafeCallbackFailure(first, [State, error, description, "issuer.example"]);
        AssertSafeCallbackFailure(replay, [State, error, description, "issuer.example"]);
    }

    [Test]
    public async Task MissingDuplicateOrAmbiguousCallbackResultsAreRejectedBeforeStateConsumption()
    {
        await using var factory = CreateFactory(withSigningKey: true);
        await StoreFlowStateAsync(factory);
        using var client = CreateClient(factory);
        var issuer = Uri.EscapeDataString("https://issuer.example/");
        string[] callbacks =
        [
            $"/signin-atproto?state={State}&iss={issuer}",
            $"/signin-atproto?state={State}&code=one&error=access_denied&iss={issuer}",
            $"/signin-atproto?state={State}&code=one&error=bad!&iss={issuer}",
            $"/signin-atproto?state={State}&error=access_denied&code=&iss={issuer}",
            $"/signin-atproto?state={State}&code=one&code=two&iss={issuer}",
            $"/signin-atproto?state={State}&error=access_denied&error=server_error&iss={issuer}"
        ];

        foreach (var callback in callbacks)
        {
            using var response = await client.GetAsync(callback);
            AssertSafeCallbackFailure(response, [State, "access_denied", "server_error", "issuer.example"]);
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<CacheBackedOAuthStateStore>();
        (await store.GetPinnedKeyIdAsync(State, CancellationToken.None)).Should().Be("oauth-active");
    }

    [Test]
    public async Task AuthorizationUrlGuardAllowsOnlyCredentialFreeHttpsCarpaNetRedirects()
    {
        var guard = typeof(AtprotoAuthenticationHandler).GetMethod(
            "ValidateAuthorizationUrl",
            BindingFlags.NonPublic | BindingFlags.Static);
        guard.Should().NotBeNull();
        const string valid = "https://oauth.example/authorize?client_id=https%3A%2F%2Fevents.example.com%2Foauth%2Fclient-metadata.json&request_uri=urn%3Aexample%3Apar";

        guard!.Invoke(null, [valid]).Should().Be(valid);

        string[] rejected =
        [
            "http://oauth.example/authorize",
            "https://user:password@oauth.example/authorize",
            "https://oauth.example/authorize?login_hint=alice.example",
            "https://oauth.example/authorize#access_token=secret"
        ];
        foreach (var value in rejected)
        {
            Action action = () => guard.Invoke(null, [value]);
            action.Should().Throw<TargetInvocationException>()
                .Where(exception => exception.InnerException is InvalidOperationException);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RealCarpaHttpFlowCreatesDirectOrCrossHostCookie(bool crossHost)
    {
        using var atprotoServer = new HermeticAtprotoServer();
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(crossHost, atprotoServer, apiServer);
        var loginOrigin = crossHost ? "https://tenant.example.com" : CanonicalOrigin;
        using var loginClient = CreateClient(factory, loginOrigin);
        var antiforgeryToken = await IssueAntiforgeryTokenAsync(loginClient);
        using var challengeRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
        {
            Content = new StringContent(
                "{\"handle\":\"alice.example\",\"returnPath\":\"/events?source=atproto\"}",
                Encoding.UTF8,
                "application/json")
        };
        challengeRequest.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);

        using var challenge = await loginClient.SendAsync(challengeRequest);
        challenge.StatusCode.Should().Be(HttpStatusCode.OK);
        atprotoServer.State.Should().NotBeNullOrWhiteSpace(
            "the real CarpaNet PAR request must be reached by the HTTP challenge");
        var challengeBody = await challenge.Content.ReadAsStringAsync();
        challengeBody.Should().NotBeEmpty(
            "the ATProto challenge endpoint must return its authorization URL; response was {0}",
            challenge);
        using var challengeJson = JsonDocument.Parse(challengeBody);
        var authorizationUrl = challengeJson.RootElement.GetProperty("authorizationUrl").GetString();
        authorizationUrl.Should().StartWith("https://issuer.example/oauth/authorize?");
        authorizationUrl.Should().NotContain("login_hint");

        using var callbackClient = CreateClient(factory, CanonicalOrigin);
        var callbackPath = $"/signin-atproto?code=authorization-code&state={Uri.EscapeDataString(atprotoServer.State!)}&iss={Uri.EscapeDataString("https://issuer.example")}";
        using var callback = await callbackClient.GetAsync(callbackPath);
        callback.StatusCode.Should().Be(HttpStatusCode.Redirect);
        callback.Headers.CacheControl?.NoStore.Should().BeTrue();
        apiServer.BridgeCalls.Should().Be(1);

        HttpResponseMessage cookieResponse;
        if (crossHost)
        {
            var handoffLocation = callback.Headers.Location;
            handoffLocation.Should().NotBeNull();
            handoffLocation!.GetLeftPart(UriPartial.Authority).Should().Be(loginOrigin);
            handoffLocation.AbsolutePath.Should().Be("/auth/atproto/handoff");
            handoffLocation.Query.Should().NotContain("access_token");
            cookieResponse = await loginClient.GetAsync(handoffLocation.PathAndQuery);
        }
        else
        {
            callback.Headers.Location?.OriginalString.Should().Be("/events?source=atproto");
            cookieResponse = callback;
        }

        using (cookieResponse)
        {
            cookieResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
            cookieResponse.Headers.Location?.OriginalString.Should().Be("/events?source=atproto");
            cookieResponse.Headers.GetValues("Set-Cookie")
                .Should().Contain(value => value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
            var browserVisible = string.Join('\n', cookieResponse.Headers.SelectMany(header => header.Value));
            browserVisible.Should().NotContain("pds-access-token");
            browserVisible.Should().NotContain("pds-refresh-token");
            browserVisible.Should().NotContain(HermeticBffApiServer.PlatformAccessToken);
        }
    }

    [Test]
    public async Task RepeatedChallengesReuseIdentityResolutionButKeepAuthorizationFlowsIndependent()
    {
        using var atprotoServer = new HermeticAtprotoServer();
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);
        using var client = CreateClient(factory);
        var antiforgeryToken = await IssueAntiforgeryTokenAsync(client);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
            {
                Content = new StringContent(
                    "{\"handle\":\"alice.example\"}",
                    Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);

            using var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        atprotoServer.DnsRequestCount.Should().Be(1);
        atprotoServer.DidDocumentRequestCount.Should().Be(1);
        atprotoServer.PushedAuthorizationRequestCount.Should().Be(2);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        bool withSigningKey = false,
        bool bypassAntiforgery = false,
        bool enableRealAtprotoRateLimit = false)
    {
        var factory = new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Atproto:PublicUrl", CanonicalOrigin);
            builder.UseSetting("Atproto:CallbackPath", "/signin-atproto");
            builder.UseSetting("Atproto:UseSingleNodeMemoryStore", "true");
            builder.UseSetting("Explore:MultiTenancy:DefaultTenantId", TenantId.ToString("D"));
            builder.UseSetting("Explore:MultiTenancy:DefaultTenant", "default");
            if (enableRealAtprotoRateLimit)
            {
                builder.UseSetting("RateLimiting:DisableInTesting", "false");
                builder.UseSetting("RateLimiting:AtprotoAuthentication:PermitLimit", "1");
                builder.UseSetting("RateLimiting:AtprotoAuthentication:WindowSeconds", "60");
            }
            if (withSigningKey)
            {
                builder.UseSetting(AtprotoClientKeyProvider.ConfigurationKey, CreatePrivateJwks());
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddAuthentication()
                    .AddScheme<AtprotoAuthenticationOptions, AtprotoAuthenticationHandler>(
                        AuthSchemeNames.Atproto,
                        _ => { });

                services.RemoveAll<IBffProviderReadinessService>();
                var readiness = Substitute.For<IBffProviderReadinessService>();
                readiness.IsProviderReadyAsync(AuthSchemeNames.Atproto, Arg.Any<CancellationToken>())
                    .Returns(true);
                services.AddSingleton(readiness);

                if (bypassAntiforgery)
                {
                    services.RemoveAll<IBffSelfCallTokenService>();
                    var selfCall = Substitute.For<IBffSelfCallTokenService>();
                    selfCall.Validate(Arg.Any<HttpContext>()).Returns(true);
                    services.AddSingleton(selfCall);
                }

                services.RemoveAll<IBffOnboardingStatusProvider>();
                var onboarding = Substitute.For<IBffOnboardingStatusProvider>();
                onboarding.GetStatusAsync(Arg.Any<CancellationToken>())
                    .Returns(new BffOnboardingStatus(true, false, true));
                services.AddSingleton(onboarding);
            });
        });

        return factory;
    }

    private static WebApplicationFactory<Program> CreateHappyPathFactory(
        bool crossHost,
        HermeticAtprotoServer atprotoServer,
        HermeticBffApiServer apiServer) =>
        new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Atproto:PublicUrl", CanonicalOrigin);
            builder.UseSetting("Atproto:CallbackPath", "/signin-atproto");
            builder.UseSetting("Atproto:UseSingleNodeMemoryStore", "true");
            builder.UseSetting(AtprotoClientKeyProvider.ConfigurationKey, CreatePrivateJwks());
            builder.UseSetting("Explore:MultiTenancy:DefaultTenantId", TenantId.ToString("D"));
            builder.UseSetting("Explore:MultiTenancy:DefaultTenant", "default");
            if (crossHost)
            {
                builder.UseSetting("Atproto:TenantOrigins:0:Origin", "https://tenant.example.com");
                builder.UseSetting("Atproto:TenantOrigins:0:TenantId", TenantId.ToString("D"));
                builder.UseSetting("Atproto:TenantOrigins:0:TenantSlug", "default");
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnectionMultiplexer>();
                services.RemoveAll<IAtprotoOAuthTransportFactory>();
                services.AddSingleton<IAtprotoOAuthTransportFactory>(
                    new HermeticAtprotoTransportFactory(atprotoServer));
                services.AddHttpClient(ApiBackedOAuthSessionStore.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => apiServer);
                services.AddHttpClient("AdminAuthority")
                    .ConfigurePrimaryHttpMessageHandler(() => apiServer);
                services.AddScoped<BffAdminClaimsTransformation>();
                services.AddAuthentication()
                    .AddScheme<AtprotoAuthenticationOptions, AtprotoAuthenticationHandler>(
                        AuthSchemeNames.Atproto,
                        _ => { });

                services.RemoveAll<IBffProviderReadinessService>();
                var readiness = Substitute.For<IBffProviderReadinessService>();
                readiness.IsProviderReadyAsync(AuthSchemeNames.Atproto, Arg.Any<CancellationToken>())
                    .Returns(true);
                services.AddSingleton(readiness);

                services.RemoveAll<IBffOnboardingStatusProvider>();
                var onboarding = Substitute.For<IBffOnboardingStatusProvider>();
                onboarding.GetStatusAsync(Arg.Any<CancellationToken>())
                    .Returns(new BffOnboardingStatus(true, false, true));
                services.AddSingleton(onboarding);
            });
        });

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        CreateClient(factory, CanonicalOrigin);

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string origin) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri(origin),
            HandleCookies = true
        });

    private static async Task<string> IssueAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/auth/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        var token = values!.Select(ReadXsrfToken).FirstOrDefault(value => value is not null);
        token.Should().NotBeNullOrWhiteSpace();
        return token!;
    }

    private static string? ReadXsrfToken(string setCookie)
    {
        const string prefix = "XSRF-TOKEN=";
        if (!setCookie.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var end = setCookie.IndexOf(';', prefix.Length);
        var value = end < 0 ? setCookie[prefix.Length..] : setCookie[prefix.Length..end];
        return Uri.UnescapeDataString(value);
    }

    private static RouteEndpoint GetChallengeEndpoint(WebApplicationFactory<Program> factory) =>
        factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/auth/atproto/challenge");

    private static async Task<EndpointResponse> InvokeChallengeAsync(
        WebApplicationFactory<Program> factory,
        RouteEndpoint endpoint,
        string payload)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.SetEndpoint(endpoint);
        context.Request.Method = HttpMethods.Post;
        context.Request.Scheme = Uri.UriSchemeHttps;
        context.Request.Host = new HostString("events.example.com");
        context.Request.Path = "/auth/atproto/challenge";
        context.Request.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(payload);
        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return new(
            context.Response.StatusCode,
            context.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null,
            await reader.ReadToEndAsync());
    }

    private static async Task<EndpointResponse> InvokeChallengeHandlerAsync(
        WebApplicationFactory<Program> factory,
        string payload)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = CreateChallengeContext(scope.ServiceProvider, payload);
        var handler = typeof(BffAuthEndpoints).GetMethod(
            "HandleAtprotoChallengeAsync",
            BindingFlags.NonPublic | BindingFlags.Static);
        handler.Should().NotBeNull();

        var resultTask = handler!.Invoke(null, [context]) as Task<IResult>;
        resultTask.Should().NotBeNull();
        var result = await resultTask!;
        await result.ExecuteAsync(context);

        return await ReadEndpointResponseAsync(context);
    }

    private static DefaultHttpContext CreateChallengeContext(IServiceProvider services, string payload)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Scheme = Uri.UriSchemeHttps;
        context.Request.Host = new HostString("events.example.com");
        context.Request.Path = "/auth/atproto/challenge";
        context.Request.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(payload);
        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<EndpointResponse> ReadEndpointResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return new(
            context.Response.StatusCode,
            context.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null,
            await reader.ReadToEndAsync());
    }

    private static void AssertSafeCallbackFailure(
        HttpResponseMessage response,
        IReadOnlyCollection<string> forbiddenValues)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
        var location = response.Headers.Location?.OriginalString;
        location.Should().NotBeNull();
        location.Should().StartWith("/login?");
        location.Should().Contain("provider=atproto");
        location.Should().Contain("challengeError=1");
        location.Should().Contain("errorCode=atproto_callback_failed");
        location.Should().Contain("correlationId=");
        foreach (var value in forbiddenValues)
        {
            location.Should().NotContain(value);
        }

        response.Content.Headers.ContentLength.Should().Be(0);
    }

    private static async Task StoreFlowStateAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<CacheBackedOAuthStateStore>();
        await store.StoreAsync(State, new OAuthStateData
        {
            Issuer = "https://issuer.example/",
            PdsUrl = "https://pds.example/",
            AppState = CacheBackedOAuthStateStore.EncodeAppState(new AtprotoOAuthFlowSeed(
                "did:plc:alice",
                new Uri("https://pds.example/"),
                TenantId,
                "default",
                new Uri($"{CanonicalOrigin}/"),
                "/events",
                "oauth-active")),
            Verifier = "verifier",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(4)
        });
    }

    private static string CreatePrivateJwks()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(true);
        return JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new
                {
                    kty = "EC",
                    crv = "P-256",
                    x = Base64Url(parameters.Q.X!),
                    y = Base64Url(parameters.Q.Y!),
                    d = Base64Url(parameters.D!),
                    kid = "oauth-active",
                    use = "sig",
                    alg = "ES256",
                    status = "active"
                }
            }
        });
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class HermeticAtprotoTransportFactory(HermeticAtprotoServer server)
        : IAtprotoOAuthTransportFactory
    {
        public HttpMessageHandler CreatePrimaryHandler(
            Explore.Atproto.Transport.AtprotoOutboundPolicy policy,
            TimeSpan connectTimeout) => server;

        public IDnsResolver CreateDnsResolver() => new HermeticDnsResolver(server);
    }

    private sealed class HermeticDnsResolver(HermeticAtprotoServer server) : IDnsResolver
    {
        public Task<IReadOnlyList<string>> GetTxtRecordsAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            server.DnsRequestCount++;
            return Task.FromResult<IReadOnlyList<string>>(
                name == "_atproto.alice.example" ? ["did=did:plc:alice"] : []);
        }
    }

    private sealed class HermeticAtprotoServer : HttpMessageHandler
    {
        public string? State { get; private set; }
        public int DnsRequestCount { get; set; }
        public int DidDocumentRequestCount { get; private set; }
        public int PushedAuthorizationRequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            if (request.Method == HttpMethod.Get
                && uri.Host == "plc.directory"
                && uri.AbsolutePath == "/did:plc:alice")
            {
                DidDocumentRequestCount++;
                return Json("""
                    {"id":"did:plc:alice","alsoKnownAs":["at://alice.example"],"service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"https://pds.example"}]}
                    """);
            }

            if (request.Method == HttpMethod.Get
                && uri.Host == "pds.example"
                && uri.AbsolutePath == "/.well-known/oauth-protected-resource")
            {
                return Json("{\"authorization_servers\":[\"https://issuer.example\"]}");
            }

            if (request.Method == HttpMethod.Get
                && uri.Host == "issuer.example"
                && uri.AbsolutePath == "/.well-known/oauth-authorization-server")
            {
                return Json(AuthorizationServerMetadata);
            }

            if (request.Method == HttpMethod.Post && uri.AbsolutePath == "/oauth/par")
            {
                PushedAuthorizationRequestCount++;
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                State = ParseForm(body)["state"];
                return JsonWithNonce("{\"request_uri\":\"urn:ietf:params:oauth:request_uri:hermetic\",\"expires_in\":90}");
            }

            if (request.Method == HttpMethod.Post && uri.AbsolutePath == "/oauth/token")
            {
                return JsonWithNonce("{\"access_token\":\"pds-access-token\",\"token_type\":\"DPoP\",\"expires_in\":3600,\"refresh_token\":\"pds-refresh-token\",\"scope\":\"atproto transition:generic\",\"sub\":\"did:plc:alice\"}");
            }

            throw new InvalidOperationException($"Unexpected hermetic ATProto request: {request.Method} {uri}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private static Dictionary<string, string> ParseForm(string body) =>
            body.Split('&').Select(part => part.Split('=', 2)).ToDictionary(
                parts => Uri.UnescapeDataString(parts[0].Replace('+', ' ')),
                parts => Uri.UnescapeDataString(parts[1].Replace('+', ' ')),
                StringComparer.Ordinal);

        private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(value, Encoding.UTF8, "application/json")
        };

        private static HttpResponseMessage JsonWithNonce(string value)
        {
            var response = Json(value);
            response.Headers.TryAddWithoutValidation("DPoP-Nonce", "hermetic-nonce");
            return response;
        }

        private const string AuthorizationServerMetadata = """
            {"issuer":"https://issuer.example","authorization_endpoint":"https://issuer.example/oauth/authorize","token_endpoint":"https://issuer.example/oauth/token","pushed_authorization_request_endpoint":"https://issuer.example/oauth/par","revocation_endpoint":"https://issuer.example/oauth/revoke","require_pushed_authorization_requests":true,"token_endpoint_auth_methods_supported":["private_key_jwt"],"token_endpoint_auth_signing_alg_values_supported":["ES256"],"dpop_signing_alg_values_supported":["ES256"],"grant_types_supported":["authorization_code","refresh_token"],"response_types_supported":["code"],"code_challenge_methods_supported":["S256"],"authorization_response_iss_parameter_supported":true,"client_id_metadata_document_supported":true,"scopes_supported":["atproto"],"require_request_uri_registration":true}
            """;
    }

    private sealed class HermeticBffApiServer : HttpMessageHandler
    {
        public const string PlatformAccessToken = "opaque-platform-access-token";
        private static readonly Guid UserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");
        public int BridgeCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == AtprotoBootstrapAssertionService.BridgePath)
            {
                BridgeCalls++;
                return Task.FromResult(Json(JsonSerializer.Serialize(new
                {
                    userId = UserId,
                    did = "did:plc:alice",
                    accessToken = PlatformAccessToken,
                    expiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
                })));
            }

            return Task.FromResult(Json("{\"hasAnyAuthority\":false,\"isInstanceAdmin\":false,\"adminTenantIds\":[],\"adminOrganizationIds\":[],\"adminGroupIds\":[]}"));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(value, Encoding.UTF8, "application/json")
        };
    }

    private sealed record EndpointResponse(int StatusCode, string? Location, string Body);
}

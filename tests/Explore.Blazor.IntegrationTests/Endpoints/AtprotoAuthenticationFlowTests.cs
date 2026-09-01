// ABOUTME: Exercises AT Protocol OAuth challenge and callback security at the BFF endpoint boundary.
// ABOUTME: Proves antiforgery, pre-network input rejection, safe redirects, and authorization URL constraints.

using System.Net;
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
    private static readonly Guid CanonicalActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000003");
    private static readonly Guid ExpectedCanonicalActorConcurrencyStamp = Guid.Parse("018e4e5c-7f00-7000-8000-000000000004");

    [Test]
    public async Task ChallengeIsPostOnlyAndRejectsMissingAntiforgery()
    {
        await using var factory = CreateFactory();
        var endpoint = GetChallengeEndpoint(factory);
        await Assert.That(endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["POST"])).IsTrue();

        var response = await InvokeChallengeAsync(
            factory,
            endpoint,
            "{\"handle\":\"alice.example\",\"classification\":\"person\"}");

        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(response.Body).Contains("Antiforgery validation failed");
    }

    [Test]
    public async Task AtprotoChallengeAndCallbackUseRegisteredBoundedHttpRateLimitPolicy()
    {
        await using (var challengeFactory = CreateFactory(enableRealAtprotoRateLimit: true))
        {
            using var client = CreateClient(challengeFactory);
            using var firstContent = new StringContent(
                "{\"handle\":\"alice.example\",\"classification\":\"person\"}", Encoding.UTF8, "application/json");
            using var first = await client.PostAsync(
                "/auth/atproto/challenge",
                firstContent);
            using var secondContent = new StringContent(
                "{\"handle\":\"alice.example\",\"classification\":\"person\"}", Encoding.UTF8, "application/json");
            using var second = await client.PostAsync(
                "/auth/atproto/challenge",
                secondContent);

            await Assert.That(first.StatusCode).IsNotEqualTo(HttpStatusCode.TooManyRequests);
            await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        }

        await using (var callbackFactory = CreateFactory(enableRealAtprotoRateLimit: true))
        {
            using var client = CreateClient(callbackFactory);
            using var first = await client.GetAsync("/signin-atproto");
            using var second = await client.GetAsync("/signin-atproto");

            await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
            await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
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
            "{\"handle\":\"\",\"classification\":\"person\"}",
            "{\"handle\":\"single-label\",\"classification\":\"person\"}",
            "{\"handle\":\"bad..example\",\"classification\":\"person\"}",
            JsonSerializer.Serialize(new { handle = sensitiveHandle, classification = "person" })
        ];

        foreach (var payload in payloads)
        {
            var response = await InvokeChallengeAsync(factory, GetChallengeEndpoint(factory), payload);

            await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest)
                .Because($"payload {payload} must fail with a bounded problem response, but returned {response.Body}");
            await Assert.That(response.Location).IsNull();
            await Assert.That(response.Body).Contains("ATProto sign-in could not be started.");
            await Assert.That(response.Body).DoesNotContain("oauth-access-token");
            await Assert.That(response.Body).DoesNotContain("login_hint");
            await Assert.That(response.Body).DoesNotContain("credential");
        }

        var oversizedPayload = JsonSerializer.Serialize(new
        {
            handle = "alice.example",
            classification = "person",
            padding = new string('x', 2200)
        });
        var oversizedResponse = await InvokeChallengeAsync(
            factory,
            GetChallengeEndpoint(factory),
            oversizedPayload);

        await Assert.That(oversizedResponse.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(oversizedResponse.Location).IsNull();
        await Assert.That(oversizedResponse.Body).DoesNotContain(new string('x', 64));
    }

    [Test]
    public async Task MissingOrUnknownClassificationIsRejectedBeforeOAuthStateCreation()
    {
        using var atprotoServer = new HermeticAtprotoServer();
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);

        foreach (var payload in new[]
                 {
                     "{\"handle\":\"alice.example\"}",
                     "{\"handle\":\"alice.example\",\"classification\":\"bot\"}"
                 })
        {
            var response = await InvokeChallengeAsync(factory, GetChallengeEndpoint(factory), payload);

            await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
            await Assert.That(response.Location).IsNull();
            await Assert.That(response.Body).Contains("ATProto sign-in could not be started.");
        }
    }

    [Test]
    public async Task ConfiguredKeycloakPendingRejectsAtprotoChallengeAfterValidAntiforgery()
    {
        await using var factory = CreateFactory(
            bypassAntiforgery: false,
            onboardingStatus: PendingConfigured("Keycloak"));
        using var client = CreateClient(factory);
        var antiforgeryToken = await IssueAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
        {
            Content = new StringContent(
                "{\"handle\":\"alice.example\",\"classification\":\"person\"}",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);

        using var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(response.Headers.Location).IsNull();
        await Assert.That(response.Headers.CacheControl?.NoStore).IsTrue();
    }

    [Test]
    public async Task CanonicalActorTargetChallengeRoundTripsOnlyAsProtectedBootstrapBinding()
    {
        using var atprotoServer = new HermeticAtprotoServer();
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);
        using var client = CreateClient(factory);
        var antiforgeryToken = await IssueAntiforgeryTokenAsync(client);
        using var challengeRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                handle = "alice.example",
                classification = "person",
                canonicalActorId = CanonicalActorId,
                expectedCanonicalActorConcurrencyStamp = ExpectedCanonicalActorConcurrencyStamp
            }), Encoding.UTF8, "application/json")
        };
        challengeRequest.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);

        using var challenge = await client.SendAsync(challengeRequest);
        await Assert.That(challenge.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var callbackClient = CreateClient(factory, CanonicalOrigin);
        using var callback = await callbackClient.GetAsync(
            $"/signin-atproto?code=authorization-code&state={Uri.EscapeDataString(atprotoServer.State!)}&iss={Uri.EscapeDataString("https://issuer.example")}");

        await Assert.That(callback.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(apiServer.CanonicalActorId).IsEqualTo(CanonicalActorId);
        await Assert.That(apiServer.ExpectedCanonicalActorConcurrencyStamp).IsEqualTo(ExpectedCanonicalActorConcurrencyStamp);
    }

    [Test]
    public async Task CanonicalActorTargetChallengeRejectsHalfOrEmptyPairBeforeOAuthStateCreation()
    {
        using var atprotoServer = new HermeticAtprotoServer();
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);

        foreach (var payload in new[]
                 {
                     JsonSerializer.Serialize(new { handle = "alice.example", classification = "person", canonicalActorId = CanonicalActorId }),
                     JsonSerializer.Serialize(new { handle = "alice.example", classification = "person", expectedCanonicalActorConcurrencyStamp = ExpectedCanonicalActorConcurrencyStamp }),
                     JsonSerializer.Serialize(new { handle = "alice.example", classification = "person", canonicalActorId = Guid.Empty, expectedCanonicalActorConcurrencyStamp = ExpectedCanonicalActorConcurrencyStamp })
                 })
        {
            var response = await InvokeChallengeAsync(factory, GetChallengeEndpoint(factory), payload);

            await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
            await Assert.That(atprotoServer.PushedAuthorizationRequestCount).IsEqualTo(0);
        }
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

        await AssertSafeCallbackFailure(missingResponse, []);
        await AssertSafeCallbackFailure(unknownResponse, [State, code, issuer, "oauth-code", "issuer-secret"]);
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

        await AssertSafeCallbackFailure(substituted, [State, code, attackerIssuer]);
        await AssertSafeCallbackFailure(replay, [State, code, "https://issuer.example/"]);
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
            await Assert.That((await store.GetPinnedKeyIdAsync(State, CancellationToken.None))).IsNull();
        }

        using var replay = await client.GetAsync(callback);

        await AssertSafeCallbackFailure(first, [State, error, description, "issuer.example"]);
        await AssertSafeCallbackFailure(replay, [State, error, description, "issuer.example"]);
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
            await AssertSafeCallbackFailure(response, [State, "access_denied", "server_error", "issuer.example"]);
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<CacheBackedOAuthStateStore>();
        await Assert.That((await store.GetPinnedKeyIdAsync(State, CancellationToken.None))).IsEqualTo("oauth-active");
    }

    [Test]
    [Arguments(IdentityDocumentScenario.HttpAuthorizationEndpoint)]
    [Arguments(IdentityDocumentScenario.CredentialedAuthorizationEndpoint)]
    [Arguments(IdentityDocumentScenario.LoginHintAuthorizationEndpoint)]
    [Arguments(IdentityDocumentScenario.TokenFragmentAuthorizationEndpoint)]
    public async Task UnsafeAuthorizationEndpointFailsThroughHttpWithoutCredentialReflection(
        IdentityDocumentScenario scenario)
    {
        using var atprotoServer = new HermeticAtprotoServer(scenario);
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);

        using var response = await StartChallengeAsync(factory);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Headers.Location).IsNull();
        await Assert.That(body).DoesNotContain("login_hint");
        await Assert.That(body).DoesNotContain("access_token");
        await Assert.That(body).DoesNotContain("user:password");
        await Assert.That(apiServer.BridgeCalls).IsEqualTo(0);
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
                "{\"handle\":\"alice.example\",\"returnPath\":\"/events?source=atproto\",\"classification\":\"person\"}",
                Encoding.UTF8,
                "application/json")
        };
        challengeRequest.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);

        using var challenge = await loginClient.SendAsync(challengeRequest);
        await Assert.That(challenge.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(string.IsNullOrWhiteSpace(atprotoServer.State)).IsFalse()
            .Because("the real CarpaNet PAR request must be reached by the HTTP challenge");
        var challengeBody = await challenge.Content.ReadAsStringAsync();
        await Assert.That(challengeBody).IsNotEmpty()
            .Because($"the ATProto challenge endpoint must return its authorization URL; response was {challenge}");
        using var challengeJson = JsonDocument.Parse(challengeBody);
        var authorizationUrl = challengeJson.RootElement.GetProperty("authorizationUrl").GetString();
        await Assert.That(authorizationUrl).StartsWith("https://issuer.example/oauth/authorize?");
        await Assert.That(authorizationUrl).DoesNotContain("login_hint");

        using var callbackClient = CreateClient(factory, CanonicalOrigin);
        var callbackPath = $"/signin-atproto?code=authorization-code&state={Uri.EscapeDataString(atprotoServer.State!)}&iss={Uri.EscapeDataString("https://issuer.example")}";
        using var callback = await callbackClient.GetAsync(callbackPath);
        await Assert.That(callback.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(callback.Headers.CacheControl?.NoStore).IsTrue();
        await Assert.That(apiServer.BridgeCalls).IsEqualTo(1);

        HttpResponseMessage cookieResponse;
        if (crossHost)
        {
            var handoffLocation = callback.Headers.Location;
            await Assert.That(handoffLocation).IsNotNull();
            await Assert.That(handoffLocation!.GetLeftPart(UriPartial.Authority)).IsEqualTo(loginOrigin);
            await Assert.That(handoffLocation.AbsolutePath).IsEqualTo("/auth/atproto/handoff");
            await Assert.That(handoffLocation.Query).DoesNotContain("access_token");
            cookieResponse = await loginClient.GetAsync(handoffLocation.PathAndQuery);
        }
        else
        {
            await Assert.That(callback.Headers.Location?.OriginalString).IsEqualTo("/events?source=atproto");
            cookieResponse = callback;
        }

        using (cookieResponse)
        {
            await Assert.That(cookieResponse.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
            await Assert.That(cookieResponse.Headers.Location?.OriginalString).IsEqualTo("/events?source=atproto");
            await Assert.That(cookieResponse.Headers.GetValues("Set-Cookie")).Contains(value => value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
            var browserVisible = string.Join('\n', cookieResponse.Headers.SelectMany(header => header.Value));
            await Assert.That(browserVisible).DoesNotContain("pds-access-token");
            await Assert.That(browserVisible).DoesNotContain("pds-refresh-token");
            await Assert.That(browserVisible).DoesNotContain(HermeticBffApiServer.PlatformAccessToken);
        }
    }

    [Test]
    public async Task ConfiguredAtprotoPendingClaimsAndRefreshesStatusBeforeIssuingCookieOnce()
    {
        using var atprotoServer = new HermeticAtprotoServer();
        using var apiServer = new HermeticBffApiServer();
        var onboarding = new ClaimCompletingOnboardingStatusProvider("Atproto");
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer, onboarding);

        using var challenge = await StartChallengeAsync(factory);
        await Assert.That(challenge.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var callbackClient = CreateClient(factory, CanonicalOrigin);
        using var callback = await callbackClient.GetAsync(
            $"/signin-atproto?code=authorization-code&state={Uri.EscapeDataString(atprotoServer.State!)}&iss={Uri.EscapeDataString("https://issuer.example")}");

        await Assert.That(callback.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(callback.Headers.Location?.OriginalString).IsEqualTo("/");
        await Assert.That(callback.Headers.GetValues("Set-Cookie")
            .Count(value => value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal))).IsEqualTo(1);
        await Assert.That(onboarding.InvalidationCount).IsEqualTo(1);
        await Assert.That(apiServer.BridgeCalls).IsEqualTo(1);
        await Assert.That(apiServer.SyncCalls).IsEqualTo(1);
        await Assert.That(apiServer.AuthorityCalls).IsEqualTo(1);

        var browserVisible = string.Join('\n', callback.Headers.SelectMany(header => header.Value));
        await Assert.That(browserVisible).DoesNotContain(HermeticBffApiServer.PlatformAccessToken);
        await Assert.That(browserVisible).DoesNotContain("pds-access-token");
        await Assert.That(browserVisible).DoesNotContain("pds-refresh-token");
    }

    [Test]
    public async Task UnsafePostedReturnPathFallsBackToRootWithoutTokenReflection()
    {
        using var atprotoServer = new HermeticAtprotoServer();
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);
        using var client = CreateClient(factory);
        var antiforgeryToken = await IssueAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
        {
            Content = new StringContent(
                "{\"handle\":\"alice.example\",\"returnPath\":\"https://evil.example/steal\",\"classification\":\"person\"}",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        using var challenge = await client.SendAsync(request);
        await Assert.That(challenge.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var callbackClient = CreateClient(factory, CanonicalOrigin);
        using var callback = await callbackClient.GetAsync(
            $"/signin-atproto?code=authorization-code&state={Uri.EscapeDataString(atprotoServer.State!)}&iss={Uri.EscapeDataString("https://issuer.example")}");

        await Assert.That(callback.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(callback.Headers.Location?.OriginalString).IsEqualTo("/");
        var browserVisible = string.Join('\n', callback.Headers.SelectMany(header => header.Value));
        await Assert.That(browserVisible).DoesNotContain("evil.example");
        await Assert.That(browserVisible).DoesNotContain(HermeticBffApiServer.PlatformAccessToken);
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
                    "{\"handle\":\"alice.example\",\"classification\":\"person\"}",
                    Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);

            using var response = await client.SendAsync(request);

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        await Assert.That(atprotoServer.DnsRequestCount).IsEqualTo(1);
        await Assert.That(atprotoServer.DidDocumentRequestCount).IsEqualTo(1);
        await Assert.That(atprotoServer.PushedAuthorizationRequestCount).IsEqualTo(2);
    }

    [Test]
    public async Task HostnameOnlyDidWebIdentityReachesItsPdsAuthorizationServerAndPar()
    {
        using var atprotoServer = new HermeticAtprotoServer(IdentityDocumentScenario.DidWeb);
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);

        using var response = await StartChallengeAsync(factory);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(atprotoServer.DidDocumentRequestCount).IsEqualTo(1);
        await Assert.That(atprotoServer.PushedAuthorizationRequestCount).IsEqualTo(1);
        await Assert.That(apiServer.BridgeCalls).IsEqualTo(0);
    }

    [Test]
    [Arguments(IdentityDocumentScenario.ConflictingHandle)]
    [Arguments(IdentityDocumentScenario.MissingPds)]
    [Arguments(IdentityDocumentScenario.DuplicatePds)]
    [Arguments(IdentityDocumentScenario.NonHttpsPds)]
    [Arguments(IdentityDocumentScenario.InvalidPds)]
    public async Task ConflictingHandleOrInvalidPdsServiceFailsBeforeParAndBridge(
        IdentityDocumentScenario scenario)
    {
        using var atprotoServer = new HermeticAtprotoServer(scenario);
        using var apiServer = new HermeticBffApiServer();
        await using var factory = CreateHappyPathFactory(false, atprotoServer, apiServer);

        using var response = await StartChallengeAsync(factory);

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.OK);
        await Assert.That(atprotoServer.PushedAuthorizationRequestCount).IsEqualTo(0);
        await Assert.That(apiServer.BridgeCalls).IsEqualTo(0);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        bool withSigningKey = false,
        bool bypassAntiforgery = false,
        bool enableRealAtprotoRateLimit = false,
        BffOnboardingStatus? onboardingStatus = null)
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
                    .Returns(onboardingStatus ?? CompletedStatus());
                services.AddSingleton(onboarding);
            });
        });

        return factory;
    }

    private static WebApplicationFactory<Program> CreateHappyPathFactory(
        bool crossHost,
        HermeticAtprotoServer atprotoServer,
        HermeticBffApiServer apiServer,
        IBffOnboardingStatusProvider? onboardingStatusProvider = null) =>
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
                if (onboardingStatusProvider is null)
                {
                    var onboarding = Substitute.For<IBffOnboardingStatusProvider>();
                    onboarding.GetStatusAsync(Arg.Any<CancellationToken>())
                        .Returns(CompletedStatus());
                    onboardingStatusProvider = onboarding;
                }
                services.AddSingleton(onboardingStatusProvider);
            });
        });

    private static BffOnboardingStatus CompletedStatus() => new(
        true,
        "Completed",
        "Interactive",
        null,
        1,
        BffOnboardingDisposition.Completed);

    private static BffOnboardingStatus PendingConfigured(string provider) => new(
        false,
        "Pending",
        "ConfiguredAdministrator",
        provider,
        1,
        BffOnboardingDisposition.ConfiguredAdministratorPending);

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
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.TryGetValues("Set-Cookie", out var values)).IsTrue();
        var token = values!.Select(ReadXsrfToken).FirstOrDefault(value => value is not null);
        await Assert.That(string.IsNullOrWhiteSpace(token)).IsFalse();
        return token!;
    }

    private static async Task<HttpResponseMessage> StartChallengeAsync(
        WebApplicationFactory<Program> factory)
    {
        using var client = CreateClient(factory);
        var antiforgeryToken = await IssueAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/atproto/challenge")
        {
            Content = new StringContent(
                "{\"handle\":\"alice.example\",\"classification\":\"person\"}",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        return await client.SendAsync(request);
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

    private static async Task AssertSafeCallbackFailure(
        HttpResponseMessage response,
        IReadOnlyCollection<string> forbiddenValues)
    {
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.CacheControl?.NoStore).IsTrue();
        var location = response.Headers.Location?.OriginalString;
        await Assert.That(location).IsNotNull();
        await Assert.That(location).StartsWith("/login?");
        await Assert.That(location).Contains("provider=atproto");
        await Assert.That(location).Contains("challengeError=1");
        await Assert.That(location).Contains("errorCode=atproto_callback_failed");
        await Assert.That(location).Contains("correlationId=");
        foreach (var value in forbiddenValues)
        {
            await Assert.That(location).DoesNotContain(value);
        }

        await Assert.That(response.Content.Headers.ContentLength).IsEqualTo(0);
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
                "oauth-active",
                "person")),
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
                name == "_atproto.alice.example" ? [$"did={server.ResolvedDid}"] : []);
        }
    }

    private sealed class HermeticAtprotoServer(
        IdentityDocumentScenario scenario = IdentityDocumentScenario.Valid) : HttpMessageHandler
    {
        public string? State { get; private set; }
        public string ResolvedDid { get; } = scenario == IdentityDocumentScenario.DidWeb
            ? "did:web:alice.example"
            : "did:plc:alice";
        public int DnsRequestCount { get; set; }
        public int DidDocumentRequestCount { get; private set; }
        public int PushedAuthorizationRequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            if (request.Method == HttpMethod.Get
                && ((scenario == IdentityDocumentScenario.DidWeb
                        && uri.Host == "alice.example"
                        && uri.AbsolutePath == "/.well-known/did.json")
                    || (scenario != IdentityDocumentScenario.DidWeb
                        && uri.Host == "plc.directory"
                        && uri.AbsolutePath == "/did:plc:alice")))
            {
                DidDocumentRequestCount++;
                return Json(CreateDidDocument(scenario));
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
                return Json(CreateAuthorizationServerMetadata(scenario));
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

        private static string CreateDidDocument(IdentityDocumentScenario scenario) => scenario switch
        {
            IdentityDocumentScenario.DidWeb => """
                {"id":"did:web:alice.example","alsoKnownAs":["at://alice.example"],"service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"https://pds.example"}]}
                """,
            IdentityDocumentScenario.ConflictingHandle => """
                {"id":"did:plc:alice","alsoKnownAs":["at://mallory.example"],"service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"https://pds.example"}]}
                """,
            IdentityDocumentScenario.MissingPds => """
                {"id":"did:plc:alice","alsoKnownAs":["at://alice.example"],"service":[]}
                """,
            IdentityDocumentScenario.DuplicatePds => """
                {"id":"did:plc:alice","alsoKnownAs":["at://alice.example"],"service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"https://pds.example"},{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"https://other-pds.example"}]}
                """,
            IdentityDocumentScenario.NonHttpsPds => """
                {"id":"did:plc:alice","alsoKnownAs":["at://alice.example"],"service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"http://pds.example"}]}
                """,
            IdentityDocumentScenario.InvalidPds => """
                {"id":"did:plc:alice","alsoKnownAs":["at://alice.example"],"service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"not-a-uri"}]}
                """,
            _ => """
                {"id":"did:plc:alice","alsoKnownAs":["at://alice.example"],"service":[{"id":"#atproto_pds","type":"AtprotoPersonalDataServer","serviceEndpoint":"https://pds.example"}]}
                """
        };

        private static string CreateAuthorizationServerMetadata(IdentityDocumentScenario scenario)
        {
            var authorizationEndpoint = scenario switch
            {
                IdentityDocumentScenario.HttpAuthorizationEndpoint => "http://issuer.example/oauth/authorize",
                IdentityDocumentScenario.CredentialedAuthorizationEndpoint => "https://user:password@issuer.example/oauth/authorize",
                IdentityDocumentScenario.LoginHintAuthorizationEndpoint => "https://issuer.example/oauth/authorize?login_hint=alice.example",
                IdentityDocumentScenario.TokenFragmentAuthorizationEndpoint => "https://issuer.example/oauth/authorize#access_token=secret",
                _ => "https://issuer.example/oauth/authorize"
            };
            return AuthorizationServerMetadata.Replace(
                "https://issuer.example/oauth/authorize",
                authorizationEndpoint,
                StringComparison.Ordinal);
        }

        private const string AuthorizationServerMetadata = """
            {"issuer":"https://issuer.example","authorization_endpoint":"https://issuer.example/oauth/authorize","token_endpoint":"https://issuer.example/oauth/token","pushed_authorization_request_endpoint":"https://issuer.example/oauth/par","revocation_endpoint":"https://issuer.example/oauth/revoke","require_pushed_authorization_requests":true,"token_endpoint_auth_methods_supported":["private_key_jwt"],"token_endpoint_auth_signing_alg_values_supported":["ES256"],"dpop_signing_alg_values_supported":["ES256"],"grant_types_supported":["authorization_code","refresh_token"],"response_types_supported":["code"],"code_challenge_methods_supported":["S256"],"authorization_response_iss_parameter_supported":true,"client_id_metadata_document_supported":true,"scopes_supported":["atproto"],"require_request_uri_registration":true}
            """;
    }

    public enum IdentityDocumentScenario
    {
        Valid,
        DidWeb,
        ConflictingHandle,
        MissingPds,
        DuplicatePds,
        NonHttpsPds,
        InvalidPds,
        HttpAuthorizationEndpoint,
        CredentialedAuthorizationEndpoint,
        LoginHintAuthorizationEndpoint,
        TokenFragmentAuthorizationEndpoint
    }

    private sealed class HermeticBffApiServer : HttpMessageHandler
    {
        public const string PlatformAccessToken = "opaque-platform-access-token";
        private static readonly Guid UserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");
        public int BridgeCalls { get; private set; }
        public int SyncCalls { get; private set; }
        public int AuthorityCalls { get; private set; }
        public Guid? CanonicalActorId { get; private set; }
        public Guid? ExpectedCanonicalActorConcurrencyStamp { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == AtprotoBootstrapAssertionService.BridgePath)
            {
                BridgeCalls++;
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                CanonicalActorId = body.RootElement.TryGetProperty("canonicalActorId", out var canonicalActorId)
                    && canonicalActorId.ValueKind == JsonValueKind.String
                    ? canonicalActorId.GetGuid()
                    : null;
                ExpectedCanonicalActorConcurrencyStamp = body.RootElement.TryGetProperty("expectedCanonicalActorConcurrencyStamp", out var expectedConcurrencyStamp)
                    && expectedConcurrencyStamp.ValueKind == JsonValueKind.String
                    ? expectedConcurrencyStamp.GetGuid()
                    : null;
                return Json(JsonSerializer.Serialize(new
                {
                    userId = UserId,
                    actorId = Guid.NewGuid(),
                    participationId = Guid.NewGuid(),
                    did = "did:plc:alice",
                    classification = "person",
                    canonicalActorId = CanonicalActorId,
                    expectedCanonicalActorConcurrencyStamp = ExpectedCanonicalActorConcurrencyStamp,
                    accessToken = PlatformAccessToken,
                    expiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
                }));
            }

            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == "/api/user/sync")
            {
                SyncCalls++;
                return Json(JsonSerializer.Serialize(new { success = true, id = UserId }));
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/user/admin-authority")
            {
                AuthorityCalls++;
                return Json("{\"hasAnyAuthority\":true,\"isInstanceAdmin\":true,\"adminTenantIds\":[],\"adminOrganizationIds\":[],\"adminGroupIds\":[]}");
            }

            return Json("{\"hasAnyAuthority\":false,\"isInstanceAdmin\":false,\"adminTenantIds\":[],\"adminOrganizationIds\":[],\"adminGroupIds\":[]}");
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

    private sealed class ClaimCompletingOnboardingStatusProvider(string provider)
        : IBffOnboardingStatusProvider
    {
        private bool _invalidated;

        public int InvalidationCount { get; private set; }

        public Task<BffOnboardingStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_invalidated
                ? new BffOnboardingStatus(
                    true,
                    "Completed",
                    "ConfiguredAdministrator",
                    provider,
                    1,
                    BffOnboardingDisposition.Completed)
                : PendingConfigured(provider));
        }

        public void Invalidate()
        {
            InvalidationCount++;
            _invalidated = true;
        }
    }

    private sealed record EndpointResponse(int StatusCode, string? Location, string Body);
}

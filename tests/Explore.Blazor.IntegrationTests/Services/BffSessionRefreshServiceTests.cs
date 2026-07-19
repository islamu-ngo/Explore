// ABOUTME: Focused tests for BFF session refresh orchestration after extraction from auth endpoints.
// ABOUTME: Verifies refresh-session response shape and circuit token cleanup without exposing bearer tokens.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Explore.Blazor.Authentication;
using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Services;
using Explore.Blazor.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffSessionRefreshServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");

    [Test]
    public async Task RefreshSessionAsync_WithMissingCookieAuthentication_ReturnsUnauthorized()
    {
        var authService = new TestAuthenticationService(AuthenticateResult.NoResult());
        var context = CreateContext(authService: authService);
        var service = CreateService();

        var result = await service.RefreshSessionAsync(context, CancellationToken.None);

        await ExecuteAsync(result, context);
        await Assert.That(context.Response.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task RefreshSessionAsync_WithMissingAccessToken_ReturnsConflictAndClearsCircuitState()
    {
        var principal = CreatePrincipal("user-1", "session-1");
        var authService = new TestAuthenticationService(AuthenticateResult.Success(CreateTicket(principal, accessToken: null)));
        var tokenService = Substitute.For<ICircuitAccessTokenService>();
        var userContext = Substitute.For<ICircuitUserContext>();
        var cookieStore = Substitute.For<IBffAuthCookieStore>();
        var tokenStore = Substitute.For<ICircuitTokenStore>();
        var context = CreateContext(authService, tokenService, userContext, cookieStore, tokenStore);
        var service = CreateService();

        var result = await service.RefreshSessionAsync(context, CancellationToken.None);

        await ExecuteAsync(result, context);

        await Assert.That(context.Response.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        tokenService.Received(1).ClearToken();
        userContext.Received(1).Clear();
        cookieStore.Received(1).Clear();
        tokenStore.Received(1).ClearSession("user-1", "session-1");
    }

    [Test]
    public async Task RefreshSessionAsync_WithValidAccessToken_ReturnsTokenStatusAndNeverRawToken()
    {
        var accessToken = CreateJwt("user-1", DateTime.UtcNow.AddMinutes(30), "session-1");
        var principal = CreatePrincipal("user-1", "session-1");
        var authService = new TestAuthenticationService(AuthenticateResult.Success(CreateTicket(principal, accessToken)));
        var tokenService = Substitute.For<ICircuitAccessTokenService>();
        var onboardingStatusProvider = Substitute.For<IBffOnboardingStatusProvider>();
        onboardingStatusProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new BffOnboardingStatus(IsCompleted: false, IsSetupModeActive: true, Known: true));
        var context = CreateContext(authService: authService, tokenService: tokenService, onboardingStatusProvider: onboardingStatusProvider);
        var service = CreateService(onboardingStatusProvider);

        var result = await service.RefreshSessionAsync(context, CancellationToken.None);

        await ExecuteAsync(result, context);
        await Assert.That(context.Response.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        tokenService.Received(1).SetToken(accessToken);
        await Assert.That(authService.SignInCalled).IsTrue();

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("refreshed").GetBoolean()).IsTrue();
        await Assert.That(root.GetProperty("adminClaimsUpdated").GetBoolean()).IsFalse();
        await Assert.That(root.TryGetProperty("tokenStatus", out var tokenStatus)).IsTrue();
        tokenStatus.GetString().Should().StartWith("valid_until:");
        await Assert.That(root.TryGetProperty("token", out _)).IsFalse();
        root.GetRawText().Should().NotContain(accessToken);
    }

    [Test]
    public async Task RefreshSessionAsync_WithAtprotoCookie_UsesPrivateBridgeAndStoresOnlyReplacementToken()
    {
        var principal = CreateAtprotoPrincipal();
        var authService = new TestAuthenticationService(AuthenticateResult.Success(CreateTicket(principal, "old-platform-token")));
        var tokenService = Substitute.For<ICircuitAccessTokenService>();
        var handler = new AtprotoBridgeHandler(HttpStatusCode.OK);
        var context = CreateContext(authService: authService, tokenService: tokenService);
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("events.example.com");
        var service = CreateService(bridgeHandler: handler);

        var result = await service.RefreshSessionAsync(context, CancellationToken.None);

        await ExecuteAsync(result, context);
        await Assert.That(context.Response.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(handler.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.Authorization).IsEqualTo("Bearer old-platform-token");
        await Assert.That(handler.TenantSlug).IsEqualTo("default");
        await Assert.That(handler.PrivateAssertion).IsNotNull();
        tokenService.Received(1).SetToken("new-platform-token");
        await Assert.That(authService.SignInProperties!.GetTokenValue("access_token"))
            .IsEqualTo("new-platform-token");

        context.Response.Body.Position = 0;
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseBody.Should().NotContain("old-platform-token").And.NotContain("new-platform-token");
    }

    [Test]
    public async Task RefreshSessionAsync_WithRejectedAtprotoSession_ClearsCookieAndRequiresReauthentication()
    {
        var principal = CreateAtprotoPrincipal();
        var authService = new TestAuthenticationService(AuthenticateResult.Success(CreateTicket(principal, "old-platform-token")));
        var tokenService = Substitute.For<ICircuitAccessTokenService>();
        var context = CreateContext(authService: authService, tokenService: tokenService);
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("events.example.com");
        var service = CreateService(bridgeHandler: new AtprotoBridgeHandler(HttpStatusCode.Unauthorized));

        var result = await service.RefreshSessionAsync(context, CancellationToken.None);

        await ExecuteAsync(result, context);
        await Assert.That(context.Response.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        await Assert.That(authService.SignOutCalled).IsTrue();
        tokenService.Received(1).ClearToken();
    }

    [Test]
    public async Task RevokeAtprotoSessionAsync_WithRemoteOutage_RemainsBestEffortAndUsesPrivateDelete()
    {
        var principal = CreateAtprotoPrincipal();
        var authentication = AuthenticateResult.Success(CreateTicket(principal, "old-platform-token"));
        var handler = new AtprotoBridgeHandler(HttpStatusCode.ServiceUnavailable);
        var context = CreateContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("events.example.com");
        var service = CreateService(bridgeHandler: handler);

        await service.RevokeAtprotoSessionAsync(context, authentication, CancellationToken.None);

        await Assert.That(handler.Method).IsEqualTo(HttpMethod.Delete);
        await Assert.That(handler.Authorization).IsEqualTo("Bearer old-platform-token");
        await Assert.That(handler.PrivateAssertion).IsNotNull();
    }

    private static BffSessionRefreshService CreateService(
        IBffOnboardingStatusProvider? onboardingStatusProvider = null,
        HttpMessageHandler? bridgeHandler = null)
    {
        onboardingStatusProvider ??= Substitute.For<IBffOnboardingStatusProvider>();
        onboardingStatusProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new BffOnboardingStatus(IsCompleted: false, IsSetupModeActive: true, Known: true));

        var adminClaimsTransformation = new BffAdminClaimsTransformation(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IMemoryCache>(),
            onboardingStatusProvider,
            NullLogger<BffAdminClaimsTransformation>.Instance);

        bridgeHandler ??= new AtprotoBridgeHandler(HttpStatusCode.ServiceUnavailable);
        var bridgeClient = new HttpClient(bridgeHandler) { BaseAddress = new("https://api.example/") };
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        return new BffSessionRefreshService(
            adminClaimsTransformation,
            new BffAccessTokenAssessmentService(),
            new FixedHttpClientFactory(bridgeClient),
            new AtprotoBootstrapAssertionService(CreateKeyProvider(), TimeProvider.System),
            new AtprotoTenantOriginResolver(
                Options.Create(new AtprotoAuthenticationOptions { PublicUrl = "https://events.example.com/" }),
                Options.Create(new TenantConfiguration { DefaultTenantId = TenantId, DefaultTenant = "default" }),
                environment),
            new AtprotoAuthenticationMetrics());
    }

    private static DefaultHttpContext CreateContext(
        TestAuthenticationService? authService = null,
        ICircuitAccessTokenService? tokenService = null,
        ICircuitUserContext? userContext = null,
        IBffAuthCookieStore? cookieStore = null,
        ICircuitTokenStore? tokenStore = null,
        IBffOnboardingStatusProvider? onboardingStatusProvider = null)
    {
        authService ??= new TestAuthenticationService(AuthenticateResult.NoResult());
        tokenService ??= Substitute.For<ICircuitAccessTokenService>();
        userContext ??= Substitute.For<ICircuitUserContext>();
        cookieStore ??= Substitute.For<IBffAuthCookieStore>();
        tokenStore ??= Substitute.For<ICircuitTokenStore>();
        onboardingStatusProvider ??= Substitute.For<IBffOnboardingStatusProvider>();

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IAuthenticationService>(authService)
            .AddSingleton(tokenService)
            .AddSingleton(userContext)
            .AddSingleton(cookieStore)
            .AddSingleton(tokenStore)
            .AddSingleton(onboardingStatusProvider)
            .BuildServiceProvider();

        return new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
    }

    private static async Task ExecuteAsync(IResult result, HttpContext context)
    {
        await result.ExecuteAsync(context);
        context.Response.Body.Position = 0;
    }

    private static AuthenticationTicket CreateTicket(ClaimsPrincipal principal, string? accessToken)
    {
        var properties = new AuthenticationProperties();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = accessToken }]);
        }

        return new AuthenticationTicket(principal, properties, "Cookies");
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, string sessionId) => new(new ClaimsIdentity([
        new Claim("sub", userId),
        new Claim("sid", sessionId)
    ], "test"));

    private static ClaimsPrincipal CreateAtprotoPrincipal() => new(new ClaimsIdentity([
        new Claim("sub", UserId.ToString("D")),
        new Claim("sid", Guid.CreateVersion7().ToString("D")),
        new Claim("did", "did:plc:alice"),
        new Claim("tenant_id", TenantId.ToString("D")),
        new Claim("auth_provider", "atproto")
    ], "test"));

    private static AtprotoClientKeyProvider CreateKeyProvider()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(true);
        var ring = JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new
                {
                    kty = "EC",
                    crv = "P-256",
                    x = Encode(parameters.Q.X!),
                    y = Encode(parameters.Q.Y!),
                    d = Encode(parameters.D!),
                    kid = "oauth-active",
                    use = "sig",
                    alg = "ES256",
                    status = "active"
                }
            }
        });
        return new(Options.Create(new AtprotoClientKeyOptions { OAuthClientPrivateJwks = ring }));
    }

    private static string Encode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string CreateJwt(string sub, DateTime expires, string sessionId)
    {
        var jwt = new JwtSecurityToken(
            claims: [new Claim("sub", sub), new Claim("sid", sessionId)],
            expires: expires);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private sealed class TestAuthenticationService(AuthenticateResult authenticateResult) : IAuthenticationService
    {
        public bool SignInCalled { get; private set; }
        public bool SignOutCalled { get; private set; }
        public AuthenticationProperties? SignInProperties { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(authenticateResult);

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            SignInCalled = true;
            SignInProperties = properties;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignOutCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class AtprotoBridgeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Authorization { get; private set; }
        public string? TenantSlug { get; private set; }
        public string? PrivateAssertion { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            Authorization = request.Headers.Authorization?.ToString();
            TenantSlug = request.Headers.GetValues("X-Tenant-Slug").Single();
            PrivateAssertion = request.Headers
                .GetValues(AtprotoBootstrapAssertionService.SessionBridgeHeaderName)
                .Single();
            var response = new HttpResponseMessage(statusCode);
            if (statusCode == HttpStatusCode.OK)
            {
                response.Content = JsonContent.Create(new
                {
                    userId = UserId,
                    did = "did:plc:alice",
                    accessToken = "new-platform-token",
                    expiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
                });
            }

            return Task.FromResult(response);
        }
    }
}

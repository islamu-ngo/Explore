// ABOUTME: Focused tests for BFF session refresh orchestration after extraction from auth endpoints.
// ABOUTME: Verifies refresh-session response shape and circuit token cleanup without exposing bearer tokens.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Explore.Blazor.Services;
using Explore.Blazor.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffSessionRefreshServiceTests
{
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

    private static BffSessionRefreshService CreateService(IBffOnboardingStatusProvider? onboardingStatusProvider = null)
    {
        onboardingStatusProvider ??= Substitute.For<IBffOnboardingStatusProvider>();
        onboardingStatusProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new BffOnboardingStatus(IsCompleted: false, IsSetupModeActive: true, Known: true));

        var adminClaimsTransformation = new BffAdminClaimsTransformation(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IMemoryCache>(),
            onboardingStatusProvider,
            NullLogger<BffAdminClaimsTransformation>.Instance);

        return new BffSessionRefreshService(
            adminClaimsTransformation,
            new BffAccessTokenAssessmentService());
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
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;
    }
}

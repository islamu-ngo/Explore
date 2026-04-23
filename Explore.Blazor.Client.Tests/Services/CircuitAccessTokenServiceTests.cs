using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Explore.Blazor.Client.Tests.Services;

[NotInParallel("CircuitAccessTokenStore")]
public class CircuitAccessTokenServiceTests
{
    [Test]
    public async Task AccessTokenForwardingHandler_UsesHttpContextToken_WhenAvailable()
    {
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var storeToken = CreateJwt(userId);
        var contextToken = CreateJwt(userId);

        var storeContext = CreateHttpContext(userId);
        var storeService = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = storeContext },
            Substitute.For<ILogger<CircuitAccessTokenService>>());
        storeService.SetToken(storeToken);

        var requestContext = CreateHttpContext(userId, contextToken);
        var accessor = new HttpContextAccessor { HttpContext = requestContext };
        var handler = new TestableAccessTokenForwardingHandler(
            accessor,
            Substitute.For<ICircuitAccessTokenService>(),
            Substitute.For<ICircuitUserContext>(),
            Substitute.For<ILogger<AccessTokenForwardingHandler>>());
        var terminal = new CaptureHandler();
        handler.InnerHandler = terminal;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/protected");
        var response = await handler.InvokeAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(terminal.Request?.Headers.Authorization?.Parameter).IsEqualTo(contextToken);
    }

    [Test]
    public async Task AccessTokenForwardingHandler_UsesUserStoreToken_WhenHttpContextTokenMissing()
    {
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var storeToken = CreateJwt(userId);

        var storeContext = CreateHttpContext(userId);
        var storeService = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = storeContext },
            Substitute.For<ILogger<CircuitAccessTokenService>>());
        storeService.SetToken(storeToken);

        var requestContext = CreateHttpContext(userId);
        var accessor = new HttpContextAccessor { HttpContext = requestContext };
        var handler = new TestableAccessTokenForwardingHandler(
            accessor,
            Substitute.For<ICircuitAccessTokenService>(),
            Substitute.For<ICircuitUserContext>(),
            Substitute.For<ILogger<AccessTokenForwardingHandler>>());
        var terminal = new CaptureHandler();
        handler.InnerHandler = terminal;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/protected");
        var response = await handler.InvokeAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(terminal.Request?.Headers.Authorization?.Parameter).IsEqualTo(storeToken);
    }

    [Test]
    public async Task AccessTokenForwardingHandler_DoesNotUseOtherUsersToken()
    {
        ClearTokenStore();

        var ownerUserId = Guid.NewGuid().ToString();
        var requesterUserId = Guid.NewGuid().ToString();
        var ownerToken = CreateJwt(ownerUserId);

        var storeContext = CreateHttpContext(ownerUserId);
        var storeService = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = storeContext },
            Substitute.For<ILogger<CircuitAccessTokenService>>());
        storeService.SetToken(ownerToken);

        var requestContext = CreateHttpContext(requesterUserId);
        var accessor = new HttpContextAccessor { HttpContext = requestContext };
        var handler = new TestableAccessTokenForwardingHandler(
            accessor,
            Substitute.For<ICircuitAccessTokenService>(),
            Substitute.For<ICircuitUserContext>(),
            Substitute.For<ILogger<AccessTokenForwardingHandler>>());
        var terminal = new CaptureHandler();
        handler.InnerHandler = terminal;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/protected");
        var response = await handler.InvokeAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(terminal.Request?.Headers.Authorization).IsNull();
    }

    [Test]
    public async Task AccessTokenForwardingHandler_UsesCircuitUserContext_WhenHttpContextIsNull()
    {
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var storeToken = CreateJwt(userId);

        var storeContext = CreateHttpContext(userId);
        var storeService = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = storeContext },
            Substitute.For<ILogger<CircuitAccessTokenService>>());
        storeService.SetToken(storeToken);

        var accessor = new HttpContextAccessor { HttpContext = null };
        var circuitUserContext = Substitute.For<ICircuitUserContext>();
        circuitUserContext.UserId.Returns(userId);

        var handler = new TestableAccessTokenForwardingHandler(
            accessor,
            Substitute.For<ICircuitAccessTokenService>(),
            circuitUserContext,
            Substitute.For<ILogger<AccessTokenForwardingHandler>>());
        var terminal = new CaptureHandler();
        handler.InnerHandler = terminal;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/protected");
        var response = await handler.InvokeAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(terminal.Request?.Headers.Authorization?.Parameter).IsEqualTo(storeToken);
    }

    [Test]
    public async Task CircuitAccessTokenService_GetTokenForUser_ReturnsOnlyMatchingUserToken()
    {
        ClearTokenStore();

        var userA = Guid.NewGuid().ToString();
        var userB = Guid.NewGuid().ToString();
        var tokenA = CreateJwt(userA);

        var context = CreateHttpContext(userA);
        var service = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = context },
            Substitute.For<ILogger<CircuitAccessTokenService>>());
        service.SetToken(tokenA);

        var forA = CircuitAccessTokenService.GetTokenForUser(userA);
        var forB = CircuitAccessTokenService.GetTokenForUser(userB);

        await Assert.That(forA).IsEqualTo(tokenA);
        await Assert.That(forB).IsNull();
    }

    [Test]
    public async Task CircuitAccessTokenService_AccessToken_PrefersSharedStoreTokenOverStaleLocalToken()
    {
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var staleToken = CreateJwt(userId);
        var refreshedToken = CreateJwt(userId, DateTime.UtcNow.AddMinutes(30));

        var circuitContext = CreateHttpContext(userId);
        var circuitService = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = circuitContext },
            Substitute.For<ILogger<CircuitAccessTokenService>>());
        circuitService.SetToken(staleToken);

        var refreshRequestContext = CreateHttpContext(userId);
        var refreshScopeService = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = refreshRequestContext },
            Substitute.For<ILogger<CircuitAccessTokenService>>());
        refreshScopeService.SetToken(refreshedToken);

        await Assert.That(circuitService.AccessToken).IsEqualTo(refreshedToken);
    }

    [Test]
    public async Task CircuitAccessTokenService_SetToken_PersistsSidFallbackToSharedStore()
    {
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var sidToken = CreateJwtWithSid(userId);
        var context = CreateHttpContextWithSid(userId);
        var service = new CircuitAccessTokenService(
            new HttpContextAccessor { HttpContext = context },
            Substitute.For<ILogger<CircuitAccessTokenService>>());

        service.SetToken(sidToken);

        await Assert.That(CircuitAccessTokenService.GetTokenForUser(userId)).IsEqualTo(sidToken);
    }

    [Test]
    public async Task AccessTokenForwardingHandler_DoesNotForwardSetupSecret_ForInstanceOnboardingEndpoints()
    {
        // Setup-secret forwarding is now handled by SetupSecretForwardingHandler.
        // AccessTokenForwardingHandler should NOT add X-Setup-Secret headers.
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var context = CreateHttpContext(userId);
        context.Request.Headers.Cookie = "setup-secret=test-setup-secret";

        var accessor = new HttpContextAccessor { HttpContext = context };
        var handler = new TestableAccessTokenForwardingHandler(
            accessor,
            Substitute.For<ICircuitAccessTokenService>(),
            Substitute.For<ICircuitUserContext>(),
            Substitute.For<ILogger<AccessTokenForwardingHandler>>());
        var terminal = new CaptureHandler();
        handler.InnerHandler = terminal;

        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/InstanceOnboarding/complete");
        _ = await handler.InvokeAsync(request);

        await Assert.That(terminal.Request?.Headers.Contains("X-Setup-Secret")).IsFalse();
    }

    [Test]
    public async Task AccessTokenForwardingHandler_DoesNotForwardSetupSecret_ForUnrelatedEndpoints()
    {
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var context = CreateHttpContext(userId);
        context.Request.Headers.Cookie = "setup-secret=test-setup-secret";

        var accessor = new HttpContextAccessor { HttpContext = context };
        var handler = new TestableAccessTokenForwardingHandler(
            accessor,
            Substitute.For<ICircuitAccessTokenService>(),
            Substitute.For<ICircuitUserContext>(),
            Substitute.For<ILogger<AccessTokenForwardingHandler>>());
        var terminal = new CaptureHandler();
        handler.InnerHandler = terminal;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/events");
        _ = await handler.InvokeAsync(request);

        await Assert.That(terminal.Request?.Headers.Contains("X-Setup-Secret")).IsFalse();
    }

    [Test]
    public async Task AccessTokenForwardingHandler_DoesNotAddSetupSecret_WhenCookieMissing()
    {
        // Setup-secret forwarding is now handled by SetupSecretForwardingHandler.
        ClearTokenStore();

        var userId = Guid.NewGuid().ToString();
        var context = CreateHttpContext(userId);

        var accessor = new HttpContextAccessor { HttpContext = context };
        var handler = new TestableAccessTokenForwardingHandler(
            accessor,
            Substitute.For<ICircuitAccessTokenService>(),
            Substitute.For<ICircuitUserContext>(),
            Substitute.For<ILogger<AccessTokenForwardingHandler>>());
        var terminal = new CaptureHandler();
        handler.InnerHandler = terminal;

        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/InstanceOnboarding/complete");
        _ = await handler.InvokeAsync(request);

        await Assert.That(terminal.Request?.Headers.Contains("X-Setup-Secret")).IsFalse();
    }

    private static DefaultHttpContext CreateHttpContext(string userId, string? authToken = null)
    {
        return CreateHttpContext(new Claim("sub", userId), authToken);
    }

    private static DefaultHttpContext CreateHttpContextWithSid(string userId, string? authToken = null)
    {
        return CreateHttpContext(new Claim("sid", userId), authToken);
    }

    private static DefaultHttpContext CreateHttpContext(Claim userIdClaim, string? authToken = null)
    {
        var claims = new[] { userIdClaim };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext { User = principal };

        var services = new ServiceCollection();

        if (!string.IsNullOrEmpty(authToken))
        {
            var authService = Substitute.For<IAuthenticationService>();
            var properties = new AuthenticationProperties();
            properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = authToken } });
            var ticket = new AuthenticationTicket(principal, properties, "Cookies");

            authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
                .Returns(Task.FromResult(AuthenticateResult.Success(ticket)));

            services.AddSingleton(authService);
            services.AddSingleton<IAuthenticationSchemeProvider>(new AuthenticationSchemeProvider(
                Options.Create(new AuthenticationOptions { DefaultAuthenticateScheme = "Cookies" })));
        }

        context.RequestServices = services.BuildServiceProvider();
        return context;
    }

    private static string CreateJwt(string sub, DateTime? expires = null)
    {
        var jwt = new JwtSecurityToken(claims: new[] { new Claim("sub", sub) }, expires: expires);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static string CreateJwtWithSid(string sid, DateTime? expires = null)
    {
        var jwt = new JwtSecurityToken(claims: new[] { new Claim("sid", sid) }, expires: expires);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static void ClearTokenStore()
    {
        var storeField = typeof(CircuitAccessTokenService).GetField("_tokenStore", BindingFlags.NonPublic | BindingFlags.Static);
        var store = storeField?.GetValue(null);
        var clearMethod = store?.GetType().GetMethod("Clear");
        clearMethod?.Invoke(store, null);
    }

    private sealed class TestableAccessTokenForwardingHandler : AccessTokenForwardingHandler
    {
        public TestableAccessTokenForwardingHandler(
            IHttpContextAccessor httpContextAccessor,
            ICircuitAccessTokenService circuitAccessTokenService,
            ICircuitUserContext circuitUserContext,
            ILogger<AccessTokenForwardingHandler> logger)
            : base(httpContextAccessor, circuitAccessTokenService, circuitUserContext, logger)
        {
        }

        public Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request)
        {
            return SendAsync(request, CancellationToken.None);
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

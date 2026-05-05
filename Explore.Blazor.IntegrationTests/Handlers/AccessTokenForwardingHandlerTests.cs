// ABOUTME: Unit-style DelegatingHandler tests for access token forwarding from the current authenticated context.
// ABOUTME: Verifies Bearer authorization behavior for present token, absent token, and pre-existing Authorization headers.

using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Explore.Blazor.IntegrationTests.Handlers;

public class AccessTokenForwardingHandlerTests
{
    [Test]
    public async Task SendAsync_WithAccessTokenInHttpContext_AddsAuthorizationHeader()
    {
        var userId = Guid.NewGuid().ToString();
        var httpContext = CreateHttpContext(userId, "context-token-123");
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var innerHandler = new CapturingHandler();
        var circuitTokenService = Substitute.For<ICircuitAccessTokenService>();
        var circuitUserContext = Substitute.For<ICircuitUserContext>();
        var handler = new AccessTokenForwardingHandler(httpContextAccessor, circuitTokenService, circuitUserContext, NullLogger<AccessTokenForwardingHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/protected");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Authorization).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest.Headers.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(innerHandler.CapturedRequest.Headers.Authorization.Parameter).IsEqualTo("context-token-123");
    }

    [Test]
    public async Task SendAsync_WithoutToken_DoesNotAddAuthorizationHeader()
    {
        var userId = Guid.NewGuid().ToString();
        var httpContext = CreateHttpContext(userId, token: null);
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var innerHandler = new CapturingHandler();
        var circuitTokenService = Substitute.For<ICircuitAccessTokenService>();
        var circuitUserContext = Substitute.For<ICircuitUserContext>();
        var handler = new AccessTokenForwardingHandler(httpContextAccessor, circuitTokenService, circuitUserContext, NullLogger<AccessTokenForwardingHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/protected");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Authorization).IsNull();
    }

    [Test]
    public async Task SendAsync_WithExistingAuthorizationHeader_DoesNotOverwrite()
    {
        var userId = Guid.NewGuid().ToString();
        var httpContext = CreateHttpContext(userId, "context-token-123");
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var innerHandler = new CapturingHandler();
        var circuitTokenService = Substitute.For<ICircuitAccessTokenService>();
        var circuitUserContext = Substitute.For<ICircuitUserContext>();
        var handler = new AccessTokenForwardingHandler(httpContextAccessor, circuitTokenService, circuitUserContext, NullLogger<AccessTokenForwardingHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/protected");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "preset-token");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Authorization).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest.Headers.Authorization!.Parameter).IsEqualTo("preset-token");
    }

    [Test]
    public async Task SendAsync_WithExpiredHttpContextToken_UsesFreshStoredToken()
    {
        var userId = Guid.NewGuid().ToString();
        var expiredContextToken = CreateUnsignedJwt(userId, DateTime.UtcNow.AddMinutes(-5));
        var freshStoredToken = CreateUnsignedJwt(userId, DateTime.UtcNow.AddMinutes(30));
        var httpContext = CreateHttpContext(userId, expiredContextToken);
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var tokenStoreService = new CircuitAccessTokenService(
            httpContextAccessor,
            NullLogger<CircuitAccessTokenService>.Instance);
        tokenStoreService.SetToken(freshStoredToken);

        var innerHandler = new CapturingHandler();
        var circuitTokenService = Substitute.For<ICircuitAccessTokenService>();
        var circuitUserContext = Substitute.For<ICircuitUserContext>();
        var handler = new AccessTokenForwardingHandler(httpContextAccessor, circuitTokenService, circuitUserContext, NullLogger<AccessTokenForwardingHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/protected");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Authorization).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest.Headers.Authorization!.Parameter).IsEqualTo(freshStoredToken);
    }

    [Test]
    public async Task SendAsync_WithinCircuitActivityScope_UsesCircuitUserContextTokenStore()
    {
        var userId = Guid.NewGuid().ToString();
        var token = CreateUnsignedJwt(userId);
        var httpContextAccessor = new HttpContextAccessor();

        var tokenStoreService = new CircuitAccessTokenService(
            httpContextAccessor,
            NullLogger<CircuitAccessTokenService>.Instance);
        tokenStoreService.SetToken(token);

        var circuitUserContext = new CircuitUserContext();
        circuitUserContext.SetUserId(userId);

        var innerHandler = new CapturingHandler();

        using (circuitUserContext.BeginActivityScope())
        {
            var handler = new AccessTokenForwardingHandler(
                httpContextAccessor,
                Substitute.For<ICircuitAccessTokenService>(),
                new CircuitUserContext(),
                NullLogger<AccessTokenForwardingHandler>.Instance)
            {
                InnerHandler = innerHandler
            };

            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/protected");
            _ = await invoker.SendAsync(request, CancellationToken.None);
        }

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Authorization).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest.Headers.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(innerHandler.CapturedRequest.Headers.Authorization.Parameter).IsEqualTo(token);
    }

    private static string CreateUnsignedJwt(string userId, DateTime? expires = null)
    {
        var jwt = new JwtSecurityToken(
            claims:
            [
                new Claim("sub", userId)
            ],
            expires: expires ?? DateTime.UtcNow.AddMinutes(30));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static HttpContext CreateHttpContext(string userId, string? token)
    {
        var authService = Substitute.For<IAuthenticationService>();
        var authProperties = new AuthenticationProperties();

        if (!string.IsNullOrWhiteSpace(token))
        {
            authProperties.StoreTokens(
            [
                new AuthenticationToken { Name = "access_token", Value = token }
            ]);
        }

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId)
            ],
            authenticationType: "Test"));

        var ticket = new AuthenticationTicket(principal, authProperties, TestAuthHandler.SchemeName);
        authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>())
            .Returns(AuthenticateResult.Success(ticket));

        var services = new ServiceCollection();
        services.AddSingleton(authService);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.User = principal;
        return httpContext;
    }
}

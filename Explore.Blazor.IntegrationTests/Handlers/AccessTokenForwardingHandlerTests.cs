// ABOUTME: Unit-style DelegatingHandler tests for access token forwarding from the current authenticated context.
// ABOUTME: Verifies Bearer authorization behavior for present token, absent token, and pre-existing Authorization headers.

using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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
        var handler = new AccessTokenForwardingHandler(httpContextAccessor, NullLogger<AccessTokenForwardingHandler>.Instance)
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
        var handler = new AccessTokenForwardingHandler(httpContextAccessor, NullLogger<AccessTokenForwardingHandler>.Instance)
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
        var handler = new AccessTokenForwardingHandler(httpContextAccessor, NullLogger<AccessTokenForwardingHandler>.Instance)
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

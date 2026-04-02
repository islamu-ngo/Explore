// ABOUTME: Unit-style DelegatingHandler tests for forwarding setup secret headers to onboarding endpoints.
// ABOUTME: Verifies cookie/session-source behavior and endpoint gating for X-Setup-Secret forwarding.

using Explore.Blazor.Services;
using Microsoft.AspNetCore.Http;

namespace Explore.Blazor.IntegrationTests.Handlers;

public class SetupSecretForwardingHandlerTests
{
    [Test]
    public async Task SendAsync_OnboardingPath_WithCookieSecret_AddsXSetupSecretHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "setup-secret=cookie-secret-123";

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        var handler = new SetupSecretForwardingHandler(httpContextAccessor, sessionService)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/complete");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Setup-Secret").Single()).IsEqualTo("cookie-secret-123");
    }

    [Test]
    public async Task SendAsync_NonOnboardingPath_DoesNotAddHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "setup-secret=cookie-secret-123";

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        var handler = new SetupSecretForwardingHandler(httpContextAccessor, sessionService)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/api/Events");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsFalse();
    }

    [Test]
    public async Task SendAsync_OnboardingPath_WithoutSecret_DoesNotAddHeader()
    {
        var userId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId)
            ],
            authenticationType: "Test"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        var handler = new SetupSecretForwardingHandler(httpContextAccessor, sessionService)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/validate-secret");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsFalse();
    }

    [Test]
    public async Task SendAsync_OnboardingPath_WithSessionSecret_AddsXSetupSecretHeader()
    {
        var userId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId)
            ],
            authenticationType: "Test"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var sessionService = new SetupSecretSessionService();
        sessionService.SetForUser(userId, "session-secret-456");

        var innerHandler = new CapturingHandler();
        var handler = new SetupSecretForwardingHandler(httpContextAccessor, sessionService)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/auth-provider-configuration");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        sessionService.ClearForUser(userId);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Setup-Secret").Single()).IsEqualTo("session-secret-456");
    }
}

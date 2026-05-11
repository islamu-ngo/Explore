// ABOUTME: Unit-style DelegatingHandler tests for forwarding setup secret headers to onboarding endpoints.
// ABOUTME: Verifies cookie/session-source behavior and endpoint gating for X-Setup-Secret forwarding.

using Explore.Blazor.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.IntegrationTests.Handlers;

public class SetupSecretForwardingHandlerTests
{
    [Test]
    public async Task SendAsync_OnboardingPath_WithCookieSecret_AddsXSetupSecretHeader()
    {
        var httpContext = new DefaultHttpContext();
        var cookieProtector = CreateCookieProtector();
        httpContext.Request.Headers.Cookie = $"setup-secret={cookieProtector.Protect("cookie-secret-123")}";

        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler, cookieProtector);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
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
        var cookieProtector = CreateCookieProtector();
        httpContext.Request.Headers.Cookie = $"setup-secret={cookieProtector.Protect("cookie-secret-123")}";

        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler, cookieProtector);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
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

        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/validate-secret");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsFalse();
    }

    [Test]
    public async Task SendAsync_OnboardingPath_WithInboundHeaderAndNoTrustedSecret_DoesNotForwardClientHeader()
    {
        var httpContext = new DefaultHttpContext();

        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/validate-secret");
        request.Headers.Add("X-Setup-Secret", "client-controlled-secret");

        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsFalse();
    }

    [Test]
    public async Task SendAsync_OnboardingPath_WithInboundHeaderAndSessionSecret_ForwardsTrustedSessionSecret()
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

        var sessionService = new SetupSecretSessionService();
        sessionService.SetForUser(userId, "trusted-session-secret");

        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/auth-provider-configuration");
        request.Headers.Add("X-Setup-Secret", "client-controlled-secret");

        _ = await invoker.SendAsync(request, CancellationToken.None);

        sessionService.ClearForUser(userId);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Setup-Secret").Single()).IsEqualTo("trusted-session-secret");
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

        var sessionService = new SetupSecretSessionService();
        sessionService.SetForUser(userId, "session-secret-456");

        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/auth-provider-configuration");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        sessionService.ClearForUser(userId);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Setup-Secret").Single()).IsEqualTo("session-secret-456");
    }

    [Test]
    public async Task SendAsync_AuthzOnboardingVerifyPath_WithCookieSecret_AddsXSetupSecretHeader()
    {
        var httpContext = new DefaultHttpContext();
        var cookieProtector = CreateCookieProtector();
        httpContext.Request.Headers.Cookie = $"setup-secret={cookieProtector.Protect("cookie-secret-789")}";

        var sessionService = new SetupSecretSessionService();
        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler, cookieProtector);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/InstanceOnboarding/authz-provider-configuration/verify");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Setup-Secret").Single()).IsEqualTo("cookie-secret-789");
    }

    [Test]
    public async Task SendAsync_AuthzOnboardingInternalPath_WithSessionSecret_AddsXSetupSecretHeader()
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

        var sessionService = new SetupSecretSessionService();
        sessionService.SetForUser(userId, "session-secret-999");

        var innerHandler = new CapturingHandler();
        using var handler = CreateHandler(httpContext, sessionService, innerHandler);

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/api/InstanceOnboarding/authz-provider-configuration/internal");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        sessionService.ClearForUser(userId);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Setup-Secret")).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Setup-Secret").Single()).IsEqualTo("session-secret-999");
    }

    private static SetupSecretForwardingHandler CreateHandler(
        DefaultHttpContext httpContext,
        SetupSecretSessionService sessionService,
        CapturingHandler innerHandler,
        ISetupSecretCookieProtector? cookieProtector = null)
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var resolver = new SetupSecretResolver(
            httpContextAccessor,
            sessionService,
            cookieProtector ?? CreateCookieProtector(),
            Options.Create(new SetupSecretResolverOptions()),
            new TestHostEnvironment());

        return new SetupSecretForwardingHandler(resolver)
        {
            InnerHandler = innerHandler
        };
    }

    private static SetupSecretCookieProtector CreateCookieProtector()
    {
        var keyRingPath = Path.Combine(
            Path.GetTempPath(),
            "explore-setup-secret-tests",
            Guid.NewGuid().ToString("N"));

        _ = Directory.CreateDirectory(keyRingPath);
        var provider = DataProtectionProvider.Create(new DirectoryInfo(keyRingPath));
        return new SetupSecretCookieProtector(provider);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Explore.Blazor.IntegrationTests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

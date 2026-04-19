// ABOUTME: Component tests for the authorization-provider onboarding page browser-fetch flow.
// ABOUTME: Verifies Cerbos auto-detection, endpoint verification, and local save navigation use bff.js.

using Explore.Blazor.Client.Pages.Onboarding;

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public class AuthorizationProviderConfigurationTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _instanceOnboardingService;
    private readonly FakeNavigationManager _nav;

    public AuthorizationProviderConfigurationTests()
    {
        _ctx = new BlazorTestContext();
        _instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        _ctx.Services.AddSingleton(_instanceOnboardingService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<AuthorizationProviderConfiguration>>());
        _nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public async Task Load_WhenCerbosDetectedFromEnvironment_ShowsAutoDetectedVerifiedState()
    {
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });

        var module = _ctx.JSInterop.SetupModule("/js/bff.js");
        module.Setup<AuthorizationProviderConfigurationModel>("fetchJson", invocation =>
                invocation.Arguments.Count > 0 &&
                string.Equals(
                    invocation.Arguments[0]?.ToString(),
                    "/api/InstanceOnboarding/authz-provider-configuration/internal",
                    StringComparison.Ordinal))
            .SetResult(new AuthorizationProviderConfigurationModel
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
                CerbosDetectedFromEnvironment = true,
                CerbosEndpointVerified = true
            });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Auto-detected from environment", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Endpoint verified and ready to use.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected Cerbos environment auto-detection state was not rendered.");
            }
        });

        await _instanceOnboardingService.DidNotReceive()
            .GetAuthorizationProviderConfigurationAsync();
    }

    [Test]
    public async Task VerifyCerbos_WhenBrowserCommandSucceeds_ShowsVerifiedMessage()
    {
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });

        var module = _ctx.JSInterop.SetupModule("/js/bff.js");
        module.Setup<AuthorizationProviderConfigurationModel>("fetchJson", invocation =>
                invocation.Arguments.Count > 0 &&
                string.Equals(
                    invocation.Arguments[0]?.ToString(),
                    "/api/InstanceOnboarding/authz-provider-configuration/internal",
                    StringComparison.Ordinal))
            .SetResult(new AuthorizationProviderConfigurationModel
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
                CerbosDetectedFromEnvironment = false,
                CerbosEndpointVerified = false
            });

        module.Setup<InstanceCommandResponseModel>("sendCommand", invocation =>
                invocation.Arguments.Count >= 3 &&
                string.Equals(invocation.Arguments[0]?.ToString(), "POST", StringComparison.Ordinal) &&
                string.Equals(invocation.Arguments[1]?.ToString(), "/api/InstanceOnboarding/authz-provider-configuration/verify", StringComparison.Ordinal))
            .SetResult(new InstanceCommandResponseModel
            {
                Success = true,
                Message = "Cerbos endpoint verified successfully."
            });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Verify Endpoint", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Authorization provider page did not finish loading.");
            }
        });

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Verify Endpoint", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Endpoint verified and ready to use.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected verified Cerbos endpoint message was not rendered.");
            }
        });
    }

    [Test]
    public async Task SaveLocal_WhenBrowserCommandSucceeds_RedirectsToLogin()
    {
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });

        var module = _ctx.JSInterop.SetupModule("/js/bff.js");
        module.Setup<AuthorizationProviderConfigurationModel>("fetchJson", invocation =>
                invocation.Arguments.Count > 0 &&
                string.Equals(
                    invocation.Arguments[0]?.ToString(),
                    "/api/InstanceOnboarding/authz-provider-configuration/internal",
                    StringComparison.Ordinal))
            .SetResult(new AuthorizationProviderConfigurationModel
            {
                Provider = "local"
            });

        module.Setup<InstanceCommandResponseModel>("sendCommand", invocation =>
                invocation.Arguments.Count >= 3 &&
                string.Equals(invocation.Arguments[0]?.ToString(), "PUT", StringComparison.Ordinal) &&
                string.Equals(invocation.Arguments[1]?.ToString(), "/api/InstanceOnboarding/authz-provider-configuration", StringComparison.Ordinal))
            .SetResult(new InstanceCommandResponseModel
            {
                Success = true,
                Message = "Authorization provider saved."
            });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Save & Continue", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Authorization provider page did not finish loading.");
            }
        });

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Save & Continue", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            if (!_nav.Uri.EndsWith("/login?returnUrl=/setup", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected redirect to setup login, got '{_nav.Uri}'.");
            }
        });

        await _instanceOnboardingService.DidNotReceive()
            .SaveAuthorizationProviderConfigurationAsync(Arg.Any<AuthorizationProviderConfigurationModel>());
    }
}

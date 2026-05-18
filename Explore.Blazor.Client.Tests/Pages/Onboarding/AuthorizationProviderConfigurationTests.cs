// ABOUTME: Component tests for the authorization-provider onboarding page browser-fetch flow.
// ABOUTME: Verifies Cerbos auto-detection, endpoint verification, and local save navigation use bff.js.

using Bunit.TestDoubles;
using Explore.Blazor.Client.Pages.Onboarding;

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public class AuthorizationProviderConfigurationTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _instanceOnboardingService;
    private readonly BunitNavigationManager _nav;

    public AuthorizationProviderConfigurationTests()
    {
        _ctx = new BlazorTestContext();
        _instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        _ctx.Services.AddSingleton(_instanceOnboardingService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<AuthorizationProviderConfiguration>>());
        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
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
            if (!cut.Markup.Contains("Cerbos Detected", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("We've automatically configured Cerbos", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Continue with Cerbos", StringComparison.OrdinalIgnoreCase))
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
    public async Task Load_WhenCerbosSelected_ShowsManualPolicyPackageDownloadFallback()
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
                CerbosEndpointVerified = false
            });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Manual policy package fallback", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("/api/InstanceOnboarding/authz-provider-configuration/package", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Download Policy ZIP", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected manual policy package fallback download affordance was not rendered.");
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
            if (!_nav.Uri.EndsWith("/login?returnUrl=/onboarding/instance", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected redirect to setup login, got '{_nav.Uri}'.");
            }
        });

        await _instanceOnboardingService.DidNotReceive()
            .SaveAuthorizationProviderConfigurationAsync(Arg.Any<AuthorizationProviderConfigurationModel>());
    }

    [Test]
    public async Task SaveCerbos_WhenPolicySyncSucceeds_RedirectsToLogin()
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
                CerbosEndpointVerified = true
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

        module.Setup<InstanceCommandResponseModel>("sendCommand", invocation =>
                invocation.Arguments.Count >= 3 &&
                string.Equals(invocation.Arguments[0]?.ToString(), "POST", StringComparison.Ordinal) &&
                string.Equals(invocation.Arguments[1]?.ToString(), "/api/InstanceOnboarding/authz-provider-configuration/sync", StringComparison.Ordinal))
            .SetResult(new InstanceCommandResponseModel
            {
                Success = true,
                Message = "Authorization policy package synced."
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
            if (!_nav.Uri.EndsWith("/login?returnUrl=/onboarding/instance", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected redirect to setup login, got '{_nav.Uri}'.");
            }
        });
    }

    [Test]
    public async Task SaveCerbos_WhenPolicySyncFails_DoesNotRedirectAndShowsSafeError()
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
                CerbosEndpointVerified = true
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

        module.Setup<InstanceCommandResponseModel>("sendCommand", invocation =>
                invocation.Arguments.Count >= 3 &&
                string.Equals(invocation.Arguments[0]?.ToString(), "POST", StringComparison.Ordinal) &&
                string.Equals(invocation.Arguments[1]?.ToString(), "/api/InstanceOnboarding/authz-provider-configuration/sync", StringComparison.Ordinal))
            .SetResult(new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Authorization policy package sync failed.",
                Errors = ["Configure Cerbos Admin API credentials before publishing."]
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
            var successRedirect = _nav.Uri.EndsWith(
                "/login?returnUrl=/onboarding/instance",
                StringComparison.OrdinalIgnoreCase);

            if (!cut.Markup.Contains("Authorization policy package sync failed", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Continue after manual install", StringComparison.OrdinalIgnoreCase) ||
                successRedirect)
            {
                throw new InvalidOperationException("Expected sync failure and manual fallback continuation to be displayed without navigation.");
            }
        });
    }

    [Test]
    public async Task SaveCerbos_WhenPolicySyncFailsAndManualInstallAcknowledged_RedirectsToLogin()
    {
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });

        var module = _ctx.JSInterop.SetupModule("/js/bff.js");
        module.Setup<AuthorizationProviderConfigurationModel>(invocation =>
                invocation.Identifier == "fetchJson" &&
                invocation.Arguments.Count > 0 &&
                string.Equals(
                    invocation.Arguments[0]?.ToString(),
                    "/api/InstanceOnboarding/authz-provider-configuration/internal",
                    StringComparison.Ordinal))
            .SetResult(new AuthorizationProviderConfigurationModel
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
                CerbosEndpointVerified = true
            });

        module.Setup<InstanceCommandResponseModel>(invocation =>
                invocation.Identifier == "sendCommand" &&
                invocation.Arguments.Count >= 3 &&
                string.Equals(invocation.Arguments[0]?.ToString(), "PUT", StringComparison.Ordinal) &&
                string.Equals(invocation.Arguments[1]?.ToString(), "/api/InstanceOnboarding/authz-provider-configuration", StringComparison.Ordinal))
            .SetResult(new InstanceCommandResponseModel
            {
                Success = true,
                Message = "Authorization provider saved."
            });

        module.Setup<InstanceCommandResponseModel>(invocation =>
                invocation.Identifier == "sendCommand" &&
                invocation.Arguments.Count >= 3 &&
                string.Equals(invocation.Arguments[0]?.ToString(), "POST", StringComparison.Ordinal) &&
                string.Equals(invocation.Arguments[1]?.ToString(), "/api/InstanceOnboarding/authz-provider-configuration/sync", StringComparison.Ordinal))
            .SetResult(new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Authorization policy package sync failed."
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
            if (!cut.Markup.Contains("Continue after manual install", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Manual install continuation was not rendered.");
            }
        });

        var continueButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Continue after manual install", StringComparison.OrdinalIgnoreCase));
        await Assert.That(continueButton.HasAttribute("disabled")).IsTrue();

        cut.Find("input[type=\"checkbox\"]").Change(true);
        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Continue after manual install", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            if (!_nav.Uri.EndsWith("/login?returnUrl=/onboarding/instance", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected redirect to setup login after manual install acknowledgement, got '{_nav.Uri}'.");
            }
        });
    }
}

// ABOUTME: Component tests for the authorization-provider onboarding page browser-fetch flow.
// ABOUTME: Verifies choice-first provider selection, PDP verification, policy readiness, and browser-safe sync behavior.

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
    public async Task Load_WhenCerbosDetectedFromEnvironment_ShowsChoiceFirstShellAndBootstrapState()
    {
        SetupIncompleteOnboardingStatus();
        SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "local",
            CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
            CerbosDetectedFromEnvironment = true,
            CerbosEndpointVerified = true,
            CerbosEndpointOwnership = BootstrapOwnership()
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            if (!markup.Contains("Step 3 of 6", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("Choose authorization provider", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("Local RBAC", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("Cerbos PDP", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("PDP reachable", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("From deployment", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("Policies not verified", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("Ready to continue?", StringComparison.OrdinalIgnoreCase) ||
                !markup.Contains("Download ZIP", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected choice-first Cerbos bootstrap state was not rendered.");
            }

            var localIndex = markup.IndexOf("Local RBAC", StringComparison.OrdinalIgnoreCase);
            var cerbosIndex = markup.IndexOf("Cerbos PDP", StringComparison.OrdinalIgnoreCase);
            if (localIndex < 0 || cerbosIndex < 0 || localIndex > cerbosIndex)
            {
                throw new InvalidOperationException("Expected Local RBAC to render before Cerbos in the main decision area.");
            }
        });

        await _instanceOnboardingService.DidNotReceive()
            .GetAuthorizationProviderConfigurationAsync();
    }

    [Test]
    public async Task Load_WhenLocalAlreadySavedAndCerbosDetected_KeepsSavedLocalSelection()
    {
        SetupIncompleteOnboardingStatus();
        SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "local",
            CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
            CerbosDetectedFromEnvironment = true,
            CerbosEndpointVerified = true,
            AuthorizationProviderConfigured = true,
            CerbosEndpointOwnership = BootstrapOwnership()
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Provider selected: Local RBAC", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Continue with Local RBAC", StringComparison.OrdinalIgnoreCase) ||
                cut.Markup.Contains("Continue with detected settings", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected saved Local RBAC choice to win over deployment bootstrap Cerbos detection.");
            }
        });
    }

    [Test]
    public async Task VerifyCerbos_WhenBrowserCommandSucceeds_ShowsPdpReachableButPoliciesUnknown()
    {
        SetupIncompleteOnboardingStatus();
        var module = SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
            CerbosDetectedFromEnvironment = false,
            CerbosEndpointVerified = false
        });
        SetupCommand(module, "POST", "/api/InstanceOnboarding/authz-provider-configuration/verify", new InstanceCommandResponseModel
        {
            Success = true,
            Message = "Cerbos PDP endpoint verified successfully."
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Retest endpoint", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Authorization provider page did not finish loading.");
            }
        });

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Retest endpoint", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Cerbos PDP endpoint verified successfully", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Runtime endpoint: Reachable", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Policy package: Policies not verified", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected reachable PDP status with unknown policy readiness was not rendered.");
            }
        });
    }

    [Test]
    public async Task Load_WhenLocalSelected_StillShowsCompactPolicyPackageDownload()
    {
        SetupIncompleteOnboardingStatus();
        SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "local"
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Policy package", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("/api/InstanceOnboarding/authz-provider-configuration/package", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Download ZIP", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Optional. Useful if you plan to migrate to Cerbos later", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected compact always-visible policy package download affordance was not rendered.");
            }
        });
    }

    [Test]
    public async Task SaveLocal_WhenBrowserCommandSucceeds_RedirectsToLogin()
    {
        SetupIncompleteOnboardingStatus();
        var module = SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "local"
        });
        SetupCommand(module, "PUT", "/api/InstanceOnboarding/authz-provider-configuration", new InstanceCommandResponseModel
        {
            Success = true,
            Message = "Authorization provider saved."
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.FindAll("button")
                    .Any(button => button.TextContent.Contains("Continue with Local RBAC", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Authorization provider page did not finish loading.");
            }
        });

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Continue with Local RBAC", StringComparison.OrdinalIgnoreCase))
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
    public async Task SaveCerbos_WhenPdpVerifiedAndPoliciesManuallyConfirmed_SavesWithoutBrowserPolicySyncAndRedirectsToLogin()
    {
        SetupIncompleteOnboardingStatus();
        var module = SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
            CerbosEndpointVerified = true
        });
        SetupCommand(module, "PUT", "/api/InstanceOnboarding/authz-provider-configuration", new InstanceCommandResponseModel
        {
            Success = true,
            Message = "Authorization provider saved."
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("I have manually installed the Cerbos policy package", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Manual policy confirmation was not rendered.");
            }
        });

        cut.Find("input[type=checkbox]").Change(true);

        cut.WaitForAssertion(() =>
        {
            if (!cut.FindAll("button")
                    .Any(button => button.TextContent.Contains("Continue with Cerbos", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Expected Cerbos continuation after manual policy confirmation.");
            }
        });

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Continue with Cerbos", StringComparison.OrdinalIgnoreCase))
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
    public async Task SyncNow_WhenServerCredentialsConfiguredAndSyncFails_ShowsSafeManualFallback()
    {
        SetupIncompleteOnboardingStatus();
        var module = SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
            CerbosEndpointVerified = true,
            CerbosAdminUsernameConfigured = true,
            CerbosAdminPasswordConfigured = true
        });
        SetupCommand(module, "POST", "/api/InstanceOnboarding/authz-provider-configuration/sync", new InstanceCommandResponseModel
        {
            Success = false,
            Message = "Authorization policy package sync failed.",
            Errors = ["Configure Cerbos Admin API credentials before publishing."]
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Sync now", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Sync policies now", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Server-side sync affordance was not rendered.");
            }
        });

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Sync now", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authorization policy package sync failed", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("I have manually installed the Cerbos policy package", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected sync failure and manual policy confirmation to be displayed.");
            }
        });
    }

    [Test]
    public async Task SyncPolicies_WhenServerCredentialsConfiguredAndSyncSucceeds_AllowsCerbosContinuation()
    {
        SetupIncompleteOnboardingStatus();
        var module = SetupFetchConfiguration(new AuthorizationProviderConfigurationModel
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "cerbosgrpc.openislamu.org:443",
            CerbosEndpointVerified = true,
            CerbosAdminUsernameConfigured = true,
            CerbosAdminPasswordConfigured = true
        });
        SetupCommand(module, "POST", "/api/InstanceOnboarding/authz-provider-configuration/sync", new InstanceCommandResponseModel
        {
            Success = true,
            Message = "Policies synced. You can continue with Cerbos."
        });

        var cut = _ctx.RenderMudComponent<AuthorizationProviderConfiguration>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.FindAll("button")
                    .Any(button => button.TextContent.Contains("Sync policies now", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Primary sync CTA was not rendered.");
            }
        });

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Sync policies now", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Policies synced. You can continue with Cerbos.", StringComparison.OrdinalIgnoreCase) ||
                !cut.Markup.Contains("Policy package: Policies confirmed", StringComparison.OrdinalIgnoreCase) ||
                !cut.FindAll("button").Any(button => button.TextContent.Contains("Continue with Cerbos", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Expected successful policy sync to unlock Cerbos continuation.");
            }
        });
    }

    private void SetupIncompleteOnboardingStatus()
    {
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });
    }

    private BunitJSModuleInterop SetupFetchConfiguration(AuthorizationProviderConfigurationModel model)
    {
        var module = _ctx.JSInterop.SetupModule("/js/bff.js");
        module.Setup<AuthorizationProviderConfigurationModel>("fetchJson", invocation =>
                invocation.Arguments.Count > 0 &&
                string.Equals(
                    invocation.Arguments[0]?.ToString(),
                    "/api/InstanceOnboarding/authz-provider-configuration/internal",
                    StringComparison.Ordinal))
            .SetResult(model);
        return module;
    }

    private static void SetupCommand(
        BunitJSModuleInterop module,
        string method,
        string path,
        InstanceCommandResponseModel response)
    {
        module.Setup<InstanceCommandResponseModel>("sendCommand", invocation =>
                invocation.Arguments.Count >= 3 &&
                string.Equals(invocation.Arguments[0]?.ToString(), method, StringComparison.Ordinal) &&
                string.Equals(invocation.Arguments[1]?.ToString(), path, StringComparison.Ordinal))
            .SetResult(response);
    }

    private static SecretOwnershipModel BootstrapOwnership() => new()
    {
        Mode = "application-managed",
        Source = "deployment-bootstrap",
        Badge = "Bootstrap from Deployment",
        Description = "These values were detected from environment variables. If you modify them, saved application settings will be used from now on.",
        Editable = true,
        BootstrapAvailable = true
    };
}

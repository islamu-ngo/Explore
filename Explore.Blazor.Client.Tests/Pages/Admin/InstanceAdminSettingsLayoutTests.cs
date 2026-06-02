// ABOUTME: bUnit tests for instance admin settings layout section reachability.
// ABOUTME: Verifies single-tenant administration exposes tenant-level public experience controls.

using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Tests.Common.Authentication;
using System.Reflection;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public sealed class InstanceAdminSettingsLayoutTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _instanceOnboardingService;
    private readonly ITenantOnboardingService _tenantOnboardingService;
    private readonly ITenantPublicExperienceAdminService _publicExperienceAdminService;
    private readonly IOrganizationService _organizationService;
    private readonly IUserService _userService;

    public InstanceAdminSettingsLayoutTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.AddShellStateMocks();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");

        _instanceOnboardingService = _ctx.AddMockService<IInstanceOnboardingService>();
        _tenantOnboardingService = _ctx.AddMockService<ITenantOnboardingService>();
        _publicExperienceAdminService = _ctx.AddMockService<ITenantPublicExperienceAdminService>();
        _organizationService = _ctx.AddMockService<IOrganizationService>();
        _userService = _ctx.AddMockService<IUserService>();

        ConfigureSingleTenantInstanceDefaults();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task InstanceAdminSettingsLayout_SingleTenant_ExposesPublicExperienceNavigation()
    {
        // Arrange
        Type componentType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "InstanceAdminSettingsLayout" && typeof(IComponent).IsAssignableFrom(type));

        // Act
        IRenderedComponent<DynamicComponent> cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, componentType));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Public Experience", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Public Experience navigation item was not rendered.");
            }
        });

        await Assert.That(cut.Markup).Contains("Public Experience", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_SingleTenant_RendersPublicExperienceSectionWhenSelected()
    {
        // Arrange
        Type componentType = typeof(EventList).Assembly
            .GetTypes()
            .First(type => type.Name == "InstanceAdminSettingsLayout" && typeof(IComponent).IsAssignableFrom(type));

        IRenderedComponent<DynamicComponent> cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, componentType));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Public Experience", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Public Experience navigation item was not rendered.");
            }
        });

        // Act
        object layout = cut.Instance.Instance
            ?? throw new InvalidOperationException("Dynamic component did not expose the rendered layout instance.");
        layout.GetType()
            .GetField("_currentSection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(layout, "public-experience");
        layout.GetType()
            .GetField("_showMobileMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(layout, false);

        await cut.InvokeAsync(() => typeof(ComponentBase)
            .GetMethod("StateHasChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(layout, null));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Choose what anonymous visitors see after launch", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Public Experience settings section was not rendered.");
            }
        });

        await Assert.That(cut.Markup).Contains("Event catalog label", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Advanced home blocks JSON", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_RendersAuthenticationAndAuthorizationProviderSettings()
    {
        // Arrange
        _instanceOnboardingService.GetAuthProviderConfigurationAsAdminAsync()
            .Returns(new AuthProviderConfigurationModel
            {
                KeycloakEnabled = true,
                KeycloakDetectedFromEnvironment = true
            });
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationModel
            {
                Provider = "local",
                CerbosDetectedFromEnvironment = true,
                CerbosGrpcEndpoint = "cerbosgrpc.local:3593"
            });

        Type componentType = GetLayoutComponentType();
        IRenderedComponent<DynamicComponent> cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, componentType));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Provider navigation item was not rendered.");
            }
        });

        // Act
        object layout = GetRenderedLayout(cut);
        SetPrivateField(layout, "_currentSection", "auth-providers");
        SetPrivateField(layout, "_showMobileMenu", false);
        await InvokeStateHasChangedAsync(cut, layout);

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authentication Providers", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Authorization Providers", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Provider settings sections were not rendered.");
            }
        });

        await Assert.That(cut.Markup).Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Authentication Providers", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Authorization Providers", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Keycloak", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Disable Keycloak", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Local (Built-in RBAC)", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Cerbos (External PDP)", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Enable Cerbos Authorization", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_AuthProvidersSave_UpdatesAuthenticationAndAuthorizationProviders()
    {
        // Arrange
        _instanceOnboardingService.GetAuthProviderConfigurationAsAdminAsync()
            .Returns(new AuthProviderConfigurationModel { KeycloakEnabled = true });
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationModel { Provider = "cerbos", CerbosGrpcEndpoint = "cerbosgrpc.local:3593" });
        _instanceOnboardingService.UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationModel>())
            .Returns(new InstanceCommandResponseModel { Success = true });
        _instanceOnboardingService.UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationModel>())
            .Returns(new InstanceCommandResponseModel { Success = true });
        _instanceOnboardingService.RefreshAuthSchemesAsync().Returns(Task.CompletedTask);

        Type componentType = GetLayoutComponentType();
        IRenderedComponent<DynamicComponent> cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, componentType));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Provider navigation item was not rendered.");
            }
        });

        object layout = GetRenderedLayout(cut);
        SetPrivateField(layout, "_currentSection", "auth-providers");

        // Act
        await InvokePrivateTaskAsync(layout, "SaveAsync");

        // Assert
        await _instanceOnboardingService.Received(1)
            .UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationModel>());
        await _instanceOnboardingService.Received(1)
            .UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationModel>());
        await _instanceOnboardingService.Received(1).RefreshAuthSchemesAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_CerbosPolicySync_InvokesAdminBffSync()
    {
        // Arrange
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationModel
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.local:3593",
                CerbosEndpointVerified = true,
                CerbosAdminUsernameConfigured = true,
                CerbosAdminPasswordConfigured = true
            });
        _instanceOnboardingService.SyncAuthorizationPolicyPackageAsAdminAsync()
            .Returns(new InstanceCommandResponseModel
            {
                Success = true,
                Message = "Authorization policy package synced."
            });

        IRenderedComponent<DynamicComponent> cut = await RenderAuthProvidersSectionAsync();

        // Act
        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Sync Policies", StringComparison.OrdinalIgnoreCase))
            .Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authorization policy package synced.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Policy sync success message was not rendered.");
            }
        });
        await _instanceOnboardingService.Received(1).SyncAuthorizationPolicyPackageAsAdminAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_CerbosPolicySync_FailureShowsSafeMessage()
    {
        // Arrange
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationModel
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.local:3593",
                CerbosEndpointVerified = true,
                CerbosAdminUsernameConfigured = true,
                CerbosAdminPasswordConfigured = true
            });
        _instanceOnboardingService.SyncAuthorizationPolicyPackageAsAdminAsync()
            .Returns(new InstanceCommandResponseModel
            {
                Success = false,
                Message = "secret-token leaked by backend",
                Errors = ["stack trace"]
            });

        IRenderedComponent<DynamicComponent> cut = await RenderAuthProvidersSectionAsync();

        // Act
        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Sync Policies", StringComparison.OrdinalIgnoreCase))
            .Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authorization policy package sync failed. Check Admin API settings and retry.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Safe policy sync failure message was not rendered.");
            }
        });
        await Assert.That(cut.Markup).DoesNotContain("secret-token", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("stack trace", StringComparison.OrdinalIgnoreCase);
        await _instanceOnboardingService.Received(1).SyncAuthorizationPolicyPackageAsAdminAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_CerbosPolicySync_DisabledUntilEndpointVerified()
    {
        // Arrange
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationModel
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.local:3593",
                CerbosEndpointVerified = false
            });

        IRenderedComponent<DynamicComponent> cut = await RenderAuthProvidersSectionAsync();

        // Act
        var syncButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Sync Policies", StringComparison.OrdinalIgnoreCase));

        // Assert
        await Assert.That(syncButton.HasAttribute("disabled")).IsTrue();
        await Assert.That(cut.Markup).Contains("Save and verify the Cerbos endpoint before syncing authorization policies.", StringComparison.OrdinalIgnoreCase);
        await _instanceOnboardingService.DidNotReceive().SyncAuthorizationPolicyPackageAsAdminAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_CerbosPolicyDownload_AvailableWithoutVerifiedEndpointAndShowsSafeFailure()
    {
        // Arrange
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationModel
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.local:3593",
                CerbosEndpointVerified = false
            });
        _instanceOnboardingService.DownloadAuthorizationPolicyPackageAsAdminAsync()
            .Returns(Task.FromResult<PolicyPackageDownloadModel?>(null));

        IRenderedComponent<DynamicComponent> cut = await RenderAuthProvidersSectionAsync();

        // Act
        var downloadButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Download ZIP", StringComparison.OrdinalIgnoreCase));
        downloadButton.Click();

        // Assert
        await Assert.That(downloadButton.HasAttribute("disabled")).IsFalse();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authorization policy package download failed. Try again or check server logs.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Safe policy package download failure message was not rendered.");
            }
        });
        await _instanceOnboardingService.Received(1).DownloadAuthorizationPolicyPackageAsAdminAsync();
        await _instanceOnboardingService.DidNotReceive().SyncAuthorizationPolicyPackageAsAdminAsync();
    }

    private void ConfigureSingleTenantInstanceDefaults()
    {
        _instanceOnboardingService.GetStatusAsync()
            .Returns(new InstanceOnboardingStatusModel
            {
                IsCompleted = true,
                IsAuthenticated = true,
                IsCurrentUserInstanceAdmin = true,
                SelectedDeploymentMode = "SingleTenant"
            });
        _userService.GetAdminAuthorityAsync()
            .Returns(new AdminAuthorityDto
            {
                IsInstanceAdmin = true,
                AdminTenantIds = [AuthenticationTestConstants.DefaultTenantId],
                AdminOrganizationIds = [],
                HasAnyAuthority = true
            });
        _instanceOnboardingService.GetDeploymentModeAsync().Returns(new DeploymentModeModel { Mode = "SingleTenant" });
        _instanceOnboardingService.GetModuleSettingsAsync().Returns(new ModuleSettingsModel());
        _instanceOnboardingService.GetEventPolicyAsync().Returns(new EventPolicyModel());
        _instanceOnboardingService.GetOrganizationPolicyAsync().Returns(new OrganizationPolicyModel());
        _instanceOnboardingService.GetBrandingSettingsAsync().Returns(new BrandingSettingsModel());
        _instanceOnboardingService.GetDomainSettingsAsync().Returns(new DomainSettingsModel());
        _instanceOnboardingService.GetTenantDelegationAsync().Returns(new TenantDelegationModel());
        _instanceOnboardingService.GetRenderPolicyAsync().Returns(new RenderPolicyModel());
        _instanceOnboardingService.GetStorageSettingsAsync().Returns(new InstanceStorageSettingsModel());
        _instanceOnboardingService.GetSmtpSettingsAsync().Returns(new InstanceSmtpSettingsModel());
        _instanceOnboardingService.GetAuthProviderConfigurationAsAdminAsync().Returns(new AuthProviderConfigurationModel());
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationModel { Provider = "local" });
        _instanceOnboardingService.UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationModel>())
            .Returns(new InstanceCommandResponseModel { Success = true });
        _instanceOnboardingService.UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationModel>())
            .Returns(new InstanceCommandResponseModel { Success = true });
        _instanceOnboardingService.RefreshAuthSchemesAsync().Returns(Task.CompletedTask);
        _instanceOnboardingService.RefreshAuthSessionAsync().Returns(true);
        _instanceOnboardingService.GetAnalyticsGovernanceSettingsAsync()
            .Returns(new Explore.Blazor.Client.Models.Analytics.AnalyticsGovernanceSettingsModel());
        _instanceOnboardingService.GetFooterGovernanceSettingsAsync().Returns(new FooterGovernanceSettingsModel());
        _instanceOnboardingService.GetActiveTenantCountAsync().Returns(1);

        _tenantOnboardingService.GetSettingsAsync().Returns(new TenantPolicySettingsModel());
        _publicExperienceAdminService.ApplySingleTenantPolicySettingsAsync(
                Arg.Any<TenantPolicySettingsModel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _publicExperienceAdminService.ApplyAnnouncementBarSettingsAsync(
                Arg.Any<TenantPolicySettingsModel>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _publicExperienceAdminService.GetSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantPublicExperienceAdminModel());
        _organizationService.GetOrganizationsPagedAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(PaginatedResult<OrganizationListDto>.Empty());
    }

    private static Type GetLayoutComponentType() => typeof(EventList).Assembly
        .GetTypes()
        .First(type => type.Name == "InstanceAdminSettingsLayout" && typeof(IComponent).IsAssignableFrom(type));

    private static object GetRenderedLayout(IRenderedComponent<DynamicComponent> cut) => cut.Instance.Instance
        ?? throw new InvalidOperationException("Dynamic component did not expose the rendered layout instance.");

    private async Task<IRenderedComponent<DynamicComponent>> RenderAuthProvidersSectionAsync()
    {
        Type componentType = GetLayoutComponentType();
        IRenderedComponent<DynamicComponent> cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, componentType));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Provider navigation item was not rendered.");
            }
        });

        object layout = GetRenderedLayout(cut);
        SetPrivateField(layout, "_currentSection", "auth-providers");
        SetPrivateField(layout, "_showMobileMenu", false);
        await InvokeStateHasChangedAsync(cut, layout);

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Policy package sync", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Policy sync affordance was not rendered.");
            }
        });

        return cut;
    }

    private static void SetPrivateField(object instance, string fieldName, object value) => instance.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
        .SetValue(instance, value);

    private static async Task InvokeStateHasChangedAsync(IRenderedComponent<DynamicComponent> cut, object layout) =>
        await cut.InvokeAsync(() => typeof(ComponentBase)
            .GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(layout, null));

    private static async Task InvokePrivateTaskAsync(object instance, string methodName)
    {
        var task = (Task)instance.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!
            .Invoke(instance, null)!;

        await task;
    }
}

// ABOUTME: bUnit tests for instance admin settings layout section reachability.
// ABOUTME: Verifies single-tenant administration exposes tenant-level public experience controls.

using System.Reflection;
using Explore.Blazor.Client.Contracts.Services.ControlPlane;
using Explore.Blazor.Client.Contracts.Services.Federation;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Tests.Common.Authentication;
using MudBlazor;

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
        _ctx.AddMockService<IControlPlaneOperationsService>();

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
    public async Task InstanceAdminSettingsLayout_SingleTenantInstanceOnly_HidesTenantAdministrationSections()
    {
        _userService.GetAdminAuthorityAsync()
            .Returns(new AdminAuthorityDto
            {
                IsInstanceAdmin = true,
                AdminTenantIds = [],
                AdminOrganizationIds = [],
                HasAnyAuthority = true
            });
        _tenantOnboardingService.GetStatusAsync()
            .Returns(new TenantOnboardingStatusDto
            {
                IsAuthenticated = true,
                IsCurrentUserTenantAdministrator = false,
                IsCurrentUserPlatformAdministrator = true
            });

        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));

        cut.WaitForState(() => cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase));
        var navLabels = cut.FindAll("[role='option']")
            .Select(item => item.TextContent.Trim())
            .ToArray();
        await Assert.That(navLabels).DoesNotContain("Public Experience");
        await Assert.That(navLabels).DoesNotContain("Members");
        await Assert.That(navLabels).DoesNotContain("Organizations");
        await Assert.That(navLabels).DoesNotContain("Policies");
        cut.FindAll("[role='option']")
            .Single(item => item.TextContent.Trim() == "Advanced")
            .Click();
        cut.WaitForState(() => cut.Markup.Contains("AT Protocol Event Federation", StringComparison.Ordinal));
        await _ctx.Services.GetRequiredService<IAtprotoFederationSettingsService>().Received(1)
            .GetInstanceAsync(Arg.Any<CancellationToken>());
        await _publicExperienceAdminService.DidNotReceive()
            .ApplySingleTenantPolicySettingsAsync(Arg.Any<TenantPolicySettingsDto>(), Arg.Any<CancellationToken>());
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
            .Returns(new AuthProviderConfigurationDto
            {
                KeycloakEnabled = true,
                KeycloakDetectedFromEnvironment = true
            });
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationDto
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
    public async Task InstanceAdminSettingsLayout_WhenIdentityIsNotReady_ShowsInlineRecoveryInsteadOfProviderEditor()
    {
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationDto());

        IRenderedComponent<DynamicComponent> cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("admin session is still becoming ready", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected inline identity-readiness guidance.");
            }
        });

        await Assert.That(cut.Markup).Contains("Reload settings", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("sign out and back in", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Exactly one authorization provider is active", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Enable Cerbos Authorization", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_AuthProvidersSave_UpdatesAuthenticationAndAuthorizationProviders()
    {
        // Arrange
        _instanceOnboardingService.GetAuthProviderConfigurationAsAdminAsync()
            .Returns(new AuthProviderConfigurationDto { KeycloakEnabled = true });
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationDto { Provider = "cerbos", CerbosGrpcEndpoint = "cerbosgrpc.local:3593" });
        _instanceOnboardingService.UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        _instanceOnboardingService.UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
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
            .UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _instanceOnboardingService.Received(1)
            .UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationDto>());
        await _instanceOnboardingService.Received(1).RefreshAuthSchemesAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_AuthProvidersSaveFailure_ReloadsBothAuthoritativeModels()
    {
        _instanceOnboardingService.UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = false, Message = "Authentication update failed." });
        _instanceOnboardingService.UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

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

        await InvokePrivateTaskAsync(layout, "SaveAsync");

        await _instanceOnboardingService.Received(2).GetAuthProviderConfigurationAsAdminAsync();
        await _instanceOnboardingService.Received(2).GetAuthorizationProviderConfigurationAsAdminAsync();
        await _instanceOnboardingService.DidNotReceive().RefreshAuthSchemesAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_SparseSaveFailures_ReloadAuthoritativeSectionModels()
    {
        var failure = new BaseCommandResponseOfGuid { Success = false, Message = "Update failed." };
        _instanceOnboardingService.UpdateBrandingSettingsAsync(Arg.Any<BrandingSettingsDto>()).Returns(failure);
        _instanceOnboardingService.UpdateDomainSettingsAsync(Arg.Any<DomainSettingsDto>()).Returns(failure);
        _instanceOnboardingService.UpdateAnalyticsGovernanceSettingsAsync(Arg.Any<AnalyticsGovernanceSettingsDto>()).Returns(failure);

        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));
        cut.WaitForState(() => cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase));
        object layout = GetRenderedLayout(cut);

        await InvokePrivateTaskAsync(layout, "SaveBrandingAsync", new BrandingSettingsDto { LockTenantBrandDisplayName = true });
        await InvokePrivateTaskAsync(layout, "SaveDomainAsync", new DomainSettingsDto { AllowTenantCustomDomains = true });
        await InvokePrivateTaskAsync(layout, "SaveAnalyticsAsync", new AnalyticsGovernanceSettingsDto { GlobalDisableClientTracking = true });

        await _instanceOnboardingService.Received(2).GetBrandingSettingsAsync();
        await _instanceOnboardingService.Received(2).GetDomainSettingsAsync();
        await _instanceOnboardingService.Received(2).GetAnalyticsGovernanceSettingsAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_FailedModuleAutosave_RendersAuthoritativeValue()
    {
        _instanceOnboardingService.GetModuleSettingsAsync()
            .Returns(
                new ModuleSettingsDto { EnableIslamicModule = false },
                new ModuleSettingsDto { EnableIslamicModule = false });
        _instanceOnboardingService.UpdateModuleSettingsAsync(Arg.Any<ModuleSettingsDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = false, Message = "Module update failed." });
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));
        cut.WaitForState(() => cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase));
        object layout = GetRenderedLayout(cut);
        SetPrivateField(layout, "_currentSection", "modules");
        SetPrivateField(layout, "_showMobileMenu", false);
        await InvokeStateHasChangedAsync(cut, layout);
        var moduleSwitch = cut.FindComponents<MudSwitch<bool>>()
            .Single(component => component.Markup.Contains("Enable Islamic module", StringComparison.Ordinal));

        await cut.InvokeAsync(() => moduleSwitch.Instance.ValueChanged.InvokeAsync(true));
        cut.WaitForAssertion(() =>
        {
            var restored = cut.FindComponents<MudSwitch<bool>>()
                .Single(component => component.Markup.Contains("Enable Islamic module", StringComparison.Ordinal));
            if (restored.Instance.Value)
            {
                throw new InvalidOperationException("The failed autosave did not render the authoritative module value.");
            }
        });

        await _instanceOnboardingService.Received(2).GetModuleSettingsAsync();
        await Assert.That(cut.Find("[role='alert']").TextContent).Contains("Module update failed.", StringComparison.Ordinal);
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_OrdinaryPolicyAndAiSections_HaveNoBroadSaveButton()
    {
        _instanceOnboardingService.GetAiAssistantGovernanceSettingsAsync()
            .Returns(new AiAssistantGovernanceSettingsDto
            {
                Enabled = true,
                Provider = "openai-compatible",
                EndpointUrl = "https://ai.example.test/v1",
                ModelId = "model-a"
            });
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));
        cut.WaitForState(() => cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase));
        object layout = GetRenderedLayout(cut);

        SetPrivateField(layout, "_currentSection", "policies");
        SetPrivateField(layout, "_showMobileMenu", false);
        await InvokeStateHasChangedAsync(cut, layout);
        await Assert.That(cut.Markup).DoesNotContain("Save Settings", StringComparison.Ordinal);

        SetPrivateField(layout, "_currentSection", "ai");
        await InvokeStateHasChangedAsync(cut, layout);
        await Assert.That(cut.Markup).DoesNotContain("Save Settings", StringComparison.Ordinal);
        await Assert.That(cut.Markup).Contains("Save provider configuration", StringComparison.Ordinal);
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_FailedGovernanceAutosave_RendersAuthoritativeValue()
    {
        _instanceOnboardingService.GetMcpGovernanceSettingsAsync()
            .Returns(
                new McpGovernanceSettingsDto { Enabled = false },
                new McpGovernanceSettingsDto { Enabled = false });
        _instanceOnboardingService.UpdateMcpGovernanceSettingsAsync(Arg.Any<McpGovernanceSettingsDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = false, Message = "MCP update failed." });
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));
        cut.WaitForState(() => cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase));
        object layout = GetRenderedLayout(cut);
        SetPrivateField(layout, "_currentSection", "advanced");
        SetPrivateField(layout, "_showMobileMenu", false);
        await InvokeStateHasChangedAsync(cut, layout);
        cut.WaitForState(() => cut.Markup.Contains("Enable MCP adapter at runtime", StringComparison.Ordinal));
        var mcpSwitch = cut.FindComponents<MudSwitch<bool>>()
            .Single(component => component.Markup.Contains("Enable MCP adapter at runtime", StringComparison.Ordinal));

        await cut.InvokeAsync(() => mcpSwitch.Instance.ValueChanged.InvokeAsync(true));
        cut.WaitForAssertion(() =>
        {
            var restored = cut.FindComponents<MudSwitch<bool>>()
                .Single(component => component.Markup.Contains("Enable MCP adapter at runtime", StringComparison.Ordinal));
            if (restored.Instance.Value)
            {
                throw new InvalidOperationException("The failed governance autosave did not render the authoritative MCP value.");
            }
        });

        await _instanceOnboardingService.Received(2).GetMcpGovernanceSettingsAsync();
        await Assert.That(cut.FindAll("[role='alert']").Any(alert =>
            alert.TextContent.Contains("MCP update failed.", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_FailedAiAutosave_RendersAuthoritativeValue()
    {
        _instanceOnboardingService.GetAiAssistantGovernanceSettingsAsync()
            .Returns(
                new AiAssistantGovernanceSettingsDto { Enabled = false },
                new AiAssistantGovernanceSettingsDto { Enabled = false });
        _instanceOnboardingService.UpdateAiAssistantGovernanceSettingsAsync(Arg.Any<AiAssistantGovernanceSettingsDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = false, Message = "AI update failed." });
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));
        cut.WaitForState(() => cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase));
        object layout = GetRenderedLayout(cut);
        SetPrivateField(layout, "_currentSection", "ai");
        SetPrivateField(layout, "_showMobileMenu", false);
        await InvokeStateHasChangedAsync(cut, layout);
        var aiSwitch = cut.FindComponents<MudSwitch<bool>>()
            .Single(component => component.Markup.Contains("Enable AI Assistant", StringComparison.Ordinal));

        await cut.InvokeAsync(() => aiSwitch.Instance.ValueChanged.InvokeAsync(true));
        cut.WaitForAssertion(() =>
        {
            var restored = cut.FindComponents<MudSwitch<bool>>()
                .Single(component => component.Markup.Contains("Enable AI Assistant", StringComparison.Ordinal));
            if (restored.Instance.Value)
            {
                throw new InvalidOperationException("The failed AI autosave did not render the authoritative value.");
            }
        });

        await _instanceOnboardingService.Received(2).GetAiAssistantGovernanceSettingsAsync();
        await Assert.That(cut.FindAll("[role='alert']").Any(alert =>
            alert.TextContent.Contains("AI update failed.", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_FailedAiProviderSave_ReloadsAuthoritativeModel()
    {
        _instanceOnboardingService.UpdateAiAssistantProviderConfigurationAsync(Arg.Any<AiAssistantProviderConfigurationWriteDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = false, Message = "AI provider update failed." });
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetLayoutComponentType()));
        cut.WaitForState(() => cut.Markup.Contains("Authentication and Authorization Providers", StringComparison.OrdinalIgnoreCase));
        object layout = GetRenderedLayout(cut);

        await InvokePrivateTaskAsync(layout, "SaveAiProviderConfigurationAsync", new AiAssistantProviderConfigurationWriteDto
        {
            Provider = "openai-compatible",
            EndpointUrl = "https://ai.example.test/v1",
            ApiKey = "replacement-key",
            ModelId = "model-a",
            AllowedModelIds = ["model-a"]
        });

        await _instanceOnboardingService.Received(2).GetAiAssistantGovernanceSettingsAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_DeploymentManagedAuthorization_KeepsAuthenticationSaveAvailable()
    {
        _instanceOnboardingService.GetAuthProviderConfigurationAsAdminAsync()
            .Returns(new AuthProviderConfigurationDto { KeycloakEnabled = true });
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationDto
            {
                Provider = "local",
                AuthorizationProviderManagedByDeployment = true,
                AuthorizationProviderBootstrapStatus = "ready"
            });

        IRenderedComponent<DynamicComponent> cut = await RenderAuthProvidersSectionAsync();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authorization is managed by deployment configuration", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Enable Cerbos Authorization", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected read-only deployment authorization with authentication controls intact.");
            }
        });

        object layout = GetRenderedLayout(cut);
        await InvokePrivateTaskAsync(layout, "SaveAsync");

        await _instanceOnboardingService.Received(1)
            .UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationDto>());
        await _instanceOnboardingService.DidNotReceive()
            .UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationDto>());
        await _instanceOnboardingService.Received(1).RefreshAuthSchemesAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_CerbosPolicySync_InvokesAdminBffSync()
    {
        // Arrange
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationDto
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.local:3593",
                CerbosEndpointVerified = true,
                CerbosAdminUsernameConfigured = true,
                CerbosAdminPasswordConfigured = true
            });
        _instanceOnboardingService.SyncAuthorizationPolicyPackageAsAdminAsync()
            .Returns(new BaseCommandResponseOfGuid
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
            .Returns(new AuthorizationProviderConfigurationDto
            {
                Provider = "cerbos",
                CerbosGrpcEndpoint = "cerbosgrpc.local:3593",
                CerbosEndpointVerified = true,
                CerbosAdminUsernameConfigured = true,
                CerbosAdminPasswordConfigured = true
            });
        _instanceOnboardingService.SyncAuthorizationPolicyPackageAsAdminAsync()
            .Returns(new BaseCommandResponseOfGuid
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
            .Returns(new AuthorizationProviderConfigurationDto
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
    public async Task InstanceAdminSettingsLayout_DeploymentManagedCerbosFailure_EnablesServerRetry()
    {
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationDto
            {
                Provider = "cerbos",
                AuthorizationProviderManagedByDeployment = true,
                AuthorizationProviderBootstrapStatus = "failed",
                AuthorizationProviderBootstrapMessage = "The deployment-managed Cerbos PDP endpoint could not be reached.",
                CerbosGrpcEndpoint = "http://cerbos:3593",
                CerbosEndpointVerified = false,
                CerbosEndpointOwnership = new SecretOwnershipDto
                {
                    Mode = "deployment-managed",
                    Badge = "Managed by Deployment",
                    Editable = false
                }
            });
        _instanceOnboardingService.SyncAuthorizationPolicyPackageAsAdminAsync()
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Automatic Cerbos setup did not complete."
            });

        IRenderedComponent<DynamicComponent> cut = await RenderAuthProvidersSectionAsync();
        var retryButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Retry Authorization Setup", StringComparison.OrdinalIgnoreCase));

        await Assert.That(retryButton.HasAttribute("disabled")).IsFalse();
        retryButton.Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Authorization policy package sync failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected safe retry failure feedback.");
            }
        });
        await _instanceOnboardingService.Received(1).SyncAuthorizationPolicyPackageAsAdminAsync();
    }

    [Test]
    public async Task InstanceAdminSettingsLayout_DeploymentManagedCerbosRetry_RefreshesReadiness()
    {
        var failed = new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            AuthorizationProviderManagedByDeployment = true,
            AuthorizationProviderBootstrapStatus = "failed",
            CerbosGrpcEndpoint = "http://cerbos:3593",
            CerbosEndpointVerified = false,
            CerbosEndpointOwnership = new SecretOwnershipDto
            {
                Mode = "deployment-managed",
                Badge = "Managed by Deployment",
                Editable = false
            }
        };
        var ready = new AuthorizationProviderConfigurationDto
        {
            Provider = "cerbos",
            AuthorizationProviderManagedByDeployment = true,
            AuthorizationProviderConfigured = true,
            AuthorizationProviderBootstrapStatus = "ready",
            CerbosGrpcEndpoint = "http://cerbos:3593",
            CerbosEndpointVerified = true,
            CerbosPoliciesSynchronized = true,
            CerbosEndpointOwnership = failed.CerbosEndpointOwnership
        };
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(failed, ready);
        _instanceOnboardingService.SyncAuthorizationPolicyPackageAsAdminAsync()
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Cerbos endpoint verification and policy synchronization completed."
            });

        IRenderedComponent<DynamicComponent> cut = await RenderAuthProvidersSectionAsync();
        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Retry Authorization Setup", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Verified", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Cerbos endpoint verification and policy synchronization completed.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected refreshed deployment readiness after a successful retry.");
            }
        });
        await _instanceOnboardingService.Received(2)
            .GetAuthorizationProviderConfigurationAsAdminAsync();
    }

    private void ConfigureSingleTenantInstanceDefaults()
    {
        _instanceOnboardingService.GetStatusAsync()
            .Returns(new InstanceOnboardingStatusDto
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
        _instanceOnboardingService.GetDeploymentModeAsync()
            .Returns(new DeploymentModeDto { Mode = DeploymentMode.SingleTenant });
        _instanceOnboardingService.GetModuleSettingsAsync().Returns(new ModuleSettingsDto());
        _instanceOnboardingService.GetEventPolicyAsync().Returns(new EventPolicyDto());
        _instanceOnboardingService.GetOrganizationPolicyAsync().Returns(new OrganizationPolicyDto());
        _instanceOnboardingService.GetBrandingSettingsAsync().Returns(new BrandingSettingsDto());
        _instanceOnboardingService.GetDomainSettingsAsync().Returns(new DomainSettingsDto());
        _instanceOnboardingService.GetTenantDelegationAsync().Returns(new TenantDelegationSettingsDto());
        _instanceOnboardingService.GetRenderPolicyAsync().Returns(new RenderPolicySettingsDto());
        _instanceOnboardingService.GetStorageSettingsAsync().Returns(new HalResourceOfInstanceStorageSettingsDto());
        _instanceOnboardingService.GetSmtpSettingsAsync().Returns(new InstanceSmtpSettingsDto());
        _instanceOnboardingService.GetAuthProviderConfigurationAsAdminAsync().Returns(new AuthProviderConfigurationDto());
        _instanceOnboardingService.GetAuthorizationProviderConfigurationAsAdminAsync()
            .Returns(new AuthorizationProviderConfigurationDto { Provider = "local" });
        _instanceOnboardingService.UpdateAuthProviderConfigurationAsAdminAsync(Arg.Any<AuthProviderConfigurationDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        _instanceOnboardingService.UpdateAuthorizationProviderConfigurationAsAdminAsync(Arg.Any<AuthorizationProviderConfigurationDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });
        _instanceOnboardingService.RefreshAuthSchemesAsync().Returns(Task.CompletedTask);
        _instanceOnboardingService.RefreshAuthSessionAsync().Returns(true);
        _instanceOnboardingService.GetAnalyticsGovernanceSettingsAsync()
            .Returns(new AnalyticsGovernanceSettingsDto());
        _instanceOnboardingService.GetFooterGovernanceSettingsAsync().Returns(new FooterGovernanceSettingsDto());
        _instanceOnboardingService.GetActiveTenantCountAsync().Returns(1);

        _tenantOnboardingService.GetSettingsAsync().Returns(new TenantPolicySettingsDto());
        _publicExperienceAdminService.ApplySingleTenantPolicySettingsAsync(
                Arg.Any<TenantPolicySettingsDto>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _publicExperienceAdminService.ApplyAnnouncementBarSettingsAsync(
                Arg.Any<TenantPolicySettingsDto>(),
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
            if (!cut.Markup.Contains("Exactly one authorization provider is active", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Authorization provider settings were not rendered.");
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

    private static async Task InvokePrivateTaskAsync(object instance, string methodName, object argument)
    {
        var task = (Task)instance.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic, [argument.GetType()])!
            .Invoke(instance, [argument])!;

        await task;
    }
}

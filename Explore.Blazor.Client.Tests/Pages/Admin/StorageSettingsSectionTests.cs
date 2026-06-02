// ABOUTME: bUnit tests for provider-neutral instance and tenant storage settings sections.
// ABOUTME: Verifies HAL-gated actions and locked tenant storage states in admin UI components.

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public sealed class StorageSettingsSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public StorageSettingsSectionTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Storage Admin", "admin@example.com");
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task InstanceStorageSection_WithHalActions_RendersProviderActions()
    {
        // Arrange
        var onboardingService = _ctx.AddMockService<IInstanceOnboardingService>();
        var model = new InstanceStorageSettingsModel
        {
            CanUpdate = true,
            CanTestProvider = true,
            CanRecalculateUsage = true,
            Provider = StorageProviderOptions.Local
        };

        // Act
        var cut = RenderComponent("InstanceStorageSection", new Dictionary<string, object>
        {
            ["Model"] = model,
            ["OnboardingService"] = onboardingService,
            ["IsSingleTenant"] = true
        });

        // Assert
        await Assert.That(cut.Markup).Contains("Test Provider", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Recalculate Usage", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task InstanceStorageSection_WithoutEditHalLink_RendersReadOnlyState()
    {
        // Arrange
        var model = new InstanceStorageSettingsModel
        {
            CanUpdate = false,
            CanTestProvider = false,
            CanRecalculateUsage = false,
            Provider = StorageProviderOptions.Local
        };

        // Act
        var cut = RenderComponent("InstanceStorageSection", new Dictionary<string, object>
        {
            ["Model"] = model,
            ["IsSingleTenant"] = true
        });

        // Assert
        await Assert.That(cut.Markup).Contains("read-only", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Test Provider", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("Recalculate Usage", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task TenantStorageSection_WhenLocked_RendersReadOnlyPolicyMessage()
    {
        // Arrange
        var model = new TenantStorageSettingsModel
        {
            TenantOverridesAllowed = false,
            TenantStorageLocked = true,
            IsReadOnly = true,
            CanUpdate = false,
            Provider = StorageProviderOptions.Local
        };

        // Act
        var cut = RenderComponent("TenantStorageSection", new Dictionary<string, object>
        {
            ["Model"] = model
        });

        // Assert
        await Assert.That(cut.Markup).Contains("read-only", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("inherited from platform policy", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task TenantStorageSection_WhenEditable_RendersOverrideFields()
    {
        // Arrange
        var model = new TenantStorageSettingsModel
        {
            TenantOverridesAllowed = true,
            TenantStorageLocked = false,
            IsReadOnly = false,
            CanUpdate = true,
            Provider = StorageProviderOptions.Local,
            EffectivePolicy = new StorageEffectivePolicyModel
            {
                Provider = StorageProviderOptions.Local,
                InstanceMaxUploadBytes = 100L * 1024 * 1024,
                MaxUploadBytes = 10L * 1024 * 1024,
                TenantQuotaBytes = 1024L * 1024 * 1024
            }
        };

        // Act
        var cut = RenderComponent("TenantStorageSection", new Dictionary<string, object>
        {
            ["Model"] = model
        });

        // Assert
        await Assert.That(cut.Markup).Contains("Tenant Storage Provider", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Max upload", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Tenant quota", StringComparison.OrdinalIgnoreCase);
    }

    private IRenderedComponent<DynamicComponent> RenderComponent(string componentName, IDictionary<string, object> parameters)
    {
        return _ctx.RenderMudComponent<DynamicComponent>(builder =>
            builder.Add(component => component.Type, GetComponentType(componentName))
                   .Add(component => component.Parameters, parameters));
    }

    private static Type GetComponentType(string componentName)
    {
        var componentType = typeof(IInstanceOnboardingService).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.Name == componentName && typeof(IComponent).IsAssignableFrom(type));

        return componentType ?? throw new InvalidOperationException($"Could not find component type '{componentName}'.");
    }
}

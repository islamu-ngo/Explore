// ABOUTME: bUnit tests for provider-neutral instance and tenant storage settings sections.
// ABOUTME: Verifies HAL-gated actions and locked tenant storage states in admin UI components.

using InstanceStorageRouteDto = Explore.Blazor.Client.Clients.Routes;
using TenantStorageRouteDto = Explore.Blazor.Client.Clients.Routes2;

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
        var model = new HalResourceOfInstanceStorageSettingsDto
        {
            Provider = StorageProviderOptions.Local,
            _links = new Dictionary<string, HalLink>
            {
                ["edit"] = new() { Href = "/api/instance/settings/storage", Method = "PUT" },
                ["provider-test"] = new() { Href = "/api/instance/settings/storage/test", Method = "POST" },
                ["recalculate-usage"] = new() { Href = "/api/instance/settings/storage/recalculate-usage", Method = "POST" }
            }
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
    public async Task InstanceStorageSection_WithRoutes_RendersHybridRouteMatrix()
    {
        // Arrange
        var model = new HalResourceOfInstanceStorageSettingsDto
        {
            Provider = StorageProviderOptions.Local,
            Routes =
            [
                new InstanceStorageRouteDto { RouteKey = StorageRouteOptions.Images, Provider = StorageProviderOptions.Local, MaxUploadBytes = 8L * 1024 * 1024 },
                new InstanceStorageRouteDto { RouteKey = StorageRouteOptions.Documents, Provider = StorageProviderOptions.S3Compatible, MaxUploadBytes = 64L * 1024 * 1024 },
                new InstanceStorageRouteDto { RouteKey = StorageRouteOptions.General, Provider = StorageProviderOptions.Local, MaxUploadBytes = 16L * 1024 * 1024 }
            ],
            _links = new Dictionary<string, HalLink>
            {
                ["edit"] = new() { Href = "/api/instance/settings/storage", Method = "PUT" }
            }
        };

        // Act
        var cut = RenderComponent("InstanceStorageSection", new Dictionary<string, object>
        {
            ["Model"] = model,
            ["IsSingleTenant"] = true
        });

        // Assert
        await Assert.That(cut.Markup).Contains("Hybrid storage routes", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Images", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Documents", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("General uploads", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task InstanceStorageSection_WithoutEditHalLink_RendersReadOnlyState()
    {
        // Arrange
        var model = new HalResourceOfInstanceStorageSettingsDto
        {
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
        var model = new HalResourceOfTenantStorageSettingsDto
        {
            TenantOverridesAllowed = false,
            TenantStorageLocked = true,
            IsReadOnly = true,
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
        var model = new HalResourceOfTenantStorageSettingsDto
        {
            TenantOverridesAllowed = true,
            TenantStorageLocked = false,
            IsReadOnly = false,
            Provider = StorageProviderOptions.Local,
            EffectivePolicy = new EffectivePolicy2
            {
                Provider = StorageProviderOptions.Local,
                InstanceMaxUploadBytes = 100L * 1024 * 1024,
                MaxUploadBytes = 10L * 1024 * 1024,
                TenantQuotaBytes = 1024L * 1024 * 1024
            },
            _links = new Dictionary<string, HalLink>
            {
                ["edit"] = new() { Href = "/api/tenant/settings/storage", Method = "PUT" }
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

    [Test]
    public async Task TenantStorageSection_WhenLocked_RendersRouteMatrixAsInheritedPolicy()
    {
        // Arrange
        var model = new HalResourceOfTenantStorageSettingsDto
        {
            TenantOverridesAllowed = false,
            TenantStorageLocked = true,
            IsReadOnly = true,
            Provider = StorageProviderOptions.Local,
            Routes =
            [
                new TenantStorageRouteDto { RouteKey = StorageRouteOptions.Images, Provider = StorageProviderOptions.Local, MaxUploadBytes = 8L * 1024 * 1024, IsReadOnly = true },
                new TenantStorageRouteDto { RouteKey = StorageRouteOptions.Documents, Provider = StorageProviderOptions.S3Compatible, MaxUploadBytes = 64L * 1024 * 1024, IsReadOnly = true },
                new TenantStorageRouteDto { RouteKey = StorageRouteOptions.General, Provider = StorageProviderOptions.Local, MaxUploadBytes = 16L * 1024 * 1024, IsReadOnly = true }
            ],
            EffectivePolicy = new EffectivePolicy2
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
        await Assert.That(cut.Markup).Contains("Hybrid storage routes", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Tenant overrides stay bounded", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Documents", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task StorageSettingsDto_ToUpdateRequest_PreservesTypedRouteMatrix()
    {
        // Arrange
        var model = new HalResourceOfInstanceStorageSettingsDto
        {
            Routes =
            [
                new InstanceStorageRouteDto { RouteKey = StorageRouteOptions.Images, Provider = StorageProviderOptions.Local, MaxUploadBytes = 8L * 1024 * 1024 },
                new InstanceStorageRouteDto { RouteKey = StorageRouteOptions.Documents, Provider = StorageProviderOptions.S3Compatible, MaxUploadBytes = 64L * 1024 * 1024 },
                new InstanceStorageRouteDto { RouteKey = StorageRouteOptions.General, Provider = StorageProviderOptions.Local, MaxUploadBytes = 16L * 1024 * 1024 }
            ]
        };

        // Act
        var dto = model.ToUpdateRequest();

        // Assert
        await Assert.That(dto.Routes).IsNotNull();
        await Assert.That(dto.Routes!.Count).IsEqualTo(3);
        await Assert.That(dto.Routes!.Any(route => route.RouteKey == StorageRouteOptions.Documents && route.Provider == StorageProviderOptions.S3Compatible)).IsTrue();
        await Assert.That(dto.Routes!.Single(route => route.RouteKey == StorageRouteOptions.Documents).MaxUploadBytes)
            .IsEqualTo(64L * 1024 * 1024);
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

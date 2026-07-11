// ABOUTME: bUnit tests verifying lock toggle visibility in instance section components.
// ABOUTME: Ensures lock toggles are hidden in single-tenant mode and visible in multi-tenant mode.

using Explore.Blazor.Client.Models.Analytics;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class InstanceSectionLockToggleTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public InstanceSectionLockToggleTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");
        _ctx.AddMockService<IInstanceOnboardingService>();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task StorageSection_SingleTenant_NoLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceStorageSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new HalResourceOfInstanceStorageSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).DoesNotContain("Lock storage settings", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task SmtpSection_SingleTenant_NoLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceSmtpSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new InstanceSmtpSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).DoesNotContain("Lock SMTP settings", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task AnalyticsSection_SingleTenant_NoLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceAnalyticsPrivacySection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new AnalyticsGovernanceSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).DoesNotContain("Lock analytics settings", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task StorageSection_MultiTenant_HasLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceStorageSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new HalResourceOfInstanceStorageSettingsDto(),
                          ["IsSingleTenant"] = false,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).Contains("Lock storage settings", StringComparison.OrdinalIgnoreCase);
    }

    private static Type GetComponentType(string componentName)
    {
        var componentType = typeof(IInstanceOnboardingService).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.Name == componentName && typeof(IComponent).IsAssignableFrom(type));

        return componentType ?? throw new InvalidOperationException($"Could not find component type '{componentName}'.");
    }
}

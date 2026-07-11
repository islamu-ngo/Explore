// ABOUTME: bUnit tests for the tenant branding typed settings section.
// ABOUTME: Verifies HAL-driven editability and missing-document messaging without role checks.

using Explore.Blazor.Client.Pages.Admin.Tenant.Components;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public sealed class TenantBrandingSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task TenantBrandingSection_WhenReplaceLinkMissing_RendersReadOnlyMessage()
    {
        TenantBrandingSettingsAdminModel model = new()
        {
            Exists = true,
            CanReplace = false,
            DisplayName = "Read Only Tenant",
            CustomCssUrl = "https://cdn.example.test/tenant.css"
        };

        IRenderedComponent<TenantBrandingSection> cut = _ctx.Render<TenantBrandingSection>(parameters => parameters
            .Add(component => component.Model, model));

        await Assert.That(cut.Markup).Contains("API did not emit the replace-settings action");
        await Assert.That(cut.Markup).Contains("Read Only Tenant");
        await Assert.That(cut.Markup).Contains("https://cdn.example.test/tenant.css");
    }

    [Test]
    public async Task TenantBrandingSection_WhenDocumentMissing_RendersAlert()
    {
        TenantBrandingSettingsAdminModel model = TenantBrandingSettingsAdminModel.Missing();

        IRenderedComponent<TenantBrandingSection> cut = _ctx.Render<TenantBrandingSection>(parameters => parameters
            .Add(component => component.Model, model));

        await Assert.That(cut.Markup).Contains("Tenant branding settings have not been initialized.");
    }
}

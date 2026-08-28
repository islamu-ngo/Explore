// ABOUTME: bUnit coverage for anonymous public footer legal notices.
// ABOUTME: Verifies the policy-gated paid-event disclaimer uses server-authored tenant branding.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Layout;
using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Tests.Layout;

public sealed class FooterTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    [Test]
    public async Task PaidEventDirectoryDisclaimer_RendersFromPublicSettings()
    {
        var publicExperience = Substitute.For<IPublicExperienceService>();
        publicExperience.GetCachedSettingsAsync().Returns(new PublicExperienceSettingsDto
        {
            BrandDisplayName = "Tenant Events",
            PaidEventDirectoryDisclaimer = "Tenant Events provides an event discovery and management directory only."
        });
        _ctx.Services.AddSingleton(publicExperience);
        _ctx.Services.AddSingleton(new CookieConsentStateService());

        var cut = _ctx.RenderMudComponent<Footer>();

        var notice = cut.WaitForElement("[data-testid='footer-paid-event-directory-disclaimer']");

        await Assert.That(notice.TextContent)
            .Contains("Tenant Events provides an event discovery and management directory only.");
        await Assert.That(notice.GetAttribute("dir")).IsNull();
        await Assert.That(notice.QuerySelectorAll("[lang='en'][dir='ltr']").Length).IsEqualTo(1);
    }

    public void Dispose() => _ctx.Dispose();
}

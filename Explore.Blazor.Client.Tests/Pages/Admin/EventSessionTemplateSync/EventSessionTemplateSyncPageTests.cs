// ABOUTME: Placeholder component tests for EventSessionTemplateSyncPage. Full coverage deferred.
// ABOUTME: TODO(templatesync-tests): replace with HAL-gated render + 409 handling + slug confirmation tests once the API emits a HAL-wrapped diff resource.

using Explore.Blazor.Client.Pages.Admin.EventSessionTemplateSync;

namespace Explore.Blazor.Client.Tests.Pages.Admin.EventSessionTemplateSync;

public sealed class EventSessionTemplateSyncPageTests
{
    [Test]
    public async Task EventSessionTemplateSyncPage_TypeExists()
    {
        var pageType = typeof(EventSessionTemplateSyncPage);
        await Assert.That(pageType).IsNotNull();
        await Assert.That(pageType.Namespace).IsEqualTo("Explore.Blazor.Client.Pages.Admin.EventSessionTemplateSync");
    }
}

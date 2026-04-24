// ABOUTME: Placeholder component tests for EventTemplateSyncPage. Full coverage deferred.
// ABOUTME: TODO(templatesync-tests): replace with HAL-gated render + 409 handling + slug confirmation tests once the API emits a HAL-wrapped diff resource.

using Explore.Blazor.Client.Pages.Admin.EventTemplateSync;

namespace Explore.Blazor.Client.Tests.Pages.Admin.EventTemplateSync;

public sealed class EventTemplateSyncPageTests
{
    [Test]
    public async Task EventTemplateSyncPage_TypeExists()
    {
        // Smoke: page type is discoverable by the client assembly.
        // Full render/flow coverage deferred until API returns HAL resource shape
        // (currently BaseCommandResponse<TemplateDiffDto> with no `Data` or `Links` surface).
        var pageType = typeof(EventTemplateSyncPage);
        await Assert.That(pageType).IsNotNull();
        await Assert.That(pageType.Namespace).IsEqualTo("Explore.Blazor.Client.Pages.Admin.EventTemplateSync");
    }
}

// ABOUTME: Source-level launch accessibility guards for event, registration, error, and admin-critical UI.
// ABOUTME: Prevents fake data and placeholder launch copy from returning to public or admin surfaces.

namespace Explore.Blazor.Client.Tests.Accessibility;

public sealed class LaunchAccessibilitySourceTests
{
    [Test]
    public async Task LaunchCriticalPages_ShouldPreserveAccessibilityContracts()
    {
        var eventDetailMarkup = await ReadClientSourceAsync("Pages/Events/EventDetail.razor");
        var eventDetailCode = await ReadClientSourceAsync("Pages/Events/EventDetail.razor.cs");
        var createEventMarkup = await ReadClientSourceAsync("Pages/Events/CreateEvent.razor");
        var errorShellMarkup = await ReadClientSourceAsync("Pages/Errors/ErrorPageShell.razor");

        await Assert.That(eventDetailMarkup).Contains("<PageTitle>");
        await Assert.That(eventDetailMarkup).Contains("HtmlTag=\"h1\"");
        await Assert.That(eventDetailMarkup).Contains("role=\"region\" aria-label=\"Event management actions\"");
        await Assert.That(eventDetailMarkup).Contains("event-registration-card");
        await Assert.That(eventDetailCode).Contains("OpenRegistrationDialog");
        await Assert.That(eventDetailCode).Contains("AccessibilityFocusService.SaveFocusAsync()");
        await Assert.That(eventDetailCode).Contains("AccessibilityFocusService.RestoreFocusAsync()");
        await Assert.That(eventDetailCode).Contains("AnnouncerService.Announce");

        await Assert.That(createEventMarkup).Contains("<PageTitle>Create Event</PageTitle>");
        await Assert.That(createEventMarkup).Contains("HtmlTag=\"h1\"");
        await Assert.That(createEventMarkup).Contains("role=\"alert\"");
        await Assert.That(createEventMarkup).Contains("aria-label=\"Upload event image\"");
        await Assert.That(createEventMarkup).Contains("aria-label=\"Registration policy\"");

        await Assert.That(errorShellMarkup).Contains("role=\"alert\"");
        await Assert.That(errorShellMarkup).Contains("role=\"search\"");
        await Assert.That(errorShellMarkup).Contains("aria-label=\"Search events\"");
    }

    [Test]
    public async Task LaunchCriticalSource_ShouldNotExposeFakeDataOrFutureCopy()
    {
        var tenantMembers = await ReadClientSourceAsync("Pages/Admin/Instance/Components/TenantMembersSection.razor");
        var privacy = await ReadClientSourceAsync("Pages/User/Components/SettingsPrivacy.razor");
        var notifications = await ReadClientSourceAsync("Pages/User/Components/SettingsNotifications.razor");

        await Assert.That(tenantMembers).DoesNotContain("_mockMembers");
        await Assert.That(tenantMembers).DoesNotContain("backend integration pending");
        await Assert.That(tenantMembers).DoesNotContain("will be replaced by API call");
        await Assert.That(tenantMembers).DoesNotContain("ibrahim@example.com", StringComparison.OrdinalIgnoreCase);
        await Assert.That(tenantMembers).DoesNotContain("fatima@example.com", StringComparison.OrdinalIgnoreCase);

        await Assert.That(privacy).DoesNotContain("coming soon", StringComparison.OrdinalIgnoreCase);
        await Assert.That(notifications).DoesNotContain("coming soon", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadClientSourceAsync(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Explore.Blazor.Client", relativePath);
            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate Explore.Blazor.Client/{relativePath} from test base directory.");
    }
}

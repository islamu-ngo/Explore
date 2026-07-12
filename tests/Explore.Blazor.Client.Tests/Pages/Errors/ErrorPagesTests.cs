// ABOUTME: Verifies branded public error pages render accessible recovery copy and actions.
// ABOUTME: Guards noindex metadata, status labels, and primary CTA regressions for WP-15.

using Bunit;
using Explore.Blazor.Client.Pages.Errors;
using Explore.Blazor.Client.Tests.Common;
using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Tests.Pages.Errors;

public class ErrorPagesTests : BlazorTestContext
{
    [Test]
    public async Task NotFound_RendersSearchAndRecoveryActions()
    {
        var component = Render<NotFound>();

        await Assert.That(component.Markup).Contains("Page Not Found");
        await Assert.That(component.Markup).Contains("Search events");
        await AssertPageSourceContainsNoIndex("NotFound.razor");
        await Assert.That(component.FindAll("h1").Count).IsEqualTo(1);
        await Assert.That(component.FindAll("a[href='/']").Count).IsEqualTo(1);
        await Assert.That(component.FindAll("a[href='/events']").Count).IsEqualTo(1);
    }

    [Test]
    public async Task Unauthorized_RendersRequestAccessAction()
    {
        var component = Render<Unauthorized>();

        await Assert.That(component.Markup).Contains("Access Denied");
        await AssertPageSourceContainsNoIndex("Unauthorized.razor");
        await Assert.That(component.FindAll("h1").Count).IsEqualTo(1);
        await Assert.That(component.FindAll("a[href='/contact']").Count).IsEqualTo(1);
    }

    [Test]
    public async Task ServerError_RendersReferenceAndSupportAction()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("traceId", "trace-123"));

        var component = Render<ServerError>();

        await Assert.That(component.Markup).Contains("Something Went Wrong");
        await Assert.That(component.Markup).Contains("Reference ID: trace-123");
        await AssertPageSourceContainsNoIndex("ServerError.razor");
        await Assert.That(component.FindAll("h1").Count).IsEqualTo(1);
        await Assert.That(component.FindAll("a[href^='/contact?subject=']").Count).IsEqualTo(1);
    }

    private static async Task AssertPageSourceContainsNoIndex(string fileName)
    {
        var filePath = FindErrorPagePath(fileName);
        var source = await File.ReadAllTextAsync(filePath);

        await Assert.That(source).Contains("noindex, nofollow");
    }

    private static string FindErrorPagePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Explore.Blazor.Client", "Pages", "Errors", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate src/Explore.Blazor.Client/Pages/Errors/{fileName} from test base directory.");
    }
}

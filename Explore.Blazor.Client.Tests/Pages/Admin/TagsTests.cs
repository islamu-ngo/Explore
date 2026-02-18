// ABOUTME: Component tests for Tags admin page loading/error/empty/success states.
// ABOUTME: Verifies resilient behavior for tag list rendering across service outcomes.

using Explore.Blazor.Client.Pages.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class TagsTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly ITagService _tagService;
    private readonly ISnackbar _snackbar;

    public TagsTests()
    {
        _ctx = new BlazorTestContext();
        _tagService = Substitute.For<ITagService>();
        _snackbar = Substitute.For<ISnackbar>();

        _ctx.Services.AddSingleton(_tagService);
        _ctx.Services.AddSingleton(_snackbar);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderTags()
    {
        var componentType = typeof(AdminList).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Tags")
                            ?? throw new InvalidOperationException("Tags component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    [Test]
    public async Task Tags_ShowsLoadingState_WhileFetchIsPending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ICollection<TagListDto>>();
        _tagService.GetTagsAsync().Returns(pending.Task);

        // Act
        var cut = RenderTags();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading tags");

        // Cleanup
        pending.TrySetResult(new List<TagListDto>());
    }

    [Test]
    public async Task Tags_ShowsEmptyState_WhenNoTagsReturned()
    {
        // Arrange
        _tagService.GetTagsAsync().Returns(new List<TagListDto>());

        // Act
        var cut = RenderTags();
        cut.WaitForState(() => cut.Markup.Contains("No tags found", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("No tags found");
        await Assert.That(cut.Markup).Contains("Create your first tag to get started.");
    }

    [Test]
    public async Task Tags_ShowsTagRows_WhenDataExists()
    {
        // Arrange
        _tagService.GetTagsAsync().Returns(
        [
            new TagListDto
            {
                Id = Guid.NewGuid(),
                FullName = "Community",
                MasterCode = "COMM"
            }
        ]);

        // Act
        var cut = RenderTags();
        cut.WaitForState(() => cut.Markup.Contains("Community", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Community");
        await Assert.That(cut.Markup).Contains("COMM");
    }

    [Test]
    public async Task Tags_UsesSnackbarError_WhenLoadFails()
    {
        // Arrange
        _tagService.GetTagsAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderTags();
        cut.WaitForState(() => cut.Markup.Contains("No tags found", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        _snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Failed to load tags", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }
}

// ABOUTME: Component tests for Categories admin page loading/error/empty/success states.
// ABOUTME: Verifies CRUD list surface renders resiliently for service failures and empty data.

using Explore.Blazor.Client.Pages.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class CategoriesTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly ICategoryService _categoryService;

    public CategoriesTests()
    {
        _ctx = new BlazorTestContext();
        _categoryService = Substitute.For<ICategoryService>();

        _ctx.Services.AddSingleton(_categoryService);
        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderCategories()
    {
        var componentType = typeof(AdminList).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Categories")
                            ?? throw new InvalidOperationException("Categories component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    [Test]
    public async Task Categories_ShowsLoadingState_WhileFetchIsPending()
    {
        // Arrange
        var pending = new TaskCompletionSource<ICollection<CategoryListDto>>();
        _categoryService.GetCategoriesAsync().Returns(pending.Task);

        // Act
        var cut = RenderCategories();

        // Assert
        await Assert.That(cut.Markup).Contains("Loading categories...");

        // Cleanup
        pending.TrySetResult(new List<CategoryListDto>());
    }

    [Test]
    public async Task Categories_ShowsErrorAlert_WhenFetchThrows()
    {
        // Arrange
        _categoryService.GetCategoriesAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderCategories();
        cut.WaitForState(() => cut.Markup.Contains("Failed to load categories", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Failed to load categories: boom");
    }

    [Test]
    public async Task Categories_ShowsEmptyState_WhenNoRecordsReturned()
    {
        // Arrange
        _categoryService.GetCategoriesAsync().Returns(new List<CategoryListDto>());

        // Act
        var cut = RenderCategories();
        cut.WaitForState(() => cut.Markup.Contains("No categories found", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("No categories found");
        await Assert.That(cut.Markup).Contains("Create your first category to get started.");
    }

    [Test]
    public async Task Categories_ShowsCategoryRows_WhenDataExists()
    {
        // Arrange
        _categoryService.GetCategoriesAsync().Returns(
        [
            new CategoryListDto
            {
                Id = Guid.NewGuid(),
                FullName = "Faith & Spirituality",
                MasterCode = "FTH",
                ParentFullName = "Islamic"
            }
        ]);

        // Act
        var cut = RenderCategories();
        cut.WaitForState(() => cut.Markup.Contains("Spirituality", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Spirituality");
        await Assert.That(cut.Markup).Contains("FTH");
        await Assert.That(cut.Markup).Contains("Islamic");
    }
}

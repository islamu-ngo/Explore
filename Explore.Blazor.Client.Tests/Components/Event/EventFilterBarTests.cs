using System.Reflection;
using EventFilterBarComponent = Explore.Blazor.Client.Components.Event.EventFilterBar;

namespace Explore.Blazor.Client.Tests.Components.Event;

public class EventFilterBarTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public EventFilterBarTests()
    {
        _ctx = new BlazorTestContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task EventFilterBar_RendersPrimaryRow_Always()
    {
        // Act
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(parameters => parameters
            .Add(x => x.IsIslamicModuleEnabled, false)
            .Add(x => x.IsTechModuleEnabled, false));

        // Assert — primary row elements are always visible
        await Assert.That(cut.Markup).Contains("Search events...");
        await Assert.That(cut.Markup).Contains("Filters");
        await Assert.That(cut.Markup).Contains("Search");
    }

    [Test]
    public async Task EventFilterBar_FiltersCollapsed_ByDefault()
    {
        // Act
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(parameters => parameters
            .Add(x => x.IsIslamicModuleEnabled, false)
            .Add(x => x.IsTechModuleEnabled, false));

        // Assert — filter panel content should not be visible when collapsed
        // The MudCollapse renders but is not expanded
        await Assert.That(cut.Markup).Contains("mud-collapse-container");
    }

    [Test]
    public async Task EventFilterBar_DoesNotRenderRemovedIslamicFilters()
    {
        // Act
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(parameters => parameters
            .Add(x => x.IsIslamicModuleEnabled, true)
            .Add(x => x.IsTechModuleEnabled, false));

        // Assert — removed Islamic filters should not appear
        await Assert.That(cut.Markup).DoesNotContain("Islamic Language");
        await Assert.That(cut.Markup).DoesNotContain("Quran Recitation");
        await Assert.That(cut.Markup).DoesNotContain("Islamic Events Only");
        await Assert.That(cut.Markup).DoesNotContain("Islamic Aspects");
    }

    [Test]
    public async Task EventFilterBar_ClearAllFilters_ResetsStateAndInvokesCallback()
    {
        // Arrange
        var callbackCount = 0;
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(parameters => parameters
            .Add(x => x.OnSearchRequested, EventCallback.Factory.Create(this, () => callbackCount++)));

        cut.Instance.SelectedDateRange = new MudBlazor.DateRange(DateTime.Today, DateTime.Today.AddDays(7));
        cut.Instance.SelectedEventTypeIds = new HashSet<int> { 2 };

        var clearAllMethod = typeof(EventFilterBarComponent).GetMethod("ClearAllFilters", BindingFlags.Instance | BindingFlags.NonPublic);
        await Assert.That(clearAllMethod is not null).IsTrue();

        // Act
        var task = (Task?)clearAllMethod!.Invoke(cut.Instance, null);
        await Assert.That(task is not null).IsTrue();
        await task!;

        // Assert
        await Assert.That(cut.Instance.SelectedDateRange).IsNull();
        await Assert.That(cut.Instance.SelectedEventTypeIds.Any()).IsFalse();
        await Assert.That(callbackCount).IsEqualTo(1);
    }

    [Test]
    public async Task EventFilterBar_GetActiveFilterCount_CountsCorrectly()
    {
        // Arrange
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>();

        // Act — set some filters
        cut.Instance.SelectedDateRange = new MudBlazor.DateRange(DateTime.Today, DateTime.Today.AddDays(1));
        cut.Instance.SelectedFormatIds = new HashSet<int> { 1 };
        cut.Instance.SelectedLanguageIds = new HashSet<int> { 2 };

        // Assert
        var count = cut.Instance.GetActiveFilterCount();
        await Assert.That(count).IsEqualTo(3);
    }
}

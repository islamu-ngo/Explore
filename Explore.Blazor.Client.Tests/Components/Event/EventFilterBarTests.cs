// ABOUTME: Behavioral bUnit tests for EventFilterBar component.
// ABOUTME: Verifies filter rendering, clear-all via UI click, and active filter counting.

using EventFilterBarComponent = Explore.Blazor.Client.Pages.Events.Components.EventFilterBar;

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
    public async Task RendersPrimaryRow_Always()
    {
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(p => p
            .Add(x => x.IsIslamicModuleEnabled, false)
            .Add(x => x.IsTechModuleEnabled, false));

        await Assert.That(cut.Markup).Contains("Search events...");
        await Assert.That(cut.Markup).Contains("Filters");
        await Assert.That(cut.Markup).Contains("Search");
    }

    [Test]
    public async Task FiltersCollapsed_ByDefault()
    {
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(p => p
            .Add(x => x.IsIslamicModuleEnabled, false)
            .Add(x => x.IsTechModuleEnabled, false));

        await Assert.That(cut.Markup).Contains("mud-collapse-container");
    }

    [Test]
    public async Task DoesNotRenderRemovedIslamicFilters()
    {
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(p => p
            .Add(x => x.IsIslamicModuleEnabled, true)
            .Add(x => x.IsTechModuleEnabled, false));

        await Assert.That(cut.Markup).DoesNotContain("Islamic Language");
        await Assert.That(cut.Markup).DoesNotContain("Quran Recitation");
        await Assert.That(cut.Markup).DoesNotContain("Islamic Events Only");
        await Assert.That(cut.Markup).DoesNotContain("Islamic Aspects");
    }

    [Test]
    public async Task ClearAllFilters_ResetsStateAndInvokesCallback()
    {
        // Arrange
        var callbackCount = 0;
        var callback = EventCallback.Factory.Create(this, () => callbackCount++);

        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(p => p
            .Add(x => x.OnSearchRequested, callback));

        // Set filter state to make activeCount > 0
        cut.Instance.SelectedDateRange = new MudBlazor.DateRange(DateTime.Today, DateTime.Today.AddDays(7));
        cut.Instance.SelectedEventTypeIds = new HashSet<int> { 2 };

        // Re-render so the "Clear All" button appears (conditional on activeCount > 0)
        cut.Render(p => p.Add(x => x.OnSearchRequested, callback));

        // Act — click the Clear All button inside the active-summary section
        var clearButton = cut.Find(".filter-bar__active-summary button");
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert — all filter state reset and callback invoked
        await Assert.That(cut.Instance.SelectedDateRange).IsNull();
        await Assert.That(cut.Instance.SelectedEventTypeIds.Any()).IsFalse();
        await Assert.That(callbackCount).IsEqualTo(1);
    }

    [Test]
    public async Task GetActiveFilterCount_CountsCorrectly()
    {
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>();

        cut.Instance.SelectedDateRange = new MudBlazor.DateRange(DateTime.Today, DateTime.Today.AddDays(1));
        cut.Instance.SelectedFormatIds = new HashSet<int> { 1 };
        cut.Instance.SelectedLanguageIds = new HashSet<int> { 2 };

        var count = cut.Instance.GetActiveFilterCount();
        await Assert.That(count).IsEqualTo(3);
    }
}

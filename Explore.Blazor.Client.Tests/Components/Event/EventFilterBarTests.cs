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
    public async Task EventFilterBar_RendersCoreFilters_WhenModulesDisabled()
    {
        // Act
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(parameters => parameters
            .Add(x => x.IsIslamicModuleEnabled, false)
            .Add(x => x.IsTechModuleEnabled, false));

        // Assert
        await Assert.That(cut.Markup).Contains("Date");
        await Assert.That(cut.Markup).Contains("Category");
        await Assert.That(cut.Markup).Contains("Location");
        await Assert.That(cut.Markup).Contains("Format");
        await Assert.That(cut.Markup).Contains("Status");
        await Assert.That(cut.Markup).DoesNotContain("Islamic Aspects");
        await Assert.That(cut.Markup).DoesNotContain("Tech Aspects");
    }

    [Test]
    public async Task EventFilterBar_RendersModuleSections_WhenEnabled()
    {
        // Act
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(parameters => parameters
            .Add(x => x.IsIslamicModuleEnabled, true)
            .Add(x => x.IsTechModuleEnabled, true));

        // Assert
        await Assert.That(cut.Markup).Contains("Islamic Aspects");
        await Assert.That(cut.Markup).Contains("Tech Aspects");
    }

    [Test]
    public async Task EventFilterBar_ShowsClearAllChip_WhenAtLeastOneFilterIsActive()
    {
        // Arrange
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>();

        // Act
        cut.Instance.SelectedDate = "today";
        cut.Render();

        // Assert
        await Assert.That(cut.Markup).Contains("Clear All");
    }

    [Test]
    public async Task EventFilterBar_ClearAllFilters_ResetsStateAndInvokesCallback()
    {
        // Arrange
        var callbackCount = 0;
        var cut = _ctx.RenderMudComponent<EventFilterBarComponent>(parameters => parameters
            .Add(x => x.OnFilterChanged, EventCallback.Factory.Create(this, () => callbackCount++)));

        cut.Instance.SelectedDate = "thisweek";
        cut.Instance.SelectedCategoryId = Guid.NewGuid();
        cut.Instance.SelectedEventTypeId = 2;
        cut.Instance.HasTechAspect = true;

        var clearAllMethod = typeof(EventFilterBarComponent).GetMethod("ClearAllFilters", BindingFlags.Instance | BindingFlags.NonPublic);
        await Assert.That(clearAllMethod is not null).IsTrue();

        // Act
        var task = (Task?)clearAllMethod!.Invoke(cut.Instance, null);
        await Assert.That(task is not null).IsTrue();
        await task!;

        // Assert
        await Assert.That(cut.Instance.SelectedDate).IsEqualTo(string.Empty);
        await Assert.That(cut.Instance.SelectedCategoryId).IsNull();
        await Assert.That(cut.Instance.SelectedEventTypeId).IsNull();
        await Assert.That(cut.Instance.HasTechAspect).IsNull();
        await Assert.That(callbackCount).IsEqualTo(1);
    }
}

// ABOUTME: Behavioral bUnit tests for TriStateTagFilterDropdown component.
// ABOUTME: Verifies tag filter cycling, reset, and filter state via public API and rendered markup.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Pages.Events.Components;
using MudBlazor;
using TriStateTagFilterDropdownComponent = Explore.Blazor.Client.Pages.Events.Components.TriStateTagFilterDropdown;

namespace Explore.Blazor.Client.Tests.Components.Event;

public class TriStateTagFilterDropdownTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public TriStateTagFilterDropdownTests()
    {
        _ctx = new BlazorTestContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private static List<TagTypeWithTagsDto> GetMockTagGroups()
    {
        return new List<TagTypeWithTagsDto>
        {
            new TagTypeWithTagsDto
            {
                Id = 1,
                FullName = "Genre",
                Tags = new List<TagListDto>
                {
                    new TagListDto { Id = Guid.NewGuid(), FullName = "Fiction" },
                    new TagListDto { Id = Guid.NewGuid(), FullName = "Non-Fiction" }
                }
            },
            new TagTypeWithTagsDto
            {
                Id = 2,
                FullName = "Format",
                Tags = new List<TagListDto>
                {
                    new TagListDto { Id = Guid.NewGuid(), FullName = "Workshop" }
                }
            }
        };
    }

    /// <summary>
    /// Simulates a tag state toggle via the component's internal method.
    /// Required because MudPopover content is not rendered in test context
    /// (MockPopoverService returns empty ActivePopovers), making MudChip
    /// elements inside the popover inaccessible for direct UI interaction.
    /// All test assertions use the public GetCurrentFilter() API.
    /// </summary>
    private static async Task SimulateTagToggle(IRenderedComponent<TriStateTagFilterDropdownComponent> cut, Guid tagId)
    {
        var method = typeof(TriStateTagFilterDropdownComponent)
            .GetMethod("ToggleTagState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ToggleTagState not found — component API may have changed.");

        await cut.InvokeAsync(() => method.Invoke(cut.Instance, [tagId]));
    }

    private IRenderedComponent<TriStateTagFilterDropdownComponent> RenderDropdown(List<TagTypeWithTagsDto>? groups = null)
    {
        _ctx.RenderComponent<MudPopoverProvider>();
        return _ctx.RenderComponent<TriStateTagFilterDropdownComponent>(p => p
            .Add(x => x.TagGroups, groups ?? GetMockTagGroups()));
    }

    [Test]
    public async Task RendersTriggerButton_WithDefaultBadgeText()
    {
        var cut = RenderDropdown();

        await Assert.That(cut.Markup).Contains("Filter Tags");
    }

    [Test]
    public async Task ClickTriggerButton_TogglesPopoverVisibility()
    {
        var cut = RenderDropdown();
        var markupBefore = cut.Markup;

        // Click trigger to open — should change EndIcon and overlay visibility
        var triggerButton = cut.Find(".tri-state-tag-filter__trigger");
        await cut.InvokeAsync(() => triggerButton.Click());

        var markupAfterOpen = cut.Markup;
        await Assert.That(markupAfterOpen).IsNotEqualTo(markupBefore);

        // Click trigger to close — markup should revert from the open state
        triggerButton = cut.Find(".tri-state-tag-filter__trigger");
        await cut.InvokeAsync(() => triggerButton.Click());

        var markupAfterClose = cut.Markup;
        await Assert.That(markupAfterClose).IsNotEqualTo(markupAfterOpen);
    }

    [Test]
    public async Task TagCycling_CyclesThroughNeutralIncludeExclude()
    {
        var groups = GetMockTagGroups();
        var tagId = groups[0].Tags!.First().Id!.Value;
        var cut = RenderDropdown(groups);

        // Cycle 1: Neutral → Include
        await SimulateTagToggle(cut, tagId);
        var filter = cut.Instance.GetCurrentFilter();
        await Assert.That(filter.IncludedTagIds).Contains(tagId);
        await Assert.That(filter.ExcludedTagIds).DoesNotContain(tagId);

        // Cycle 2: Include → Exclude
        await SimulateTagToggle(cut, tagId);
        filter = cut.Instance.GetCurrentFilter();
        await Assert.That(filter.IncludedTagIds).DoesNotContain(tagId);
        await Assert.That(filter.ExcludedTagIds).Contains(tagId);

        // Cycle 3: Exclude → Neutral
        await SimulateTagToggle(cut, tagId);
        filter = cut.Instance.GetCurrentFilter();
        await Assert.That(filter.IncludedTagIds).DoesNotContain(tagId);
        await Assert.That(filter.ExcludedTagIds).DoesNotContain(tagId);
    }

    [Test]
    public async Task ResetAll_ClearsAllTagStates()
    {
        var groups = GetMockTagGroups();
        var tagId = groups[0].Tags!.First().Id!.Value;
        var cut = RenderDropdown(groups);

        // Setup: include a tag
        await SimulateTagToggle(cut, tagId);
        await Assert.That(cut.Instance.GetCurrentFilter().IncludedTagIds).Contains(tagId);

        // Act: reset via public API
        await cut.InvokeAsync(() => cut.Instance.ResetAll());

        // Assert: filter state is empty
        var filter = cut.Instance.GetCurrentFilter();
        await Assert.That(filter.IncludedTagIds).IsEmpty();
        await Assert.That(filter.ExcludedTagIds).IsEmpty();
    }

    [Test]
    public async Task GetCurrentFilter_ReturnsIncludedExcludedAndDefaultModes()
    {
        var groups = GetMockTagGroups();
        var tagId1 = groups[0].Tags!.ElementAt(0).Id!.Value;
        var tagId2 = groups[0].Tags!.ElementAt(1).Id!.Value;
        var cut = RenderDropdown(groups);

        // tagId1 → Include (1 toggle)
        await SimulateTagToggle(cut, tagId1);
        // tagId2 → Exclude (2 toggles: Neutral→Include→Exclude)
        await SimulateTagToggle(cut, tagId2);
        await SimulateTagToggle(cut, tagId2);

        var filter = cut.Instance.GetCurrentFilter();

        await Assert.That(filter.IncludedTagIds).Contains(tagId1);
        await Assert.That(filter.ExcludedTagIds).Contains(tagId2);
        await Assert.That(filter.InclusionMode).IsEqualTo("and");
        await Assert.That(filter.ExclusionMode).IsEqualTo("or");
    }
}

using System.Reflection;
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

    private List<TagTypeWithTagsDto> GetMockTagGroups()
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

    [Test]
    public async Task TriStateTagFilterDropdown_RendersTriggerButton_ByDefault()
    {
        // Act
        _ctx.RenderComponent<MudPopoverProvider>();
        var cut = _ctx.RenderComponent<TriStateTagFilterDropdownComponent>(parameters => parameters
            .Add(x => x.TagGroups, GetMockTagGroups()));

        // Assert
        await Assert.That(cut.Markup).Contains("Filter Tags");
    }

    [Test]
    public async Task TriStateTagFilterDropdown_TogglePopover_OpensAndCloses()
    {
        // Act
        _ctx.RenderComponent<MudPopoverProvider>();
        var cut = _ctx.RenderComponent<TriStateTagFilterDropdownComponent>(parameters => parameters
            .Add(x => x.TagGroups, GetMockTagGroups()));

        var toggleMethod = typeof(TriStateTagFilterDropdownComponent).GetMethod("TogglePopover", BindingFlags.Instance | BindingFlags.NonPublic);
        await Assert.That(toggleMethod is not null).IsTrue();

        // Open
        toggleMethod!.Invoke(cut.Instance, null);
        await Assert.That(cut.Instance.GetType().GetField("_isOpen", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cut.Instance)).IsEqualTo(true);

        // Close
        toggleMethod!.Invoke(cut.Instance, null);
        await Assert.That(cut.Instance.GetType().GetField("_isOpen", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cut.Instance)).IsEqualTo(false);
    }

    [Test]
    public async Task TriStateTagFilterDropdown_TagCycling_UpdatesStateAndBadge()
    {
        // Arrange
        var groups = GetMockTagGroups();
        var tagId = groups[0].Tags!.First().Id!.Value;
        _ctx.RenderComponent<MudPopoverProvider>();
        var cut = _ctx.RenderComponent<TriStateTagFilterDropdownComponent>(parameters => parameters
            .Add(x => x.TagGroups, groups));

        var toggleTagMethod = typeof(TriStateTagFilterDropdownComponent).GetMethod("ToggleTagState", BindingFlags.Instance | BindingFlags.NonPublic);
        var getBadgeTextMethod = typeof(TriStateTagFilterDropdownComponent).GetMethod("GetBadgeText", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act & Assert 1: Include
        toggleTagMethod!.Invoke(cut.Instance, new object[] { tagId });
        await Assert.That(getBadgeTextMethod!.Invoke(cut.Instance, null)).IsEqualTo("Filter Tags +1");

        // Act & Assert 2: Exclude
        toggleTagMethod!.Invoke(cut.Instance, new object[] { tagId });
        await Assert.That(getBadgeTextMethod!.Invoke(cut.Instance, null)).IsEqualTo("Filter Tags -1");

        // Act & Assert 3: Neutral
        toggleTagMethod!.Invoke(cut.Instance, new object[] { tagId });
        await Assert.That(getBadgeTextMethod!.Invoke(cut.Instance, null)).IsEqualTo("Filter Tags");
    }

    [Test]
    public async Task TriStateTagFilterDropdown_ResetAll_ClearsStates()
    {
        // Arrange
        var groups = GetMockTagGroups();
        var tagId = groups[0].Tags!.First().Id!.Value;
        _ctx.RenderComponent<MudPopoverProvider>();
        var cut = _ctx.RenderComponent<TriStateTagFilterDropdownComponent>(parameters => parameters
            .Add(x => x.TagGroups, groups));

        var toggleTagMethod = typeof(TriStateTagFilterDropdownComponent).GetMethod("ToggleTagState", BindingFlags.Instance | BindingFlags.NonPublic);
        toggleTagMethod!.Invoke(cut.Instance, new object[] { tagId });

        // Act
        cut.Instance.ResetAll();

        // Assert
        var getBadgeTextMethod = typeof(TriStateTagFilterDropdownComponent).GetMethod("GetBadgeText", BindingFlags.Instance | BindingFlags.NonPublic);
        await Assert.That(getBadgeTextMethod!.Invoke(cut.Instance, null)).IsEqualTo("Filter Tags");
    }

    [Test]
    public async Task TriStateTagFilterDropdown_GetCurrentFilter_ReturnsCorrectState()
    {
        // Arrange
        var groups = GetMockTagGroups();
        var tagId1 = groups[0].Tags!.ElementAt(0).Id!.Value;
        var tagId2 = groups[0].Tags!.ElementAt(1).Id!.Value;
        _ctx.RenderComponent<MudPopoverProvider>();
        var cut = _ctx.RenderComponent<TriStateTagFilterDropdownComponent>(parameters => parameters
            .Add(x => x.TagGroups, groups));

        var toggleTagMethod = typeof(TriStateTagFilterDropdownComponent).GetMethod("ToggleTagState", BindingFlags.Instance | BindingFlags.NonPublic);

        // tagId1 -> Include
        toggleTagMethod!.Invoke(cut.Instance, new object[] { tagId1 });
        // tagId2 -> Include -> Exclude
        toggleTagMethod!.Invoke(cut.Instance, new object[] { tagId2 });
        toggleTagMethod!.Invoke(cut.Instance, new object[] { tagId2 });

        // Act
        var filter = cut.Instance.GetCurrentFilter();

        // Assert
        await Assert.That(filter.IncludedTagIds).Contains(tagId1);
        await Assert.That(filter.ExcludedTagIds).Contains(tagId2);
        await Assert.That(filter.InclusionMode).IsEqualTo("and");
        await Assert.That(filter.ExclusionMode).IsEqualTo("or");
    }
}

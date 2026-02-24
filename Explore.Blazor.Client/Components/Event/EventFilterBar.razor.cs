using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Components.Event;

public partial class EventFilterBar
{
    [Parameter] public bool IsIslamicModuleEnabled { get; set; }
    [Parameter] public bool IsTechModuleEnabled { get; set; }
    [Parameter] public EventCallback OnSearchRequested { get; set; }

    // Data Sources
    [Parameter] public ICollection<EventTypeListDto> EventTypes { get; set; } = new List<EventTypeListDto>();
    [Parameter] public ICollection<CategoryListDto> Categories { get; set; } = new List<CategoryListDto>();
    [Parameter] public ICollection<LocationListDto> Locations { get; set; } = new List<LocationListDto>();
    [Parameter] public ICollection<EventFormatListDto> EventFormats { get; set; } = new List<EventFormatListDto>();
    [Parameter] public ICollection<MadhabListDto> Madhabs { get; set; } = new List<MadhabListDto>();
    [Parameter] public ICollection<RegistrationModeListDto> RegistrationModes { get; set; } = new List<RegistrationModeListDto>();
    [Parameter] public ICollection<LanguageListDto> Languages { get; set; } = new List<LanguageListDto>();
    [Parameter] public ICollection<TagListDto> Tags { get; set; } = new List<TagListDto>();
    [Parameter] public ICollection<AudienceGenderListDto> AudienceGenders { get; set; } = new List<AudienceGenderListDto>();
    [Parameter] public ICollection<AudienceAgeListDto> AudienceAges { get; set; } = new List<AudienceAgeListDto>();
    [Parameter] public ICollection<EventStatusListDto> EventStatuses { get; set; } = new List<EventStatusListDto>();
    [Parameter] public ICollection<TagTypeWithTagsDto> TagGroups { get; set; } = new List<TagTypeWithTagsDto>();

    // Filter State
    public string SelectedDate { get; set; } = "";
    public string? SearchTerm { get; set; }
    public Guid? SelectedCategoryId { get; set; }
    public Guid? SelectedLocationId { get; set; }
    public int? SelectedFormatId { get; set; }
    public int? SelectedMadhabId { get; set; }
    public int? SelectedRegistrationModeId { get; set; }
    public int? SelectedLanguageId { get; set; }

    // Core Filters
    public int? SelectedEventTypeId { get; set; }
    public int? SelectedAudienceGenderId { get; set; }
    public int? SelectedAudienceAgeId { get; set; }
    public int? SelectedEventStatusId { get; set; }
    public string SelectedSortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;

    // Islamic Filters
    public GenderSegregationMode? SelectedGenderMode { get; set; }
    public bool? IncludesQuranRecitation { get; set; }
    public PrayerTime? SelectedReferencePrayer { get; set; }
    public int? SelectedIslamicPrimaryLanguageId { get; set; }
    public bool? HasIslamicAspect { get; set; }

    // Tech Filters
    public SkillLevel? SelectedSkillLevel { get; set; }
    public bool? IsCodingCompetition { get; set; }
    public bool? IsHackathon { get; set; }
    public bool? RequiresLaptop { get; set; }
    public string? TechStackTag { get; set; }
    public bool? HasTechAspect { get; set; }

    private TriStateTagFilterDropdown? _tagFilterDropdown;

    private async Task SearchAsync()
    {
        await OnSearchRequested.InvokeAsync();
    }

    public TagFilterChangedEventArgs GetTagFilter()
    {
        return _tagFilterDropdown?.GetCurrentFilter() ?? new TagFilterChangedEventArgs();
    }

    private async Task ClearAllFilters()
    {
        SelectedDate = "";
        SearchTerm = null;
        SelectedCategoryId = null;
        SelectedLocationId = null;
        SelectedFormatId = null;
        SelectedMadhabId = null;
        SelectedRegistrationModeId = null;
        SelectedLanguageId = null;
        SelectedEventTypeId = null;
        SelectedAudienceGenderId = null;
        SelectedAudienceAgeId = null;
        SelectedEventStatusId = null;
        SelectedSortBy = "date";
        SortDescending = true;

        SelectedGenderMode = null;
        IncludesQuranRecitation = null;
        SelectedReferencePrayer = null;
        SelectedIslamicPrimaryLanguageId = null;
        HasIslamicAspect = null;

        SelectedSkillLevel = null;
        IsCodingCompetition = null;
        IsHackathon = null;
        RequiresLaptop = null;
        TechStackTag = null;
        HasTechAspect = null;

        await OnSearchRequested.InvokeAsync();
    }

    public int GetActiveFilterCount()
    {
        int count = 0;
        if (!string.IsNullOrEmpty(SelectedDate)) count++;
        if (!string.IsNullOrEmpty(SearchTerm)) count++;
        if (SelectedCategoryId.HasValue) count++;
        if (SelectedLocationId.HasValue) count++;
        if (SelectedFormatId.HasValue) count++;
        if (SelectedMadhabId.HasValue) count++;
        if (SelectedRegistrationModeId.HasValue) count++;
        if (SelectedLanguageId.HasValue) count++;
        if (SelectedEventTypeId.HasValue) count++;
        if (SelectedAudienceGenderId.HasValue) count++;
        if (SelectedAudienceAgeId.HasValue) count++;
        if (SelectedEventStatusId.HasValue) count++;

        if (SelectedGenderMode.HasValue) count++;
        if (IncludesQuranRecitation.HasValue) count++;
        if (SelectedReferencePrayer.HasValue) count++;
        if (SelectedIslamicPrimaryLanguageId.HasValue) count++;
        if (HasIslamicAspect.HasValue) count++;

        if (SelectedSkillLevel.HasValue) count++;
        if (IsCodingCompetition.HasValue) count++;
        if (IsHackathon.HasValue) count++;
        if (RequiresLaptop.HasValue) count++;
        if (!string.IsNullOrEmpty(TechStackTag)) count++;
        if (HasTechAspect.HasValue) count++;

        var tagFilter = GetTagFilter();
        count += tagFilter.IncludedTagIds.Count + tagFilter.ExcludedTagIds.Count;

        return count;
    }
}

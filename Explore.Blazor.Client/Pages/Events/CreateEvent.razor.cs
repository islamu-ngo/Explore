// ABOUTME: Code-behind for the single-page Create Event page (Luma-inspired layout).
// ABOUTME: Handles publisher selection, inline image upload, description dialog, session management, and event creation.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Forms;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.EventTemplates;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Models;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

public partial class CreateEvent : IDisposable
{
    private const string MainContentAppearanceOwner = nameof(CreateEvent);

    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected IGroupService GroupService { get; set; } = null!;
    [Inject] protected IUserService UserService { get; set; } = null!;
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected IImageStorageService ImageStorageService { get; set; } = null!;
    [Inject] protected ICategoryService CategoryService { get; set; } = null!;
    [Inject] protected ITagService TagService { get; set; } = null!;
    [Inject] protected ILocationService LocationService { get; set; } = null!;
    [Inject] protected IEventTemplateService EventTemplateService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ILogger<CreateEvent> Logger { get; set; } = null!;
    [Inject] private IAccessibilityFocusService AccessibilityFocusService { get; set; } = default!;
    [Inject] private IAccessibilityAnnouncerService AccessibilityAnnouncerService { get; set; } = default!;
    [Inject] private IEventRegistrationPolicyService RegistrationPolicyService { get; set; } = default!;
    [Inject] private MainContentAppearanceState MainContentAppearanceState { get; set; } = default!;

    // Sentinel Guid values for "Create Organization" / "Create Group" dropdown items
    private static readonly Guid CreateOrgSentinelValue = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CreateGroupSentinelValue = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid? CreateOrgSentinel = CreateOrgSentinelValue;
    private static readonly Guid? CreateGroupSentinel = CreateGroupSentinelValue;

    // Publisher selection state
    private string _publisherMode = "personal";
    private Guid? _selectedOrganizationId;
    private ICollection<OrganizationListDto>? _myOrganizations;
    private string _organizationRoleError = string.Empty;
    private Guid? _selectedGroupId;
    private ICollection<GroupListDto>? _myGroups;
    private string _groupRoleError = string.Empty;
    private EventCreationContextDto? _creationContext;
    private string _creationContextError = string.Empty;

    private const int GroupCreatorRoleId = 30;
    private const int GroupAdminRoleId = 31;

    // Form state
    private Guid? _currentUserId;
    private CreateEventDraftRequestDto createDto = new();
    private EditContext _editContext = default!;
    private FormSubmitState _submitState = new();
    private ServerValidationErrorStore _errorStore = new();
    private ICollection<EventTypeListDto>? eventTypes;
    private ICollection<AudienceGenderListDto>? audienceGenders;
    private ICollection<AudienceAgeListDto>? audienceAges;
    private ICollection<EventFormatListDto>? eventFormats;
    private ICollection<VisibilityTypeListDto>? visibilityTypes;
    private ICollection<MadhabListDto>? madhabs;
    private ICollection<CategoryListDto>? allCategories;
    private ICollection<TagListDto>? allTags;
    private ICollection<LocationListDto>? locations;
    private ICollection<RegistrationModeListDto>? registrationModes;
    private ICollection<LanguageListDto>? languages;
    private ICollection<EventRegistrationPolicyListDto>? registrationPolicies;
    private IReadOnlyList<HalResourceOfEventTemplateListDto> eventTemplates = Array.Empty<HalResourceOfEventTemplateListDto>();
    private HalResourceOfEventTemplateDto? _selectedEventTemplate;
    private bool _isLoadingEventTemplates;
    private bool _isLoadingTemplatePreview;
    private string? _templateLoadError;
    private int _templateListRequestVersion;
    private int _templatePreviewRequestVersion;
    private bool IsSubmitDisabled =>
        _submitState.IsSubmitting
        || _isUploadingImage
        || _isLoadingTemplatePreview
        || (_creationContext is not null && _creationContext.CanCreate != true)
        || !string.IsNullOrEmpty(_organizationRoleError)
        || !string.IsNullOrEmpty(_groupRoleError);
    private bool isLoading = true;
    private bool _dataLoaded = false;
    private int? selectedMadhabId = null;
    private IReadOnlyList<EventPublishReadinessErrorDto> _publishReadinessErrors = Array.Empty<EventPublishReadinessErrorDto>();

    // Image upload state
    private string? imagePreviewUrl;
    private bool _isUploadingImage = false;
    private Guid? _uploadedImageStorageObjectId = null;
    private string? _uploadError;

    // Categories and Tags selection
    private IReadOnlyCollection<Guid> selectedCategoryIds = new HashSet<Guid>();
    private IReadOnlyCollection<Guid> selectedTagIds = new HashSet<Guid>();

    // Tag/Category management popup state
    private bool _showCreateTagCatPopup;
    private TagCategoryMode _createTagCatMode;
    private IReadOnlyCollection<Guid> _createTagCatInitialIds = Array.Empty<Guid>();

    private List<SessionEditorModel> sessions = new();
    private DateTime? _inlineSessionDate;
    private TimeSpan? _inlineSessionStartTime = TimeSpan.FromHours(9);
    private TimeSpan? _inlineSessionEndTime = TimeSpan.FromHours(17);
    private Guid? _inlineSessionLocationId;
    private int? _inlineSessionCapacity;
    private string _bgColor = string.Empty;
    private string _bgEffect = "None";
    private string _bgImageUri = string.Empty;

    // UI toggles
    private bool _showTimezoneSelector = false;
    private bool _isMoreOptionsExpanded;
    private bool _isThemeStudioOpen;
    private bool _isCustomSlugEnabled;
    private string _programAnnouncement = string.Empty;
    private string ProgramSummary => BuildProgramSummary();
    private string ProgramItemsSummary => BuildProgramItemsSummary();
    private string ProgramLogisticsSummary => BuildProgramLogisticsSummary();
    private string ProgramDefaultsSummary => BuildProgramDefaultsSummary();
    private string MoreOptionsSummary => BuildMoreOptionsSummary();
    private bool AreEventOptionsDisabled => _creationContext is not null && _creationContext.CanCreate != true;
    private string? EventOptionsPolicyNote => BuildEventOptionsPolicyNote();
    private string VisibilitySummary => GetLookupName(visibilityTypes, createDto.VisibilityTypeId) ?? "Select visibility";
    private string AudienceSummary => BuildAudienceSummary();
    private string RegistrationSummary => BuildRegistrationSummary();
    private string EventTypeSummary => GetLookupName(eventTypes, createDto.EventTypeId) ?? "Select event type";
    private string PricingSummary => BuildPricingSummary();
    private string MadhabSummary => GetLookupName(madhabs, selectedMadhabId) ?? "No madhab set";
    private List<string> SelectedCategoryNames => GetSelectedNames(allCategories, selectedCategoryIds);
    private List<string> SelectedTagNames => GetSelectedNames(allTags, selectedTagIds);
    private string CategoriesSummary => SelectedCategoryNames.Count == 0 ? "No categories selected" : string.Join(", ", SelectedCategoryNames);
    private string TagsSummary => SelectedTagNames.Count == 0 ? "No tags selected" : string.Join(", ", SelectedTagNames);
    // Timezone
    private TimeZoneInfo _selectedTimezone = TimeZoneInfo.Local;
    private string _selectedTimezoneDisplay => FormatTimezoneShort(_selectedTimezone);
    private static readonly IReadOnlyList<TimeZoneInfo> _allTimezones = TimeZoneInfo.GetSystemTimeZones();
    private Guid createdEventId = Guid.Empty;

    private string BuildProgramSummary()
    {
        var sessionLabel = sessions.Count switch
        {
            0 => "No sessions prepared yet",
            1 => "1 session prepared",
            _ => $"{sessions.Count} sessions prepared"
        };

        return $"{sessionLabel}; logistics are managed after the draft is saved.";
    }

    private string BuildProgramItemsSummary()
    {
        if (sessions.Count == 0)
        {
            return "Add talks, workshops, panels, classes, or activities as sessions.";
        }

        var firstTitle = sessions.First().Title?.Trim();
        var firstSession = string.IsNullOrWhiteSpace(firstTitle)
            ? "first session"
            : firstTitle;

        return sessions.Count == 1
            ? $"1 session prepared: {firstSession}"
            : $"{sessions.Count} sessions prepared, starting with {firstSession}";
    }

    private string BuildProgramLogisticsSummary()
    {
        return "Breaks, rooms, meals, prayer times, and day details are managed after the draft is saved.";
    }

    private string BuildProgramDefaultsSummary()
    {
        return $"{RegistrationSummary} · Session-specific defaults move to the session composer";
    }

    private string BuildMoreOptionsSummary()
    {
        var selectedCount = 0;
        if (createDto.TemplateId.HasValue)
        {
            selectedCount++;
        }

        return selectedCount == 0
            ? "Template and custom fields are optional."
            : $"{selectedCount} advanced setting{(selectedCount == 1 ? string.Empty : "s")} selected.";
    }

    private string? BuildEventOptionsPolicyNote()
    {
        if (_creationContext is null)
        {
            return null;
        }

        if (_creationContext.CanCreate != true)
        {
            return _creationContext.UnavailableReason ?? "Creation is currently unavailable for your account.";
        }

        return _creationContext.RequiresApproval == true
            ? "This publisher requires approval before the event becomes public. Required settings remain visible so review blockers are clear."
            : null;
    }

    private string BuildAudienceSummary()
    {
        var gender = GetLookupName(audienceGenders, createDto.AudienceGenderId) ?? "Any gender";
        var age = GetLookupName(audienceAges, createDto.AudienceAgeId) ?? "Any age";
        return $"{gender} · {age}";
    }

    private string BuildRegistrationSummary()
    {
        var policy = GetLookupName(registrationPolicies, createDto.RegistrationPolicyId) ?? "Default open registration";
        return $"{policy} · Capacity set per session";
    }

    private string BuildPricingSummary()
    {
        if (createDto.Price is > 0)
        {
            return $"{createDto.CurrencyCode ?? "EUR"} {createDto.Price:0.##}";
        }

        return "Free event";
    }

    private static string? GetLookupName(IEnumerable<VisibilityTypeListDto>? items, int? selectedId) =>
        items?.FirstOrDefault(item => item.Id == selectedId)?.FullName;

    private static string? GetLookupName(IEnumerable<AudienceGenderListDto>? items, int? selectedId) =>
        items?.FirstOrDefault(item => item.Id == selectedId)?.FullName;

    private static string? GetLookupName(IEnumerable<AudienceAgeListDto>? items, int? selectedId) =>
        items?.FirstOrDefault(item => item.Id == selectedId)?.FullName;

    private static string? GetLookupName(IEnumerable<EventRegistrationPolicyListDto>? items, int? selectedId) =>
        items?.FirstOrDefault(item => item.Id == selectedId)?.FullName;

    private static string? GetLookupName(IEnumerable<EventTypeListDto>? items, int? selectedId) =>
        items?.FirstOrDefault(item => item.Id == selectedId)?.FullName;

    private static string? GetLookupName(IEnumerable<MadhabListDto>? items, int? selectedId) =>
        items?.FirstOrDefault(item => item.Id == selectedId)?.FullName;

    private static string? GetLookupName(IEnumerable<LocationListDto>? items, Guid? selectedId) =>
        selectedId.HasValue
            ? items?.FirstOrDefault(item => item.Id == selectedId.Value)?.FullName
            : null;

    private static List<string> GetSelectedNames(IEnumerable<CategoryListDto>? items, IReadOnlyCollection<Guid> selectedIds) =>
        items?
            .Where(item => item.Id.HasValue && selectedIds.Contains(item.Id.Value))
            .Select(item => item.FullName)
            .ToList() ?? [];

    private static List<string> GetSelectedNames(IEnumerable<TagListDto>? items, IReadOnlyCollection<Guid> selectedIds) =>
        items?
            .Where(item => item.Id.HasValue && selectedIds.Contains(item.Id.Value))
            .Select(item => item.FullName)
            .ToList() ?? [];

    private void OpenCategoryManager()
    {
        if (AreEventOptionsDisabled)
        {
            return;
        }

        _createTagCatMode = TagCategoryMode.Categories;
        _createTagCatInitialIds = selectedCategoryIds.ToList().AsReadOnly();
        _showCreateTagCatPopup = true;
    }

    private void OpenTagManager()
    {
        if (AreEventOptionsDisabled)
        {
            return;
        }

        _createTagCatMode = TagCategoryMode.Tags;
        _createTagCatInitialIds = selectedTagIds.ToList().AsReadOnly();
        _showCreateTagCatPopup = true;
    }

    private async Task OpenThemeStudioAsync()
    {
        await AccessibilityFocusService.SaveFocusAsync();
        _isThemeStudioOpen = true;
        await AccessibilityAnnouncerService.AnnouncePoliteAsync("Theme studio opened.");
    }

    private async Task CloseThemeStudioAsync()
    {
        _isThemeStudioOpen = false;
        await AccessibilityFocusService.RestoreFocusAsync(".create-event__theme-advanced-button");
        await AccessibilityAnnouncerService.AnnouncePoliteAsync("Theme studio closed.");
    }

    private Task SetBackgroundColorAsync(string value)
    {
        _bgColor = value;
        PublishMainContentAppearance();
        return Task.CompletedTask;
    }

    private Task SetBackgroundEffectAsync(string value)
    {
        _bgEffect = string.IsNullOrWhiteSpace(value) ? "None" : value;
        PublishMainContentAppearance();
        return Task.CompletedTask;
    }

    private Task SetCustomSlugEnabledAsync(bool value)
    {
        _isCustomSlugEnabled = value;
        createDto.Slug = value ? EventUrlHelper.FormatSlug(createDto.Title) : null;
        return Task.CompletedTask;
    }

    private Task ApplyTitleSlugAsync()
    {
        if (_isCustomSlugEnabled)
            createDto.Slug = EventUrlHelper.FormatSlug(createDto.Title);
        else
            createDto.Slug = null;

        return Task.CompletedTask;
    }

    private string BuildCreateEventPreviewStyle()
    {
        var settings = new AppearanceSettings
        {
            BackgroundColor = _bgColor,
            ImageUri = _bgImageUri,
            BackgroundEffect = _bgEffect
        };

        return settings.IsEmpty
            ? string.Empty
            : AppearanceStyleBuilder.BuildSurfaceStyle(settings, "#F8FAFC");
    }

    private void PublishMainContentAppearance()
    {
        MainContentAppearanceState.Set(MainContentAppearanceOwner, BuildCreateEventPreviewStyle());
    }

    public void Dispose()
    {
        MainContentAppearanceState.Clear(MainContentAppearanceOwner);
    }

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(createDto);
        _errorStore.Init(_editContext);

        Logger.LogInformation("OnInitializedAsync starting");
        await LoadFormData();
        PublishMainContentAppearance();

    }

    // ========== Publisher Methods ==========

    private string SelectedPublisherKey => BuildPublisherKey(_publisherMode, _publisherMode switch
    {
        "organization" => _selectedOrganizationId,
        "group" => _selectedGroupId,
        _ => null
    });

    private IReadOnlyList<PublisherChoice> GetPublisherChoices()
    {
        if (_creationContext?.PublisherOptions?.Any() == true)
        {
            return _creationContext.PublisherOptions
                .Select(option => new PublisherChoice(
                    BuildPublisherKey(option.PublisherMode, option.PublisherId),
                    option.PublisherMode,
                    option.PublisherId,
                    option.CanPublish == true,
                    option.DisplayName ?? GetPublisherModeLabel(option.PublisherMode),
                    option.CanPublish == true
                        ? GetPublisherModeLabel(option.PublisherMode)
                        : option.Reason ?? "This publisher cannot create events."))
                .ToList();
        }

        return new[]
        {
            new PublisherChoice(BuildPublisherKey("personal", null), "personal", null, CanSelectPublisherMode("personal"), "Personal profile", "Personal profile"),
            new PublisherChoice(BuildPublisherKey("organization", null), "organization", null, CanSelectPublisherMode("organization"), "Organization", "Choose an organization below"),
            new PublisherChoice(BuildPublisherKey("group", null), "group", null, CanSelectPublisherMode("group"), "Group", "Choose a group below")
        };
    }

    private void OnPublisherSelectionChanged(string? key)
    {
        if (!TryParsePublisherKey(key, out var mode, out var publisherId))
        {
            return;
        }

        if (_creationContext is not null)
        {
            var option = GetPublisherOption(mode, publisherId);
            if (option?.CanPublish != true)
            {
                return;
            }

            _publisherMode = mode;
            _selectedOrganizationId = mode == "organization" ? publisherId : null;
            _selectedGroupId = mode == "group" ? publisherId : null;
            _organizationRoleError = string.Empty;
            _groupRoleError = string.Empty;
            return;
        }

        SetPublisherMode(mode);
    }

    private static string BuildPublisherKey(string? mode, Guid? publisherId) =>
        $"{mode ?? string.Empty}:{publisherId?.ToString("D") ?? string.Empty}";

    private static bool TryParsePublisherKey(string? key, out string mode, out Guid? publisherId)
    {
        mode = string.Empty;
        publisherId = null;

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var separatorIndex = key.IndexOf(':', StringComparison.Ordinal);
        mode = separatorIndex < 0 ? key : key[..separatorIndex];
        var rawId = separatorIndex < 0 ? string.Empty : key[(separatorIndex + 1)..];
        if (!string.IsNullOrWhiteSpace(rawId) && Guid.TryParse(rawId, out var parsedId))
        {
            publisherId = parsedId;
        }

        return !string.IsNullOrWhiteSpace(mode);
    }

    private static string GetPublisherModeLabel(string? mode) => mode switch
    {
        "personal" => "Personal profile",
        "organization" => "Organization",
        "group" => "Group",
        _ => "Publisher"
    };

    private static string GetPublisherIcon(string? mode) => mode switch
    {
        "organization" => Icons.Material.Filled.Business,
        "group" => Icons.Material.Filled.Group,
        _ => Icons.Material.Filled.Person
    };

    private void SetPublisherMode(string mode)
    {
        if (!CanSelectPublisherMode(mode))
        {
            return;
        }

        _publisherMode = mode;
        if (mode == "personal")
        {
            _selectedOrganizationId = null;
            _selectedGroupId = null;
            _organizationRoleError = string.Empty;
            _groupRoleError = string.Empty;
        }
        else if (mode == "organization")
        {
            _selectedGroupId = null;
            _groupRoleError = string.Empty;
        }
        else if (mode == "group")
        {
            _selectedOrganizationId = null;
            _organizationRoleError = string.Empty;
        }
    }

    private void OnOrganizationDropdownChanged(Guid? value)
    {
        if (value.HasValue && value.Value == CreateOrgSentinelValue)
        {
            _selectedOrganizationId = null;
            Navigation.NavigateTo("/organizations/create");
            return;
        }

        _selectedOrganizationId = value;
        _organizationRoleError = string.Empty;

        if (value.HasValue)
        {
            var contextOption = GetPublisherOption("organization", value.Value);
            if (_creationContext is not null && contextOption?.CanPublish != true)
            {
                _organizationRoleError = contextOption?.Reason ?? "You cannot publish events for this organization.";
                return;
            }

            var org = _myOrganizations?.FirstOrDefault(o => o.Id == value.Value);
            if (_creationContext is null && org?.CurrentUserRole != null && !RoleHelper.CanManage(org.CurrentUserRole))
            {
                _organizationRoleError = "You don't have the authority to publish events for this organization. Only Creator, Co-Owner, or Admin roles can publish.";
            }
        }
    }

    private void OnGroupDropdownChanged(Guid? value)
    {
        if (value.HasValue && value.Value == CreateGroupSentinelValue)
        {
            _selectedGroupId = null;
            Navigation.NavigateTo("/groups/create");
            return;
        }

        _selectedGroupId = value;
        _groupRoleError = string.Empty;

        if (value.HasValue)
        {
            var contextOption = GetPublisherOption("group", value.Value);
            if (_creationContext is not null && contextOption?.CanPublish != true)
            {
                _groupRoleError = contextOption?.Reason ?? "You cannot publish events for this group.";
                return;
            }

            var group = _myGroups?.FirstOrDefault(g => g.Id == value.Value);
            if (_creationContext is null && group?.CurrentUserRole != null && !CanPublishAsGroup(group.CurrentUserRole))
            {
                _groupRoleError = "You don't have the authority to publish events for this group. Only Creator or Admin roles can publish.";
            }
        }
    }

    private sealed record PublisherChoice(
        string Key,
        string Mode,
        Guid? PublisherId,
        bool CanPublish,
        string DisplayName,
        string Description);

    private async Task OnEventTypeChanged(int? eventTypeId)
    {
        if (createDto.EventTypeId == eventTypeId)
        {
            return;
        }

        createDto.EventTypeId = eventTypeId;
        createDto.TemplateId = null;
        _selectedEventTemplate = null;
        eventTemplates = Array.Empty<HalResourceOfEventTemplateListDto>();
        ClearSessionTemplateSelections();
        await LoadEventTemplatesAsync(eventTypeId);
    }

    private async Task OnEventTemplateChanged(Guid? templateId)
    {
        var previewRequestVersion = ++_templatePreviewRequestVersion;
        createDto.TemplateId = templateId;
        _selectedEventTemplate = null;
        _templateLoadError = null;
        ClearSessionTemplateSelections();

        if (!templateId.HasValue || templateId.Value == Guid.Empty)
        {
            _isLoadingTemplatePreview = false;
            return;
        }

        var requestedTemplateId = templateId.Value;
        _isLoadingTemplatePreview = true;
        try
        {
            var template = await EventTemplateService.GetTemplateByIdAsync(requestedTemplateId);
            if (!IsCurrentTemplatePreviewRequest(previewRequestVersion, requestedTemplateId))
            {
                return;
            }

            if (template is null)
            {
                createDto.TemplateId = null;
                _templateLoadError = "The selected template could not be loaded. The selection was cleared; choose another template or continue without one.";
                return;
            }

            _selectedEventTemplate = template;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load event template preview for {TemplateId}", requestedTemplateId);
            if (IsCurrentTemplatePreviewRequest(previewRequestVersion, requestedTemplateId))
            {
                createDto.TemplateId = null;
                _templateLoadError = "Template preview could not be loaded. The selection was cleared; choose another template or continue without one.";
            }
        }
        finally
        {
            if (IsCurrentTemplatePreviewRequest(previewRequestVersion, requestedTemplateId) || !createDto.TemplateId.HasValue)
            {
                _isLoadingTemplatePreview = false;
            }
        }
    }

    private bool IsCurrentTemplatePreviewRequest(int requestVersion, Guid requestedTemplateId) =>
        requestVersion == _templatePreviewRequestVersion && createDto.TemplateId == requestedTemplateId;

    private void ClearSessionTemplateSelections()
    {
        foreach (var session in sessions)
        {
            session.SessionTemplateId = null;
        }

    }

    // ========== Image Upload ==========

    private async Task OnImageFileSelected(IBrowserFile? file)
    {
        _uploadError = null;

        if (file == null) return;

        if (!ImageUploadClientPolicy.IsAllowedImageContentType(file.ContentType))
        {
            _uploadError = ImageUploadClientPolicy.UnsupportedImageTypeMessage;
            return;
        }

        const long maxSize = ImageUploadClientPolicy.DefaultMaxImageFileSizeBytes;
        if (file.Size > maxSize)
        {
            _uploadError = ImageUploadClientPolicy.FormatMaxFileSizeMessage(maxSize);
            return;
        }

        _isUploadingImage = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Logger.LogInformation(
                "[CreateEvent] Reading selected image. SizeBucket={SizeBucket}, ContentTypeBucket={ContentTypeBucket}",
                ImageUploadClientPolicy.GetSizeBucket(file.Size),
                ImageUploadClientPolicy.GetContentTypeBucket(file.ContentType));
            var fileData = await ImageStorageService.ReadFileAsync(file, maxSize);
            if (fileData == null)
            {
                _uploadError = ImageUploadClientPolicy.ReadFailureMessage;
                return;
            }

            var preview = ImageStorageService.GenerateLocalPreviewFromBytes(fileData);
            if (!string.IsNullOrEmpty(preview))
            {
                imagePreviewUrl = preview;
            }

            Logger.LogInformation("[CreateEvent] Uploading image...");
            var uploadResult = await ImageStorageService.UploadAndCreateRecordFromBytesAsync(fileData);
            if (uploadResult?.Success == true)
            {
                _uploadedImageStorageObjectId = uploadResult.StorageObjectId;
                _uploadError = null;
                Logger.LogInformation("[CreateEvent] Image uploaded. StorageObjectId: {Id}", uploadResult.StorageObjectId);
            }
            else
            {
                _uploadError = ImageUploadClientPolicy.ToUserSafeUploadError(uploadResult?.ErrorMessage);
                imagePreviewUrl = null;
                _uploadedImageStorageObjectId = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                "[CreateEvent] Image upload failed. FailureType={FailureType}, SizeBucket={SizeBucket}",
                ImageUploadClientPolicy.GetFailureType(ex),
                ImageUploadClientPolicy.GetSizeBucket(file.Size));
            _uploadError = ImageUploadClientPolicy.GenericUploadFailureMessage;
            imagePreviewUrl = null;
            _uploadedImageStorageObjectId = null;
        }
        finally
        {
            _isUploadingImage = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ========== Content Dialog ==========

    private async Task OpenDescriptionDialog()
    {
        var parameters = new DialogParameters<DescriptionDialog>
        {
            { x => x.Content, createDto.Content }
        };

        var options = DialogOptionsFactory.Editor();

        await AccessibilityFocusService.SaveFocusAsync();
        var dialog = await DialogService.ShowAsync<DescriptionDialog>("", parameters, options);
        var result = await dialog.Result;
        await AccessibilityFocusService.RestoreFocusAsync();

        if (result is not null && !result.Canceled)
        {
            createDto.Content = result.Data?.ToString();
        }
    }

    // ========== Session Management ==========

    private async Task AddSessionAsync()
    {
        AnnounceProgramChange("Saving this event as a draft before opening session management.");
        await SubmitEventAsync(CreateEventSubmitIntent.SaveDraftAndAddSession);
    }

    private async Task OpenDedicatedSessionComposerAsync(int _)
    {
        AnnounceProgramChange("Saving this event as a draft before opening the dedicated session composer.");
        await SubmitEventAsync(CreateEventSubmitIntent.SaveDraftAndAddSession);
    }

    private void ShowDuplicateSessionUnavailable(int _)
    {
        AnnounceProgramChange("Duplicate session will be available from the dedicated session composer.");
    }

    private void ShowProgramSectionUnavailable()
    {
        AnnounceProgramChange("Section and track setup will open from the saved event program manager.");
    }

    private void AnnounceProgramChange(string message)
    {
        _programAnnouncement = message;
    }

    private async void RemoveSession(int index)
    {
        if (index >= 0 && index < sessions.Count)
        {
            if (sessions.Count <= 1)
            {
                _submitState.Fail("You must have at least one session.");
                return;
            }

            var session = sessions[index];

            if (session.Id.HasValue && session.Id != Guid.Empty)
            {
                await AccessibilityFocusService.SaveFocusAsync();
                bool? confirm = await DialogService.ShowMessageBoxAsync(
                    "Delete Session",
                    "This session already exists. Deleting it here will remove it permanently. Continue?",
                    yesText: "Delete", cancelText: "Cancel");
                await AccessibilityFocusService.RestoreFocusAsync();

                if (confirm == true)
                {
                    try
                    {
                        await EventService.DeleteSessionAsync(session.Id.Value);
                        sessions.RemoveAt(index);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to delete session {EventSessionId} while editing draft event", session.Id.Value);
                        _submitState.Fail("Session could not be deleted. Please try again.");
                    }
                }
            }
            else
            {
                sessions.RemoveAt(index);
            }
        }
    }

    // ========== Form Data Loading ==========

    private async Task LoadFormData()
    {
        if (_dataLoaded) return;

        try
        {
            isLoading = true;
            Logger.LogInformation("Loading form data for create event page");

            if (!_currentUserId.HasValue)
            {
                try
                {
                    var currentUser = await UserService.GetCurrentUserAsync();
                    if (currentUser != null)
                    {
                        _currentUserId = currentUser.Id;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error getting current user");
                }
            }

            await LoadCreationContextAsync();

            try
            {
                _myOrganizations = await OrganizationService.GetMyOrganizationsAsync();
                Logger.LogInformation("Loaded {Count} organizations", _myOrganizations?.Count ?? 0);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading user organizations");
                _myOrganizations = new List<OrganizationListDto>();
            }

            try
            {
                _myGroups = await GroupService.GetMyGroupsAsync();
                Logger.LogInformation("Loaded {Count} groups", _myGroups?.Count ?? 0);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading user groups");
                _myGroups = new List<GroupListDto>();
            }

            var eventTypesTask = AdminService.GetEventTypesAsync();
            var audienceGendersTask = AdminService.GetAudienceGendersAsync();
            var audienceAgesTask = AdminService.GetAudienceAgesAsync();
            var eventFormatsTask = AdminService.GetEventFormatsAsync();
            var visibilityTypesTask = AdminService.GetVisibilityTypesAsync();
            var madhabsTask = AdminService.GetMadhabsAsync();
            var categoriesTask = CategoryService.GetAllCategoriesAsync();
            var tagsTask = TagService.GetAllTagsAsync();
            var locationsTask = LocationService.GetAllLocationsAsync();
            var registrationModesTask = AdminService.GetRegistrationModesAsync();
            var languagesTask = AdminService.GetLanguagesAsync();
            var registrationPoliciesTask = RegistrationPolicyService.GetEventRegistrationPoliciesAsync();
            var eventTemplatesTask = LoadEventTemplatesAsync(createDto.EventTypeId);

            await Task.WhenAll(eventTypesTask, audienceGendersTask, audienceAgesTask, eventFormatsTask, visibilityTypesTask, madhabsTask, categoriesTask, tagsTask, locationsTask, registrationModesTask, languagesTask, registrationPoliciesTask, eventTemplatesTask);

            eventTypes = await eventTypesTask;
            audienceGenders = await audienceGendersTask;
            audienceAges = await audienceAgesTask;
            eventFormats = await eventFormatsTask;
            visibilityTypes = await visibilityTypesTask;
            madhabs = await madhabsTask;
            allCategories = await categoriesTask;
            allTags = await tagsTask;
            locations = await locationsTask;
            registrationModes = await registrationModesTask;
            languages = await languagesTask;
            registrationPolicies = await registrationPoliciesTask;

            SetDefaultValues();
            _dataLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading form data");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadEventTemplatesAsync(int? eventTypeId)
    {
        var requestVersion = ++_templateListRequestVersion;
        _templateLoadError = null;
        _isLoadingEventTemplates = true;
        try
        {
            var templates = await EventTemplateService.GetTemplatesAsync(eventTypeId, pageNumber: 1, pageSize: 100);
            if (!IsCurrentTemplateListRequest(requestVersion, eventTypeId))
            {
                return;
            }

            eventTemplates = (templates._embedded?.Items ?? [])
                .Where(t => t.IsActive == true && t.IsPublished == true)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.DisplayName)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load event templates for event type {EventTypeId}", eventTypeId);
            if (IsCurrentTemplateListRequest(requestVersion, eventTypeId))
            {
                eventTemplates = Array.Empty<HalResourceOfEventTemplateListDto>();
                _templateLoadError = "Event templates could not be loaded. You can still create a vanilla event.";
            }
        }
        finally
        {
            if (IsCurrentTemplateListRequest(requestVersion, eventTypeId))
            {
                _isLoadingEventTemplates = false;
            }
        }
    }

    private bool IsCurrentTemplateListRequest(int requestVersion, int? requestedEventTypeId) =>
        requestVersion == _templateListRequestVersion && createDto.EventTypeId == requestedEventTypeId;

    private void SetDefaultValues()
    {
        if (eventFormats?.Any() == true && createDto.EventFormatId is null or <= 0)
        {
            createDto.EventFormatId = eventFormats.First().Id;
        }
        if (visibilityTypes?.Any() == true && createDto.VisibilityTypeId is null or <= 0)
        {
            createDto.VisibilityTypeId = visibilityTypes.First().Id;
        }
        if (!createDto.IsRegistrationRequired.HasValue)
        {
            createDto.IsRegistrationRequired = true;
        }
        _inlineSessionDate ??= DateTime.Today.AddDays(1);
    }

    // ========== Validation & Submission ==========

    private bool ValidateForm()
    {
        _submitState.Reset();

        if (_creationContext is not null && _creationContext.CanCreate != true)
        {
            _submitState.Fail(_creationContext.UnavailableReason ?? "You do not have access to create events.");
            return false;
        }

        if (!CanSelectPublisherMode(_publisherMode))
        {
            _submitState.Fail("Select an available publisher before creating the event.");
            return false;
        }

        if (_publisherMode == "organization")
        {
            if (!_selectedOrganizationId.HasValue)
            {
                _submitState.Fail("Please select an organization.");
                return false;
            }
            if (!string.IsNullOrEmpty(_organizationRoleError))
            {
                _submitState.Fail(_organizationRoleError);
                return false;
            }
            if (_creationContext is not null && GetPublisherOption("organization", _selectedOrganizationId.Value)?.CanPublish != true)
            {
                _submitState.Fail("You cannot publish events for the selected organization.");
                return false;
            }
        }

        if (_publisherMode == "group")
        {
            if (!_selectedGroupId.HasValue)
            {
                _submitState.Fail("Please select a group.");
                return false;
            }
            if (!string.IsNullOrEmpty(_groupRoleError))
            {
                _submitState.Fail(_groupRoleError);
                return false;
            }
            if (_creationContext is not null && GetPublisherOption("group", _selectedGroupId.Value)?.CanPublish != true)
            {
                _submitState.Fail("You cannot publish events for the selected group.");
                return false;
            }
        }

        return true;
    }

    private async Task LoadCreationContextAsync()
    {
        _creationContextError = string.Empty;

        try
        {
            _creationContext = await EventService.GetEventCreationContextAsync();
            ApplyCreationContextDefaults();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading event creation context");
            _creationContext = null;
            _creationContextError = "Creation permissions could not be loaded. You can continue filling the form, but publishing may fail.";
        }
    }

    private void ApplyCreationContextDefaults()
    {
        if (_creationContext is null)
        {
            return;
        }

        if (_creationContext.CanCreate != true)
        {
            _publisherMode = "personal";
            _selectedOrganizationId = null;
            _selectedGroupId = null;
            return;
        }

        var defaultMode = _creationContext.DefaultPublisherMode;
        if (string.IsNullOrWhiteSpace(defaultMode) || !CanSelectPublisherMode(defaultMode))
        {
            defaultMode = GetFirstPublishableOption()?.PublisherMode;
        }

        if (string.IsNullOrWhiteSpace(defaultMode))
        {
            return;
        }

        _publisherMode = defaultMode;
        _selectedOrganizationId = null;
        _selectedGroupId = null;
        _organizationRoleError = string.Empty;
        _groupRoleError = string.Empty;

        if (_publisherMode == "organization")
        {
            _selectedOrganizationId = GetFirstPublishableOption("organization")?.PublisherId;
        }
        else if (_publisherMode == "group")
        {
            _selectedGroupId = GetFirstPublishableOption("group")?.PublisherId;
        }
    }

    private EventCreationPublisherOptionDto? GetFirstPublishableOption(string? mode = null) =>
        _creationContext?.PublisherOptions?
            .FirstOrDefault(option => option.CanPublish == true && (mode is null || option.PublisherMode == mode));

    private EventCreationPublisherOptionDto? GetPublisherOption(string mode, Guid? publisherId) =>
        _creationContext?.PublisherOptions?
            .FirstOrDefault(option => option.PublisherMode == mode && option.PublisherId == publisherId);

    private bool CanSelectPublisherMode(string mode)
    {
        if (_creationContext is null)
        {
            return true;
        }

        return mode switch
        {
            "personal" => GetPublisherOption("personal", null)?.CanPublish == true,
            "organization" => _creationContext.AllowOrganizationPublishing == true && GetFirstPublishableOption("organization") is not null,
            "group" => _creationContext.AllowGroupPublishing == true && GetFirstPublishableOption("group") is not null,
            _ => false
        };
    }

    private bool CanPublishAsOrganization(Guid? organizationId)
    {
        if (organizationId == CreateOrgSentinel)
        {
            return CanSelectPublisherMode("organization");
        }

        if (_creationContext is null)
        {
            var org = _myOrganizations?.FirstOrDefault(o => o.Id == organizationId);
            return org?.CurrentUserRole is null || RoleHelper.CanManage(org.CurrentUserRole);
        }

        return GetPublisherOption("organization", organizationId)?.CanPublish == true;
    }

    private bool CanPublishAsGroup(Guid? groupId)
    {
        if (groupId == CreateGroupSentinel)
        {
            return CanSelectPublisherMode("group");
        }

        if (_creationContext is null)
        {
            var group = _myGroups?.FirstOrDefault(g => g.Id == groupId);
            return group?.CurrentUserRole is null || CanPublishAsGroup(group.CurrentUserRole);
        }

        return GetPublisherOption("group", groupId)?.CanPublish == true;
    }

    private async Task HandleSubmit() => await SubmitEventAsync(CreateEventSubmitIntent.ReviewAndPublish);

    private async Task SubmitEventAsync(CreateEventSubmitIntent intent)
    {
        if (!ValidateForm())
        {
            return;
        }

        if (_isUploadingImage)
        {
            _submitState.Fail("Please wait for the image upload to complete.");
            return;
        }

        if (_isLoadingTemplatePreview)
        {
            _submitState.Fail("Please wait for the template preview to finish loading.");
            return;
        }

        if (_submitState.IsSubmitting) return;
        _submitState.Start();
        _publishReadinessErrors = Array.Empty<EventPublishReadinessErrorDto>();

        try
        {
            Guid? featuredImageId = _uploadedImageStorageObjectId;

            if (featuredImageId.HasValue)
            {
                Logger.LogInformation("Using pre-uploaded image. StorageObjectId: {StorageObjectId}", featuredImageId);
            }

            createDto.OrganizationId = _publisherMode == "organization" ? _selectedOrganizationId : null;
            createDto.GroupId = _publisherMode == "group" ? _selectedGroupId : null;
            createDto.FeaturedImageId = featuredImageId;
            createDto.MadhabId = selectedMadhabId;
            createDto.IsRegistrationRequired = createDto.RegistrationPolicyId.HasValue;
            createDto.VisibilityTypeId ??= 1;
            createDto.EventFormatId ??= 1;
            createDto.EventStatusId = (intent == CreateEventSubmitIntent.ReviewAndPublish) ? 2 : 1;
            createDto.Timezone = _selectedTimezone.Id;
            createDto.BackgroundColor = string.IsNullOrWhiteSpace(_bgColor) ? null : _bgColor;
            createDto.BackgroundEffect = string.IsNullOrWhiteSpace(_bgEffect) || _bgEffect == "None" ? null : _bgEffect;

            PopulateCreateRequest();
            if (!ApplyInlineSessionForIntent(intent))
            {
                return;
            }

            Logger.LogInformation(
                "Creating event (publisherMode={Mode}, organizationId={OrgId}, groupId={GroupId})",
                _publisherMode,
                createDto.OrganizationId,
                createDto.GroupId);
            var response = await EventService.CreateEventAsync(createDto);

            if (response?.Success == true && response.Id.HasValue && response.Id != Guid.Empty)
            {
                _submitState.Complete();
                createdEventId = response.Id.Value;
                Logger.LogInformation("Event created with ID: {EventId}", createdEventId);

                if (intent == CreateEventSubmitIntent.SaveDraft)
                {
                    Navigation.NavigateTo($"/events/{createdEventId}/edit");
                    return;
                }

                if (intent == CreateEventSubmitIntent.SaveDraftAndAddSession)
                {
                    Navigation.NavigateTo($"/events/{createdEventId}/sessions/create");
                    return;
                }

                Navigation.NavigateTo($"/event-created/{createdEventId}");
            }
            else
            {
                var errorMsg = response?.Message ?? "Failed to create event.";
                if (response?.Errors != null && response.Errors.Any())
                {
                    errorMsg += " Errors: " + string.Join(", ", response.Errors);
                }
                _submitState.Fail(errorMsg);
            }
        }
        catch (ApiException ex)
        {
            if (!_errorStore.HandleApiError(ex))
            {
                Logger.LogError(ex, "Exception during event creation");
                _submitState.Fail("Event could not be submitted. Please try again.");
            }
            else
            {
                _submitState.Fail("Please fix the validation errors below.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during event creation");
            _submitState.Fail("Event could not be submitted. Please try again.");
        }
    }

    private bool ApplyInlineSessionForIntent(CreateEventSubmitIntent intent)
    {
        createDto.Sessions = new List<CreateEventSessionRequest>();

        if (intent != CreateEventSubmitIntent.ReviewAndPublish)
        {
            return true;
        }

        var startTimeUtc = DateTimeHelper.CombineDateTimeToUtc(_inlineSessionDate, _inlineSessionStartTime);
        var endTimeUtc = DateTimeHelper.CombineDateTimeToUtc(_inlineSessionDate, _inlineSessionEndTime);

        if (!startTimeUtc.HasValue || !endTimeUtc.HasValue)
        {
            _submitState.Fail("Choose the event date, start time, and end time before publishing.");
            return false;
        }

        if (endTimeUtc.Value <= startTimeUtc.Value)
        {
            _submitState.Fail("The event end time must be after the start time.");
            return false;
        }

        createDto.Sessions = new List<CreateEventSessionRequest>
        {
            new()
            {
                TempKey = "inline-primary-session",
                Title = string.IsNullOrWhiteSpace(createDto.Title) ? "Main session" : createDto.Title.Trim(),
                Description = createDto.Description,
                StartTime = startTimeUtc.Value,
                EndTime = endTimeUtc.Value,
                LocationId = _inlineSessionLocationId,
                SortOrder = 1,
                MaxAudienceAttendees = _inlineSessionCapacity is > 0 ? _inlineSessionCapacity : null,
                RegistrationModeId = registrationModes?.FirstOrDefault()?.Id,
                LanguageIds = new List<int>()
            }
        };

        return true;
    }

    private async Task SaveAsDraftAsync()
    {
        await SubmitEventAsync(CreateEventSubmitIntent.SaveDraft);
    }


    private async Task ShowPublishReadinessErrorsAsync()
    {
        var firstFieldPath = _publishReadinessErrors.FirstOrDefault()?.FieldPath;
        if (!string.IsNullOrWhiteSpace(firstFieldPath))
        {
            _isMoreOptionsExpanded = true;
        }

        await InvokeAsync(StateHasChanged);
        await AccessibilityFocusService.FocusByIdAsync("publish-readiness-errors");
    }

    private enum CreateEventSubmitIntent
    {
        SaveDraft,
        SaveDraftAndAddSession,
        ReviewAndPublish
    }

    // ========== Helpers ==========

    private string GetPublisherDescription()
    {
        if (_publisherMode == "personal")
            return "Publishing as yourself (User Reported)";

        if (_selectedOrganizationId.HasValue && _myOrganizations != null)
        {
            var org = _myOrganizations.FirstOrDefault(o => o.Id == _selectedOrganizationId.Value);
            if (org != null) return $"Publishing for {org.FullName}";
        }

        if (_publisherMode == "group" && _selectedGroupId.HasValue && _myGroups != null)
        {
            var group = _myGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value);
            if (group != null) return $"Publishing for {group.FullName}";
        }

        return "Select who is publishing this event";
    }

    private static string GetRoleName(int roleId) => roleId switch
    {
        GroupCreatorRoleId => "Group Creator",
        GroupAdminRoleId => "Group Admin",
        32 => "Group Moderator",
        33 => "Group Member",
        _ => RoleHelper.GetRoleName(roleId)
    };

    private static bool CanPublishAsGroup(int? roleId) => roleId is GroupCreatorRoleId or GroupAdminRoleId;

    private static bool CanPublishAsGroup(RoleEnum? role) => CanPublishAsGroup(RoleHelper.ToRoleId(role));

    // ========== Timezone Methods ==========

    private Task<IEnumerable<TimeZoneInfo>> SearchTimezones(string? searchText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Task.FromResult(_allTimezones.AsEnumerable());
        }

        var results = _allTimezones
            .Where(tz => tz.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                      || tz.Id.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                      || tz.StandardName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .AsEnumerable();

        return Task.FromResult(results);
    }

    private void OnTimezoneChanged(TimeZoneInfo? tz)
    {
        if (tz != null)
        {
            _selectedTimezone = tz;
        }
        _showTimezoneSelector = false;
    }

    private static string FormatTimezoneShort(TimeZoneInfo tz)
    {
        var offset = tz.BaseUtcOffset;
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var abs = offset.Duration();
        return $"GMT{sign}{abs.Hours}:{abs.Minutes:D2} {tz.StandardName}";
    }

    private void HandleCreateTagCatSaved(IReadOnlyCollection<Guid> newIds)
    {
        if (_createTagCatMode == TagCategoryMode.Tags)
            selectedTagIds = new HashSet<Guid>(newIds);
        else
            selectedCategoryIds = new HashSet<Guid>(newIds);

        StateHasChanged();
    }

    private void PopulateCreateRequest()
    {
        createDto.CategoryIds = selectedCategoryIds.ToList();
        createDto.TagIds = selectedTagIds.ToList();
        if (createDto.Price is > 0)
        {
            createDto.CurrencyCode = string.IsNullOrWhiteSpace(createDto.CurrencyCode)
                ? "EUR"
                : createDto.CurrencyCode.Trim().ToUpperInvariant();
        }
        else
        {
            createDto.Price = null;
            createDto.CurrencyCode = null;
        }
    }
}

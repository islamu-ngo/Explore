// ABOUTME: Code-behind for the single-page Create Event page (Luma-inspired layout).
// ABOUTME: Handles publisher selection, inline image upload, description dialog, session management, and event creation.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.EventTemplates;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models.EventTemplates;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Models;
using Explore.Blazor.Client.Pages.Events.Workflows;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

public partial class CreateEvent
{
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
    [Inject] private IEventRegistrationPolicyService RegistrationPolicyService { get; set; } = default!;

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
    private ICollection<GroupPublisherListDto>? _myGroups;
    private string _groupRoleError = string.Empty;

    private const int GroupCreatorRoleId = 30;
    private const int GroupAdminRoleId = 31;

    // Form state
    private Guid? _currentUserId;
    private CreateEventDto createDto = new();
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
    private IReadOnlyList<EventTemplateListModel> eventTemplates = Array.Empty<EventTemplateListModel>();
    private EventTemplateDetailModel? _selectedEventTemplate;
    private bool _isLoadingEventTemplates;
    private bool _isLoadingTemplatePreview;
    private string? _templateLoadError;
    private int _templateListRequestVersion;
    private int _templatePreviewRequestVersion;
    private bool IsSubmitDisabled =>
        isProcessing
        || _isUploadingImage
        || _isLoadingTemplatePreview
        || !string.IsNullOrEmpty(_organizationRoleError)
        || !string.IsNullOrEmpty(_groupRoleError);
    private bool isLoading = true;
    private bool _dataLoaded = false;
    private int? selectedMadhabId = null;
    private string errorMessage = string.Empty;

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

    // Sessions
    private List<SessionEditorModel> sessions = new();
    private readonly SessionEditorWorkflow _sessionWorkflow = new();
    private string _bgColor = string.Empty;
    private string _bgEffect = "None";
    private string _bgImageUri = string.Empty;

    // Inline scheduling state (sent with create request)
    private List<InlineDayModel> _inlineDays = new();
    private List<InlineRoomModel> _inlineRooms = new();
    private List<InlineAgendaItemModel> _inlineAgendaItems = new();

    // Day add form
    private DateTime? _newDayDate;
    private string? _newDayLabel;

    // Room add form
    private string? _newRoomName;
    private int? _newRoomCapacity;

    // Agenda item add form
    private string? _newAgendaTitle;
    private DateOnly? _newAgendaDayDate;
    private TimeSpan? _newAgendaStartTime;
    private TimeSpan? _newAgendaEndTime;
    private int? _newAgendaRoomIndex;

    // UI toggles
    private bool _showFirstSessionLocation = false;
    private bool _showTimezoneSelector = false;

    // Timezone
    private TimeZoneInfo _selectedTimezone = TimeZoneInfo.Local;
    private string _selectedTimezoneDisplay => FormatTimezoneShort(_selectedTimezone);
    private static readonly IReadOnlyList<TimeZoneInfo> _allTimezones = TimeZoneInfo.GetSystemTimeZones();
    private bool isProcessing = false;
    private Guid createdEventId = Guid.Empty;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("OnInitializedAsync starting");
        await LoadFormData();

        if (!sessions.Any())
        {
            AddDefaultFirstSession();
        }
    }

    private void AddDefaultFirstSession()
    {
        var defaultStart = DateTime.Today.AddDays(1).AddHours(9);
        var defaultEnd = DateTime.Today.AddDays(1).AddHours(10);

        sessions.Add(new SessionEditorModel
        {
            StartTime = defaultStart,
            EndTime = defaultEnd,
            RegistrationModeId = 1,
            UseEventImage = true
        });
    }

    // ========== Publisher Methods ==========

    private void SetPublisherMode(string mode)
    {
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
            var org = _myOrganizations?.FirstOrDefault(o => o.Id == value.Value);
            if (org?.CurrentUserRole != null && !RoleHelper.CanManage(org.CurrentUserRole))
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
            var group = _myGroups?.FirstOrDefault(g => g.Id == value.Value);
            if (group?.CurrentUserRole != null && !CanPublishAsGroup(group.CurrentUserRole))
            {
                _groupRoleError = "You don't have the authority to publish events for this group. Only Creator or Admin roles can publish.";
            }
        }
    }

    private async Task OnEventTypeChanged(int? eventTypeId)
    {
        if (createDto.EventTypeId == eventTypeId)
        {
            return;
        }

        createDto.EventTypeId = eventTypeId;
        createDto.TemplateId = null;
        _selectedEventTemplate = null;
        eventTemplates = Array.Empty<EventTemplateListModel>();
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

        if (_sessionWorkflow.DrawerModel is not null)
        {
            _sessionWorkflow.DrawerModel.SessionTemplateId = null;
        }
    }

    // ========== Image Upload ==========

    private async Task OnImageFileSelected(IBrowserFile? file)
    {
        _uploadError = null;

        if (file == null) return;

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            _uploadError = "Please select a valid image file (JPG, PNG, GIF, or WebP).";
            return;
        }

        const long maxSize = 5 * 1024 * 1024;
        if (file.Size > maxSize)
        {
            _uploadError = "File size must be less than 5MB.";
            return;
        }

        _isUploadingImage = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Logger.LogInformation("[CreateEvent] Reading file {FileName} ({Size} bytes)", file.Name, file.Size);
            var fileData = await ImageStorageService.ReadFileAsync(file, maxSize);
            if (fileData == null)
            {
                _uploadError = "Failed to read the selected file. Please try again.";
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
                _uploadError = uploadResult?.ErrorMessage ?? "Failed to upload image.";
                imagePreviewUrl = null;
                _uploadedImageStorageObjectId = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Image upload error for {FileName}", file.Name);
            _uploadError = $"Upload error: {ex.Message}";
            imagePreviewUrl = null;
            _uploadedImageStorageObjectId = null;
        }
        finally
        {
            _isUploadingImage = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ========== Description Dialog ==========

    private async Task OpenDescriptionDialog()
    {
        var parameters = new DialogParameters<DescriptionDialog>
        {
            { x => x.Description, createDto.Description }
        };

        var options = DialogOptionsFactory.Editor();

        await AccessibilityFocusService.SaveFocusAsync();
        var dialog = await DialogService.ShowAsync<DescriptionDialog>("", parameters, options);
        var result = await dialog.Result;
        await AccessibilityFocusService.RestoreFocusAsync();

        if (result is not null && !result.Canceled)
        {
            createDto.Description = result.Data?.ToString();
        }
    }

    // ========== Session Date/Time Handlers ==========

    private void OnSessionStartDateChanged(int index, DateTime? date)
    {
        if (date.HasValue && index >= 0 && index < sessions.Count)
        {
            sessions[index].StartTime = date.Value.Date + sessions[index].StartTime.TimeOfDay;
        }
    }

    private void OnSessionStartTimeChanged(int index, TimeSpan? time)
    {
        if (time.HasValue && index >= 0 && index < sessions.Count)
        {
            sessions[index].StartTime = sessions[index].StartTime.Date + time.Value;
        }
    }

    private void OnSessionEndDateChanged(int index, DateTime? date)
    {
        if (date.HasValue && index >= 0 && index < sessions.Count)
        {
            sessions[index].EndTime = date.Value.Date + sessions[index].EndTime.TimeOfDay;
        }
    }

    private void OnSessionEndTimeChanged(int index, TimeSpan? time)
    {
        if (time.HasValue && index >= 0 && index < sessions.Count)
        {
            sessions[index].EndTime = sessions[index].EndTime.Date + time.Value;
        }
    }

    // ========== Session Management ==========

    private void AddSession()
    {
        _sessionWorkflow.OpenForCreate(sessions, imagePreviewUrl);
    }

    private async void RemoveSession(int index)
    {
        if (index >= 0 && index < sessions.Count)
        {
            if (sessions.Count <= 1)
            {
                errorMessage = "You must have at least one session.";
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
                        errorMessage = $"Failed to delete session: {ex.Message}";
                    }
                }
            }
            else
            {
                sessions.RemoveAt(index);
            }
        }
    }

    private void HandleSessionSave(SessionEditorModel model)
    {
        _sessionWorkflow.SaveSession(sessions, model);
        StateHasChanged();
    }

    private void OnFirstSessionLanguagesChanged(IEnumerable<int> selectedLanguageIds)
    {
        if (sessions.Count > 0)
        {
            sessions[0].LanguageIds = new HashSet<int>(selectedLanguageIds);
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
                _myGroups = new List<GroupPublisherListDto>();
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

            eventTemplates = templates.Items
                .Where(t => t.IsActive && t.IsPublished)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.DisplayName)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load event templates for event type {EventTypeId}", eventTypeId);
            if (IsCurrentTemplateListRequest(requestVersion, eventTypeId))
            {
                eventTemplates = Array.Empty<EventTemplateListModel>();
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
        if (createDto.EventStatusId is null or <= 0)
        {
            createDto.EventStatusId = 1;
        }

        if (!createDto.IsRegistrationRequired.HasValue)
        {
            createDto.IsRegistrationRequired = true;
        }
    }

    // ========== Validation & Submission ==========

    private bool ValidateForm()
    {
        errorMessage = string.Empty;

        if (_publisherMode == "organization")
        {
            if (!_selectedOrganizationId.HasValue)
            {
                errorMessage = "Please select an organization.";
                return false;
            }
            if (!string.IsNullOrEmpty(_organizationRoleError))
            {
                errorMessage = _organizationRoleError;
                return false;
            }
        }

        if (_publisherMode == "group")
        {
            if (!_selectedGroupId.HasValue)
            {
                errorMessage = "Please select a group.";
                return false;
            }
            if (!string.IsNullOrEmpty(_groupRoleError))
            {
                errorMessage = _groupRoleError;
                return false;
            }
        }

        if (sessions == null || !sessions.Any())
        {
            errorMessage = "You must add at least one session.";
            return false;
        }

        return true;
    }

    private async Task HandleSubmit()
    {
        if (!ValidateForm())
        {
            return;
        }

        if (_isUploadingImage)
        {
            errorMessage = "Please wait for the image upload to complete.";
            return;
        }

        if (_isLoadingTemplatePreview)
        {
            errorMessage = "Please wait for the template preview to finish loading.";
            return;
        }

        if (isProcessing) return;
        isProcessing = true;
        errorMessage = string.Empty;

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
            createDto.IsRegistrationRequired = sessions.Any(s => s.RegistrationModeId is > 0);
            createDto.EventStatusId ??= 2; // Default: Published
            createDto.VisibilityTypeId ??= 1;
            createDto.EventFormatId ??= 1;
            createDto.Timezone = _selectedTimezone.Id;
            createDto.BackgroundColor = string.IsNullOrWhiteSpace(_bgColor) ? null : _bgColor;
            createDto.BackgroundEffect = string.IsNullOrWhiteSpace(_bgEffect) || _bgEffect == "None" ? null : _bgEffect;

            PopulateInlineSchedulingOnDto();

            var earliestStart = sessions.Min(s => DateTimeHelper.ConvertLocalToUtc(s.StartTime));
            var latestEnd = sessions.Max(s => DateTimeHelper.ConvertLocalToUtc(s.EndTime));

            createDto.FirstSessionDate = earliestStart;
            createDto.LastSessionDate = latestEnd;
            createDto.FirstSessionStartUtc = earliestStart;
            createDto.LastSessionStartUtc = latestEnd;

            Logger.LogInformation(
                "Creating event (publisherMode={Mode}, organizationId={OrgId}, groupId={GroupId})",
                _publisherMode,
                createDto.OrganizationId,
                createDto.GroupId);
            var response = await EventService.CreateEventAsync(createDto);

            if (response?.Success == true && response.Id.HasValue && response.Id != Guid.Empty)
            {
                createdEventId = response.Id.Value;
                Logger.LogInformation("Event created with ID: {EventId}", createdEventId);

                if (selectedCategoryIds.Any() || selectedTagIds.Any())
                {
                    Logger.LogWarning(
                        "Selected categories/tags are currently not persisted because the Event API does not expose event-category/event-tag assignment endpoints.");
                }

                var tenantId = Constants.TenantConstants.DefaultTenantId;

                var existingSessions = await EventService.GetSessionsByEventAsync(createdEventId);
                var defaultSession = existingSessions?.FirstOrDefault();

                if (defaultSession != null && sessions.Count > 0)
                {
                    var firstSessionModel = sessions[0];
                    firstSessionModel.Id = defaultSession.Id;

                    var updateDto = firstSessionModel.ToUpdateDto(createdEventId);
                    await EventService.UpdateSessionAsync(updateDto);
                }

                for (int i = 1; i < sessions.Count; i++)
                {
                    var sessionModel = sessions[i];
                    var createSessionDto = sessionModel.ToCreateDto(createdEventId, tenantId);
                    await EventService.CreateSessionAsync(createSessionDto);
                }

                var destination = createDto.EventStatusId == 1
                    ? $"/events/{createdEventId}/edit"
                    : $"/events/{createdEventId}";
                Navigation.NavigateTo(destination);
            }
            else
            {
                var errorMsg = response?.Message ?? "Failed to create event.";
                if (response?.Errors != null && response.Errors.Any())
                {
                    errorMsg += " Errors: " + string.Join(", ", response.Errors);
                }
                errorMessage = errorMsg;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during event creation");
            errorMessage = $"Error creating event: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }

    private async Task SaveAsDraftAsync()
    {
        createDto.EventStatusId = 1; // Draft
        await HandleSubmit();
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

    // ========== Inline Scheduling Methods ==========

    private void AddInlineDay()
    {
        if (!_newDayDate.HasValue) return;

        var localDate = DateOnly.FromDateTime(_newDayDate.Value);
        if (_inlineDays.Any(d => d.LocalDate == localDate)) return;

        _inlineDays.Add(new InlineDayModel
        {
            LocalDate = localDate,
            Label = _newDayLabel,
            SortOrder = _inlineDays.Count
        });

        _newDayDate = null;
        _newDayLabel = null;
    }

    private void AddInlineRoom()
    {
        if (string.IsNullOrWhiteSpace(_newRoomName)) return;

        _inlineRooms.Add(new InlineRoomModel
        {
            Name = _newRoomName.Trim(),
            Capacity = _newRoomCapacity,
            SortOrder = _inlineRooms.Count
        });

        _newRoomName = null;
        _newRoomCapacity = null;
    }

    private void AddInlineAgendaItem()
    {
        if (string.IsNullOrWhiteSpace(_newAgendaTitle) || !_newAgendaDayDate.HasValue
            || !_newAgendaStartTime.HasValue || !_newAgendaEndTime.HasValue) return;

        var dayDate = _newAgendaDayDate.Value;
        var startDateTime = dayDate.ToDateTime(TimeOnly.FromTimeSpan(_newAgendaStartTime.Value));
        var endDateTime = dayDate.ToDateTime(TimeOnly.FromTimeSpan(_newAgendaEndTime.Value));

        var startOffset = new DateTimeOffset(startDateTime, _selectedTimezone.GetUtcOffset(startDateTime));
        var endOffset = new DateTimeOffset(endDateTime, _selectedTimezone.GetUtcOffset(endDateTime));

        _inlineAgendaItems.Add(new InlineAgendaItemModel
        {
            Title = _newAgendaTitle.Trim(),
            StartTime = startOffset,
            EndTime = endOffset,
            RoomIndex = _newAgendaRoomIndex,
            SortOrder = _inlineAgendaItems.Count
        });

        _newAgendaTitle = null;
        _newAgendaDayDate = null;
        _newAgendaStartTime = null;
        _newAgendaEndTime = null;
        _newAgendaRoomIndex = null;
    }

    private void PopulateInlineSchedulingOnDto()
    {
        if (_inlineDays.Count > 0)
        {
            createDto.Days = _inlineDays.Select(d => new InlineEventDayDto
            {
                LocalDate = d.LocalDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Label = d.Label,
                IsPublished = true,
                SortOrder = d.SortOrder,
                AllowsDayScopeRegistration = true
            }).ToList();
        }

        if (_inlineRooms.Count > 0 && sessions.Count > 0 && sessions[0].LocationId.HasValue)
        {
            createDto.Rooms = _inlineRooms.Select(r => new InlineLocationRoomDto
            {
                LocationId = sessions[0].LocationId!.Value,
                Name = r.Name,
                Capacity = r.Capacity,
                SortOrder = r.SortOrder
            }).ToList();
        }

        if (_inlineAgendaItems.Count > 0)
        {
            createDto.AgendaItems = _inlineAgendaItems.Select(a => new InlineEventAgendaItemDto
            {
                Title = a.Title,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                RoomId = null,
                KindId = null,
                SortOrder = a.SortOrder
            }).ToList();
        }
    }

    private sealed class InlineDayModel
    {
        public DateOnly LocalDate { get; set; }
        public string? Label { get; set; }
        public int SortOrder { get; set; }
    }

    private sealed class InlineRoomModel
    {
        public string Name { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public int SortOrder { get; set; }
    }

    private sealed class InlineAgendaItemModel
    {
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int? RoomIndex { get; set; }
        public int SortOrder { get; set; }
    }
}

// ABOUTME: Code-behind for the single-page Create Event page (Luma-inspired layout).
// ABOUTME: Handles publisher selection, inline image upload, description dialog, session management, and event creation.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;
using static Explore.Blazor.Client.Pages.Events.Components.EventSessionEditor;

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
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ILogger<CreateEvent> Logger { get; set; } = null!;

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

    // Sessions
    private List<SessionEditorModel> sessions = new();
    private EventAppearanceSettings _appearance = new();

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
            AddSession();
        }
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

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            BackdropClick = true
        };

        var dialog = await DialogService.ShowAsync<DescriptionDialog>("", parameters, options);
        var result = await dialog.Result;

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
        var defaultStart = DateTime.Today.AddDays(1).AddHours(9);
        var defaultEnd = DateTime.Today.AddDays(1).AddHours(17);

        if (sessions.Any())
        {
            var last = sessions.Last();
            defaultStart = last.StartTime.AddDays(1);
            defaultEnd = last.EndTime.AddDays(1);
        }

        sessions.Add(new SessionEditorModel
        {
            StartTime = defaultStart,
            EndTime = defaultEnd,
            RegistrationModeId = sessions.FirstOrDefault()?.RegistrationModeId ?? 1
        });
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
                bool? confirm = await DialogService.ShowMessageBoxAsync(
                    "Delete Session",
                    "This session already exists. Deleting it here will remove it permanently. Continue?",
                    yesText: "Delete", cancelText: "Cancel");

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

    private void OnSessionChanged(int index, SessionEditorModel session)
    {
        if (index >= 0 && index < sessions.Count)
        {
            sessions[index] = session;
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

            await Task.WhenAll(eventTypesTask, audienceGendersTask, audienceAgesTask, eventFormatsTask, visibilityTypesTask, madhabsTask, categoriesTask, tagsTask, locationsTask, registrationModesTask, languagesTask);

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
            createDto.EventStatusId ??= 1;
            createDto.VisibilityTypeId ??= 1;
            createDto.EventFormatId ??= 1;
            createDto.BackgroundColor = _appearance.BackgroundColor;
            createDto.BackgroundEffect = _appearance.BackgroundEffect;

            var earliestStart = sessions.Min(s => DateTimeHelper.ConvertLocalToUtc(s.StartTime));
            var latestEnd = sessions.Max(s => DateTimeHelper.ConvertLocalToUtc(s.EndTime));

            createDto.FirstSessionDate = earliestStart;
            createDto.LastSessionDate = latestEnd;

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

                Navigation.NavigateTo($"/event/detail/{createdEventId}");
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
}

// ABOUTME: Code-behind for the Create Event page.
// Allows users to create events as themselves (user-reported) or as an organization they have admin rights to.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Components;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Components.Event;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;
using static Explore.Blazor.Client.Components.Event.EventSessionEditor;

namespace Explore.Blazor.Client.Pages.Event;

public partial class CreateEvent
{
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected IUserService UserService { get; set; } = null!;
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected IImageStorageService ImageStorageService { get; set; } = null!;
    [Inject] protected ICategoryService CategoryService { get; set; } = null!;
    [Inject] protected ITagService TagService { get; set; } = null!;
    [Inject] protected ILocationService LocationService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ILogger<CreateEvent> Logger { get; set; } = null!;

    // Publisher selection state
    private string _publisherMode = "personal";
    private Guid? _selectedOrganizationId;
    private ICollection<OrganizationListDto>? _myOrganizations;
    private string _organizationRoleError = string.Empty;

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
    private bool _isRetrying = false;
    private bool _dataLoaded = false;
    private int? selectedMadhabId;
    private string errorMessage = string.Empty;

    // Image upload state
    private FileUploadData? _selectedFileData;
    private string? imagePreviewUrl;
    private bool _isUploadingImage = false;
    private Guid? _uploadedImageStorageObjectId = null;
    private ImageUpload? _imageUploadComponent;
    private string? _uploadError;

    // Categories and Tags selection
    private IEnumerable<Guid> selectedCategoryIds = new HashSet<Guid>();
    private IEnumerable<Guid> selectedTagIds = new HashSet<Guid>();

    // Sessions
    private List<SessionEditorModel> sessions = new();

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

    /// <summary>
    /// Handles organization selection change. Validates user role for the selected organization.
    /// </summary>
    private void OnOrganizationSelected(Guid? orgId)
    {
        _selectedOrganizationId = orgId;
        _organizationRoleError = string.Empty;

        if (orgId.HasValue && _myOrganizations != null)
        {
            var org = _myOrganizations.FirstOrDefault(o => o.Id == orgId.Value);
            if (org?.CurrentUserRole != null)
            {
                if (!RoleHelper.CanManage(org.CurrentUserRole))
                {
                    _organizationRoleError = "You don't have the authority to perform that action. Only Creator, Co-Owner, or Admin roles can publish events.";
                }
            }
        }
    }

    /// <summary>
    /// Handles file selection with FileUploadData (bytes already in memory).
    /// This is the preferred method for reliable uploads in Blazor WASM.
    /// </summary>
    private async Task OnImageFileDataSelected(FileUploadData? fileData)
    {
        Logger.LogInformation("[CreateEvent] OnImageFileDataSelected called with fileData={HasData}", fileData != null);

        _selectedFileData = fileData;
        _uploadError = null;

        if (fileData == null)
        {
            _uploadedImageStorageObjectId = null;
            Logger.LogInformation("[CreateEvent] Image selection cleared");
            return;
        }

        _isUploadingImage = true;
        Logger.LogInformation("[CreateEvent] Setting _isUploadingImage=true, calling StateHasChanged...");
        await InvokeAsync(StateHasChanged);

        try
        {
            Logger.LogInformation("[CreateEvent] Starting upload for {FileName} ({Size} bytes)", fileData.FileName, fileData.Size);

            Logger.LogInformation("[CreateEvent] Calling ImageStorageService.UploadAndCreateRecordFromBytesAsync...");
            var uploadResult = await ImageStorageService.UploadAndCreateRecordFromBytesAsync(fileData);
            Logger.LogInformation("[CreateEvent] Upload result: Success={Success}, StorageObjectId={Id}, Error={Error}",
                uploadResult?.Success, uploadResult?.StorageObjectId, uploadResult?.ErrorMessage);

            if (uploadResult?.Success == true)
            {
                _uploadedImageStorageObjectId = uploadResult.StorageObjectId;
                _uploadError = null;
                Logger.LogInformation("[CreateEvent] Featured image uploaded successfully. StorageObjectId: {StorageObjectId}", uploadResult.StorageObjectId);
            }
            else
            {
                var errorMsg = uploadResult?.ErrorMessage ?? "Failed to upload image. Please try again.";
                Logger.LogWarning("[CreateEvent] Image upload failed: {ErrorMessage}", errorMsg);
                _uploadError = errorMsg;

                await ClearUploadState();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during image upload for {FileName}", fileData.FileName);
            _uploadError = $"Upload error: {ex.Message}";

            await ClearUploadState();
        }
        finally
        {
            _isUploadingImage = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Clears the upload state on error or cancellation.
    /// </summary>
    private async Task ClearUploadState()
    {
        try
        {
            if (_imageUploadComponent != null)
            {
                await _imageUploadComponent.RemoveImage();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error clearing image upload component");
        }

        _selectedFileData = null;
        _uploadedImageStorageObjectId = null;
        imagePreviewUrl = null;
    }

    private void SetDefaultValues()
    {
        if (eventTypes?.Any() == true && createDto.EventTypeId == 0)
        {
            createDto.EventTypeId = eventTypes.First().Id;
        }
        if (audienceGenders?.Any() == true && createDto.AudienceGenderId == 0)
        {
            createDto.AudienceGenderId = audienceGenders.First().Id;
        }
        if (audienceAges?.Any() == true && createDto.AudienceAgeId == 0)
        {
            createDto.AudienceAgeId = audienceAges.First().Id;
        }
        if (eventFormats?.Any() == true && createDto.EventFormatId == 0)
        {
            createDto.EventFormatId = eventFormats.First().Id;
        }
        if (visibilityTypes?.Any() == true && createDto.VisibilityTypeId == 0)
        {
            createDto.VisibilityTypeId = visibilityTypes.First().Id;
        }
        if (createDto.EventFormatId == 0)
        {
            createDto.IsRegistrationRequired = true;
            createDto.EventFormatId = 1; // Default: In-Person
        }
    }

    private async Task RetryLoad()
    {
        _isRetrying = true;
        _dataLoaded = false;
        StateHasChanged();

        await Task.Delay(100);
        await LoadFormData();

        _isRetrying = false;
        StateHasChanged();
    }

    private async Task LoadFormData()
    {
        if (_dataLoaded) return;

        try
        {
            isLoading = true;

            Logger.LogInformation("Loading form data for create event page");

            // Get current user ID
            if (!_currentUserId.HasValue)
            {
                try
                {
                    var currentUser = await UserService.GetCurrentUserAsync();
                    if (currentUser != null)
                    {
                        _currentUserId = currentUser.Id;
                        Logger.LogInformation("Current user ID: {UserId}", _currentUserId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error getting current user");
                }
            }

            // Load user's organizations for the publisher selector
            try
            {
                _myOrganizations = await OrganizationService.GetMyOrganizationsAsync();
                Logger.LogInformation("Loaded {Count} organizations for publisher selector", _myOrganizations?.Count ?? 0);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading user organizations");
                _myOrganizations = new List<OrganizationListDto>();
            }

            // Load dropdown data in parallel
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

    private bool ValidateForm()
    {
        errorMessage = string.Empty;

        // Validate publisher selection
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

        if (string.IsNullOrWhiteSpace(createDto.Title))
        {
            errorMessage = "Event title is required.";
            return false;
        }

        if (createDto.EventTypeId == 0)
        {
            errorMessage = "Please select an event type.";
            return false;
        }

        if (createDto.AudienceGenderId == 0)
        {
            errorMessage = "Please select target gender.";
            return false;
        }

        if (createDto.AudienceAgeId == 0)
        {
            errorMessage = "Please select target age group.";
            return false;
        }

        if (createDto.VisibilityTypeId == 0)
        {
            errorMessage = "Please select visibility type.";
            return false;
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

        // Don't allow submit while image is uploading
        if (_isUploadingImage)
        {
            errorMessage = "Please wait for the image upload to complete.";
            return;
        }

        if (isProcessing) return;
        isProcessing = true;
        errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            // Image was already uploaded when selected - use the stored ID
            Guid? featuredImageId = _uploadedImageStorageObjectId;

            if (featuredImageId.HasValue)
            {
                Logger.LogInformation("Using pre-uploaded image. StorageObjectId: {StorageObjectId}", featuredImageId);
            }

            // Set publisher context: null = personal (user-reported), Guid = organization
            createDto.OrganizationId = _publisherMode == "organization" ? _selectedOrganizationId : null;
            createDto.FeaturedImageId = featuredImageId;
            createDto.MadhabId = selectedMadhabId;

            // Calculate Event Start/End dates from Sessions
            var earliestStart = sessions.Min(s => DateTimeHelper.ConvertLocalToUtc(s.StartTime));
            var latestEnd = sessions.Max(s => DateTimeHelper.ConvertLocalToUtc(s.EndTime));

            createDto.FirstSessionDate = earliestStart;
            createDto.LastSessionDate = latestEnd;

            // Create Event
            Logger.LogInformation("Creating event record (publisherMode={Mode}, organizationId={OrgId})", _publisherMode, createDto.OrganizationId);
            var response = await EventService.CreateEventAsync(createDto);

            if (response?.Success == true && response.Id.HasValue && response.Id != Guid.Empty)
            {
                createdEventId = response.Id.Value;
                Logger.LogInformation("Event created with ID: {EventId}", createdEventId);

                // Assign Categories
                var tenantId = Constants.TenantConstants.DefaultTenantId;
                foreach (var categoryId in selectedCategoryIds)
                {
                    await CategoryService.AssignCategoryToEventAsync(new { EventId = createdEventId, CategoryId = categoryId, TenantId = tenantId });
                }

                // Assign Tags
                foreach (var tagId in selectedTagIds)
                {
                    await TagService.AssignTagToEventAsync(new { EventId = createdEventId, TagId = tagId, TenantId = tenantId });
                }

                // Handle Sessions
                var existingSessions = await EventService.GetSessionsByEventAsync(createdEventId);
                var defaultSession = existingSessions?.FirstOrDefault();

                // Process first session (Update default)
                if (defaultSession != null && sessions.Count > 0)
                {
                    var firstSessionModel = sessions[0];
                    firstSessionModel.Id = defaultSession.Id;

                    var updateDto = firstSessionModel.ToUpdateDto(createdEventId);
                    await EventService.UpdateSessionAsync(updateDto);

                    if (defaultSession.Id.HasValue)
                    {
                        await SaveSessionLanguages(firstSessionModel, defaultSession.Id.Value, tenantId);
                    }
                }

                // Process additional sessions (Create)
                for (int i = 1; i < sessions.Count; i++)
                {
                    var sessionModel = sessions[i];
                    var createSessionDto = sessionModel.ToCreateDto(createdEventId, tenantId);
                    var createResponse = await EventService.CreateSessionAsync(createSessionDto);

                    if (createResponse?.Success == true && createResponse.Id.HasValue)
                    {
                         await SaveSessionLanguages(sessionModel, createResponse.Id.Value, tenantId);
                    }
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
            StateHasChanged();
        }
    }

    private async Task SaveSessionLanguages(SessionEditorModel session, Guid sessionId, Guid tenantId)
    {
        if (session.LanguageIds.Any())
        {
            foreach (var languageId in session.LanguageIds)
            {
                try
                {
                    await EventService.AssignLanguageToSessionAsync(new { EventSessionId = sessionId, LanguageId = languageId, TenantId = tenantId });
                }
                catch { /* Ignore duplicates or if endpoint doesn't exist */ }
            }
        }
    }

    // Session management
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
                bool? confirm = await DialogService.ShowMessageBox(
                    "Delete Session",
                    "This session already exists. Deleting it here will remove it permanently. Continue?",
                    yesText: "Delete", cancelText: "Cancel");

                if (confirm == true)
                {
                    try
                    {
                        await EventService.DeleteSessionAsync(session.Id.Value);
                        sessions.RemoveAt(index);
                        StateHasChanged();
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
                StateHasChanged();
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

    private string GetPublisherDescription()
    {
        if (_publisherMode == "personal")
            return "Publishing as yourself (User Reported)";

        if (_selectedOrganizationId.HasValue && _myOrganizations != null)
        {
            var org = _myOrganizations.FirstOrDefault(o => o.Id == _selectedOrganizationId.Value);
            if (org != null) return $"Publishing for {org.FullName}";
        }

        return "Select who is publishing this event";
    }

    private static string GetRoleName(int roleId) => RoleHelper.GetRoleName(roleId);
}

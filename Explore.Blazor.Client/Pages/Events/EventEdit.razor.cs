// ABOUTME: Code-behind for the Luma-inspired Edit Event page.
// ABOUTME: Loads existing event data, pre-fills the form, handles session management, image upload, and event update.

using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Models;
using Explore.Blazor.Client.Pages.Events.Workflows;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

public partial class EventEdit
{
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected IImageStorageService ImageStorageService { get; set; } = null!;
    [Inject] protected ICategoryService CategoryService { get; set; } = null!;
    [Inject] protected ITagService TagService { get; set; } = null!;
    [Inject] protected ILocationService LocationService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ILogger<EventEdit> Logger { get; set; } = null!;

    private Guid EventId { get; set; }

    // Event data
    private EventDto? currentEvent;
    private UpdateEventDto? updateDto;

    // Form state
    private ICollection<EventTypeListDto>? eventTypes;
    private ICollection<AudienceGenderListDto>? audienceGenders;
    private ICollection<AudienceAgeListDto>? audienceAges;
    private ICollection<EventFormatListDto>? eventFormats;
    private ICollection<EventStatusListDto>? eventStatuses;
    private ICollection<VisibilityTypeListDto>? visibilityTypes;
    private ICollection<MadhabListDto>? madhabs;
    private ICollection<CategoryListDto>? allCategories;
    private ICollection<TagListDto>? allTags;
    private ICollection<LocationListDto>? locations;
    private ICollection<RegistrationModeListDto>? registrationModes;
    private ICollection<LanguageListDto>? languages;
    private bool isLoading = true;
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
    private bool _showEditTagCatPopup;
    private TagCategoryMode _editTagCatMode;
    private IReadOnlyCollection<Guid> _editTagCatInitialIds = Array.Empty<Guid>();

    // Sessions
    private List<SessionEditorModel> sessions = new();
    private readonly SessionEditorWorkflow _sessionWorkflow = new();
    private EventAppearanceSettings _appearance = new();

    // UI toggles
    private bool _showFirstSessionLocation = false;
    private bool _showTimezoneSelector = false;

    // Timezone
    private TimeZoneInfo _selectedTimezone = TimeZoneInfo.Local;
    private string _selectedTimezoneDisplay => FormatTimezoneShort(_selectedTimezone);
    private static readonly IReadOnlyList<TimeZoneInfo> _allTimezones = TimeZoneInfo.GetSystemTimeZones();
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        var eventIdStr = RouterState.GetParam("eventId");
        if (Guid.TryParse(eventIdStr, out var id))
        {
            EventId = id;
        }

        await LoadEventData();
    }

    // ========== Data Loading ==========

    private async Task LoadEventData()
    {
        try
        {
            isLoading = true;

            var eventTypesTask = AdminService.GetEventTypesAsync();
            var audienceGendersTask = AdminService.GetAudienceGendersAsync();
            var audienceAgesTask = AdminService.GetAudienceAgesAsync();
            var eventFormatsTask = AdminService.GetEventFormatsAsync();
            var eventStatusesTask = AdminService.GetEventStatusesAsync();
            var visibilityTypesTask = AdminService.GetVisibilityTypesAsync();
            var madhabsTask = AdminService.GetMadhabsAsync();
            var categoriesTask = CategoryService.GetAllCategoriesAsync();
            var tagsTask = TagService.GetAllTagsAsync();
            var locationsTask = LocationService.GetAllLocationsAsync();
            var registrationModesTask = AdminService.GetRegistrationModesAsync();
            var languagesTask = AdminService.GetLanguagesAsync();

            await Task.WhenAll(
                eventTypesTask, audienceGendersTask, audienceAgesTask,
                eventFormatsTask, eventStatusesTask, visibilityTypesTask,
                madhabsTask, categoriesTask, tagsTask, locationsTask,
                registrationModesTask, languagesTask);

            eventTypes = await eventTypesTask;
            audienceGenders = await audienceGendersTask;
            audienceAges = await audienceAgesTask;
            eventFormats = await eventFormatsTask;
            eventStatuses = await eventStatusesTask;
            visibilityTypes = await visibilityTypesTask;
            madhabs = await madhabsTask;
            allCategories = await categoriesTask;
            allTags = await tagsTask;
            locations = await locationsTask;
            registrationModes = await registrationModesTask;
            languages = await languagesTask;

            currentEvent = await EventService.GetEventByIdAsync(EventId);

            if (currentEvent != null)
            {
                PopulateFormFromEvent();

                var eventSessions = await EventService.GetSessionsByEventAsync(EventId);
                sessions = eventSessions?.Select(s => SessionEditorModel.FromDto(s)).ToList()
                           ?? new List<SessionEditorModel>();

                // Show location selector if first session has a location
                if (sessions.Count > 0 && sessions[0].LocationId.HasValue)
                {
                    _showFirstSessionLocation = true;
                }
            }
            else
            {
                errorMessage = "Event not found";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading event data for editing");
            errorMessage = $"Error loading event: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void PopulateFormFromEvent()
    {
        if (currentEvent == null) return;

        updateDto = new UpdateEventDto
        {
            Id = currentEvent.Id,
            Title = currentEvent.Title,
            Subtitle = currentEvent.Subtitle,
            Description = currentEvent.Description,
            AudienceGenderId = currentEvent.AudienceGenderId,
            AudienceAgeId = currentEvent.AudienceAgeId,
            ActorId = currentEvent.ActorId,
            Price = currentEvent.Price,
            CurrencyCode = currentEvent.CurrencyCode,
            FeaturedImageId = currentEvent.FeaturedImageId,
            IsRegistrationRequired = currentEvent.IsRegistrationRequired,
            ExternalRegistrationUrl = currentEvent.EventUrl,
            EventTypeId = currentEvent.EventTypeId,
            EventFormatId = currentEvent.EventFormatId,
            EventStatusId = currentEvent.EventStatusId,
            VisibilityTypeId = currentEvent.VisibilityTypeId,
            MadhabId = currentEvent.MadhabId,
            FirstSessionDate = currentEvent.FirstSessionDate,
            LastSessionDate = currentEvent.LastSessionDate,
            Timezone = currentEvent.Timezone,
            BackgroundColor = currentEvent.BackgroundColor,
            BackgroundImageId = currentEvent.BackgroundImageId,
            BackgroundEffect = currentEvent.BackgroundEffect
        };

        _appearance = EventAppearanceMetadataHelper.FromColumns(
            currentEvent.BackgroundColor, currentEvent.BackgroundImageUri, currentEvent.BackgroundEffect);
        imagePreviewUrl = currentEvent.FeaturedImageUri;

        if (!string.IsNullOrEmpty(currentEvent.Timezone))
        {
            try
            {
                _selectedTimezone = TimeZoneInfo.FindSystemTimeZoneById(currentEvent.Timezone);
            }
            catch
            {
                _selectedTimezone = TimeZoneInfo.Local;
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

            var uploadResult = await ImageStorageService.UploadAndCreateRecordFromBytesAsync(fileData);
            if (uploadResult?.Success == true)
            {
                _uploadedImageStorageObjectId = uploadResult.StorageObjectId;
                _uploadError = null;
            }
            else
            {
                _uploadError = uploadResult?.ErrorMessage ?? "Failed to upload image.";
                imagePreviewUrl = currentEvent?.FeaturedImageUri;
                _uploadedImageStorageObjectId = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Image upload error for {FileName}", file.Name);
            _uploadError = $"Upload error: {ex.Message}";
            imagePreviewUrl = currentEvent?.FeaturedImageUri;
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
        if (updateDto == null) return;

        var parameters = new DialogParameters<DescriptionDialog>
        {
            { x => x.Description, updateDto.Description }
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
            updateDto.Description = result.Data?.ToString();
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

    private void HandleSessionSave(SessionEditorModel model)
    {
        _sessionWorkflow.SaveSession(sessions, model);
        StateHasChanged();
    }

    // ========== Validation & Submission ==========

    private async Task HandleSubmit()
    {
        if (updateDto == null) return;

        if (_isUploadingImage)
        {
            errorMessage = "Please wait for the image upload to complete.";
            return;
        }

        if (sessions == null || !sessions.Any())
        {
            errorMessage = "You must have at least one session.";
            return;
        }

        if (isProcessing) return;
        isProcessing = true;
        errorMessage = string.Empty;

        try
        {
            if (_uploadedImageStorageObjectId.HasValue)
            {
                updateDto.FeaturedImageId = _uploadedImageStorageObjectId.Value;
            }

            updateDto.IsRegistrationRequired = sessions.Any(s => s.RegistrationModeId is > 0);
            updateDto.BackgroundColor = _appearance.BackgroundColor;
            updateDto.BackgroundEffect = _appearance.BackgroundEffect;

            var earliestStart = sessions.Min(s => DateTimeHelper.ConvertLocalToUtc(s.StartTime));
            var latestEnd = sessions.Max(s => DateTimeHelper.ConvertLocalToUtc(s.EndTime));
            updateDto.FirstSessionDate = earliestStart;
            updateDto.LastSessionDate = latestEnd;

            if (!updateDto.Id.HasValue)
            {
                errorMessage = "Event ID is missing";
                return;
            }

            var response = await EventService.UpdateEventAsync(updateDto.Id.Value, updateDto);

            if (response?.Success == true)
            {
                // Update existing sessions and create new ones
                var tenantId = Constants.TenantConstants.DefaultTenantId;

                foreach (var session in sessions)
                {
                    if (session.Id.HasValue && session.Id != Guid.Empty)
                    {
                        var sessionUpdateDto = session.ToUpdateDto(EventId);
                        await EventService.UpdateSessionAsync(sessionUpdateDto);
                    }
                    else
                    {
                        var createSessionDto = session.ToCreateDto(EventId, tenantId);
                        await EventService.CreateSessionAsync(createSessionDto);
                    }
                }

                Navigation.NavigateTo($"/events/{EventId}");
            }
            else
            {
                var errorMsg = response?.Message ?? "Failed to update event.";
                if (response?.Errors != null && response.Errors.Any())
                {
                    errorMsg += " Errors: " + string.Join(", ", response.Errors);
                }
                errorMessage = errorMsg;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during event update");
            errorMessage = $"Error updating event: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }

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

    private void HandleEditTagCatSaved(IReadOnlyCollection<Guid> newIds)
    {
        if (_editTagCatMode == TagCategoryMode.Tags)
            selectedTagIds = new HashSet<Guid>(newIds);
        else
            selectedCategoryIds = new HashSet<Guid>(newIds);

        StateHasChanged();
    }
}

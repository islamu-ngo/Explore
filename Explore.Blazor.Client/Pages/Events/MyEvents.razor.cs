using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Events.Components;
using Explore.Blazor.Client.Pages.Events.Dialogs;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events;

public partial class MyEvents : ComponentBase
{
    [Inject] private IEventService EventService { get; set; } = default!;
    [Inject] private IOrganizationService OrganizationService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILogger<MyEvents> Logger { get; set; } = default!;
    [Inject] private IAccessibilityFocusService AccessibilityFocusService { get; set; } = default!;

    private ICollection<EventListDto> _events = new List<EventListDto>();
    private ICollection<OrganizationListDto> _myOrganizations = new List<OrganizationListDto>();

    private bool _isLoading = true;
    private string? _errorMessage;
    private string _searchString = string.Empty;
    private string _selectedCategory = string.Empty;
    private Guid _selectedOrganizationId = Guid.Empty;

    private List<string> UniqueEventTypes => _events
        .Select(e => e.EventTypeFullName)
        .Where(t => !string.IsNullOrEmpty(t))
        .Distinct()
        .OrderBy(t => t)
        .ToList()!;

    private List<EventListDto> AllFilteredEvents => _events
        .Where(e => string.IsNullOrEmpty(_searchString) ||
                    e.Title.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                    (e.Description?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(e => string.IsNullOrEmpty(_selectedCategory) ||
                    (e.EventTypeFullName?.Contains(_selectedCategory, StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(e => _selectedOrganizationId == Guid.Empty ||
                    e.ActorId == _selectedOrganizationId)
        .ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var eventsTask = EventService.GetMyEventsAsync();
            var organizationsTask = OrganizationService.GetMyOrganizationsAsync();
            await Task.WhenAll(eventsTask, organizationsTask);
            _events = await eventsTask ?? new List<EventListDto>();
            _myOrganizations = await organizationsTask ?? new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[MyEvents] Error loading data");
            _errorMessage = "Unable to load your events. Please try again.";
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task RetryLoadAsync()
    {
        _errorMessage = null;
        await LoadDataAsync();
    }

    private bool CanEditEvent(EventListDto evt)
    {
        var org = _myOrganizations.FirstOrDefault(o => o.Id == evt.ActorId);
        if (org == null) return false;
        return RoleHelper.CanManage(org.CurrentUserRole);
    }

    private void EditEvent(EventListDto evt)
    {
        if (CanEditEvent(evt))
        {
            Navigation.NavigateTo($"/events/{evt.Id}/edit");
        }
        else
        {
            Snackbar.Add("You do not have permission to edit this event.", Severity.Error);
        }
    }

    private void ViewEventDetails(EventListDto evt)
    {
        Navigation.NavigateTo($"/events/{evt.Id}");
    }

    private async Task ViewRegistrations(EventListDto evt)
    {
        if (!evt.Id.HasValue) return;
        var sessions = await EventService.GetSessionsByEventAsync(evt.Id.Value);
        if (sessions == null || !sessions.Any())
        {
            Snackbar.Add("No sessions available for this event yet.", Severity.Warning);
            return;
        }

        await AccessibilityFocusService.SaveFocusAsync();
        var sessionParameters = new DialogParameters { ["EventId"] = evt.Id };
        var sessionDialog = await SelectSessionDialog.ShowAsync(DialogService, $"Select a session for {evt.Title}", sessionParameters);
        var sessionResult = await sessionDialog.Result;

        if (sessionResult is { Canceled: false, Data: Guid selectedSessionId })
        {
            var parameters = new DialogParameters { ["EventSessionId"] = selectedSessionId };
            var options = DialogOptionsFactory.Editor();
            var dialog = await RegistrationManagerDialog.ShowAsync(DialogService, "Registrations for session", parameters, options);
            await dialog.Result;
        }
        await AccessibilityFocusService.RestoreFocusAsync();
    }

    private async Task DeleteEvent(EventListDto evt)
    {
        if (!CanEditEvent(evt))
        {
            Snackbar.Add("You do not have permission to delete this event.", Severity.Error);
            return;
        }

        await AccessibilityFocusService.SaveFocusAsync();
        var result = await DialogService.ShowMessageBoxAsync(
            "Delete Event",
            $"Are you sure you want to delete '{evt.Title}'? This action cannot be undone.",
            yesText: "Delete", cancelText: "Cancel",
            options: DialogOptionsFactory.Small());
        await AccessibilityFocusService.RestoreFocusAsync();

        if (result == true && evt.Id.HasValue)
        {
            var success = await EventService.DeleteEventAsync(evt.Id.Value);
            if (success)
            {
                Snackbar.Add($"Event '{evt.Title}' has been deleted.", Severity.Success);
                (_events as List<EventListDto>)?.Remove(evt);
            }
            else
            {
                Snackbar.Add("Failed to delete event. You may not have permission.", Severity.Error);
            }
        }
    }

    private bool IsDraftEvent(EventListDto evt) => evt.EventStatusId == 1;

    private async Task PublishEvent(EventListDto evt)
    {
        if (!CanEditEvent(evt))
        {
            Snackbar.Add("You do not have permission to publish this event.", Severity.Error);
            return;
        }

        await AccessibilityFocusService.SaveFocusAsync();
        var result = await DialogService.ShowMessageBoxAsync(
            "Publish Event",
            $"Are you sure you want to publish '{evt.Title}'? It will become visible to the public.",
            yesText: "Publish", cancelText: "Cancel",
            options: DialogOptionsFactory.Small());
        await AccessibilityFocusService.RestoreFocusAsync();

        if (result == true && evt.Id.HasValue)
        {
            var success = await EventService.UpdateEventStatusAsync(evt.Id.Value, 2); // Published = 2
            if (success)
            {
                evt.EventStatusId = 2;
                evt.EventStatusFullName = "Published";
                Snackbar.Add($"Event '{evt.Title}' has been published.", Severity.Success);
                StateHasChanged();
            }
            else
            {
                Snackbar.Add("Failed to publish event.", Severity.Error);
            }
        }
    }

    private void OnSearch(string value) => _searchString = value;
    private void OnCategoryChanged(string value) => _selectedCategory = value;
    private void OnOrganizationChanged(Guid? organizationId) => _selectedOrganizationId = organizationId ?? Guid.Empty;
    private void NavigateToCreateEvent() => Navigation.NavigateTo("/events/create");
    private void NavigateToCreateOrganization() => Navigation.NavigateTo("/organizations/create");

    private static string GetEventColorCode(EventListDto evt) =>
        EventColorHelper.GetColorByTypeName(evt.EventTypeFullName);

    private static string GetEventImageUrl(EventListDto evt)
    {
        if (!string.IsNullOrEmpty(evt.FeaturedImageUri)) return evt.FeaturedImageUri;
        var encodedTitle = Uri.EscapeDataString(evt.Title.Length > 30 ? evt.Title.Substring(0, 30) + "..." : evt.Title);
        var color = GetEventColorCode(evt);
        return $"https://placehold.co/600x400/{color}/ffffff?text={encodedTitle}";
    }

    private string GetSelectedOrganizationName()
    {
        if (_selectedOrganizationId == Guid.Empty) return "All Organizations";
        return _myOrganizations.FirstOrDefault(o => o.Id == _selectedOrganizationId)?.FullName ?? "All Organizations";
    }

    private static string GetActorInitials(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return "?";
        var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2) return $"{words[0][0]}{words[1][0]}".ToUpperInvariant();
        return displayName.Length >= 2 ? displayName.Substring(0, 2).ToUpperInvariant() : displayName.ToUpperInvariant();
    }
}

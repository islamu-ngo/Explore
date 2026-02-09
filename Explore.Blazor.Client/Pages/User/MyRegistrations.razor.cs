using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.User;

public partial class MyRegistrations
{
    [Inject] protected IEventService EventService { get; set; } = null!;
    [Inject] protected IUserService UserService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    private List<MyRegistrationViewModel> _registrations = new();
    private bool _loading = true;
    private string _searchString = "";

    private IEnumerable<MyRegistrationViewModel> FilteredRegistrations =>
        string.IsNullOrWhiteSpace(_searchString)
            ? _registrations
            : _registrations.Where(r => r.EventTitle?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true);

    protected override async Task OnInitializedAsync()
    {
        await LoadRegistrations();
    }

    private async Task LoadRegistrations()
    {
        _loading = true;
        try
        {
            var user = await UserService.GetCurrentUserAsync();
            if (user != null && user.Id.HasValue)
            {
                var registrations = await EventService.GetRegistrationsByUserAsync(user.Id.Value);

                var tasks = registrations.Select(async r =>
                {
                    var vm = new MyRegistrationViewModel
                    {
                        RegistrationId = r.Id ?? Guid.Empty,
                        EventSessionId = r.EventSessionId ?? Guid.Empty,
                        Status = r.ApprovalStatusFullName,
                        StatusId = r.ApprovalStatusId
                    };

                    if (r.EventSessionId.HasValue)
                    {
                        var session = await EventService.GetSessionByIdAsync(r.EventSessionId.Value);
                        if (session != null)
                        {
                            vm.EventId = session.EventId ?? Guid.Empty;
                            vm.EventTitle = session.EventTitle;
                            vm.StartTime = session.StartTime;

                            if (session.EventId.HasValue)
                            {
                                var evt = await EventService.GetEventByIdAsync(session.EventId.Value);
                                if (evt != null)
                                {
                                    vm.FeaturedImageUri = evt.FeaturedImageUri;
                                    vm.EventTitle = evt.Title;
                                }
                            }
                        }
                    }

                    return vm;
                });

                _registrations = (await Task.WhenAll(tasks)).ToList();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading registrations: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CancelRegistration(Guid registrationId)
    {
        var result = await DialogService.ShowMessageBox(
            "Cancel Registration",
            "Are you sure you want to cancel your registration for this event?",
            yesText: "Yes, Cancel", cancelText: "No");

        if (result == true)
        {
            try
            {
                var success = await EventService.CancelEventRegistrationAsync(registrationId);
                if (success)
                {
                    Snackbar.Add("Registration cancelled successfully.", Severity.Success);
                    await LoadRegistrations();
                }
                else
                {
                    Snackbar.Add("Failed to cancel registration.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error cancelling registration: {ex.Message}", Severity.Error);
            }
        }
    }

    public class MyRegistrationViewModel
    {
        public Guid RegistrationId { get; set; }
        public Guid EventId { get; set; }
        public Guid EventSessionId { get; set; }
        public string? EventTitle { get; set; }
        public string? FeaturedImageUri { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public string? Status { get; set; }
        public int? StatusId { get; set; }
    }
}

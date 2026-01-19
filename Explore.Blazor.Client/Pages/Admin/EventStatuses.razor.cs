using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class EventStatuses
{
    [Inject] protected IEventStatusService EventStatusService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    private MudTable<EventStatusListDto> _table = null!;
    private IEnumerable<EventStatusListDto> _eventStatuses = new List<EventStatusListDto>();

    protected override async Task OnInitializedAsync()
    {
        _eventStatuses = await EventStatusService.GetEventStatusesAsync();
    }

    private async Task OpenDialog(int? id)
    {
        // For now, this is a placeholder. I will implement the dialog later.
        Snackbar.Add("Create/Edit functionality not yet implemented.", Severity.Info);
        await Task.CompletedTask;
    }

    private async Task DeleteEventStatus(int id)
    {
        // For now, this is a placeholder. I will implement the delete functionality later.
        Snackbar.Add("Delete functionality not yet implemented.", Severity.Info);
        await Task.CompletedTask;
    }
}

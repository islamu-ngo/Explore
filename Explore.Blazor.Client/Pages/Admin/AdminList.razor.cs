using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class AdminList
{
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected IUserService UserService { get; set; } = null!;
    [Inject] protected ILogger<AdminList> Logger { get; set; } = null!;

    // Status mapping from API to local enum
    private enum RequestStatus { Pending = 1, Approved = 2, Rejected = 3 }

    private ICollection<OrganizationListDto> _organizationRequests = new List<OrganizationListDto>();
    private string _search = string.Empty;
    private int _activeTab = 0;
    private bool _isLoading = true;
    private string? _errorMessage;
    private HashSet<RequestStatus> _selectedStatuses = new();
    private enum SortOption { OldestFirst, NewestFirst, Name, Status }
    private SortOption _sort = SortOption.OldestFirst;

    private int PendingCount => _organizationRequests.Count(r => r.ApprovalStatusId == (int)RequestStatus.Pending);
    private int ApprovedCount => _organizationRequests.Count(r => r.ApprovalStatusId == (int)RequestStatus.Approved);
    private int RejectedCount => _organizationRequests.Count(r => r.ApprovalStatusId == (int)RequestStatus.Rejected);

    protected override async Task OnInitializedAsync()
    {
        await LoadOrganizationRequests();
    }

    private async Task LoadOrganizationRequests()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            Logger.LogDebug("Loading organizations from API");
            var tempOrgs = await AdminService.GetOrganizationRequestsAsync();

            if (tempOrgs != null)
            {
                _organizationRequests = tempOrgs;
                Logger.LogDebug("Loaded {OrganizationCount} organizations", _organizationRequests.Count);
            }
            else
            {
                _organizationRequests = new List<OrganizationListDto>();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load organizations: {ex.Message}";
            Logger.LogError(ex, "Failed to load organizations");
            _organizationRequests = new List<OrganizationListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private bool IsStatusSelected(RequestStatus s) => _selectedStatuses.Contains(s);

    private void ToggleStatus(RequestStatus s)
    {
        if (_selectedStatuses.Contains(s)) _selectedStatuses.Remove(s); else _selectedStatuses.Add(s);
        StateHasChanged();
    }

    private void ClearFilters()
    {
        _selectedStatuses.Clear();
        _search = string.Empty;
        _sort = SortOption.OldestFirst;
        StateHasChanged();
    }

    private IEnumerable<OrganizationListDto> GetFilteredAndSorted(RequestStatus? status)
    {
        IEnumerable<OrganizationListDto> q = _organizationRequests;

        if (status.HasValue)
            q = q.Where(r => r.ApprovalStatusId == (int)status.Value);

        // apply chip filters if any selected
        if (_selectedStatuses.Count > 0)
        {
            var allowed = _selectedStatuses.Select(s => (int)s).ToHashSet();
            q = q.Where(r => r.ApprovalStatusId.HasValue && allowed.Contains(r.ApprovalStatusId.Value));
        }

        if (!string.IsNullOrWhiteSpace(_search))
        {
            var s = _search.Trim().ToLowerInvariant();
            q = q.Where(r =>
                (r.FullName?.ToLowerInvariant().Contains(s) ?? false) ||
                (r.Id.ToString().ToLowerInvariant().Contains(s)) ||
                (r.Email?.ToLowerInvariant().Contains(s) ?? false) ||
                (r.City?.ToLowerInvariant().Contains(s) ?? false) ||
                (r.Country?.ToLowerInvariant().Contains(s) ?? false));
        }

        // sorting
        q = _sort switch
        {
            SortOption.NewestFirst => q.OrderByDescending(r => r.CreatedAt),
            SortOption.Name => q.OrderBy(r => r.FullName),
            SortOption.Status => q.OrderBy(r => r.ApprovalStatusId),
            _ => q.OrderBy(r => r.CreatedAt)
        };

        return q;
    }

    private async Task Approve(OrganizationListDto req)
    {
        if (!req.Id.HasValue) return;

        bool? ok = await DialogService.ShowMessageBox(
            "Confirm approval",
            $"Approve {req.FullName}?",
            yesText: "Approve", cancelText: "Cancel");

        if (ok == true)
        {
            var success = await AdminService.ApproveOrganizationAsync(req.Id.Value);
            if (success)
            {
                await LoadOrganizationRequests(); // Reload data
            }
            else
            {
                _errorMessage = $"Failed to approve {req.FullName}";
            }
        }
    }

    private async Task Reject(OrganizationListDto req)
    {
        if (!req.Id.HasValue) return;

        bool? ok = await DialogService.ShowMessageBox(
            "Confirm rejection",
            $"Reject {req.FullName}?",
            yesText: "Reject", cancelText: "Cancel");

        if (ok == true)
        {
            var success = await AdminService.RejectOrganizationAsync(req.Id.Value);
            if (success)
            {
                await LoadOrganizationRequests(); // Reload data
            }
            else
            {
                _errorMessage = $"Failed to reject {req.FullName}";
            }
        }
    }

    private async Task RevertToPending(OrganizationListDto req)
    {
        if (!req.Id.HasValue) return;
        var success = await AdminService.RevertToPendingAsync(req.Id.Value);
        if (success)
        {
            await LoadOrganizationRequests(); // Reload data
        }
        else
        {
            _errorMessage = $"Failed to revert {req.FullName} to pending";
        }
    }

    private void ViewDetails(OrganizationListDto req)
    {
        if (req.Id.HasValue)
        {
            Nav.NavigateTo($"/admin/organization/{req.Id.Value}");
        }
    }
}

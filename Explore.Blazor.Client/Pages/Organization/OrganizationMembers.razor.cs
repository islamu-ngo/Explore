using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Blazouter.Services;

namespace Explore.Blazor.Client.Pages.Organization;

public partial class OrganizationMembers
{
    [Inject] protected IOrganizationMemberService MemberService { get; set; } = null!;
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;

    private Guid Id { get; set; }
    private List<OrganizationMemberDto> Members = new();
    private bool _loading = true;
    private string? currentUserId;
    private int? currentUserRole;
    private string? _errorMessage;

    private string _searchString = "";
    private int? _roleFilter;

    private IEnumerable<OrganizationMemberDto> FilteredMembers =>
        Members
            .Where(x => string.IsNullOrWhiteSpace(_searchString) ||
                        (x.UserFullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (x.UserEmail?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false))
            .Where(x => _roleFilter == null || x.OrganizationRoleId == _roleFilter);

    protected override async Task OnInitializedAsync()
    {
        var idStr = RouterState.GetParam("id");
        if (Guid.TryParse(idStr, out var id))
        {
            Id = id;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        currentUserId = user.FindFirst("sub")?.Value
            ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? user.FindFirst("oid")?.Value;

        await LoadMembers();
    }

    private void DetermineCurrentUserRole()
    {
        if (currentUserId != null)
        {
            var me = Members.FirstOrDefault(m => m.UserId.ToString().Equals(currentUserId, StringComparison.OrdinalIgnoreCase));
            if (me != null)
            {
                currentUserRole = me.OrganizationRoleId;
            }
        }
    }

    private async Task LoadMembers()
    {
        _loading = true;
        _errorMessage = null;
        try
        {
            Members = (await MemberService.GetMembersAsync(Id))?.ToList() ?? new List<OrganizationMemberDto>();
            DetermineCurrentUserRole();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading members: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private Color GetRoleColor(int role)
    {
        return RoleHelper.GetRoleColor(role);
    }

    private string GetRoleName(int role)
    {
        return RoleHelper.GetRoleName(role);
    }

    private async Task OpenInviteDialog()
    {
        var parameters = new DialogParameters { ["OrganizationId"] = Id };
        var dialog = await DialogService.ShowAsync<InviteMemberDialog>("Invite Member", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadMembers();
        }
    }

    private async Task OpenEditRoleDialog(OrganizationMemberDto member)
    {
        var parameters = new DialogParameters { ["Member"] = member, ["OrganizationId"] = Id };
        var dialog = await DialogService.ShowAsync<EditMemberRoleDialog>("Edit Role", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadMembers();
        }
    }

    private async Task RemoveMember(OrganizationMemberDto member)
    {
        bool? result = await DialogService.ShowMessageBox(
            "Remove Member",
            $"Are you sure you want to remove {member.UserEmail} from the organization?",
            yesText: "Remove", cancelText: "Cancel");

        if (result == true)
        {
            try
            {
                if (!member.Id.HasValue)
                {
                    _errorMessage = "Member ID is missing";
                    return;
                }
                await MemberService.DeleteMemberAsync(member.Id.Value);
                await LoadMembers();
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error removing member: {ex.Message}";
            }
        }
    }
}
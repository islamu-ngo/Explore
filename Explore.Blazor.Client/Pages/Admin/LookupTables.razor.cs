using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class LookupTables
{
    [Inject] protected IAdminService AdminService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    private bool isLoading = true;

    private ICollection<EventTypeListDto>? eventTypes;
    private ICollection<EventFormatListDto>? eventFormats;
    private ICollection<EventStatusListDto>? eventStatuses;
    private ICollection<VisibilityTypeListDto>? visibilityTypes;
    private ICollection<RegistrationModeListDto>? registrationModes;
    private ICollection<AudienceGenderListDto>? audienceGenders;
    private ICollection<AudienceAgeListDto>? audienceAges;
    private ICollection<MadhabListDto>? madhabs;
    private ICollection<LanguageListDto>? languages;
    private ICollection<OrganizationRoleListDto>? organizationRoles;
    private ICollection<OrganizationPositionListDto>? organizationPositions;
    private ICollection<StatusTypeListDto>? approvalStatuses;
    private ICollection<ActorTypeListDto>? actorTypes;
    private ICollection<FileTypeListDto>? fileTypes;
    private ICollection<DidCustodyTypeListDto>? didCustodyTypes;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            isLoading = true;

            var tasks = new List<Task>
            {
                LoadEventTypesAsync(),
                LoadEventFormatsAsync(),
                LoadEventStatusesAsync(),
                LoadVisibilityTypesAsync(),
                LoadRegistrationModesAsync(),
                LoadAudienceGendersAsync(),
                LoadAudienceAgesAsync(),
                LoadMadhabsAsync(),
                LoadLanguagesAsync(),
                LoadOrganizationRolesAsync(),
                LoadOrganizationPositionsAsync(),
                LoadApprovalStatusesAsync(),
                LoadActorTypesAsync(),
                LoadFileTypesAsync(),
                LoadDidCustodyTypesAsync()
            };

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading lookup tables: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadEventTypesAsync() => eventTypes = await AdminService.GetEventTypesAsync();
    private async Task LoadEventFormatsAsync() => eventFormats = await AdminService.GetEventFormatsAsync();
    private async Task LoadEventStatusesAsync() => eventStatuses = await AdminService.GetEventStatusesAsync();
    private async Task LoadVisibilityTypesAsync() => visibilityTypes = await AdminService.GetVisibilityTypesAsync();
    private async Task LoadRegistrationModesAsync() => registrationModes = await AdminService.GetRegistrationModesAsync();
    private async Task LoadAudienceGendersAsync() => audienceGenders = await AdminService.GetAudienceGendersAsync();
    private async Task LoadAudienceAgesAsync() => audienceAges = await AdminService.GetAudienceAgesAsync();
    private async Task LoadMadhabsAsync() => madhabs = await AdminService.GetMadhabsAsync();
    private async Task LoadLanguagesAsync() => languages = await AdminService.GetLanguagesAsync();
    private async Task LoadOrganizationRolesAsync() => organizationRoles = await AdminService.GetOrganizationRolesAsync();
    private async Task LoadOrganizationPositionsAsync() => organizationPositions = await AdminService.GetOrganizationPositionsAsync();
    private async Task LoadApprovalStatusesAsync() => approvalStatuses = await AdminService.GetApprovalStatusesAsync();
    private async Task LoadActorTypesAsync() => actorTypes = await AdminService.GetActorTypesAsync();
    private async Task LoadFileTypesAsync() => fileTypes = await AdminService.GetFileTypesAsync();
    private async Task LoadDidCustodyTypesAsync() => didCustodyTypes = await AdminService.GetDidCustodyTypesAsync();
}

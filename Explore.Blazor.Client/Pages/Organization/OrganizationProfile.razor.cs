using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Organization;

public partial class OrganizationProfile
{
    [Inject] protected IOrganizationService OrganizationService { get; set; } = null!;
    [Inject] protected IOrganizationReviewService OrganizationReviewService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected RouterStateService RouterState { get; set; } = null!;
    [Inject] protected ILogger<OrganizationProfile> Logger { get; set; } = null!;

    private Guid Id { get; set; }

    private OrganizationDto? _organization;
    private bool _isLoading = true;
    private List<OrganizationReviewDto> _reviews = new();
    private OrganizationAppearanceSettings _appearance = new();
    private string _bannerStyle = OrganizationAppearanceMetadataHelper.BuildBannerStyle(new OrganizationAppearanceSettings(), "#1f6feb");

    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        // Get route parameter from Blazouter
        var idStr = RouterState.GetParam("id");
        if (Guid.TryParse(idStr, out var id))
        {
            Id = id;
        }

        _isLoading = true;
        _errorMessage = null;

        try
        {
            // Load organization and its reviews
            _organization = await OrganizationService.GetOrganizationByIdAsync(Id);
            _reviews = (await OrganizationReviewService.GetReviewsByOrganizationId(Id)).ToList();
            _appearance = OrganizationAppearanceMetadataHelper.Parse(_organization?.MetadataJson);
            _bannerStyle = OrganizationAppearanceMetadataHelper.BuildBannerStyle(_appearance, "#1f6feb");
            Logger.LogDebug("Loaded organization {OrganizationId} with {ReviewCount} reviews", Id, _reviews.Count);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load organization data: {ex.Message}";
            Logger.LogError(ex, "Error loading organization {OrganizationId}", Id);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ShowAllReviews()
    {
        Navigation.NavigateTo($"/organization/reviews/{Id}");
    }

    private string GetOrganizationPlaceholder()
    {
        if (_organization == null)
            return ImageHelper.GetOrganizationPlaceholder(null, "ORG");

        return ImageHelper.GetOrganizationPlaceholder(null, _organization.FullName);
    }
}

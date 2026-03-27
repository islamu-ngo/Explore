using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.User;

public partial class MyReviews
{
    [Inject] protected IUserService UserService { get; set; } = null!;
    [Inject] protected IOrganizationReviewService OrganizationReviewService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] private IAccessibilityFocusService AccessibilityFocusService { get; set; } = default!;

    private List<OrganizationReviewDto> _reviews = new();
    private bool _loading = true;
    private string _searchString = "";
    private string? _errorMessage;

    private IEnumerable<OrganizationReviewDto> FilteredReviews =>
        string.IsNullOrWhiteSpace(_searchString)
            ? _reviews
            : _reviews.Where(r => r.OrganizationFullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true
                               || r.Comment?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true);

    protected override async Task OnInitializedAsync()
    {
        await LoadReviews();
    }

    private async Task LoadReviews()
    {
        _loading = true;
        _errorMessage = null;
        try
        {
            var user = await UserService.GetCurrentUserAsync();
            if (user != null)
            {
                if (user.Id.HasValue)
                {
                    _reviews = (await OrganizationReviewService.GetReviewsByUserId(user.Id.Value)).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading reviews: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task DeleteReview(OrganizationReviewDto review)
    {
        await AccessibilityFocusService.SaveFocusAsync();
        bool? result = await DialogService.ShowMessageBoxAsync(
            "Delete Review",
            "Are you sure you want to delete this review?",
            yesText: "Delete", cancelText: "Cancel");
        await AccessibilityFocusService.RestoreFocusAsync();

        if (result == true)
        {
            try
            {
                // await OrganizationReviewService.DeleteOrganizationReviewAsync(review.Id);
                // Implementation pending in service
                await LoadReviews();
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error deleting review: {ex.Message}";
            }
        }
    }
}

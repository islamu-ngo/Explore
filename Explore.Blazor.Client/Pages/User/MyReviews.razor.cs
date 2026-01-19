using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Clients;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.User;

public partial class MyReviews
{
    [Inject] protected IUserService UserService { get; set; } = null!;
    [Inject] protected IOrganizationReviewService OrganizationReviewService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;

    private List<OrganizationReviewDto> _reviews = new();
    private bool _loading = true;
    private string _searchString = "";

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
        try
        {
            var user = await UserService.GetCurrentUserAsync();
            if (user != null)
            {
                _reviews = (await OrganizationReviewService.GetReviewsByUserId(user.Id)).ToList();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading reviews: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task DeleteReview(OrganizationReviewDto review)
    {
        var parameters = new DialogParameters
        {
            { "ContentText", "Are you sure you want to delete this review?" },
            { "ButtonText", "Delete" },
            { "Color", Color.Error }
        };

        // Note: SimpleDialog is not a standard MudBlazor component, using generic DialogService.ShowMessageBox instead or standard approach
        bool? result = await DialogService.ShowMessageBox(
            "Delete Review",
            "Are you sure you want to delete this review?",
            yesText: "Delete", cancelText: "Cancel");

        if (result == true)
        {
            try
            {
                // await OrganizationReviewService.DeleteOrganizationReviewAsync(review.Id);
                Snackbar.Add("Review deletion not implemented yet", Severity.Info);
                await LoadReviews();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error deleting review: {ex.Message}", Severity.Error);
            }
        }
    }
}

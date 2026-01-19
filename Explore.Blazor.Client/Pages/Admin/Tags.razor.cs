using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Admin;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class Tags
{
    [Inject] protected ITagService TagService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<TagListDto> _tags = new List<TagListDto>();
    private string _searchString = string.Empty;
    private bool _isLoading = true;

    private IEnumerable<TagListDto> FilteredTags => string.IsNullOrWhiteSpace(_searchString)
        ? _tags
        : _tags.Where(t =>
            (t.FullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (t.MasterCode?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false));

    protected override async Task OnInitializedAsync()
    {
        await LoadTags();
    }

    private async Task LoadTags()
    {
        _isLoading = true;
        try
        {
            _tags = await TagService.GetTags();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load tags: {ex.Message}", Severity.Error);
            _tags = new List<TagListDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OpenCreateDialog()
    {
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<CreateTagDialog>("Create Tag", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: CreateTagDto createDto })
        {
            var response = await TagService.CreateTagAsync(createDto);
            if (response?.Success == true)
            {
                Snackbar.Add($"Tag '{createDto.FullName}' created successfully", Severity.Success);
                await LoadTags();
            }
            else
            {
                var errors = response?.Errors != null ? string.Join(", ", response.Errors) : "Unknown error";
                Snackbar.Add($"Failed to create tag: {errors}", Severity.Error);
            }
        }
    }

    private async Task OpenEditDialog(TagListDto tag)
    {
        // First, get the full tag details
        var tagDetails = await TagService.GetTagByIdAsync(tag.Id);
        if (tagDetails == null)
        {
            Snackbar.Add("Failed to load tag details", Severity.Error);
            return;
        }

        var parameters = new DialogParameters<EditTagDialog>
        {
            { x => x.ExistingTag, tagDetails }
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<EditTagDialog>("Edit Tag", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: UpdateTagDto updateDto })
        {
            var response = await TagService.UpdateTagAsync(updateDto.Id, updateDto);
            if (response?.Success == true)
            {
                Snackbar.Add($"Tag '{updateDto.FullName}' updated successfully", Severity.Success);
                await LoadTags();
            }
            else
            {
                var errors = response?.Errors != null ? string.Join(", ", response.Errors) : "Unknown error";
                Snackbar.Add($"Failed to update tag: {errors}", Severity.Error);
            }
        }
    }

    private async Task DeleteTag(TagListDto tag)
    {
        bool? confirm = await DialogService.ShowMessageBox(
            "Confirm Delete",
            $"Are you sure you want to delete the tag '{tag.FullName}'? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirm == true)
        {
            var success = await TagService.DeleteTagAsync(tag.Id);
            if (success)
            {
                Snackbar.Add($"Tag '{tag.FullName}' deleted successfully", Severity.Success);
                await LoadTags();
            }
            else
            {
                Snackbar.Add($"Failed to delete tag '{tag.FullName}'", Severity.Error);
            }
        }
    }
}

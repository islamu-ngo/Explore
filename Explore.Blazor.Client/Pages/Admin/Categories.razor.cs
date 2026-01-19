using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Admin;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin;

public partial class Categories
{
    [Inject] protected ICategoryService CategoryService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<CategoryListDto> _categories = new List<CategoryListDto>();
    private string _searchString = string.Empty;
    private bool _isLoading = true;

    private IEnumerable<CategoryListDto> FilteredCategories => string.IsNullOrWhiteSpace(_searchString)
        ? _categories
        : _categories.Where(c =>
            (c.FullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.MasterCode?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.ParentFullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false));

    protected override async Task OnInitializedAsync()
    {
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        _isLoading = true;
        try
        {
            _categories = await CategoryService.GetCategories();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load categories: {ex.Message}", Severity.Error);
            _categories = new List<CategoryListDto>();
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

        var dialog = await DialogService.ShowAsync<CreateCategoryDialog>("Create Category", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: CreateCategoryDto createDto })
        {
            var response = await CategoryService.CreateCategoryAsync(createDto);
            if (response?.Success == true)
            {
                Snackbar.Add($"Category '{createDto.FullName}' created successfully", Severity.Success);
                await LoadCategories();
            }
            else
            {
                var errors = response?.Errors != null ? string.Join(", ", response.Errors) : "Unknown error";
                Snackbar.Add($"Failed to create category: {errors}", Severity.Error);
            }
        }
    }

    private async Task OpenEditDialog(CategoryListDto category)
    {
        var parameters = new DialogParameters<EditCategoryDialog>
        {
            { x => x.ExistingCategory, category }
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<EditCategoryDialog>("Edit Category", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: UpdateCategoryDto updateDto })
        {
            var response = await CategoryService.UpdateCategoryAsync(updateDto.Id, updateDto);
            if (response?.Success == true)
            {
                Snackbar.Add($"Category '{updateDto.FullName}' updated successfully", Severity.Success);
                await LoadCategories();
            }
            else
            {
                var errors = response?.Errors != null ? string.Join(", ", response.Errors) : "Unknown error";
                Snackbar.Add($"Failed to update category: {errors}", Severity.Error);
            }
        }
    }

    private async Task DeleteCategory(CategoryListDto category)
    {
        bool? confirm = await DialogService.ShowMessageBox(
            "Confirm Delete",
            $"Are you sure you want to delete the category '{category.FullName}'? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirm == true)
        {
            var success = await CategoryService.DeleteCategoryAsync(category.Id);
            if (success)
            {
                Snackbar.Add($"Category '{category.FullName}' deleted successfully", Severity.Success);
                await LoadCategories();
            }
            else
            {
                Snackbar.Add($"Failed to delete category '{category.FullName}'", Severity.Error);
            }
        }
    }
}

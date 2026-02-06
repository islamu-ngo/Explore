// ABOUTME: Code-behind for Categories admin page. Handles CRUD operations for event categories.

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
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    private ICollection<CategoryListDto> _categories = new List<CategoryListDto>();
    private string _searchString = string.Empty;
    private bool _isLoading = true;
    private string? _errorMessage;

    private IEnumerable<CategoryListDto> FilteredCategories
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_searchString))
            {
                return _categories;
            }

            return _categories.Where(c =>
                (c.FullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.MasterCode?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.ParentFullName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        _isLoading = true;
        _errorMessage = null;
        try
        {
            _categories = await CategoryService.GetCategoriesAsync() ?? new List<CategoryListDto>();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load categories: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OpenCreateDialog()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<CreateCategoryDialog>("Create Category", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: CreateCategoryDto createDto })
        {
            var response = await CategoryService.CreateCategoryAsync(createDto);
            if (response?.Success == true)
            {
                await LoadCategories();
            }
            else
            {
                _errorMessage = $"Failed to create category: {response?.Message ?? "Unknown error"}";
            }
        }
    }

    private async Task OpenEditDialog(CategoryListDto category)
    {
        var parameters = new DialogParameters<EditCategoryDialog>
        {
            { x => x.ExistingCategory, category }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<EditCategoryDialog>("Edit Category", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: UpdateCategoryDto updateDto } && updateDto.Id.HasValue)
        {
            var response = await CategoryService.UpdateCategoryAsync(updateDto.Id.Value, updateDto);
            if (response?.Success == true)
            {
                await LoadCategories();
            }
            else
            {
                _errorMessage = $"Failed to update category: {response?.Message ?? "Unknown error"}";
            }
        }
    }

    private async Task DeleteCategory(CategoryListDto category)
    {
        if (!category.Id.HasValue) return;

        bool? confirm = await DialogService.ShowMessageBox(
            "Confirm Delete",
            $"Are you sure you want to delete the category '{category.FullName}'? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirm == true)
        {
            var success = await CategoryService.DeleteCategoryAsync(category.Id.Value);
            if (success)
            {
                await LoadCategories();
            }
            else
            {
                _errorMessage = $"Failed to delete category '{category.FullName}'";
            }
        }
    }
}

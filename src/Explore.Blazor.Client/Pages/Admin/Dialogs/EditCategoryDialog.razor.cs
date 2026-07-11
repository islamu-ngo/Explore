// ABOUTME: Dialog helper entrypoint for showing EditCategoryDialog via typed static API.
// ABOUTME: Keeps dialog invocation logic in code-behind rather than inline Razor blocks.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin.Dialogs;

public partial class EditCategoryDialog : ComponentBase
{
    public static Task<IDialogReference> ShowAsync(
        IDialogService dialogService,
        string title,
        DialogParameters? parameters = null,
        DialogOptions? options = null)
        => dialogService.ShowAsync<EditCategoryDialog>(title, parameters ?? new DialogParameters(), options);
}

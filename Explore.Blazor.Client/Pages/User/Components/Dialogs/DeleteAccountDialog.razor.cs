// ABOUTME: Dialog helper entrypoint for showing DeleteAccountDialog via typed static API.
// ABOUTME: Keeps dialog invocation logic in code-behind rather than inline Razor blocks.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.User.Components.Dialogs;

public partial class DeleteAccountDialog : ComponentBase
{
    public static Task<IDialogReference> ShowAsync(
        IDialogService dialogService,
        DialogOptions? options = null)
        => dialogService.ShowAsync<DeleteAccountDialog>(
            "Delete Account",
            new DialogParameters(),
            options ?? new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true });
}

// ABOUTME: Dialog helper entrypoint for showing SelectSessionDialog via typed static API.
// ABOUTME: Keeps dialog invocation logic in code-behind rather than inline Razor blocks.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events.Dialogs;

public partial class SelectSessionDialog : ComponentBase
{
    public static Task<IDialogReference> ShowAsync(
        IDialogService dialogService,
        string title,
        DialogParameters? parameters = null,
        DialogOptions? options = null)
        => dialogService.ShowAsync<SelectSessionDialog>(title, parameters ?? new DialogParameters(), options);
}

// ABOUTME: Dialog helper entrypoint for showing ProgramSectionsDialog via typed static API.
// ABOUTME: Keeps section/track dialog invocation in code-behind instead of inline Razor blocks.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Events.Dialogs;

public partial class ProgramSectionsDialog : ComponentBase
{
    public static Task<IDialogReference> ShowAsync(
        IDialogService dialogService,
        string title,
        DialogParameters? parameters = null,
        DialogOptions? options = null)
        => dialogService.ShowAsync<ProgramSectionsDialog>(title, parameters ?? new DialogParameters(), options);
}

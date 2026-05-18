// ABOUTME: Dialog helper entrypoint for showing CreateApiKeyDialog via typed static API.
// ABOUTME: Keeps dialog invocation logic in code-behind rather than inline Razor blocks.

using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Admin.Dialogs;

public partial class CreateApiKeyDialog : ComponentBase
{
    public static Task<IDialogReference> ShowAsync(
        IDialogService dialogService,
        string title,
        DialogParameters? parameters = null,
        DialogOptions? options = null)
        => dialogService.ShowAsync<CreateApiKeyDialog>(
            title,
            parameters ?? new DialogParameters(),
            options ?? DialogOptionsFactory.Small());
}

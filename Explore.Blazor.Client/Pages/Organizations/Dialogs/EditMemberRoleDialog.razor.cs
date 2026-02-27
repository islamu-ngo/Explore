// ABOUTME: Dialog helper entrypoint for showing EditMemberRoleDialog via typed static API.
// ABOUTME: Keeps dialog invocation logic in code-behind rather than inline Razor blocks.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Pages.Organizations.Dialogs;

public partial class EditMemberRoleDialog : ComponentBase
{
    public static Task<IDialogReference> ShowAsync(
        IDialogService dialogService,
        string title,
        DialogParameters? parameters = null,
        DialogOptions? options = null)
        => dialogService.ShowAsync<EditMemberRoleDialog>(title, parameters ?? new DialogParameters(), options);
}

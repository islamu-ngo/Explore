// ABOUTME: Static factory producing standard DialogOptions presets for consistent dialog behavior.
// ABOUTME: Replaces scattered `new DialogOptions { ... }` with named presets (Small, Medium, Confirmation, Editor).

using MudBlazor;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Standard <see cref="DialogOptions"/> presets.
/// Use these instead of manually constructing DialogOptions to ensure
/// consistent dialog behavior across the application.
/// </summary>
public static class DialogOptionsFactory
{
    /// <summary>
    /// Small dialog — confirmations, simple selections, delete prompts.
    /// CloseOnEscapeKey + MaxWidth.Small + FullWidth.
    /// </summary>
    public static DialogOptions Small() => new()
    {
        CloseOnEscapeKey = true,
        MaxWidth = MaxWidth.Small,
        FullWidth = true
    };

    /// <summary>
    /// Medium dialog — forms, registration, aspect editing.
    /// CloseOnEscapeKey + MaxWidth.Medium + FullWidth.
    /// </summary>
    public static DialogOptions Medium() => new()
    {
        CloseOnEscapeKey = true,
        MaxWidth = MaxWidth.Medium,
        FullWidth = true
    };

    /// <summary>
    /// Centered confirmation — session selection, centered prompts.
    /// Small + Position.Center.
    /// </summary>
    public static DialogOptions Confirmation() => new()
    {
        CloseOnEscapeKey = true,
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        Position = DialogPosition.Center
    };

    /// <summary>
    /// Editor dialog — description editing, rich content.
    /// Medium + CloseButton + BackdropClick.
    /// </summary>
    public static DialogOptions Editor() => new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseButton = true,
        BackdropClick = true
    };
}

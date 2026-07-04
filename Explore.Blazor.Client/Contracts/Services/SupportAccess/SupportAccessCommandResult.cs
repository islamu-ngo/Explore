// ABOUTME: Client-side result model for support-access BFF commands.
// ABOUTME: Keeps command feedback separate from the pure support-access service interface.

namespace Explore.Blazor.Client.Contracts.Services.SupportAccess;

public sealed record SupportAccessCommandResult(bool Success, string? ErrorMessage)
{
    public static SupportAccessCommandResult Succeeded() => new(true, null);

    public static SupportAccessCommandResult Failed(string errorMessage) => new(false, errorMessage);
}

namespace Explore.Blazor.Client.Models;

/// <summary>
/// Represents user information that can be serialized and transferred between server and client.
/// Used with PersistentComponentState to preserve authentication state during hydration.
/// </summary>
public sealed class UserInfo
{
    /// <summary>
    /// The unique identifier for the user (typically the 'sub' claim from OIDC).
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The display name of the user (typically 'name' or 'preferred_username' claim).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Additional claims that should be preserved (type -> value).
    /// </summary>
    public Dictionary<string, string> Claims { get; init; } = new();
}

// ABOUTME: Client read model for projection dirty-scope drain outcomes.
// ABOUTME: Mirrors the server DrainDirtyScopesResponseDto for admin status display.

namespace Explore.Blazor.Client.Models.CustomProperties;

public sealed class DrainDirtyScopesResult
{
    public int DrainedCount { get; set; }
    public DateTimeOffset? DrainedAt { get; set; }
}

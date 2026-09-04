// ABOUTME: Normalized lookup row for authentication provider kinds persisted by user identity links.
// ABOUTME: Pairs stable enum IDs with machine codes and operator-facing names.

namespace Explore.Domain;

public sealed class AuthenticationProvider
{
    public int Id { get; set; }

    public required string MasterCode { get; set; }

    public required string FullName { get; set; }

    public string? Description { get; set; }
}

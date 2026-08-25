// ABOUTME: Authenticated API input for rotating backend Listmonk Basic Auth credentials.
// ABOUTME: Plaintext crosses only this write boundary and is stored through encrypted SecretBinding rows.

namespace Explore.Application.DTOs.Integrations;

public sealed record RotateListmonkIntegrationCredentialsDto
{
    public string? ApiUsername { get; init; }
    public string? ApiKey { get; init; }
}

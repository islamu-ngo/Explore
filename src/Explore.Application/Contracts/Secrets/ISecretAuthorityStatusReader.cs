// ABOUTME: Server-side port for the bounded selected-secret-authority status.
// ABOUTME: Exposes only provider, state, and remediation code without values or source coordinates.

namespace Explore.Application.Contracts.Secrets;

public interface ISecretAuthorityStatusReader
{
    Task<SecretAuthorityStatusSnapshot> ReadAsync(CancellationToken cancellationToken = default);
}

public sealed record SecretAuthorityStatusSnapshot(
    string Provider,
    string Status,
    string RemediationCode);

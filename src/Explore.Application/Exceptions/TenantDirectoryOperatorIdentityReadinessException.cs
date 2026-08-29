// ABOUTME: Payload-free failure raised when tenant creation cannot satisfy legal-identity readiness.
// ABOUTME: Carries stable machine codes without echoing names, contacts, identifiers, or URLs.

namespace Explore.Application.Exceptions;

using System.Collections.Immutable;

public sealed class TenantDirectoryOperatorIdentityReadinessException(
    string failureCode,
    IEnumerable<string> reasonCodes)
    : InvalidOperationException("Tenant directory operator identity is not ready.")
{
    public string FailureCode { get; } = failureCode;

    public ImmutableArray<string> ReasonCodes { get; } = [.. reasonCodes];
}

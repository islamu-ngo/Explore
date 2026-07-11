// ABOUTME: Provider-neutral operator status for authorization policy package health diagnostics.
// ABOUTME: Separates safe status classification from provider-specific Admin API/PDP transport details.

namespace Explore.Application.Authorization;

public sealed record PolicyPackageStatusResult(
    string PackageId,
    string ContentHash,
    DateTimeOffset CheckedAt,
    PolicyPackageIssueCode IssueCode,
    string Message,
    IReadOnlyList<string> Warnings)
{
    public bool IsHealthy => IssueCode is PolicyPackageIssueCode.None or PolicyPackageIssueCode.PackageStatusUnknown;
}

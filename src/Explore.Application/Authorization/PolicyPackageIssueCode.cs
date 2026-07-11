// ABOUTME: Provider-neutral issue codes for authorization policy package publishing and diagnostics.
// ABOUTME: Lets operators distinguish Admin API, package, reload, and PDP health failures without provider secrets.

namespace Explore.Application.Authorization;

public enum PolicyPackageIssueCode
{
    None = 0,
    AdminApiNotConfigured = 1,
    AdminApiAuthenticationFailed = 2,
    AdminApiUnavailable = 3,
    PackageMismatch = 4,
    PackageStatusUnknown = 5,
    ReloadFailed = 6,
    PdpUnreachable = 7,
    PublishFailed = 8,
    PackageUnavailable = 9
}

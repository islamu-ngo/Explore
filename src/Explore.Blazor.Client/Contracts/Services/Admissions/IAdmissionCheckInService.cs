// ABOUTME: Defines the target-aware online admission check-in boundary used by the Studio scanner.
// ABOUTME: Exposes only bounded public outcomes and keeps credential material transient and redacted.

using ISLAMU.Wire.Contracts.Admissions;

namespace Explore.Blazor.Client.Contracts.Services.Admissions;

public interface IAdmissionCheckInService
{
    Task<AdmissionCheckInUiResult> CheckInAsync(
        Guid eventId,
        Guid targetId,
        AdmissionCredentialBearer credential,
        CancellationToken cancellationToken);
}

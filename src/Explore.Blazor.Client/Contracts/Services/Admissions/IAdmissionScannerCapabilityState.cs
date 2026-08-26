// ABOUTME: Holds one scanner capability only for the current Blazor route and scoped client lifetime.
// ABOUTME: Exposes activation state without making capability material available to UI consumers.

namespace Explore.Blazor.Client.Contracts.Services.Admissions;

public interface IAdmissionScannerCapabilityState : IDisposable
{
    bool IsActive { get; }
    long Generation { get; }

    void Activate(string capability);
    void Clear();
}

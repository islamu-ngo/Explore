// ABOUTME: Application contract for resolving a tenant's effective event-reporting intake policy.
// ABOUTME: Keeps report reads and submissions independent from external provider routing.

namespace Explore.Application.Contracts.Services;

public sealed record EventReportingIntakeDecision(
    bool TenantResolved,
    bool IntakeEnabled,
    string ReasonCode,
    string Message);

public interface IEventReportingIntakeGuard
{
    Task<EventReportingIntakeDecision> ResolveAsync(Guid tenantId, CancellationToken cancellationToken);
}

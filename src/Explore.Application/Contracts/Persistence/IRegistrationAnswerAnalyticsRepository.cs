// ABOUTME: Persistence contract for governed registration-answer aggregate projections.
// ABOUTME: Keeps raw answer rows behind the repository boundary and returns Domain-owned projections only.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IRegistrationAnswerAnalyticsRepository
{
    Task<RegistrationAnswerAnalyticsProjection?> GetEventFormVersionAnalyticsAsync(
        Guid tenantId,
        Guid eventId,
        Guid formId,
        Guid formVersionId,
        int minimumCellSize,
        CancellationToken cancellationToken);
}

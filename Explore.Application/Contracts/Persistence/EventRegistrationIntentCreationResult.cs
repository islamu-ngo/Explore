// ABOUTME: Persistence result for capacity-aware event registration intent creation.
// ABOUTME: Keeps Application informed about waitlisted child sessions without exposing EF details.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public sealed record EventRegistrationIntentCreationResult(
    EventRegistrationIntent Intent,
    IReadOnlyList<Guid> WaitlistedSessionIds)
{
    public bool HasWaitlistedSessions => WaitlistedSessionIds.Count > 0;
}

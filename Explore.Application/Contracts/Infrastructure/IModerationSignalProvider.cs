// ABOUTME: Application boundary for moderation signal providers that evaluate event-report safety metadata.
// ABOUTME: Keeps Osprey, model, and local signal implementations behind provider-neutral envelopes.

using Explore.Application.Features.EventReporting.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IModerationSignalProvider
{
    Task<EventSafetySignalProviderResult> EvaluateAsync(
        EventReportProviderEnvelope envelope,
        CancellationToken cancellationToken = default);
}

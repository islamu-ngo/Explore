// ABOUTME: No-op moderation signal provider for LocalOnly and not-yet-configured external signal modes.
// ABOUTME: Returns an empty signal set without making network calls or exposing report evidence.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.EventReporting.Models;

namespace Explore.Infrastructure.Services.Moderation;

public sealed class NoopModerationSignalProvider : IModerationSignalProvider
{
    public Task<EventSafetySignalProviderResult> EvaluateAsync(
        EventReportProviderEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(EventSafetySignalProviderResult.Success());
    }
}

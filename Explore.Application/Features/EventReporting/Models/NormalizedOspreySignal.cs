// ABOUTME: Internal normalized value object for Osprey callback signal processing.
// ABOUTME: Keeps callback handler logic typed without leaking provider payload shapes across layers.

using Explore.Domain.Enums;

namespace Explore.Application.Features.EventReporting.Models;

public sealed record NormalizedOspreySignal(
    string SignalType,
    string PolicyCode,
    decimal? Score,
    EventReportSignalVerdict Verdict,
    EventReportRecommendedAction? RecommendedAction,
    string? SafeSummary,
    string? ExternalSignalId,
    string CorrelationId,
    EventReportProviderTargetScope ProviderTargetScope,
    string ProviderTargetId,
    DateTime CreatedAtUtc);

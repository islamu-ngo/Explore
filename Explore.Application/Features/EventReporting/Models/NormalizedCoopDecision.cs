// ABOUTME: Internal normalized value object for Coop decision callback processing.
// ABOUTME: Resolves provider action text into local decision, trace, and audit metadata.

using Explore.Domain.Enums;

namespace Explore.Application.Features.EventReporting.Models;

public sealed record NormalizedCoopDecision(
    Guid TenantId,
    Guid EventId,
    Guid ReportId,
    Guid CaseId,
    Guid? ExpectedCaseConcurrencyStamp,
    EventReportDecisionKind DecisionKind,
    string ReasonCode,
    string? SafeNote,
    Guid? DuplicateGroupId,
    string ExternalDecisionId,
    string? ProviderCaseId,
    string? ProviderUrl,
    string CorrelationId);

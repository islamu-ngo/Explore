// ABOUTME: Decision record created from local or provider report review.
// ABOUTME: Captures safe decision metadata before existing moderation enforcement runs.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventReportDecision : ITenantEntity, IAuditableEntity
{
    public const int MaxReasonCodeLength = 100;
    public const int MaxSafeNoteLength = 1000;
    public const int MaxExternalDecisionIdLength = 200;

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; private set; } = null!;
    public Guid CaseId { get; private set; }
    public EventReportCase Case { get; private set; } = null!;
    public Guid ReportId { get; private set; }
    public EventReport Report { get; private set; } = null!;
    public EventReportDecisionSource DecisionSource { get; private set; }
    public EventReportDecisionKind DecisionKind { get; private set; }
    public string ReasonCode { get; private set; } = null!;
    public string? SafeNote { get; private set; }
    public Guid? ModeratorUserId { get; private set; }
    public User? ModeratorUser { get; private set; }
    public string? ExternalDecisionId { get; private set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static EventReportDecision Create(
        Guid tenantId,
        Guid caseId,
        Guid reportId,
        EventReportDecisionSource decisionSource,
        EventReportDecisionKind decisionKind,
        string reasonCode,
        string? safeNote,
        Guid? moderatorUserId,
        string? externalDecisionId,
        DateTime? createdAt = null)
    {
        EventReportGuards.RequireGuid(tenantId, nameof(tenantId), "Tenant id is required.");
        EventReportGuards.RequireGuid(caseId, nameof(caseId), "Case id is required.");
        EventReportGuards.RequireGuid(reportId, nameof(reportId), "Report id is required.");
        EventReportGuards.RequireDefined(decisionSource, nameof(decisionSource));
        EventReportGuards.RequireDefined(decisionKind, nameof(decisionKind));

        if (moderatorUserId == Guid.Empty)
        {
            throw new ArgumentException("Moderator user id cannot be empty.", nameof(moderatorUserId));
        }

        if (decisionSource == EventReportDecisionSource.LocalModerator && moderatorUserId is null)
        {
            throw new ArgumentException("Local moderator decisions require a moderator user id.", nameof(moderatorUserId));
        }

        return new EventReportDecision
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CaseId = caseId,
            ReportId = reportId,
            DecisionSource = decisionSource,
            DecisionKind = decisionKind,
            ReasonCode = EventReportGuards.NormalizeRequired(reasonCode, MaxReasonCodeLength, nameof(reasonCode)),
            SafeNote = EventReportGuards.NormalizeOptional(safeNote, MaxSafeNoteLength, nameof(safeNote)),
            ModeratorUserId = moderatorUserId,
            ExternalDecisionId = EventReportGuards.NormalizeOptional(externalDecisionId, MaxExternalDecisionIdLength, nameof(externalDecisionId)),
            CreatedAt = createdAt ?? DateTime.UtcNow,
            CreatedBy = moderatorUserId
        };
    }
}

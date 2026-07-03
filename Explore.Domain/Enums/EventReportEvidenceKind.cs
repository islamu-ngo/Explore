// ABOUTME: Evidence item categories attached to event reports.
// ABOUTME: Phase-one supports reporter text while keeping uploads and external references explicit.

namespace Explore.Domain.Enums;

public enum EventReportEvidenceKind
{
    ReporterText = 1,
    TargetReference = 2,
    UploadedAttachment = 3,
    SystemSignal = 4,
    ExternalReference = 5
}

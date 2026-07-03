// ABOUTME: Sensitivity classifications for event-report evidence rows.
// ABOUTME: Enables retention and access-control decisions without reading evidence content.

namespace Explore.Domain.Enums;

public enum EventReportEvidenceClassification
{
    Normal = 1,
    Sensitive = 2,
    IllegalOrHighRisk = 3
}

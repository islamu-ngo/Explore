// ABOUTME: Stable closed vocabularies for EventLocation privacy audit and erasure-authority facts.
// ABOUTME: Prevents free-form text or encoded physical-location values from entering durable evidence.

namespace Explore.Domain.Enums;

public enum EventLocationDisclosureAuditReasonEnum
{
    AssociationCreated = 1,
    OrganizerPolicyChange = 2,
    GovernanceTightening = 3,
    PrivacyErasureRemediation = 4,
    LegacyBackfill = 5
}

public enum EventLocationExactReadPurposeEnum
{
    EventManagement = 1,
    SupportCaseReview = 2,
    ModerationReview = 3,
    PrivacyRemediation = 4
}

public enum LocationPrivacyErasureReasonEnum
{
    AccountDeletion = 1,
    OwnerErasureRequest = 2,
    PrivacyIncidentRemediation = 3
}

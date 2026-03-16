// ABOUTME: Enum representing the lifecycle status of a contact-sharing consent record.
// ABOUTME: Used by EventContactShareConsent to track whether consent is currently granted or withdrawn.

namespace Explore.Domain.Enums;

public enum ConsentStatus
{
    Granted = 1,
    Withdrawn = 2
}

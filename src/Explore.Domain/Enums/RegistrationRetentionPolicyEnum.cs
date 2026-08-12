// ABOUTME: Stable enum mirror for registration retention policy lookup identities.
// ABOUTME: Durations are resolved at creation so cleanup never infers deadlines later.

namespace Explore.Domain.Enums;

public enum RegistrationRetentionPolicyEnum
{
    StandardOperational = 1,
    SensitiveShort = 2,
    MarketingConsentEvidence = 3,
    LegalHold = 4
}

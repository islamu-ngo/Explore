// ABOUTME: PII sensitivity tiers for AI context disclosure (NIST SP 800-122 / GDPR Art. 4(1)).
// ABOUTME: Higher values are more restrictive; unclassified fields default to the highest tier.

namespace Explore.Domain.Enums;

public enum AiContextSensitivityEnum
{
    Public = 0,
    Internal = 1,
    Confidential = 2,
    Restricted = 3,
    Special = 4
}

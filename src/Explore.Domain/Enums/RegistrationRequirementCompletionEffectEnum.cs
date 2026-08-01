// ABOUTME: Enum mirror for stable registration-requirement completion-effect lookup identities.
// ABOUTME: Separates registration blocking from enrichment and non-registration effects.

namespace Explore.Domain.Enums;

public enum RegistrationRequirementCompletionEffectEnum
{
    BlocksRegistration = 1,
    EnrichesRegistration = 2,
    NoRegistrationEffect = 3
}

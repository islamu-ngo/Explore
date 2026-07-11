// ABOUTME: Per-field disclosure rule that the AI context gateway applies to a classified field.
// ABOUTME: Effective rule is the intersection of matrix base rule, viewer scope, consent, and provider trust.

namespace Explore.Domain.Enums;

public enum AiContextDisclosureRuleEnum
{
    Deny = 0,
    Redact = 1,
    Aggregate = 2,
    Allow = 3
}

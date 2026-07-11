// ABOUTME: Contract for deciding whether event custom-property definitions may drive automation conditions.
// ABOUTME: Keeps Layer 3 metadata optional and projection-backed while blocking workflow-critical EAV state.

using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface ICustomPropertyAutomationConditionPolicy
{
    CustomPropertyAutomationConditionEvaluation Evaluate(EventCustomPropertyDefinition definition);
}

public sealed record CustomPropertyAutomationConditionEvaluation(
    bool IsEligible,
    string NormalizedNamespace,
    string NormalizedKey,
    bool RequiresProjection,
    IReadOnlyList<string> Errors);

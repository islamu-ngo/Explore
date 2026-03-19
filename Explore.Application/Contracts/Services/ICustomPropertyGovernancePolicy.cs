// ABOUTME: Contract for Layer 3 custom-property boundary enforcement before persistence or CQRS writes.
// ABOUTME: Normalizes machine identity and rejects reserved namespace or Layer 2 semantic collisions.

namespace Explore.Application.Contracts.Services;

public interface ICustomPropertyGovernancePolicy
{
    CustomPropertyGovernanceEvaluation EvaluateDefinition(string namespaceValue, string key, bool canManageReservedNamespaces = false);
}

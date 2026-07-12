// ABOUTME: Operator-safe outcome of reconciling a deployment-selected authorization provider.
// ABOUTME: Separates desired provider selection from Cerbos endpoint and policy readiness.

namespace Explore.Application.Authorization;

public sealed record AuthorizationProviderReconciliationResult(
    bool Attempted,
    bool Succeeded,
    bool EndpointVerified,
    bool PoliciesSynchronized,
    string Message);

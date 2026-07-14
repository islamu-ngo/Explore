// ABOUTME: Mutates the scoped machine principal only for trusted non-HTTP execution boundaries.
// ABOUTME: Separates background-worker principal binding from the read-only authorization accessor.

using Explore.Application.Authentication;

namespace Explore.Application.Contracts.Identity;

public interface IMachinePrincipalExecutionAccessor
{
    void SetPrincipal(ApiKeyPrincipalContext principal);

    void Clear();
}

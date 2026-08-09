// ABOUTME: Resolves registration-provider callback bindings without requiring tenant identity from the route.
// ABOUTME: Keeps anonymous controller code away from registration aggregates and repositories.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Domain;

namespace Explore.API.Services;

public sealed class RegistrationProviderCallbackBindingResolver(
    IRegistrationProviderRepository repository) : IRegistrationProviderCallbackBindingResolver
{
    public async Task<RegistrationProviderBinding?> ResolveForCallbackAsync(
        string provider,
        Guid bindingId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider) || bindingId == Guid.Empty)
        {
            return null;
        }

        RegistrationProviderBinding? binding = await repository.GetBindingForCallbackAsync(bindingId, cancellationToken);
        return binding is not null && binding.Capabilities.Any(capability =>
                !capability.IsDeleted &&
                string.Equals(capability.ProviderCode, provider.Trim(), StringComparison.OrdinalIgnoreCase))
            ? binding
            : null;
    }
}

// ABOUTME: Keeps configured-administrator bootstrap unavailable until runtime activation is complete.
// ABOUTME: Performs no configuration, management, network, or identity-selection behavior.

using Explore.Application.Authentication;
using Explore.Application.Contracts.Services;
using Explore.Application.Models;

namespace Explore.Infrastructure.Services;

public sealed class DisabledConfiguredAdministratorBootstrapProvider
    : IConfiguredAdministratorBootstrapProvider
{
    public Task<ConfiguredAdministratorBootstrapBinding?> GetVerifiedBindingAsync(
        ProviderAccountKey authenticatedAccount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authenticatedAccount);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<ConfiguredAdministratorBootstrapBinding?>(null);
    }
}

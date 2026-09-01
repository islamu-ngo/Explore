// ABOUTME: Serializes secret-binding operations for one exact Setup enrollment generation.
// ABOUTME: Reuses provider-neutral session locks so coordination holds across application instances.

namespace Explore.Persistence;

using System.Globalization;
using Explore.Application.Contracts.SetupLive;
using Explore.Persistence.Database;

public sealed class RelationalSetupSecretBindingOperationCoordinator(
    ExploreDbContext dbContext) : ISetupSecretBindingOperationCoordinator
{
    public Task<IAsyncDisposable> AcquireAsync(
        SetupSecretBindingCoordinationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return RelationalNamedLock.AcquireSessionAsync(
            dbContext,
            string.Create(
                CultureInfo.InvariantCulture,
                $"explore:setup-secret-binding:{request.TenantId:D}:{request.EnrollmentId:D}:{request.EnrollmentGeneration}"),
            cancellationToken);
    }
}

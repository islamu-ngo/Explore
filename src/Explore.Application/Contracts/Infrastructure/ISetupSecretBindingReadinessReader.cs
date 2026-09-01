// ABOUTME: Defines the value-free Setup secret-binding readiness infrastructure port.
// ABOUTME: Reports dispatch feasibility without reading or accepting secret material.

namespace Explore.Application.Contracts.Infrastructure;

using Explore.Application.Contracts.SetupLive;

public interface ISetupSecretBindingReadinessReader
{
    Task<SetupSecretBindingWriteOutcome> GetReadinessAsync(
        Guid bindingId,
        string bindingKey,
        CancellationToken cancellationToken);
}

// ABOUTME: Defines the one-way target-local Setup secret write port.
// ABOUTME: Accepts only server-resolved binding identity and borrowed bytes.

namespace Explore.Application.Contracts.Secrets;

using Explore.Application.Contracts.SetupLive;

public interface ISetupSecretBindingWriter
{
    Task<SetupSecretBindingWriteOutcome> WriteAsync(
        SetupSecretBindingWriteRequest request,
        CancellationToken cancellationToken);
}

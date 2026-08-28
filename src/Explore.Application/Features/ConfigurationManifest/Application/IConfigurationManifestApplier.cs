// ABOUTME: Exposes the validated configuration-manifest application boundary to startup composition roots.
// ABOUTME: Keeps hosts independent of MediatR dispatch while preserving one canonical command handler.

namespace Explore.Application.Features.ConfigurationManifest.Application;

using Explore.Application.Features.ConfigurationManifest.Ingestion;
using Explore.Application.Responses;

public interface IConfigurationManifestApplier
{
    Task<BaseCommandResponse<Guid>> ApplyAsync(
        ConfigurationManifestReadResult source,
        CancellationToken cancellationToken);
}

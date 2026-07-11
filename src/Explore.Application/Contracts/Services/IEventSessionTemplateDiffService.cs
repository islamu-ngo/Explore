// ABOUTME: Read-only contract for computing explicit event-session template-to-runtime diffs before operator-confirmed sync.
// ABOUTME: Returns deterministic DTO output that the apply path can recompute and validate server-side.

using Explore.Application.DTOs.EventSessionTemplateSync;

namespace Explore.Application.Contracts.Services;

public interface IEventSessionTemplateDiffService
{
    Task<TemplateDiffDto> ComputeDiffAsync(
        Guid eventSessionId,
        int targetTemplateVersion,
        CancellationToken cancellationToken);
}

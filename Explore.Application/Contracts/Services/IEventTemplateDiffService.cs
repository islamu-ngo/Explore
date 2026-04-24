// ABOUTME: Read-only contract for computing explicit event template-to-runtime diffs before operator-confirmed sync.
// ABOUTME: Returns deterministic DTO output that the apply path can recompute and validate server-side.

using Explore.Application.DTOs.EventTemplateSync;

namespace Explore.Application.Contracts.Services;

public interface IEventTemplateDiffService
{
    Task<TemplateDiffDto> ComputeDiffAsync(
        Guid eventId,
        int targetTemplateVersion,
        CancellationToken cancellationToken);
}

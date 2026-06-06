// ABOUTME: Interface for fetching diffs and applying template sync operations for an Event.
// ABOUTME: Manual implementation to be replaced by NSwag generated client if possible.

using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventTemplateSync;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services.EventTemplateSync;

public interface IEventTemplateSyncService
{
    Task<TemplateDiffDto?> GetDiffAsync(Guid eventId, int templateVersion, CancellationToken cancellationToken = default);
    Task<BaseCommandResponse<TemplateSyncOutcomeDto>> ApplySyncAsync(Guid eventId, EventTemplateSyncApplyRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResult<EventTemplateSyncHistoryItemDto>> GetHistoryAsync(Guid eventId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

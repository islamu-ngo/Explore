// ABOUTME: Interface for fetching diffs and applying template sync operations for an Event Session.
// ABOUTME: Manual implementation to be replaced by NSwag generated client if possible.

using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventSessionTemplateSync;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services.EventSessionTemplateSync;

public interface IEventSessionTemplateSyncService
{
    Task<TemplateDiffDto?> GetDiffAsync(Guid sessionId, int templateVersion, CancellationToken cancellationToken = default);
    Task<BaseCommandResponse<TemplateSyncOutcomeDto>> ApplySyncAsync(Guid sessionId, EventSessionTemplateSyncApplyRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResult<EventSessionTemplateSyncHistoryItemDto>> GetHistoryAsync(Guid sessionId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

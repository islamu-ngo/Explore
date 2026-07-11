// ABOUTME: Generated-client service for fetching and applying Event Session template synchronization.
// ABOUTME: Routes every backend call through IEventApiClient and preserves generated payload types.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.EventSessionTemplateSync;

public sealed class EventSessionTemplateSyncService : IEventSessionTemplateSyncService
{
    private readonly IEventApiClient _api;

    public EventSessionTemplateSyncService(IEventApiClient api)
    {
        _api = api;
    }

    public Task<HalResourceOfTemplateDiffDto> GetDiffAsync(Guid sessionId, int templateVersion, CancellationToken cancellationToken = default) =>
        _api.GetEventSessionTemplateSyncDiffAsync(sessionId, templateVersion, cancellationToken: cancellationToken);

    public Task<TemplateSyncOutcomeDto> ApplySyncAsync(Guid sessionId, EventSessionTemplateSyncApplyRequest request, CancellationToken cancellationToken = default) =>
        _api.ApplyEventSessionTemplateSyncAsync(sessionId, request, cancellationToken: cancellationToken);

    public Task<PaginatedResultOfEventSessionTemplateSyncHistoryItemDto> GetHistoryAsync(Guid sessionId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        _api.GetEventSessionTemplateSyncHistoryAsync(sessionId, page, pageSize, cancellationToken: cancellationToken);
}

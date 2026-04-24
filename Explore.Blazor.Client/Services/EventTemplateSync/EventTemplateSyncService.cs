// ABOUTME: HTTP service for fetching diffs and applying template sync operations for an Event.
// ABOUTME: Manual implementation to be replaced by NSwag generated client if possible.

using System.Net.Http.Json;
using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventTemplateSync;

namespace Explore.Blazor.Client.Services.EventTemplateSync;

public sealed class EventTemplateSyncService : IEventTemplateSyncService
{
    private readonly HttpClient _httpClient;

    public EventTemplateSyncService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BaseCommandResponse<TemplateDiffDto>> GetDiffAsync(Guid eventId, int templateVersion, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<BaseCommandResponse<TemplateDiffDto>>(
            $"api/events/{eventId}/template-sync/diff?templateVersion={templateVersion}", cancellationToken);
        return response ?? throw new InvalidOperationException("Failed to read diff response.");
    }

    public async Task<BaseCommandResponse<TemplateSyncOutcomeDto>> ApplySyncAsync(Guid eventId, EventTemplateSyncApplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/events/{eventId}/template-sync/apply", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BaseCommandResponse<TemplateSyncOutcomeDto>>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to read apply response.");
    }

    public async Task<PaginatedResult<EventTemplateSyncHistoryItemDto>> GetHistoryAsync(Guid eventId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<PaginatedResult<EventTemplateSyncHistoryItemDto>>(
            $"api/events/{eventId}/template-sync/history?page={page}&pageSize={pageSize}", cancellationToken);
        return response ?? throw new InvalidOperationException("Failed to read history response.");
    }
}

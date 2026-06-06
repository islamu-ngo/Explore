// ABOUTME: HTTP service for fetching diffs and applying template sync operations for an Event.
// ABOUTME: Manual implementation to be replaced by NSwag generated client if possible.

using System.Net.Http.Json;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventTemplateSync;
using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Services.Http;

namespace Explore.Blazor.Client.Services.EventTemplateSync;

public sealed class EventTemplateSyncService : IEventTemplateSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IApiClientExecutor _apiClientExecutor;

    public EventTemplateSyncService(HttpClient httpClient, IApiClientExecutor? apiClientExecutor = null)
    {
        _httpClient = httpClient;
        _apiClientExecutor = apiClientExecutor ?? new ApiClientExecutor();
    }

    public async Task<TemplateDiffDto?> GetDiffAsync(Guid eventId, int templateVersion, CancellationToken cancellationToken = default)
    {
        var result = await _apiClientExecutor.ReadJsonAsync<TemplateDiffDto>(
            token => _httpClient.GetAsync($"api/events/{eventId}/template-sync/diff?templateVersion={templateVersion}", token),
            "event template sync diff",
            cancellationToken);

        return result.IsSuccess
            ? result.Value
            : throw CreateExecutorException(result, "Failed to read diff response.");
    }

    public async Task<BaseCommandResponse<TemplateSyncOutcomeDto>> ApplySyncAsync(Guid eventId, EventTemplateSyncApplyRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<TemplateSyncOutcomeDto>>(
            token => _httpClient.PostAsJsonAsync($"api/events/{eventId}/template-sync/apply", request, token),
            "event template sync apply",
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw CreateExecutorException(result, "Failed to read apply response.");
        }

        return result.Value ?? throw new InvalidOperationException("Failed to read apply response.");
    }

    public async Task<PaginatedResult<EventTemplateSyncHistoryItemDto>> GetHistoryAsync(Guid eventId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _apiClientExecutor.ReadJsonAsync<PaginatedResult<EventTemplateSyncHistoryItemDto>>(
            token => _httpClient.GetAsync($"api/events/{eventId}/template-sync/history?page={page}&pageSize={pageSize}", token),
            "event template sync history",
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw CreateExecutorException(result, "Failed to read history response.");
        }

        return result.Value ?? throw new InvalidOperationException("Failed to read history response.");
    }

    private static InvalidOperationException CreateExecutorException<T>(ApiResult<T> result, string fallbackMessage)
    {
        if (IsNullSuccessBodyFailure(result))
        {
            return new InvalidOperationException(fallbackMessage, result.Exception ?? result.Problem);
        }

        return new InvalidOperationException(result.ErrorMessage ?? fallbackMessage, result.Exception ?? result.Problem);
    }

    private static bool IsNullSuccessBodyFailure<T>(ApiResult<T> result)
    {
        return result.Exception is InvalidOperationException
            && result.ErrorMessage?.Contains("response body deserialized to null", StringComparison.OrdinalIgnoreCase) == true;
    }
}

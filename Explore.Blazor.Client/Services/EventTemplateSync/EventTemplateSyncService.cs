// ABOUTME: Refit-backed service for fetching diffs and applying template sync operations for an Event.
// ABOUTME: Keeps template-sync calls on the shared BFF HTTP pipeline without hand-written HttpClient calls.

using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventTemplateSync;
using Explore.Blazor.Client.Models.Responses;
using Refit;

namespace Explore.Blazor.Client.Services.EventTemplateSync;

public interface IEventTemplateSyncApi
{
    [Get("/api/events/{eventId}/template-sync/diff")]
    Task<IApiResponse<TemplateDiffDto>> GetDiffAsync(
        Guid eventId,
        int templateVersion,
        CancellationToken cancellationToken);

    [Post("/api/events/{eventId}/template-sync/apply")]
    Task<IApiResponse<BaseCommandResponse<TemplateSyncOutcomeDto>>> ApplySyncAsync(
        Guid eventId,
        [Body] EventTemplateSyncApplyRequest request,
        CancellationToken cancellationToken);

    [Get("/api/events/{eventId}/template-sync/history")]
    Task<IApiResponse<PaginatedResult<EventTemplateSyncHistoryItemDto>>> GetHistoryAsync(
        Guid eventId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class EventTemplateSyncService : IEventTemplateSyncService
{
    private readonly IEventTemplateSyncApi _api;

    public EventTemplateSyncService(IEventTemplateSyncApi api)
    {
        _api = api;
    }

    public async Task<TemplateDiffDto?> GetDiffAsync(Guid eventId, int templateVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _api.GetDiffAsync(eventId, templateVersion, cancellationToken);

            return response.IsSuccessStatusCode
                ? response.Content ?? throw new InvalidOperationException("Failed to read diff response.")
                : throw CreateResponseException(response, "Failed to read diff response.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to read diff response.", ex);
        }
    }

    public async Task<BaseCommandResponse<TemplateSyncOutcomeDto>> ApplySyncAsync(Guid eventId, EventTemplateSyncApplyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _api.ApplySyncAsync(eventId, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw CreateResponseException(response, "Failed to read apply response.");
            }

            return response.Content ?? throw new InvalidOperationException("Failed to read apply response.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to read apply response.", ex);
        }
    }

    public async Task<PaginatedResult<EventTemplateSyncHistoryItemDto>> GetHistoryAsync(Guid eventId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _api.GetHistoryAsync(eventId, page, pageSize, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw CreateResponseException(response, "Failed to read history response.");
            }

            return response.Content ?? throw new InvalidOperationException("Failed to read history response.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to read history response.", ex);
        }
    }

    private static InvalidOperationException CreateResponseException(IApiResponse response, string fallbackMessage)
    {
        var message = response.Error?.Content;
        message = string.IsNullOrWhiteSpace(message) ? response.Error?.Message : message;
        message = string.IsNullOrWhiteSpace(message) ? fallbackMessage : message;

        return new InvalidOperationException(message, response.Error);
    }
}

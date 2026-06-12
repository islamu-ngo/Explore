// ABOUTME: Refit-backed service for fetching diffs and applying template sync operations for an Event Session.
// ABOUTME: Keeps session template-sync calls on the shared BFF HTTP pipeline without hand-written HttpClient calls.

using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventSessionTemplateSync;
using Explore.Blazor.Client.Models.Responses;
using Refit;

namespace Explore.Blazor.Client.Services.EventSessionTemplateSync;

public interface IEventSessionTemplateSyncApi
{
    [Get("/api/event-sessions/{sessionId}/template-sync/diff")]
    Task<IApiResponse<TemplateDiffDto>> GetDiffAsync(
        Guid sessionId,
        int templateVersion,
        CancellationToken cancellationToken);

    [Post("/api/event-sessions/{sessionId}/template-sync/apply")]
    Task<IApiResponse<BaseCommandResponse<TemplateSyncOutcomeDto>>> ApplySyncAsync(
        Guid sessionId,
        [Body] EventSessionTemplateSyncApplyRequest request,
        CancellationToken cancellationToken);

    [Get("/api/event-sessions/{sessionId}/template-sync/history")]
    Task<IApiResponse<PaginatedResult<EventSessionTemplateSyncHistoryItemDto>>> GetHistoryAsync(
        Guid sessionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class EventSessionTemplateSyncService : IEventSessionTemplateSyncService
{
    private readonly IEventSessionTemplateSyncApi _api;

    public EventSessionTemplateSyncService(IEventSessionTemplateSyncApi api)
    {
        _api = api;
    }

    public async Task<TemplateDiffDto?> GetDiffAsync(Guid sessionId, int templateVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _api.GetDiffAsync(sessionId, templateVersion, cancellationToken);

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

    public async Task<BaseCommandResponse<TemplateSyncOutcomeDto>> ApplySyncAsync(Guid sessionId, EventSessionTemplateSyncApplyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _api.ApplySyncAsync(sessionId, request, cancellationToken);
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

    public async Task<PaginatedResult<EventSessionTemplateSyncHistoryItemDto>> GetHistoryAsync(Guid sessionId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _api.GetHistoryAsync(sessionId, page, pageSize, cancellationToken);
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

// ABOUTME: Implementation of IEventSessionTemplateService wrapping IEventApiClient.
// ABOUTME: Handles HAL unwrap, error catching, and logging for session templates.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.EventSessionTemplates;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventSessionTemplates;
using Explore.Blazor.Client.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class EventSessionTemplateService : IEventSessionTemplateService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventSessionTemplateService> _logger;

    public EventSessionTemplateService(IEventApiClient apiClient, ILogger<EventSessionTemplateService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResult<EventSessionTemplateListModel>> GetTemplatesAsync(
        Guid? eventTemplateId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _apiClient.GetEventSessionTemplatesAsync(
                eventTemplateId,
                pageNumber,
                pageSize,
                cancellationToken: ct);

            return response.ToEventSessionTemplatePaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SESSION TEMPLATE] Failed to fetch templates for event template {EventTemplateId}", eventTemplateId);
            return PaginatedResult<EventSessionTemplateListModel>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<EventSessionTemplateDetailModel?> GetTemplateByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var hal = await _apiClient.GetEventSessionTemplateByIdAsync(id, cancellationToken: ct);
            return hal.ToEventSessionTemplateModel();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SESSION TEMPLATE] Failed to fetch template {SessionTemplateId}", id);
            return null;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> CreateTemplateAsync(
        CreateEventSessionTemplateDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var response = await _apiClient.CreateEventSessionTemplateAsync(dto, cancellationToken: ct);
            return ToClientResponse(response);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[EVENT SESSION TEMPLATE] API error creating template: {Detail}", ex.Result?.Detail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SESSION TEMPLATE] Unexpected error creating template");
            return null;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> UpdateTemplateAsync(
        Guid id,
        UpdateEventSessionTemplateDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var response = await _apiClient.UpdateEventSessionTemplateAsync(id, dto, cancellationToken: ct);
            return ToClientResponse(response);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[EVENT SESSION TEMPLATE] API error updating template {SessionTemplateId}: {Detail}", id, ex.Result?.Detail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SESSION TEMPLATE] Unexpected error updating template {SessionTemplateId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteTemplateAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            await _apiClient.DeleteEventSessionTemplateAsync(id, cancellationToken: ct);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SESSION TEMPLATE] Unexpected error deleting template {SessionTemplateId}", id);
            return false;
        }
    }

    private static BaseCommandResponse<Guid> ToClientResponse(BaseCommandResponseOfGuid response)
    {
        return new BaseCommandResponse<Guid>
        {
            Success = response.Success ?? false,
            Id = response.Id ?? Guid.Empty,
            Message = response.Message,
            Errors = response.Errors?.ToList()
        };
    }
}

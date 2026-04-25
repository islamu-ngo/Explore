// ABOUTME: Implementation of IEventTemplateService wrapping IEventApiClient.
// ABOUTME: Handles HAL unwrap, error catching, and logging for event templates.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.EventTemplates;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventTemplates;
using Explore.Blazor.Client.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class EventTemplateService : IEventTemplateService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventTemplateService> _logger;

    public EventTemplateService(IEventApiClient apiClient, ILogger<EventTemplateService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResult<EventTemplateListModel>> GetTemplatesAsync(
        int? eventTypeId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _apiClient.GetEventTemplatesAsync(
                eventTypeId,
                pageNumber,
                pageSize,
                cancellationToken: ct);

            return response.ToEventTemplatePaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT TEMPLATE] Failed to fetch templates");
            return PaginatedResult<EventTemplateListModel>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<EventTemplateDetailModel?> GetTemplateByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var hal = await _apiClient.GetEventTemplateByIdAsync(id, cancellationToken: ct);
            return hal.ToEventTemplateModel();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT TEMPLATE] Failed to fetch template {TemplateId}", id);
            return null;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> CreateTemplateAsync(
        CreateEventTemplateDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var response = await _apiClient.CreateEventTemplateAsync(dto, cancellationToken: ct);
            return ToClientResponse(response);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[EVENT TEMPLATE] API error creating template: {Detail}", ex.Result?.Detail);
            throw; // UI handles this for validation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT TEMPLATE] Unexpected error creating template");
            return null;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> UpdateTemplateAsync(
        Guid id,
        UpdateEventTemplateDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var response = await _apiClient.UpdateEventTemplateAsync(id, dto, cancellationToken: ct);
            return ToClientResponse(response);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[EVENT TEMPLATE] API error updating template {TemplateId}: {Detail}", id, ex.Result?.Detail);
            throw; // UI handles this for validation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT TEMPLATE] Unexpected error updating template {TemplateId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteTemplateAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            await _apiClient.DeleteEventTemplateAsync(id, cancellationToken: ct);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return true; // Already deleted
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT TEMPLATE] Unexpected error deleting template {TemplateId}", id);
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
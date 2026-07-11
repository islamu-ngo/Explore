// ABOUTME: Implementation of IEventTemplateService wrapping IEventApiClient.
// ABOUTME: Handles HAL unwrap, error catching, and logging for event templates.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.EventTemplates;
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

    public async Task<HalCollectionResourceOfEventTemplateListDto> GetTemplatesAsync(
        int? eventTypeId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            return await _apiClient.GetEventTemplatesAsync(
                eventTypeId,
                pageNumber,
                pageSize,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT TEMPLATE] Failed to fetch templates");
            return new HalCollectionResourceOfEventTemplateListDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0,
                _embedded = new HalCollectionEmbeddedOfEventTemplateListDto { Items = [] }
            };
        }
    }

    public async Task<HalResourceOfEventTemplateDto?> GetTemplateByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        try
        {
            return await _apiClient.GetEventTemplateByIdAsync(id, cancellationToken: ct);
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

    public async Task<BaseCommandResponseOfGuid?> CreateTemplateAsync(
        CreateEventTemplateDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            return await _apiClient.CreateEventTemplateAsync(dto, cancellationToken: ct);
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

    public async Task<BaseCommandResponseOfGuid?> UpdateTemplateAsync(
        Guid id,
        UpdateEventTemplateDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            return await _apiClient.UpdateEventTemplateAsync(id, dto, cancellationToken: ct);
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
}

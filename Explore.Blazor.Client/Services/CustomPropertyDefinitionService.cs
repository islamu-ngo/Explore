// ABOUTME: Implementation of ICustomPropertyDefinitionService wrapping IEventApiClient.
// ABOUTME: Handles HAL unwrap, error catching, and logging for CRUD of layer 3 definitions.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.CustomProperties;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.CustomProperties;
using Explore.Blazor.Client.Models.Responses;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class CustomPropertyDefinitionService : ICustomPropertyDefinitionService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<CustomPropertyDefinitionService> _logger;

    public CustomPropertyDefinitionService(IEventApiClient apiClient, ILogger<CustomPropertyDefinitionService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResult<CustomPropertyDefinitionListModel>> GetDefinitionsAsync(
        EntityTypeName entityTypeName,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetCustomPropertyDefinitionsAsync(
                (int)entityTypeName,
                pageNumber,
                pageSize,
                cancellationToken: cancellationToken);

            return response.ToPaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP DEF] Failed to fetch definitions for {EntityType}", entityTypeName);
            return PaginatedResult<CustomPropertyDefinitionListModel>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<CustomPropertyDefinitionDetailModel?> GetDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hal = await _apiClient.GetCustomPropertyDefinitionByIdAsync(id, cancellationToken: cancellationToken);
            return hal.ToModel();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP DEF] Failed to fetch definition {DefinitionId}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<CustomPropertyDefinitionDetailModel>> GetEventDefinitionsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetEventCustomPropertyDefinitionsAsync(
                eventId,
                1,
                200,
                cancellationToken: cancellationToken);

            var listItems = response.GetItems()
                .Where(d => d.IsActive)
                .OrderBy(d => d.SortOrder)
                .ToList();

            var detailTasks = listItems.Select(d => GetEventDefinitionAsync(d.Id, cancellationToken));
            var details = await Task.WhenAll(detailTasks);
            return NonNull(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP DEF] Failed to fetch event-local definitions for Event {EventId}", eventId);
            throw;
        }
    }

    public async Task<IReadOnlyList<CustomPropertyDefinitionDetailModel>> GetEventSessionDefinitionsAsync(
        Guid eventSessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetEventSessionCustomPropertyDefinitionsAsync(
                eventSessionId,
                1,
                200,
                cancellationToken: cancellationToken);

            var listItems = response.GetItems()
                .Where(d => d.IsActive)
                .OrderBy(d => d.SortOrder)
                .ToList();

            var detailTasks = listItems.Select(d => GetEventSessionDefinitionAsync(d.Id, cancellationToken));
            var details = await Task.WhenAll(detailTasks);
            return NonNull(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP DEF] Failed to fetch session-local definitions for EventSession {EventSessionId}", eventSessionId);
            throw;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> CreateDefinitionAsync(
        CreateCustomPropertyDefinitionDto body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            var response = await _apiClient.CreateCustomPropertyDefinitionAsync(body, cancellationToken: cancellationToken);
            return ToClientResponse(response);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[CP DEF] API error creating definition: {Detail}", ex.Result?.Detail);
            throw; // Let the UI handle API exceptions for validation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP DEF] Unexpected error creating definition");
            return null;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> UpdateDefinitionAsync(
        Guid id,
        UpdateCustomPropertyDefinitionDto body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            var response = await _apiClient.UpdateCustomPropertyDefinitionAsync(id, body, cancellationToken: cancellationToken);
            return ToClientResponse(response);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[CP DEF] API error updating definition {DefinitionId}: {Detail}", id, ex.Result?.Detail);
            throw; // Let the UI handle API exceptions for validation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP DEF] Unexpected error updating definition {DefinitionId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _apiClient.DeleteCustomPropertyDefinitionAsync(id, cancellationToken: cancellationToken);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return true; // Already deleted
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP DEF] Unexpected error deleting definition {DefinitionId}", id);
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

    private async Task<CustomPropertyDefinitionDetailModel?> GetEventDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var hal = await _apiClient.GetEventCustomPropertyDefinitionByIdAsync(id, cancellationToken: cancellationToken);
            return hal.ToModel();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    private async Task<CustomPropertyDefinitionDetailModel?> GetEventSessionDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var hal = await _apiClient.GetEventSessionCustomPropertyDefinitionByIdAsync(id, cancellationToken: cancellationToken);
            return hal.ToModel();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    private static IReadOnlyList<CustomPropertyDefinitionDetailModel> NonNull(
        IEnumerable<CustomPropertyDefinitionDetailModel?> definitions)
    {
        var result = new List<CustomPropertyDefinitionDetailModel>();
        foreach (var definition in definitions)
        {
            if (definition is not null)
            {
                result.Add(definition);
            }
        }

        return result;
    }
}

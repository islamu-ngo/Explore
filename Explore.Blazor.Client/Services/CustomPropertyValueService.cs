// ABOUTME: Implementation of ICustomPropertyValueService wrapping IEventApiClient.
// ABOUTME: Handles getting and setting single and multi values for Event and EventSession.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.CustomProperties;
using Explore.Blazor.Client.Models.CustomProperties;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class CustomPropertyValueService : ICustomPropertyValueService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<CustomPropertyValueService> _logger;

    public CustomPropertyValueService(IEventApiClient apiClient, ILogger<CustomPropertyValueService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<CustomPropertyValueModel>> GetEventValuesAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetEventCustomPropertyValuesAsync(eventId, cancellationToken: cancellationToken);
        return response?.Select(CustomPropertyValueModel.FromEventDto).ToList() ?? new List<CustomPropertyValueModel>();
    }

    public async Task<BaseCommandResponseOfGuid?> SetEventValueAsync(Guid definitionId, Guid eventId, CustomPropertyValueModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new SetEventCustomPropertyValueDto
            {
                EventCustomPropertyDefinitionId = definitionId,
                EventId = eventId,
                Ordinal = model.Ordinal,
                TextValue = model.TextValue,
                NumberValue = model.NumberValue,
                BooleanValue = model.BooleanValue,
                DateTimeValue = model.DateTimeValue,
                OptionId = model.OptionId
            };

            return await _apiClient.SetEventCustomPropertyValueAsync(body, cancellationToken: cancellationToken);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[CP VAL] API error setting value for Event {EventId} / Def {DefinitionId}: {Detail}", eventId, definitionId, ex.Result?.Detail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP VAL] Unexpected error setting value for Event {EventId} / Def {DefinitionId}", eventId, definitionId);
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> SetEventMultiValuesAsync(Guid definitionId, Guid eventId, IEnumerable<CustomPropertyValueModel> models, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new SetEventCustomPropertyMultiValuesDto
            {
                DefinitionId = definitionId,
                EventId = eventId,
                Values = models.Select((m, i) => new SetEventCustomPropertyValueDto
                {
                    EventCustomPropertyDefinitionId = definitionId,
                    EventId = eventId,
                    Ordinal = i,
                    TextValue = m.TextValue,
                    NumberValue = m.NumberValue,
                    BooleanValue = m.BooleanValue,
                    DateTimeValue = m.DateTimeValue,
                    OptionId = m.OptionId
                }).ToList()
            };

            return await _apiClient.SetEventCustomPropertyMultiValuesAsync(body, cancellationToken: cancellationToken);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[CP VAL] API error setting multi values for Event {EventId} / Def {DefinitionId}: {Detail}", eventId, definitionId, ex.Result?.Detail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP VAL] Unexpected error setting multi values for Event {EventId} / Def {DefinitionId}", eventId, definitionId);
            throw;
        }
    }

    public async Task<IReadOnlyList<CustomPropertyValueModel>> GetEventSessionValuesAsync(Guid eventSessionId, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetEventSessionCustomPropertyValuesAsync(eventSessionId, cancellationToken: cancellationToken);
        return response?.Select(CustomPropertyValueModel.FromEventSessionDto).ToList() ?? new List<CustomPropertyValueModel>();
    }

    public async Task<BaseCommandResponseOfGuid?> SetEventSessionValueAsync(Guid definitionId, Guid eventSessionId, CustomPropertyValueModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new SetEventSessionCustomPropertyValueDto
            {
                EventSessionCustomPropertyDefinitionId = definitionId,
                EventSessionId = eventSessionId,
                Ordinal = model.Ordinal,
                TextValue = model.TextValue,
                NumberValue = model.NumberValue,
                BooleanValue = model.BooleanValue,
                DateTimeValue = model.DateTimeValue,
                OptionId = model.OptionId
            };

            return await _apiClient.SetEventSessionCustomPropertyValueAsync(body, cancellationToken: cancellationToken);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[CP VAL] API error setting value for EventSession {EventSessionId} / Def {DefinitionId}: {Detail}", eventSessionId, definitionId, ex.Result?.Detail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP VAL] Unexpected error setting value for EventSession {EventSessionId} / Def {DefinitionId}", eventSessionId, definitionId);
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> SetEventSessionMultiValuesAsync(Guid definitionId, Guid eventSessionId, IEnumerable<CustomPropertyValueModel> models, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new SetEventSessionCustomPropertyMultiValuesDto
            {
                DefinitionId = definitionId,
                EventSessionId = eventSessionId,
                Values = models.Select((m, i) => new SetEventSessionCustomPropertyValueDto
                {
                    EventSessionCustomPropertyDefinitionId = definitionId,
                    EventSessionId = eventSessionId,
                    Ordinal = i,
                    TextValue = m.TextValue,
                    NumberValue = m.NumberValue,
                    BooleanValue = m.BooleanValue,
                    DateTimeValue = m.DateTimeValue,
                    OptionId = m.OptionId
                }).ToList()
            };

            return await _apiClient.SetEventSessionCustomPropertyMultiValuesAsync(body, cancellationToken: cancellationToken);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "[CP VAL] API error setting multi values for EventSession {EventSessionId} / Def {DefinitionId}: {Detail}", eventSessionId, definitionId, ex.Result?.Detail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP VAL] Unexpected error setting multi values for EventSession {EventSessionId} / Def {DefinitionId}", eventSessionId, definitionId);
            throw;
        }
    }
}

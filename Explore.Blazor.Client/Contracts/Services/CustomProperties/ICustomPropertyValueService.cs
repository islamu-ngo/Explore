// ABOUTME: Interface for retrieving and saving custom property values.
// ABOUTME: Wraps EventApiClient for both Event and EventSession property values.

using Explore.Blazor.Client.Models.CustomProperties;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Contracts.Services.CustomProperties;

public interface ICustomPropertyValueService
{
    Task<IReadOnlyList<CustomPropertyValueModel>> GetEventValuesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<BaseCommandResponse<Guid>?> SetEventValueAsync(Guid definitionId, Guid eventId, CustomPropertyValueModel model, CancellationToken cancellationToken = default);
    Task<BaseCommandResponse<Guid>?> SetEventMultiValuesAsync(Guid definitionId, Guid eventId, IEnumerable<CustomPropertyValueModel> models, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomPropertyValueModel>> GetEventSessionValuesAsync(Guid eventSessionId, CancellationToken cancellationToken = default);
    Task<BaseCommandResponse<Guid>?> SetEventSessionValueAsync(Guid definitionId, Guid eventSessionId, CustomPropertyValueModel model, CancellationToken cancellationToken = default);
    Task<BaseCommandResponse<Guid>?> SetEventSessionMultiValuesAsync(Guid definitionId, Guid eventSessionId, IEnumerable<CustomPropertyValueModel> models, CancellationToken cancellationToken = default);
}

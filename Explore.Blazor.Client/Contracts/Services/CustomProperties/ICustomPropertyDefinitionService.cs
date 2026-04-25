// ABOUTME: Client contract for CRUD operations on custom-property definitions.
// ABOUTME: Wraps IEventApiClient with HAL unwrap + error handling.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.CustomProperties;
using Explore.Blazor.Client.Models.Responses;
using Explore.Domain.Enums;

namespace Explore.Blazor.Client.Contracts.Services.CustomProperties;

public interface ICustomPropertyDefinitionService
{
    Task<PaginatedResult<CustomPropertyDefinitionListModel>> GetDefinitionsAsync(
        EntityTypeName entityTypeName,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<CustomPropertyDefinitionDetailModel?> GetDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponse<Guid>?> CreateDefinitionAsync(
        CreateCustomPropertyDefinitionDto body,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponse<Guid>?> UpdateDefinitionAsync(
        Guid id,
        UpdateCustomPropertyDefinitionDto body,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

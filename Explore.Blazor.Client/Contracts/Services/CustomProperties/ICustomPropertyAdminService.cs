// ABOUTME: Client contract for tenant-scoped governance operations on custom-property definitions.
// ABOUTME: Wraps IEventApiClient with HAL unwrap + error handling so admin pages bind to plain models.

using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.CustomProperties;
using Explore.Blazor.Client.Models.Responses;
using Explore.Domain.Enums;

namespace Explore.Blazor.Client.Contracts.Services.CustomProperties;

public interface ICustomPropertyAdminService
{
    Task<PaginatedResult<CustomPropertyDefinitionListModel>> GetDefinitionsAsync(
        EntityTypeName entityTypeName,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<CustomPropertyDefinitionDetailModel?> GetDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponse<Guid>?> UpdateDefinitionFlagsAsync(
        DefinitionFlagUpdateModel update,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponse<Guid>> UpdateManyDefinitionFlagsAsync(
        IReadOnlyList<DefinitionFlagUpdateModel> updates,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<CustomPropertyGovernanceRowModel>> GetGovernanceReportAsync(
        Guid? tenantId = null,
        string? scope = null,
        PromotionRecommendation? recommendation = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectionStatusModel>> GetEventProjectionStatusAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectionStatusModel>> GetSessionProjectionStatusAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<ProjectionDirtyScopeModel>> GetDirtyScopesAsync(
        Guid? tenantId = null,
        string? projectionName = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponse<RebuildProjectionResult>?> RebuildEventProjectionAsync(
        Guid tenantId,
        int? batchSize = null,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponse<RebuildProjectionResult>?> RebuildSessionProjectionAsync(
        Guid tenantId,
        int? batchSize = null,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponse<DrainDirtyScopesResult>?> DrainDirtyScopesAsync(
        Guid tenantId,
        string? projectionName = null,
        CancellationToken cancellationToken = default);
}

// ABOUTME: Tenant-scoped custom-property governance service — wraps IEventApiClient with HAL unwrap + logging.
// ABOUTME: Single source of truth for admin pages interacting with Layer 3 definitions and projection runtime.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.CustomProperties;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.CustomProperties;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class CustomPropertyAdminService : ICustomPropertyAdminService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<CustomPropertyAdminService> _logger;

    public CustomPropertyAdminService(IEventApiClient apiClient, ILogger<CustomPropertyAdminService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResult<CustomPropertyDefinitionListDto>> GetDefinitionsAsync(
        EntityTypeName entityTypeName,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetCustomPropertyDefinitionsAsync(
                entityTypeName,
                pageNumber,
                pageSize,
                cancellationToken: cancellationToken);

            return response.ToPaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Failed to fetch definitions for {EntityType}", entityTypeName);
            return PaginatedResult<CustomPropertyDefinitionListDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<CustomPropertyDefinitionDto?> GetDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hal = await _apiClient.GetCustomPropertyDefinitionByIdAsync(id, cancellationToken: cancellationToken);
            return hal.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Failed to fetch definition {DefinitionId}", id);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateDefinitionFlagsAsync(
        DefinitionFlagUpdateModel update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        try
        {
            var hal = await _apiClient.GetCustomPropertyDefinitionByIdAsync(update.DefinitionId, cancellationToken: cancellationToken);
            var detail = hal.ToDto();
            if (detail is null)
            {
                return new BaseCommandResponseOfGuid
                {
                    Success = false,
                    Message = "Definition not found."
                };
            }

            var dto = BuildUpdateDto(detail, update);
            var response = await _apiClient.UpdateCustomPropertyDefinitionAsync(update.DefinitionId, dto, cancellationToken: cancellationToken);
            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "[CP ADMIN] Flag update rejected for {DefinitionId} — status {Status}", update.DefinitionId, ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = TryReadErrorMessage(ex.Response) ?? $"API error ({ex.StatusCode})."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Flag update failed for {DefinitionId}", update.DefinitionId);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid> UpdateManyDefinitionFlagsAsync(
        IReadOnlyList<DefinitionFlagUpdateModel> updates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "No updates supplied."
            };
        }

        var failures = new List<string>();
        foreach (var update in updates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await UpdateDefinitionFlagsAsync(update, cancellationToken);
            if (result is null || result.Success != true)
            {
                failures.Add($"{update.DefinitionId}: {result?.Message ?? "unknown error"}");
            }
        }

        return new BaseCommandResponseOfGuid
        {
            Success = failures.Count == 0,
            Message = failures.Count == 0
                ? $"Updated {updates.Count} definition(s)."
                : $"Updated {updates.Count - failures.Count} of {updates.Count}; {failures.Count} failed.",
            Errors = failures.Count == 0 ? null : failures
        };
    }

    public async Task<PaginatedResult<CustomPropertyGovernanceRowDto>> GetGovernanceReportAsync(
        Guid? tenantId = null,
        string? scope = null,
        PromotionRecommendation? recommendation = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetCustomPropertyGovernanceReportAsync(
                tenantId,
                scope,
                recommendation,
                pageNumber,
                pageSize,
                cancellationToken: cancellationToken);

            if (response is null)
                return PaginatedResult<CustomPropertyGovernanceRowDto>.Empty(pageNumber, pageSize);

            return new PaginatedResult<CustomPropertyGovernanceRowDto>
            {
                Items = response.Items?.ToList() ?? [],
                PageNumber = response.PageNumber ?? pageNumber,
                PageSize = response.PageSize ?? pageSize,
                TotalCount = response.TotalCount ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Governance report fetch failed");
            return PaginatedResult<CustomPropertyGovernanceRowDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<IReadOnlyList<HalResourceOfProjectionStatusDto>> GetEventProjectionStatusAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetCustomPropertyProjectionStatusAsync(tenantId, cancellationToken: cancellationToken);
            return response?._embedded?.Items?.ToList() ?? new List<HalResourceOfProjectionStatusDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Event projection status fetch failed");
            return Array.Empty<HalResourceOfProjectionStatusDto>();
        }
    }

    public async Task<IReadOnlyList<HalResourceOfProjectionStatusDto>> GetSessionProjectionStatusAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetSessionCustomPropertyProjectionStatusAsync(tenantId, cancellationToken: cancellationToken);
            return response?._embedded?.Items?.ToList() ?? new List<HalResourceOfProjectionStatusDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Session projection status fetch failed");
            return Array.Empty<HalResourceOfProjectionStatusDto>();
        }
    }

    public async Task<PaginatedResult<HalResourceOfProjectionDirtyScopeDto>> GetDirtyScopesAsync(
        Guid? tenantId = null,
        string? projectionName = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetCustomPropertyProjectionDirtyScopesAsync(
                tenantId,
                projectionName,
                pageNumber,
                pageSize,
                cancellationToken: cancellationToken);

            if (response is null)
                return PaginatedResult<HalResourceOfProjectionDirtyScopeDto>.Empty(pageNumber, pageSize);

            return new PaginatedResult<HalResourceOfProjectionDirtyScopeDto>
            {
                Items = response._embedded?.Items?.ToList() ?? [],
                PageNumber = response.PageNumber ?? pageNumber,
                PageSize = response.PageSize ?? pageSize,
                TotalCount = response.TotalCount ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Dirty scopes fetch failed");
            return PaginatedResult<HalResourceOfProjectionDirtyScopeDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<BaseCommandResponseOfRebuildProjectionResponseDto?> RebuildEventProjectionAsync(
        Guid tenantId,
        int? batchSize = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RebuildProjectionRequestDto
            {
                TenantId = tenantId,
                BatchSize = batchSize
            };

            return await _apiClient.RebuildCustomPropertyProjectionAsync(request, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Event projection rebuild failed for {TenantId}", tenantId);
            return new BaseCommandResponseOfRebuildProjectionResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseCommandResponseOfRebuildProjectionResponseDto?> RebuildSessionProjectionAsync(
        Guid tenantId,
        int? batchSize = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RebuildProjectionRequestDto
            {
                TenantId = tenantId,
                BatchSize = batchSize
            };

            return await _apiClient.RebuildSessionCustomPropertyProjectionAsync(request, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Session projection rebuild failed for {TenantId}", tenantId);
            return new BaseCommandResponseOfRebuildProjectionResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseCommandResponseOfDrainDirtyScopesResponseDto?> DrainDirtyScopesAsync(
        Guid tenantId,
        string? projectionName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DrainDirtyScopesRequestDto
            {
                TenantId = tenantId,
                ProjectionName = projectionName
            };

            return await _apiClient.DrainCustomPropertyProjectionDirtyScopesAsync(request, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Dirty-scope drain failed for {TenantId}", tenantId);
            return new BaseCommandResponseOfDrainDirtyScopesResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static UpdateCustomPropertyDefinitionDto BuildUpdateDto(
        CustomPropertyDefinitionDto detail,
        DefinitionFlagUpdateModel update)
    {
        return new UpdateCustomPropertyDefinitionDto
        {
            Id = detail.Id,
            ExpectedConcurrencyStamp = detail.ConcurrencyStamp,
            EntityTypeName = (EntityTypeName?)detail.EntityTypeName,
            Namespace = detail.Namespace,
            Key = detail.Key,
            DisplayName = detail.DisplayName,
            Description = detail.Description,
            PropertyType = (PropertyType?)detail.PropertyType,
            IsRequired = detail.IsRequired,
            IsMulti = detail.IsMulti,
            IsActive = detail.IsActive,
            SortOrder = detail.SortOrder,
            ExposureLevel = update.ExposureLevel,
            IsSearchable = update.IsSearchable,
            IsFilterable = update.IsFilterable,
            IsExportable = update.IsExportable,
            IsModerationRelevant = update.IsModerationRelevant,
            IsAnalyticsRelevant = update.IsAnalyticsRelevant,
            IsSystemOwned = detail.IsSystemOwned,
            DefaultTextValue = detail.DefaultTextValue,
            DefaultNumberValue = detail.DefaultNumberValue.HasValue ? (double)detail.DefaultNumberValue.Value : null,
            DefaultBooleanValue = detail.DefaultBooleanValue,
            DefaultDateTimeValue = detail.DefaultDateTimeValue,
            MinLength = detail.MinLength,
            MaxLength = detail.MaxLength,
            RegexPattern = detail.RegexPattern,
            MinNumber = detail.MinNumber.HasValue ? (double)detail.MinNumber.Value : null,
            MaxNumber = detail.MaxNumber.HasValue ? (double)detail.MaxNumber.Value : null,
            MinDateTime = detail.MinDateTime,
            MaxDateTime = detail.MaxDateTime,
            AllowedUrlSchemes = detail.AllowedUrlSchemes,
            Options = detail.Options?.Select(o => new CreateCustomPropertyOptionDto
            {
                Namespace = o.Namespace,
                Key = o.Key,
                DisplayName = o.DisplayName,
                Description = o.Description,
                Value = o.Value,
                IsDefault = o.IsDefault == true,
                IsActive = o.IsActive == true,
                SortOrder = o.SortOrder
            }).ToList() ?? []
        };
    }

    private static string? TryReadErrorMessage(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                return msg.GetString();
            if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString();
        }
        catch (JsonException)
        {
        }

        return null;
    }
}

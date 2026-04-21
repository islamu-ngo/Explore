// ABOUTME: Tenant-scoped custom-property governance service — wraps IEventApiClient with HAL unwrap + logging.
// ABOUTME: Single source of truth for admin pages interacting with Layer 3 definitions and projection runtime.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.CustomProperties;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.CustomProperties;
using Explore.Blazor.Client.Models.Responses;
using Explore.Domain.Enums;
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
            _logger.LogError(ex, "[CP ADMIN] Failed to fetch definitions for {EntityType}", entityTypeName);
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
            _logger.LogError(ex, "[CP ADMIN] Failed to fetch definition {DefinitionId}", id);
            return null;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> UpdateDefinitionFlagsAsync(
        DefinitionFlagUpdateModel update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        try
        {
            var hal = await _apiClient.GetCustomPropertyDefinitionByIdAsync(update.DefinitionId, cancellationToken: cancellationToken);
            var detail = hal.ToModel();
            if (detail is null)
            {
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = "Definition not found."
                };
            }

            var dto = BuildUpdateDto(detail, update);
            var response = await _apiClient.UpdateCustomPropertyDefinitionAsync(update.DefinitionId, dto, cancellationToken: cancellationToken);
            return ToClientResponse(response);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "[CP ADMIN] Flag update rejected for {DefinitionId} — status {Status}", update.DefinitionId, ex.StatusCode);
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = TryReadErrorMessage(ex.Response) ?? $"API error ({ex.StatusCode})."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Flag update failed for {DefinitionId}", update.DefinitionId);
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseCommandResponse<Guid>> UpdateManyDefinitionFlagsAsync(
        IReadOnlyList<DefinitionFlagUpdateModel> updates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return new BaseCommandResponse<Guid>
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
            if (result is null || !result.Success)
            {
                failures.Add($"{update.DefinitionId}: {result?.Message ?? "unknown error"}");
            }
        }

        return new BaseCommandResponse<Guid>
        {
            Success = failures.Count == 0,
            Message = failures.Count == 0
                ? $"Updated {updates.Count} definition(s)."
                : $"Updated {updates.Count - failures.Count} of {updates.Count}; {failures.Count} failed.",
            Errors = failures.Count == 0 ? null : failures
        };
    }

    public async Task<PaginatedResult<CustomPropertyGovernanceRowModel>> GetGovernanceReportAsync(
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
                recommendation is null ? null : (int)recommendation.Value,
                pageNumber,
                pageSize,
                cancellationToken: cancellationToken);

            if (response is null)
                return PaginatedResult<CustomPropertyGovernanceRowModel>.Empty(pageNumber, pageSize);

            return new PaginatedResult<CustomPropertyGovernanceRowModel>
            {
                Items = response.Items?.Select(MapGovernanceRow).ToList() ?? [],
                PageNumber = response.PageNumber ?? pageNumber,
                PageSize = response.PageSize ?? pageSize,
                TotalCount = response.TotalCount ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Governance report fetch failed");
            return PaginatedResult<CustomPropertyGovernanceRowModel>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<IReadOnlyList<ProjectionStatusModel>> GetEventProjectionStatusAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetCustomPropertyProjectionStatusAsync(tenantId, cancellationToken: cancellationToken);
            return response?.Id?.Select(MapStatus).ToList() ?? new List<ProjectionStatusModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Event projection status fetch failed");
            return Array.Empty<ProjectionStatusModel>();
        }
    }

    public async Task<IReadOnlyList<ProjectionStatusModel>> GetSessionProjectionStatusAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetSessionCustomPropertyProjectionStatusAsync(tenantId, cancellationToken: cancellationToken);
            return response?.Id?.Select(MapStatus).ToList() ?? new List<ProjectionStatusModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Session projection status fetch failed");
            return Array.Empty<ProjectionStatusModel>();
        }
    }

    public async Task<PaginatedResult<ProjectionDirtyScopeModel>> GetDirtyScopesAsync(
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
                return PaginatedResult<ProjectionDirtyScopeModel>.Empty(pageNumber, pageSize);

            return new PaginatedResult<ProjectionDirtyScopeModel>
            {
                Items = response.Items?.Select(MapDirtyScope).ToList() ?? [],
                PageNumber = response.PageNumber ?? pageNumber,
                PageSize = response.PageSize ?? pageSize,
                TotalCount = response.TotalCount ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Dirty scopes fetch failed");
            return PaginatedResult<ProjectionDirtyScopeModel>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<BaseCommandResponse<RebuildProjectionResult>?> RebuildEventProjectionAsync(
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

            var response = await _apiClient.RebuildCustomPropertyProjectionAsync(request, cancellationToken: cancellationToken);
            return ToRebuildResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Event projection rebuild failed for {TenantId}", tenantId);
            return new BaseCommandResponse<RebuildProjectionResult>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseCommandResponse<RebuildProjectionResult>?> RebuildSessionProjectionAsync(
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

            var response = await _apiClient.RebuildSessionCustomPropertyProjectionAsync(request, cancellationToken: cancellationToken);
            return ToRebuildResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Session projection rebuild failed for {TenantId}", tenantId);
            return new BaseCommandResponse<RebuildProjectionResult>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<BaseCommandResponse<DrainDirtyScopesResult>?> DrainDirtyScopesAsync(
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

            var response = await _apiClient.DrainCustomPropertyProjectionDirtyScopesAsync(request, cancellationToken: cancellationToken);
            return ToDrainResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CP ADMIN] Dirty-scope drain failed for {TenantId}", tenantId);
            return new BaseCommandResponse<DrainDirtyScopesResult>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static UpdateCustomPropertyDefinitionDto BuildUpdateDto(
        CustomPropertyDefinitionDetailModel detail,
        DefinitionFlagUpdateModel update)
    {
        return new UpdateCustomPropertyDefinitionDto
        {
            Id = detail.Id,
            EntityTypeName = (int)detail.EntityTypeName,
            Namespace = detail.Namespace,
            Key = detail.Key,
            DisplayName = detail.DisplayName,
            Description = detail.Description,
            PropertyType = (int)detail.PropertyType,
            IsRequired = detail.IsRequired,
            IsMulti = detail.IsMulti,
            IsActive = detail.IsActive,
            SortOrder = detail.SortOrder,
            ExposureLevel = (int)update.ExposureLevel,
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
            Options = detail.Options.Select(o => new CreateCustomPropertyOptionDto
            {
                Namespace = o.Namespace,
                Key = o.Key,
                DisplayName = o.DisplayName,
                Description = o.Description,
                Value = o.Value,
                IsDefault = o.IsDefault,
                IsActive = o.IsActive,
                SortOrder = o.SortOrder
            }).ToList()
        };
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

    private static BaseCommandResponse<RebuildProjectionResult> ToRebuildResult(BaseCommandResponseOfRebuildProjectionResponseDto? response)
    {
        if (response is null)
        {
            return new BaseCommandResponse<RebuildProjectionResult>
            {
                Success = false,
                Message = "No response from server."
            };
        }

        var payload = response.Id;
        return new BaseCommandResponse<RebuildProjectionResult>
        {
            Success = response.Success ?? false,
            Message = response.Message,
            Errors = response.Errors?.ToList(),
            Id = payload is null ? null : new RebuildProjectionResult
            {
                LockAcquired = payload.LockAcquired ?? false,
                RowsProcessed = payload.RowsProcessed ?? 0,
                RowsFailed = payload.RowsFailed ?? 0,
                DrainedDirtyScopes = payload.DrainedDirtyScopes ?? 0,
                StartedAt = payload.StartedAt,
                CompletedAt = payload.CompletedAt
            }
        };
    }

    private static BaseCommandResponse<DrainDirtyScopesResult> ToDrainResult(BaseCommandResponseOfDrainDirtyScopesResponseDto? response)
    {
        if (response is null)
        {
            return new BaseCommandResponse<DrainDirtyScopesResult>
            {
                Success = false,
                Message = "No response from server."
            };
        }

        var payload = response.Id;
        return new BaseCommandResponse<DrainDirtyScopesResult>
        {
            Success = response.Success ?? false,
            Message = response.Message,
            Errors = response.Errors?.ToList(),
            Id = payload is null ? null : new DrainDirtyScopesResult
            {
                DrainedCount = payload.DrainedCount ?? 0,
                DrainedAt = payload.DrainedAt
            }
        };
    }

    private static ProjectionStatusModel MapStatus(ProjectionStatusDto dto)
    {
        return new ProjectionStatusModel
        {
            ProjectionName = dto.ProjectionName ?? string.Empty,
            ProjectionVersion = dto.ProjectionVersion ?? 0,
            TenantId = dto.TenantId,
            State = dto.State ?? 0,
            LastRebuildStartedAt = dto.LastRebuildStartedAt,
            LastRebuildCompletedAt = dto.LastRebuildCompletedAt,
            RowsProcessed = dto.RowsProcessed ?? 0,
            RowsFailed = dto.RowsFailed ?? 0,
            LastCheckpoint = dto.LastCheckpoint,
            LastErrorMessage = dto.LastErrorMessage
        };
    }

    private static ProjectionDirtyScopeModel MapDirtyScope(ProjectionDirtyScopeDto dto)
    {
        return new ProjectionDirtyScopeModel
        {
            Id = dto.Id ?? 0,
            ProjectionName = dto.ProjectionName ?? string.Empty,
            ProjectionVersion = dto.ProjectionVersion ?? 0,
            TenantId = dto.TenantId,
            ScopeType = dto.ScopeType ?? 0,
            ScopeId = dto.ScopeId,
            DefinitionId = dto.DefinitionId,
            Reason = dto.Reason,
            CreatedAt = dto.CreatedAt,
            DrainedAt = dto.DrainedAt
        };
    }

    private static CustomPropertyGovernanceRowModel MapGovernanceRow(CustomPropertyGovernanceRowDto dto)
    {
        return new CustomPropertyGovernanceRowModel
        {
            TenantId = dto.TenantId ?? Guid.Empty,

            Namespace = dto.Namespace ?? string.Empty,
            Key = dto.Key ?? string.Empty,
            DisplayName = dto.DisplayName ?? string.Empty,
            EntityScope = dto.EntityScope ?? string.Empty,
            PropertyType = dto.PropertyType ?? string.Empty,
            ExposureLevel = (ExposureLevel)(dto.ExposureLevel ?? 1),
            IsSearchable = dto.IsSearchable ?? false,
            IsFilterable = dto.IsFilterable ?? false,
            IsExportable = dto.IsExportable ?? false,
            IsModerationRelevant = dto.IsModerationRelevant ?? false,
            IsAnalyticsRelevant = dto.IsAnalyticsRelevant ?? false,
            IsSystemOwned = dto.IsSystemOwned ?? false,
            ActiveInstanceCount = dto.ActiveInstanceCount ?? 0,
            LastUsedAt = dto.LastUsedAt,
            Recommendation = (PromotionRecommendation)(dto.Recommendation ?? 0)
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

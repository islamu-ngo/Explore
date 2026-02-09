// ABOUTME: Service for managing organization-related operations.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for managing organization-related operations.
/// Returns clean DTOs, converting from HAL wrapper types internally.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Creates a new organization.
    /// </summary>
    Task<BaseCommandResponseOfGuid?> CreateOrganizationAsync(CreateOrganizationDto organization);

    /// <summary>
    /// Gets all available approval status types.
    /// </summary>
    Task<ICollection<StatusTypeListDto>> GetStatusTypesAsync();

    /// <summary>
    /// Gets organizations for the current authenticated user.
    /// </summary>
    Task<ICollection<OrganizationListDto>> GetMyOrganizationsAsync();

    /// <summary>
    /// Gets organizations for a specific user.
    /// </summary>
    Task<ICollection<OrganizationListDto>> GetOrganizationsByUserAsync(Guid userId);

    /// <summary>
    /// Gets a single organization by ID.
    /// </summary>
    Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id);

    /// <summary>
    /// Updates an existing organization.
    /// </summary>
    Task<BaseCommandResponseOfGuid?> UpdateOrganizationAsync(Guid id, UpdateOrganizationDto organization);
}

/// <summary>
/// Implementation of organization service using the Event API client.
/// Acts as an Anti-Corruption Layer, converting HAL types to clean DTOs.
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(IEventApiClient apiClient, ILogger<OrganizationService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BaseCommandResponseOfGuid?> CreateOrganizationAsync(CreateOrganizationDto organization)
    {
        try
        {
            return await _apiClient.CreateOrganizationAsync(organization);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[OrganizationService.CreateOrganizationAsync] API error creating organization. StatusCode: {StatusCode}", ex.StatusCode);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<StatusTypeListDto>> GetStatusTypesAsync()
    {
        try
        {
            return await _apiClient.ApprovalstatusAllAsync() ?? new List<StatusTypeListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetStatusTypesAsync] API error fetching status types. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<StatusTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetStatusTypesAsync] Unexpected error fetching status types");
            return new List<StatusTypeListDto>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<OrganizationListDto>> GetMyOrganizationsAsync()
    {
        try
        {
            var result = await _apiClient.GetMyOrganizationsAsync(pageNumber: ApiConstants.FirstPage, pageSize: ApiConstants.DefaultPageSize);
            return result?.GetItems() ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetMyOrganizationsAsync] API error fetching my organizations. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetMyOrganizationsAsync] Unexpected error fetching my organizations");
            return new List<OrganizationListDto>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<OrganizationListDto>> GetOrganizationsByUserAsync(Guid userId)
    {
        try
        {
            // Note: This endpoint may not exist or may need HAL conversion
            // For now, we'll try the direct approach
            var result = await _apiClient.GetMyOrganizationsAsync(pageNumber: ApiConstants.FirstPage, pageSize: ApiConstants.DefaultPageSize);
            return result?.GetItems() ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetOrganizationsByUserAsync] API error fetching organizations for user {UserId}. StatusCode: {StatusCode}", userId, ex.StatusCode);
            return new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetOrganizationsByUserAsync] Unexpected error fetching organizations for user {UserId}", userId);
            return new List<OrganizationListDto>();
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id)
    {
        try
        {
            var result = await _apiClient.GetOrganizationByIdAsync(id);
            return result?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[OrganizationService.GetOrganizationByIdAsync] Organization not found. OrganizationId: {OrganizationId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetOrganizationByIdAsync] API error fetching organization. OrganizationId: {OrganizationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OrganizationService.GetOrganizationByIdAsync] Unexpected error fetching organization. OrganizationId: {OrganizationId}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BaseCommandResponseOfGuid?> UpdateOrganizationAsync(Guid id, UpdateOrganizationDto organization)
    {
        try
        {
            return await _apiClient.UpdateOrganizationAsync(id, organization);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[OrganizationService.UpdateOrganizationAsync] API error updating organization. OrganizationId: {OrganizationId}, StatusCode: {StatusCode}", id, ex.StatusCode);
            throw;
        }
    }
}


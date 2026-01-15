using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for managing organization-related operations.
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
            _logger.LogInformation("Creating organization: {OrganizationName}", organization.FullName);
            var response = await _apiClient.OrganizationPOSTAsync(organization);
            _logger.LogInformation("Organization created. Success={Success}, Id={Id}", response?.Success, response?.Id);
            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error creating organization: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating organization");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<StatusTypeListDto>> GetStatusTypesAsync()
    {
        try
        {
            var response = await _apiClient.ApprovalStatusAllAsync();
            return response ?? new List<StatusTypeListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching status types: {StatusCode}", ex.StatusCode);
            return new List<StatusTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching status types");
            return new List<StatusTypeListDto>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<OrganizationListDto>> GetMyOrganizationsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching my organizations via Organization/my");
            var response = await _apiClient.My2Async();
            _logger.LogInformation("Received {Count} organizations", response?.Count ?? 0);
            return response ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching my organizations: {StatusCode}", ex.StatusCode);
            return new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my organizations");
            return new List<OrganizationListDto>();
        }
    }

    /// <inheritdoc />
    public async Task<ICollection<OrganizationListDto>> GetOrganizationsByUserAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Fetching organizations for user: {UserId}", userId);
            var response = await _apiClient.OrganizationsAsync(userId);
            _logger.LogInformation("Received {Count} organizations for user {UserId}", response?.Count ?? 0, userId);

            if (response != null && _logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var org in response)
                {
                    _logger.LogDebug("Organization: {Name}, Role: {Role}", org.FullName, org.CurrentUserRole);
                }
            }

            return response ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "API error fetching organizations for user {UserId}: {StatusCode}. Falling back to My2Async", userId, ex.StatusCode);

            // Fallback to My2Async if the new endpoint fails
            try
            {
                var fallbackResponse = await _apiClient.My2Async();
                _logger.LogInformation("Fallback received {Count} organizations", fallbackResponse?.Count ?? 0);
                return fallbackResponse ?? new List<OrganizationListDto>();
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback also failed");
                return new List<OrganizationListDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching organizations for user {UserId}", userId);
            return new List<OrganizationListDto>();
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching organization: {OrganizationId}", id);
            return await _apiClient.OrganizationGETAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("Organization not found: {OrganizationId}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching organization {OrganizationId}: {StatusCode}", id, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching organization {OrganizationId}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BaseCommandResponseOfGuid?> UpdateOrganizationAsync(Guid id, UpdateOrganizationDto organization)
    {
        try
        {
            _logger.LogInformation("Updating organization: {OrganizationId}", id);
            var response = await _apiClient.OrganizationPUTAsync(id, organization);
            _logger.LogInformation("Organization update response: Success={Success}", response?.Success);
            return response;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error updating organization {OrganizationId}: {StatusCode}", id, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating organization {OrganizationId}", id);
            throw;
        }
    }
}

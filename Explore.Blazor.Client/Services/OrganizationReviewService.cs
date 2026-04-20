using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for managing organization reviews.
/// </summary>
public interface IOrganizationReviewService
{
    Task<ICollection<OrganizationReviewDto>> GetReviewsByOrganizationId(Guid organizationId);
    Task<ICollection<OrganizationReviewDto>> GetReviewsByUserId(Guid userId);
    Task<BaseCommandResponseOfGuid?> CreateReview(CreateOrganizationReviewDto review);
}

/// <summary>
/// Implementation of organization review service.
/// </summary>
public class OrganizationReviewService : IOrganizationReviewService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<OrganizationReviewService> _logger;

    public OrganizationReviewService(IEventApiClient apiClient, ILogger<OrganizationReviewService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<OrganizationReviewDto>> GetReviewsByOrganizationId(Guid organizationId)
    {
        try
        {
            var response = await _apiClient.GetOrganizationReviewsByOrganizationAsync(organizationId);
            return response ?? new List<OrganizationReviewDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching reviews by organization: {StatusCode}", ex.StatusCode);
            return new List<OrganizationReviewDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews by organization");
            return new List<OrganizationReviewDto>();
        }
    }

    public async Task<ICollection<OrganizationReviewDto>> GetReviewsByUserId(Guid userId)
    {
        try
        {
            var response = await _apiClient.GetOrganizationReviewsByUserAsync(userId);
            return response ?? new List<OrganizationReviewDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching reviews by user: {StatusCode}", ex.StatusCode);
            return new List<OrganizationReviewDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews by user");
            return new List<OrganizationReviewDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateReview(CreateOrganizationReviewDto review)
    {
        try
        {
            return await _apiClient.CreateOrganizationReviewAsync(review);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error creating review: {StatusCode}", ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            throw;
        }
    }
}


using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IOrganizationReviewService
{
    Task<ICollection<OrganizationReviewDto>> GetReviewsByOrganizationId(Guid organizationId);
    Task<ICollection<OrganizationReviewDto>> GetReviewsByUserId(Guid userId);
    Task<BaseCommandResponseOfGuid?> CreateReview(CreateOrganizationReviewDto review);
}

public class OrganizationReviewService : IOrganizationReviewService
{
    private readonly IEventApiClient _apiClient;

    public OrganizationReviewService(IEventApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ICollection<OrganizationReviewDto>> GetReviewsByOrganizationId(Guid organizationId)
    {
        try
        {
            var response = await _apiClient.OrganizationReviewAllAsync(organizationId);
            return response ?? new List<OrganizationReviewDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error fetching reviews by organization: {ex.StatusCode} - {ex.Message}");
            return new List<OrganizationReviewDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching reviews by organization: {ex.Message}");
            return new List<OrganizationReviewDto>();
        }
    }

    public async Task<ICollection<OrganizationReviewDto>> GetReviewsByUserId(Guid userId)
    {
        try
        {
            var response = await _apiClient.UserAsync(userId);
            return response ?? new List<OrganizationReviewDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error fetching reviews by user: {ex.StatusCode} - {ex.Message}");
            return new List<OrganizationReviewDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching reviews by user: {ex.Message}");
            return new List<OrganizationReviewDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateReview(CreateOrganizationReviewDto review)
    {
        try
        {
            return await _apiClient.OrganizationReviewAsync(review);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error creating review: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating review: {ex.Message}");
            throw;
        }
    }
}

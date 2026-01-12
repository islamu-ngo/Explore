using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IOrganizationService
{
    Task<BaseCommandResponseOfGuid?> CreateOrganizationAsync(CreateOrganizationDto organization);
    Task<ICollection<StatusTypeListDto>> GetStatusTypesAsync();
    Task<ICollection<OrganizationListDto>> GetMyOrganizationsAsync();
    Task<ICollection<OrganizationListDto>> GetOrganizationsByUserAsync(Guid userId);
    Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id);
    Task<BaseCommandResponseOfGuid?> UpdateOrganizationAsync(Guid id, UpdateOrganizationDto organization);
}

public class OrganizationService : IOrganizationService
{
    private readonly IEventApiClient _apiClient;

    public OrganizationService(IEventApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<BaseCommandResponseOfGuid?> CreateOrganizationAsync(CreateOrganizationDto organization)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ORG SERVICE] ERROR: API client is null");
                return null;
            }
            
            Console.WriteLine($"[ORG SERVICE] Creating organization: {organization.FullName}");
            var response = await _apiClient.OrganizationPOSTAsync(organization);
            Console.WriteLine($"[ORG SERVICE] API Response: Success={response?.Success}, Id={response?.Id}");
            return response;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ORG SERVICE] API error creating organization: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORG SERVICE] Exception creating organization: {ex.Message}");
            throw;
        }
    }

    public async Task<ICollection<StatusTypeListDto>> GetStatusTypesAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ORG SERVICE] ERROR: API client is null");
                return new List<StatusTypeListDto>();
            }
            
            var response = await _apiClient.ApprovalStatusAllAsync();
            return response ?? new List<StatusTypeListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ORG SERVICE] API error fetching status types: {ex.StatusCode} - {ex.Message}");
            return new List<StatusTypeListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORG SERVICE] Error fetching status types: {ex.Message}");
            return new List<StatusTypeListDto>();
        }
    }

    public async Task<ICollection<OrganizationListDto>> GetMyOrganizationsAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ORG SERVICE] ERROR: API client is null");
                return new List<OrganizationListDto>();
            }
            
            Console.WriteLine("[ORG SERVICE] Fetching my organizations via Organization/my...");
            var response = await _apiClient.My2Async();
            Console.WriteLine($"[ORG SERVICE] Received {response?.Count ?? 0} organizations");
            return response ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ORG SERVICE] API error fetching my organizations: {ex.StatusCode} - {ex.Message}");
            return new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORG SERVICE] Error fetching my organizations: {ex.Message}");
            return new List<OrganizationListDto>();
        }
    }

    public async Task<ICollection<OrganizationListDto>> GetOrganizationsByUserAsync(Guid userId)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ORG SERVICE] ERROR: API client is null");
                return new List<OrganizationListDto>();
            }
            
            Console.WriteLine($"[ORG SERVICE] Fetching organizations for user: {userId} via User/{userId}/organizations...");
            var response = await _apiClient.OrganizationsAsync(userId);
            Console.WriteLine($"[ORG SERVICE] Received {response?.Count ?? 0} organizations for user {userId}");
            
            if (response != null)
            {
                foreach (var org in response)
                {
                    Console.WriteLine($"[ORG SERVICE] - Org: {org.FullName}, Role: {org.CurrentUserRole}");
                }
            }
            
            return response ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ORG SERVICE] API error fetching organizations for user {userId}: {ex.StatusCode} - {ex.Message}");
            
            // Fallback to My2Async if the new endpoint fails
            Console.WriteLine("[ORG SERVICE] Falling back to My2Async...");
            try
            {
                var fallbackResponse = await _apiClient.My2Async();
                Console.WriteLine($"[ORG SERVICE] Fallback received {fallbackResponse?.Count ?? 0} organizations");
                return fallbackResponse ?? new List<OrganizationListDto>();
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"[ORG SERVICE] Fallback also failed: {fallbackEx.Message}");
                return new List<OrganizationListDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORG SERVICE] Error fetching organizations for user {userId}: {ex.Message}");
            return new List<OrganizationListDto>();
        }
    }

    public async Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ORG SERVICE] ERROR: API client is null");
                return null;
            }
            
            Console.WriteLine($"[ORG SERVICE] Fetching organization: {id}");
            return await _apiClient.OrganizationGETAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Console.WriteLine($"[ORG SERVICE] Organization not found: {id}");
            return null;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ORG SERVICE] API error fetching organization: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORG SERVICE] Error fetching organization: {ex.Message}");
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateOrganizationAsync(Guid id, UpdateOrganizationDto organization)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ORG SERVICE] ERROR: API client is null");
                return null;
            }
            
            Console.WriteLine($"[ORG SERVICE] Updating organization: {id}");
            var response = await _apiClient.OrganizationPUTAsync(id, organization);
            Console.WriteLine($"[ORG SERVICE] API Response: Success={response?.Success}");
            return response;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ORG SERVICE] API error updating organization: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ORG SERVICE] Exception updating organization: {ex.Message}");
            throw;
        }
    }
}
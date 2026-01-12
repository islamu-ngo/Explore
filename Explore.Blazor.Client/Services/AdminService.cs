using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IAdminService
{
    Task<ICollection<OrganizationListDto>> GetOrganizationRequestsAsync();
    Task<OrganizationDto?> GetOrganizationDetailsAsync(Guid id);
    Task<bool> ApproveOrganizationAsync(Guid id);
    Task<bool> RejectOrganizationAsync(Guid id);
    Task<bool> RevertToPendingAsync(Guid id);
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync();
    Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync();
}

public class AdminService : IAdminService
{
    private readonly IEventApiClient _apiClient;

    public AdminService(IEventApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ICollection<OrganizationListDto>> GetOrganizationRequestsAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return new List<OrganizationListDto>();
            }
            
            Console.WriteLine("[ADMIN SERVICE] Fetching all organizations via API client");
            var response = await _apiClient.OrganizationAllAsync();
            Console.WriteLine($"[ADMIN SERVICE] Received {response?.Count ?? 0} organizations from API");
            return response ?? new List<OrganizationListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error fetching organizations: {ex.StatusCode} - {ex.Message}");
            return new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error: {ex.Message}");
            return new List<OrganizationListDto>();
        }
    }

    public async Task<OrganizationDto?> GetOrganizationDetailsAsync(Guid id)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return null;
            }
            
            return await _apiClient.OrganizationGETAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Console.WriteLine($"[ADMIN SERVICE] Organization not found: {id}");
            return null;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error fetching organization details: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error fetching organization details: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ApproveOrganizationAsync(Guid id)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return false;
            }
            
            // Status 2 = Approved
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 2 };
            Console.WriteLine($"[ADMIN SERVICE] Approving organization {id} with status 2");
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            Console.WriteLine("[ADMIN SERVICE] Organization approved successfully");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 204 || ex.StatusCode == 200)
        {
            Console.WriteLine($"[ADMIN SERVICE] Organization approved successfully (HTTP {ex.StatusCode})");
            return true;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error approving organization: {ex.StatusCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error approving organization: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RejectOrganizationAsync(Guid id)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return false;
            }
            
            // Status 3 = Rejected
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 3 };
            Console.WriteLine($"[ADMIN SERVICE] Rejecting organization {id} with status 3");
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            Console.WriteLine("[ADMIN SERVICE] Organization rejected successfully");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 204 || ex.StatusCode == 200)
        {
            Console.WriteLine($"[ADMIN SERVICE] Organization rejected successfully (HTTP {ex.StatusCode})");
            return true;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error rejecting organization: {ex.StatusCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error rejecting organization: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RevertToPendingAsync(Guid id)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return false;
            }
            
            // Status 1 = Pending
            var updateDto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 1 };
            Console.WriteLine($"[ADMIN SERVICE] Reverting organization {id} to pending with status 1");
            await _apiClient.UpdatestatustypeAsync(id, updateDto);
            Console.WriteLine("[ADMIN SERVICE] Organization reverted to pending successfully");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 204 || ex.StatusCode == 200)
        {
            Console.WriteLine($"[ADMIN SERVICE] Organization reverted to pending successfully (HTTP {ex.StatusCode})");
            return true;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error reverting organization: {ex.StatusCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error reverting organization: {ex.Message}");
            return false;
        }
    }

    public async Task<ICollection<EventTypeListDto>> GetEventTypesAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return new List<EventTypeListDto>();
            }
            
            Console.WriteLine("[ADMIN SERVICE] Fetching event types...");
            var response = await _apiClient.EventTypeAllAsync();
            Console.WriteLine($"[ADMIN SERVICE] Received {response?.Count ?? 0} event types");
            return response ?? new List<EventTypeListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error fetching event types: {ex.StatusCode} - {ex.Message}");
            return new List<EventTypeListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error fetching event types: {ex.Message}");
            return new List<EventTypeListDto>();
        }
    }

    public async Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return new List<AudienceGenderListDto>();
            }
            
            Console.WriteLine("[ADMIN SERVICE] Fetching audience genders...");
            var response = await _apiClient.AudienceGenderAllAsync();
            Console.WriteLine($"[ADMIN SERVICE] Received {response?.Count ?? 0} audience genders");
            return response ?? new List<AudienceGenderListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error fetching audience genders: {ex.StatusCode} - {ex.Message}");
            return new List<AudienceGenderListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error fetching audience genders: {ex.Message}");
            return new List<AudienceGenderListDto>();
        }
    }

    public async Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[ADMIN SERVICE] ERROR: API client is null");
                return new List<AudienceAgeListDto>();
            }
            
            Console.WriteLine("[ADMIN SERVICE] Fetching audience ages...");
            var response = await _apiClient.AudienceAgeAllAsync();
            Console.WriteLine($"[ADMIN SERVICE] Received {response?.Count ?? 0} audience ages");
            return response ?? new List<AudienceAgeListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] API error fetching audience ages: {ex.StatusCode} - {ex.Message}");
            return new List<AudienceAgeListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN SERVICE] Error fetching audience ages: {ex.Message}");
            return new List<AudienceAgeListDto>();
        }
    }
}

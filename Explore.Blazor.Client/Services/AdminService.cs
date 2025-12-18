using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;

namespace Explore.Blazor.Client.Services;

// DTO class needed for status updates
public class UpdateOrganizationStatusTypeDto
{
    public int ApprovalStatusId { get; set; }
}

public interface IAdminService
{
    Task<List<OrganizationListDto>> GetOrganizationRequestsAsync();
    Task<OrganizationListDto?> GetOrganizationDetailsAsync(Guid id);
    Task<bool> ApproveOrganizationAsync(Guid id);
    Task<bool> RejectOrganizationAsync(Guid id);
    Task<bool> RevertToPendingAsync(Guid id);
    Task<List<EventTypeListDto>> GetEventTypesAsync();
    Task<List<AudienceGenderListDto>> GetAudienceGendersAsync();
    Task<List<AudienceAgeListDto>> GetAudienceAgesAsync();
}

public class AdminService : IAdminService
{
    private readonly HttpClient _httpClient;

    public AdminService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<OrganizationListDto>> GetOrganizationRequestsAsync()
    {
        try
        {
            Console.WriteLine("Calling /bff/api/Organization");
            var response = await _httpClient.GetFromJsonAsync<List<OrganizationListDto>>("/bff/api/Organization");
            var organizations = response ?? new List<OrganizationListDto>();
            Console.WriteLine($"AdminService: Received {organizations.Count} organizations from API");
            return organizations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AdminService Error: {ex.Message}");
            Console.WriteLine($"Fout bij ophalen organisatie aanvragen: {ex.Message}");
            return new List<OrganizationListDto>();
        }
    }

    public async Task<OrganizationListDto?> GetOrganizationDetailsAsync(Guid id)
    {
        try
        {
            // We kunnen de organization details ophalen via de GetAll en dan filteren op ID
            // Of als er een GetById is, kunnen we die gebruiken en de data mappen
            var organizations = await GetOrganizationRequestsAsync();
            return organizations.FirstOrDefault(o => o.Id == id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij ophalen organisatie details: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ApproveOrganizationAsync(Guid id)
    {
        try
        {
            // Status 2 = Approved (volgens ERD status_type tabel)
            var updateDto = new UpdateOrganizationStatusTypeDto { StatusTypeId = 2 };
            Console.WriteLine($"Approving organization {id} with status 2");
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/admin/organizations/{id}/status", updateDto);
            Console.WriteLine($"Approve response status: {response.StatusCode}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Approve error: {errorContent}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij goedkeuren organisatie: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RejectOrganizationAsync(Guid id)
    {
        try
        {
            // Status 3 = Rejected (volgens ERD status_type tabel)
            var updateDto = new UpdateOrganizationStatusTypeDto { StatusTypeId = 3 };
            Console.WriteLine($"Rejecting organization {id} with status 3");
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/admin/organizations/{id}/status", updateDto);
            Console.WriteLine($"Reject response status: {response.StatusCode}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Reject error: {errorContent}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij afwijzen organisatie: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RevertToPendingAsync(Guid id)
    {
        try
        {
            // Status 1 = Pending (volgens ERD status_type tabel)
            var updateDto = new UpdateOrganizationStatusTypeDto { StatusTypeId = 1 };
            Console.WriteLine($"Reverting organization {id} to pending with status 1");
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/admin/organizations/{id}/status", updateDto);
            Console.WriteLine($"Revert response status: {response.StatusCode}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Revert error: {errorContent}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij terugzetten naar pending: {ex.Message}");
            return false;
        }
    }

    public async Task<List<EventTypeListDto>> GetEventTypesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<EventTypeListDto>>("/bff/api/EventType");
            return response ?? new List<EventTypeListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching event types: {ex.Message}");
            return new List<EventTypeListDto>();
        }
    }

    public async Task<List<AudienceGenderListDto>> GetAudienceGendersAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<AudienceGenderListDto>>("/bff/api/AudienceGender");
            return response ?? new List<AudienceGenderListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching audience genders: {ex.Message}");
            return new List<AudienceGenderListDto>();
        }
    }

    public async Task<List<AudienceAgeListDto>> GetAudienceAgesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<AudienceAgeListDto>>("/bff/api/AudienceAge");
            return response ?? new List<AudienceAgeListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching audience ages: {ex.Message}");
            return new List<AudienceAgeListDto>();
        }
    }
}
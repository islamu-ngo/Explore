using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;

namespace Explore.Blazor.Client.Services;

public interface IAdminService
{
    Task<List<AdminOrganizationListDto>> GetOrganizationRequestsAsync();
    Task<AdminOrganizationListDto?> GetOrganizationDetailsAsync(Guid id);
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

    public async Task<List<AdminOrganizationListDto>> GetOrganizationRequestsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<AdminOrganizationListDto>>("/bff/api/admin/organizations");
            return response ?? new List<AdminOrganizationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij ophalen organisatie aanvragen: {ex.Message}");
            return new List<AdminOrganizationListDto>();
        }
    }

    public async Task<AdminOrganizationListDto?> GetOrganizationDetailsAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AdminOrganizationListDto>($"/bff/api/admin/organizations/{id}");
            return response;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return null;
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
            var updateDto = new UpdateOrganizationStatusDto { StatusTypeId = 2 };
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/admin/organizations/{id}/status", updateDto);
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
            var updateDto = new UpdateOrganizationStatusDto { StatusTypeId = 3 };
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/admin/organizations/{id}/status", updateDto);
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
            var updateDto = new UpdateOrganizationStatusDto { StatusTypeId = 1 };
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/admin/organizations/{id}/status", updateDto);
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
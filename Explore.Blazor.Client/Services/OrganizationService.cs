using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;

namespace Explore.Blazor.Client.Services;

public interface IOrganizationService
{
    Task<OrganizationDto?> CreateOrganizationAsync(OrganizationCreateDto organization);
    Task<List<StatusTypeDto>> GetStatusTypesAsync();
}

public class OrganizationService : IOrganizationService
{
    private readonly HttpClient _httpClient;

    public OrganizationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OrganizationDto?> CreateOrganizationAsync(OrganizationCreateDto organization)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/bff/api/Organization", organization);
            
            if (response.IsSuccessStatusCode)
            {
                var createdOrganization = await response.Content.ReadFromJsonAsync<OrganizationDto>();
                return createdOrganization;
            }
            
            // Log de foutmelding voor debugging
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API fout bij aanmaken organisatie: {response.StatusCode} - {errorContent}");
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij aanmaken organisatie: {ex.Message}");
            return null;
        }
    }

    public async Task<List<StatusTypeDto>> GetStatusTypesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<StatusTypeDto>>("/bff/api/StatusType");
            return response ?? new List<StatusTypeDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij ophalen status types: {ex.Message}");
            return new List<StatusTypeDto>();
        }
    }
}
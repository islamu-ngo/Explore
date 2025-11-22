using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services;

public interface IOrganizationService
{
    Task<OrganizationDto?> CreateOrganizationAsync(OrganizationCreateDto organization);
    Task<List<OrganizationStatusTypeListDto>> GetStatusTypesAsync();
    Task<List<OrganizationListDto>> GetMyOrganizationsAsync();
    Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id);
    Task<bool> UpdateOrganizationAsync(Guid id, OrganizationCreateDto organization);
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
            Console.WriteLine($"Verzenden naar API: /bff/api/Organization");
            Console.WriteLine($"Data: {System.Text.Json.JsonSerializer.Serialize(organization)}");
            
            var response = await _httpClient.PostAsJsonAsync("/bff/api/Organization", organization);
            
            Console.WriteLine($"API Response Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                // API geeft een BaseCommandResponse<Guid> terug
                var commandResponse = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
                
                if (commandResponse != null && commandResponse.Success)
                {
                    var orgId = commandResponse.Id;
                    
                    if (orgId == Guid.Empty)
                    {
                        throw new Exception("API retourneerde geen geldig GUID");
                    }
                    
                    Console.WriteLine($"Organisatie succesvol aangemaakt met ID: {orgId}");
                    
                    // Maak een OrganizationDto met de bekende gegevens
                    var createdOrg = new OrganizationDto
                    {
                        Id = orgId,
                        FullName = organization.FullName,
                        WebsiteUrl = organization.WebsiteUrl,
                        Email = organization.Email,
                        Country = organization.Country,
                        City = organization.City,
                        Postcode = organization.Postcode,
                        Address = organization.Address,
                        StatusTypeId = 1, // Pending
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    return createdOrg;
                }
                else if (commandResponse != null && !commandResponse.Success)
                {
                    var errors = commandResponse.Errors != null 
                        ? string.Join(", ", commandResponse.Errors) 
                        : commandResponse.Message ?? "Onbekende fout";
                    Console.WriteLine($"API fout: {errors}");
                    throw new Exception(errors);
                }
            }
            
            // Log de foutmelding voor debugging
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API fout bij aanmaken organisatie: {response.StatusCode} - {errorContent}");
            throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception bij aanmaken organisatie: {ex.Message}");
            throw; // Gooi de exception door zodat de UI deze kan afhandelen
        }
    }

    public async Task<List<OrganizationStatusTypeListDto>> GetStatusTypesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<OrganizationStatusTypeListDto>>("/bff/api/StatusType");
            return response ?? new List<OrganizationStatusTypeListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching status types: {ex.Message}");
            return new List<OrganizationStatusTypeListDto>();
        }
    }

    public async Task<List<OrganizationListDto>> GetMyOrganizationsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<OrganizationListDto>>("/bff/api/Organization/my");
            return response ?? new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij ophalen mijn organisaties: {ex.Message}");
            return new List<OrganizationListDto>();
        }
    }

    public async Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id)
    {
        try
        {
            Console.WriteLine($"Fetching organization: /bff/api/Organization/{id}");
            var response = await _httpClient.GetFromJsonAsync<OrganizationDto>($"/bff/api/Organization/{id}");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching organization: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateOrganizationAsync(Guid id, OrganizationCreateDto organization)
    {
        try
        {
            Console.WriteLine($"Updating organization: /bff/api/Organization/{id}");
            Console.WriteLine($"Data: {System.Text.Json.JsonSerializer.Serialize(organization)}");
            
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/Organization/{id}", organization);
            
            Console.WriteLine($"API Response Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var commandResponse = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
                
                if (commandResponse != null && commandResponse.Success)
                {
                    Console.WriteLine($"Organization updated successfully");
                    return true;
                }
                else if (commandResponse != null && !commandResponse.Success)
                {
                    var errors = commandResponse.Errors != null 
                        ? string.Join(", ", commandResponse.Errors) 
                        : commandResponse.Message ?? "Unknown error";
                    Console.WriteLine($"API error: {errors}");
                    throw new Exception(errors);
                }
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API error updating organization: {response.StatusCode} - {errorContent}");
            throw new Exception($"HTTP {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception updating organization: {ex.Message}");
            throw;
        }
    }
}
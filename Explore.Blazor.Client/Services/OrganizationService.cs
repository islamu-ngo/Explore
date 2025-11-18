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
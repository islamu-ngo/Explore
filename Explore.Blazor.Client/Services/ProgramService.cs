using System.Net.Http.Json;
using System.Text.Json;
using Explore.Blazor.Client.Models.DTOs;

namespace Explore.Blazor.Client.Services;

public class RegistrationStatusDto
{
    public bool IsRegistered { get; set; }
}

public interface IProgramService
{
    Task<List<ProgramListDto>> GetAllProgramsAsync();
    Task<ProgramDto?> GetProgramByIdAsync(Guid id);
    Task<List<EventTypeListDto>> GetEventTypesAsync();
    Task<List<ProgramTypeListDto>> GetProgramTypesAsync();
    Task<bool> RegisterForProgramAsync(ProgramRegistrationDto registration);
    Task<bool> IsUserRegisteredAsync(Guid programId);
    Task<List<ProgramRegistrationListDto>> GetRegistrationsForProgramAsync(Guid programId);
    Task<List<ProgramRegistrationListDto>> GetMyRegistrationsAsync();
    Task<bool> UnregisterFromProgramAsync(Guid registrationId);
}

public class ProgramService : IProgramService
{
    private readonly HttpClient _httpClient;

    public ProgramService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProgramRegistrationListDto>> GetRegistrationsForProgramAsync(Guid programId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ProgramRegistrationListDto>>($"/bff/api/ProgramRegistration/program/{programId}");
            return response ?? new List<ProgramRegistrationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching registrations: {ex.Message}");
            return new List<ProgramRegistrationListDto>();
        }
    }

    public async Task<List<ProgramRegistrationListDto>> GetMyRegistrationsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ProgramRegistrationListDto>>("/bff/api/ProgramRegistration/my");
            return response ?? new List<ProgramRegistrationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching my registrations: {ex.Message}");
            return new List<ProgramRegistrationListDto>();
        }
    }

    public async Task<bool> UnregisterFromProgramAsync(Guid registrationId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/bff/api/ProgramRegistration/{registrationId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unregistering from program: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ProgramListDto>> GetAllProgramsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ProgramListDto>>("/bff/api/Program");
            return response ?? new List<ProgramListDto>();
        }
        catch
        {
            return new List<ProgramListDto>();
        }
    }

    public async Task<ProgramDto?> GetProgramByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ProgramDto>($"/bff/api/Program/{id}");
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<EventTypeListDto>> GetEventTypesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<EventTypeListDto>>("/bff/api/EventType");
            return response ?? new List<EventTypeListDto>();
        }
        catch
        {
            return new List<EventTypeListDto>();
        }
    }

    public async Task<List<ProgramTypeListDto>> GetProgramTypesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ProgramTypeListDto>>("/bff/api/ProgramType");
            return response ?? new List<ProgramTypeListDto>();
        }
        catch
        {
            return new List<ProgramTypeListDto>();
        }
    }

    public async Task<bool> RegisterForProgramAsync(ProgramRegistrationDto registration)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/bff/api/ProgramRegistration", registration);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsUserRegisteredAsync(Guid programId)
    {
        try
        {
            Console.WriteLine($"[ProgramService] Checking registration for program: {programId}");
            var response = await _httpClient.GetAsync($"/bff/api/ProgramRegistration/check/{programId}");
            Console.WriteLine($"[ProgramService] Response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ProgramService] Response content: {content}");
                
                var result = await response.Content.ReadFromJsonAsync<RegistrationStatusDto>();
                Console.WriteLine($"[ProgramService] Parsed result: IsRegistered = {result?.IsRegistered}");
                
                return result?.IsRegistered ?? false;
            }
            
            Console.WriteLine($"[ProgramService] HTTP error: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProgramService] Error checking registration: {ex.Message}");
            return false;
        }
    }
}

using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;

namespace Explore.Blazor.Client.Services;

public interface IProgramService
{
    Task<List<ProgramListDto>> GetAllProgramsAsync();
    Task<ProgramDto?> GetProgramByIdAsync(Guid id);
    Task<List<EventTypeListDto>> GetEventTypesAsync();
    Task<List<ProgramTypeListDto>> GetProgramTypesAsync();
}

public class ProgramService : IProgramService
{
    private readonly HttpClient _httpClient;

    public ProgramService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
}

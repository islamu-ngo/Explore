using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services;

public interface IUserService
{
    Task SyncUserAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<bool> UpdateUserAsync(UpdateUserDto userDto);
    Task<bool> DeleteUserAsync();
}

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SyncUserAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/bff/api/User/sync", null);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to sync user: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing user: {ex.Message}");
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>("/bff/api/User");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(UpdateUserDto userDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("/bff/api/User", userDto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/bff/api/User");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting user: {ex.Message}");
            return false;
        }
    }
}


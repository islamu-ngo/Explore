using System.Net.Http.Json;
using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Services;

public interface IUserService
{
    Task SyncUserAsync();
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
}

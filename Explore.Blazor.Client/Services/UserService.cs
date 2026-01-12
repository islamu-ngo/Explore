using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IUserService
{
    Task<BaseCommandResponseOfGuid?> SyncUserAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<BaseCommandResponseOfGuid?> UpdateUserAsync(UpdateUserDto userDto);
    Task<bool> DeleteUserAsync();
}

public class UserService : IUserService
{
    private readonly IEventApiClient _apiClient;

    public UserService(IEventApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<BaseCommandResponseOfGuid?> SyncUserAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[USER SERVICE] ERROR: API client is null");
                return new BaseCommandResponseOfGuid
                {
                    Success = false,
                    Message = "API client is null"
                };
            }
            
            Console.WriteLine("[USER SERVICE] Syncing user...");
            var response = await _apiClient.SyncAsync();
            Console.WriteLine($"[USER SERVICE] Sync result: Success={response?.Success}, Id={response?.Id}");
            return response;
        }
        catch (ApiException ex) when (ex.StatusCode == 200)
        {
            // NSwag sometimes throws when response body doesn't match expected schema
            // but the operation was successful (status 200)
            Console.WriteLine($"[USER SERVICE] Sync completed with status 200 but response parsing issue: {ex.Message}");
            return new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "User synced successfully"
            };
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[USER SERVICE] API error syncing user: {ex.StatusCode} - {ex.Message}");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[USER SERVICE] Error syncing user: {ex.Message}");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[USER SERVICE] ERROR: API client is null");
                return null;
            }
            
            Console.WriteLine("[USER SERVICE] Fetching current user...");
            var user = await _apiClient.UserGETAsync();
            Console.WriteLine($"[USER SERVICE] User found: {user?.Email}");
            return user;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Console.WriteLine("[USER SERVICE] User not found (404) - attempting auto-sync");
            
            // Auto-sync user if not found
            try
            {
                var syncResult = await SyncUserAsync();
                if (syncResult?.Success == true)
                {
                    Console.WriteLine("[USER SERVICE] Auto-sync successful, retrying GetCurrentUser");
                    await Task.Delay(100); // Wait for DB write to complete
                    return await _apiClient.UserGETAsync();
                }
                else
                {
                    Console.WriteLine($"[USER SERVICE] Auto-sync failed: {syncResult?.Message}");
                }
            }
            catch (Exception syncEx)
            {
                Console.WriteLine($"[USER SERVICE] Auto-sync exception: {syncEx.Message}");
            }
            
            return null;
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            Console.WriteLine("[USER SERVICE] User not authenticated (401)");
            return null;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[USER SERVICE] API error: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[USER SERVICE] Error getting user: {ex.Message}");
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateUserAsync(UpdateUserDto userDto)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[USER SERVICE] ERROR: API client is null");
                return null;
            }
            
            return await _apiClient.UserPUTAsync(userDto);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[USER SERVICE] API error updating user: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[USER SERVICE] Error updating user: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[USER SERVICE] ERROR: API client is null");
                return false;
            }
            
            await _apiClient.UserDELETEAsync();
            return true;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[USER SERVICE] API error deleting user: {ex.StatusCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[USER SERVICE] Error deleting user: {ex.Message}");
            return false;
        }
    }
}


// ABOUTME: Client service for authenticated user profile, sync, account, and admin-authority calls.
// ABOUTME: Wraps generated BFF API client failures into nullable/command response contracts for UI use.

using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for managing user-related operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Synchronizes the current authenticated user with the backend.
    /// </summary>
    Task<BaseCommandResponseOfGuid?> SyncUserAsync();

    /// <summary>
    /// Gets the current authenticated user's details.
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync();

    /// <summary>
    /// Gets the current authenticated user's DB-backed admin authority.
    /// </summary>
    Task<AdminAuthorityDto?> GetAdminAuthorityAsync();

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    Task<BaseCommandResponseOfGuid?> UpdateUserAsync(UpdateUserDto userDto);

    /// <summary>
    /// Deletes the current user's account.
    /// </summary>
    Task<bool> DeleteUserAsync();
}

/// <summary>
/// Implementation of user service using the Event API client.
/// </summary>
public class UserService : IUserService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<UserService> _logger;

    public UserService(IEventApiClient apiClient, ILogger<UserService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BaseCommandResponseOfGuid?> SyncUserAsync()
    {
        try
        {
            _logger.LogInformation("Syncing user");
            var response = await _apiClient.SyncUserAsync();
            _logger.LogInformation("Sync result: Success={Success}, Id={Id}", response?.Success, response?.Id);
            return response;
        }
        catch (ApiException ex) when (ex.StatusCode == 200)
        {
            // NSwag sometimes throws when response body doesn't match expected schema
            // but the operation was successful (status 200)
            _logger.LogWarning(ex, "Sync completed with status 200 but response parsing issue");
            return new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "User synced successfully"
            };
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error syncing user: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            _logger.LogInformation("Fetching current user");
            var user = await _apiClient.GetCurrentUserAsync();
            _logger.LogInformation("User found: {Email}", user?.Email);
            return user;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("User not found (404) - attempting auto-sync");

            // Auto-sync user if not found
            try
            {
                var syncResult = await SyncUserAsync();
                if (syncResult?.Success == true)
                {
                    _logger.LogInformation("Auto-sync successful, retrying GetCurrentUser");
                    await Task.Delay(100); // Wait for DB write to complete
                    return await _apiClient.GetCurrentUserAsync();
                }
                else
                {
                    _logger.LogWarning("Auto-sync failed: {Message}", syncResult?.Message);
                }
            }
            catch (Exception syncEx)
            {
                _logger.LogError(syncEx, "Auto-sync exception");
            }

            return null;
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            _logger.LogWarning("User not authenticated (401)");
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AdminAuthorityDto?> GetAdminAuthorityAsync()
    {
        try
        {
            return await _apiClient.GetCurrentUserAdminAuthorityAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403)
        {
            _logger.LogWarning("User is not authorized to fetch admin authority: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching admin authority: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin authority");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BaseCommandResponseOfGuid?> UpdateUserAsync(UpdateUserDto userDto)
    {
        try
        {
            _logger.LogInformation("Updating user");
            return await _apiClient.UpdateCurrentUserAsync(userDto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error updating user: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync()
    {
        try
        {
            _logger.LogInformation("Deleting user");
            await _apiClient.DeleteCurrentUserAsync();
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error deleting user: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return false;
        }
    }
}

// ABOUTME: Implementation of IUserAppearancePreferencesService wrapping IEventApiClient.
// ABOUTME: Handles error catching and logging for user appearance preferences.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class UserAppearancePreferencesService : IUserAppearancePreferencesService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<UserAppearancePreferencesService> _logger;

    public UserAppearancePreferencesService(IEventApiClient apiClient, ILogger<UserAppearancePreferencesService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserAppearancePreferencesDto> GetCurrentPreferencesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _apiClient.GetCurrentUserAppearancePreferencesAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user appearance preferences");
            throw;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdatePreferencesAsync(UpdateUserAppearancePreferencesDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            return await _apiClient.UpdateCurrentUserAppearancePreferencesAsync(dto, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user appearance preferences");
            return null;
        }
    }
}

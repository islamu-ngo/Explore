// ABOUTME: Event team service that delegates to the NSwag-generated IEventApiClient for team management.
// ABOUTME: Lists team members and performs event-role writes through BFF endpoints.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public class EventTeamService : IEventTeamService
{
    private readonly IEventApiClient _client;
    private readonly ILogger<EventTeamService> _logger;

    public EventTeamService(
        IEventApiClient client,
        ILogger<EventTeamService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ICollection<EventTeamMemberDto>> GetTeamMembersAsync(Guid eventId, bool includeInactive = false)
    {
        try
        {
            var result = await _client.GetEventTeamAsync(eventId, includeInactive);
            return result ?? new List<EventTeamMemberDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching team for event {EventId}", eventId);
            return new List<EventTeamMemberDto>();
        }
    }

    public async Task<CurrentUserEventPermissionsDto?> GetCurrentUserPermissionsAsync(Guid eventId)
    {
        try
        {
            return await _client.GetCurrentUserEventPermissionsAsync(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current user permissions for event {EventId}", eventId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> AssignRoleAsync(Guid eventId, string userEmail, int roleId)
    {
        try
        {
            var payload = new Clients.AssignEventTeamRoleRequest
            {
                UserEmail = userEmail,
                RoleId = roleId
            };
            return await _client.AssignEventRoleAsync(eventId, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning event role for event {EventId} to {UserEmail}", eventId, userEmail);
            return new BaseCommandResponseOfGuid { Success = false, Message = ex.Message };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> RevokeAssignmentAsync(Guid eventId, Guid assignmentId)
    {
        try
        {
            return await _client.RevokeEventRoleAsync(eventId, assignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking assignment {AssignmentId} for event {EventId}", assignmentId, eventId);
            return new BaseCommandResponseOfGuid { Success = false, Message = ex.Message };
        }
    }

    public async Task<ICollection<EventRolePresetDto>> GetAssignablePresetsAsync(Guid eventId)
    {
        try
        {
            var result = await _client.GetEventTeamAssignablePresetsAsync(eventId);
            return result ?? new List<EventRolePresetDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assignable presets for event {EventId}", eventId);
            return new List<EventRolePresetDto>();
        }
    }
}

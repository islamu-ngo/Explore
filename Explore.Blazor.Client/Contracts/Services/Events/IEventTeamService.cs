// ABOUTME: Contract for event team management operations consumed by Blazor UI components.
// ABOUTME: Provides team listing, role preset lookup, assignment, and revocation through the BFF.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface IEventTeamService
{
    Task<ICollection<EventTeamMemberDto>> GetTeamMembersAsync(Guid eventId, bool includeInactive = false);
    Task<CurrentUserEventPermissionsDto?> GetCurrentUserPermissionsAsync(Guid eventId);
    Task<ICollection<EventRolePresetDto>> GetAssignablePresetsAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> AssignRoleAsync(Guid eventId, string userEmail, int roleId);
    Task<BaseCommandResponseOfGuid?> RevokeAssignmentAsync(Guid eventId, Guid assignmentId);
}

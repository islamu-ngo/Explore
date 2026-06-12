// ABOUTME: Refit interface for event team BFF write endpoints not covered by the generated API client.
// ABOUTME: Keeps event-role assignment and revocation on the shared Refit/BFF transport pipeline.

using Explore.Blazor.Client.Clients;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface IEventTeamBffApi
{
    [Post("/api/eventteam/by-event/{eventId}/assignments")]
    Task<IApiResponse<BaseCommandResponseOfGuid>> AssignRoleAsync(
        Guid eventId,
        [Body] AssignEventTeamRoleRequest request,
        CancellationToken cancellationToken);

    [Delete("/api/eventteam/by-event/{eventId}/assignments/{assignmentId}")]
    Task<IApiResponse<BaseCommandResponseOfGuid>> RevokeAssignmentAsync(
        Guid eventId,
        Guid assignmentId,
        CancellationToken cancellationToken);
}

public sealed record AssignEventTeamRoleRequest(string UserEmail, int RoleId);

// ABOUTME: Shared command helpers for event-role assignment write handlers.
// ABOUTME: Centralizes response failure codes and same-event authority checks.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;

namespace Explore.Application.Features.EventRoleAssignments.Handlers.Commands;

public abstract class EventRoleAssignmentCommandHandlerBase
{
    protected static BaseCommandResponse<Guid> Failure(string message, string failureCode, Guid? id = null)
    {
        return new BaseCommandResponse<Guid>
        {
            Id = id ?? Guid.Empty,
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Errors = new List<string> { message }
        };
    }

    protected static BaseCommandResponse<Guid> Success(Guid id, string message)
    {
        return new BaseCommandResponse<Guid>
        {
            Id = id,
            Success = true,
            Message = message
        };
    }

    protected static async Task<Event?> GetEventInTenantAsync(
        IEventRepository eventRepository,
        Guid tenantId,
        Guid eventId)
    {
        var @event = await eventRepository.GetById(eventId);
        return @event is not null && @event.TenantId == tenantId ? @event : null;
    }

    protected static async Task<bool> UserExistsAsync(IUserRepository userRepository, Guid userId)
    {
        return await userRepository.Exists(userId);
    }

    protected static async Task<bool> HasOwnershipTransferAuthorityAsync(
        IEventAuthoritySnapshotService snapshotService,
        Guid tenantId,
        Guid eventId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetForUserAndEventsAsync(
            tenantId, actorUserId, new[] { eventId }, cancellationToken);

        return snapshot.Events.TryGetValue(eventId, out var authority) &&
               authority.PermissionCodes.Contains(PermissionCodes.EventTransferOwnership);
    }

    protected static BaseCommandResponse<Guid> AuthorityFailure(EventRoleAssignmentAuthorityResult result)
    {
        return Failure(
            result.ErrorMessage ?? "You do not have authority to assign this event role.",
            result.FailureCode ?? EventRoleAuthorityFailureCodes.AuthorityMissing);
    }

    protected static string RoleCodeFor(int roleId)
    {
        return Enum.IsDefined(typeof(RoleEnum), roleId)
            ? ((RoleEnum)roleId).ToString()
            : "unknown";
    }
}

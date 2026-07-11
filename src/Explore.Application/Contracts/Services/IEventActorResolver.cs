// ABOUTME: Contract for resolving the actor (org/group/personal) that owns a new event.
// ABOUTME: Encapsulates permission checks and publishing-policy enforcement.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Contracts.Services;

public interface IEventActorResolver
{
    Task<EventActorResult> ResolveAsync(
        Guid currentUserId,
        Guid? organizationId,
        Guid? groupId,
        CancellationToken cancellationToken);
}

public sealed class EventActorResult
{
    public bool Succeeded { get; private init; }
    public Guid ActorId { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string? ErrorDetail { get; private init; }
    public bool IsUserReported { get; private init; }

    public static EventActorResult Success(Guid actorId, bool isUserReported) =>
        new() { Succeeded = true, ActorId = actorId, IsUserReported = isUserReported };

    public static EventActorResult Failure(string message, string detail) =>
        new() { Succeeded = false, ErrorMessage = message, ErrorDetail = detail };
}

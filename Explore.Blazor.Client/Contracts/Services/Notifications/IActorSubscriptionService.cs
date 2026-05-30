// ABOUTME: Blazor service contract for current-user actor subscription operations.
// ABOUTME: Keeps components behind the BFF service layer instead of calling generated clients directly.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Notifications;

public interface IActorSubscriptionService
{
    Task<ActorSubscriptionDto?> GetSubscriptionAsync(Guid targetActorId, CancellationToken cancellationToken = default);

    Task<ActorSubscriptionCommandResult> SubscribeAsync(Guid targetActorId, CancellationToken cancellationToken = default);

    Task<ActorSubscriptionCommandResult> UnsubscribeAsync(Guid targetActorId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);

    Task<ActorSubscriptionCommandResult> UpdateNotificationLevelAsync(
        Guid targetActorId,
        int notificationLevelId,
        Guid expectedConcurrencyStamp,
        CancellationToken cancellationToken = default);
}

public sealed record ActorSubscriptionCommandResult(
    bool Success,
    Guid? SubscriptionId = null,
    string? Message = null,
    IReadOnlyList<string>? Errors = null)
{
    public static ActorSubscriptionCommandResult Failed(string message) =>
        new(false, Message: message, Errors: [message]);
}

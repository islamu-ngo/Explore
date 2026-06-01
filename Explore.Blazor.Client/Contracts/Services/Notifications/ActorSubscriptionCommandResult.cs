// ABOUTME: Result model for actor subscription create, update, and delete operations in the Blazor client.
// ABOUTME: Carries success, affected subscription identity, and user-facing error messages across the service boundary.

namespace Explore.Blazor.Client.Contracts.Services.Notifications;

public sealed record ActorSubscriptionCommandResult(
    bool Success,
    Guid? SubscriptionId = null,
    string? Message = null,
    IReadOnlyList<string>? Errors = null)
{
    public static ActorSubscriptionCommandResult Failed(string message) =>
        new(false, Message: message, Errors: [message]);
}

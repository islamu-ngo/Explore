// ABOUTME: Describes the resolved owner for a notification intent.
// ABOUTME: Records account-authority and external-provider details without invoking delivery infrastructure.

namespace Explore.Application.Notifications;

public sealed record NotificationOwnershipDecision(
    NotificationCategory Category,
    NotificationOwnership Ownership,
    AccountAuthorityKind AccountAuthorityKind = AccountAuthorityKind.None,
    ExternalWorkflowProviderKind ExternalWorkflowProviderKind = ExternalWorkflowProviderKind.None,
    bool RequiresLocalAudit = true)
{
    public bool IsLocalIslamuDelivery => Ownership == NotificationOwnership.IslamuEvent;
}

// ABOUTME: Identifies external workflow providers that may own internal provider notifications.
// ABOUTME: Keeps external delegation explicit instead of hiding it behind SMTP configuration.

namespace Explore.Application.Notifications;

public enum ExternalWorkflowProviderKind
{
    None = 0,
    Coop = 1,
    Osprey = 2,
    TicketingProvider = 3,
    WebhookProvider = 4,
    Other = 5
}

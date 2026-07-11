// ABOUTME: Stable lookup identifiers for durable notification intent ownership and delivery audit tables.
// ABOUTME: Values back normalized lookup rows seeded by LookupTableSeeder and must not be renumbered.

namespace Explore.Domain.Enums;

public enum NotificationCategoryEnum
{
    IdentityLifecycle = 1,
    ProductLifecycle = 2,
    EventLifecycle = 3,
    RegistrationLifecycle = 4,
    TrustSafetyReporting = 5,
    TrustSafetyModeration = 6,
    ProviderInternal = 7,
    PlatformOperations = 8,
    Marketing = 9
}

public enum NotificationOwnershipTypeEnum
{
    IslamuEvent = 1,
    AccountAuthority = 2,
    ExternalWorkflowProvider = 3,
    Disabled = 4
}

public enum NotificationIntentStatusEnum
{
    Pending = 1,
    Resolved = 2,
    DispatchQueued = 3,
    Delegated = 4,
    Skipped = 5,
    Failed = 6
}

public enum NotificationRecipientKindEnum
{
    User = 1,
    TenantAdmin = 2,
    Organizer = 3,
    Reporter = 4,
    Moderator = 5,
    ProviderOperator = 6,
    System = 7,
    Other = 8
}

public enum NotificationDeliveryStatusEnum
{
    Pending = 1,
    LinkedToEmailDispatch = 2,
    Sent = 3,
    Skipped = 4,
    Failed = 5,
    DeadLettered = 6
}

public enum NotificationExternalDelegationStatusEnum
{
    Pending = 1,
    Requested = 2,
    Accepted = 3,
    Delivered = 4,
    Failed = 5,
    Rejected = 6,
    Unknown = 7
}

public enum ExternalWorkflowProviderKindEnum
{
    None = 1,
    Coop = 2,
    Osprey = 3,
    TicketingProvider = 4,
    WebhookProvider = 5,
    Other = 6
}

public enum AccountAuthorityKindEnum
{
    Keycloak = 1,
    AtprotoPds = 2,
    IslamuOperatedPds = 3,
    LocalIdentity = 4,
    ExternalOidc = 5
}

// ABOUTME: Controlled notification categories used by ownership routing decisions.
// ABOUTME: Prevents provider and delivery settings from replacing product-domain responsibility rules.

namespace Explore.Application.Notifications;

public enum NotificationCategory
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

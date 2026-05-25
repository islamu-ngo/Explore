// ABOUTME: Lifecycle states for provider-neutral external binding correlation records.
// ABOUTME: Active bindings participate in provisioning idempotency; inactive states are reserved for future governance flows.

namespace Explore.Domain.Enums;

public enum ExternalBindingStatusEnum
{
    Active = 1,
    Suspended = 2,
    Archived = 3
}

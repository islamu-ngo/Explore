// ABOUTME: Enum for supported messaging providers in the pluggable messaging system.
// ABOUTME: Used by runtime messaging resolution and governance settings.

namespace Explore.Domain.Enums;

public enum MessagingProviderEnum
{
    None = 0,
    RabbitMq = 1,
    InMemory = 2
}

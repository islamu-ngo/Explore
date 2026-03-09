// ABOUTME: Enumerates supported ownership models for persisted external API keys.
// ABOUTME: Keeps credential ownership explicit without coupling the aggregate to one concrete owner table.

namespace Explore.Domain.Enums;

public enum ExternalApiKeyOwnerType
{
    User = 1,
    Organization = 2
}

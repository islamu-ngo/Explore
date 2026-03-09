// ABOUTME: Defines persisted lifecycle states for external API keys.
// ABOUTME: Active keys can authenticate; revoked keys fail closed even if the secret still matches.

namespace Explore.Domain.Enums;

public enum ExternalApiKeyStatus
{
    Active = 1,
    Revoked = 2
}

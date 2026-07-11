// ABOUTME: Defines machine-auth claim types shared by API authentication, middleware, and tests.
// ABOUTME: Separates persisted credentials from runtime principal identity and authorization context.

namespace Explore.Application.Constants;

public static class ApiAuthenticationClaimTypes
{
    public const string AuthMethod = "explore:auth:method";

    public const string ApiKeyId = "explore:api-key:id";

    public const string TenantId = "explore:tenant:id";

    public const string OwnerType = "explore:api-key:owner:type";

    public const string OwnerId = "explore:api-key:owner:id";

    public const string Scope = "explore:api-key:scope";
}

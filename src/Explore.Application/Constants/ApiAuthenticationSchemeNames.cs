// ABOUTME: Defines authentication scheme names used by the API host for direct-consumer auth dispatch.
// ABOUTME: Avoids scattered magic strings across Program, handlers, middleware, and tests.

namespace Explore.Application.Constants;

public static class ApiAuthenticationSchemeNames
{
    public const string MultiAuth = "MultiAuth";

    public const string ApiKey = "ApiKey";

    public const string AtprotoBootstrap = "AtprotoBootstrap";

    public const string AtprotoSession = "AtprotoSession";
}

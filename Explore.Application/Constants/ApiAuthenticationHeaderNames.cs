// ABOUTME: Centralizes API authentication header names shared between the API host and tests.
// ABOUTME: Keeps direct-consumer auth dispatch aligned on one explicit transport contract.

namespace Explore.Application.Constants;

public static class ApiAuthenticationHeaderNames
{
    public const string ApiKey = "X-API-Key";
}

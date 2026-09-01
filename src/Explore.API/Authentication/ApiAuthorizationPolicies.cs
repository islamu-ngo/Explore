// ABOUTME: Names API-local authorization policies for coarse HTTP endpoint gates.
// ABOUTME: Keeps the Admin role requirement separate from resource-level MediatR, Cerbos, and HAL authorization.

namespace Explore.API.Authentication;

public static class ApiAuthorizationPolicies
{
    public const string Admin = "Api.Admin";
}

// ABOUTME: Resolves current internal user id from provider identity claims when subject is not a GUID.
// ABOUTME: Supports provider-key link lookup first, then verified email fallback for Keycloak/Google.
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

public class ResolveCurrentUserIdByIdentityRequest : IRequest<Guid?>
{
    public required string Provider { get; init; }
    public required string ProviderId { get; init; }
    public string? Email { get; init; }
    public bool EmailVerified { get; init; }
}

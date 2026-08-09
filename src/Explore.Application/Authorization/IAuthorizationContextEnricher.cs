// ABOUTME: Defines request-specific authorization context enrichment for MediatR commands.
// ABOUTME: Keeps repository-backed policy context out of the generic authorization behavior.

namespace Explore.Application.Authorization;

public interface IAuthorizationContextEnricher<in TRequest>
    where TRequest : notnull
{
    Task<AuthorizationContext> ResolveAsync(TRequest request, CancellationToken cancellationToken);
}

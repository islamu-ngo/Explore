// ABOUTME: Resolves a visible federated event source only after rechecking the current tenant capability.
// ABOUTME: Revalidates the persisted redirect target through the shared HTTPS source policy.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Requests.Queries;
using Explore.Application.Services.Federation;
using Explore.Domain.Federation;
using MediatR;

namespace Explore.Application.Features.Federation.Atproto.Handlers.Queries;

public sealed class GetAtprotoEventSourceQueryHandler(
    IAtprotoEventProjectionRepository projectionRepository,
    AtprotoEventGovernanceResolver governanceResolver,
    Explore.Application.Contracts.Infrastructure.ITenantContext tenantContext)
    : IRequestHandler<GetAtprotoEventSourceQuery, string?>
{
    public async Task<string?> Handle(GetAtprotoEventSourceQuery request, CancellationToken cancellationToken)
    {
        AtprotoEventGovernance governance = await governanceResolver.ResolveAsync(
            tenantContext.TenantId,
            null,
            cancellationToken);
        if (!governance.EventsEnabled)
        {
            return null;
        }

        AtprotoEventProjection? projection = await projectionRepository.GetVisibleByRecordIdAsync(
            request.AtprotoRecordId,
            cancellationToken);
        return AtprotoExternalUriPolicy.Normalize(projection?.SourceUrl);
    }
}

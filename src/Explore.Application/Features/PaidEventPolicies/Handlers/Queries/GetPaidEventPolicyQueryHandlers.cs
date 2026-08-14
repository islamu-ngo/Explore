// ABOUTME: Handles paid-event policy read requests for instance and tenant scopes.
// ABOUTME: Projects active policy revisions into DTOs for management clients.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.PaidEventPolicies;
using Explore.Application.Features.PaidEventPolicies.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.PaidEventPolicies.Handlers.Queries;

public sealed class GetInstancePaidEventPolicyQueryHandler(IPaidEventPolicyRepository policies)
    : IRequestHandler<GetInstancePaidEventPolicyQuery, PaidEventPolicyDto?>
{
    public async Task<PaidEventPolicyDto?> Handle(GetInstancePaidEventPolicyQuery request, CancellationToken cancellationToken) =>
        (await policies.GetActiveInstanceAsync(cancellationToken)) is { } policy ? PaidEventPolicyMapper.ToDto(policy) : null;
}

public sealed class GetTenantPaidEventPolicyQueryHandler(IPaidEventPolicyRepository policies)
    : IRequestHandler<GetTenantPaidEventPolicyQuery, PaidEventPolicyDto?>
{
    public async Task<PaidEventPolicyDto?> Handle(GetTenantPaidEventPolicyQuery request, CancellationToken cancellationToken) =>
        request.TenantId == Guid.Empty || (await policies.GetActiveTenantAsync(request.TenantId, cancellationToken)) is not { } policy
            ? null
            : PaidEventPolicyMapper.ToDto(policy);
}

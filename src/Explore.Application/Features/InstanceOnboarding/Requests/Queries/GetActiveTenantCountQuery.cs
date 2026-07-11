// ABOUTME: Query contract for retrieving the count of active tenants in the system.
// Used by deployment mode toggle to enforce single-tenant revert safeguards.

using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public class GetActiveTenantCountQuery : IRequest<int>
{
}

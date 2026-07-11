// ABOUTME: Query handler for side-effect-free tenant plan assignment previews.
// ABOUTME: Compares current effective settings to a proposed plan draft before assignment.

using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Queries;

public sealed class PreviewControlPlaneTenantPlanDiffQueryHandler
    : IRequestHandler<PreviewControlPlaneTenantPlanDiffQuery, TenantPlanDiffResult>
{
    public Task<TenantPlanDiffResult> Handle(
        PreviewControlPlaneTenantPlanDiffQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(TenantPlanDiffService.Diff(request.Current, request.Draft));
    }
}

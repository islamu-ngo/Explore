// ABOUTME: Handles manual authorization policy package archive download requests.
// ABOUTME: Delegates archive construction to the provider-neutral Infrastructure package service seam.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public sealed class DownloadAuthorizationPolicyPackageQueryHandler(IPolicyPackageService policyPackageService)
    : IRequestHandler<DownloadAuthorizationPolicyPackageQuery, PolicyPackageArchive>
{
    public Task<PolicyPackageArchive> Handle(
        DownloadAuthorizationPolicyPackageQuery request,
        CancellationToken cancellationToken)
    {
        return policyPackageService.ExportArchiveAsync(cancellationToken);
    }
}

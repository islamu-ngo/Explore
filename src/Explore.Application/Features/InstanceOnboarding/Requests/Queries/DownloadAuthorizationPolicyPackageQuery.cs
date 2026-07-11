// ABOUTME: Query for exporting the current authorization policy package as a manual fallback archive.
// ABOUTME: Keeps controllers independent from provider-specific packaging and archive construction details.

using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

/// <summary>
/// Downloads the current authorization policy package archive for manual operator installation.
/// </summary>
public sealed class DownloadAuthorizationPolicyPackageQuery : IRequest<PolicyPackageArchive>
{
}

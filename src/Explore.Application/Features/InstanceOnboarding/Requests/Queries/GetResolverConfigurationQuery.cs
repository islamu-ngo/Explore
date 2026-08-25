// ABOUTME: Query contract for reading instance-level tenant resolver configuration.
// ABOUTME: Used by instance-admin APIs and future activation flows.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed record GetResolverConfigurationQuery : IRequest<ResolverConfigurationDto>
{
}

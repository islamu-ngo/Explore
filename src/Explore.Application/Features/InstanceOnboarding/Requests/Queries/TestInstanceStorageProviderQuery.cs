// ABOUTME: Query request for testing the currently selected instance storage provider.
// ABOUTME: Returns a provider-neutral health snapshot without exposing paths, keys, or secrets.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed class TestInstanceStorageProviderQuery : IRequest<InstanceStorageProviderStatusDto>
{
}

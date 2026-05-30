// ABOUTME: Query request for reading provider-neutral instance storage admin settings.
// ABOUTME: Returns redacted settings plus effective policy, usage, and provider status.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public class GetInstanceStorageSettingsQuery : IRequest<InstanceStorageSettingsDto>
{
}

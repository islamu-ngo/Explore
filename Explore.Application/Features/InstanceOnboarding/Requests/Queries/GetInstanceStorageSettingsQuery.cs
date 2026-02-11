// ABOUTME: Query request for reading instance-level S3 storage settings from SystemSetting records.
// ABOUTME: Returns InstanceStorageSettingsDto with current values for all 8 S3 configuration fields.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public class GetInstanceStorageSettingsQuery : IRequest<InstanceStorageSettingsDto>
{
}

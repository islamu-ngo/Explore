// ABOUTME: Query request for testing the current tenant's effective storage provider.
// ABOUTME: Returns bounded provider diagnostics without exposing storage credentials.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.TenantStorageSettings.Requests.Queries;

public sealed class TestTenantStorageProviderQuery : IRequest<InstanceStorageProviderStatusDto>
{
}

// ABOUTME: Query contract for reading auth provider configuration during setup and admin UI.
// ABOUTME: Returns configuration with secrets redacted (write-only pattern).

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed record GetAuthProviderConfigurationQuery : IRequest<AuthProviderConfigurationDto>
{
}

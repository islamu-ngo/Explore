// ABOUTME: Query contract for reading authorization provider configuration during setup and admin UI.
// ABOUTME: Returns configuration including environment detection state and Cerbos endpoint.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed record GetAuthorizationProviderConfigurationQuery : IRequest<AuthorizationProviderConfigurationDto>
{
}

// ABOUTME: MediatR query for checking whether localization has a TMS API key binding.
// ABOUTME: Keeps secret-binding repository access inside Application instead of API controllers.

using MediatR;

namespace Explore.Application.Features.Localization.Requests.Queries;

public sealed class GetLocalizationTmsApiKeyConfiguredQuery : IRequest<bool>
{
}

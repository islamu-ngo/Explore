// ABOUTME: Query for listing external API keys visible to the current user.
// ABOUTME: Aggregates the caller's own keys plus organization-owned keys they are allowed to manage.

using Explore.Application.DTOs.ExternalApiKey;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Requests.Queries;

public sealed record GetExternalApiKeyListRequest : IRequest<List<ExternalApiKeyListDto>>
{
}

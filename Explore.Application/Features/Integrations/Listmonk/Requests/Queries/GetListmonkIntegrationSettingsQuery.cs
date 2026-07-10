// ABOUTME: Query for sanitized tenant Listmonk integration settings.
// ABOUTME: Returns credential configured flags rather than secret values.

using Explore.Application.DTOs.Integrations;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Requests.Queries;

public sealed class GetListmonkIntegrationSettingsQuery : IRequest<ListmonkIntegrationSettingsDto>
{
}
